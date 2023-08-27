using System.Numerics;
using System.Security.Cryptography;

namespace CryptoVarna.PheVotingNet
{
    /// <summary>
    /// https://en.wikipedia.org/wiki/Paillier_cryptosystem
    /// https://www.cs.tau.ac.il/~fiat/crypt07/papers/Pai99pai.pdf
    /// https://paillier.daylightingsociety.org/Paillier_Zero_Knowledge_Proof.pdf
    /// </summary>
    public partial class Paillier : IDisposable
    {
        private RandomNumberGenerator _rng = default!;
        private RandomNumberGenerator rng
        {
            get
            {
                if (_rng == null)
                    _rng = RandomNumberGenerator.Create();
                return _rng;
            }
        }

        public (PublicKey, PrivateKey) GenerateKeyPair(int bits)
        {
            if (bits < 160)
                throw new ArgumentOutOfRangeException(nameof(bits), "Key must be at least 160 bits");

            // It is very unlikely the rng to return same number twice however we add this check
            BigInteger p, q, n;
            using (var rng = RandomNumberGenerator.Create())
            {
                // Choose two large primes p and q randomly and independently of each other
                // such that gcd(p * q, (p - 1)(q - 1)) = 1
                // This property is assured if both primes are of equivalent length
                do
                {
                    do
                    {
                        p = BigMath.GenerateRandomPrime(bits / 2, rng);
                        q = BigMath.GenerateRandomPrime(bits / 2, rng);
                    } while (p == q);

                    // Compute RSA modulus n = pq
                    n = p * q;
                } while (n.BitLength() != bits);
            }

            // Carmichael’s function lambda = lcm(𝑝 − 1, 𝑞 − 1)
            BigInteger lambda = ((p - 1) * (q - 1)) / BigInteger.GreatestCommonDivisor((p - 1), (q - 1));

            // Select generator g where g ∈ Z∗n^2
            // TODO: Try alternatively g = (an + 1)*b^n mod n^2 where a, b are randoms in Z*n
            BigInteger g = n + 1; // Shortcut

            // mu = (L(g^lambda mod n^2))^-1 mod n
            // L(u) = (u - 1) / n
            // u = g^lambda mod n^2
            BigInteger u = BigInteger.ModPow(g, lambda, n * n);
            BigInteger u2 = (u - 1) / n;
            BigInteger mu = BigMath.ModInverse(u2, n);

            return (new PublicKey(n, g), new PrivateKey(lambda, mu));
        }

        public (BigInteger, ZkpCommitment) EncryptWithZkp(BigInteger m, IList<BigInteger> valid, PublicKey pub)
        {
            var (c, r) = EncryptWithR(m, pub);
            var commitment = CreateZkp(m, c, r, valid, pub);
            return (c, commitment);
        }

        public BigInteger Encrypt(BigInteger m, PublicKey pub)
        {
            var (c, r) = EncryptWithR(m, pub);
            return c;
        }

        public (BigInteger, BigInteger) EncryptWithR(BigInteger m, PublicKey pub)
        {
            // Find a random r where 𝑟 ∈ 𝑍𝑛*2
            BigInteger r;
            do
            {
                r = BigMath.GenerateCoprime(pub.N, pub.N.BitLength() - 1, rng);
            } while (r >= pub.N); // This should always be false and is just a precaution
            return EncryptWithR(m, pub, r);
        }

        /// <summary>
        /// Steps for Encryption
        /// </summary>
        /// <param name="pub"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public (BigInteger, BigInteger) EncryptWithR(BigInteger m, PublicKey pub, BigInteger r)
        {
            // Plaintext is m where m < n
            if (m >= pub.N)
                throw new ArgumentOutOfRangeException(nameof(m), "plaintext must be less than modulo n");

            // Let ciphertext c = g^m * r^n mod n^2
            var gm = (pub.N * m + 1) % pub.NSquared;
            var rn = BigInteger.ModPow(r, pub.N, pub.NSquared);
            var c = (gm * rn) % pub.NSquared;
            return (c, r);
        }

