using System.Numerics;

namespace CryptoVarna.PheVotingNet
{
    public partial class Paillier
    {
        public class PublicKey
        {
            public BigInteger N { get; set; }
            public BigInteger NSquared { get; set; }
            public BigInteger G { get; set; }

            public PublicKey() { }

            public PublicKey(BigInteger n, BigInteger g)
            {
                N = n;
                G = g;
                NSquared = n * n;
            }
        }
    }
}
