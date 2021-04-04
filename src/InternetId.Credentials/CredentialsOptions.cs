using System.Collections.Generic;

namespace InternetId.Credentials
{
    public class CredentialsOptions
    {
        public Dictionary<string, PurposeOptions> Purposes { get; set; } = new Dictionary<string, PurposeOptions>();

        public class PurposeOptions
        {
            public bool Enabled { get; set; }
            /// <summary>
            /// Configurable, defaults to 1 hour.
            /// </summary>
            public float LifespanDays { get; set; } = 1f / 24f;
            /// <summary>
            /// Configurable, defaults to 10 minutes.
            /// </summary>
            public float LockMinutes { get; set; } = 10;
            /// <summary>
            /// Configurable, defaults to 10.
            /// </summary>
            public int AttemptsPerLockout { get; set; } = 10;
        }
    }
}
