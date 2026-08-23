using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class AdminReportOverviewViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int PendingOrders { get; set; }
        public int SoldQuantity { get; set; }
        public int NewCustomers { get; set; }

        public List<AdminReportChartPointViewModel> RevenueLast7Days { get; set; } = new();
        public List<AdminReportChartPointViewModel> RevenueLast30Days { get; set; } = new();

        public List<AdminProductReportItemViewModel> TopProducts { get; set; } = new();
        public List<AdminCustomerReportItemViewModel> TopCustomers { get; set; } = new();
        public List<AdminInventoryReportItemViewModel> InventoryWarnings { get; set; } = new();
    }

    public class AdminSalesReportViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public OrderStatus? OrderStatus { get; set; }
        public PaymentStatus? PaymentStatus { get; set; }

        public int TotalOrders { get; set; }
        public int TotalSoldQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }

        public List<AdminSalesReportRowViewModel> Rows { get; set; } = new();
    }

    public class AdminSalesReportRowViewModel
    {
        public DateTime Date { get; set; }
        public int OrderCount { get; set; }
        public int SoldQuantity { get; set; }
        public decimal Revenue { get; set; }
        public int CompletedOrders { get; set; }
        public int CancelledOrders { get; set; }
    }

    public class AdminProductReportViewModel
    {
        public string SortBy { get; set; } = "quantity";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int TotalProducts { get; set; }
        public int TotalSoldQuantity { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<AdminProductReportItemViewModel> Items { get; set; } = new();
    }

    public class AdminProductReportItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int SoldQuantity { get; set; }
        public decimal Revenue { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminCustomerReportViewModel
    {
        public string SortBy { get; set; } = "amount";
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        public int TotalCustomers { get; set; }
        public int CustomersWithOrders { get; set; }
        public int CustomersWithoutOrders { get; set; }
        public decimal TotalRevenue { get; set; }

        public List<AdminCustomerReportItemViewModel> Items { get; set; } = new();
    }

    public class AdminCustomerReportItemViewModel
    {
        public string UserId { get; set; } = "";
        public string CustomerName { get; set; } = "";
        public string Email { get; set; } = "";
        public int OrderCount { get; set; }
        public decimal TotalSpent { get; set; }
        public DateTime? LastOrderAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class AdminInventoryReportViewModel
    {
        public string StockFilter { get; set; } = "all";

        public int TotalProducts { get; set; }
        public int OutOfStockCount { get; set; }
        public int LowStockCount { get; set; }
        public int HighStockCount { get; set; }
        public int HiddenProductCount { get; set; }

        public List<AdminInventoryReportItemViewModel> Items { get; set; } = new();
    }

    public class AdminInventoryReportItemViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string CategoryName { get; set; } = "";
        public int StockQuantity { get; set; }
        public decimal Price { get; set; }
        public int SoldQuantity { get; set; }
        public bool IsActive { get; set; }
        public string WarningText { get; set; } = "";
        public string WarningType { get; set; } = "";
    }

    public class AdminReportChartPointViewModel
    {
        public string Label { get; set; } = "";
        public decimal Amount { get; set; }
        public int Count { get; set; }
    }
}