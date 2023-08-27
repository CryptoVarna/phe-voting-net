using Xunit;
using System.Numerics;

namespace CryptoVarna.PheVotingNet.Tests
{
    public class HashingTests
    {
        [Theory]
        [InlineData("1", "6b86b273ff34fce19d6b804eff5a3f5747ada4eaa22f1d49c01e52ddb7875b4b")]
        [InlineData("2", "d4735e3a265e16eee03f59718b9b5d03019c07d8b6c51f90da3a666eec13ab35")]
        [InlineData("The quick brown fox jumps over the lazy dog", "d7a8fbb307d7809469ca9abcb0082e4f8d5651e46d3cdb762d02d0bf37c9e592")]
        public void Sha256String(string input, string expected)
        {
            Assert.Equal(Hashing.Sha256(input), expected);
        }

        [Theory]
        [InlineData("1", "69779012276202546540741613998220636891790827476075440677599814057037833368907")]
        [InlineData("115792089237316195423570985008687907853269984665640564039457584007913129639936", "101269101239529058805446891785578411241239202721812756881225539903539495883807")]
        [InlineData("5288447750321988791615322464262168318627237463714249754277190362195246329890490766601513683517722278780729696200186866434048", "115725342810475012330786269972724152072837003485246572940632249624415138572568")]
        [InlineData("9530038175818582050", "98427123223779439728095785902276362274174097928697797684547286593556878676893")]
        public void Sha256BigInt(string input, string expected)
        {
            Assert.Equal(Hashing.Sha256BigInt(BigInteger.Parse(input)), BigInteger.Parse(expected));
        }

        public static IEnumerable<object[]> GetInputBigIntArrays()
        {
            yield return new object[]
            {
                new BigInteger[] { 1, 2, 3 },
                BigInteger.Parse("58792029373485431080020190069280238670986975160278682014465441287328362958851")
            };

            yield return new object[]
            {
                new BigInteger[]
                {
                    BigInteger.Pow(2, 256),
                    BigInteger.Pow(2, 512),
                    BigInteger.Pow(2, 1024),
                    BigInteger.Pow(2, 2048)
                },
                BigInteger.Parse("111283729660902656606379717697774823971458332087106280608132549324510598677818")
            };
        }

        [Theory]
        [MemberData(nameof(GetInputBigIntArrays))]
        public void Sha256BigIntArray(BigInteger[] input, BigInteger expected)
        {
            Assert.Equal(Hashing.Sha256BigInt(input), expected);
        }

        public static IEnumerable<object[]> GetInputBytes()
        {
            yield return new object[]
            {
                new byte[] { 0x01, 0x02, 0x03 },
                "039058c6f2c0cb492c533b0a4d14ef77cc0f78abccced5287d84a1a2011cfb81"
            };

            yield return new object[]
            {
                new byte[] { 0x48, 0x65, 0x6c, 0x6c, 0x6f, 0x20, 0x57, 0x6f, 0x72, 0x6c, 0x64, 0x21 },
                "7f83b1657ff1fc53b92dc18148a1d65dfc2d4b1fa3d677284addd200126d9069"
            };
        }

        [Theory]
        [MemberData(nameof(GetInputBytes))]
        public void Sha256Bytes(byte[] input, string expected)
        {
            Assert.Equal(Hashing.Sha256(input), expected);
        }

        [Theory]
        [InlineData("")]
        public void Sha256BadArguments(string input)
        {
            Assert.Throws<ArgumentNullException>(() => Hashing.Sha256(input));
        }
    }
}
