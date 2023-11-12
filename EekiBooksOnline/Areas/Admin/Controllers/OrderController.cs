using EekiBooks.DataAcess.Repository.IRepository;
using EekiBooks.Models;
using EekiBooks.Models.ViewModels;
using EekiBooks.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Diagnostics;
using System.Security.Claims;

namespace EekiBooksOnline.Areas.Admin.Controllers
{
	[Area("Admin")]
    [Authorize]
	public class OrderController : Controller
	{
		private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
		public OrderController(IUnitOfWork unitOfWork) 
		{ 
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			return View();
		}


        public IActionResult Detail(int orderId)
        {
            OrderVM = new()
            {
                OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderId,
                    includeProperties:"ApplicationUser"),
                 OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == orderId,
                    includeProperties: "Product"),
            };
            return View(OrderVM);
        }

        [ActionName("Detail")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PayNow()
        {
            OrderVM.OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id,
                    includeProperties: "ApplicationUser");
            OrderVM.OrderDetail = _unitOfWork.OrderDetail.GetAll(u => u.OrderId == OrderVM.OrderHeader.Id,
                    includeProperties: "Product");

            // Stripe Checkout Setup
            var domain = "https://localhost:44331/";
            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>(),

                Mode = "payment",
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderid={OrderVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeader.Id}",
            };

            foreach (var item in OrderVM.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100),
                        Currency = "gbp",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title,
                        },
                    },
                    Quantity = item.Count,
                };
                options.LineItems.Add(sessionLineItem);
            }

            var service = new SessionService();
            Session session = service.Create(options);


          
            _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }



        public IActionResult PaymentConfirmation(int orderHeaderid)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderHeaderid);

            var options = new SessionGetOptions
            {
                Expand = new List<string> { "customer", "payment_intent" },
            };

            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPaymnt)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId, options);

                // check stripe payment status
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _unitOfWork.OrderHeader.UpdateStatus(orderHeaderid, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    orderHeader.PaymentIntent = session.PaymentIntent.Id;
                    _unitOfWork.Save();
                }
            }

            return View(orderHeaderid);
        }




        [HttpPost]
        [Authorize(Roles=SD.Role_Admin + ", " + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetail()
        {
            var OrderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == OrderVM.OrderHeader.Id);
            OrderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
            OrderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
            OrderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
            OrderHeaderFromDb.City = OrderVM.OrderHeader.City;
            OrderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;
            if(OrderVM.OrderHeader.Carrier != null)
            {
                OrderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            }
            if (OrderVM.OrderHeader.TrackingNumber != null)
            {
                OrderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            }
            //_unitOfWork.OrderHeader.Update(OrderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Details updated successfully.";
            return RedirectToAction("Detail", "Order", new {orderId = OrderHeaderFromDb.Id});
        }



        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + ", " + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["success"] = "Order Status updated successfully.";
            return RedirectToAction("Detail", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }



        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + ", " + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder()
        {
            var OrderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(filter: u => u.Id == OrderVM.OrderHeader.Id);
            OrderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            OrderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            OrderHeaderFromDb.OrderStatus = SD.StatusShipped;
            OrderHeaderFromDb.ShippingDate = DateTime.Now;
            if (OrderHeaderFromDb.PaymentStatus == SD.PaymentStatusDelayedPaymnt)
            {
                OrderHeaderFromDb.PaymentDueDate = DateTime.Now.AddDays(30);
            };
            _unitOfWork.OrderHeader.Update(OrderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Status updated successfully.";
            return RedirectToAction("Detail", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + ", " + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder()
        {
            var OrderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(filter: u => u.Id == OrderVM.OrderHeader.Id);
            if (OrderHeaderFromDb.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = OrderHeaderFromDb.PaymentIntent,
                };
                var service = new RefundService();
                Refund refund = service.Create(options);

                _unitOfWork.OrderHeader.UpdateStatus(OrderHeaderFromDb.Id,
                    SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _unitOfWork.OrderHeader.UpdateStatus(OrderHeaderFromDb.Id, 
                    SD.StatusCancelled, SD.StatusCancelled);
            }

            _unitOfWork.Save();
            TempData["success"] = "Order cancelled successfully.";
            return RedirectToAction("Detail", "Order", new { orderId = OrderVM.OrderHeader.Id });
        }



        #region API CALLS
        [HttpGet]
		public IActionResult GetAll(string status)
		{
			IEnumerable<OrderHeader> orderHeaders;

			if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
			{
                orderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claims = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                orderHeaders = _unitOfWork.OrderHeader.GetAll(u => u.ApplicationUserId == claims.Value, includeProperties: "ApplicationUser");

            }


            switch (status)
            {
                case "pending":
                    orderHeaders = orderHeaders.Where(u=>u.PaymentStatus==SD.PaymentStatusDelayedPaymnt);
                    break;

                case "inprocess":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;

                case "approved":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;

                case "completed":
                    orderHeaders = orderHeaders.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;

                default:
                    break;
            }

            return Json(new {data = orderHeaders});

		
		}
		#endregion
	}
}
