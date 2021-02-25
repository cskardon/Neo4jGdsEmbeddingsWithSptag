namespace SptagTests
{
    using System;
    using System.Linq;

    public class EmbeddingResult : IEquatable<EmbeddingResult>
    {
        public int NodeId { get; set; }
        public float[] Embedding { get; set; }

        public string NodeIdAsMetadata()
        {
            return $"{NodeId}\n";
        }

        public byte[] EmbeddingByteArray()
        {
            var arrays = Embedding.Select(BitConverter.GetBytes).ToList();
            return arrays.Concat();
        }

        public bool Equals(EmbeddingResult other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return NodeId == other.NodeId && Equals(Embedding, other.Embedding);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((EmbeddingResult) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (NodeId * 397) ^ (Embedding != null ? Embedding.GetHashCode() : 0);
            }
        }
    }
}