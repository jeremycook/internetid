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
        /// <summary>
        /// RFC2898 using SHA256.
        /// </summary>
        private const string rfc2898 = "rfc2898";
        private const int saltBytes = 16;
        private const int keyBytes = 32;
        /// <summary>
        /// 100K iterations take about 0.02 seconds.
        /// </summary>
        private const int minimumIterations = 100_000;
        /// <summary>
        /// 10 million iterations take about 2 seconds.
        /// </summary>
        private const int maximumIterations = 10_000_000;
        /// <summary>
        /// The number of iterations is partially based on an assumption about the resources
        /// an attacker would have access to. The attack factor multiplies the current hardware
        /// used for deriving a key by some value like 1000.
        /// </summary>
        private const int attackFactor = 1000;

        private static readonly Dictionary<int, int> entropyToIterations = new Dictionary<int, int>();
        private static readonly JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly DateTimeOffset utcNow;

        public Hasher()
        {
            utcNow = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Support testing
        /// </summary>
        /// <param name="utcNow"></param>
        private Hasher(DateTimeOffset utcNow)
        {
            this.utcNow = utcNow;
        }

        /// <summary>
        /// WARNING: Public-internal API, can change at anytime.
        /// </summary>
        /// <param name="utcNow"></param>
        /// <returns></returns>
        [Obsolete("Public-internal API, can change at anytime.")]
        public static Hasher CreateTestHasher(DateTimeOffset utcNow)
        {
            return new Hasher(utcNow: utcNow);
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
            // The total time an adversary would have to brute force a hash.
            double daysAvailableToAttack = utcNow >= notAfter ? 0 : (notAfter - utcNow).TotalDays;

            int complexityEntropy = (int)Math.Log(passwordCombinations, 2);

            int timeComplexityEntropy = (int)Math.Log(daysAvailableToAttack, 2) + complexityEntropy;

            if (!entropyToIterations.TryGetValue(timeComplexityEntropy, out int iterations))
            {
                iterations = minimumIterations;
            }

            // Apply a step function to password combinations based on bits of entropy
            passwordCombinations = Math.Pow(2, complexityEntropy);

            var sw = new Stopwatch();

            byte[] key = null;
            for (int i = 0; i < 5; i++)
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

                double iterationBooster = attackFactor * daysAvailableToAttack / daysToBruteForce;

                if (iterationBooster > 1)
                {
                    // Increase iterations to compensate and try again.
                    iterations = (int)Math.Min(maximumIterations, Math.Max(minimumIterations, iterations * iterationBooster));

                    if (!entropyToIterations.TryGetValue(timeComplexityEntropy, out int currentIterations) || currentIterations < iterations)
                    {
                        // Cache to save time recalculating in the future.
                        entropyToIterations[timeComplexityEntropy] = iterations;
                    }
                }
                else
                {
                    break;
                }
            }

            if (key == null)
            {
                throw new NullReferenceException($"'{nameof(key)}' is null.");
            }

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
