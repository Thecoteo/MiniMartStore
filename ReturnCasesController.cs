using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;

namespace MiniSmartstoreMvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ReturnCaseController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReturnCaseController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> List(
            int? id,
            ReturnCaseType? type,
            DateTime? createdFrom,
            DateTime? createdTo,
            string? orderCode,
            string? customerName,
            string? email,
            ReturnCaseStatus? status,
            int page = 1,
            int pageSize = 25,
            bool filterOpen = false)
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize <= 0 ? 25 : pageSize;

            var query = _context.ReturnCases
                .Include(r => r.Order)
                .Include(r => r.Product)
                .Include(r => r.User)
                .AsQueryable();

            if (id.HasValue)
            {
                query = query.Where(r => r.Id == id.Value);
            }

            if (type.HasValue)
            {
                query = query.Where(r => r.Type == type.Value);
            }

            if (createdFrom.HasValue)
            {
                query = query.Where(r => r.CreatedAt.Date >= createdFrom.Value.Date);
            }

            if (createdTo.HasValue)
            {
                query = query.Where(r => r.CreatedAt.Date <= createdTo.Value.Date);
            }

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                query = query.Where(r =>
                    r.Order != null &&
                    r.Order.OrderCode.Contains(orderCode));
            }

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                query = query.Where(r =>
                    r.CustomerName.Contains(customerName));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(r =>
                    r.CustomerEmail.Contains(email));
            }

            if (status.HasValue)
            {
                query = query.Where(r => r.Status == status.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var model = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new AdminReturnCaseListItemViewModel
                {
                    Id = r.Id,
                    Type = r.Type,
                    Status = r.Status,
                    OrderId = r.OrderId,
                    OrderCode = r.Order != null ? r.Order.OrderCode : "",
                    ProductId = r.ProductId,
                    ProductName = r.Product != null ? r.Product.Name : "Sản phẩm không tồn tại",
                    Quantity = r.Quantity,
                    CustomerName = r.CustomerName,
                    CustomerEmail = r.CustomerEmail,
                    CreatedAt = r.CreatedAt,
                    Reason = r.Reason
                })
                .ToListAsync();

            ViewBag.Id = id;
            ViewBag.Type = type?.ToString();
            ViewBag.CreatedFrom = createdFrom?.ToString("yyyy-MM-dd");
            ViewBag.CreatedTo = createdTo?.ToString("yyyy-MM-dd");
            ViewBag.OrderCode = orderCode;
            ViewBag.CustomerName = customerName;
            ViewBag.Email = email;
            ViewBag.Status = status?.ToString();
            ViewBag.FilterOpen = filterOpen;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages == 0 ? 1 : totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int id, ReturnCaseStatus status)
        {
            var returnCase = await _context.ReturnCases
                .FirstOrDefaultAsync(r => r.Id == id);

            if (returnCase == null)
            {
                TempData["Error"] = "Không tìm thấy yêu cầu hoàn trả.";
                return RedirectToAction(nameof(List));
            }

            returnCase.Status = status;
            returnCase.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật trạng thái yêu cầu hoàn trả.";
            return RedirectToAction(nameof(List));
        }
    }
}