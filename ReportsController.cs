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
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var sevenDaysAgo = today.AddDays(-6);
            var thirtyDaysAgo = today.AddDays(-29);

            var revenueOrders = GetRevenueOrdersQuery(null, null);

            var model = new AdminReportOverviewViewModel
            {
                TotalRevenue = await revenueOrders.SumAsync(o => o.TotalAmount),
                TotalOrders = await _context.Orders.CountAsync(),
                CompletedOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Completed),
                PendingOrders = await _context.Orders.CountAsync(o => o.OrderStatus == OrderStatus.Pending),

                SoldQuantity = await GetRevenueOrderDetailsQuery(null, null)
                    .SumAsync(od => (int?)od.Quantity) ?? 0,

                NewCustomers = await _context.Users
                    .CountAsync(u => u.CreatedAt >= thirtyDaysAgo)
            };

            model.RevenueLast7Days = await BuildRevenueChartAsync(sevenDaysAgo, today);
            model.RevenueLast30Days = await BuildRevenueChartAsync(thirtyDaysAgo, today);

            model.TopProducts = await BuildProductReportItemsAsync(null, null, "quantity", 5);
            model.TopCustomers = await BuildCustomerReportItemsAsync(null, null, "amount", 5);
            model.InventoryWarnings = await BuildInventoryReportItemsAsync("warning", 5);

            return View(model);
        }

        public async Task<IActionResult> Sales(
            DateTime? fromDate,
            DateTime? toDate,
            OrderStatus? orderStatus,
            PaymentStatus? paymentStatus)
        {
            var from = fromDate?.Date ?? DateTime.Today.AddDays(-29);
            var to = toDate?.Date ?? DateTime.Today;

            var query = _context.Orders
                .Include(o => o.OrderDetails)
                .AsQueryable();

            query = query.Where(o => o.CreatedAt >= from && o.CreatedAt < to.AddDays(1));

            if (orderStatus.HasValue)
            {
                query = query.Where(o => o.OrderStatus == orderStatus.Value);
            }

            if (paymentStatus.HasValue)
            {
                query = query.Where(o => o.PaymentStatus == paymentStatus.Value);
            }

            var orders = await query.ToListAsync();

            var rows = orders
                .GroupBy(o => o.CreatedAt.Date)
                .OrderBy(g => g.Key)
                .Select(g => new AdminSalesReportRowViewModel
                {
                    Date = g.Key,
                    OrderCount = g.Count(),
                    SoldQuantity = g
                        .Where(IsRevenueOrder)
                        .SelectMany(o => o.OrderDetails ?? new List<OrderDetail>())
                        .Sum(od => od.Quantity),
                    Revenue = g
                        .Where(IsRevenueOrder)
                        .Sum(o => o.TotalAmount),
                    CompletedOrders = g.Count(o => o.OrderStatus == OrderStatus.Completed),
                    CancelledOrders = g.Count(o => o.OrderStatus == OrderStatus.Cancelled)
                })
                .ToList();

            var model = new AdminSalesReportViewModel
            {
                FromDate = from,
                ToDate = to,
                OrderStatus = orderStatus,
                PaymentStatus = paymentStatus,
                Rows = rows,

                TotalOrders = orders.Count,
                TotalSoldQuantity = rows.Sum(x => x.SoldQuantity),
                TotalRevenue = rows.Sum(x => x.Revenue),
                CompletedOrders = rows.Sum(x => x.CompletedOrders),
                CancelledOrders = rows.Sum(x => x.CancelledOrders)
            };

            return View(model);
        }

        public async Task<IActionResult> Products(
            DateTime? fromDate,
            DateTime? toDate,
            string sortBy = "quantity")
        {
            sortBy = string.IsNullOrWhiteSpace(sortBy) ? "quantity" : sortBy;

            var items = await BuildProductReportItemsAsync(fromDate, toDate, sortBy, null);

            var model = new AdminProductReportViewModel
            {
                SortBy = sortBy,
                FromDate = fromDate,
                ToDate = toDate,
                Items = items,

                TotalProducts = items.Count,
                TotalSoldQuantity = items.Sum(x => x.SoldQuantity),
                TotalRevenue = items.Sum(x => x.Revenue)
            };

            return View(model);
        }

        public async Task<IActionResult> Customers(
            DateTime? fromDate,
            DateTime? toDate,
            string sortBy = "amount")
        {
            sortBy = string.IsNullOrWhiteSpace(sortBy) ? "amount" : sortBy;

            var items = await BuildCustomerReportItemsAsync(fromDate, toDate, sortBy, null);

            var totalCustomers = await _context.Users.CountAsync();

            var model = new AdminCustomerReportViewModel
            {
                SortBy = sortBy,
                FromDate = fromDate,
                ToDate = toDate,
                Items = items,

                TotalCustomers = totalCustomers,
                CustomersWithOrders = items.Count,
                CustomersWithoutOrders = Math.Max(0, totalCustomers - items.Count),
                TotalRevenue = items.Sum(x => x.TotalSpent)
            };

            return View(model);
        }

        public async Task<IActionResult> Inventory(string stockFilter = "all")
        {
            stockFilter = string.IsNullOrWhiteSpace(stockFilter) ? "all" : stockFilter;

            var items = await BuildInventoryReportItemsAsync(stockFilter, null);

            var allProducts = await _context.Products.ToListAsync();

            var model = new AdminInventoryReportViewModel
            {
                StockFilter = stockFilter,
                Items = items,

                TotalProducts = allProducts.Count,
                OutOfStockCount = allProducts.Count(p => p.StockQuantity <= 0),
                LowStockCount = allProducts.Count(p => p.StockQuantity > 0 && p.StockQuantity <= 5),
                HighStockCount = allProducts.Count(p => p.StockQuantity >= 100),
                HiddenProductCount = allProducts.Count(p => !p.IsActive)
            };

            return View(model);
        }

        private IQueryable<Order> GetRevenueOrdersQuery(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.Orders
                .Where(o => o.OrderStatus == OrderStatus.Completed || o.PaymentStatus == PaymentStatus.Paid);

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(o => o.CreatedAt >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(o => o.CreatedAt < to);
            }

            return query;
        }

        private IQueryable<OrderDetail> GetRevenueOrderDetailsQuery(DateTime? fromDate, DateTime? toDate)
        {
            var query = _context.OrderDetails
                .Where(od =>
                    od.Order != null &&
                    (
                        od.Order.OrderStatus == OrderStatus.Completed ||
                        od.Order.PaymentStatus == PaymentStatus.Paid
                    ));

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(od => od.Order != null && od.Order.CreatedAt >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1);
                query = query.Where(od => od.Order != null && od.Order.CreatedAt < to);
            }

            return query;
        }

        private async Task<List<AdminReportChartPointViewModel>> BuildRevenueChartAsync(DateTime fromDate, DateTime toDate)
        {
            var groupedData = await GetRevenueOrdersQuery(fromDate, toDate)
                .GroupBy(o => o.CreatedAt.Date)
                .Select(g => new
                {
                    Date = g.Key,
                    Amount = g.Sum(o => o.TotalAmount),
                    Count = g.Count()
                })
                .ToListAsync();

            var result = new List<AdminReportChartPointViewModel>();

            for (var date = fromDate.Date; date <= toDate.Date; date = date.AddDays(1))
            {
                var item = groupedData.FirstOrDefault(x => x.Date == date);

                result.Add(new AdminReportChartPointViewModel
                {
                    Label = date.ToString("dd/MM"),
                    Amount = item?.Amount ?? 0,
                    Count = item?.Count ?? 0
                });
            }

            return result;
        }

        private async Task<List<AdminProductReportItemViewModel>> BuildProductReportItemsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string sortBy,
            int? take)
        {
            var soldData = await GetRevenueOrderDetailsQuery(fromDate, toDate)
                .GroupBy(od => od.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SoldQuantity = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.UnitPrice * x.Quantity)
                })
                .ToListAsync();

            var productIds = soldData.Select(x => x.ProductId).ToList();

            var products = await _context.Products
                .Include(p => p.Category)
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id);

            var items = soldData
                .Select(x =>
                {
                    products.TryGetValue(x.ProductId, out var product);

                    return new AdminProductReportItemViewModel
                    {
                        ProductId = x.ProductId,
                        ProductName = product?.Name ?? "Sản phẩm không còn tồn tại",
                        CategoryName = product?.Category?.Name ?? "Không có danh mục",
                        SoldQuantity = x.SoldQuantity,
                        Revenue = x.Revenue,
                        StockQuantity = product?.StockQuantity ?? 0,
                        IsActive = product?.IsActive ?? false
                    };
                })
                .ToList();

            items = sortBy == "amount"
                ? items.OrderByDescending(x => x.Revenue).ToList()
                : items.OrderByDescending(x => x.SoldQuantity).ToList();

            if (take.HasValue)
            {
                items = items.Take(take.Value).ToList();
            }

            return items;
        }

        private async Task<List<AdminCustomerReportItemViewModel>> BuildCustomerReportItemsAsync(
            DateTime? fromDate,
            DateTime? toDate,
            string sortBy,
            int? take)
        {
            var ordersQuery = GetRevenueOrdersQuery(fromDate, toDate);

            var orderData = await ordersQuery
                .GroupBy(o => o.UserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    OrderCount = g.Count(),
                    TotalSpent = g.Sum(o => o.TotalAmount),
                    LastOrderAt = g.Max(o => o.CreatedAt)
                })
                .ToListAsync();

            var userIds = orderData.Select(x => x.UserId).ToList();

            var users = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            var items = orderData
                .Select(x =>
                {
                    users.TryGetValue(x.UserId, out var user);

                    return new AdminCustomerReportItemViewModel
                    {
                        UserId = x.UserId,
                        CustomerName = string.IsNullOrWhiteSpace(user?.FullName)
                            ? user?.Email ?? "Khách hàng không còn tồn tại"
                            : user.FullName,
                        Email = user?.Email ?? "",
                        OrderCount = x.OrderCount,
                        TotalSpent = x.TotalSpent,
                        LastOrderAt = x.LastOrderAt,
                        IsActive = user != null && IsUserActive(user)
                    };
                })
                .ToList();

            items = sortBy == "orders"
                ? items.OrderByDescending(x => x.OrderCount).ToList()
                : items.OrderByDescending(x => x.TotalSpent).ToList();

            if (take.HasValue)
            {
                items = items.Take(take.Value).ToList();
            }

            return items;
        }

        private async Task<List<AdminInventoryReportItemViewModel>> BuildInventoryReportItemsAsync(
            string stockFilter,
            int? take)
        {
            var soldData = await GetRevenueOrderDetailsQuery(null, null)
                .GroupBy(od => od.ProductId)
                .Select(g => new
                {
                    ProductId = g.Key,
                    SoldQuantity = g.Sum(x => x.Quantity)
                })
                .ToDictionaryAsync(x => x.ProductId, x => x.SoldQuantity);

            var query = _context.Products
                .Include(p => p.Category)
                .AsQueryable();

            query = stockFilter switch
            {
                "out" => query.Where(p => p.StockQuantity <= 0),
                "low" => query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= 5),
                "high" => query.Where(p => p.StockQuantity >= 100),
                "hidden" => query.Where(p => !p.IsActive),
                "warning" => query.Where(p => p.StockQuantity <= 5 || !p.IsActive),
                _ => query
            };

            var products = await query
                .OrderBy(p => p.StockQuantity)
                .ThenBy(p => p.Name)
                .ToListAsync();

            var items = products
                .Select(p =>
                {
                    soldData.TryGetValue(p.Id, out var soldQuantity);

                    var warning = GetInventoryWarning(p);

                    return new AdminInventoryReportItemViewModel
                    {
                        ProductId = p.Id,
                        ProductName = p.Name,
                        CategoryName = p.Category?.Name ?? "Không có danh mục",
                        StockQuantity = p.StockQuantity,
                        Price = p.Price,
                        SoldQuantity = soldQuantity,
                        IsActive = p.IsActive,
                        WarningText = warning.Text,
                        WarningType = warning.Type
                    };
                })
                .ToList();

            if (take.HasValue)
            {
                items = items.Take(take.Value).ToList();
            }

            return items;
        }

        private static bool IsRevenueOrder(Order order)
        {
            return order.OrderStatus == OrderStatus.Completed ||
                   order.PaymentStatus == PaymentStatus.Paid;
        }

        private static bool IsUserActive(ApplicationUser user)
        {
            return !user.LockoutEnd.HasValue || user.LockoutEnd.Value <= DateTimeOffset.UtcNow;
        }

        private static (string Text, string Type) GetInventoryWarning(Product product)
        {
            if (!product.IsActive)
            {
                return ("Đang ẩn", "hidden");
            }

            if (product.StockQuantity <= 0)
            {
                return ("Hết hàng", "danger");
            }

            if (product.StockQuantity <= 5)
            {
                return ("Sắp hết hàng", "warning");
            }

            if (product.StockQuantity >= 100)
            {
                return ("Tồn kho nhiều", "info");
            }

            return ("Ổn định", "success");
        }
    }
}