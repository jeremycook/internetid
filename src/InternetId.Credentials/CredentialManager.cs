using InternetId.Common.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InternetId.Credentials
{
    public class CredentialManager : IDisposable
    {
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

        public async Task<string> GenerateCodeAsync(string purpose, string key, string data = "")
        {
            string secret = RandomNumberGenerator.GetInt32(1000000, 10000000).ToString().Substring(1);
            return await CreateCredentialAsync(purpose, key, secret, data);
        }

        /// <summary>
        /// Generates a new credential or updates the matching credential if one already exists.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="purpose"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> CreateCredentialAsync(string purpose, string key, string secret, string data)
        {
            if (string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException($"'{nameof(purpose)}' cannot be null or empty.", nameof(purpose));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
            if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException($"'{nameof(secret)}' cannot be null or empty.", nameof(secret));

            data ??= string.Empty;

            var purposeOptions = GetPurposeOptions(purpose);
            string hashedCode = HashSecret(purpose, key, secret, data, TimeSpan.FromMinutes(purposeOptions.LifespanMinutes));

            var userCode = await FindCredentialAsync(purpose, key);
            if (userCode != null)
            {
                // Reset the credential while maintaining lockout information.
                userCode.Data = data;
                userCode.HashedCode = hashedCode;
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
                    LockedOutUntil = null,
                    Attempts = 0,
                    HashedCode = hashedCode,
                };
                db.Credentials.Add(userCode);
                await db.SaveChangesAsync();

                logger.LogInformation("Created {Purpose} credential", purpose);
            }

            return secret;
        }

        public async Task<CredentialResult> VerifySecretAsync(string purpose, string key, string secret)
        {
            if (string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException($"'{nameof(purpose)}' cannot be null or empty.", nameof(purpose));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
            if (string.IsNullOrWhiteSpace(secret)) throw new ArgumentException($"'{nameof(secret)}' cannot be null or empty.", nameof(secret));

            var credential = await FindCredentialAsync(purpose, key);
            var purposeOptions = GetPurposeOptions(purpose);

            if (credential == null)
            {
                logger.LogInformation("Invalid {Purpose} credential", purpose);
                return CredentialResult.InvalidCode();
            }

            if (credential.Attempts >= purposeOptions.AttemptsPerLockout)
            {
                // Verification attempts exceeded.

                if (credential.LockedOutUntil == null)
                {
                    throw new InvalidOperationException("The LockedOutUntil field must already be set.");
                }

                if (credential.LockedOutUntil.Value >= DateTimeOffset.UtcNow)
                {
                    // Track failed attempt
                    credential.Attempts++;
                    await db.SaveChangesAsync();

                    // Fail until we hit the unlock timeout.
                    logger.LogInformation("Locked {Purpose} credential", purpose);
                    return CredentialResult.TryAgainLater(credential, credential.LockedOutUntil.Value);
                }

                // The lockout has passed, unlock it and permit verification.
                credential.LockedOutUntil = null;
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
                        credential.LockedOutUntil = DateTimeOffset.UtcNow.AddMinutes(purposeOptions.LockoutMinutes);
                    }
                    // Track failed attempt
                    credential.Attempts++;
                    await db.SaveChangesAsync();

                    logger.LogInformation("Invalid {Purpose} credential", purpose);
                    return CredentialResult.InvalidCode(credential);

                case HasherVerificationResult.Expired:

                    // Only let the user know it expired if the credential is valid.
                    logger.LogInformation("Expired {Purpose} credential", purpose);
                    return CredentialResult.Expired(credential);

                case HasherVerificationResult.Valid:

                    // Remove the record, no reason to keep it around.
                    db.Credentials.Remove(credential);
                    await db.SaveChangesAsync();

                    logger.LogInformation("Valid {Purpose} credential", purpose);
                    return CredentialResult.Valid(credential);

                case HasherVerificationResult.Inactive:
                default:

                    throw new NotSupportedException($"The {verificationResult} HasherResult is not supported.");
            }
        }

        public CredentialsOptions.PurposeOptions GetPurposeOptions(string purpose)
        {
            if (!userCodeOptions.Value.Purposes.TryGetValue(purpose, out var purposeOptions))
            {
                throw new ArgumentOutOfRangeException(nameof(purpose), $"'{purpose}' purpose options could not be found. It must be configured.");
            }

            return purposeOptions;
        }

        private async Task<Credential> FindCredentialAsync(string purpose, string key)
        {
            return await db.Credentials.SingleOrDefaultAsync(o => o.Purpose == purpose && o.Key == key);
        }

        private string HashSecret(string purpose, string key, string secret, string data, TimeSpan lifespan)
        {
            secret = Regex.Replace(secret, "[^0-9]+", "");
            return passwordHasher.Hash(purpose + key + secret + data, (int)Math.Pow(10, secret.Length), lifespan);
        }

        private HasherVerificationResult VerifyHashedSecret(Credential credential, string secret)
        {
            secret = Regex.Replace(secret, "[^0-9]+", "");
            return passwordHasher.Verify(credential.Purpose + credential.Key + secret + credential.Data, credential.HashedCode);
        }
    }
}
