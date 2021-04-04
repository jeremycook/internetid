using Humanizer;
using InternetId.Common.Email;
using InternetId.Credentials;
using InternetId.Users.Data;
using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InternetId.Users.Services
{
    public class PasswordResetService
    {
        private const string purpose = "password_reset";

        private readonly CredentialManager credentialManager;
        private readonly PasswordService passwordService;
        private readonly IEmailer emailer;

        public PasswordResetService(CredentialManager credentialManager, PasswordService passwordService, IEmailer emailer)
        {
            this.credentialManager = credentialManager;
            this.passwordService = passwordService;
            this.emailer = emailer;
        }

        public async Task SendPasswordResetCodeAsync(User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));

            if (string.IsNullOrWhiteSpace(user.Email)) throw new ValidationException($"The user does not have an email address for sending a password reset code to.");

            var shortcode = await credentialManager.CreateShortcodeAsync(purpose, user.Id.ToString());
            var purposeOptions = credentialManager.GetPurposeOptions(purpose);

            await emailer.SendEmailAsync(user.Email, "Password Reset Code", $"<strong>{Encode(shortcode)}</strong> is your password reset code and will be valid for {TimeSpan.FromDays(purposeOptions.LifespanDays).Humanize()}. Use it to reset your password.");
        }

        /// <summary>
        /// Returns <c>true</c> if successful.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="code"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<CredentialResult> ResetPasswordAsync(User user, string code, string password)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(code)) throw new ArgumentException($"'{nameof(code)}' cannot be null or empty.", nameof(code));
            if (string.IsNullOrWhiteSpace(password)) throw new ArgumentException($"'{nameof(password)}' cannot be null or empty.", nameof(password));

            var result = await credentialManager.VerifyShortcodeAsync(purpose, user.Id.ToString(), code);

            if (result.Outcome == VerifySecretOutcome.Verified)
            {
                await passwordService.SetPasswordAsync(user, password);

                // Success, delete the shortcode.
                await credentialManager.RemoveCredentialAsync(purpose, user.Id.ToString());
            }

            return result;
        }

        private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
    }
}
