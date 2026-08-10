using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;
using System.Text.Json;

namespace MiniSmartstoreMvc.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private const string CartSessionKey = "GUEST_CART";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartCountViewComponent(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var count = 0;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(HttpContext.User);

                if (!string.IsNullOrEmpty(userId))
                {
                    // ===== LƯU Ý: BADGE ĐẾM TOÀN BỘ SẢN PHẨM THỰC SỰ CÒN TRONG GIỎ =====
                    count = await _context.CartItems
                        .Where(c => c.UserId == userId)
                        .SumAsync(c => (int?)c.Quantity) ?? 0;
                    // ===== KẾT THÚC ĐẾM TOÀN BỘ SẢN PHẨM TRONG GIỎ =====
                }
            }
            else
            {
                var json = HttpContext.Session.GetString(CartSessionKey);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var sessionCart =
                        JsonSerializer.Deserialize<List<SessionCartItem>>(json)
                        ?? new List<SessionCartItem>();

                    var productIds = sessionCart
                        .Select(x => x.ProductId)
                        .Distinct()
                        .ToList();

                    // ===== LƯU Ý: CHỈ LOẠI SẢN PHẨM KHÔNG CÒN TỒN TẠI TRONG DATABASE =====
                    var existingProductIds = await _context.Products
                        .Where(p => productIds.Contains(p.Id))
                        .Select(p => p.Id)
                        .ToListAsync();

                    count = sessionCart
                        .Where(x => existingProductIds.Contains(x.ProductId))
                        .Sum(x => x.Quantity);
                    // ===== KẾT THÚC KIỂM TRA SẢN PHẨM TỒN TẠI =====
                }
            }

            return View(count);
        }
    }
}