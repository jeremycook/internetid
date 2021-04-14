using InternetId.Common.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InternetId.Credentials
{
    public class CredentialManager : IDisposable
    {
        private const string alphaTokens = "abcdefghijklmnopqrstuvwxyz";
        private const string numericTokens = "0123456789";

        private readonly Hasher passwordHasher;
        private readonly ILogger<CredentialManager> logger;
        private readonly IOptions<CredentialsOptions> userCodeOptions;
        private readonly IServiceScope scope;
        private readonly ICredentialsDbContext db;

        public CredentialManager(IServiceProvider serviceProvider, Hasher passwordHasher, ILogger<CredentialManager> logger, IOptions<CredentialsOptions> userCodeOptions)
        {
            this.passwordHasher = passwordHasher;
            this.logger = logger;
            this.userCodeOptions = userCodeOptions;
            scope = serviceProvider.CreateScope();
            db = scope.ServiceProvider.GetRequiredService<ICredentialsDbContext>();
        }

        public void Dispose()
        {
            scope?.Dispose();
        }

        /// <summary>
        /// Returns a random 8 digit code after creating a new credential or updating the matching credential.
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> CreatePinAsync(string purpose, string key, string data = "")
        {
            string secret = string.Concat(Enumerable.Range(0, 8).Select(i => numericTokens[RandomNumberGenerator.GetInt32(0, numericTokens.Length)]));

            await SetCredentialAsync(purpose, key, secret, data);

            return secret;
        }

        /// <summary>
        /// Returns a random 6 letter code after creating a new credential or updating the matching credential.
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="key"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> CreateShortcodeAsync(string purpose, string key, string data = "")
        {
            string secret = string.Concat(Enumerable.Range(0, 6).Select(i => alphaTokens[RandomNumberGenerator.GetInt32(0, alphaTokens.Length)]));

            await SetCredentialAsync(purpose, key, secret, data);

            return secret;
        }

        /// <summary>
        /// Creates a new credential or updates the matching credential if one already exists.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="purpose"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task SetCredentialAsync(string purpose, string key, string secret, string data = "")
        {
            if (string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException($"'{nameof(purpose)}' cannot be null or empty.", nameof(purpose));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
            if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException($"'{nameof(secret)}' cannot be null or empty.", nameof(secret));

            data ??= string.Empty;

            var purposeOptions = GetPurposeOptions(purpose);
            string hash = HashSecret(purpose, key, secret, data, TimeSpan.FromDays(purposeOptions.LifespanDays));

            var userCode = await FindCredentialAsync(purpose, key);
            if (userCode != null)
            {
                // Reset the credential while maintaining lockout information.
                userCode.Data = data;
                userCode.Hash = hash;
                await db.SaveChangesAsync();

                logger.LogInformation("Reset {Purpose} credential", purpose);
            }
            else
            {
                // If a credential does not exist, create one.
                userCode = new Credential
                {
                    Purpose = purpose,
                    Key = key,
                    Data = data,
                    LockedUntil = null,
                    Attempts = 0,
                    Hash = hash,
                };
                db.Credentials.Add(userCode);
                await db.SaveChangesAsync();

                logger.LogInformation("Created {Purpose} credential", purpose);
            }
        }

        /// <summary>
        /// Verifies a 8 digit code, automatically removing invalid characters before verifying.
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="key"></param>
        /// <param name="pin"></param>
        /// <returns></returns>
        public async Task<CredentialResult> VerifyPinAsync(string purpose, string key, string pin, bool removeIfVerified)
        {
            string secret = pin ?? string.Empty;

            // Keep valid characters
            secret = string.Concat(secret.Where(ch => numericTokens.Contains(ch)));

            return await VerifySecretAsync(purpose, key, secret, removeIfVerified);
        }

        /// <summary>
        /// Verifies a 6 letter shortcode, automatically removing invalid characters before verifying.
        /// </summary>
        /// <param name="purpose"></param>
        /// <param name="key"></param>
        /// <param name="shortcode"></param>
        /// <returns></returns>
        public async Task<CredentialResult> VerifyShortcodeAsync(string purpose, string key, string shortcode, bool removeIfVerified)
        {
            // Shortcodes will always be lowercase
            string secret = shortcode?.ToLowerInvariant() ?? string.Empty;

            // Keep valid characters
            secret = string.Concat(secret.Where(ch => alphaTokens.Contains(ch)));

            return await VerifySecretAsync(purpose, key, secret, removeIfVerified);
        }

        public async Task<CredentialResult> VerifySecretAsync(string purpose, string key, string secret, bool removeIfVerified)
        {
            if (string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException($"'{nameof(purpose)}' cannot be null or whitespace.", nameof(purpose));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException($"'{nameof(key)}' cannot be null or whitespace.", nameof(key));
            if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException($"'{nameof(secret)}' cannot be null or whitespace.", nameof(secret));

            var credential = await FindCredentialAsync(purpose, key);
            var purposeOptions = GetPurposeOptions(purpose);

            if (credential == null)
            {
                logger.LogInformation("Invalid {Purpose} credential", purpose);
                return CredentialResult.Invalid();
            }

            if (credential.Attempts >= purposeOptions.AttemptsPerLockout)
            {
                // Verification attempts exceeded.

                if (credential.LockedUntil == null)
                {
                    throw new InvalidOperationException("The LockedOutUntil field must already be set.");
                }

                if (credential.LockedUntil.Value >= DateTimeOffset.UtcNow)
                {
                    // Track failed attempt
                    credential.Attempts++;
                    await db.SaveChangesAsync();

                    // Fail until we hit the unlock timeout.
                    logger.LogInformation("Locked {Purpose} credential", purpose);
                    return CredentialResult.Locked(credential, credential.LockedUntil.Value);
                }

                // The lockout has passed, unlock it and permit verification.
                credential.LockedUntil = null;
                credential.Attempts = 0;
                await db.SaveChangesAsync();
            }

            HasherVerificationResult verificationResult = VerifyHashedSecret(credential, secret);
            switch (verificationResult)
            {
                case HasherVerificationResult.Invalid:

                    if (credential.Attempts == 0)
                    {
                        // If this is the first failed attempt then start the lockout.
                        credential.LockedUntil = DateTimeOffset.UtcNow.AddMinutes(purposeOptions.LockMinutes);
                    }
                    // Track failed attempt
                    credential.Attempts++;
                    await db.SaveChangesAsync();

                    logger.LogInformation("Invalid {Purpose} credential", purpose);
                    return CredentialResult.Invalid(credential);

                case HasherVerificationResult.Expired:

                    // Only let the user know it expired if the credential is valid.
                    logger.LogInformation("Expired {Purpose} credential", purpose);
                    return CredentialResult.Expired(credential);

                case HasherVerificationResult.Valid:

                    if (removeIfVerified)
                    {
                        await RemoveCredentialAsync(purpose, key);
                    }

                    logger.LogInformation("Valid {Purpose} credential", purpose);
                    return CredentialResult.Verified(credential);

                case HasherVerificationResult.Inactive:
                default:

                    throw new NotSupportedException($"The {verificationResult} HasherResult is not supported.");
            }
        }

        public async Task RemoveCredentialAsync(string purpose, string key)
        {
            if (await FindCredentialAsync(purpose, key) is Credential credential)
            {
                db.Credentials.Remove(credential);
                await db.SaveChangesAsync();
                logger.LogInformation("Removed {Purpose} credential", purpose);
            }
        }

        public CredentialsOptions.PurposeOptions GetPurposeOptions(string purpose)
        {
            if (!userCodeOptions.Value.Purposes.TryGetValue(purpose, out var purposeOptions))
            {
                throw new ArgumentOutOfRangeException(nameof(purpose), $"'{purpose}' purpose options could not be found. It must be configured.");
            }
            else if (!purposeOptions.Enabled)
            {
                throw new ArgumentException($"'{purpose}' purpose is disabled.", nameof(purpose));
            }

            return purposeOptions;
        }

        private async Task<Credential> FindCredentialAsync(string purpose, string key)
        {
            return await db.Credentials.SingleOrDefaultAsync(o => o.Purpose == purpose && o.Key == key);
        }

        private string HashSecret(string purpose, string key, string secret, string data, TimeSpan lifespan)
        {
            var x = Regex.IsMatch(secret, "[A-Za-z]") ? 26 : 10;
            return passwordHasher.Hash(purpose + key + secret + data, Math.Pow(x, secret.Length), lifespan);
        }

        private HasherVerificationResult VerifyHashedSecret(Credential credential, string secret)
        {
            return passwordHasher.Verify(credential.Purpose + credential.Key + secret + credential.Data, credential.Hash);
        }
    }
}
