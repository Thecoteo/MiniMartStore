using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;

namespace MiniSmartstoreMvc.ViewComponents
{
    public class CategoryMenuViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CategoryMenuViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .Where(c => c.IsActive && c.IncludeInMenu)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var menuItems = BuildCategoryTree(categories, null);

            return View(menuItems);
        }

        private List<CategoryMenuItemViewModel> BuildCategoryTree(List<Category> categories, int? parentId)
        {
            return categories
                .Where(c => c.ParentCategoryId == parentId)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Select(c => new CategoryMenuItemViewModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Alias = c.Alias,
                    ProductCount = c.Products?.Count(p => p.IsActive) ?? 0,
                    Children = BuildCategoryTree(categories, c.Id)
                })
                .ToList();
        }
    }
}