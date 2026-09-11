using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class AdminReportViewModel
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public int TotalOrders { get; set; }

        public int PendingOrders { get; set; }

        public int ProcessingOrders { get; set; }

        public int CompletedOrders { get; set; }

        public int CancelledOrders { get; set; }

        public decimal TotalRevenue { get; set; }

        public decimal AverageOrderValue { get; set; }

        public List<TopProductReportItem> TopProducts { get; set; } = new();

        public List<DailyRevenueReportItem> DailyRevenues { get; set; } = new();

        public List<Order> LatestOrders { get; set; } = new();
    }

    public class TopProductReportItem
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int QuantitySold { get; set; }

        public decimal Revenue { get; set; }
    }

    public class DailyRevenueReportItem
    {
        public DateTime Date { get; set; }

        public int OrderCount { get; set; }

        public decimal Revenue { get; set; }
    }
}