using System.Text.Json.Serialization;

namespace InternetId.Common.Crypto
{
    public class Hash
    {
        public Hash() { }
        public Hash(string algorithm, byte[] key, byte[] salt)
        {
            A = algorithm;
            K = key;
            S = salt;
        }

        /// <summary>
        /// Algorithm
        /// </summary>
        public string A { get; set; }

        /// Key
        public byte[] K { get; set; }

        /// <summary>
        /// Salt
        /// </summary>
        public byte[] S { get; set; }

        public string GetAlgorithm() => A;
        public byte[] GetKey() => K;
        public byte[] GetSalt() => S;
    }
}
