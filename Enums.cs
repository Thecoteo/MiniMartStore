namespace MiniSmartstoreMvc.Models
{
    public enum PaymentMethod
    {
        CashOnDelivery = 1,
        BankTransfer = 2,
        OnlinePaymentComingSoon = 3
    }

    public enum PaymentStatus
    {
        Pending = 1,
        Paid = 2,
        Failed = 3,
        Cancelled = 4
    }

    public enum OrderStatus
    {
        Pending = 1,
        Processing = 2,
        Completed = 3,
        Cancelled = 4
    }

    public enum DiscountType
    {
        FixedAmount = 1,
        Percentage = 2
    }
}