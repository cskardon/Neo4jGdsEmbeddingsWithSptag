namespace SptagTests
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ANN.SPTAGManaged;

    internal class Original : SptagSearcherBase
    {
        /// <summary>
        /// The number of embeddings to generate
        /// </summary>
        protected int N {get;}

        public Original(int dimension, int n, int k)
            : base(dimension, k)
        {
            N = n;
        }

        private static void Search(string indexName, int dimension, int k)
        {
            var index = AnnIndex.Load(indexName);
            var res = index.SearchWithMetaData(CreateFloatArray(1, dimension), k);
            for (var i = 0; i < res.Length; i++)
                Console.WriteLine($"result {i}:{res[i].Dist}@({res[i].VID},{Encoding.ASCII.GetString(res[i].Meta)})");
        }

        private static void SetupIndex(string name, int dimension, int n)
        {
            //AlgoType, Value Type, Dimension
            var idx = new AnnIndex(SptagHelper.Algorithms.Bkt, SptagHelper.ValueTypes.Float, dimension);

            idx.SetBuildParam(nameof(SptagHelper.Parameters.DistCalcMethod), SptagHelper.Parameters.DistCalcMethod.L2);
            var data = CreateFloatArray(n, dimension);
            var meta = CreateMetadata(n);
            idx.BuildWithMetaData(data, meta, n, false);
            idx.Save(name);
        }

        private static byte[] CreateFloatArray(int n, int dimension)
        {
            //10 * 10 * 4
            var data = new byte[n * dimension * sizeof(float)];
            for (var i = 0; i < n; i++)
            {
                for (var j = 0; j < dimension; j++)
                {
                    Array.Copy(BitConverter.GetBytes((float) i), 0, data, (i * dimension + j) * sizeof(float), 4); //length 4 = size of float
                }
            }

            return data;
        }

        //Is this the 'Node ID' in my case?
        private static byte[] CreateMetadata(int n)
        {
            var sb = new StringBuilder();
            
            for (var i = 0; i < n; i++)
                sb.Append(i.ToString() + '\n');


            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        public override Task Run(bool reindex = true)
        {
            const string indexName = "testcsharp";
            if(reindex)
                SetupIndex(indexName, Dimension, N);
            
            Search(indexName, Dimension, K);
            
            return Task.CompletedTask;
        }
    }
}