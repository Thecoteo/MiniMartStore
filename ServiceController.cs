using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;

namespace MiniSmartstoreMvc.Controllers
{
    public class ServiceController : Controller
    {
        private const string RecentlyViewedCookieName = "MiniRecentlyViewedProducts";

        private readonly ApplicationDbContext _context;

        public ServiceController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> WhatsNew()
        {
            var baseQuery = _context.Products
                .Include(p => p.Category)
                .Where(p => p.IsActive);

            var model = new WhatsNewViewModel
            {
                NewProducts = await baseQuery
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync(),

                FeaturedProducts = await baseQuery
                    .Where(p => p.IsFeatured)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync(),

                SaleProducts = await baseQuery
                    .Where(p => p.OldPrice.HasValue && p.OldPrice.Value > p.Price)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(8)
                    .ToListAsync()
            };

            return View(model);
        }

        public async Task<IActionResult> RecentlyViewed()
        {
            var viewedIds = ParseRecentlyViewedIds(Request.Cookies[RecentlyViewedCookieName]);

            var products = new List<Product>();

            if (viewedIds.Any())
            {
                var dbProducts = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => p.IsActive && viewedIds.Contains(p.Id))
                    .ToListAsync();

                products = viewedIds
                    .Select(id => dbProducts.FirstOrDefault(p => p.Id == id))
                    .Where(p => p != null)
                    .Cast<Product>()
                    .ToList();
            }

            return View(products);
        }

        public IActionResult AboutUs()
        {
            return View();
        }

        private static List<int> ParseRecentlyViewedIds(string? cookieValue)
        {
            if (string.IsNullOrWhiteSpace(cookieValue))
            {
                return new List<int>();
            }

            return cookieValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct()
                .Take(12)
                .ToList();
        }
    }
}