        /// <summary>
        /// Steps for Decryption
        /// 1. The ciphertext c < n^2
        /// 2. Retrieve message m = L(c^lambda mod n^2) * mu mod n
        /// </summary>
        /// <param name="priv"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        public BigInteger Decrypt(BigInteger c, PublicKey pub, PrivateKey priv)
        {
            // The ciphertext c < n ^ 2
            if (c >= pub.NSquared)
                throw new ArgumentOutOfRangeException(nameof(c), "ciphertext must be less than modulo n^2");

            // m = L(c^lambda mod n^2) * mu mod n
            // L(u) = (u - 1) / n
            var u = BigInteger.ModPow(c, priv.Lambda, pub.NSquared);
            var m = (((u - 1) / pub.N) * priv.Mu) % pub.N;
            return m;
        }

        public Signature CreateSignature(BigInteger m, PublicKey pub, PrivateKey priv)
        {
            // Calculate h(m) = hash of message m
            var h = Hashing.Sha256BigInt(m);

            // s1 = (L(h(m)^lambda mod n^2) / L(g^lambda mod n^2)) mod n
            var s1Num = (BigInteger.ModPow(h, priv.Lambda, pub.NSquared) - 1) / pub.N;
            var s1Den = priv.Mu;
            var s1 = (s1Num * s1Den) % pub.N;

            // s2 = ((h(m)g^-s1)^(1/n mod lambda)) mod n
            var invN = BigMath.ModInverse(pub.N, priv.Lambda);
            var test = BigInteger.ModPow(pub.G, s1, pub.N);
            var invG = BigMath.ModInverse(test, pub.N);
            var s2 = BigInteger.ModPow(h * invG, invN, pub.N);

            return new Signature(s1, s2);
        }

        public bool VerifySignature(BigInteger m, Signature sig, PublicKey pub)
        {
            // h(m) ?= g^s1 * s2^n | mod n^2
            var h = Hashing.Sha256BigInt(m);
            var gs1 = BigInteger.ModPow(pub.G, sig.S1, pub.NSquared);
            var s2n = BigInteger.ModPow(sig.S2, pub.N, pub.NSquared);
            var hm = (gs1 * s2n) % pub.NSquared;
            return hm == h;
        }

        /// <summary>
        /// </summary>
        /// <param name="c"></param>
        /// <param name="pub"></param>
        /// <param name="valid"></param>
        /// <param name="r"></param>
        /// <returns></returns>
        public ZkpCommitment CreateZkp(BigInteger m, BigInteger c, BigInteger r, IList<BigInteger> valid, PublicKey pub)
        {
            var commitment = new ZkpCommitment(valid.Count);

            // Choose random ω ∈ Z∗n
            BigInteger omega;
            do
            {
                omega = BigMath.GenerateCoprime(pub.N, pub.N.BitLength() - 1, rng);
            } while (omega >= pub.N); // This should always be false and is just a precaution
            var mk = -1;

            // For reach valid message m[i]
            for (int i = 0; i < valid.Count; i++)
            {
                // u[i] = c / g^m[i] mod n^2
                var mi = valid[i];
                var gmi = BigInteger.ModPow(pub.G, mi, pub.NSquared);
                var ui = (c * BigMath.ModInverse(gmi, pub.NSquared)) % pub.NSquared;

                if (mi != m)
                {
                    // e1,e2,e3, ...,ek ∈ 2^b < min(p, q)
                    var ei = BigMath.GenerateRandom(pub.N.BitLength() / 2 - 1, rng); // bit length of p and q = pubkey length / 2
                    commitment.E.Add(ei);

                    // z1, z2, z3, ..., zk ∈ Z∗n
                    var zi = BigMath.GenerateCoprime(pub.N, pub.N.BitLength() - 1, rng);
                    commitment.Z.Add(zi);

                    // a1, a2, a3, ..., ak where a[i] = z[i]^n / u[i]^e[i] mod n^2
                    var zin = BigInteger.ModPow(zi, pub.N, pub.NSquared);
                    var uiei = BigInteger.ModPow(ui, ei, pub.NSquared);
                    var ai = (zin * BigMath.ModInverse(uiei, pub.NSquared)) % pub.NSquared;
                    commitment.A.Add(ai);
                }
                else
                {
                    // For m[i] = m, we calculate a[i] as follows
                    // a[i] = ω^n mod n^2
                    var ai = BigInteger.ModPow(omega, pub.N, pub.NSquared);
                    commitment.A.Add(ai);

                    mk = i;
                }
            }

            if (mk < 0)
                throw new ArgumentException("Message m isn't included in the list of valid messages");

            // Non-interactive version
            var challenge = Hashing.Sha256BigInt(commitment.A.ToArray());
            // modulo equal to the length of the hash - 256 bits
            var hashMod = BigInteger.Pow(2, 256);

            // The prover now calculates z[k] and e[k] for m[k] = m as follows
            var esum = commitment.E.Aggregate(BigInteger.Add) % hashMod;
            // e[k] = e_challange - sum(e[i])
            var ek = BigMath.PositiveMod(challenge - esum, hashMod);

            commitment.E.Insert(mk, ek);
            // z[k] = ω ∗ r^e[k] mod n
            var zk = (omega * BigInteger.ModPow(r, ek, pub.N)) % pub.N;
            commitment.Z.Insert(mk, zk);

            return commitment;
        }

