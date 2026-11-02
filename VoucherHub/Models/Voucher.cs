namespace VoucherHub.Models
{

    public enum VoucherStatus
    {
        Available,
        Reserved,
        Sold
    }

    public enum ExamType { Wassce, Bece}
    public class Voucher
    {


        public int VoucherId { get; set; }
        public string SerialNumber { get; set; } = "";
        public string Pin { get; set; } = "";
        public decimal price { get; set; }
        public ExamType ExamType { get; set; } = ExamType.Wassce;
        public VoucherStatus Status { get; set; } = VoucherStatus.Available;
        public DateTime? SoldAt { get; set; }
        public DateTime? ReservedAt { get; set; }

    }
}
