using System.ComponentModel.DataAnnotations;
using System.Linq;
using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class CheckoutViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        [StringLength(255)]
        public string ShippingAddress { get; set; } = string.Empty;

        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn phương thức giao hàng")]
        public int ShippingMethodId { get; set; }

        [StringLength(500)]
        public string? OrderNote { get; set; }

        [StringLength(50)]
        public string? CouponCode { get; set; }

        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        public List<CartItemViewModel> CartItems { get; set; } = new();

        public List<ShippingMethod> ShippingMethods { get; set; } = new();

        public decimal ShippingFee { get; set; }

        public decimal DiscountAmount { get; set; }

        public decimal SubTotalAmount => CartItems.Sum(x => x.TotalPrice);

        public decimal TotalAmount => Math.Max(0, SubTotalAmount + ShippingFee - DiscountAmount);
    }
}