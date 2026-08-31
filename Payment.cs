using System.ComponentModel.DataAnnotations.Schema;

namespace MiniSmartstoreMvc.Models
{
    public class Payment
    {
        public int Id { get; set; }

        public int OrderId { get; set; }

        public Order? Order { get; set; }

        public PaymentMethod PaymentMethod { get; set; }

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string? TransactionCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}