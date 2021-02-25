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

    public class Neo4jVersion : SptagSearcherBase
    {
        private const string CatalogName = "got-people";
        private const string DefaultDatabase = "gameofthrones";

        private const string Neo4jUri = "neo4j://localhost:7687";
        private const string Username = "neo4j";
        private const string Password = "neo";


        private readonly IGraphClient _graphClient;

        public Neo4jVersion(int dimension, int k)
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
            await TryCreateGotPeople(_graphClient);
            //Get the Embeddings from Neo4j
            var embeddings = await GetEmbeddingsFromNeo4j(_graphClient, 0, Dimension);

            const string l2IndexName = "got-index-l2";
            const string cosineIndexName = "got-index-cosine";

            if (reindex)
            {
                SetupIndex(l2IndexName, embeddings, Dimension, SptagHelper.Algorithms.Bkt, SptagHelper.Parameters.DistCalcMethod.L2);
                SetupIndex(cosineIndexName, embeddings, Dimension, SptagHelper.Algorithms.Bkt, SptagHelper.Parameters.DistCalcMethod.Cosine);
            }

            Console.WriteLine("--------------------------------------------------<<>>");
            // SearchIndexWithOutput(l2IndexName, embeddings.Single(e => e.NodeId == 171), K);
            // SearchIndexWithOutput(cosineIndexName, embeddings.Single(e => e.NodeId == 171), K);
            await StoreInNeo4j(_graphClient, l2IndexName, embeddings, K, "SPTAG_L2");
            await StoreInNeo4j(_graphClient, cosineIndexName, embeddings, K, "SPTAG_COSINE");
        }




        /// <summary>
        /// Will try to create the 'got-people' catalog. If it exists
        /// already, it will just return, else it will create it.
        /// </summary>
        /// <param name="client">The <see cref="IGraphClient"/> instance to use to call on Neo4j.</param>
        private static async Task TryCreateGotPeople(IGraphClient client)
        {
            var gotPeopleExists = (await client.Cypher
                .Call($"gds.graph.exists('{CatalogName}')")
                .Yield("exists")
                .Return(exists => exists.As<bool>())
                .ResultsAsync).Single();

            if (gotPeopleExists)
                return;

            await client.Cypher
                .Call($@"gds.graph.create(
                             '{CatalogName}', 
                             'Person', 
                             {{
                                 INTERACTS: {{ orientation: 'UNDIRECTED'}}
                             }}
                        )")
                .ExecuteWithoutResultsAsync();
        }

       

        public static async Task StoreInNeo4j(IGraphClient client, string indexName, IList<EmbeddingResult> embeddingResults, int k, string relationshipType)
        {
            var index = AnnIndex.Load(indexName);
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
            
            return query;

        }

        private static void SetupIndex(string indexName, IList<EmbeddingResult> embeddings, int dimension, string algorithm, string distCalcMethod)
        {
            var index = new AnnIndex(algorithm, SptagHelper.ValueTypes.Float, dimension);
            index.SetBuildParam(nameof(SptagHelper.Parameters.DistCalcMethod), distCalcMethod);
            index.SetBuildParam(SptagHelper.Parameters.NumberOfThreads, "4");

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
                .Call($"gds.fastRP.stream('{CatalogName}', {{ embeddingDimension: {dimension}}})")
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