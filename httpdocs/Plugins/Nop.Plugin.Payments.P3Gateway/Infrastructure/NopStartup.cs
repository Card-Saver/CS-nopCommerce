using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nop.Core.Infrastructure;
using Nop.Services.Configuration;
using Nop.Core;


namespace Nop.Plugin.Payments.P3Gateway.Infrastructure
{
    /// <summary>
    ///     Represents object for the configuring services on application startup
    /// </summary>
    public class NopStartup : INopStartup
    {
        /// <summary>
        ///     Gets order of this startup configuration implementation
        /// </summary>
        public int Order => 101;

        /// <summary>
        ///     Add and configure any of the middleware
        /// </summary>
        /// <param name="services">Collection of service descriptors</param>
        /// <param name="configuration">Configuration of the application</param>
        public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<Gateway>(ctx =>
            {
                var _storeContext = ctx.GetService<IStoreContext>();
                var _settingService = ctx.GetService<ISettingService>();

                var storeScope = _storeContext.GetActiveStoreScopeConfigurationAsync();
                var manualPaymentSettings = _settingService.LoadSettingAsync<P3GatewaySettings>(storeScope.Result).Result;

                return new Gateway(manualPaymentSettings.MerchantId, manualPaymentSettings.Secret);
            });

            // services.Add(new ServiceDescriptor(typeof(Gateway), new Gateway()));
        }

        /// <summary>
        ///     Configure the using of added middleware
        /// </summary>
        /// <param name="application">Builder for configuring an application's request pipeline</param>
        public void Configure(IApplicationBuilder application)
        {
        }
    }
}