namespace Nop.Plugin.Payments.P3Gateway
{
    /// <summary>
    ///     Represents manual payment processor transaction mode
    /// </summary>
    public enum IntegrationMode
    {
        /// <summary>
        ///     Pending
        /// </summary>
        Hosted = 0

        //
        // /// <summary>
        // /// Authorize
        // /// </summary>
        // Authorize = 1,
        //
        // /// <summary>
        // /// Authorize and capture
        // /// </summary>
        // AuthorizeAndCapture= 2
    }
}