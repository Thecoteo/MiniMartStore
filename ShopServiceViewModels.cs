using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class WhatsNewViewModel
    {
        public List<Product> NewProducts { get; set; } = new();
        public List<Product> FeaturedProducts { get; set; } = new();
        public List<Product> SaleProducts { get; set; } = new();
    }
}