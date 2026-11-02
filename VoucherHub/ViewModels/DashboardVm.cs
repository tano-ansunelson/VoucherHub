using VoucherHub.Models;

namespace VoucherHub.ViewModels
{
    public class DashboardVm
    {

        public int Total, Available, Reserved, Sold, Sales;
        public decimal Revenue;
        public List<StockRow> Stock { get; set; } = new();
        public List<Purchase> Recent { get; set; } = new();
        public List<DayRevenue> Last7 { get; set; } = new();
    }

    public record StockRow(ExamType Type, int Available, int Reserved, int Sold);
}
