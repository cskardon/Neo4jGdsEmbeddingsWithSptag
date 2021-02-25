namespace SptagTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ANN.SPTAGManaged;
    using Neo4jClient;
    using Neo4jClient.Cypher;

    public class Neo4jVersionRetail : SptagSearcherBase
    {
        private const string CatalogName = "retail_graph";
        private const string ModelName = "graphsage_multipartite_retail";
        private const string DefaultDatabase = "retail";

        private const string Neo4jUri = "neo4j://localhost:7687";
        private const string Username = "neo4j";
        private const string Password = "neo";

        private readonly IGraphClient _graphClient;

        public Neo4jVersionRetail(int dimension, int k)
            : base(dimension, k)
        {
            _graphClient = new BoltGraphClient(Neo4jUri, Username, Password)
            {
                DefaultDatabase = DefaultDatabase
            };
            _graphClient.ConnectAsync().Wait();
        }

        public override async Task Run(bool reindex = true)
        {
            //Create the catalog
            await TryCreateCatalog(_graphClient);
            //Get the Embeddings from Neo4j
            var embeddings = await GetEmbeddingsFromNeo4j(_graphClient, 0, Dimension);

            const string l2IndexName = "retail-index-l2";
            const string cosineIndexName = "retail-index-cosine";

            if (reindex)
            {
                SetupIndex(l2IndexName, embeddings, Dimension, SptagHelper.Algorithms.Bkt, SptagHelper.Parameters.DistCalcMethod.L2);
                SetupIndex(cosineIndexName, embeddings, Dimension, SptagHelper.Algorithms.Bkt, SptagHelper.Parameters.DistCalcMethod.Cosine);
            }

            Console.WriteLine("--------------------------------------------------<<>>");
            SearchIndexWithOutput(l2IndexName, embeddings.Single(e => e.NodeId == 17), K);
            SearchIndexWithOutput(cosineIndexName, embeddings.Single(e => e.NodeId == 17), K);
            // await StoreInNeo4j(_graphClient, l2IndexName, embeddings, K, "SPTAG_GraphSAGE_L2");
            // await StoreInNeo4j(_graphClient, cosineIndexName, embeddings, K, "SPTAG_GraphSAGE_COSINE");
        }




        /// <summary>
        /// Will try to create the 'retail' catalog. If it exists
        /// already, it will just return, else it will create it.
        /// </summary>
        /// <param name="client">The <see cref="IGraphClient"/> instance to use to call on Neo4j.</param>
        private static async Task TryCreateCatalog(IGraphClient client)
        {
            var catalogExists = (await client.Cypher
                .Call($"gds.graph.exists('{CatalogName}')")
                .Yield("exists")
                .Return(exists => exists.As<bool>())
                .ResultsAsync).Single();

            if (!catalogExists)
            {
                Console.Write($"Creating {CatalogName} catalog...");
                await client.Cypher
                    .Call($@"gds.graph.create(
                              '{CatalogName}',
                              {{
                                Item: {{
                                  label: 'Item',
                                  properties: {{
                                    price: {{
                                      property: 'Price',
                                      defaultValue: 0.0
                                    }},
                                    StockCode: {{
                                     property: 'StockCode',
                                     defaultValue: 0
                                   }}
                                 }}
                                }},
                                Transaction: {{
                                  label: 'Transaction',
                                  properties: {{
                                   EpochTime:{{
       	                            property:'EpochTime',
                                    defaultValue:0
                                   }},
                                   TransactionID:{{
       	                            property:'TransactionID',
                                    defaultValue:0
                                   }}
                                 }}
                                }}
                             }}, {{
                                
                                CONTAINS: {{
                                  type: 'CONTAINS',
                                  orientation: 'UNDIRECTED',
                                  properties: {{
                                      Quantity:{{
                                          property:'Quantity',
                                          defaultValue: 0
                                      }}
                                  }}
                                }}
                            }})")
                    .ExecuteWithoutResultsAsync();

                Console.WriteLine("...Done");
            }

            var modelExists = (await client.Cypher
                .Call($"gds.beta.model.exists('{ModelName}')")
                .Yield("exists")
                .Return(exists => exists.As<bool>())
                .ResultsAsync).Single();

            if (!modelExists)
            {
                Console.Write("Training GraphSAGE...");

                await client.Cypher
                    .Call($@"gds.beta.graphSage.train(
                              'retail_graph',
                              {{
                                modelName: '{ModelName}',
                                featureProperties: ['price','StockCode','EpochTime','TransactionID'],
                                projectedFeatureDimension:6, //2 labels + 4 properties
                                degreeAsProperty: true, //adding more properties
                                epochs: 3, //how many times to traverse the graph during training
                                searchDepth:5 //depth of the random walk
                              }}
                            )")
                    .ExecuteWithoutResultsAsync();

                Console.WriteLine("...Done");
            }
        }
        
        public static async Task StoreInNeo4j(IGraphClient client, string indexName, IList<EmbeddingResult> embeddingResults, int k, string relationshipType)
        {
            var index = AnnIndex.Load(indexName);


            foreach (var embeddingResult in embeddingResults)
            {
                var newEmbeddingResult = embeddingResult;
                newEmbeddingResult.Embedding = new float[64];
                var result = SearchIndex(index, newEmbeddingResult, k);
                int i = 0;
            }

            var searchResults = embeddingResults.ToDictionary(
                embeddingResult => embeddingResult, 
                embeddingResult => SearchIndex(index, embeddingResult, k).Select(x => new ConvertedResult(x)).ToList()
                );

            Console.WriteLine($"Adding {relationshipType}...");
            foreach (var searchResult in searchResults)
            {
                Console.Write($"{searchResult.Key.NodeId}, ");
                await AddRelationshipQuery(client, searchResult, relationshipType).ExecuteWithoutResultsAsync();
            }

            Console.WriteLine();

        }




        private static ICypherFluentQuery AddRelationshipQuery(IGraphClient client, KeyValuePair<EmbeddingResult, List<ConvertedResult>> data, string typeName = "SPTAG")
        {
            var dataToUse = data.Value.Where(d => d.NodeId != data.Key.NodeId);

            ICypherFluentQuery query = new CypherFluentQuery(client);
            query = query
                .Match($"(n)")
                .Where("id(n) = $idParam")
                .Unwind(dataToUse, "ann")
                .Match("(n2)")
                .Where("id(n2) = ann.NodeId")
                .Create($"(n)-[r:{typeName}]->(n2)")
                .Set("r = ann")
                .WithParams( new
                {
                    idParam = data.Key.NodeId
                });

          //  var text = query.Query.DebugQueryText;

            return query;

        }
        

        private static void SetupIndex(string indexName, IList<EmbeddingResult> embeddings, int dimension, string algorithm, string distCalcMethod)
        {
            var index = new AnnIndex(algorithm, SptagHelper.ValueTypes.Float, dimension);
            index.SetBuildParam(nameof(SptagHelper.Parameters.DistCalcMethod), distCalcMethod);
            index.SetBuildParam(SptagHelper.Parameters.NumberOfThreads, "12");

            var data = GetByteArrayFromEmbeddings(embeddings);
            var meta = GetMetaFromEmbeddings(embeddings);

            index.BuildWithMetaData(data, meta, embeddings.Count, false);
            index.Save(indexName);
        }

        private static byte[] GetByteArrayFromEmbeddings(IList<EmbeddingResult> embeddings)
        {
            var byteEmbeddings = embeddings.Select(e => e.EmbeddingByteArray()).ToList();
            return byteEmbeddings.Concat();
        }

        private static byte[] GetMetaFromEmbeddings(IList<EmbeddingResult> embeddings)
        {
            var sb = new StringBuilder();
            foreach (var embeddingResult in embeddings)
                sb.Append(embeddingResult.NodeIdAsMetadata());

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        private static async Task<IList<EmbeddingResult>> GetEmbeddingsFromNeo4j(IGraphClient client, int limit = 0, int dimension = 10)
        {
            var query = client.Cypher
                .Call($"gds.beta.graphSage.stream('retail_graph', {{modelName: '{ModelName}'}})")
                .Yield("nodeId, embedding")
                .With($"{{{nameof(EmbeddingResult.NodeId)}: nodeId, {nameof(EmbeddingResult.Embedding)}: embedding}} AS embeddingResult")
                .Return(embeddingResult => embeddingResult.As<EmbeddingResult>());

            if (limit > 0)
                query = query.Limit(limit);

            var results = await query.ResultsAsync;
            return results.ToList();
        }


    }
}