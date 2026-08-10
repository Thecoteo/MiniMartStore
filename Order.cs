using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniSmartstoreMvc.Models
{
    public class Order
    {
        public int Id { get; set; }

        public string OrderCode { get; set; } = "ORD" + DateTime.Now.Ticks;

        public string UserId { get; set; } = string.Empty;

        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string ShippingAddress { get; set; } = string.Empty;

        public int? ShippingMethodId { get; set; }

        public ShippingMethod? ShippingMethod { get; set; }

        [StringLength(100)]
        public string? ShippingMethodName { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal SubTotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal ShippingFee { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountAmount { get; set; }

        [StringLength(50)]
        public string? CouponCode { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [StringLength(500)]
        public string? OrderNote { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;

        public OrderStatus OrderStatus { get; set; } = OrderStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public ICollection<OrderDetail> OrderDetails { get; set; } = new List<OrderDetail>();

        public Payment? Payment { get; set; }
    }
}