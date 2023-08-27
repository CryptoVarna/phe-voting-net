using Xunit;
using System.Numerics;

namespace CryptoVarna.PheVotingNet.Tests
{
    public class VoteEncoderTests
    {
        [Theory]
        [InlineData(0, 3, 8, "1")]
        [InlineData(1, 3, 8, "256")]
        [InlineData(2, 3, 8, "65536")]
        public void EncodeWithoutGrouping(int choice, int numChoices, int bitsPerChoice, string expected)
        {
            var encoded = VoteEncoder.Encode(choice, numChoices, bitsPerChoice);
            Assert.Equal(encoded, BigInteger.Parse(expected));
            var decoded = VoteEncoder.Decode(encoded, numChoices, bitsPerChoice);
            Assert.Equal(1, decoded[choice]);
        }

        [Theory]
        [InlineData(0, 2, 0, 3, 8, "1")]
        [InlineData(1, 2, 0, 3, 8, "256")]
        [InlineData(0, 2, 1, 3, 8, "65536")]
        [InlineData(1, 2, 1, 3, 8, "16777216")]
        public void EncodeWithGrouping(int choice, int numChoices, int bin, int numBins, int bitsPerChoice, string expected)
        {
            var encoded = VoteEncoder.Encode(choice, numChoices, bin, numBins, bitsPerChoice);
            Assert.Equal(encoded, BigInteger.Parse(expected));
            var decoded = VoteEncoder.Decode(encoded, numChoices, numBins, bitsPerChoice);
            Assert.Equal(1, decoded[bin, choice]);
        }

        [Theory]
        [InlineData(2, 2, 0, 2, 8)]   // choice >= numChoices
        [InlineData(1, 2, 2, 2, 8)]   // bin >= numBins
        [InlineData(1, 20, 1, 20, 1)] // low bits
        public void EncodeWithBadArguments(int choice, int numChoices, int bin, int numBins, int bitsPerChoice)
        {
            Assert.Throws<ArgumentException>(() => VoteEncoder.Encode(choice, numChoices, bin, numBins, bitsPerChoice));
        }

        public static IEnumerable<object[]> GetMultipleVotesWithoutGrouping()
        {
            yield return new object[] { new int[] { 0 }, 2, 8, "1" };
            yield return new object[] { new int[] { 0, 1 }, 2, 8, "257" };
            yield return new object[] { new int[] { 0, 1, 2 }, 3, 8, "65793" };
        }

        [Theory]
        [MemberData(nameof(GetMultipleVotesWithoutGrouping))]
        public void EncodeMultipleWithoutGrouping(int[] choices, int numChoices, int bitsPerChoice, string expected)
        {
            var encoded = VoteEncoder.Encode(choices, numChoices, bitsPerChoice);
            Assert.Equal(encoded, BigInteger.Parse(expected));
            var decoded = VoteEncoder.Decode(encoded, numChoices, bitsPerChoice);
            foreach (int i in choices)
                Assert.True(decoded[i] == 1);
        }

        public static IEnumerable<object[]> GetMultipleVotesWithGrouping()
        {
            yield return new object[] { new int[] { 0 }, 2, 0, 2, 8, "1" };
            yield return new object[] { new int[] { 0, 1 }, 2, 1, 2, 8, "16842752" };
            yield return new object[] { new int[] { 0, 1, 2 }, 3, 1, 2, 8, "1103823372288" };
        }

        [Theory]
        [MemberData(nameof(GetMultipleVotesWithGrouping))]
        public void EncodeDecodeMultipleWithGrouping(int[] choices, int numChoices, int bin, int numBins, int bitsPerChoice, string expected)
        {
            var encoded = VoteEncoder.Encode(choices, numChoices, bin, numBins, bitsPerChoice);
            Assert.Equal(encoded, BigInteger.Parse(expected));
            var decoded = VoteEncoder.Decode(encoded, numChoices, numBins, bitsPerChoice);
            foreach (int i in choices)
                Assert.True(decoded[bin, i] == 1);
        }
        //

