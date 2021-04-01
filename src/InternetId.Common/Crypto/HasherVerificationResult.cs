namespace InternetId.Common.Crypto
{
    public enum HasherVerificationResult
    {
        /// <summary>
        /// The password is incorrect.
        /// </summary>
        Invalid,

        /// <summary>
        /// The password is correct, but will be active some time in the future.
        /// </summary>
        Inactive,

        /// <summary>
        /// The password is correct, but expired some time in the past.
        /// </summary>
        Expired,

        /// <summary>
        /// The password is correct and is active.
        /// </summary>
        Valid,
    }
}