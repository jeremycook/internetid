using InternetId.Common.Email;
using InternetId.Credentials;
using InternetId.Users.Data;
using PwnedPasswords.Client;
using System;
using System.Threading.Tasks;

namespace InternetId.Users.Services
{
    public class UserPasswordService
    {
        private const string purpose = "user_password";

        private readonly CredentialManager credentialManager;
        private readonly IPwnedPasswordsClient pwnedPasswordsClient;
        private readonly IEmailer emailer;

        public UserPasswordService(CredentialManager credentialManager, IPwnedPasswordsClient pwnedPasswordsClient, IEmailer emailer)
        {
            this.credentialManager = credentialManager;
            this.pwnedPasswordsClient = pwnedPasswordsClient;
            this.emailer = emailer;
        }

        public async Task SetPasswordAsync(User user, string password)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or whitespace.", nameof(password));

            password = password.Trim();
            if (password.Length < 9) throw new ArgumentException("A password must be at least 9 characters long.", nameof(password));

            await credentialManager.SetCredentialAsync(purpose, user.Id.ToString(), password);
        }

        public async Task ChangePasswordAsync(User user, string password, string newPassword)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or whitespace.", nameof(password));
            if (string.IsNullOrWhiteSpace(newPassword)) throw new ArgumentException($"'{nameof(newPassword)}' cannot be null or whitespace.", nameof(newPassword));

            newPassword = newPassword.Trim();
            if (newPassword.Length < 9) throw new ArgumentException("A password must be at least 9 characters long.", nameof(newPassword));

            if (await VerifyPasswordAsync(user, password) is var result)
            {
                if (result.Outcome == VerifySecretOutcome.Verified ||
                    result.Outcome == VerifySecretOutcome.Expired)
                {
                    await credentialManager.SetCredentialAsync(purpose, user.Id.ToString(), newPassword);

                    if (user.Email != null && user.EmailVerified)
                    {
                        await emailer.SendEmailAsync(user.Email, "Password Changed", $"Your password has changed.");
                    }
                }
            }
        }

        public async Task<CredentialResult> VerifyPasswordAsync(User user, string password)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or whitespace.", nameof(password));

            password = password.Trim();

            var result = await credentialManager.VerifySecretAsync(purpose, user.Id.ToString(), password);
            return result;
        }
    }
}
