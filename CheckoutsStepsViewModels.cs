using System.ComponentModel.DataAnnotations;
using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class CheckoutAddressViewModel
    {
        public bool HasSavedAddress { get; set; }

        public string SavedCustomerName { get; set; } = string.Empty;

        public string SavedEmail { get; set; } = string.Empty;

        public string SavedPhoneNumber { get; set; } = string.Empty;

        public string SavedShippingAddress { get; set; } = string.Empty;

        public bool UseNewAddress { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        public string ShippingAddress { get; set; } = string.Empty;
    }

    public class CheckoutShippingViewModel
    {
        public string SelectedShippingMethod { get; set; } = "pickup";

        public decimal ProductsTotal { get; set; }

        public decimal ShippingFee { get; set; }

        public decimal Total => ProductsTotal + ShippingFee;
    }

    public class CheckoutPaymentViewModel
    {
        public PaymentMethod SelectedPaymentMethod { get; set; } = PaymentMethod.CashOnDelivery;

        public decimal ProductsTotal { get; set; }

        public decimal ShippingFee { get; set; }

        public decimal Total => ProductsTotal + ShippingFee;
        // ===== LƯU Ý: DANH SÁCH TÊN SẢN PHẨM CHUYỂN KHOẢN =====
        public List<string> ProductNames { get; set; } = new();
        // ===== KẾT THÚC DANH SÁCH TÊN SẢN PHẨM CHUYỂN KHOẢN =====
    }

    public class CheckoutConfirmViewModel
    {
        public string CustomerName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public string ShippingMethodName { get; set; } = string.Empty;

        public PaymentMethod PaymentMethod { get; set; }

        public List<CartItemViewModel> CartItems { get; set; } = new();

        public decimal ProductsTotal { get; set; }

        public decimal ShippingFee { get; set; }

        public decimal Total => ProductsTotal + ShippingFee;

        public string? OrderNote { get; set; }
    }
}