using Humanizer;
using System;

namespace InternetId.Credentials
{
    public class CredentialResult
    {
        public bool IsValid { get; private set; }
        public string Message { get; private set; } = null!;
        public Credential? UserCode { get; private set; }

        public static CredentialResult InvalidCode(Credential? userCode = null) => new CredentialResult
        {
            IsValid = false,
            Message = "The code is invalid. Verify you entered the correct code. If it looks correct you may want to generate a new code.",
            UserCode = userCode,
        };

        public static CredentialResult TryAgainLater(Credential userCode, DateTimeOffset lockedOutUntil) => new CredentialResult
        {
            IsValid = false,
            Message = $"Too many failed attempts. You'll be able to try again in {(DateTimeOffset.Now - lockedOutUntil).Humanize()}.",
            UserCode = userCode,
        };

        public static CredentialResult Expired(Credential userCode) => new CredentialResult
        {
            IsValid = false,
            Message = "The code has expired. Please generate and use a new code.",
            UserCode = userCode,
        };

        public static CredentialResult Valid(Credential userCode) => new CredentialResult
        {
            IsValid = true,
            Message = "Valid code.",
            UserCode = userCode,
        };
    }
}
