namespace SptagTests
{
    using System.Text;
    using Microsoft.ANN.SPTAGManaged;

    public class ConvertedResult
    {
        public ConvertedResult(BasicResult basicResult)
        {
            NodeId = basicResult.VID == -1 ? -1 : int.Parse(Encoding.ASCII.GetString(basicResult.Meta));
            VID = basicResult.VID;
            Dist = basicResult.Dist;
        }

        public int NodeId { get; set; }
        /// <summary>
        /// Vector ID?
        /// </summary>
        public int VID { get; set; }

        public float Dist { get; set; }
    }
}