using Humanizer;
using InternetId.Common.Email;
using InternetId.Credentials;
using InternetId.Users.Data;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InternetId.Users.Services
{
    public class UserVerifyEmailService
    {
        private const string purpose = "user_verify_email";

        private readonly UsersDbContext usersDbContext;
        private readonly CredentialManager credentialManager;
        private readonly IEmailer emailer;

        public UserVerifyEmailService(UsersDbContext usersDbContext, CredentialManager credentialManager, IEmailer emailer)
        {
            this.usersDbContext = usersDbContext;
            this.credentialManager = credentialManager;
            this.emailer = emailer;
        }

        public async Task SendVerifyEmailCodeAsync(User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(user.Email)) throw new ArgumentException($"'{nameof(user)}' does not have an email.", nameof(user));

            var email = user.Email;

            var shortcode = await credentialManager.CreateShortcodeAsync(purpose, user.Id.ToString(), data: email);
            var purposeOptions = credentialManager.GetPurposeOptions(purpose);

            await emailer.SendEmailAsync(email, "Verification Shortcode", $"<strong>{HtmlEncoder.Default.Encode(shortcode)}</strong> is your email verification shortcode. You have {TimeSpan.FromMinutes(purposeOptions.LifespanMinutes).Humanize()} to use it.");
        }

        public async Task SendChangeEmailCodeAsync(User user, string email)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(user.Email)) throw new ArgumentException($"'{nameof(user)}' does not have an email.", nameof(user));
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            email = email.Trim();

            var shortcode = await credentialManager.CreateShortcodeAsync(purpose, user.Id.ToString(), data: email);
            var purposeOptions = credentialManager.GetPurposeOptions(purpose);

            await emailer.SendEmailAsync(user.Email, "Verification Shortcode", $"<strong>{Encode(shortcode)}</strong> is your email verification shortcode. Use it to verify that you want to change your account's email from {Encode(user.Email)} to {Encode(email)}. You have {TimeSpan.FromMinutes(purposeOptions.LifespanMinutes).Humanize()} to use it.");
        }

        public async Task<CredentialResult> VerifyEmailAsync(User user, string code)
        {
            var result = await credentialManager.VerifyShortcodeAsync(purpose, user.Id.ToString(), code);

            if (result.Outcome == VerifySecretOutcome.Verified)
            {
                var dbUser = await usersDbContext.Users.FindAsync(user.Id);

                dbUser.Email = result.UserCode!.Data;
                dbUser.EmailVerified = true;

                await usersDbContext.SaveChangesAsync();

                await credentialManager.RemoveCredentialAsync(purpose, user.Id.ToString());
            }

            return result;
        }

        private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
    }
}
