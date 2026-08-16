using MiniSmartstoreMvc.Models;

namespace MiniSmartstoreMvc.ViewModels
{
    public class AdminOrderListItemViewModel
    {
        public int Id { get; set; }
        public string OrderCode { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;

        public string ShippingAddress { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public PaymentMethod PaymentMethod { get; set; }
        public PaymentStatus PaymentStatus { get; set; }
        public OrderStatus OrderStatus { get; set; }

        public DateTime CreatedAt { get; set; }

        public int ProductCount { get; set; }
        public int TotalQuantity { get; set; }
    }

    public class AdminOrderDetailViewModel
    {
        public Order Order { get; set; } = new Order();

        public string CustomerEmail { get; set; } = string.Empty;

        public List<OrderDetail> OrderDetails { get; set; } = new();
    }
}