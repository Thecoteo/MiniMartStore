using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace MiniSmartstoreMvc.Models
{
    public class ApplicationUser : IdentityUser
    {
        [StringLength(100)]
        public string? FullName { get; set; }

        [StringLength(255)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int? PreferredShippingMethodId { get; set; }

        public ShippingMethod? PreferredShippingMethod { get; set; }

        public PaymentMethod? PreferredPaymentMethod { get; set; }
    }
}