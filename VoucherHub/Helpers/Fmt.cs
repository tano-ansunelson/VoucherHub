using System.Globalization;
using VoucherHub.Models;

namespace VoucherHub.Helpers
{
    public static class Fmt
    {

        public static string Money(decimal v) => "GH₵" + v.ToString("N2", CultureInfo.InvariantCulture);

        public static string Exam(ExamType t) => t == ExamType.Bece ? "BECE" : "WASSCE";
        public static string Date(DateTime utc) => utc.ToString("dd MMM yyyy, HH:mm", CultureInfo.InvariantCulture);
        public static string Badge(object status) => status.ToString() switch
        {
            "Paid" or "Available" => "g",
            "Pending" or "Reserved" => "y",
            "Failed" or "NeedsAttention" => "r",
            _ => "n"
        };
    }
}
