using System;
using System.Collections.Generic;using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Web;
using Nop.Core;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Core.Domain.Shipping;
using Nop.Core.Plugins;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Localization;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Tax;
using System.Web.Routing;
using Nop.Plugin.Payments.CardstreamHosted.Controllers;
using System.Text.RegularExpressions;
using Nop.Web.Framework;
using System.Security.Cryptography;
using System.Collections.Specialized;

namespace Nop.Plugin.Payments.CardstreamHosted
{
    class CardstreamHostedPaymentProcessor : BasePlugin, IPaymentMethod
    {

        const string GATEWAY_URL = "https://gateway.cardstream.com/hosted/";

        private readonly ILocalizationService _localizationService;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly ISettingService _settingService;
        private readonly CardstreamHostedPaymentSettings _cardstreamPaymentSettings;
        private readonly IWebHelper _webHelper;

        public CardstreamHostedPaymentProcessor(
            ILocalizationService localizationService,
            IOrderTotalCalculationService orderTotalCalculationService,
            ISettingService settingService,
            CardstreamHostedPaymentSettings cardstreamPaymentSettings,
            IWebHelper webHelper
        ) {
            this._localizationService = localizationService;
            this._orderTotalCalculationService = orderTotalCalculationService;
            this._settingService = settingService;
            this._cardstreamPaymentSettings = cardstreamPaymentSettings;
            this._webHelper = webHelper;
        }


        public PaymentMethodType PaymentMethodType => PaymentMethodType.Redirection;

        public bool SkipPaymentInfo => true;

        public string PaymentMethodDescription => "Pay quickly and securely with Credit or Debit card with Cardstream";

        public bool CanRePostProcessPayment(Order order)
        {
            return true;
        }


        public void GetConfigurationRoute(out string actionName, out string controllerName, out System.Web.Routing.RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "PaymentCardstreamHosted";
            routeValues = new RouteValueDictionary { { "Namespaces", "Nop.Plugin.Payments.CardstreamHosted.Controllers" }, { "area", null } };
        }

        public void GetPaymentInfoRoute(out string actionName, out string controllerName, out System.Web.Routing.RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "PaymentCardstreamHosted";
            routeValues = new RouteValueDictionary()
            {
                { "Namespaces", "Nop.Plugin.Payments.CardstreamHosted.Controllers" },
                { "area", null }
            };
        }

