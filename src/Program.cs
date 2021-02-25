namespace SptagTests
{
    using System;
    using System.Threading.Tasks;

    /*
     * Attempts to use Neo4j GDS with MS SPTAG
     *
     * To Run: Needs to be x64 (not Any CPU)
     */

    internal class Program
    {
        private static async Task Main(string[] args)
        {
            const int dimension = 50;
            const int k = 6;

            // var original = new Original(dimension, 10, k);
            // await original.Run();

            // var neo4jVersion = new Neo4jVersion(dimension, k);
            // await neo4jVersion.Run(false);

            var neo4jVersionRetail = new Neo4jVersionRetail(64, k);
            await neo4jVersionRetail.Run(false);

            
            Console.WriteLine("Finish!");
            Console.ReadLine();
        }
    }
}