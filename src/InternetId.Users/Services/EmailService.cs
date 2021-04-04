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
        private const string purpose = "email_verification";

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
        /// Returns <c>true</c> if a verification is needed and a code was sent.
        /// Changes the <see cref="User.Email"/> of <paramref name="user"/> to <paramref name="email"/>,
        /// sets <see cref="User.EmailVerified"/> to <c>false</c>,
        /// and sends a verification code if needed.
        /// </summary>
        /// <param name="user"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        public async Task<bool> ChangeEmailAsync(User user, string? email)
        {
            if (user is null) throw new ArgumentNullException(nameof(user));

            email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();

            if (string.Equals(user.Email, email, StringComparison.InvariantCultureIgnoreCase))
            {
                // Do nothing, the email hasn't changed.
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

            var shortcode = await credentialManager.CreateShortcodeAsync(purpose, user.Id.ToString(), data: email);
            var purposeOptions = credentialManager.GetPurposeOptions(purpose);

            await emailer.SendEmailAsync(email, "Email Verification Code", $"<strong>{Encode(shortcode)}</strong> is your email verification code and will be valid for {TimeSpan.FromDays(purposeOptions.LifespanDays).Humanize()}. Use it to demonstrate that you have access to this email account.");
        }

        public async Task<CredentialResult> VerifyAsync(User user, string code)
        {
            var result = await credentialManager.VerifyShortcodeAsync(purpose, user.Id.ToString(), code, removeIfVerified: false);

            if (result.Outcome == VerifySecretOutcome.Verified)
            {
                var dbUser = await usersDbContext.Users.FindAsync(user.Id);

                if (!string.Equals(dbUser.Email, result.Credential!.Data, StringComparison.InvariantCultureIgnoreCase))
                {
                    // Treat this code as expired since the user's email has changed since this code was generated.
                    return CredentialResult.Expired(result.Credential);
                }

                dbUser.EmailVerified = true;
                await usersDbContext.SaveChangesAsync();

                await credentialManager.RemoveCredentialAsync(purpose, user.Id.ToString());
            }

            return result;
        }

        private static string Encode(string value) => HtmlEncoder.Default.Encode(value);
    }
}
