using System.Numerics;

namespace CryptoVarna.PheVotingNet
{
    public partial class Paillier
    {
        public class PrivateKey
        {
            public BigInteger Lambda { get; set; }
            public BigInteger Mu { get; set; }

            public PrivateKey() { }

            public PrivateKey(BigInteger lambda, BigInteger mu)
            {
                Lambda = lambda;
                Mu = mu;
            }
        }
    }
}
