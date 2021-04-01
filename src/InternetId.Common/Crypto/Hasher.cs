using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace InternetId.Common.Crypto
{
    public class Hasher
    {
        private static readonly Dictionary<int, int> rates = new Dictionary<int, int>();

        private readonly int iterations = 100_000;
        private readonly int saltLength = 16;
        private readonly int keyLength = 32;

        private const string rfc2898derivebytes = "rfc2898derivebytes";

        public string Hash(string plain, TimeSpan lifespan)
        {
            byte[] salt = new byte[saltLength];
            RandomNumberGenerator.Fill(salt);

            return Hash(plain, lifespan, salt, rfc2898derivebytes);
        }

        public string Hash(string plain, TimeSpan lifespan, byte[] salt, string algorithm = rfc2898derivebytes)
        {
            if (string.IsNullOrEmpty(plain)) throw new ArgumentException($"'{nameof(plain)}' cannot be null or empty.", nameof(plain));
            if (salt is null || salt.Length < 16) throw new ArgumentOutOfRangeException(nameof(salt), $"'{nameof(salt)}' cannot be null or less than 16 bits.");
            if (string.IsNullOrEmpty(algorithm)) throw new ArgumentException($"'{nameof(algorithm)}' cannot be null or empty.", nameof(algorithm));

            var sw = new Stopwatch();
            double entropy = Math.Pow(2, keyLength);
            int its = iterations;

            byte[] key;
            do
            {
                sw.Restart();
                switch (algorithm)
                {
                    case rfc2898derivebytes:

                        Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password: plain, salt: salt, iterations: its);

                        key = deriveBytes.GetBytes(keyLength);

                        break;
                    default:
                        throw new NotSupportedException($"The {algorithm} algorithm is not supported.");
                }

                var timeToBruteForce = (entropy * sw.Elapsed) / 1000;
                if (timeToBruteForce < lifespan)
                {
                    its = (int)Math.Min(int.MaxValue, Math.Max(iterations, its * 1000 * (lifespan.Ticks / sw.Elapsed.Ticks)));
                }
                else
                {
                    break;
                }

            } while (true);

            return JsonSerializer.Serialize(new Hash(algorithm, key, salt));
        }

        public bool Verify(string plain, TimeSpan lifespan, string hashString)
        {
            Hash hash = JsonSerializer.Deserialize<Hash>(hashString);
            string rehash = Hash(plain, lifespan, hash.GetSalt(), hash.GetAlgorithm());

            return SlowEquals(hashString, rehash);
        }

        /// <summary>
        /// Compare two arrays for equality using a constant time comparison method.
        /// </summary>
        /// <param name="red"></param>
        /// <param name="blue"></param>
        /// <returns></returns>
        public static bool SlowEquals(string red, string blue)
        {
            uint diff = (uint)red.Length ^ (uint)blue.Length;
            for (int i = 0; i < red.Length && i < blue.Length; i++)
                diff |= (uint)(red[i] ^ blue[i]);
            return diff == 0;
        }
    }
}
