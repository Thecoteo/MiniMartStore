using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using System.Security.Claims;
namespace MiniSmartstoreMvc.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? search,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? priceRange,
            string? stockStatus,
            string? sortOrder,
            string? viewMode)
        {
            viewMode = string.IsNullOrWhiteSpace(viewMode) ? "grid" : viewMode;
            sortOrder = string.IsNullOrWhiteSpace(sortOrder) ? "featured" : sortOrder;

            var categories = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var activeProductsForCount = await _context.Products
                .Where(p => p.IsActive)
                .Select(p => new
                {
                    p.Id,
                    p.CategoryId
                })
                .ToListAsync();

            var categoryCounts = categories.ToDictionary(
                c => c.Id,
                c =>
                {
                    var categoryAndChildIds = GetCategoryAndChildIds(categories, c.Id);

                    return activeProductsForCount.Count(p =>
                        categoryAndChildIds.Contains(p.CategoryId));
                }
            );

            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductColors)
                .Where(p => p.IsActive)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim();

                query = query.Where(p =>
                    p.Name.Contains(search) ||
                    (p.Description != null && p.Description.Contains(search)));
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                var categoryAndChildIds = GetCategoryAndChildIds(categories, categoryId.Value);

                query = query.Where(p =>
                    categoryAndChildIds.Contains(p.CategoryId));
            }

            if (!string.IsNullOrWhiteSpace(priceRange))
            {
                switch (priceRange)
                {
                    case "under_1m":
                        minPrice = null;
                        maxPrice = 1000000;
                        break;

                    case "1m_5m":
                        minPrice = 1000000;
                        maxPrice = 5000000;
                        break;

                    case "5m_10m":
                        minPrice = 5000000;
                        maxPrice = 10000000;
                        break;

                    case "10m_20m":
                        minPrice = 10000000;
                        maxPrice = 20000000;
                        break;

                    case "over_20m":
                        minPrice = 20000000;
                        maxPrice = null;
                        break;
                }
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            if (!string.IsNullOrWhiteSpace(stockStatus))
            {
                if (stockStatus == "in_stock")
                {
                    query = query.Where(p => p.StockQuantity > 0);
                }
                else if (stockStatus == "out_stock")
                {
                    query = query.Where(p => p.StockQuantity <= 0);
                }
            }

            query = sortOrder switch
            {
                "price_asc" => query.OrderBy(p => p.Price),

                "price_desc" => query.OrderByDescending(p => p.Price),

                "newest" => query.OrderByDescending(p => p.CreatedAt),

                "name_asc" => query.OrderBy(p => p.Name),

                "name_desc" => query.OrderByDescending(p => p.Name),

                "featured" => query
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.CreatedAt),

                _ => query
                    .OrderByDescending(p => p.IsFeatured)
                    .ThenByDescending(p => p.CreatedAt)
            };

            var products = await query.ToListAsync();

            ViewBag.Categories = categories;
            ViewBag.CategoryCounts = categoryCounts;

            ViewBag.Search = search;
            ViewBag.CategoryId = categoryId;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.PriceRange = priceRange;
            ViewBag.StockStatus = stockStatus;
            ViewBag.SortOrder = sortOrder;
            ViewBag.ViewMode = viewMode;

            return View(products);
        }
        [HttpGet]
        public async Task<IActionResult> Reviews(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductReviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            return View(product);
        }
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddReview(int productId, int rating, string title, string comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var product = await _context.Products
                .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            title = title?.Trim() ?? string.Empty;
            comment = comment?.Trim() ?? string.Empty;

            if (rating < 1 || rating > 5)
            {
                TempData["ReviewError"] = "Số sao đánh giá không hợp lệ.";
                return RedirectToProductReviews(productId);
            }

            if (string.IsNullOrWhiteSpace(title))
            {
                TempData["ReviewError"] = "Vui lòng nhập tiêu đề đánh giá.";
                return RedirectToProductReviews(productId);
            }

            if (title.Length > 150)
            {
                TempData["ReviewError"] = "Tiêu đề đánh giá không được vượt quá 150 ký tự.";
                return RedirectToProductReviews(productId);
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ReviewError"] = "Vui lòng nhập nội dung đánh giá.";
                return RedirectToProductReviews(productId);
            }

            if (comment.Length > 2000)
            {
                TempData["ReviewError"] = "Nội dung đánh giá không được vượt quá 2000 ký tự.";
                return RedirectToProductReviews(productId);
            }

            var hasReviewed = await _context.ProductReviews
                .AnyAsync(r => r.ProductId == productId && r.UserId == userId);

            if (hasReviewed)
            {
                TempData["ReviewError"] = "Bạn đã đánh giá sản phẩm này rồi.";
                return RedirectToProductReviews(productId);
            }

            var review = new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                Rating = rating,
                Title = title,
                Comment = comment,
                IsApproved = true,
                CreatedAt = DateTime.Now
            };

            _context.ProductReviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["ReviewSuccess"] = "Cảm ơn bạn đã đánh giá sản phẩm.";
            return RedirectToProductReviews(productId);
        }

        private IActionResult RedirectToProductReviews(int productId)
        {
            return Redirect($"{Url.Action(nameof(Details), new { id = productId })}#reviewsTab");
        }
        public async Task<IActionResult> Details(int id)
        {
            var product = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.ProductImages)
                .Include(p => p.ProductColors)
                .Include(p => p.ProductReviews)
                    .ThenInclude(r => r.User)
                .FirstOrDefaultAsync(p => p.Id == id && p.IsActive);

            if (product == null)
            {
                return NotFound();
            }

            var relatedCategoryIds = new List<int>
            {
                product.CategoryId
            };

            if (product.Category != null && product.Category.ParentCategoryId.HasValue)
            {
                var categories = await _context.Categories
                    .Where(c => c.IsActive)
                    .ToListAsync();

                relatedCategoryIds = GetCategoryAndChildIds(
                    categories,
                    product.Category.ParentCategoryId.Value
                );
            }

            var relatedProducts = await _context.Products
                .Include(p => p.Category)
                .Where(p =>
                    p.IsActive &&
                    p.Id != product.Id &&
                    relatedCategoryIds.Contains(p.CategoryId))
                .OrderByDescending(p => p.IsFeatured)
                .ThenByDescending(p => p.CreatedAt)
                .Take(4)
                .ToListAsync();
            SaveRecentlyViewedProduct(product.Id);

            ViewBag.RelatedProducts = relatedProducts;

            return View(product);
        }

        private const string RecentlyViewedCookieName = "MiniRecentlyViewedProducts";

        private void SaveRecentlyViewedProduct(int productId)
        {
            var ids = ParseRecentlyViewedIds(Request.Cookies[RecentlyViewedCookieName]);

            ids.Remove(productId);
            ids.Insert(0, productId);

            ids = ids
                .Take(12)
                .ToList();

            Response.Cookies.Append(
                RecentlyViewedCookieName,
                string.Join(",", ids),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true,
                    IsEssential = true,
                    SameSite = SameSiteMode.Lax,
                    Secure = Request.IsHttps
                });
        }

        private static List<int> ParseRecentlyViewedIds(string? cookieValue)
        {
            if (string.IsNullOrWhiteSpace(cookieValue))
            {
                return new List<int>();
            }

            return cookieValue
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var id) ? id : 0)
                .Where(x => x > 0)
                .Distinct()
                .Take(12)
                .ToList();
        }
        private List<int> GetCategoryAndChildIds(List<Category> categories, int parentId)
        {
            var result = new List<int> { parentId };

            var childCategories = categories
                .Where(c => c.ParentCategoryId == parentId)
                .ToList();

            foreach (var child in childCategories)
            {
                result.AddRange(GetCategoryAndChildIds(categories, child.Id));
            }

            return result.Distinct().ToList();
        }
    }
}