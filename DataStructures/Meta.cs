using System.Numerics;

namespace FtLib
{
    /// <summary> 
    /// Struct-like class for holding meta information. Immutable.
    /// </summary>
    public class Meta
    {
        public string Name { get; }
        public BigInteger Size { get; }
        public Meta(string name, BigInteger size)
        {
            this.Name = name;
            this.Size = size;
        }
    }
}