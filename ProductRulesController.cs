using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;
using MiniSmartstoreMvc.Services;

namespace MiniSmartstoreMvc.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProductRuleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ProductRuleService _productRuleService;

        public ProductRuleController(
            ApplicationDbContext context,
            ProductRuleService productRuleService)
        {
            _context = context;
            _productRuleService = productRuleService;
        }

        public async Task<IActionResult> Index()
        {
            var rules = await _context.ProductRules
                .Include(r => r.Category)
                .Include(r => r.Product)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            return View(rules);
        }

        public async Task<IActionResult> Create()
        {
            await LoadSelectLists();

            return View(new ProductRule
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddDays(7),
                IsActive = true,
                Priority = 1
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductRule rule)
        {
            ValidateProductRule(rule);

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return View(rule);
            }

            rule.CreatedAt = DateTime.Now;

            _context.ProductRules.Add(rule);
            await _context.SaveChangesAsync();

            await _productRuleService.ApplyActiveRulesAsync();

            TempData["Success"] = "Đã tạo quy tắc sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var rule = await _context.ProductRules.FindAsync(id);

            if (rule == null)
            {
                return NotFound();
            }

            await LoadSelectLists();
            return View(rule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductRule rule)
        {
            if (id != rule.Id)
            {
                return NotFound();
            }

            ValidateProductRule(rule);

            if (!ModelState.IsValid)
            {
                await LoadSelectLists();
                return View(rule);
            }

            var existingRule = await _context.ProductRules.FindAsync(id);

            if (existingRule == null)
            {
                return NotFound();
            }

            existingRule.RuleName = rule.RuleName;
            existingRule.Description = rule.Description;
            existingRule.ActionType = rule.ActionType;
            existingRule.TargetType = rule.TargetType;
            existingRule.CategoryId = rule.CategoryId;
            existingRule.ProductId = rule.ProductId;
            existingRule.DiscountType = rule.DiscountType;
            existingRule.DiscountValue = rule.DiscountValue;
            existingRule.StartDate = rule.StartDate;
            existingRule.EndDate = rule.EndDate;
            existingRule.Priority = rule.Priority;
            existingRule.IsActive = rule.IsActive;

            await _context.SaveChangesAsync();

            await _productRuleService.ApplyActiveRulesAsync();

            TempData["Success"] = "Đã cập nhật quy tắc sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(int id)
        {
            var rule = await _context.ProductRules.FindAsync(id);

            if (rule == null)
            {
                return NotFound();
            }

            rule.IsActive = !rule.IsActive;

            await _context.SaveChangesAsync();

            await _productRuleService.ApplyActiveRulesAsync();

            TempData["Success"] = rule.IsActive
                ? "Đã bật quy tắc sản phẩm."
                : "Đã tắt quy tắc sản phẩm.";

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var rule = await _context.ProductRules.FindAsync(id);

            if (rule == null)
            {
                return NotFound();
            }

            _context.ProductRules.Remove(rule);
            await _context.SaveChangesAsync();

            await _productRuleService.ApplyActiveRulesAsync();

            TempData["Success"] = "Đã xóa quy tắc sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApplyNow()
        {
            await _productRuleService.ApplyActiveRulesAsync();

            TempData["Success"] = "Đã áp dụng lại toàn bộ quy tắc sản phẩm.";
            return RedirectToAction(nameof(Index));
        }

        private async Task LoadSelectLists()
        {
            ViewBag.Categories = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(),
                "Id",
                "Name"
            );

            ViewBag.Products = new SelectList(
                await _context.Products.OrderBy(p => p.Name).ToListAsync(),
                "Id",
                "Name"
            );
        }

        private void ValidateProductRule(ProductRule rule)
        {
            if (rule.StartDate >= rule.EndDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải lớn hơn ngày bắt đầu.");
            }

            if (rule.TargetType == ProductRuleTargetType.Category && !rule.CategoryId.HasValue)
            {
                ModelState.AddModelError("CategoryId", "Vui lòng chọn danh mục áp dụng.");
            }

            if (rule.TargetType == ProductRuleTargetType.Product && !rule.ProductId.HasValue)
            {
                ModelState.AddModelError("ProductId", "Vui lòng chọn sản phẩm áp dụng.");
            }

            if (rule.ActionType == ProductRuleActionType.Discount && rule.DiscountValue <= 0)
            {
                ModelState.AddModelError("DiscountValue", "Giá trị giảm giá phải lớn hơn 0.");
            }
        }
    }
}