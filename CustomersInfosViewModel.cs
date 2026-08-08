using Microsoft.AspNetCore.Mvc.Rendering;
using MiniSmartstoreMvc.Models;
using System.ComponentModel.DataAnnotations;

namespace MiniSmartstoreMvc.ViewModels
{
    public class CustomerInfoViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public bool SubscribeNewsletter { get; set; }

        public int? PreferredShippingMethodId { get; set; }

        public PaymentMethod? PreferredPaymentMethod { get; set; }

        public List<SelectListItem> ShippingMethods { get; set; } = new();

        public List<SelectListItem> PaymentMethods { get; set; } = new();
    }
}