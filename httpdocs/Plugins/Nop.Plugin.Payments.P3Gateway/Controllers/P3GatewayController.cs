using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Plugin.Payments.P3Gateway.Models;
using Nop.Services;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Services.Security;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc.Filters;

namespace Nop.Plugin.Payments.P3Gateway.Controllers
{
    public class P3GatewayController : BasePaymentController
    {
        #region Ctor

        public P3GatewayController(ILocalizationService localizationService,
            INotificationService notificationService,
            IPermissionService permissionService,
            ISettingService settingService,
            IAddressService addressService,
            ICountryService countryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IStateProvinceService stateProvinceService,
            CurrencySettings currencySettings,
            ILogger logger,
            IWebHelper webHelper,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            IStoreContext storeContext,
            Gateway gateway
        ) {
            _localizationService = localizationService;
            _notificationService = notificationService;
            _permissionService = permissionService;
            _settingService = settingService;
            _addressService = addressService;
            _countryService = countryService;
            _currencyService = currencyService;
            _customerService = customerService;
            _stateProvinceService = stateProvinceService;
            _currencySettings = currencySettings;
            _logger = logger;
            _webHelper = webHelper;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _storeContext = storeContext;
            _gateway = gateway;
        }

        #endregion

        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly INotificationService _notificationService;
        private readonly IPermissionService _permissionService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IAddressService _addressService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly ILogger _logger;
        private readonly IOrderService _orderService;
        private readonly IWebHelper _webHelper;
        private readonly Gateway _gateway;
        private readonly IOrderProcessingService _orderProcessingService;

        #endregion

        #region Methods

        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Configure()
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var manualPaymentSettings = await _settingService.LoadSettingAsync<P3GatewaySettings>(storeScope);
            var model = new ConfigurationModel()
            {
                MerchantId = manualPaymentSettings.MerchantId,
                Secret = manualPaymentSettings.Secret,
                IntegrationModeId = Convert.ToInt32(manualPaymentSettings.IntegrationMode),
                IntegrationModeValues = await manualPaymentSettings.IntegrationMode.ToSelectListAsync(),
                GatewayUrl = manualPaymentSettings.GatewayUrl,
                CurrencyExponent = manualPaymentSettings.CurrencyExponent,
                DebugMode = manualPaymentSettings.DebugMode,
                ActiveStoreScopeConfiguration = storeScope
            };


            return View("~/Plugins/Payments.P3Gateway/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AuthorizeAdmin]
        [Area(AreaNames.Admin)]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> Configure(ConfigurationModel model)
        {
            if (!await _permissionService.AuthorizeAsync(StandardPermissionProvider.ManagePaymentMethods))
                return AccessDeniedView();

            if (!ModelState.IsValid)
                return await Configure();

            //load settings for a chosen store scope
            var storeScope = await _storeContext.GetActiveStoreScopeConfigurationAsync();
            var manualPaymentSettings = await _settingService.LoadSettingAsync<P3GatewaySettings>(storeScope);

            //save settings
            manualPaymentSettings.MerchantId = model.MerchantId;
            manualPaymentSettings.Secret = model.Secret;
            manualPaymentSettings.IntegrationMode = (IntegrationMode) model.IntegrationModeId;
            manualPaymentSettings.GatewayUrl = model.GatewayUrl;
            manualPaymentSettings.CurrencyExponent = model.CurrencyExponent;
            manualPaymentSettings.DebugMode = model.DebugMode;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */

            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.MerchantId,
                model.MerchantId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.Secret,
                model.Secret_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.IntegrationMode,
                model.IntegrationModeId_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.GatewayUrl,
                model.GatewayUrl_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.DebugMode,
                model.DebugMode_OverrideForStore, storeScope, false);
            await _settingService.SaveSettingOverridablePerStoreAsync(manualPaymentSettings, x => x.CurrencyExponent,
                model.CurrencyExponent_OverrideForStore, storeScope, false);
            //
            //now clear settings cache
            await _settingService.ClearCacheAsync();

            _notificationService.SuccessNotification(
                await _localizationService.GetResourceAsync("Admin.Plugins.Saved"));

            return await Configure();
        }

        #endregion
    }
}