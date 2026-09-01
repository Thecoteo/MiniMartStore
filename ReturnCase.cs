using System.ComponentModel.DataAnnotations;

namespace MiniSmartstoreMvc.Models
{
    public enum ReturnCaseType
    {
        Return = 1,
        Withdrawal = 2
    }

    public enum ReturnCaseStatus
    {
        Pending = 1,
        Received = 2,
        ReturnAuthorized = 3,
        ItemsRepaired = 4,
        ItemsRefunded = 5,
        RequestRejected = 6,
        Cancelled = 7
    }

    public class ReturnCase
    {
        public int Id { get; set; }

        public ReturnCaseType Type { get; set; } = ReturnCaseType.Return;

        public ReturnCaseStatus Status { get; set; } = ReturnCaseStatus.Pending;

        public int OrderId { get; set; }

        public Order? Order { get; set; }

        public int ProductId { get; set; }

        public Product? Product { get; set; }

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        public int Quantity { get; set; } = 1;

        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [StringLength(150)]
        public string CustomerEmail { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Reason { get; set; }

        [StringLength(1000)]
        public string? StaffNote { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public DateTime? UpdatedAt { get; set; }
    }
}