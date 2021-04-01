using InternetId.Common.Crypto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InternetId.Common.Codes
{
    public class CodeManager : IDisposable
    {
        private readonly Hasher passwordHasher;
        private readonly ILogger<CodeManager> logger;
        private readonly IOptions<InternetIdCodesOptions> userCodeOptions;
        private readonly IServiceScope scope;
        private readonly InternetIdCodesDbContext db;

        public CodeManager(IServiceProvider serviceProvider, Hasher passwordHasher, ILogger<CodeManager> logger, IOptions<InternetIdCodesOptions> userCodeOptions)
        {
            this.passwordHasher = passwordHasher;
            this.logger = logger;
            this.userCodeOptions = userCodeOptions;
            scope = serviceProvider.CreateScope();
            db = scope.ServiceProvider.GetRequiredService<InternetIdCodesDbContext>();
        }

        public void Dispose()
        {
            scope?.Dispose();
        }

        /// <summary>
        /// Generates a new code. If a code already exists for the user with the same purpose then it will be updated.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="purpose"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<string> GenerateCodeAsync(string purpose, string key, string data = "")
        {
            if (string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException($"'{nameof(purpose)}' cannot be null or empty.", nameof(purpose));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
            data ??= string.Empty;

            var purposeOptions = GetPurposeOptions(purpose);
            string code = RandomNumberGenerator.GetInt32(1000000, 10000000).ToString().Substring(1);
            string hashedCode = HashCode(purpose, key, data, code, TimeSpan.FromMinutes(purposeOptions.LifespanMinutes));

            var userCode = await FindCodeAsync(purpose, key);
            if (userCode != null)
            {
                // Reset the code while maintaining lockout information.
                userCode.Data = data;
                userCode.ValidUntil = DateTimeOffset.Now.AddMinutes(purposeOptions.LifespanMinutes);
                userCode.HashedCode = hashedCode;
                await db.SaveChangesAsync();

                logger.LogInformation("Reset {Purpose} code", purpose);
            }
            else
            {
                // If a code does not exist, create one.
                userCode = new Code
                {
                    Purpose = purpose,
                    Key = key,
                    Data = data,
                    ValidUntil = DateTimeOffset.Now.AddMinutes(purposeOptions.LifespanMinutes),
                    LockedOutUntil = null,
                    Attempts = 0,
                    HashedCode = hashedCode,
                };
                db.Codes.Add(userCode);
                await db.SaveChangesAsync();

                logger.LogInformation("Created {Purpose} code", purpose);
            }

            return code;
        }

        public async Task<CodeResult> VerifyCodeAsync(string purpose, string key, string code)
        {
            if (string.IsNullOrWhiteSpace(purpose)) throw new ArgumentException($"'{nameof(purpose)}' cannot be null or empty.", nameof(purpose));
            if (string.IsNullOrWhiteSpace(key)) throw new ArgumentException($"'{nameof(key)}' cannot be null or empty.", nameof(key));
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException($"'{nameof(code)}' cannot be null or empty.", nameof(code));

            var userCode = await FindCodeAsync(purpose, key);
            var purposeOptions = GetPurposeOptions(purpose);

            if (userCode == null)
            {
                logger.LogInformation("Invalid {Purpose} code", purpose);
                return CodeResult.InvalidCode();
            }

            if (userCode.Attempts >= purposeOptions.AttemptsPerLockout)
            {
                // Verification attempts exceeded.

                if (userCode.LockedOutUntil == null)
                {
                    throw new InvalidOperationException("The LockedOutUntil field must already be set.");
                }

                if (userCode.LockedOutUntil.Value >= DateTimeOffset.Now)
                {
                    // Track failed attempt
                    userCode.Attempts++;
                    await db.SaveChangesAsync();

                    // Fail until we hit the unlock timeout.
                    logger.LogInformation("Locked {Purpose} code", purpose);
                    return CodeResult.TryAgainLater(userCode, userCode.LockedOutUntil.Value);
                }

                // The lockout has passed, unlock it and permit verification.
                userCode.LockedOutUntil = null;
                userCode.Attempts = 0;
                await db.SaveChangesAsync();
            }

            if (IsValidCode(userCode, code, TimeSpan.FromMinutes(purposeOptions.LifespanMinutes)))
            {
                if (userCode.ValidUntil < DateTimeOffset.Now)
                {
                    // Only let the user know if it expired if the code is valid.
                    logger.LogInformation("Expired {Purpose} code", purpose);
                    return CodeResult.Expired(userCode);
                }
                else
                {
                    // Remove the record, no reason to keep it around
                    db.Codes.Remove(userCode);
                    await db.SaveChangesAsync();

                    logger.LogInformation("Valid {Purpose} code", purpose);
                    return CodeResult.Valid(userCode);
                }
            }
            else
            {
                if (userCode.Attempts == 0)
                {
                    // If this is the first failed attempt then start the lockout.
                    userCode.LockedOutUntil = DateTimeOffset.Now.AddMinutes(purposeOptions.LockoutMinutes);
                }
                // Track failed attempt
                userCode.Attempts++;
                await db.SaveChangesAsync();

                logger.LogInformation("Invalid {Purpose} code", purpose);
                return CodeResult.InvalidCode(userCode);
            }
        }

        public InternetIdCodesOptions.PurposeOptions GetPurposeOptions(string purpose)
        {
            if (!userCodeOptions.Value.Purposes.TryGetValue(purpose, out var purposeOptions))
            {
                purposeOptions = InternetIdCodesOptions.PurposeOptions.Fallback;
            }

            return purposeOptions;
        }

        private async Task<Code> FindCodeAsync(string purpose, string key)
        {
            return await db.Codes.SingleOrDefaultAsync(o => o.Purpose == purpose && o.Key == key);
        }

        private string HashCode(string purpose, string key, string data, string code, TimeSpan lifespan)
        {
            code = Regex.Replace(code, "[^0-9]+", "");
            return passwordHasher.Hash(purpose + key + data + code, lifespan);
        }

        private bool IsValidCode(Code userCode, string code, TimeSpan lifespan)
        {
            code = Regex.Replace(code, "[^0-9]+", "");
            return passwordHasher.Verify(userCode.Purpose + userCode.Key + userCode.Data + code, lifespan, userCode.HashedCode);
        }
    }
}
