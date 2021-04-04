using System.Collections.Generic;

namespace InternetId.Credentials
{
    public class CredentialsOptions
    {
        public Dictionary<string, PurposeOptions> Purposes { get; set; } = new Dictionary<string, PurposeOptions>();

        public class PurposeOptions
        {
            public float LifespanMinutes { get; set; } = 10;
            public float LockoutMinutes { get; set; } = 10;
            public int AttemptsPerLockout { get; set; } = 10;
            public bool RetainAfterVerification { get; set; } = false;
        }
    }
}
