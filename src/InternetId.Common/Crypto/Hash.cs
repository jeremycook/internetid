using System;

namespace InternetId.Common.Crypto
{
    public class Hash
    {
        public Hash() { }
        public Hash(string algorithm, int iterations, byte[] key, byte[] salt)
        {
            A = algorithm;
            I = iterations;
            K = key;
            S = salt;
        }

        /// <summary>
        /// Algorithm
        /// </summary>
        public string A { get; set; }

        /// <summary>
        /// Iterations
        /// </summary>
        public int I { get; set; }

        /// Key
        public byte[] K { get; set; }

        /// <summary>
        /// Salt
        /// </summary>
        public byte[] S { get; set; }

        public string GetAlgorithm() => A;
        public int GetIterations() => I;
        public byte[] GetKey() => K;
        public byte[] GetSalt() => S;
    }
}
