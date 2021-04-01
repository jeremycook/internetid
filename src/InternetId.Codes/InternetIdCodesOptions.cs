using System;
using System.Collections.Generic;

namespace InternetId.Common.Codes
{
    public class InternetIdCodesOptions
    {
        public Dictionary<string, PurposeOptions> Purposes { get; set; } = new Dictionary<string, PurposeOptions>();

        public class PurposeOptions
        {
            public static PurposeOptions Fallback { get; } = new PurposeOptions();

            public float LifespanMinutes { get; set; } = 10;
            public float LockoutMinutes { get; set; } = 10;
            public int AttemptsPerLockout { get; set; } = 10;
        }
    }
}
