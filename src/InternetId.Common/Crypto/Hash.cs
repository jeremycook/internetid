using System;
using System.Text.Json.Serialization;

namespace InternetId.Common.Crypto
{
    public class Hash
    {
        public Hash(byte[] key, string algorithm, byte[] salt, int iterations, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            Key = key;
            Algorithm = algorithm;
            Salt = salt;
            Iterations = iterations;
            NotBefore = notBefore;
            NotAfter = notAfter;
        }

        /// Key
        [JsonPropertyName("k")]
        public byte[] Key { get; set; }

        /// <summary>
        /// Algorithm
        /// </summary>
        [JsonPropertyName("a")]
        public string Algorithm { get; set; }

        /// <summary>
        /// Salt
        /// </summary>
        [JsonPropertyName("s")]
        public byte[] Salt { get; set; }

        /// <summary>
        /// Iterations
        /// </summary>
        [JsonPropertyName("i")]
        public int Iterations { get; set; }

        /// <summary>
        /// Not valid before
        /// </summary>
        [JsonPropertyName("nb")]
        public DateTimeOffset NotBefore { get; set; }

        /// <summary>
        /// Not valid after
        /// </summary>
        [JsonPropertyName("na")]
        public DateTimeOffset NotAfter { get; set; }

        public override int GetHashCode()
        {
            return HashCode.Combine(Key, Algorithm, Salt, Iterations, NotBefore, NotAfter);
        }

        public override bool Equals(object obj)
        {
            return
                obj is Hash hash &&
                SlowEquals(Key, hash.Key) &&
                SlowEquals(Algorithm, hash.Algorithm) &&
                SlowEquals(Salt, hash.Salt) &&
                Iterations == hash.Iterations &&
                NotBefore.Equals(hash.NotBefore) &&
                NotAfter.Equals(hash.NotAfter);
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
