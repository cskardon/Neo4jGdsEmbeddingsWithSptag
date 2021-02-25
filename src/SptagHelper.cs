namespace SptagTests
{
    public static class SptagHelper
    {


        public static class Algorithms
        {
            /// <summary>Balanced K-Means Tree</summary>
            /// <remarks>Good for search accuracy in very high-dimensional data.</remarks>
            public const string Bkt = "BKT";

            /// <summary>KD-Tree and Relative Neighborhood Graph</summary>
            /// <remarks>Good for quicker index building.</remarks>
            public const string Kdt = "KDT";
        }

        public static class ValueTypes
        {
            public const string Float = "Float";
        }

        public static class Parameters
        {
            /// <summary>
            /// How many points will be sampled to do tree node split (type = int, default = 1000)
            /// </summary>
            public const string Samples = "Samples";

            /// <summary>
            /// Number of TPT trees to help with graph construction (type = int, default = 32)
            /// </summary>
            /// <remarks>Affects index build time and index quality</remarks>
            public const string TptNumber = "TPTNumber";

            /// <summary>
            /// TPT tree leaf size (type = int, default = 2000)
            /// </summary>
            /// <remarks>Affects index build time and index quality</remarks>
            public const string TptLeafSize = "TPTLeafSize";

            /// <summary>
            /// Number of neighbors each node has in the neighborhood graph (type = int, default = 32)
            /// </summary>
            /// <remarks>Affects index size and index quality</remarks>
            public const string NeighborhoodSize = "NeighborhoodSize";

            /// <summary>
            /// Number of neighborhood size scale in the build stage (type = int, default = 2)
            /// </summary>
            /// <remarks>Affects index build time and index quality</remarks>
            public const string GraphNeighborhoodScale = "GraphNeighborhoodScale";

            /// <summary>
            /// Number of results used to construct RNG (type = int, default = 1000)
            /// </summary>
            /// <remarks>Affects index build time and index quality</remarks>
            public const string Cef = "Cef";

            /// <summary>
            /// How many nodes each node will visit during graph refine in the build stage (type = int, default = 10000)
            /// </summary>
            /// <remarks>Affects index build time and index quality</remarks>
            public const string MaxCheckForRefineGraph = "MaxCheckForRefineGraph";

            /// <summary>
            /// Number of threads to uses for speed up the build (type = int, default = 1)
            /// </summary>
            /// <remarks>Affects index build time</remarks>
            public const string NumberOfThreads = "NumberOfThreads";

            /// <summary>
            /// Choose from Cosine and L2 (type = string, default = cosine)
            /// </summary>
            public static class DistCalcMethod
            {
                public const string L2 = "L2";
                public const string Cosine = "Cosine";
            }

            /// <summary>
            /// How many nodes will be visited for a query in the search stage (type = int, default = 8192)
            /// </summary>
            /// <remarks>Affects search latency and recall</remarks>
            public const string MaxCheck = "MaxCheck";

            public static class Bkt
            {
                /// <summary>
                /// Number of BKT Trees (type = int, default = 1)
                /// </summary>
                /// <remarks>Affects index size</remarks>
                public const string BktNumber = "BKTNumber";

                /// <summary>
                /// How many children each tree node has (type = int, default = 32)
                /// </summary>
                public const string BktKMeansK = "BKTKMeansK";
            }

            public static class Kdt
            {
                /// <summary>
                /// Number of KDT Trees (type = int, default = 1)
                /// </summary>
                /// <remarks>Affects index size and index quality</remarks>
                public const string KdtNumber = "KDTNumber";
            }
        }
    }
}