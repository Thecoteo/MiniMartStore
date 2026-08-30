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
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction(nameof(List));
        }

        public async Task<IActionResult> List(
            string? search,
            string? customerName,
            string? email,
            string? orderCode,
            DateTime? createdFrom,
            DateTime? createdTo,
            OrderStatus? orderStatus,
            PaymentStatus? paymentStatus,
            int page = 1,
            int pageSize = 25,
            bool filterOpen = false)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize <= 0)
            {
                pageSize = 25;
            }

            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(o =>
                    o.OrderCode.Contains(keyword) ||
                    o.CustomerName.Contains(keyword) ||
                    o.PhoneNumber.Contains(keyword) ||
                    o.ShippingAddress.Contains(keyword) ||
                    (o.User != null && o.User.Email != null && o.User.Email.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                query = query.Where(o => o.CustomerName.Contains(customerName));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(o => o.User != null &&
                                         o.User.Email != null &&
                                         o.User.Email.Contains(email));
            }

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                query = query.Where(o => o.OrderCode.Contains(orderCode));
            }

            if (createdFrom.HasValue)
            {
                query = query.Where(o => o.CreatedAt.Date >= createdFrom.Value.Date);
            }

            if (createdTo.HasValue)
            {
                query = query.Where(o => o.CreatedAt.Date <= createdTo.Value.Date);
            }

            if (orderStatus.HasValue)
            {
                query = query.Where(o => o.OrderStatus == orderStatus.Value);
            }

            if (paymentStatus.HasValue)
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
            }

            var totalItems = await query.CountAsync();

            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (totalPages == 0)
            {
                totalPages = 1;
            }

            if (page > totalPages)
            {
                page = totalPages;
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(o => new AdminOrderListItemViewModel
                {
                    Id = o.Id,
                    OrderCode = o.OrderCode,
                    CustomerName = o.CustomerName,
                    CustomerEmail = o.User != null && o.User.Email != null ? o.User.Email : "",
                    PhoneNumber = o.PhoneNumber,
                    ShippingAddress = o.ShippingAddress,
                    TotalAmount = o.TotalAmount,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt,
                    ProductCount = o.OrderDetails != null ? o.OrderDetails.Count : 0,
                    TotalQuantity = o.OrderDetails != null ? o.OrderDetails.Sum(x => x.Quantity) : 0
                })
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.CustomerName = customerName;
            ViewBag.Email = email;
            ViewBag.OrderCode = orderCode;
            ViewBag.CreatedFrom = createdFrom?.ToString("yyyy-MM-dd");
            ViewBag.CreatedTo = createdTo?.ToString("yyyy-MM-dd");
            ViewBag.OrderStatus = orderStatus;
            ViewBag.PaymentStatus = paymentStatus;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.FilterOpen = filterOpen;

            return View(orders);
        }

        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.Payment)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            var model = new AdminOrderDetailViewModel
            {
                Order = order,
                CustomerEmail = order.User?.Email ?? "",
                OrderDetails = order.OrderDetails?.ToList() ?? new List<OrderDetail>()
            };

            return View(model);
        }
        public async Task<IActionResult> Print(
            string? search,
            string? customerName,
            string? email,
            string? orderCode,
            DateTime? createdFrom,
            DateTime? createdTo,
            OrderStatus? orderStatus,
            PaymentStatus? paymentStatus)
        {
            var query = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var keyword = search.Trim();

                query = query.Where(o =>
                    o.OrderCode.Contains(keyword) ||
                    o.CustomerName.Contains(keyword) ||
                    o.PhoneNumber.Contains(keyword) ||
                    o.ShippingAddress.Contains(keyword) ||
                    (o.User != null && o.User.Email != null && o.User.Email.Contains(keyword)));
            }

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                query = query.Where(o => o.CustomerName.Contains(customerName));
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(o => o.User != null &&
                                         o.User.Email != null &&
                                         o.User.Email.Contains(email));
            }

            if (!string.IsNullOrWhiteSpace(orderCode))
            {
                query = query.Where(o => o.OrderCode.Contains(orderCode));
            }

            if (createdFrom.HasValue)
            {
                query = query.Where(o => o.CreatedAt.Date >= createdFrom.Value.Date);
            }

            if (createdTo.HasValue)
            {
                query = query.Where(o => o.CreatedAt.Date <= createdTo.Value.Date);
            }

            if (orderStatus.HasValue)
            {
                query = query.Where(o => o.OrderStatus == orderStatus.Value);
            }

            if (paymentStatus.HasValue)
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
            }

            var orders = await query
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new AdminOrderListItemViewModel
                {
                    Id = o.Id,
                    OrderCode = o.OrderCode,
                    CustomerName = o.CustomerName,
                    CustomerEmail = o.User != null && o.User.Email != null ? o.User.Email : "",
                    PhoneNumber = o.PhoneNumber,
                    ShippingAddress = o.ShippingAddress,
                    TotalAmount = o.TotalAmount,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt,
                    ProductCount = o.OrderDetails != null ? o.OrderDetails.Count : 0,
                    TotalQuantity = o.OrderDetails != null ? o.OrderDetails.Sum(x => x.Quantity) : 0
                })
                .ToListAsync();

            return View(orders);
        }
        public async Task<IActionResult> PrintSelected(List<int> ids)
        {
            if (ids == null || !ids.Any())
            {
                TempData["Error"] = "Vui lòng chọn ít nhất một đơn hàng để xuất PDF.";
                return RedirectToAction(nameof(List));
            }

            var orders = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderDetails)
                .ThenInclude(od => od.Product)
                .Where(o => ids.Contains(o.Id))
                .OrderByDescending(o => o.CreatedAt)
                .Select(o => new AdminOrderListItemViewModel
                {
                    Id = o.Id,
                    OrderCode = o.OrderCode,
                    CustomerName = o.CustomerName,
                    CustomerEmail = o.User != null && o.User.Email != null ? o.User.Email : "",
                    PhoneNumber = o.PhoneNumber,
                    ShippingAddress = o.ShippingAddress,
                    TotalAmount = o.TotalAmount,
                    PaymentMethod = o.PaymentMethod,
                    PaymentStatus = o.PaymentStatus,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt,
                    ProductCount = o.OrderDetails != null ? o.OrderDetails.Count : 0,
                    TotalQuantity = o.OrderDetails != null ? o.OrderDetails.Sum(x => x.Quantity) : 0
                })
                .ToListAsync();

            return View("Print", orders);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrderStatus(int id, OrderStatus orderStatus)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = orderStatus;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật trạng thái đơn hàng.";

            return RedirectToAction(nameof(Details), new { id });
        }

        // ===== LƯU Ý: HỦY ĐƠN HÀNG =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = OrderStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã hủy đơn hàng thành công.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int id, PaymentStatus paymentStatus)
        {
            var order = await _context.Orders
                .Include(o => o.Payment)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                return NotFound();
            }

            order.PaymentStatus = paymentStatus;

            if (order.Payment != null)
            {
                order.Payment.PaymentStatus = paymentStatus;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật trạng thái thanh toán.";

            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickComplete(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = OrderStatus.Completed;
            order.PaymentStatus = PaymentStatus.Paid;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đơn hàng đã được đánh dấu hoàn thành.";

            return RedirectToAction(nameof(List));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCancel(int id)
        {
            var order = await _context.Orders.FindAsync(id);

            if (order == null)
            {
                return NotFound();
            }

            order.OrderStatus = OrderStatus.Cancelled;
            order.PaymentStatus = PaymentStatus.Cancelled;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đơn hàng đã được hủy.";

            return RedirectToAction(nameof(List));
        }

        private string GetOrderStatusText(OrderStatus status)
        {
            return status switch
            {
                OrderStatus.Pending => "Chờ xử lý",
                OrderStatus.Processing => "Đang xử lý",
                OrderStatus.Completed => "Hoàn thành",
                OrderStatus.Cancelled => "Đã hủy",
                _ => "Không xác định"
            };
        }
    }
}