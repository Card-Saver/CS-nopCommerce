using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Nop.Web.Framework.Mvc.Routing;

namespace Nop.Plugin.Payments.P3Gateway.Infrastructure
{
    public class RouteProvider : IRouteProvider
    {
        /// <summary>
        ///     Gets a priority of route provider
        /// </summary>
        public int Priority => -1;

        /// <summary>
        ///     Register routes
        /// </summary>
        /// <param name="endpointRouteBuilder">Route builder</param>
        public void RegisterRoutes(IEndpointRouteBuilder endpointRouteBuilder)
        {
            //PDT
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.P3Gateway.ProcessHandler",
                "Plugins/P3Gateway/ProcessHandler",
                new {controller = "P3Process", action = "ProcessHandler"});

            //PDT
            endpointRouteBuilder.MapControllerRoute("Plugin.Payments.P3Gateway.ReturnHandler",
                "Plugins/P3Gateway/ReturnHandler",
                new {controller = "P3Return", action = "ReturnHandler"});
            //
            // //IPN
            // endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PayPalStandard.IPNHandler", "Plugins/PaymentPayPalStandard/IPNHandler",
            //      new { controller = "PaymentPayPalStandardIpn", action = "IPNHandler" });
            //
            // //Cancel
            // endpointRouteBuilder.MapControllerRoute("Plugin.Payments.PayPalStandard.CancelOrder", "Plugins/PaymentPayPalStandard/CancelOrder",
            //      new { controller = "PaymentPayPalStandard", action = "CancelOrder" });
        }
    }
}