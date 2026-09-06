namespace MiniSmartstoreMvc.Models
{
    public class ProductColor
    {
        public int Id { get; set; }

        public string ColorName { get; set; } = string.Empty;

        public string ColorCode { get; set; } = "#000000";

        public int ProductId { get; set; }

        public Product? Product { get; set; }
    }
}