using System.Numerics;

namespace CryptoVarna.PheVotingNet
{
    public partial class Paillier
    {
        public class Signature
        {
            public BigInteger S1 { get; set; }
            public BigInteger S2 { get; set; }

            public Signature() { }

            public Signature(BigInteger s1, BigInteger s2)
            {
                S1 = s1;
                S2 = s2;
            }
        }
    }
}
