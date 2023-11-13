using EekiBooks.DataAcess.Repository.IRepository;
using EekiBooks.Utilities;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace EekiBooksOnline.ViewComponents
{
    public class CartCounter: ViewComponent
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartCounter(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            //check if a user is logged in
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)    //if claim is not null, user is loggedin
            {
                if (HttpContext.Session.GetInt32(SD.SessionCart) != null) //check if session is null
                {
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
                else
                {
                    HttpContext.Session.SetInt32(SD.SessionCart,
                        _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count);
                    return View(HttpContext.Session.GetInt32(SD.SessionCart));
                }
            }
            else
            {
                HttpContext.Session.Clear();
                return View(0);
            }
        }
    }
}
