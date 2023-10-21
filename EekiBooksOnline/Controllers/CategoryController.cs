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

        // GET 
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();
            }
            var categoryFromDb = _db.Categories.Find(id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            // Validate the model based on data annotations
            if (!TryValidateModel(obj))
            {
                return View(obj); // Return to the view with validation errors
            }
            // Check if Name matches Display Order
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("CustomError", "The display order cannot exactly match the name");
            }
            if (ModelState.IsValid)
            {
                _db.Categories.Update(obj);
                _db.SaveChanges();
                TempData["Success"] = $"{obj.Name} category updated successfully";
                return RedirectToAction("Index");
            }
            return View(obj);
        }

    }
}
