using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;

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
            int count = 0;

            if (User.Identity?.IsAuthenticated == true)
            {
                var userId = _userManager.GetUserId(HttpContext.User);

                if (!string.IsNullOrEmpty(userId))
                {
                    count = await _context.CartItems
                        .Where(c => c.UserId == userId)
                        .SumAsync(c => (int?)c.Quantity) ?? 0;
                }
            }
            else
            {
                var json = HttpContext.Session.GetString(CartSessionKey);

                if (!string.IsNullOrWhiteSpace(json))
                {
                    var sessionCart = JsonSerializer.Deserialize<List<SessionCartItem>>(json)
                        ?? new List<SessionCartItem>();

                    count = sessionCart.Sum(x => x.Quantity);
                }
            }

            return View(count);
        }
    }
}