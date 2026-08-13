using Microsoft.AspNetCore.Mvc.Rendering;
using MiniSmartstoreMvc.Models;
using System.ComponentModel.DataAnnotations;

namespace MiniSmartstoreMvc.ViewModels
{
    public class AdminCustomerListViewModel
    {
        public List<AdminCustomerListItemViewModel> Items { get; set; } = new();

        public string? Search { get; set; }
        public string? Role { get; set; }
        public string? Active { get; set; }

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }

        public List<SelectListItem> Roles { get; set; } = new();
    }

    public class AdminCustomerListItemViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string UserName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string RolesText { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public int OrderCount { get; set; }

        public decimal TotalSpent { get; set; }

        public int CartItemCount { get; set; }
        public string UserId
        {
            get => Id;
            set => Id = value;
        }

        public string? Address { get; set; }
    }

    public class AdminCustomerFormViewModel
    {
        public string? Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public string? Password { get; set; }

        public string? ConfirmPassword { get; set; }

        public bool IsActive { get; set; } = true;

        public List<string> SelectedRoles { get; set; } = new();

        public List<SelectListItem> AvailableRoles { get; set; } = new();

        public List<AdminCustomerOrderItemViewModel> Orders { get; set; } = new();

        public List<AdminCustomerCartItemViewModel> CartItems { get; set; } = new();

        public AdminCustomerAddressViewModel AddressInfo { get; set; } = new();
        public int? PreferredShippingMethodId { get; set; }

        public PaymentMethod? PreferredPaymentMethod { get; set; }

        public List<SelectListItem> ShippingMethods { get; set; } = new();

        public List<SelectListItem> PaymentMethods { get; set; } = new();
    }

    public class AdminCustomerOrderItemViewModel
    {
        public int Id { get; set; }

        public string OrderCode { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public decimal TotalAmount { get; set; }

        public string OrderStatusText { get; set; } = string.Empty;

        public string PaymentStatusText { get; set; } = string.Empty;
    }

    public class AdminCustomerCartItemViewModel
    {
        public string ProductName { get; set; } = string.Empty;

        public bool ProductActive { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal TotalPrice { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }

    public class AdminCustomerAddressViewModel
    {
        public string CustomerName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string PhoneNumber { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;
    }
}