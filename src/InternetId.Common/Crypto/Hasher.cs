using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;

namespace InternetId.Common.Crypto
{
    public class Hasher
    {
        // 100K takes about 0.02 seconds
        private const int minimumIterations = 100_000;
        // 10 million takes about 1.5 seconds
        private const int maximumIterations = 10_000_000;
        private const int saltBytes = 16;
        private const int keyBytes = 32;

        private const string rfc2898 = "rfc2898";

        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private static readonly Dictionary<int, int> entropyToIterations = new Dictionary<int, int>();

        private readonly DateTimeOffset utcNow;

        public Hasher()
        {
            utcNow = DateTimeOffset.UtcNow;
        }

        public Hasher(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }


        public string Hash(string password, double combinations, TimeSpan lifespan)
        {
            byte[] salt = new byte[saltBytes];
            RandomNumberGenerator.Fill(salt);

            return Hash(password, combinations, salt, rfc2898, utcNow.AddSeconds(-1), utcNow.Add(lifespan));
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
            if (salt is null || salt.Length < 16 || salt.All(o => o == 0)) throw new ArgumentOutOfRangeException(nameof(salt), $"'{nameof(salt)}' cannot be null, less than 16 bytes in length or only contain zeros.");
            if (algorithm is null) throw new ArgumentNullException(nameof(algorithm));
            if (iterations < minimumIterations) throw new ArgumentOutOfRangeException(nameof(iterations), $"The number of iterations {iterations} is less than the minimum of {minimumIterations}.");

            byte[] key;

            switch (algorithm)
            {
                case rfc2898:
                    Rfc2898DeriveBytes deriveBytes = new Rfc2898DeriveBytes(
                        password: password + notBefore.ToUnixTimeMilliseconds() + notAfter.ToUnixTimeMilliseconds(),
                        salt: salt,
                        iterations: iterations,
                        hashAlgorithm: HashAlgorithmName.SHA256);
                    key = deriveBytes.GetBytes(keyBytes);
                    break;
                default:
                    throw new ArgumentOutOfRangeException($"'{algorithm}' {nameof(algorithm)} is not valid.");
            }

            return key;
        }

        /// <summary>
        /// Hash the <paramref name="password"/>, adjusting algorithm so that the key cannot easily be brute forced before it expires.
        /// </summary>
        /// <param name="password"></param>
        /// <param name="passwordCombinations"></param>
        /// <param name="salt"></param>
        /// <param name="algorithm"></param>
        /// <param name="iterations"></param>
        /// <param name="notBefore"></param>
        /// <param name="notAfter"></param>
        /// <returns></returns>
        public string Hash(string password, double passwordCombinations, byte[] salt, string algorithm, DateTimeOffset notBefore, DateTimeOffset notAfter)
        {
            var sw = new Stopwatch();

            int passwordEntropy = (int)Math.Log(passwordCombinations, 2);
            // Lower bound password combinations based on bits of entropy
            passwordCombinations = Math.Pow(2, passwordEntropy);

            if (!entropyToIterations.TryGetValue(passwordEntropy, out int iterations))
            {
                iterations = minimumIterations;
            }

            // The total time an adversary would have to brute force a hash.
            TimeSpan lifespan = notAfter - utcNow;

            byte[] key;
            do
            {
                sw.Restart();
                key = CalculateKey(password, salt, algorithm, iterations, notBefore, notAfter);
                sw.Stop();

                // We've reached the maximum number of iterations.
                if (iterations >= maximumIterations)
                {
                    break;
                }

                // Estimated days to brute force the hash with the current hardware.
                double daysToBruteForce = passwordCombinations * sw.Elapsed.TotalDays;

                // Allow for up to 1000 times the compute of the current hardware.
                double ratioOfLifespanVsBruteForce = 1000 * lifespan.TotalDays / daysToBruteForce;

                if (ratioOfLifespanVsBruteForce > 1)
                {
                    // Increase iterations to compensate and try again.
                    iterations = (int)Math.Min(maximumIterations, Math.Max(minimumIterations, iterations * ratioOfLifespanVsBruteForce));

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
