namespace VoucherHub.Models
{
    public class PurchaseVoucher
    {

//public int PurchaseVoucherId { get; set; }
        public int PurchaseId { get; set; }
        public Purchase? Purchase { get; set; }
        public int VoucherId { get; set; }
        public Voucher? Voucher { get; set; }
    }
}
