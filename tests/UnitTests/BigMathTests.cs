using Xunit;
using System.Numerics;
using System.Security.Cryptography;

namespace CryptoVarna.PheVotingNet.Tests
{
    public class BigMathTests
    {
        [Theory]
        [InlineData("1", false)]
        [InlineData("2", true)]
        [InlineData("3", true)]
        [InlineData("4", false)]
        [InlineData("27", false)]
        [InlineData("221924657", true)]
        [InlineData("179424517", true)]
        [InlineData("109494317", false)]
        public void IsPrime(string input, bool expected)
        {
            Assert.Equal(BigInteger.Parse(input).IsPrime(), expected);
        }

        [Fact]
        public void TestFirstPrimes()
        {
            foreach (int prime in BigMath.FirstPrimes)
            {
                Assert.True(BigMath.IsPrime(prime));
            }

        }

        [Theory]
        [InlineData("120", "428860", "20", "3574", "-1")]
        [InlineData("95642", "1681", "1", "682", "-38803")]
        [InlineData("180324234311", "1502342", "1", "249631", "-29962897220")]
        public void ExtendedGcd(string a, string b, string expectedRemainder, string expectedX, string expectedY)
        {
            var (r, x, y) = BigMath.ExtendedGcd(BigInteger.Parse(a), BigInteger.Parse(b));
            Assert.Equal(r, BigInteger.Parse(expectedRemainder));
            Assert.Equal(x, BigInteger.Parse(expectedX));
            Assert.Equal(y, BigInteger.Parse(expectedY));
        }

        [Theory]
        [InlineData("0", "5", "0")]
        [InlineData("5", "5", "0")]
        [InlineData("-1", "5", "4")]
        [InlineData("1431655765", "129140163", "11113972")]
        [InlineData("-1431655765", "129140163", "118026191")]
        public void PositiveMod(string a, string n, string expected)
        {
            Assert.Equal(BigMath.PositiveMod(BigInteger.Parse(a), BigInteger.Parse(n)), BigInteger.Parse(expected));
        }

        [Theory]
        [InlineData("0", 1)]
        [InlineData("1", 1)]
        [InlineData("2", 2)]
        [InlineData("4", 3)]
        [InlineData("15", 4)]
        [InlineData("16", 5)]
        [InlineData("127", 7)]
        [InlineData("128", 8)]
        [InlineData("179424517", 28)]
        [InlineData("10942194317", 34)]
        public void BitLength(string input, int expected)
        {
            Assert.Equal(BigInteger.Parse(input).BitLength(), expected);
        }

        [Fact]
        public void CreatePositiveNumber256()
        {
            for (int i = 1; i < 100; i += 2)
            {
                byte[] data = Hashing.Sha256Bytes(BitConverter.GetBytes(i));
                Assert.Equal(32, data.Length);
                BigInteger bn = BigMath.CreatePositiveNumber(data);
                Assert.True(bn.Sign >= 0);
                Assert.Equal(256, bn.BitLength());
            }
        }

        [Fact]
        public void CreatePositiveNumberOddLength()
        {
            for (int i = 1; i < 100; i++)
            {
                byte[] data = Hashing.Sha256Bytes(BitConverter.GetBytes(i));
                BigInteger bn = BigMath.CreatePositiveNumber(data, i);
                Assert.True(bn.Sign >= 0);
                Assert.Equal(i, bn.BitLength());
            }
        }

        [Fact]
        public void CreatePositiveNumberShortData()
        {
            byte[] data = Hashing.Sha256Bytes(BitConverter.GetBytes(256));
            BigInteger bn = BigMath.CreatePositiveNumber(data, 512);
            Assert.True(bn.Sign >= 0);
            Assert.Equal(512, bn.BitLength());
        }

        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(47)]
        [InlineData(256)]
        public void GenerateRandom(int bits)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                BigInteger n = BigMath.GenerateRandom(bits, rng);
                Assert.Equal(n.BitLength(), bits);
            }
        }

        [Theory]
        [InlineData(2)]
        [InlineData(7)]
        [InlineData(8)]
        [InlineData(16)]
        [InlineData(47)]
        [InlineData(256)]
        public void GenerateRandomPrime(int bits)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                BigInteger n = BigMath.GenerateRandomPrime(bits, rng);
                Assert.Equal(n.BitLength(), bits);
            }
        }
        [Theory]
        [InlineData("27", "5", "3")]
        [InlineData("313", "666", "283")]
        [InlineData("13071045182806587517", "17251775975084797103", "11872988828737907282")]
        [InlineData("242247844719048885554803570785517513399", "303973086149861113256557118428879895141", "1078163951979426678640449531090748273")]
        [InlineData("9743058751872955419186937937098934043617740051423303343669999731870069068315939620757085681843800296979658975377935910429450130682071669025623356152414071", "10918124914316066474370068470773121422369575386029444828405443603442066581336833549701079673432573651310160456275625573092089494734700078849324448506698259", "8032459876848510187578790026438602343361385137976737623650664493180883339144418543736465474079653597036190862024047326280631854974420781116743995466601567")]
        public void ModInverse(string a, string b, string expected)
        {
            Assert.Equal(BigMath.ModInverse(BigInteger.Parse(a), BigInteger.Parse(b)), BigInteger.Parse(expected));
        }

        [Theory]
        [InlineData("0", "0")]
        [InlineData("2", "4")]
        [InlineData("3", "9")]
        public void ModInverseBadArguments(string a, string b)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BigMath.ModInverse(BigInteger.Parse(a), BigInteger.Parse(b)));
        }

        [Theory]
        [InlineData("179", 8)]
        [InlineData("60917", 16)]
        [InlineData("3529232269", 32)]
        [InlineData("13003964625990873607", 64)]
        public void GenerateCoprime(string input, int bits)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var a = BigInteger.Parse(input);
                var coPrime = a.GenerateCoprime(bits, rng);
                Assert.True(coPrime.IsCoprime(a));
                Assert.Equal(coPrime.BitLength(), bits);
            }
        }

        [Fact]
        public void Base64()
        {
            for (int i = 2; i < 4096; i *= 2)
            {
                BigInteger n = BigInteger.Pow(2, i);
                var base64 = n.ToBase64();
                var bn = BigMath.FromBase64(base64);
                Assert.Equal(n, bn);
            }
        }
    }
}