        public bool VerifyZkp(BigInteger c, IList<BigInteger> valid, ZkpCommitment commitment, PublicKey pub)
        {
            if (valid.Count * 3 != commitment.A.Count + commitment.E.Count + commitment.Z.Count)
                throw new ArgumentException("Invalid commitment or valid messages");

            // sum(e[k]) = challenger mod 2^2b
            var challenger = Hashing.Sha256BigInt(commitment.A.ToArray());
            var hashMod = BigInteger.Pow(2, 256);
            var esum = commitment.E.Aggregate(BigInteger.Add) % hashMod;
            // If this fails, then the prover did not follow the rules or attempted to cheat
            if (esum != challenger)
                return false;

            // For reach valid message m[i]
            for (int i = 0; i < valid.Count; i++)
            {
                // u[i] = c / g^m[i] mod n^2
                var mi = valid[i];
                var gmi = BigInteger.ModPow(pub.G, mi, pub.NSquared);
                var ui = (c * BigMath.ModInverse(gmi, pub.NSquared)) % pub.NSquared;

                // z[i]^n = a[i] * u[i]^e[i] nod n^2
                var zi = commitment.Z[i];
                var ai = commitment.A[i];
                var ei = commitment.E[i];
                var zin = BigInteger.ModPow(zi, pub.N, pub.NSquared);
                var uiei = BigInteger.ModPow(ui, ei, pub.NSquared);
                var aiuiei = (ai * uiei) % pub.NSquared;
                // If this fails, then the prover did not follow the rules or attempted to cheat
                if (zin != aiuiei)
                    return false;

            }

            return true;
        }


        public BigInteger AddEncrypted(BigInteger em1, BigInteger em2, PublicKey pub)
        {
            // d(e(m1) * e(m2) mod n^2) = m1 + m2 mod n
            return (em1 * em2) % pub.NSquared;
        }

        public BigInteger AddScalar(BigInteger em, BigInteger k, PublicKey pub)
        {
            // d(e(m) * g^k mod n^2) = m + k mod n
            return (em * BigInteger.ModPow(pub.G, k, pub.NSquared)) % pub.NSquared;
        }

        public BigInteger MulScalar(BigInteger em, BigInteger k, PublicKey pub)
        {
            // d(e(m)^k mod n^2) = k * m mod n
            return BigInteger.ModPow(em, k, pub.NSquared);
        }

        public void Dispose()
        {
            if (_rng != null)
                _rng.Dispose();
        }
    }
}
