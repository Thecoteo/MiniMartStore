using Microsoft.EntityFrameworkCore;
using MiniSmartstoreMvc.Data;
using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.Services
{
    public class ProductRuleService
    {
        private readonly ApplicationDbContext _context;

        public ProductRuleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task ApplyActiveRulesAsync()
        {
            var now = DateTime.Now;

            var products = await _context.Products.ToListAsync();

            foreach (var product in products)
            {
                if (product.BasePrice <= 0)
                {
                    product.BasePrice = product.Price;
                }

                product.Price = product.BasePrice;
                product.OldPrice = null;
            }

            var activeRules = await _context.ProductRules
                .Where(r =>
                    r.IsActive &&
                    r.StartDate <= now &&
                    r.EndDate >= now)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Id)
                .ToListAsync();

            foreach (var rule in activeRules)
            {
                var matchedProducts = GetMatchedProducts(products, rule);

                foreach (var product in matchedProducts)
                {
                    ApplyRuleToProduct(product, rule);
                }
            }

            await _context.SaveChangesAsync();
        }

        private IEnumerable<Product> GetMatchedProducts(List<Product> products, ProductRule rule)
        {
            if (rule.TargetType == ProductRuleTargetType.Category && rule.CategoryId.HasValue)
            {
                return products.Where(p => p.CategoryId == rule.CategoryId.Value);
            }

            if (rule.TargetType == ProductRuleTargetType.Product && rule.ProductId.HasValue)
            {
                return products.Where(p => p.Id == rule.ProductId.Value);
            }

            return products;
        }

        private void ApplyRuleToProduct(Product product, ProductRule rule)
        {
            switch (rule.ActionType)
            {
                case ProductRuleActionType.Discount:
                    ApplyDiscountRule(product, rule);
                    break;

                case ProductRuleActionType.MarkAsFeatured:
                    product.IsFeatured = true;
                    break;

                case ProductRuleActionType.HideProduct:
                    product.IsActive = false;
                    break;

                case ProductRuleActionType.ShowProduct:
                    product.IsActive = true;
                    break;
            }
        }

        private void ApplyDiscountRule(Product product, ProductRule rule)
        {
            if (rule.DiscountValue <= 0)
            {
                return;
            }

            var basePrice = product.BasePrice > 0 ? product.BasePrice : product.Price;
            var newPrice = basePrice;

            if (rule.DiscountType == DiscountType.Percentage)
            {
                newPrice = basePrice - basePrice * rule.DiscountValue / 100;
            }
            else
            {
                newPrice = basePrice - rule.DiscountValue;
            }

            if (newPrice < 0)
            {
                newPrice = 0;
            }

            if (newPrice < product.Price)
            {
                product.OldPrice = basePrice;
                product.Price = newPrice;
            }
        }
    }
}