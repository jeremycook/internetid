using Humanizer;
using InternetId.Credentials;
using InternetId.Common.Email;
using InternetId.Users.Data;
using System;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace InternetId.Users.Services
{
    public class VerificationService
    {
        private const string veriyEmailPurpose = "verify_email";

        private readonly UsersDbContext usersDbContext;
        private readonly CredentialManager credentialManager;
        private readonly IEmailer emailer;

        public VerificationService(UsersDbContext usersDbContext, CredentialManager credentialManager, IEmailer emailer)
        {
            this.usersDbContext = usersDbContext;
            this.credentialManager = credentialManager;
            this.emailer = emailer;
        }

        public async Task SendVerifyEmailCodeAsync(User user, string email)
        {
            if (user is null) throw new System.ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(email)) throw new System.ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            var code = await credentialManager.GenerateCodeAsync(veriyEmailPurpose, user.Id, email.Trim());
            var purposeOptions = credentialManager.GetPurposeOptions(veriyEmailPurpose);

            await emailer.SendEmailAsync(email.Trim(), "Verification Code", $"{HtmlEncoder.Default.Encode(code)} is your email verification code. You have {TimeSpan.FromMinutes(purposeOptions.LifespanMinutes).Humanize()} to use it.");
        }

        public async Task<CredentialResult> VerifyEmailAsync(User user, string code)
        {
            var result = await credentialManager.VerifySecretAsync(veriyEmailPurpose, user.Id, code);

            if (result.IsValid)
            {
                var dbUser = await usersDbContext.Users.FindAsync(user.Id);

                dbUser.Email = result.UserCode.Data;
                dbUser.EmailConfirmed = true;

                await usersDbContext.SaveChangesAsync();
            }

            return result;
        }
    }
}
