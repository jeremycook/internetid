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
        private const int saltBytes = 16;
        private const int keyBytes = 32;

        private const string rfc2898 = "rfc2898";

        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static readonly Dictionary<int, int> entropyToIterations = new Dictionary<int, int>();

        public string Hash(string password, int passwordEntropy, TimeSpan lifespan)
        {
            byte[] salt = new byte[saltBytes];
            RandomNumberGenerator.Fill(salt);

            return Hash(password, passwordEntropy, salt, rfc2898, DateTimeOffset.UtcNow.AddSeconds(-1), DateTimeOffset.UtcNow.Add(lifespan));
        }

        /// <summary>
        /// Calculate the key.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="salt"></param>
        /// <param name="algorithm"></param>
        /// <param name="iterations"></param>
        /// <returns></returns>
        public byte[] CalculateKey(string password, byte[] salt, string algorithm, int iterations, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or empty.", nameof(password));
            if (salt is null || salt.Length < 16) throw new ArgumentOutOfRangeException(nameof(salt), $"'{nameof(salt)}' cannot be null or less than 16 bytes in length.");
            if (string.IsNullOrWhiteSpace(algorithm)) throw new ArgumentException($"'{nameof(algorithm)}' cannot be null or empty.", nameof(algorithm));
            if (iterations < minimumIterations) throw new InvalidOperationException($"The number of iterations {iterations} is less than the minimum of {minimumIterations}.");

            byte[] key;

            switch (algorithm)
            {
                case rfc2898:
                    Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(password: password + notBefore.ToUnixTimeMilliseconds() + notAfter.ToUnixTimeMilliseconds(), salt: salt, iterations: iterations);
                    key = deriveBytes.GetBytes(keyBytes);
                    break;
                default:
                    throw new NotSupportedException($"The {algorithm} algorithm is not supported.");
            }

            return key;
        }

        /// <summary>
        /// Hash the <paramref name="password"/>, adjusting algorithm so that the key cannot easily be brute forced before it expires.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="passwordEntropy"></param>
        /// <param name="salt"></param>
        /// <param name="algorithm"></param>
        /// <param name="iterations"></param>
        /// <param name="notBefore"></param>
        /// <param name="notAfter"></param>
        /// <returns></returns>
        public string Hash(string password, int passwordEntropy, byte[] salt, string algorithm, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            var sw = new Stopwatch();

            // Step password entropy so that the entropy-to-iterations cache clusters.
            passwordEntropy = 10 * (int)Math.Floor((double)passwordEntropy / 10);

            if (!entropyToIterations.TryGetValue(passwordEntropy, out int iterations))
            {
                iterations = minimumIterations;
            }

            // The total time an adversary would have to brute force a hash
            TimeSpan lifespan = notAfter - DateTimeOffset.UtcNow;

            byte[] key;
            do
            {
                sw.Restart();
                key = CalculateKey(password, salt, algorithm, iterations, notBefore, notAfter);
                sw.Stop();

                // We've reached the maximum number of iterations.
                if (iterations >= int.MaxValue)
                {
                    break;
                }

                // Estimated days to brute force the hash with the current hardware.
                double daysToBruteForce = passwordEntropy * sw.Elapsed.TotalDays;

                // Allow for up to 1000 times the compute of the current hardware.
                double ratioOfLifespanVsBruteForce = 1000 * lifespan.TotalDays / daysToBruteForce;

                if (ratioOfLifespanVsBruteForce > 1)
                {
                    // Increase iterations to compensate and try again.
                    iterations = (int)Math.Min(int.MaxValue, Math.Max(minimumIterations, iterations * ratioOfLifespanVsBruteForce));

                    if (!entropyToIterations.TryGetValue(passwordEntropy, out int currentIterations) || currentIterations < iterations)
                    {
                        // Cache to save time recalculating in the future.
                        entropyToIterations[passwordEntropy] = iterations;
                    }
                }
                else
                {
                    break;
                }

            } while (true);

            return JsonSerializer.Serialize(new Hash(key, algorithm, salt, iterations, notBefore, notAfter), jsonSerializerOptions);
        }

        public HasherVerificationResult Verify(string password, string hashString)
        {
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or empty.", nameof(password));
            if (string.IsNullOrWhiteSpace(hashString)) throw new ArgumentException($"'{nameof(hashString)}' cannot be null or empty.", nameof(hashString));

            Hash hash = JsonSerializer.Deserialize<Hash>(hashString, jsonSerializerOptions);

            byte[] salt = hash.GetSalt();
            string algorithm = hash.GetAlgorithm();
            int iterations = hash.GetIterations();
            DateTimeOffset notBefore = hash.GetNotBefore();
            DateTimeOffset notAfter = hash.GetNotAfter();

            byte[] key = CalculateKey(password, salt, algorithm, iterations, notBefore, notAfter);

            Hash rehash = new Hash(key, algorithm, salt, iterations, notBefore, notAfter);

            if (hash.Equals(rehash))
            {
                DateTimeOffset utcNow = DateTimeOffset.UtcNow;
                if (utcNow < notBefore)
                {
                    return HasherVerificationResult.Inactive;
                }
                else if (notAfter < utcNow)
                {
                    return HasherVerificationResult.Expired;
                }
                else
                {
                    return HasherVerificationResult.Valid;
                }
            }
            else
            {
                return HasherVerificationResult.Invalid;
            }
        }
    }
}
