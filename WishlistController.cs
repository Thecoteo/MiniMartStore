using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.Controllers
{
    public class WishlistController : Controller
    {
        private const string WishlistSessionKey = "WISHLIST_PRODUCTS";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public WishlistController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private bool IsLoggedIn()
        {
            return User.Identity != null && User.Identity.IsAuthenticated;
        }

        private string GetUserId()
        {
            return _userManager.GetUserId(User) ?? string.Empty;
        }

        private List<int> GetSessionWishlistIds()
        {
            var json = HttpContext.Session.GetString(WishlistSessionKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<int>();
            }

            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }

        private void SaveSessionWishlistIds(List<int> ids)
        {
            var json = JsonSerializer.Serialize(ids.Distinct().ToList());
            HttpContext.Session.SetString(WishlistSessionKey, json);
        }

        public async Task<IActionResult> Index()
        {
            List<Product> products;

            if (IsLoggedIn())
            {
                var userId = GetUserId();

                products = await _context.WishlistItems
                    .Include(w => w.Product)
                    .ThenInclude(p => p!.Category)
                    .Where(w => w.UserId == userId && w.Product != null && w.Product.IsActive)
                    .OrderByDescending(w => w.CreatedAt)
                    .Select(w => w.Product!)
                    .ToListAsync();
            }
            else
            {
                var ids = GetSessionWishlistIds();

                products = await _context.Products
                    .Include(p => p.Category)
                    .Where(p => ids.Contains(p.Id) && p.IsActive)
                    .ToListAsync();

                products = products
                    .OrderBy(p => ids.IndexOf(p.Id))
                    .ToList();
            }

            return View(products);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(int id)
        {
            if (IsLoggedIn())
            {
                var userId = GetUserId();

                var item = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == id);

                if (item != null)
                {
                    _context.WishlistItems.Remove(item);
                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var ids = GetSessionWishlistIds();

                if (ids.Contains(id))
                {
                    ids.Remove(id);
                    SaveSessionWishlistIds(ids);
                }
            }

            TempData["Success"] = "Đã xóa sản phẩm khỏi danh sách yêu thích.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Clear()
        {
            if (IsLoggedIn())
            {
                var userId = GetUserId();

                var items = await _context.WishlistItems
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                _context.WishlistItems.RemoveRange(items);
                await _context.SaveChangesAsync();
            }
            else
            {
                HttpContext.Session.Remove(WishlistSessionKey);
            }

            TempData["Success"] = "Đã xóa toàn bộ danh sách yêu thích.";
            return RedirectToAction("Index");
        }
    }
}