using Humanizer;
using InternetId.Common.Email;
using InternetId.Credentials;
using InternetId.Users.Data;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InternetId.Users.Services
{
    public class EmailService
    {
        private const string verificationPurpose = "email_verification";
        private const string changePurpose = "email_change";

        private readonly UsersDbContext usersDbContext;
        private readonly CredentialManager credentialManager;
        private readonly IEmailer emailer;

        public EmailService(UsersDbContext usersDbContext, CredentialManager credentialManager, IEmailer emailer)
        {
            this.usersDbContext = usersDbContext;
            this.credentialManager = credentialManager;
            this.emailer = emailer;
        }

        /// <summary>
        /// Returns <c>true</c> if a verification code was sent.
        /// Changes the <see cref="User.Email"/> of <paramref name="user"/> and sends a verification code if needed.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<bool> ChangeEmailAsync(User user, string? email)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));

            email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

            if (user.LowercaseEmail == email?.ToLowerInvariant())
            {
                return false;
            }

            user.Email = email;
            user.EmailVerified = false;
            await usersDbContext.SaveChangesAsync();

            if (user.Email == null)
            {
                return false;
            }

            await SendVerificationCodeAsync(user);
            return true;
        }

        public async Task SendVerificationCodeAsync(User user)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(user.Email)) throw new ArgumentException($"'{nameof(user)}' does not have an email.", nameof(user));

            var email = user.Email;

            var shortcode = await credentialManager.CreateShortcodeAsync(verificationPurpose, user.Id.ToString(), data: email);
            var purposeOptions = credentialManager.GetPurposeOptions(verificationPurpose);

            await emailer.SendEmailAsync(email, "Email Verification Code", $"<strong>{HtmlEncoder.Default.Encode(shortcode)}</strong> is your email verification code and will be valid for {TimeSpan.FromDays(purposeOptions.LifespanDays).Humanize()}. Use it to demonstrate that you have access to this email account.");
        }

        public async Task<CredentialResult> VerifyAsync(User user, string code)
        {
            var result = await credentialManager.VerifyShortcodeAsync(verificationPurpose, user.Id.ToString(), code);

            if (result.Outcome == VerifySecretOutcome.Verified)
            {
                var dbUser = await usersDbContext.Users.FindAsync(user.Id);

                dbUser.Email = result.Credential!.Data;
                dbUser.EmailVerified = true;

                await usersDbContext.SaveChangesAsync();
            }

            return result;
        }

        public async Task SendChangeCodeAsync(User user, string newEmail)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(user.Email)) throw new ArgumentException($"'{nameof(user)}' does not have an email.", nameof(user));
            if (string.IsNullOrWhiteSpace(newEmail)) throw new ArgumentException($"'{nameof(newEmail)}' cannot be null or empty.", nameof(newEmail));

            newEmail = newEmail.Trim();

            var shortcode = await credentialManager.CreateShortcodeAsync(changePurpose, user.Id.ToString(), data: newEmail);
            var purposeOptions = credentialManager.GetPurposeOptions(changePurpose);

            await emailer.SendEmailAsync(user.Email, "Change Email Code", $"<strong>{Encode(shortcode)}</strong> is your change email code and will be valid for {TimeSpan.FromDays(purposeOptions.LifespanDays).Humanize()}. Use it to confirm that you want to change your account's email from {Encode(user.Email)} to {Encode(newEmail)}.");
        }

        public async Task<CredentialResult> ConfirmChangeAsync(User user, string code)
        {
            var result = await credentialManager.VerifyShortcodeAsync(changePurpose, user.Id.ToString(), code);

            if (result.Outcome == VerifySecretOutcome.Verified)
            {
                var newEmail = result.Credential!.Data.Trim();
                await SendChangeCodeAsync(user, newEmail);
            }

            return result;
        }

        private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
    }
}
