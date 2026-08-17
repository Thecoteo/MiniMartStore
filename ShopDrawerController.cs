using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;

namespace MiniSmartstoreMvc.Controllers
{
    public class ShopDrawerController : Controller
    {
        private const string CartSessionKey = "GUEST_CART";

        // Wishlist chỉ dùng session cho khách chưa đăng nhập.
        // Nếu đã đăng nhập thì sẽ lưu vào database bảng WishlistItems.
        private const string WishlistSessionKey = "WISHLIST_ITEMS";

        // Compare giữ bằng session, không lưu database.
        private const string CompareSessionKey = "COMPARE_ITEMS";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShopDrawerController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        private string GetUserId()
        {
            return _userManager.GetUserId(User) ?? string.Empty;
        }

        private bool IsLoggedIn()
        {
            return User.Identity != null && User.Identity.IsAuthenticated;
        }

        private List<int> GetSessionProductIds(string key)
        {
            var json = HttpContext.Session.GetString(key);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<int>();
            }

            return JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>();
        }

        private void SaveSessionProductIds(string key, List<int> productIds)
        {
            var json = JsonSerializer.Serialize(productIds.Distinct().ToList());
            HttpContext.Session.SetString(key, json);
        }

        private List<SessionCartItem> GetSessionCart()
        {
            var json = HttpContext.Session.GetString(CartSessionKey);

            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<SessionCartItem>();
            }

            return JsonSerializer.Deserialize<List<SessionCartItem>>(json) ?? new List<SessionCartItem>();
        }

        private async Task<List<CartItemViewModel>> GetCartItemsAsync()
        {
            if (IsLoggedIn())
            {
                var userId = GetUserId();

                var dbItems = await _context.CartItems
                    .Include(c => c.Product)
                    .ThenInclude(p => p!.Category)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                return dbItems.Select(c => new CartItemViewModel
                {
                    ProductId = c.ProductId,
                    Product = c.Product,
                    Quantity = c.Quantity
                }).ToList();
            }

            var sessionCart = GetSessionCart();
            var productIds = sessionCart.Select(c => c.ProductId).ToList();

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            return sessionCart
                .Select(item => new CartItemViewModel
                {
                    ProductId = item.ProductId,
                    Product = products.FirstOrDefault(p => p.Id == item.ProductId),
                    Quantity = item.Quantity
                })
                .Where(item => item.Product != null)
                .ToList();
        }

        private async Task MergeSessionWishlistToDatabaseAsync()
        {
            if (!IsLoggedIn())
            {
                return;
            }

            var sessionWishlistIds = GetSessionProductIds(WishlistSessionKey);

            if (!sessionWishlistIds.Any())
            {
                return;
            }

            var userId = GetUserId();

            foreach (var productId in sessionWishlistIds.Distinct())
            {
                var productExists = await _context.Products
                    .AnyAsync(p => p.Id == productId && p.IsActive);

                if (!productExists)
                {
                    continue;
                }

                var existed = await _context.WishlistItems
                    .AnyAsync(w => w.UserId == userId && w.ProductId == productId);

                if (!existed)
                {
                    _context.WishlistItems.Add(new WishlistItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        CreatedAt = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            HttpContext.Session.Remove(WishlistSessionKey);
        }

        private async Task<List<Product>> GetWishlistProductsAsync()
        {
            if (IsLoggedIn())
            {
                await MergeSessionWishlistToDatabaseAsync();

                var userId = GetUserId();

                return await _context.WishlistItems
                    .Include(w => w.Product)
                    .ThenInclude(p => p!.Category)
                    .Where(w => w.UserId == userId && w.Product != null && w.Product.IsActive)
                    .OrderByDescending(w => w.CreatedAt)
                    .Select(w => w.Product!)
                    .ToListAsync();
            }

            var wishlistIds = GetSessionProductIds(WishlistSessionKey);

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => wishlistIds.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            return products
                .OrderBy(p => wishlistIds.IndexOf(p.Id))
                .ToList();
        }

        private async Task<List<Product>> GetCompareProductsAsync()
        {
            var compareIds = GetSessionProductIds(CompareSessionKey);

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => compareIds.Contains(p.Id) && p.IsActive)
                .ToListAsync();

            return products
                .OrderBy(p => compareIds.IndexOf(p.Id))
                .ToList();
        }

        public async Task<IActionResult> Panel(string tab = "cart", string? message = null)
        {
            var model = new ShopDrawerViewModel
            {
                ActiveTab = tab,
                CartItems = await GetCartItemsAsync(),
                WishlistProducts = await GetWishlistProductsAsync(),
                CompareProducts = await GetCompareProductsAsync()
            };

            ViewBag.DrawerMessage = message;

            return PartialView("~/Views/Shared/_ShopDrawerPanel.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToWishlist(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy sản phẩm."
                });
            }

            if (IsLoggedIn())
            {
                var userId = GetUserId();

                var existed = await _context.WishlistItems
                    .AnyAsync(w => w.UserId == userId && w.ProductId == id);

                if (!existed)
                {
                    _context.WishlistItems.Add(new WishlistItem
                    {
                        UserId = userId,
                        ProductId = id,
                        CreatedAt = DateTime.Now
                    });

                    await _context.SaveChangesAsync();
                }
            }
            else
            {
                var ids = GetSessionProductIds(WishlistSessionKey);

                if (!ids.Contains(id))
                {
                    ids.Add(id);
                    SaveSessionProductIds(WishlistSessionKey, ids);
                }
            }

            return Json(new
            {
                success = true,
                message = $"Sản phẩm {product.Name} đã được thêm vào danh sách yêu thích.",
                tab = "wishlist"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCompare(int id)
        {
            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return Json(new
                {
                    success = false,
                    message = "Không tìm thấy sản phẩm."
                });
            }

            var ids = GetSessionProductIds(CompareSessionKey);

            if (!ids.Contains(id))
            {
                ids.Add(id);
            }

            if (ids.Count > 4)
            {
                ids = ids.TakeLast(4).ToList();
            }

            SaveSessionProductIds(CompareSessionKey, ids);

            return Json(new
            {
                success = true,
                message = $"Sản phẩm {product.Name} đã được thêm vào danh sách so sánh.",
                tab = "compare"
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveWishlist(int id)
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
                var ids = GetSessionProductIds(WishlistSessionKey);

                if (ids.Contains(id))
                {
                    ids.Remove(id);
                    SaveSessionProductIds(WishlistSessionKey, ids);
                }
            }

            return Json(new
            {
                success = true,
                tab = "wishlist",
                message = "Đã xóa sản phẩm khỏi danh sách yêu thích."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RemoveCompare(int id)
        {
            var ids = GetSessionProductIds(CompareSessionKey);

            if (ids.Contains(id))
            {
                ids.Remove(id);
                SaveSessionProductIds(CompareSessionKey, ids);
            }

            return Json(new
            {
                success = true,
                tab = "compare",
                message = "Đã xóa sản phẩm khỏi danh sách so sánh."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearWishlist()
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

            return Json(new
            {
                success = true,
                tab = "wishlist",
                message = "Đã xóa toàn bộ danh sách yêu thích."
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ClearCompare()
        {
            HttpContext.Session.Remove(CompareSessionKey);

            return Json(new
            {
                success = true,
                tab = "compare",
                message = "Đã làm sạch danh sách so sánh."
            });
        }
    }
}