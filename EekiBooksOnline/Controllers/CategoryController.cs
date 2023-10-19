using Microsoft.AspNetCore.Mvc;

namespace EekiBooksOnline.Controllers
{
    public class CategoryController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
