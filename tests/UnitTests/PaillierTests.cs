using System;
using Xunit;
using CryptoVarna.PheVotingNet;
using System.Numerics;
using System.Collections.Generic;

namespace CryptoVarna.PheVotingNet.Tests
{
    public class PaillierTests
    {
        [Fact]
        public void KeyGeneration()
        {
            using (var p = new Paillier())
            {
                for (int bits = 256; bits < 2048; bits *= 2)
                {
                    var (pub, priv) = p.GenerateKeyPair(bits);
                    Assert.Equal(pub.N.BitLength(), bits);
                    Assert.True(priv.Lambda.BitLength() > 0);
                    Assert.True(priv.Mu.BitLength() > 0);
                }
            }
        }

        [Theory]
        [InlineData(256, "0")]
        [InlineData(160, "1")]
        [InlineData(160, "8572057275")]
        [InlineData(256, "95477148500050043847142")]
        [InlineData(512, "93875198749187950505012983050847247412455461")]
        public void EncryptionDecryption(int keySize, string input)
        {
            using (var p = new Paillier())
            {
                var (pub, priv) = p.GenerateKeyPair(keySize);
                var m = BigInteger.Parse(input);
                var c = p.Encrypt(m, pub);
                var d = p.Decrypt(c, pub, priv);
                Assert.Equal(m, d);
            }
        }

        [Theory]
        [InlineData(256, "0")]
        [InlineData(160, "1")]
        [InlineData(160, "8572057275")]
        [InlineData(256, "95477148500050043847142")]
        [InlineData(512, "93875198749187950505012983050847247412455461")]
        public void SignVerify(int keySize, string input)
        {
            using (var p = new Paillier())
            {
                var (pub, priv) = p.GenerateKeyPair(keySize);
                var m = BigInteger.Parse(input);
                var sig = p.CreateSignature(m, pub, priv);
                var v = p.VerifySignature(m, sig, pub);
                Assert.True(v);
            }
        }

        public static IEnumerable<object[]> GetZkpCorrectInput()
        {
            yield return new object[]
            {
                256,
                BigInteger.Parse("0"),
                new List<BigInteger> { 0, 1, 2, 3 }
            };

            yield return new object[]
            {
                256,
                BigInteger.Parse("1"),
                new List<BigInteger> { 1, 2, 3 }
            };

            yield return new object[]
            {
                256,
                BigInteger.Pow(2, 255),
                new List<BigInteger> { BigInteger.Pow(2, 16), BigInteger.Pow(2, 64), BigInteger.Pow(2, 255) }
            };

            yield return new object[]
            {
                2048,
                BigInteger.Pow(2, 1024),
                new List<BigInteger> { BigInteger.Pow(2, 256), BigInteger.Pow(2, 512), BigInteger.Pow(2, 1024) }
            };
        }

        [Theory]
        [MemberData(nameof(GetZkpCorrectInput))]
        public void ZkpCorrect(int keySize, BigInteger input, IList<BigInteger> valid)
        {
            using (var p = new Paillier())
            {
                var (pub, priv) = p.GenerateKeyPair(keySize);
                var (c, commitment) = p.EncryptWithZkp(input, valid, pub);
                var result = p.VerifyZkp(c, valid, commitment, pub);
                Assert.True(result);
            }
        }

        public static IEnumerable<object[]> GetZkpIncorrectInput()
        {
            yield return new object[]
            {
                256,
                BigInteger.Parse("1"),
                BigInteger.Parse("1"),
                new List<BigInteger> { 1, 2, 3 }
            };

            yield return new object[]
            {
                256,
                BigInteger.Parse("1"),
                BigInteger.Parse("4"),
                new List<BigInteger> { 1, 2, 3 }
            };

            yield return new object[]
            {
                256,
                BigInteger.Pow(2, 128),
                BigInteger.Pow(2, 129),
                new List<BigInteger> { BigInteger.Pow(2, 16), BigInteger.Pow(2, 128), BigInteger.Pow(2, 255) }
            };
        }

        [Theory]
        [MemberData(nameof(GetZkpIncorrectInput))]
        public void ZkpIncorrect(int keySize, BigInteger input, BigInteger cheatInput, IList<BigInteger> valid)
        {
            using (var p = new Paillier())
            {
                var (pub, priv) = p.GenerateKeyPair(keySize);
                var (c, commitment) = p.EncryptWithZkp(input, valid, pub);
                var cheatC = p.Encrypt(cheatInput, pub);
                var result = p.VerifyZkp(cheatC, valid, commitment, pub);
                Assert.False(result);
            }
        }

        [Fact]
        public void ZkpInvalid()
        {
            using (var p = new Paillier())
            {
                var keySize = 256;
                var input = BigInteger.Parse("4");
                var valid = new List<BigInteger> { 1, 2, 3 };
                var (pub, priv) = p.GenerateKeyPair(keySize);
                Assert.Throws<ArgumentException>(() => p.EncryptWithZkp(input, valid, pub));
            }
        }

        [Fact]
        public void AddEncrypted()
        {
            using (var p = new Paillier())
            {
                var keySize = 256;
                var (pub, priv) = p.GenerateKeyPair(keySize);

                BigInteger sum = BigInteger.Zero;
                BigInteger encryptedSum = p.Encrypt(BigInteger.Zero, pub);

                for (int i = 0; i < 100; i++)
                {
                    BigInteger n = BigInteger.Pow(2, i);
                    sum += n;
                    BigInteger c = p.Encrypt(n, pub);
                    encryptedSum = p.AddEncrypted(encryptedSum, c, pub);
                }

                BigInteger decryptedSum = p.Decrypt(encryptedSum, pub, priv);
                Assert.Equal(decryptedSum, sum);
            }
        }

        [Fact]
        public void AddScalar()
        {
            using (var p = new Paillier())
            {
                var keySize = 256;
                var (pub, priv) = p.GenerateKeyPair(keySize);

                BigInteger sum = BigInteger.Zero;
                BigInteger encryptedSum = p.Encrypt(BigInteger.Zero, pub);

                for (int i = 0; i < 100; i++)
                {
                    BigInteger n = BigInteger.Pow(2, i);
                    sum += n;
                    encryptedSum = p.AddScalar(encryptedSum, n, pub);
                }

                BigInteger decryptedSum = p.Decrypt(encryptedSum, pub, priv);
                Assert.Equal(decryptedSum, sum);
            }
        }

        [Fact]
        public void MulScalar()
        {
            using (var p = new Paillier())
            {
                var keySize = 256;
                var (pub, priv) = p.GenerateKeyPair(keySize);

                BigInteger prod = BigInteger.One;
                BigInteger encryptedProd = p.Encrypt(BigInteger.One, pub);

                for (int i = 0; i < 100; i++)
                {
                    BigInteger n = i * 2;
                    prod *= n;
                    encryptedProd = p.MulScalar(encryptedProd, n, pub);
                }

                BigInteger decryptedSum = p.Decrypt(encryptedProd, pub, priv);
                Assert.Equal(decryptedSum, prod);
            }
        }
    }
}
