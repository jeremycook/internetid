using Humanizer;
using System;

namespace InternetId.Credentials
{
    public class CredentialResult
    {
        public VerifySecretOutcome Outcome { get; private set; }
        public string Message { get; private set; } = null!;
        public Credential? Credential { get; private set; }

        public static CredentialResult Invalid(Credential? credential = null) => new CredentialResult
        {
            Outcome = VerifySecretOutcome.Invalid,
            Message = "The credential is invalid. Verify that what was entered is correct. If it looks correct you may want to generate a new one.",
            Credential = credential,
        };

        public static CredentialResult Locked(Credential credential, DateTimeOffset lockedOutUntil) => new CredentialResult
        {
            Outcome = VerifySecretOutcome.Locked,
            Message = $"Too many failed attempts. You'll be able to try again in {(DateTimeOffset.Now - lockedOutUntil).Humanize()}.",
            Credential = credential,
        };

        public static CredentialResult Expired(Credential credential) => new CredentialResult
        {
            Outcome = VerifySecretOutcome.Expired,
            Message = "The credential has expired. You will need to generate a new one.",
            Credential = credential,
        };

        public static CredentialResult Verified(Credential credential) => new CredentialResult
        {
            Outcome = VerifySecretOutcome.Verified,
            Message = "The credential was successfully verified.",
            Credential = credential,
        };
    }
}
