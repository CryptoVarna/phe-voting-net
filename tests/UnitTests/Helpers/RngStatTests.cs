using System;
using System.Collections.Generic;
using System.Text;

namespace CryptoVarna.PheVotingNet.Tests.Helpers
{
    /// <summary>
    /// https://nvlpubs.nist.gov/nistpubs/Legacy/SP/nistspecialpublication800-22r1a.pdf
    /// TODO: Implement more tests
    /// </summary>
    class RngStatTests
    {
        public const double ONE_PERCENT = 0.01;

        public static bool FrequencyMonobitTest(byte[] input)
        {
            int n = input.Length * 8;
            if (n < 100)
                throw new ArgumentOutOfRangeException(nameof(input), "bit length must be at least 100");

            int sum = MonobitSum(input, 1, -1);
            double sObs = Math.Abs(sum) / Math.Sqrt(n);
            double pValue = SpecialFunctions.erfc(sObs / Math.Sqrt(2));

            // If the computed P-value is < 0.01, then conclude that the sequence is non-random. Otherwise, conclude that the sequence is random.
            return pValue >= ONE_PERCENT;
        }

        // TODO: Fix this
        public static bool FrequencyBlockTest(byte[] input, int blockSize)
        {
            int n = input.Length * 8;
            if (n < 100)
                throw new ArgumentOutOfRangeException(nameof(input), "bit length must be at least 100");
            if (blockSize % 8 != 0)
                throw new ArgumentOutOfRangeException(nameof(blockSize), "blockSize must be multiple of 8");

            // π = ∑(onesIn(input) / blockSize)
            // χ^2(obs) = 4*blockSize*∑(π - 1/2)^2
            double chiObs = 0;
            int blockSizeBytes = blockSize / 8;
            double blocksNum = n / blockSize;
            byte[] block = new byte[blockSize];
            for (int i = 0; i < blocksNum; i++)
            {
                Array.Copy(input, i * blockSizeBytes, block, 0, blockSizeBytes);
                double pi = (double)MonobitSum(block, 1, 0) / blockSize;
                chiObs += Math.Pow((pi - 1 / 2), 2);
            }
            chiObs *= 4 * blockSize;

            double pValue = SpecialFunctions.igamc(blocksNum / 2, chiObs / 2);

            // If the computed P-value is < 0.01, then conclude that the sequence is non-random. Otherwise, conclude that the sequence is random.
            return pValue >= ONE_PERCENT;
        }

        public static bool RunTest(byte[] input)
        {
            // Compute the pre-test proportion π of ones in the input sequence:  
            // π = ∑input / n
            int n = input.Length * 8;
            int setBits = MonobitSum(input, 1, 0);
            double pi = (double)setBits / n;

            // Monobit Test
            // Determine if the prerequisite Frequency test is passed: If it can be shown that |π - 1/2| ≥ τ , then the 
            // Runs test need not be performed(i.e., the test should not have been run because of a failure to
            // pass test 1, the Frequency(Monobit) test)
            double tau = 2 / Math.Sqrt(n);
            bool test1 = (pi - 1 / 2) >= tau;
            if (!test1)
                return false;

            // Runs Test
            int vObs = setBits + 1;
            double nom = Math.Abs(vObs - 2 * n * pi * (1 - pi));
            double den = 2 * Math.Sqrt(2 * n) * pi * (1 - pi);
            double pValue = SpecialFunctions.erfc(nom / den);

            // If the computed P-value is < 0.01, then conclude that the sequence is non-random. Otherwise, conclude that the sequence is random.
            return pValue >= ONE_PERCENT;
        }

        private static int MonobitSum(byte[] input, int deltaSet = 1, int deltaZero = 0)
        {
            int result = 0;
            foreach (byte b in input)
                for (int bit = 0; bit < 8; bit++)
                    result += (b >> bit & 0x01) > 0 ? deltaSet : deltaZero;

            return result;
        }
    }
}
