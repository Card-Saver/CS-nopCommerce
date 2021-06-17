using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Plugin.Payments.P3Gateway.Models;
using Nop.Plugin.Payments.P3Gateway.Validators;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Payments;
using Nop.Services.Plugins;

namespace Nop.Plugin.Payments.P3Gateway
{
    public class P3GatewayProcessor : BasePlugin, IPaymentMethod
    {
        #region Ctor

        public P3GatewayProcessor(ILocalizationService localizationService,
            IPaymentService paymentService,
            IHttpContextAccessor httpContextAccessor,
            ISettingService settingService,
            IWebHelper webHelper,
            IAddressService addressService,
            ICountryService countryService,
            ICurrencyService currencyService,
            ICustomerService customerService,
            IStateProvinceService stateProvinceService,
            CurrencySettings currencySettings,
            ILogger logger,
            P3GatewaySettings paymentSettings)
        {
            _currencySettings = currencySettings;
            _localizationService = localizationService;
            _paymentService = paymentService;
            _settingService = settingService;
            _webHelper = webHelper;
            _paymentSettings = paymentSettings;
            _httpContextAccessor = httpContextAccessor;
            _addressService = addressService;
            _stateProvinceService = stateProvinceService;
            _countryService = countryService;
            _currencyService = currencyService;
            _customerService = customerService;
            _logger = logger;
        }

        #endregion

        #region Fields

        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly ISettingService _settingService;
        private readonly IWebHelper _webHelper;
        private readonly P3GatewaySettings _paymentSettings;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IAddressService _addressService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ICountryService _countryService;
        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly ILogger _logger;

        #endregion

        #region Methods

        /// <summary>
        ///     Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                AllowStoringCreditCardNumber = false,
                NewPaymentStatus = PaymentStatus.Pending
            };

