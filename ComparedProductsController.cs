using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;

namespace MiniSmartstoreMvc.Controllers
{
    [Route("compareproducts")]
    public class CompareProductsController : Controller
    {
        private const string CompareSessionKey = "COMPARE_ITEMS";

        private readonly ApplicationDbContext _context;

        public CompareProductsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private List<int> GetCompareIds()
        {
            var json = HttpContext.Session.GetString(CompareSessionKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<int>();
            }

            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }

        private void SaveCompareIds(List<int> ids)
        {
            var json = JsonSerializer.Serialize(ids.Distinct().ToList());
            HttpContext.Session.SetString(CompareSessionKey, json);
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var ids = GetCompareIds();

            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductColors)
                .Where(p => ids.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            products = products
                .OrderBy(p => ids.IndexOf(p.Id))
                .ToList();

            return View(products);
        }

        [HttpPost("remove")]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int id)
        {
            var ids = GetCompareIds();

            if (ids.Contains(id))
            {
                ids.Remove(id);
                SaveCompareIds(ids);
                TempData["Success"] = "Đã xóa sản phẩm khỏi danh sách so sánh.";
            }

            return RedirectToAction("Index");
        }

        [HttpPost("clear")]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CompareSessionKey);
            TempData["Success"] = "Đã xóa toàn bộ danh sách so sánh.";

            return RedirectToAction("Index");
        }
    }
}