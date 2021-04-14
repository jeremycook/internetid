using InternetId.Common.Email;
using InternetId.Credentials;
using InternetId.Users.Data;
using Microsoft.Extensions.Logging;
using PwnedPasswords.Client;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace InternetId.Users.Services
{
    public class PasswordService
    {
        private const string password = "password";

        private readonly ILogger<PasswordService> logger;
        private readonly CredentialManager credentialManager;
        private readonly IPwnedPasswordsClient pwnedPasswordsClient;
        private readonly IEmailer emailer;

        public PasswordService(ILogger<PasswordService> logger, CredentialManager credentialManager, IPwnedPasswordsClient pwnedPasswordsClient, IEmailer emailer)
        {
            this.logger = logger;
            this.credentialManager = credentialManager;
            this.pwnedPasswordsClient = pwnedPasswordsClient;
            this.emailer = emailer;
        }

        /// <summary>
        /// Creates or updates the password of the <see cref="User.Id"/> of <paramref name="user"/>.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ValidationException"></exception>
        public async Task SetPasswordAsync(User user, string password)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or empty.", nameof(password));

            password = password.Trim();

            if (password.Length < 9)
            {
                throw new ValidationException("The password must be at least 9 characters long.");
            }

            bool pwned;
            try
            {
                pwned = await pwnedPasswordsClient.HasPasswordBeenPwned(password);
            }
            catch (Exception ex)
            {
                // Log and fail-open if cannot connect to the service.
                logger.LogWarning(ex, $"Suppressed {ex.GetType()}: {ex.Message}");
                pwned = false;
            }

            if (pwned)
            {
                throw new ValidationException("The password is considered weak. Please enter another password.");
            }

            await credentialManager.SetCredentialAsync(PasswordService.password, user.Id.ToString(), password);
        }

        /// <summary>
        /// Changes the password of the <see cref="User.Id"/> of <paramref name="user"/>
        /// by first verifying that <paramref name="password"/> is correct and then setting <paramref name="newPassword"/>
        /// if it is.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public async Task ChangePasswordAsync(User user, string password, string newPassword)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or whitespace.", nameof(password));
            if (string.IsNullOrWhiteSpace(newPassword)) throw new ArgumentException($"'{nameof(newPassword)}' cannot be null or whitespace.", nameof(newPassword));

            if (await VerifyPasswordAsync(user, password) is var result)
            {
                if (result.Outcome == VerifySecretOutcome.Verified ||
                    result.Outcome == VerifySecretOutcome.Expired)
                {
                    await SetPasswordAsync(user, newPassword);

                    if (user.Email != null && user.EmailVerified)
                    {
                        await emailer.SendEmailAsync(user.Email, "Password Changed", $"Your password has changed.");
                    }
                }
            }
        }

        /// <summary>
        /// Verifies the password of the <see cref="User.Id"/> of <paramref name="user"/>.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<CredentialResult> VerifyPasswordAsync(User user, string password)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or whitespace.", nameof(password));

            password = password.Trim();

            var result = await credentialManager.VerifySecretAsync(PasswordService.password, user.Id.ToString(), password, removeIfVerified: false);
            return result;
        }
    }
}