            return Task.FromResult(result);
        }

        /// <summary>
        ///     Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public Task PostProcessPaymentAsync(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            return Task.Run(() => _httpContextAccessor.HttpContext.Response.Redirect(
                QueryHelpers.AddQueryString(
                    $"{_webHelper.GetStoreLocation()}Plugins/P3Gateway/ProcessHandler",
                    new Dictionary<string, string>
                    {
                        ["orderGuid"] = postProcessPaymentRequest.Order.OrderGuid.ToString()
                    }
                )
            ));
        }

        /// <summary>
        ///     Returns a value indicating whether payment method should be hidden during checkout
        /// </summary>
        /// <param name="cart">Shopping cart</param>
        /// <returns>true - hide; false - display.</returns>
        public Task<bool> HidePaymentMethodAsync(IList<ShoppingCartItem> cart)
        {
            //you can put any logic here
            //for example, hide this payment method if all products in the cart are downloadable
            //or hide this payment method if current customer is from certain country
            return Task.FromResult(false);
        }

        /// <summary>
        ///     Gets additional handling fee
        /// </summary>
        /// <returns>Additional handling fee</returns>
        public Task<decimal> GetAdditionalHandlingFeeAsync(IList<ShoppingCartItem> cart)
        {
            return Task.FromResult(decimal.Zero);
        }

        /// <summary>
        ///     Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public Task<CapturePaymentResult> CaptureAsync(CapturePaymentRequest capturePaymentRequest)
        {
            return Task.FromResult(new CapturePaymentResult {Errors = new[] {"Capture method not supported"}});
        }

        /// <summary>
        ///     Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<RefundPaymentResult> RefundAsync(RefundPaymentRequest refundPaymentRequest)
        {
            return Task.FromResult(new RefundPaymentResult {Errors = new[] {"Refund method not supported"}});
        }

        /// <summary>
        ///     Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<VoidPaymentResult> VoidAsync(VoidPaymentRequest voidPaymentRequest)
        {
            return Task.FromResult(new VoidPaymentResult {Errors = new[] {"Void method not supported"}});
        }

        /// <summary>
        ///     Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public Task<ProcessPaymentResult> ProcessRecurringPaymentAsync(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult
            {
                AllowStoringCreditCardNumber = true
            };

            result.NewPaymentStatus = PaymentStatus.Pending;

            return Task.FromResult(result);
        }

        /// <summary>
        ///     Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public Task<CancelRecurringPaymentResult> CancelRecurringPaymentAsync(
            CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            //always success
            return Task.FromResult(new CancelRecurringPaymentResult());
        }

        /// <summary>
        ///     Gets a value indicating whether customers can complete a payment after order is placed but not completed (for
        ///     redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public Task<bool> CanRePostProcessPaymentAsync(Order order)
        {
            if (order == null)
                throw new ArgumentNullException(nameof(order));

            //it's not a redirection payment method. So we always return false
            return Task.FromResult(false);
        }

        /// <summary>
        ///     Validate payment form
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>List of validating errors</returns>
        public Task<IList<string>> ValidatePaymentFormAsync(IFormCollection form)
        {
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_localizationService);
            var model = new PaymentInfoModel
            {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };
            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                warnings.AddRange(validationResult.Errors.Select(error => error.ErrorMessage));

            return Task.FromResult<IList<string>>(warnings);
        }

        /// <summary>
        ///     Get payment information
        /// </summary>
        /// <param name="form">The parsed form values</param>
        /// <returns>Payment info holder</returns>
        public Task<ProcessPaymentRequest> GetPaymentInfoAsync(IFormCollection form)
        {
            if (_paymentSettings.IntegrationMode == IntegrationMode.Hosted)
                return Task.FromResult(new ProcessPaymentRequest());

            return Task.FromResult(new ProcessPaymentRequest
            {
                CreditCardType = form["CreditCardType"],
                CreditCardName = form["CardholderName"],
                CreditCardNumber = form["CardNumber"],
                CreditCardExpireMonth = int.Parse(form["ExpireMonth"]),
                CreditCardExpireYear = int.Parse(form["ExpireYear"]),
                CreditCardCvv2 = form["CardCode"]
            });
        }

        /// <summary>
        ///     Gets a configuration page URL
        /// </summary>
        public override string GetConfigurationPageUrl()
        {
            return $"{_webHelper.GetStoreLocation()}Admin/P3Gateway/Configure";
        }

        /// <summary>
        ///     Gets a name of a view component for displaying plugin in public store ("payment info" checkout step)
        /// </summary>
        /// <returns>View component name</returns>
        public string GetPublicViewComponentName()
        {
            return "P3Gateway";
        }

        /// <summary>
        ///     Install the plugin
        /// </summary>
        public override async Task InstallAsync()
        {
            //settings
            var settings = new P3GatewaySettings
            {
                IntegrationMode = IntegrationMode.Hosted,
                DebugMode = true
            };
            await _settingService.SaveSettingAsync(settings);

            //locales
            await _localizationService.AddLocaleResourceAsync(new Dictionary<string, string>
            {
                ["Plugins.Payments.P3Gateway.Instructions"] = "This payment method is a third-party processor. Please fill in the below fields to be able to make payments.",
                ["Plugins.Payments.P3Gateway.PaymentMethodDescription"] = "Pay via P3 Gateway",
                ["Plugins.Payments.P3Gateway.Fields.MerchantId"] = "Merchant ID",
                ["Plugins.Payments.P3Gateway.Fields.Secret"] = "Secret",
                ["Plugins.Payments.P3Gateway.Fields.IntegrationMode"] = "Integration Mode",
                ["Plugins.Payments.P3Gateway.Fields.GatewayUrl"] = "Gateway Url",
                ["Plugins.Payments.P3Gateway.Fields.CurrencyExponent"] = "Currency Exponent",
                ["Plugins.Payments.P3Gateway.Fields.DebugMode"] = "Debug"
            });

            await base.InstallAsync();
        }

        /// <summary>
        ///     Uninstall the plugin
        /// </summary>
        public override async Task UninstallAsync()
        {
            //settings
            await _settingService.DeleteSettingAsync<P3GatewaySettings>();

            //locales
            await _localizationService.DeleteLocaleResourcesAsync("Plugins.Payments.P3Gateway");

            await base.UninstallAsync();
        }

        /// <summary>
        ///     Gets a payment method description that will be displayed on checkout pages in the public store
        /// </summary>
        /// <remarks>
        ///     return description of this payment method to be display on "payment method" checkout step. good practice is to make
        ///     it localizable
        ///     for example, for a redirection payment method, description may be like this: "You will be redirected to PayPal site
        ///     to complete the payment"
        /// </remarks>
        public async Task<string> GetPaymentMethodDescriptionAsync()
        {
            return await _localizationService.GetResourceAsync("Plugins.Payments.P3Gateway.PaymentMethodDescription");
        }

        #endregion

        #region Properties

        /// <summary>
        ///     Gets a value indicating whether capture is supported
        /// </summary>
        public bool SupportCapture => false;

        /// <summary>
        ///     Gets a value indicating whether partial refund is supported
        /// </summary>
        public bool SupportPartiallyRefund => false;

        /// <summary>
        ///     Gets a value indicating whether refund is supported
        /// </summary>
        public bool SupportRefund => false;

        /// <summary>
        ///     Gets a value indicating whether void is supported
        /// </summary>
        public bool SupportVoid => false;

        /// <summary>
        ///     Gets a recurring payment type of payment method
        /// </summary>
        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.Manual;

        /// <summary>
        ///     Gets a payment method type
        /// </summary>
        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        /// <summary>
        ///     Gets a value indicating whether we should display a payment information page for this plugin
        /// </summary>
        public bool SkipPaymentInfo => true;

        #endregion
    }
}