using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace MiniSmartstoreMvc.ViewModels
{
    public class AdminCategoryListItemViewModel
    {
        public int Id { get; set; }

        public int? ParentCategoryId { get; set; }

        public int Level { get; set; }

        public bool HasChildren { get; set; }

        public string Name { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string? Alias { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsActive { get; set; }

        public bool ShowOnHomePage { get; set; }

        public bool IncludeInMenu { get; set; }

        public int DisplayOrder { get; set; }

        public int ProductCount { get; set; }
    }

    public class AdminCategoryEditViewModel
    {
        public int Id { get; set; }

        public int? ParentCategoryId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên danh mục")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Alias { get; set; }

        [StringLength(1000)]
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public IFormFile? PictureFile { get; set; }

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

        public int ProductCount { get; set; }

        public List<SelectListItem> ParentCategories { get; set; } = new();
    }
}