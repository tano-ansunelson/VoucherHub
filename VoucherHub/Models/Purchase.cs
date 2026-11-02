namespace VoucherHub.Models
{

    public enum PaymentStatus
    {
        Pending,
        Paid,
        NeedsAttention,
        Failed
    }
 
    public class Purchase
    {

        public int PurchaseId { get; set; }
        public int CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public ExamType ExamType { get; set; } = ExamType.Wassce;
        public int Quantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaystackReference { get; set; } = "";
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Pending;
        public DateTime PurchasedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }
        public DateTime? EmailSentAt { get; set; }

        public List<PurchaseVoucher> PurchaseVouchers { get; set; } = new();

    }
}
