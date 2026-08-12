using System.ComponentModel.DataAnnotations;

namespace MiniSmartstoreMvc.Models
{
    public class Category
    {
        public int Id { get; set; }

        public int? ParentCategoryId { get; set; }

        public Category? ParentCategory { get; set; }

        public ICollection<Category> Children { get; set; } = new List<Category>();

        [Required(ErrorMessage = "Tên danh mục không được để trống")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Alias { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; } = true;

        public bool ShowOnHomePage { get; set; } = true;

        public bool IncludeInMenu { get; set; } = true;

        public int DisplayOrder { get; set; } = 0;

        [StringLength(50)]
        public string? BadgeText { get; set; }

        [StringLength(200)]
        public string? MetaTitle { get; set; }

        [StringLength(500)]
        public string? MetaDescription { get; set; }

        [StringLength(300)]
        public string? MetaKeywords { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}