using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;

namespace MiniSmartstoreMvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductReviewController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductReviewController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(
            string? search,
            int? rating,
            string? status,
            DateTime? fromDate,
            DateTime? toDate,
            int page = 1,
            int pageSize = 20)
        {
            if (page < 1)
            {
                page = 1;
            }

            if (pageSize != 10 && pageSize != 20 && pageSize != 50)
            {
                pageSize = 20;
            }

            var query = _context.ProductReviews
                .Include(x => x.Product)
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(x =>
                    x.Title.Contains(search) ||
                    x.Comment.Contains(search) ||
                    (x.Product != null && x.Product.Name.Contains(search)) ||
                    (x.User != null && (
                        (x.User.FullName != null && x.User.FullName.Contains(search)) ||
                        (x.User.Email != null && x.User.Email.Contains(search))
                    )));
            }

            if (rating.HasValue && rating.Value >= 1 && rating.Value <= 5)
            {
                query = query.Where(x => x.Rating == rating.Value);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                if (status == "approved")
                {
                    query = query.Where(x => x.IsApproved);
                }
                else if (status == "hidden")
                {
                    query = query.Where(x => !x.IsApproved);
                }
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(x => x.CreatedAt >= from);
            }

            if (toDate.HasValue)
            {
                var to = toDate.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.CreatedAt <= to);
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var reviews = await query
                .OrderByDescending(x => x.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.Search = search;
            ViewBag.Rating = rating;
            ViewBag.Status = status;
            ViewBag.FromDate = fromDate?.ToString("yyyy-MM-dd");
            ViewBag.ToDate = toDate?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;

            ViewBag.TotalReviews = await _context.ProductReviews.CountAsync();
            ViewBag.ApprovedReviews = await _context.ProductReviews.CountAsync(x => x.IsApproved);
            ViewBag.HiddenReviews = await _context.ProductReviews.CountAsync(x => !x.IsApproved);

            ViewBag.AverageRating = await _context.ProductReviews.AnyAsync()
                ? await _context.ProductReviews.AverageAsync(x => x.Rating)
                : 0;

            return View(reviews);
        }

        public async Task<IActionResult> Details(int id)
        {
            var review = await _context.ProductReviews
                .Include(x => x.Product)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (review == null)
            {
                return NotFound();
            }

            return View(review);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            review.IsApproved = true;
            review.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã duyệt đánh giá.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Hide(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            review.IsApproved = false;
            review.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã ẩn đánh giá khỏi trang sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _context.ProductReviews.FindAsync(id);

            if (review == null)
            {
                return NotFound();
            }

            _context.ProductReviews.Remove(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Đã xóa đánh giá.";
            return RedirectToAction(nameof(Index));
        }
    }
}