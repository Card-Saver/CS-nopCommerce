using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.P3Gateway
{
    /// <summary>
    ///     Represents settings of manual payment plugin
    /// </summary>
    public class P3GatewaySettings : ISettings
    {
        /// <summary>
        ///     Gets or sets merchant id
        /// </summary>
        public string MerchantId { get; set; }

        /// <summary>
        ///     Gets or sets secret key
        /// </summary>
        public string Secret { get; set; }

        /// <summary>
        ///     Gets or sets payment integration mode
        /// </summary>
        public IntegrationMode IntegrationMode { get; set; }

        /// <summary>
        ///     Gets or sets secret key
        /// </summary>
        public string GatewayUrl { get; set; }

        /// <summary>
        ///     Gets or sets secret key
        /// </summary>
        public int CurrencyExponent { get; set; }

        /// <summary>
        ///     Gets or sets debug mode
        /// </summary>
        public bool DebugMode { get; set; }
    }
}