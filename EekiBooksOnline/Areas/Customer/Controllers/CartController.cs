using EekiBooks.DataAcess.Repository.IRepository;
using EekiBooks.Models;
using EekiBooks.Models.ViewModels;
using EekiBooks.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe.Checkout;
using System.Security.Claims;

namespace EekiBooksOnline.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }

        public int OrderTotal { get; set; }

        public CartController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
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

            ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u=>u.Id == claim.Value);

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
                //ShoppingCartVM.OrderHeader.PaymentIntentId = session.PaymentIntentId;
                _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _unitOfWork.Save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            else
            {
                return RedirectToAction("OrderConfirmation", "Cart", new {id = ShoppingCartVM.OrderHeader.Id});
            }
		}



        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == id);

            if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPaymnt)
            {
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);

				// check stripe payment status
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
			}

			
            List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart.
                    GetAll(u=>u.ApplicationUserId==orderHeader.ApplicationUserId).ToList();
            //OrderHeader and OrderDetail tables have been populated, the shoppingcart has to be cleared now:
            _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
            _unitOfWork.Save();
            return View(id);
        }

	}
}