        [Theory]
        [InlineData(10, 10, 8, 800)]
        [InlineData(2, 2, 32, 128)]
        [InlineData(0, 0, 0, 0)]
        [InlineData(20, 20, 32, 12800)]
        public void GetTotalVotesCount(int numChoices, int numBins, int bitsPerChoice, int expected)
        {
            Assert.Equal(VoteEncoder.GetTotalVotesBits(numChoices, numBins, bitsPerChoice), expected);
        }

        [Theory]
        [InlineData(5, 100, 160, 8)]
        [InlineData(2, 100, 256, 8)]
        //[InlineData(20, 200, 256, 8)]
        public void EncryptedAggregationWithoutGrouping(int numChoices, int numVotes, int keySize, int bitsPerChoice)
        {
            using (var p = new Paillier())
            {
                var rnd = new Random();
                // Key generation
                var (pub, priv) = p.GenerateKeyPair(keySize);

                var validVotes = VoteEncoder.GetSingleChoicePermutations(numChoices, bitsPerChoice);
                var realVotes = new int[validVotes.Count];

                BigInteger sum = BigInteger.Zero;
                BigInteger encryptedSum = p.Encrypt(BigInteger.Zero, pub);

                for (int i = 0; i < numVotes; i++)
                {
                    // Choose random vote
                    int vote = rnd.Next(numChoices);
                    // Encode vote
                    var encodedVote = VoteEncoder.Encode(vote, numChoices, bitsPerChoice);
                    // Encrypt vote
                    var (encryptedVote, commitment) = p.EncryptWithZkp(encodedVote, validVotes, pub);

                    // Check vote validity
                    var valid = p.VerifyZkp(encryptedVote, validVotes, commitment, pub);
                    Assert.True(valid);
                    // Aggregate votes
                    realVotes[vote]++;
                    encryptedSum = p.AddEncrypted(encryptedSum, encryptedVote, pub);
                }

                BigInteger decryptedSum = p.Decrypt(encryptedSum, pub, priv);
                int[] decodedSum = VoteEncoder.Decode(decryptedSum, numChoices, bitsPerChoice);
                for (int i = 0; i < decodedSum.Length; i++)
                    Assert.Equal(decodedSum[i], realVotes[i]);
            }
        }

        [Theory]
        [InlineData(5, 100, 0, 2, 160, 8)]
        [InlineData(2, 100, 1, 2, 512, 8)]
        //[InlineData(20, 200, 2, 3, 512, 8)]
        public void EncryptedAggregationWithGrouping(int numChoices, int numVotes, int bin, int numBins, int keySize, int bitsPerChoice)
        {
            using (var p = new Paillier())
            {
                var rnd = new Random();
                // Key generation
                var (pub, priv) = p.GenerateKeyPair(keySize);

                var validVotes = VoteEncoder.GetSingleChoicePermutations(numChoices, numBins, bitsPerChoice);
                var realVotes = new int[validVotes.Count];

                BigInteger sum = BigInteger.Zero;
                BigInteger encryptedSum = p.Encrypt(BigInteger.Zero, pub);

                for (int i = 0; i < numVotes; i++)
                {
                    // Choose random vote
                    int vote = rnd.Next(numChoices);
                    // Encode vote
                    var encodedVote = VoteEncoder.Encode(vote, numChoices, bin, numBins, bitsPerChoice);
                    // Encrypt vote
                    var (encryptedVote, commitment) = p.EncryptWithZkp(encodedVote, validVotes, pub);

                    // Check vote validity
                    var valid = p.VerifyZkp(encryptedVote, validVotes, commitment, pub);
                    Assert.True(valid);
                    // Aggregate votes
                    realVotes[vote]++;
                    encryptedSum = p.AddEncrypted(encryptedSum, encryptedVote, pub);
                }

                BigInteger decryptedSum = p.Decrypt(encryptedSum, pub, priv);
                int[,] decodedSum = VoteEncoder.Decode(decryptedSum, numChoices, numBins, bitsPerChoice);
                for (int i = 0; i < numChoices; i++)
                    Assert.Equal(decodedSum[bin, i], realVotes[i]);
            }
        }
    }
}
