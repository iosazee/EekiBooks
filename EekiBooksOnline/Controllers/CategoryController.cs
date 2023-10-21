using EekiBooksOnline.Data;
using EekiBooksOnline.Models;
using Microsoft.AspNetCore.Mvc;

namespace EekiBooksOnline.Controllers
{
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }
        public IActionResult Index()
        {
            IEnumerable<Category> objCategoryList = _db.Categories;
            return View(objCategoryList);
        }
        // GET
        public IActionResult Create()
        {
            return View();
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            // Validate the model based on data annotations
            if (!TryValidateModel(obj))
            {
                return View(obj); // Return to the view with validation errors
            }
            if (ModelState.IsValid)
            {
                _db.Categories.Add(obj);
                _db.SaveChanges();
                TempData["Success"] = $"{obj.Name} category created successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }
    }
}
