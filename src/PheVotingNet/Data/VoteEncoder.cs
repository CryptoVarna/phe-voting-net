using System.Numerics;

namespace CryptoVarna.PheVotingNet
{
    public static class VoteEncoder
    {
        public static BigInteger Encode(int choice, int numChoices, int bin, int numBins, int bitsPerChoice)
        {
            if (choice >= numChoices)
                throw new ArgumentException("Invlid choice");
            if (numBins > 0 && bin >= numBins)
                throw new ArgumentException("Invalid group");
            if (bitsPerChoice < 2)
                throw new ArgumentException("bitsPerChoice must be at least 2");
            if (2 << (bitsPerChoice - 1) >= int.MaxValue - 1)
                throw new ArgumentOutOfRangeException("bitsPerChoice", "Too big voting space, exceeds max int");
            return BigInteger.One << (bitsPerChoice * (bin * numChoices + choice));
        }

        public static BigInteger Encode(int choice, int numChoices, int bitsPerChoice)
        {
            return Encode(choice, numChoices, 0, 0, bitsPerChoice);
        }

        public static BigInteger Encode(int[] choices, int numChoices, int bin, int numBins, int bitsPerChoice)
        {
            BigInteger result = BigInteger.Zero;
            foreach (int choice in choices)
            {
                result += Encode(choice, numChoices, bin, numBins, bitsPerChoice);
            }
            return result;
        }

        public static BigInteger Encode(int[] choices, int numChoices, int bitsPerChoice)
        {
            return Encode(choices, numChoices, 0, 0, bitsPerChoice);
        }

        public static int[,] Decode(BigInteger encoded, int numChoices, int numBins, int bitsPerChoice)
        {
            if (2 << (bitsPerChoice - 1) >= int.MaxValue - 1)
                throw new ArgumentOutOfRangeException("bitsPerChoice", "Too big voting space, exceeds max int");

            int[,] result = new int[numBins, numChoices];
            for (int bin = 0; bin < numBins; bin++)
            {
                for (int choice = 0; choice < numChoices; choice++)
                {
                    result[bin, choice] = (int)(encoded >> ((choice + bin * numChoices) * bitsPerChoice) & ((1 << (bitsPerChoice - 1)) - 1));
                }
            }
            return result;
        }

        public static int[] Decode(BigInteger encoded, int numChoices, int bitsPerChoice)
        {
            if (2 << (bitsPerChoice - 1) >= int.MaxValue - 1)
                throw new ArgumentOutOfRangeException("bitsPerChoice", "Too big voting space, exceeds max int");

            int[] result = new int[numChoices];
            for (int choice = 0; choice < numChoices; choice++)
            {
                result[choice] = (int)(encoded >> (choice * bitsPerChoice) & ((1 << (bitsPerChoice - 1)) - 1));
            }
            return result;
        }

        public static BigInteger GetTotalVotesBits(int numChoices, int numBins, int bitsPerChoice)
        {
            if (numChoices <= 0 || bitsPerChoice <= 0)
                return 0;
            if (numBins <= 0)
                numBins = 1;
            return numChoices * numBins * bitsPerChoice;
        }

        public static IList<BigInteger> GetSingleChoicePermutations(int numChoices, int bitsPerChoice)
        {
            return GetSingleChoicePermutations(numChoices, 1, bitsPerChoice);
        }

        public static IList<BigInteger> GetSingleChoicePermutations(int numChoices, int numBins, int bitsPerChoice)
        {
            if (numChoices < 2 || numBins < 1)
                throw new ArgumentOutOfRangeException();

            int permutations = (int)Math.Pow(2, numChoices);
            var list = new List<BigInteger>(permutations * numBins);
            for (int bin = 0; bin < numBins; bin++)
                for (int choice = 0; choice < numChoices; choice++)
                    list.Add(VoteEncoder.Encode(choice, numChoices, bin, numBins, bitsPerChoice));
            return list;
        }

        /*public static IList<BigInteger> GetMultiChoicePermutations(int numChoices, int numBins, int bitsPerChoice)
        {
            var list = new List<BigInteger>(2^numChoices * numBins);
            for (int bin = 0; bin < numBins; bin++)
                for (int choice = 0; choice < numChoices; choice++)
                    list.Add(VoteEncoder.Encode(choice, numChoices, bin, numBins, bitsPerChoice));
            return list;
        }*/
    }
}
