using Xunit;
using System.Security.Cryptography;
using CryptoVarna.PheVotingNet.Tests.Helpers;

namespace CryptoVarna.PheVotingNet.Tests
{
    public class RandomGeneratorTests
    {
        private static byte[] GenerateRandomBytesFromBigInteger(int bits, int count)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var bList = new List<byte>();
                for (int i = 0; i < count; i++)
                {
                    var r = BigMath.GenerateRandom(bits, rng);
                    bList.AddRange(r.ToUnsignedByteArray());
                }
                return bList.ToArray();
            }
        }

        [Theory(Skip = "Work in progress")]
        [InlineData(128, 100)]
        [InlineData(256, 100)]
        [InlineData(512, 80)]
        [InlineData(1024, 40)]
        public void FrequencyMonobitsTest(int bits, int count)
        {
            var data = GenerateRandomBytesFromBigInteger(bits, count);
            Assert.True(RngStatTests.FrequencyMonobitTest(data));
        }

        [Theory(Skip = "Work in progress")]
        [InlineData(128, 100)]
        [InlineData(256, 100)]
        [InlineData(512, 80)]
        [InlineData(1024, 40)]
        public void RunTest(int bits, int count)
        {
            var data = GenerateRandomBytesFromBigInteger(bits, count);
            Assert.True(RngStatTests.RunTest(data));
        }
    }
}
