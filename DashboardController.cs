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
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var yesterday = today.AddDays(-1);
            var last7Days = today.AddDays(-6);
            var last28Days = today.AddDays(-27);
            var yearStart = new DateTime(today.Year, 1, 1);

            var incompleteToday = await GetIncompleteMetricAsync("Hôm nay", today, tomorrow);
            var incompleteLast7Days = await GetIncompleteMetricAsync("7 ngày qua", last7Days, tomorrow);
            var incompleteLast28Days = await GetIncompleteMetricAsync("28 ngày qua", last28Days, tomorrow);
            var incompleteThisYear = await GetIncompleteMetricAsync("Năm nay", yearStart, tomorrow);

            var allOrders = await _context.Orders.ToListAsync();

            var revenueOrders = allOrders
                .Where(o => o.OrderStatus == OrderStatus.Completed || o.PaymentStatus == PaymentStatus.Paid)
                .ToList();

            var bestSellers = await _context.OrderDetails
                .Include(od => od.Product)
                .Include(od => od.Order)
                .Where(od =>
                    od.Product != null &&
                    od.Order != null &&
                    (od.Order.OrderStatus == OrderStatus.Completed ||
                     od.Order.PaymentStatus == PaymentStatus.Paid))
                .GroupBy(od => new
                {
                    od.ProductId,
                    ProductName = od.Product!.Name
                })
                .Select(g => new DashboardBestSellerItem
                {
                    ProductName = g.Key.ProductName,
                    QuantitySold = g.Sum(x => x.Quantity),
                    Amount = g.Sum(x => x.Quantity * x.UnitPrice)
                })
                .OrderByDescending(x => x.QuantitySold)
                .Take(5)
                .ToListAsync();

            var topCustomers = await _context.Orders
                .Where(o => o.OrderStatus == OrderStatus.Completed || o.PaymentStatus == PaymentStatus.Paid)
                .GroupBy(o => new
                {
                    o.UserId,
                    o.CustomerName
                })
                .Select(g => new DashboardTopCustomerItem
                {
                    CustomerName = g.Key.CustomerName,
                    OrderCount = g.Count(),
                    Amount = g.Sum(x => x.TotalAmount)
                })
                .OrderByDescending(x => x.Amount)
                .Take(5)
                .ToListAsync();

            var latestOrders = await _context.Orders
                .OrderByDescending(o => o.CreatedAt)
                .Take(8)
                .ToListAsync();

            var orderToday = await GetOrderPeriodDataAsync("today", "Hôm nay", today, tomorrow, "hour");
            var orderYesterday = await GetOrderPeriodDataAsync("yesterday", "Hôm qua", yesterday, today, "hour");
            var orderLast7 = await GetOrderPeriodDataAsync("last7", "7 ngày qua", last7Days, tomorrow, "day");
            var orderLast28 = await GetOrderPeriodDataAsync("last28", "28 ngày qua", last28Days, tomorrow, "day");
            var orderYear = await GetOrderPeriodDataAsync("year", "Năm nay", yearStart, tomorrow, "month");

            var regToday = await GetRegistrationPeriodDataAsync("today", "Hôm nay", today, tomorrow, "hour");
            var regYesterday = await GetRegistrationPeriodDataAsync("yesterday", "Hôm qua", yesterday, today, "hour");
            var regLast7 = await GetRegistrationPeriodDataAsync("last7", "7 ngày qua", last7Days, tomorrow, "day");
            var regLast28 = await GetRegistrationPeriodDataAsync("last28", "28 ngày qua", last28Days, tomorrow, "day");
            var regYear = await GetRegistrationPeriodDataAsync("year", "Năm nay", yearStart, tomorrow, "month");

            var model = new AdminDashboardViewModel
            {
                IncompleteToday = incompleteToday,
                IncompleteLast7Days = incompleteLast7Days,
                IncompleteLast28Days = incompleteLast28Days,
                IncompleteThisYear = incompleteThisYear,

                CompletedAmount = allOrders
                    .Where(o => o.OrderStatus == OrderStatus.Completed)
                    .Sum(o => o.TotalAmount),

                ProcessingAmount = allOrders
                    .Where(o => o.OrderStatus == OrderStatus.Processing)
                    .Sum(o => o.TotalAmount),

                PendingAmount = allOrders
                    .Where(o => o.OrderStatus == OrderStatus.Pending)
                    .Sum(o => o.TotalAmount),

                CancelledAmount = allOrders
                    .Where(o => o.OrderStatus == OrderStatus.Cancelled)
                    .Sum(o => o.TotalAmount),

                ProductCount = await _context.Products.CountAsync(),
                CategoryCount = await _context.Categories.CountAsync(),
                CustomerCount = await _context.Users.CountAsync(),
                OrderCount = await _context.Orders.CountAsync(),
                CartItemCount = await _context.CartItems.SumAsync(c => (int?)c.Quantity) ?? 0,

                TotalRevenue = revenueOrders.Sum(o => o.TotalAmount),

                BestSellers = bestSellers,
                TopCustomers = topCustomers,
                LatestOrders = latestOrders,

                OrderPeriods = new List<DashboardPeriodData>
                {
                    orderToday,
                    orderYesterday,
                    orderLast7,
                    orderLast28,
                    orderYear
                },

                RegistrationPeriods = new List<DashboardPeriodData>
                {
                    regToday,
                    regYesterday,
                    regLast7,
                    regLast28,
                    regYear
                }
            };

            return View(model);
        }

        private async Task<DashboardCircleMetric> GetIncompleteMetricAsync(string title, DateTime fromDate, DateTime toDate)
        {
            var orders = await _context.Orders
                .Where(o =>
                    o.CreatedAt >= fromDate &&
                    o.CreatedAt < toDate &&
                    (o.OrderStatus == OrderStatus.Pending ||
                     o.OrderStatus == OrderStatus.Processing))
                .ToListAsync();

            return new DashboardCircleMetric
            {
                Title = title,
                Count = orders.Count,
                Amount = orders.Sum(o => o.TotalAmount)
            };
        }

        private async Task<DashboardPeriodData> GetOrderPeriodDataAsync(
            string key,
            string label,
            DateTime fromDate,
            DateTime toDate,
            string mode)
        {
            var orders = await _context.Orders
                .Where(o => o.CreatedAt >= fromDate && o.CreatedAt < toDate)
                .ToListAsync();

            return new DashboardPeriodData
            {
                Key = key,
                Label = label,
                Count = orders.Count,
                TotalAmount = orders.Sum(o => o.TotalAmount),

                CompletedAmount = orders
                    .Where(o => o.OrderStatus == OrderStatus.Completed)
                    .Sum(o => o.TotalAmount),

                ProcessingAmount = orders
                    .Where(o => o.OrderStatus == OrderStatus.Processing)
                    .Sum(o => o.TotalAmount),

                PendingAmount = orders
                    .Where(o => o.OrderStatus == OrderStatus.Pending)
                    .Sum(o => o.TotalAmount),

                CancelledAmount = orders
                    .Where(o => o.OrderStatus == OrderStatus.Cancelled)
                    .Sum(o => o.TotalAmount),

                Points = BuildOrderChartPoints(orders, fromDate, toDate, mode)
            };
        }

        private async Task<DashboardPeriodData> GetRegistrationPeriodDataAsync(
            string key,
            string label,
            DateTime fromDate,
            DateTime toDate,
            string mode)
        {
            var users = await _context.Users
                .Where(u => u.CreatedAt >= fromDate && u.CreatedAt < toDate)
                .ToListAsync();

            return new DashboardPeriodData
            {
                Key = key,
                Label = label,
                Count = users.Count,
                TotalAmount = users.Count,
                Points = BuildRegistrationChartPoints(users, fromDate, toDate, mode)
            };
        }

        private List<DashboardChartPoint> BuildOrderChartPoints(
            List<Order> orders,
            DateTime fromDate,
            DateTime toDate,
            string mode)
        {
            var result = new List<DashboardChartPoint>();

            if (mode == "hour")
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    var hourOrders = orders
                        .Where(o => o.CreatedAt.Hour == hour)
                        .ToList();

                    var amount = hourOrders.Sum(o => o.TotalAmount);

                    result.Add(new DashboardChartPoint
                    {
                        Label = $"{hour:00}:00",
                        Count = hourOrders.Count,
                        Amount = amount,
                        Tooltip = $"{hour:00}:00 - {hour:00}:59 | Đơn hàng: {hourOrders.Count} | Tổng tiền: {amount:N0} đ"
                    });
                }

                return result;
            }

            if (mode == "month")
            {
                for (int month = 1; month <= 12; month++)
                {
                    var monthOrders = orders
                        .Where(o => o.CreatedAt.Month == month)
                        .ToList();

                    var amount = monthOrders.Sum(o => o.TotalAmount);

                    result.Add(new DashboardChartPoint
                    {
                        Label = $"T{month}",
                        Count = monthOrders.Count,
                        Amount = amount,
                        Tooltip = $"Tháng {month} | Đơn hàng: {monthOrders.Count} | Tổng tiền: {amount:N0} đ"
                    });
                }

                return result;
            }

            for (var date = fromDate.Date; date < toDate.Date; date = date.AddDays(1))
            {
                var dayOrders = orders
                    .Where(o => o.CreatedAt.Date == date)
                    .ToList();

                var amount = dayOrders.Sum(o => o.TotalAmount);

                result.Add(new DashboardChartPoint
                {
                    Label = date.ToString("dd/MM"),
                    Count = dayOrders.Count,
                    Amount = amount,
                    Tooltip = $"{date:dd/MM/yyyy} | Đơn hàng: {dayOrders.Count} | Tổng tiền: {amount:N0} đ"
                });
            }

            return result;
        }

        private List<DashboardChartPoint> BuildRegistrationChartPoints(
            List<ApplicationUser> users,
            DateTime fromDate,
            DateTime toDate,
            string mode)
        {
            var result = new List<DashboardChartPoint>();

            if (mode == "hour")
            {
                for (int hour = 0; hour < 24; hour++)
                {
                    var count = users.Count(u => u.CreatedAt.Hour == hour);

                    result.Add(new DashboardChartPoint
                    {
                        Label = $"{hour:00}:00",
                        Count = count,
                        Amount = count,
                        Tooltip = $"{hour:00}:00 - {hour:00}:59 | Đăng ký: {count}"
                    });
                }

                return result;
            }

            if (mode == "month")
            {
                for (int month = 1; month <= 12; month++)
                {
                    var count = users.Count(u => u.CreatedAt.Month == month);

                    result.Add(new DashboardChartPoint
                    {
                        Label = $"T{month}",
                        Count = count,
                        Amount = count,
                        Tooltip = $"Tháng {month} | Đăng ký: {count}"
                    });
                }

                return result;
            }

            for (var date = fromDate.Date; date < toDate.Date; date = date.AddDays(1))
            {
                var count = users.Count(u => u.CreatedAt.Date == date);

                result.Add(new DashboardChartPoint
                {
                    Label = date.ToString("dd/MM"),
                    Count = count,
                    Amount = count,
                    Tooltip = $"{date:dd/MM/yyyy} | Đăng ký: {count}"
                });
            }

            return result;
        }
    }
}