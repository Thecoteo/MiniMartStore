using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.ViewModels;

namespace MiniSmartstoreMvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ShoppingCartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ShoppingCartController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> CurrentCarts(
            DateTime? createdFrom,
            DateTime? createdTo,
            string? email,
            int page = 1,
            int pageSize = 25,
            bool filterOpen = false)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : pageSize;

            var items = await _context.CartItems
                .Include(c => c.User)
                .Include(c => c.Product)
                .ThenInclude(p => p.Category)
                .Where(c => c.Product != null)
                .ToListAsync();

            DateTime GetCartDate(Models.CartItem item)
            {
                return item.UpdatedAt ?? item.CreatedAt ?? DateTime.Now;
            }

            if (createdFrom.HasValue)
            {
                items = items
                    .Where(c => GetCartDate(c).Date >= createdFrom.Value.Date)
                    .ToList();
            }

            if (createdTo.HasValue)
            {
                items = items
                    .Where(c => GetCartDate(c).Date <= createdTo.Value.Date)
                    .ToList();
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                items = items
                    .Where(c => c.User != null &&
                                c.User.Email != null &&
                                c.User.Email.Contains(email, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            var grouped = items
                .GroupBy(c => c.UserId)
                .Select(g =>
                {
                    var first = g.First();

                    return new AdminCurrentCartViewModel
                    {
                        UserId = g.Key,
                        CustomerName = first.User?.FullName
                                       ?? first.User?.Email
                                       ?? "Khách hàng",
                        CustomerEmail = first.User?.Email ?? "Không có email",
                        UpdatedAt = g.Max(x => x.UpdatedAt ?? x.CreatedAt),
                        Products = g.Select(x => new AdminCurrentCartProductViewModel
                        {
                            ProductId = x.ProductId,
                            ProductName = x.Product?.Name ?? "Sản phẩm không tồn tại",
                            CategoryName = x.Product?.Category?.Name,
                            IsActive = x.Product?.IsActive ?? false,
                            Quantity = x.Quantity,
                            UnitPrice = x.Product?.Price ?? 0
                        }).ToList()
                    };
                })
                .OrderByDescending(x => x.UpdatedAt)
                .ToList();

            var totalItems = grouped.Count;
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var model = grouped
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.CreatedFrom = createdFrom?.ToString("yyyy-MM-dd");
            ViewBag.CreatedTo = createdTo?.ToString("yyyy-MM-dd");
            ViewBag.Email = email;
            ViewBag.FilterOpen = filterOpen;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages == 0 ? 1 : totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View(model);
        }
    }
}