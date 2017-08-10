using Nop.Web.Framework;
using Nop.Web.Framework.Mvc;

namespace Nop.Plugin.Payments.CardstreamHosted.Models
{
    public class ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CardstreamHosted.Fields.MerchantID")]
        public string MerchantID { get; set; }
        public bool MerchantID_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CardstreamHosted.Fields.HashKey")]
        public string HashKey { get; set; }
        public bool HashKey_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CardstreamHosted.Fields.CurrencyCode")]
        public string CurrencyCode { get; set; }
        public bool CurrencyCode_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CardstreamHosted.Fields.CountryCode")]
        public string CountryCode { get; set; }
        public bool CountryCode_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.CardstreamHosted.Fields.FormResponsive")]
        public bool FormResponsive { get; set; }
        public bool FormResponsive_OverrideForStore { get; set; }

    }
}