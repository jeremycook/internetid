using Humanizer;
using InternetId.Common.Codes;
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
        private readonly CodeManager codeManager;
        private readonly IEmailer emailer;

        public VerificationService(UsersDbContext usersDbContext, CodeManager codeManager, IEmailer emailer)
        {
            this.usersDbContext = usersDbContext;
            this.codeManager = codeManager;
            this.emailer = emailer;
        }

        public async Task SendVerifyEmailCodeAsync(User user, string email)
        {
            if (user is null) throw new System.ArgumentNullException(nameof(user));
            if (string.IsNullOrWhiteSpace(email)) throw new System.ArgumentException($"'{nameof(email)}' cannot be null or empty.", nameof(email));

            var code = await codeManager.GenerateCodeAsync(veriyEmailPurpose, user.Id, email.Trim());
            var purposeOptions = codeManager.GetPurposeOptions(veriyEmailPurpose);

            await emailer.SendEmailAsync(email.Trim(), "Verification Code", $"{HtmlEncoder.Default.Encode(code)} is your email verification code. You have {TimeSpan.FromMinutes(purposeOptions.LifespanMinutes).Humanize()} to use it.");
        }

        public async Task<CodeResult> VerifyEmailAsync(User user, string code)
        {
            var result = await codeManager.VerifyCodeAsync(veriyEmailPurpose, user.Id, code);

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
