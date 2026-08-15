using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class ShopDrawerViewModel
    {
        public string ActiveTab { get; set; } = "cart";

        public List<CartItemViewModel> CartItems { get; set; } = new();

        public List<Product> WishlistProducts { get; set; } = new();

        public List<Product> CompareProducts { get; set; } = new();
    }
}