using Nop.Core.Configuration;

namespace Nop.Plugin.Payments.CardstreamHosted
{
    public class CardstreamHostedPaymentSettings : ISettings
    {

        public string MerchantID { get; set; }
        public string HashKey { get; set; }
        public string CurrencyCode { get; set; }
        public string CountryCode { get; set; }
        public bool FormResponsive { get; set; }

    }
}
