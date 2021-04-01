using Humanizer;
using System;

namespace InternetId.Common.Codes
{
    public class CodeResult
    {
        public bool IsValid { get; private set; }
        public string Message { get; private set; } = null!;
        public Code? UserCode { get; private set; }

        public static CodeResult InvalidCode(Code? userCode = null) => new CodeResult
        {
            IsValid = false,
            Message = "The code is invalid. Verify you entered the correct code. If it looks correct you may want to generate a new code.",
            UserCode = userCode,
        };

        public static CodeResult TryAgainLater(Code userCode, DateTimeOffset lockedOutUntil) => new CodeResult
        {
            IsValid = false,
            Message = $"Too many failed attempts. You'll be able to try again in {(DateTimeOffset.Now - lockedOutUntil).Humanize()}.",
            UserCode = userCode,
        };

        public static CodeResult Expired(Code userCode) => new CodeResult
        {
            IsValid = false,
            Message = "The code has expired. Please generate and use a new code.",
            UserCode = userCode,
        };

        public static CodeResult Valid(Code userCode) => new CodeResult
        {
            IsValid = true,
            Message = "Valid code.",
            UserCode = userCode,
        };
    }
}
