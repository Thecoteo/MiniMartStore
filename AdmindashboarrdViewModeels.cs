using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class AdminDashboardViewModel
    {
        public DashboardCircleMetric IncompleteToday { get; set; } = new();
        public DashboardCircleMetric IncompleteLast7Days { get; set; } = new();
        public DashboardCircleMetric IncompleteLast28Days { get; set; } = new();
        public DashboardCircleMetric IncompleteThisYear { get; set; } = new();

        public decimal CompletedAmount { get; set; }
        public decimal ProcessingAmount { get; set; }
        public decimal PendingAmount { get; set; }
        public decimal CancelledAmount { get; set; }

        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }
        public int CustomerCount { get; set; }
        public int OrderCount { get; set; }
        public int CartItemCount { get; set; }

        public decimal TotalRevenue { get; set; }

        public List<DashboardBestSellerItem> BestSellers { get; set; } = new();
        public List<DashboardTopCustomerItem> TopCustomers { get; set; } = new();
        public List<Order> LatestOrders { get; set; } = new();

        public List<DashboardPeriodData> OrderPeriods { get; set; } = new();

        public List<DashboardPeriodData> RegistrationPeriods { get; set; } = new();
    }

    public class DashboardCircleMetric
    {
        public string Title { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal Amount { get; set; }
    }

    public class DashboardBestSellerItem
    {
        public string ProductName { get; set; } = string.Empty;

        public int QuantitySold { get; set; }

        public decimal Amount { get; set; }
    }

    public class DashboardTopCustomerItem
    {
        public string CustomerName { get; set; } = string.Empty;

        public int OrderCount { get; set; }

        public decimal Amount { get; set; }
    }

    public class DashboardPeriodData
    {
        public string Key { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal TotalAmount { get; set; }

        public decimal CompletedAmount { get; set; }

        public decimal ProcessingAmount { get; set; }

        public decimal PendingAmount { get; set; }

        public decimal CancelledAmount { get; set; }

        public List<DashboardChartPoint> Points { get; set; } = new();
    }

    public class DashboardChartPoint
    {
        public string Label { get; set; } = string.Empty;

        public int Count { get; set; }

        public decimal Amount { get; set; }

        public string Tooltip { get; set; } = string.Empty;
    }
}