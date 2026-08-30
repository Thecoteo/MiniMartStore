using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MiniSmartstoreMvc.Models
{
    public enum ProductRuleTargetType
    {
        AllProducts = 1,
        Category = 2,
        Product = 3
    }

    public enum ProductRuleActionType
    {
        Discount = 1,
        MarkAsFeatured = 2,
        HideProduct = 3,
        ShowProduct = 4
    }

    public class ProductRule
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên quy tắc")]
        [StringLength(150)]
        public string RuleName { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public ProductRuleActionType ActionType { get; set; } = ProductRuleActionType.Discount;

        public ProductRuleTargetType TargetType { get; set; } = ProductRuleTargetType.AllProducts;

        public int? CategoryId { get; set; }

        public Category? Category { get; set; }

        public int? ProductId { get; set; }

        public Product? Product { get; set; }

        public DiscountType DiscountType { get; set; } = DiscountType.Percentage;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        public DateTime StartDate { get; set; } = DateTime.Now;

        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(7);

        public int Priority { get; set; } = 1;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}