        /// <summary>
        /// Install the plugin
        /// </summary>
        public override void Install()
        {
            // Settings
            var settings = new CardstreamHostedPaymentSettings
            {
                MerchantID = "100001",
                HashKey = "Circle4Take40Idea"
            };
            _settingService.SaveSetting(settings);

            // Locales
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.MerchantID", "Merchant ID");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.HashKey", "Signature Key from MMS");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.CurrencyCode", "Currency Code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.CountryCode", "Country Code");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.FormResponsive", "Responsive Hosted Form?");
            this.AddOrUpdatePluginLocaleResource("Plugins.Payments.CardstreamHosted.RetryPayment", "Retry Payment");
            this.AddOrUpdatePluginLocaleResource("Checkout.Sorry", "Sorry");
            this.AddOrUpdatePluginLocaleResource("Checkout.YourPaymentHasFailed", "There has been a problem processing your payment, no money has been taken from your account. You can re-attempt to pay at any time from the order page below OR click the button to try again now.");
            base.Install();
        }

        public override void Uninstall()
        {
            // Remove all settings that may be stored by us
            _settingService.DeleteSetting<CardstreamHostedPaymentSettings>();

            // Remove all locales that were used by us
            this.DeletePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.MerchantID");
            this.DeletePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.HashKey");
            this.DeletePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.CurrencyCode");
            this.DeletePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.CountryCode");
            this.DeletePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.FormResponsive");
            this.DeletePluginLocaleResource("Plugins.Payments.CardstreamHosted.Fields.RetryPayment");

            // Uninstall from the base
            base.Uninstall();
        }


        public bool HidePaymentMethod(IList<ShoppingCartItem> cart)
        {
            return false;
        }

        /*
         * This method is always invoked right before a customer places an order which we can
         * use to set the current order payment as pending before redirecting to the gateway
         * during the new order process
        */
        public ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Pending;
            return result;
        }

        private string GetUniqID()
        {
            var ts = (DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0));
            double t = ts.TotalMilliseconds / 1000;

            int a = (int)Math.Floor(t);
            int b = (int)((t - Math.Floor(t)) * 1000000);

            return a.ToString("x8") + b.ToString("x5");
        }

        /*
         * Event handler for when the customer has placed a new order. We will use this to
         * redirect the user to the payment gateway for payment before redirect back to 
         * NopCommerce to update the customers order status based on the result.
         */
        public void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {

            var post = new RemotePost
            {
                FormName = "CardstreamReq",
                Url = GATEWAY_URL,
                Method = "POST"
            };

            NameValueCollection sortedDictionary = new NameValueCollection();

            sortedDictionary.Add("merchantID", _cardstreamPaymentSettings.MerchantID);
            sortedDictionary.Add("action", "SALE");
            sortedDictionary.Add("type", "1");
            sortedDictionary.Add("transactionUnique", GetUniqID());
            sortedDictionary.Add("currencyCode", _cardstreamPaymentSettings.CurrencyCode);
            sortedDictionary.Add("countryCode", _cardstreamPaymentSettings.CountryCode);
            sortedDictionary.Add("formResponsive", (_cardstreamPaymentSettings.FormResponsive ? "Y" : "N"));
            sortedDictionary.Add("amount", (Math.Floor(postProcessPaymentRequest.Order.OrderTotal * 100)).ToString());
            sortedDictionary.Add("orderRef",postProcessPaymentRequest.Order.OrderGuid.ToString());
            sortedDictionary.Add("customerName", postProcessPaymentRequest.Order.Customer.BillingAddress.FirstName + " " + postProcessPaymentRequest.Order.Customer.BillingAddress.LastName);
            sortedDictionary.Add("customerEmail", postProcessPaymentRequest.Order.Customer.Email);
           
            var secondline = (postProcessPaymentRequest.Order.BillingAddress.Address2 != null && postProcessPaymentRequest.Order.BillingAddress.Address2 != "") ? (postProcessPaymentRequest.Order.BillingAddress.Address2 + ", \n") : "";
            sortedDictionary.Add("customerAddress", postProcessPaymentRequest.Order.BillingAddress.Address1 + ", \n" + secondline + postProcessPaymentRequest.Order.BillingAddress.City);
            sortedDictionary.Add("customerPostcode", postProcessPaymentRequest.Order.BillingAddress.ZipPostalCode);
            sortedDictionary.Add("customerPhone", postProcessPaymentRequest.Order.BillingAddress.PhoneNumber);
            sortedDictionary.Add("redirectURL", _webHelper.GetStoreLocation(false) + "Plugins/PaymentCardstreamHosted/IPNHandler");
            sortedDictionary.Add("callbackURL", _webHelper.GetStoreLocation(false) + "Plugins/PaymentCardstreamHosted/IPNHandler");

            // Build request String
            NameValueCollection nvc = new NameValueCollection();
            foreach (string key in sortedDictionary.AllKeys)
            {
                post.Add(key, sortedDictionary[key]);
            }
            post.Add("signature", signCollection(sortedDictionary, _cardstreamPaymentSettings.HashKey));
            post.Post();
            
        }

        public static string urlEncode(string value)
        {
            String newString = UpperCaseUrlEncode(value);
            newString = newString.Replace("!", "%21");
            newString = newString.Replace("*", "%2A");
            newString = newString.Replace("(", "%28");
            newString = newString.Replace(")", "%29");
            return newString;
        }

        public static string UpperCaseUrlEncode(string value)
        {
            char[] charArray = HttpUtility.UrlEncode(value).ToCharArray();
            for (int i = 0; i < charArray.Length - 2; i++)
            {
                if (charArray[i] == '%')
                {
                    charArray[i + 1] = char.ToUpper(charArray[i + 1]);
                    charArray[i + 2] = char.ToUpper(charArray[i + 2]);
                }
            }

            return new string(charArray);
        }



        public static string signCollection(NameValueCollection nvc, string hashkey)
        {
            int count = 0;
            string reqString = "";
            foreach (string key in nvc.AllKeys.OrderBy(k => k))
            {
                var value = nvc[key];
                if (count > 0)
                {
                    reqString += "&";
                }
                reqString += key + "=" + urlEncode(value);
                count++;
            }

            // With hosted integration we pass the parameters used to sign the request
            String Hashed = BitConverter.ToString(((SHA512)new SHA512Managed()).ComputeHash(Encoding.UTF8.GetBytes(
                Regex.Replace(reqString + hashkey, @"%0D%0A|%0A%0D|%0D", "%0A")
            ))).Replace("-", "").ToLower();

            return Hashed;
        }


        public bool SupportCapture => false;

        public bool SupportPartiallyRefund => false;

        public bool SupportRefund => false;

        public bool SupportVoid => false;

        public RecurringPaymentType RecurringPaymentType => RecurringPaymentType.NotSupported;

        public decimal GetAdditionalHandlingFee(IList<ShoppingCartItem> cart)
        {
            return 0;
        }

        public Type GetControllerType()
        {
            return typeof(PaymentCardstreamHostedController);
        }

        /// <summary>
        /// Captures payment
        /// </summary>
        /// <param name="capturePaymentRequest">Capture payment request</param>
        /// <returns>Capture payment result</returns>
        public CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new CapturePaymentResult();
            result.AddError("Capture method not supported");
            return result;
        }

        /// <summary>
        /// Refunds a payment
        /// </summary>
        /// <param name="refundPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public RefundPaymentResult Refund(RefundPaymentRequest refundPaymentRequest)
        {
            var result = new RefundPaymentResult();
            result.AddError("Refund method not supported");
            return result;
        }

        /// <summary>
        /// Voids a payment
        /// </summary>
        /// <param name="voidPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public VoidPaymentResult Void(VoidPaymentRequest voidPaymentRequest)
        {
            var result = new VoidPaymentResult();
            result.AddError("Void method not supported");
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

        /// <summary>
        /// Cancels a recurring payment
        /// </summary>
        /// <param name="cancelPaymentRequest">Request</param>
        /// <returns>Result</returns>
        public CancelRecurringPaymentResult CancelRecurringPayment(CancelRecurringPaymentRequest cancelPaymentRequest)
        {
            var result = new CancelRecurringPaymentResult();
            result.AddError("Recurring payment not supported");
            return result;
        }

    }
}
