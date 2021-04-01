using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace InternetId.Common.Crypto
{
    public class Hasher
    {
        private const int minimumIterations = 100_000;
        private const int saltLength = 16;
        private const int keyLength = 32;

        private const string rfc2898 = "rfc2898";

        public string Hash(string plain, TimeSpan lifespan)
        {
            byte[] salt = new byte[saltLength];
            RandomNumberGenerator.Fill(salt);

            return Hash(plain, lifespan, salt, rfc2898, null);
        }

        public string Hash(string plain, TimeSpan lifespan, byte[] salt, string algorithm, int? iterations)
        {
            if (string.IsNullOrEmpty(plain)) throw new ArgumentException($"'{nameof(plain)}' cannot be null or empty.", nameof(plain));
            if (salt is null || salt.Length < 16) throw new ArgumentOutOfRangeException(nameof(salt), $"'{nameof(salt)}' cannot be null or less than 16 bits.");
            if (string.IsNullOrEmpty(algorithm)) throw new ArgumentException($"'{nameof(algorithm)}' cannot be null or empty.", nameof(algorithm));

            var sw = new Stopwatch();
            double entropy = Math.Pow(2, keyLength);
            int iters = iterations ?? minimumIterations;

            byte[] key;
            do
            {
                sw.Restart();
                switch (algorithm)
                {
                    case rfc2898:
                        Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password: plain, salt: salt, iterations: iters);
                        key = deriveBytes.GetBytes(keyLength);
                        break;
                    default:
                        throw new NotSupportedException($"The {algorithm} algorithm is not supported.");
                }

                // If iterations was explicitly provided we use that value as-is.
                if (iterations != null)
                {
                    break;
                }

                // Account for up to 1000 times the compute of this machine.
                var timeToBruteForce = (entropy * sw.Elapsed) / 1000;
                if (timeToBruteForce < lifespan)
                {
                    iters = (int)Math.Min(int.MaxValue, Math.Max(minimumIterations, iters * 1000 * (lifespan.Ticks / sw.Elapsed.Ticks)));
                }
                else
                {
                    break;
                }

            } while (true);

            return JsonSerializer.Serialize(new Hash(algorithm, iters, key, salt));
        }

        public bool Verify(string plain, TimeSpan lifespan, string hashString)
        {
            Hash hash = JsonSerializer.Deserialize<Hash>(hashString);
            string rehash = Hash(plain, lifespan, hash.GetSalt(), hash.GetAlgorithm(), hash.GetIterations());

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
