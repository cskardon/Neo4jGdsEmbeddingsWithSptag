namespace SptagTests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ANN.SPTAGManaged;

    public abstract class SptagSearcherBase
    {
        protected SptagSearcherBase(int dimension, int k)
        {
            Dimension = dimension;
            K = k;
        }

        /// <summary>
        ///     Dimension of the Vectors
        /// </summary>
        protected int Dimension { get; }

        /// <summary>
        ///     Number of results to get
        /// </summary>
        protected int K { get; }

        /// <summary>
        ///     Create the index and run the search.
        /// </summary>
        public abstract Task Run(bool reindex = true);

        protected static BasicResult[] SearchIndex(AnnIndex index, EmbeddingResult embeddingResult, int k)
        {
            var results = index.SearchWithMetaData(embeddingResult.EmbeddingByteArray(), k);
            return results;
        }

        protected static void SearchIndexWithOutput(string indexName, EmbeddingResult embeddingResult, int k)
        {
            var results = SearchIndex(AnnIndex.Load(indexName), embeddingResult, k);
            for (var i = 0; i < results.Length; i++)
            {
                var converted = new ConvertedResult(results[i]);
                Console.WriteLine($"{i}:{converted.Dist}@({converted.VID},{converted.NodeId})");
            }
        }
    }
}