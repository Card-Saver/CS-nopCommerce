using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LinqToDB.Common;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Services.Common;
using Nop.Services.Directory;
using Nop.Services.Orders;
using Nop.Services.Configuration;

namespace Nop.Plugin.Payments.P3Gateway.Controllers
{
    public class P3ProcessController
    {
        #region Ctor

        public P3ProcessController(
            IAddressService addressService,
            ICountryService countryService,
            ICurrencyService currencyService,
            IStateProvinceService stateProvinceService,
            CurrencySettings currencySettings,
            IWebHelper webHelper,
            IOrderService orderService,
            ISettingService settingService,
            IStoreContext storeContext,
            IWorkContext workContext,
            Gateway gateway
        )
        {
            _addressService = addressService;
            _countryService = countryService;
            _currencyService = currencyService;
            _stateProvinceService = stateProvinceService;
            _currencySettings = currencySettings;
            _webHelper = webHelper;
            _orderService = orderService;
            _settingService = settingService;
            _storeContext = storeContext;
            _workContext = workContext;
            _gateway = gateway;
        }

        #endregion

        #region Fields

        private readonly IAddressService _addressService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWebHelper _webHelper;
        private readonly IOrderService _orderService;
        private readonly ISettingService _settingService;
        private readonly IStoreContext _storeContext;
        private readonly IWorkContext _workContext;
        private readonly Gateway _gateway;

        #endregion

        #region Methods

        public async Task<ContentResult> ProcessHandler()
        {
            var orderGuid = _webHelper.QueryString<string>("orderGuid");

            Order order = await _orderService.GetOrderByGuidAsync(new Guid(orderGuid));

            var parammeters = await GetParameters(order);

            return new ContentResult
            {
                ContentType = "text/html",
                Content = _gateway.HostedRequest(parammeters, new Dictionary<string, string>())
            };
        }

        private async Task<Dictionary<string, object>> GetParameters(Order order)
        {
            Console.WriteLine(order.ShippingAddressId.ToString(), order.BillingAddressId.ToString());

            //choosing correct order address
            var orderAddress = await _addressService.GetAddressByIdAsync(
                (order.PickupInStore ? order.PickupAddressId : order.BillingAddressId) ?? 0
            );

            string billingAddressFormated = orderAddress.Address1;

            if (!Tools.IsNullOrEmpty(orderAddress.Address2))
                billingAddressFormated += "\n" + orderAddress.Address2;

            billingAddressFormated += "\n" + orderAddress.City;
            if (orderAddress.StateProvinceId != null)
            {
                var state = (await _stateProvinceService.GetStateProvinceByAddressAsync(orderAddress))?.Abbreviation;

                billingAddressFormated += "\n" + state;
            }

            var country = await _countryService.GetCountryByAddressAsync(orderAddress);
            if (country != null) billingAddressFormated += "\n" + country.Name;

            var storeScope = _storeContext.GetActiveStoreScopeConfigurationAsync();
            var paymentSettings = _settingService.LoadSettingAsync<P3GatewaySettings>(storeScope.Result).Result;

            //round order total
            var roundedOrderTotal = (int) (Math.Round(order.OrderTotal, paymentSettings.CurrencyExponent) * (int) Math.Pow(10, paymentSettings.CurrencyExponent));

            var currency = await _workContext.GetWorkingCurrencyAsync();

            var parameters = new Dictionary<string, object>
            {
                ["action"] = "SALE",
                ["currencyCode"] = currency.CurrencyCode,
                ["orderRef"] = (string) order.CustomOrderNumber,
                ["transactionUnique"] = order.OrderGuid.ToString(),
                ["countryCode"] = country?.ThreeLetterIsoCode,
                ["type"] = "1",
                ["customerEmail"] = orderAddress?.Email,
                ["customerPostcode"] = orderAddress?.ZipPostalCode,
                ["customerName"] = orderAddress?.FirstName + " " + orderAddress?.LastName,
                ["amount"] = roundedOrderTotal.ToString(),
                ["customerAddress"] = billingAddressFormated,
                ["redirectURL"] = $"{_webHelper.GetStoreLocation()}Plugins/P3Gateway/ReturnHandler"
            };

            return parameters;
        }

        #endregion
    }
}