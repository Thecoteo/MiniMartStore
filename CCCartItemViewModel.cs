using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class CartItemViewModel
    {
        public int ProductId { get; set; }

        public Product? Product { get; set; }

        public int Quantity { get; set; }
        public string? SelectedColor { get; set; }
        public decimal UnitPrice => Product?.Price ?? 0;

        public int StockQuantity => Product?.StockQuantity ?? 0;

        public decimal TotalPrice => UnitPrice * Quantity;
    }
}