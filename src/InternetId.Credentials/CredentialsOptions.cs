using System.Collections.Generic;

namespace InternetId.Credentials
{
    public class CredentialsOptions
    {
        public Dictionary<string, PurposeOptions> Purposes { get; set; } = new Dictionary<string, PurposeOptions>();

        public class PurposeOptions
        {
            /// <summary>
            /// Defaults to <c>false</c>.
            /// </summary>
            public bool Enabled { get; set; }
            /// <summary>
            /// Defaults to 10 minutes.
            /// </summary>
            public float LifespanDays { get; set; } = 10f / 60f / 24f;
            /// <summary>
            /// Defaults to 10 minutes.
            /// </summary>
            public float LockMinutes { get; set; } = 10;
            /// <summary>
            /// Defaults to 10.
            /// </summary>
            public int AttemptsPerLockout { get; set; } = 10;
        }
    }
}
