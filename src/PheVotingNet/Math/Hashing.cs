using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace CryptoVarna.PheVotingNet
{
    public static class Hashing
    {
        public static string Sha256(byte[] data)
        {
            byte[] hash = Sha256Bytes(data);
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                builder.Append(hash[i].ToString("x2"));
            }
            return builder.ToString();
        }

        public static string Sha256(string data)
        {
            byte[] bData = Encoding.UTF8.GetBytes(data);
            return Sha256(bData);
        }

        public static BigInteger Sha256BigInt(byte[] data)
        {
            byte[] hash = Sha256Bytes(data);
            return BigMath.CreatePositiveNumber(hash, 256);
        }

        public static BigInteger Sha256BigInt(BigInteger[] list)
        {
            MemoryStream ms = new MemoryStream();
            for (int i = 0; i < list.Length; i++)
            {
                byte[] b = list[i].ToByteArray();
                ms.Write(b, 0, b.Length);
            }
            return Hashing.Sha256BigInt(ms.ToArray());
        }

        public static BigInteger Sha256BigInt(BigInteger n)
        {
            return Sha256BigInt(n.ToByteArray());
        }

        public static byte[] Sha256Bytes(string data)
        {
            byte[] bData = Encoding.UTF8.GetBytes(data);
            return Sha256Bytes(bData);
        }

        public static byte[] Sha256Bytes(byte[] data)
        {
            if (data == null || data.Length == 0)
                throw new ArgumentNullException("Invalid data");

            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] hash = sha256Hash.ComputeHash(data);
                return hash;
            }
        }
    }
}