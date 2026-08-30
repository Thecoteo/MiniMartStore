using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class AdminReturnCaseListItemViewModel
    {
        public int Id { get; set; }

        public ReturnCaseType Type { get; set; }

        public ReturnCaseStatus Status { get; set; }

        public int OrderId { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public string? Reason { get; set; }
    }
}