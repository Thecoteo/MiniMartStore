using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniSmartstoreMvc.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tên sản phẩm không được để trống")]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000)]
        public string? Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? OldPrice { get; set; }
        public ICollection<ProductColor> ProductColors { get; set; } = new List<ProductColor>();
        public int StockQuantity { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsFeatured { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        [StringLength(50)]
        public string? ProductCode { get; set; }

        [StringLength(300)]
        public string? ShortDescription { get; set; }

        [StringLength(200)]
        public string? Alias { get; set; }

        [StringLength(200)]
        public string? SeoTitle { get; set; }

        [StringLength(500)]
        public string? SeoDescription { get; set; }

        [StringLength(500)]
        public string? SeoKeywords { get; set; }

        public int DisplayOrder { get; set; } = 0;

        [StringLength(100)]
        public string? DeliveryTime { get; set; } = "3 - 5 ngày";

        public int LowStockThreshold { get; set; } = 5;

        public bool AllowCustomerReviews { get; set; } = true;

        public DateTime? AvailableStartDate { get; set; }

        public DateTime? AvailableEndDate { get; set; }

        [StringLength(1000)]
        public string? AdminComment { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public int CategoryId { get; set; }

        public Category? Category { get; set; }

        public ICollection<ProductImage> ProductImages { get; set; } = new List<ProductImage>();
        public ICollection<ProductReview> ProductReviews { get; set; } = new List<ProductReview>();
    }
}