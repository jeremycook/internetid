using System;

namespace InternetId.Common.Crypto
{
    public class Hash
    {
        public Hash() { }
        public Hash(byte[] key, string algorithm, byte[] salt, int iterations, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            K = key;
            A = algorithm;
            S = salt;
            I = iterations;
            NB = notBefore;
            NA = notAfter;
        }

        /// Key
        public byte[] K { get; set; }

        /// <summary>
        /// Algorithm
        /// </summary>
        public string A { get; set; }

        /// <summary>
        /// Salt
        /// </summary>
        public byte[] S { get; set; }

        /// <summary>
        /// Iterations
        /// </summary>
        public int I { get; set; }

        /// <summary>
        /// Not valid before
        /// </summary>
        public DateTimeOffset NB { get; set; }

        /// <summary>
        /// Not valid after
        /// </summary>
        public DateTimeOffset NA { get; set; }

        public byte[] GetKey() => K;
        public string GetAlgorithm() => A;
        public byte[] GetSalt() => S;
        public int GetIterations() => I;
        public DateTimeOffset GetNotBefore() => NB;
        public DateTimeOffset GetNotAfter() => NA;

        public override int GetHashCode()
        {
            return HashCode.Combine(K, A, S, I, NB, NA);
        }

        public override bool Equals(object obj)
        {
            return
                obj is Hash hash &&
                SlowEquals(K, hash.K) &&
                SlowEquals(A, hash.A) &&
                SlowEquals(S, hash.S) &&
                I == hash.I &&
                NB.Equals(hash.NB) &&
                NA.Equals(hash.NA);
        }

        /// <summary>
        /// Compare two arrays for equality using a constant time comparison method.
        /// </summary>
        /// <param name="red"></param>
        /// <param name="blue"></param>
        /// <returns></returns>
        private static bool SlowEquals(string red, string blue)
        {
            uint diff = (uint)red.Length ^ (uint)blue.Length;
            for (int i = 0; i < red.Length && i < blue.Length; i++)
                diff |= (uint)(red[i] ^ blue[i]);
            return diff == 0;
        }

        /// <summary>
        /// Compare two arrays for equality using a constant time comparison method.
        /// </summary>
        /// <param name="red"></param>
        /// <param name="blue"></param>
        /// <returns></returns>
        private static bool SlowEquals(byte[] red, byte[] blue)
        {
            uint diff = (uint)red.Length ^ (uint)blue.Length;
            for (int i = 0; i < red.Length && i < blue.Length; i++)
                diff |= (uint)(red[i] ^ blue[i]);
            return diff == 0;
        }
    }
}
