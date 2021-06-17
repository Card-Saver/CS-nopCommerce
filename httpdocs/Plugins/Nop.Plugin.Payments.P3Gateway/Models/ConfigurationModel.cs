using Microsoft.AspNetCore.Mvc.Rendering;
using Nop.Web.Framework.Mvc.ModelBinding;
using Nop.Web.Framework.Models;

namespace Nop.Plugin.Payments.P3Gateway.Models
{
    public record ConfigurationModel : BaseNopModel
    {
        public int ActiveStoreScopeConfiguration { get; set; }

        [NopResourceDisplayName("Plugins.Payments.P3Gateway.Fields.MerchantId")]
        public string MerchantId { get; set; }

        public bool MerchantId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.P3Gateway.Fields.Secret")]
        public string Secret { get; set; }

        public bool Secret_OverrideForStore { get; set; }

        public int IntegrationModeId { get; set; }

        [NopResourceDisplayName("Plugins.Payments.P3Gateway.Fields.IntegrationMode")]
        public SelectList IntegrationModeValues { get; set; }

        public bool IntegrationModeId_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.P3Gateway.Fields.GatewayUrl")]
        public string GatewayUrl { get; set; }

        public bool GatewayUrl_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.P3Gateway.Fields.CurrencyExponent")]
        public int CurrencyExponent { get; set; }

        public bool CurrencyExponent_OverrideForStore { get; set; }

        [NopResourceDisplayName("Plugins.Payments.P3Gateway.Fields.DebugMode")]
        public bool DebugMode { get; set; }

        public bool DebugMode_OverrideForStore { get; set; }
    }
}