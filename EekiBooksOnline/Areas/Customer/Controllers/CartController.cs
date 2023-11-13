using EekiBooks.DataAcess.Repository.IRepository;
using EekiBooks.Models;
using EekiBooks.Models.ViewModels;
using EekiBooks.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace EekiBooksOnline.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailSender _emailSender;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public int OrderTotal { get; set; }

       

        public CartController(IUnitOfWork unitOfWork, IEmailSender emailSender)
        {
            _unitOfWork = unitOfWork;
            _emailSender = emailSender;
        }

        public IActionResult Index()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product"),
                OrderHeader = new ()
            };

            foreach(var item in ShoppingCartVM.ListCart)
            {
                item.Price = GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50, 
                    item.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
            }

            return View(ShoppingCartVM);
        }


        public IActionResult Plus (int cartId)
        {
            var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(c => c.Id == cartId);
            _unitOfWork.ShoppingCart.IncrementCount(cart, 1);
            _unitOfWork.Save();
            return RedirectToAction(nameof(Index));
        }


		public IActionResult Minus (int cartId)
		{
			var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(c => c.Id == cartId);
            if (cart.Count <= 1)
            {
				_unitOfWork.ShoppingCart.Remove(cart);
                var count = _unitOfWork.ShoppingCart.GetAll(p => p.ApplicationUserId == cart.ApplicationUserId).ToList().Count-1;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
            else
            {
				_unitOfWork.ShoppingCart.DecrementCount(cart, 1);
			}
            _unitOfWork.Save();
			return RedirectToAction(nameof(Index));
		}


		public IActionResult Remove(int cartId)
		{
			var cart = _unitOfWork.ShoppingCart.GetFirstOrDefault(c => c.Id == cartId);
			_unitOfWork.ShoppingCart.Remove(cart);
			_unitOfWork.Save();
            var count = _unitOfWork.ShoppingCart.GetAll(p => p.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.SessionCart, count);
			return RedirectToAction(nameof(Index));
		}

		private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if (quantity <= 50)
            {
                return price;
            }
            else
            {
                if (quantity <= 100)
                {
                    return price50;
                }
                return price100;
            }
        }

		public IActionResult Summary()
		{
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product"),
                OrderHeader = new ()
            };


			ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.
                GetFirstOrDefault(u => u.Id == claim.Value);

            ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
			ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
			ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
			ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

			foreach (var item in ShoppingCartVM.ListCart)
            {
                item.Price = GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50,
                    item.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
            }

            return View(ShoppingCartVM);
		}





        [ActionName("Summary")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPOST()
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM.ListCart = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value,
                includeProperties: "Product");


            ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;

            foreach (var item in ShoppingCartVM.ListCart)
            {
                item.Price = GetPriceBasedOnQuantity(item.Count, item.Product.Price, item.Product.Price50,
                    item.Product.Price100);
                ShoppingCartVM.OrderHeader.OrderTotal += (item.Price * item.Count);
            }

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPaymnt;
                ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }
            _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
            _unitOfWork.Save();


            foreach (var item in ShoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = item.ProductId,
                    OrderId = ShoppingCartVM.OrderHeader.Id,
                    Price = item.Price,
                    Count = item.Count,
                };
                _unitOfWork.OrderDetail.Add(orderDetail);
                _unitOfWork.Save();
            }


            if (applicationUser.CompanyId.GetValueOrDefault() == 0)
            {
                // Stripe Checkout Setup
                var domain = "https://localhost:44331/";
                var options = new SessionCreateOptions
                {
                    LineItems = new List<SessionLineItemOptions>(),

                    Mode = "payment",
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + $"customer/cart/index",
                };

                foreach (var item in ShoppingCartVM.ListCart)
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

               
                //ShoppingCartVM.OrderHeader.SessionId = session.Id;
                //ShoppingCartVM.OrderHeader.PaymentIntent = session.PaymentIntentId;
                _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            else
            {
                return RedirectToAction("OrderConfirmation", "Cart", new { id = ShoppingCartVM.OrderHeader.Id });
            }
        }




        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == id, 
                includeProperties:"ApplicationUser");

            var options = new SessionGetOptions
            {
                Expand = new List<string> { "customer", "payment_intent.latest_charge" }
            };
           

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPaymnt)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId, options);
                var receiptUrl = session.PaymentIntent.LatestCharge.ReceiptUrl;
                // check stripe payment status
                if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    orderHeader.PaymentIntent = session.PaymentIntent.Id;
                    _unitOfWork.Save();
				}
                _emailSender.SendEmailAsync(orderHeader.ApplicationUser.Email, "New Order: EekiBooks",
                  $"<p>Thank you for your Order {orderHeader.Id}, payment has been successfully processed.</P> " +
                  $"<p> Here are your payment details for your records.</p> View your receipt by " +
                  $"<a href='{HtmlEncoder.Default.Encode(receiptUrl)}'>clicking here</a>");
            }
          
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.
                    GetAll(u=>u.ApplicationUserId==orderHeader.ApplicationUserId).ToList();
           
            //OrderHeader and OrderDetail tables have been populated, the shoppingcart has to be cleared now:
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            //var count = shoppingCarts.Count();
            HttpContext.Session.SetInt32(SD.SessionCart, 0);
            _unitOfWork.Save();
            return View(id);
        }



	}
}
