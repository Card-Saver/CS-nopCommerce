using System.Web.Mvc;
using System.Web.Routing;
using Nop.Web.Framework.Mvc.Routes;

namespace Nop.Plugin.Payments.CardstreamHosted
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            //Callback/Redirect
            routes.MapRoute("Plugin.Payments.CardstreamHosted.IPNHandler",
                 "Plugins/PaymentCardstreamHosted/IPNHandler",
                 new { controller = "PaymentCardstreamHosted", action = "IPNHandler" },
                 new[] { "Nop.Plugin.Payments.CardstreamHosted.Controllers" }
            );

            routes.MapRoute("Plugin.Payments.CardstreamHosted.CheckoutFailed",
                "checkout/failed/{orderId}",
                new { controller = "PaymentCardstreamHosted", action = "Failed", orderId = UrlParameter.Optional },
                new { orderId = @"\d+" },
                new[] { "Nop.Plugin.Payments.CardstreamHosted.Controllers" }
                );

        }
        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
