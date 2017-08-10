using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Payments;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Orders;
using Nop.Services.Payments;
using Nop.Services.Stores;
using Nop.Web.Framework.Controllers;
using Nop.Plugin.Payments.CardstreamHosted.Models;
using Nop.Web.Models.Checkout;

namespace Nop.Plugin.Payments.CardstreamHosted.Controllers
{
    public class PaymentCardstreamHostedController : BasePaymentController
    {
        private readonly IWorkContext _workContext;
        private readonly IStoreService _storeService;
        private readonly ISettingService _settingService;
        private readonly ILocalizationService _localizationService;
        private readonly IPaymentService _paymentService;
        private readonly PaymentSettings _paymentSettings;
        private readonly CardstreamHostedPaymentSettings _cardstreamHostedPaymentSettings;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;


        public PaymentCardstreamHostedController(IWorkContext workContext,
            IStoreService storeService,
            ISettingService settingService,
            ILocalizationService localizationService,
            IPaymentService paymentService,
            PaymentSettings paymentSettings,
            CardstreamHostedPaymentSettings cardstreamHostedPaymentSettings,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService
            )
        {
            this._workContext = workContext;
            this._storeService = storeService;
            this._settingService = settingService;
            this._localizationService = localizationService;
            this._paymentService = paymentService;
            this._paymentSettings = paymentSettings;
            this._cardstreamHostedPaymentSettings = cardstreamHostedPaymentSettings;
            this._orderService = orderService;
            this._orderProcessingService = orderProcessingService;
        }

        [NonAction]
        public override IList<string> ValidatePaymentForm(FormCollection form)
        {
            var warnings = new List<string>();
            return warnings;
        }

        [NonAction]
        public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
        {
            var paymentInfo = new ProcessPaymentRequest();
            return paymentInfo;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {

            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var cardstreamHostedPaymentSettings = _settingService.LoadSetting<CardstreamHostedPaymentSettings>(storeScope);

            var model = new ConfigurationModel();
            model.MerchantID = cardstreamHostedPaymentSettings.MerchantID;
            model.HashKey = cardstreamHostedPaymentSettings.HashKey;
            model.CurrencyCode = cardstreamHostedPaymentSettings.CurrencyCode;
            model.CountryCode = cardstreamHostedPaymentSettings.CountryCode;
            model.FormResponsive = cardstreamHostedPaymentSettings.FormResponsive;


            model.ActiveStoreScopeConfiguration = storeScope;
            if (storeScope > 0)
            {
                model.MerchantID_OverrideForStore = _settingService.SettingExists(cardstreamHostedPaymentSettings, x => x.MerchantID, storeScope);
                model.HashKey_OverrideForStore = _settingService.SettingExists(cardstreamHostedPaymentSettings, x => x.HashKey, storeScope);
                model.CurrencyCode_OverrideForStore = _settingService.SettingExists(cardstreamHostedPaymentSettings, x => x.CurrencyCode, storeScope);
                model.CountryCode_OverrideForStore = _settingService.SettingExists(cardstreamHostedPaymentSettings, x => x.CountryCode, storeScope);
                model.FormResponsive_OverrideForStore = _settingService.SettingExists(cardstreamHostedPaymentSettings, x => x.FormResponsive, storeScope);
            }

            return View("~/Plugins/Payments.CardstreamHosted/Views/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            // Load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var cardstreamHostedPaymentSettings = _settingService.LoadSetting<CardstreamHostedPaymentSettings>(storeScope);

            // Save settings
            cardstreamHostedPaymentSettings.MerchantID = model.MerchantID;
            cardstreamHostedPaymentSettings.HashKey = model.HashKey;
            cardstreamHostedPaymentSettings.CurrencyCode = model.CurrencyCode;
            cardstreamHostedPaymentSettings.CountryCode = model.CountryCode;
            cardstreamHostedPaymentSettings.FormResponsive = model.FormResponsive;

            /* 
             * We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will 
             * not be cleared and loaded from the database after each update.
             */

            _settingService.SaveSettingOverridablePerStore(cardstreamHostedPaymentSettings, x => x.MerchantID, model.MerchantID_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cardstreamHostedPaymentSettings, x => x.HashKey, model.HashKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cardstreamHostedPaymentSettings, x => x.CurrencyCode, model.CurrencyCode_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cardstreamHostedPaymentSettings, x => x.CountryCode, model.CountryCode_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(cardstreamHostedPaymentSettings, x => x.FormResponsive, model.FormResponsive_OverrideForStore, storeScope, false);

            // Clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }


        private Boolean ValidateResponse(FormCollection form)
        {
            var returnedsig = form["signature"];
            form.Remove("signature");
            return returnedsig == CardstreamHostedPaymentProcessor.signCollection(form,_cardstreamHostedPaymentSettings.HashKey);
        }



        public ActionResult RetryPayment(int orderId)
        {

            var postProcessPaymentRequest = new PostProcessPaymentRequest
            {
                Order = _orderService.GetOrderById(orderId)
            };
            _paymentService.PostProcessPayment(postProcessPaymentRequest);

            return Content("Something has clearly gone very wrong");
        }


        [ValidateInput(false)]
        public ActionResult IPNHandler(FormCollection form)
        {
            var processor = _paymentService.LoadPaymentMethodBySystemName("Payments.CardstreamHosted") as CardstreamHostedPaymentProcessor;
            if (processor == null ||
                !processor.IsPaymentMethodActive(_paymentSettings) || !processor.PluginDescriptor.Installed)
                throw new NopException("Cardstream Hosted module cannot be loaded");

            var resCode = form["responseCode"];
            Guid tempguid;
            Guid.TryParse(form["orderRef"], out tempguid);

            if (tempguid != null && _orderService.GetOrderByGuid(tempguid) != null) {
                var order = _orderService.GetOrderByGuid(tempguid);
                if (form["responseCode"] == "0" && ValidateResponse(form))
                {
                    if (order.OrderStatus == OrderStatus.Pending) { 
                        order.OrderNotes.Add(new OrderNote
                        {
                            Note = "Cardstream Payment ID : " + form["transactionID"] + " processed. SUCCESS. " + form["responseMessage"],
                            DisplayToCustomer = true,
                            CreatedOnUtc = DateTime.UtcNow
                        });
                        _orderService.UpdateOrder(order);
                    }
                    if (order != null && _orderProcessingService.CanMarkOrderAsPaid(order))
                    {
                        _orderProcessingService.MarkOrderAsPaid(order);
                    }
                    return RedirectToRoute("CheckoutCompleted", new { orderId = order.Id });
                }
                else
                {
                    order.OrderNotes.Add(new OrderNote
                    {
                        Note = "Cardstream Payment ID : " + form["transactionID"] + " processed. FAILED with error: " + form["responseMessage"],
                        DisplayToCustomer = true,
                        CreatedOnUtc = DateTime.UtcNow
                    });
                    _orderService.UpdateOrder(order);


                    return RedirectToAction("Failed", new { orderId = order.Id, customerOrderNumber = order.CustomOrderNumber });
                }
                
            } else {
                return Content("There has been a problem processing this payment. The Order could not be found, please check the order status in Your Account and contact support if you feel there has been an issue");
            }
        }


        public virtual ActionResult Failed(int orderId, string customOrderNumber)
        {
           CheckoutCompletedModel idmodel = new CheckoutCompletedModel{ OrderId = orderId, CustomOrderNumber = customOrderNumber};
            return View("~/Plugins/Payments.CardstreamHosted/Views/Failed.cshtml", idmodel);

        }

    }
}
