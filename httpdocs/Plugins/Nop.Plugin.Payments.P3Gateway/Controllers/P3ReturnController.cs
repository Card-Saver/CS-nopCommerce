using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Nop.Core;
using Nop.Core.Domain.Orders;
using Nop.Services.Configuration;
using Nop.Services.Logging;
using Nop.Services.Messages;
using Nop.Services.Orders;
using Nop.Web.Framework.Controllers;
using System.Linq;

namespace Nop.Plugin.Payments.P3Gateway.Controllers
{
    public class P3ReturnController : BasePaymentController
    {
        #region Ctor

        public P3ReturnController(
            INotificationService notificationService,
            ISettingService settingService,
            ILogger logger,
            IOrderService orderService,
            IOrderProcessingService orderProcessingService,
            Gateway gateway,
            IStoreContext storeContext)
        {
            _notificationService = notificationService;
            _settingService = settingService;
            _logger = logger;
            _orderService = orderService;
            _orderProcessingService = orderProcessingService;
            _gateway = gateway;
        }

        #endregion

        #region Methods

        public async Task<IActionResult> ReturnHandler()
        {
            var response = Request.Form;

            var order = await _orderService.GetOrderByIdAsync(int.Parse(response["orderRef"]));
            if (order == null)
                return RedirectToAction("Index", "Home", new { area = string.Empty });

            var form = response.ToDictionary(item => (string) item.Key, item => (string) item.Value);

            if (int.Parse(response["responseCode"]) == 0)
            {
                //order note
                await _orderService.InsertOrderNoteAsync(new OrderNote
                {
                    OrderId = order.Id,
                    Note = "Payment Succesfull",
                    DisplayToCustomer = false,
                    CreatedOnUtc = DateTime.UtcNow
                });

                await _orderProcessingService.MarkOrderAsPaidAsync(order);

                return RedirectToRoute("CheckoutCompleted", new { orderId = response["orderRef"] });
            }

            var errMsg = "Payment failed. Error Code: " + response["responseCode"] + ", xref: " + response["xref"];

            //order note
            await _orderService.InsertOrderNoteAsync(new OrderNote
            {
                OrderId = order.Id,
                Note = errMsg,
                DisplayToCustomer = false,
                CreatedOnUtc = DateTime.UtcNow
            });

            await _orderProcessingService.CancelOrderAsync(order, true);

            _notificationService.ErrorNotification("Something went wrong with the processing of the payment, please get in contact with the administration.");

            return RedirectToAction("Index", "Home", new { area = string.Empty });
        }

        #endregion

        #region Fields

        private readonly INotificationService _notificationService;
        private readonly ISettingService _settingService;
        private readonly ILogger _logger;
        private readonly Gateway _gateway;
        private readonly IOrderService _orderService;
        private readonly IOrderProcessingService _orderProcessingService;

        #endregion
    }
}