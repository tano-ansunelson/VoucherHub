using VoucherHub.Models;

namespace VoucherHub.ViewModels
{
    public class PurchaseListVm
    {
        public List<Purchase> Items { get; set; } = new(); public int Page { get; set; }
        public int Pages { get; set; }
    }

    public class VoucherListVm
    {
        public List<Voucher> Items { get; set; } = new();
        public string? Search { get; set; }
        public string? Status { get; set; }
        public int Page { get; set; }
        public int Pages { get; set; }
        public string? Type { get; set; }
    }
    public record SkippedRow(
        int Row,
        string Serial,
        string Issue
     );


    public class ImportResultVm { 
        public int Imported { get; set; }
        public List<SkippedRow> Skipped { get; set; } = new();
    }
    public record DayRevenue(
        string Label, 
        decimal Amount
    );
}
