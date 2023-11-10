using EekiBooks.DataAcess.Repository.IRepository;
using EekiBooks.Models;
using EekiBooks.Models.ViewModels;
using EekiBooks.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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

        [HttpPost]
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
        [ValidateAntiForgeryToken]
        public IActionResult StartProcessing()
        {
            _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
            _unitOfWork.Save();
            TempData["success"] = "Order Status updated successfully.";
            return RedirectToAction("Detail", "Order", new { orderId = OrderVM.OrderHeader.Id });

        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ShipOrder()
        {
            var OrderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(filter: u => u.Id == OrderVM.OrderHeader.Id);
            OrderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
            OrderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
            OrderHeaderFromDb.OrderStatus = SD.StatusShipped;
            OrderHeaderFromDb.ShippingDate = DateTime.Now;
            _unitOfWork.OrderHeader.Update(OrderHeaderFromDb);
            _unitOfWork.Save();
            TempData["success"] = "Order Status updated successfully.";
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
