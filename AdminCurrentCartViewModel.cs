namespace MiniSmartstoreMvc.ViewModels
{
    public class AdminCurrentCartViewModel
    {
        public string UserId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string CustomerEmail { get; set; } = string.Empty;

        public DateTime? UpdatedAt { get; set; }

        public List<AdminCurrentCartProductViewModel> Products { get; set; } = new();

        public int TotalItems => Products.Sum(x => x.Quantity);
    }

    public class AdminCurrentCartProductViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? CategoryName { get; set; }

        public bool IsActive { get; set; }

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal Total => UnitPrice * Quantity;
    }
}