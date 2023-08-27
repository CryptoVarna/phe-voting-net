using System.Numerics;

namespace CryptoVarna.PheVotingNet
{
    public partial class Paillier
    {
        public class ZkpCommitment
        {
            public List<BigInteger> A { get; set; }
            public List<BigInteger> E { get; set; }
            public List<BigInteger> Z { get; set; }

            public ZkpCommitment()
            {
                A = default!;
                E = default!;
                Z = default!;
            }

            public ZkpCommitment(List<BigInteger> a, List<BigInteger> e, List<BigInteger> z)
            {
                A = a;
                E = e;
                Z = z;
            }

            public ZkpCommitment(int capacity = 0)
            {
                A = new List<BigInteger>(capacity);
                E = new List<BigInteger>(capacity);
                Z = new List<BigInteger>(capacity);
            }
        }
    }
}
