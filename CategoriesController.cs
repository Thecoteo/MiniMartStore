using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.ViewModels;

namespace MiniSmartstoreMvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("admin/category")]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public CategoryController(
            ApplicationDbContext context,
            IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        [HttpGet("")]
        [HttpGet("list")]
        public async Task<IActionResult> List(string? search, int page = 1, int pageSize = 25)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize != 10 && pageSize != 25 && pageSize != 50)
            {
                pageSize = 25;
            }

            var query = _context.Categories
                .Include(c => c.Products)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(c =>
                    c.Name.Contains(search) ||
                    (c.Alias != null && c.Alias.Contains(search)));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var categories = await query
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var model = categories.Select(c => new AdminCategoryListItemViewModel
            {
                Id = c.Id,
                Name = c.Name,
                FullName = c.Name,
                Alias = c.Alias,
                ProductCount = c.Products.Count,
                IsActive = c.IsActive,
                DisplayOrder = c.DisplayOrder,
                IncludeInMenu = c.IncludeInMenu,
                ShowOnHomePage = c.ShowOnHomePage,
                Level = 0
            }).ToList();

            ViewBag.Search = search;
            ViewBag.Page = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            ViewBag.TotalPages = totalPages == 0 ? 1 : totalPages;

            return View(model);
        }

        [HttpGet("tree")]
        public async Task<IActionResult> Tree()
        {
            var categories = await _context.Categories
                .Include(c => c.Products)
                .AsNoTracking()
                .ToListAsync();

            var items = BuildCategoryList(categories);

            return View(items);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create(int? parentId)
        {
            var model = new AdminCategoryEditViewModel
            {
                ParentCategoryId = parentId,
                IsActive = true,
                ShowOnHomePage = true,
                IncludeInMenu = true,
                ParentCategories = await GetParentCategorySelectListAsync()
            };

            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCategoryEditViewModel model, string saveMode = "save")
        {
            ModelState.Remove(nameof(model.ParentCategories));
            ModelState.Remove(nameof(model.PictureFile));

            if (!ModelState.IsValid)
            {
                model.ParentCategories = await GetParentCategorySelectListAsync();
                return View(model);
            }

            var category = new Category
            {
                ParentCategoryId = model.ParentCategoryId,
                Name = model.Name.Trim(),
                Alias = string.IsNullOrWhiteSpace(model.Alias)
                    ? CreateAlias(model.Name)
                    : CreateAlias(model.Alias),
                Description = model.Description,
                ImageUrl = await SaveImageAsync(model.PictureFile),
                IsActive = model.IsActive,
                ShowOnHomePage = model.ShowOnHomePage,
                IncludeInMenu = model.IncludeInMenu,
                DisplayOrder = model.DisplayOrder,
                BadgeText = model.BadgeText,
                MetaTitle = model.MetaTitle,
                MetaDescription = model.MetaDescription,
                MetaKeywords = model.MetaKeywords
            };

            _context.Categories.Add(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã thêm danh mục mới.";

            if (saveMode == "continue")
            {
                return RedirectToAction(nameof(Edit), new { id = category.Id });
            }

            return RedirectToAction(nameof(List));
        }

        [HttpGet("edit/{id:int}")]
        public async Task<IActionResult> Edit(int id)
        {
            var category = await _context.Categories
                .Include(c => c.Products)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            var model = new AdminCategoryEditViewModel
            {
                Id = category.Id,
                ParentCategoryId = category.ParentCategoryId,
                Name = category.Name,
                Alias = category.Alias,
                Description = category.Description,
                ImageUrl = category.ImageUrl,
                IsActive = category.IsActive,
                ShowOnHomePage = category.ShowOnHomePage,
                IncludeInMenu = category.IncludeInMenu,
                DisplayOrder = category.DisplayOrder,
                BadgeText = category.BadgeText,
                MetaTitle = category.MetaTitle,
                MetaDescription = category.MetaDescription,
                MetaKeywords = category.MetaKeywords,
                ProductCount = category.Products.Count,
                ParentCategories = await GetParentCategorySelectListAsync(category.Id)
            };

            return View(model);
        }

        [HttpPost("edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, AdminCategoryEditViewModel model, string saveMode = "save")
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(model.ParentCategories));
            ModelState.Remove(nameof(model.PictureFile));

            if (model.ParentCategoryId == model.Id)
            {
                ModelState.AddModelError(nameof(model.ParentCategoryId), "Danh mục cha không được là chính nó.");
            }

            if (!ModelState.IsValid)
            {
                model.ParentCategories = await GetParentCategorySelectListAsync(model.Id);
                return View(model);
            }

            var category = await _context.Categories
                .FirstOrDefaultAsync(c => c.Id == id);

            if (category == null)
            {
                return NotFound();
            }

            category.ParentCategoryId = model.ParentCategoryId;
            category.Name = model.Name.Trim();
            category.Alias = string.IsNullOrWhiteSpace(model.Alias)
                ? CreateAlias(model.Name)
                : CreateAlias(model.Alias);
            category.Description = model.Description;
            category.IsActive = model.IsActive;
            category.ShowOnHomePage = model.ShowOnHomePage;
            category.IncludeInMenu = model.IncludeInMenu;
            category.DisplayOrder = model.DisplayOrder;
            category.BadgeText = model.BadgeText;
            category.MetaTitle = model.MetaTitle;
            category.MetaDescription = model.MetaDescription;
            category.MetaKeywords = model.MetaKeywords;

            var newImageUrl = await SaveImageAsync(model.PictureFile);

            if (!string.IsNullOrWhiteSpace(newImageUrl))
            {
                category.ImageUrl = newImageUrl;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã cập nhật danh mục.";

            if (saveMode == "continue")
            {
                return RedirectToAction(nameof(Edit), new { id = category.Id });
            }

            return RedirectToAction(nameof(List));
        }

        [HttpPost("delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var hasChildren = await _context.Categories.AnyAsync(c => c.ParentCategoryId == id);
            var hasProducts = await _context.Products.AnyAsync(p => p.CategoryId == id);

            if (hasChildren)
            {
                TempData["Error"] = "Không thể xóa vì danh mục này đang có danh mục con.";
                return RedirectToAction(nameof(List));
            }

            if (hasProducts)
            {
                TempData["Error"] = "Không thể xóa vì danh mục này đang có sản phẩm.";
                return RedirectToAction(nameof(List));
            }

            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa danh mục.";

            return RedirectToAction(nameof(List));
        }

        [HttpPost("toggle-published/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePublished(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
            {
                return NotFound();
            }

            category.IsActive = !category.IsActive;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(List));
        }

        private async Task<List<SelectListItem>> GetParentCategorySelectListAsync(int? excludeId = null)
        {
            var categories = await _context.Categories
                .AsNoTracking()
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var items = BuildCategoryList(categories)
                .Where(c => excludeId == null || c.Id != excludeId.Value)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = new string('—', c.Level * 2) + " " + c.Name
                })
                .ToList();

            items.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "[Không có danh mục cha]"
            });

            return items;
        }

        private List<AdminCategoryListItemViewModel> BuildCategoryList(List<Category> categories)
        {
            var lookup = categories.ToLookup(c => c.ParentCategoryId);
            var result = new List<AdminCategoryListItemViewModel>();

            void AddChildren(int? parentId, int level, string parentPath)
            {
                var children = lookup[parentId]
                    .OrderBy(c => c.DisplayOrder)
                    .ThenBy(c => c.Name)
                    .ToList();

                foreach (var category in children)
                {
                    var fullName = string.IsNullOrWhiteSpace(parentPath)
                        ? category.Name
                        : $"{parentPath} > {category.Name}";

                    result.Add(new AdminCategoryListItemViewModel
                    {
                        Id = category.Id,
                        ParentCategoryId = category.ParentCategoryId,
                        Level = level,
                        HasChildren = lookup[category.Id].Any(),
                        Name = category.Name,
                        FullName = fullName,
                        Alias = category.Alias,
                        ImageUrl = category.ImageUrl,
                        IsActive = category.IsActive,
                        ShowOnHomePage = category.ShowOnHomePage,
                        IncludeInMenu = category.IncludeInMenu,
                        DisplayOrder = category.DisplayOrder,
                        ProductCount = category.Products?.Count ?? 0
                    });

                    AddChildren(category.Id, level + 1, fullName);
                }
            }

            AddChildren(null, 0, "");

            return result;
        }

        private async Task<string?> SaveImageAsync(IFormFile? file)
        {
            if (file == null || file.Length == 0)
            {
                return null;
            }

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLower();

            if (!allowedExtensions.Contains(extension))
            {
                return null;
            }

            var folder = Path.Combine(_environment.WebRootPath, "images", "categories");

            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }

            var fileName = $"{Guid.NewGuid():N}{extension}";
            var path = Path.Combine(folder, fileName);

            await using var stream = new FileStream(path, FileMode.Create);
            await file.CopyToAsync(stream);

            return $"/images/categories/{fileName}";
        }

        private static string CreateAlias(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return string.Empty;
            }

            text = text.Trim().ToLowerInvariant();

            var normalized = text.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder();

            foreach (var c in normalized)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);

                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(c);
                }
            }

            var result = builder.ToString().Normalize(NormalizationForm.FormC);
            result = result.Replace("đ", "d");
            result = Regex.Replace(result, @"[^a-z0-9\s-]", "");
            result = Regex.Replace(result, @"\s+", "-");
            result = Regex.Replace(result, @"-+", "-");

            return result.Trim('-');
        }
    }
}