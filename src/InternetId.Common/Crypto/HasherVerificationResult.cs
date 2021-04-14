namespace InternetId.Common.Crypto
{
    public enum HasherVerificationResult
    {
        /// <summary>
        /// The secret is incorrect.
        /// </summary>
        Invalid,

        /// <summary>
        /// The secret is correct, but will be active some time in the future.
        /// </summary>
        Inactive,

        /// <summary>
        /// The secret is correct, but expired some time in the past.
        /// </summary>
        Expired,

        /// <summary>
        /// The secret is correct and is active.
        /// </summary>
        Valid,
    }
}