using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoucherHub.Data;
using VoucherHub.Models;
using VoucherHub.ViewModels;

namespace VoucherHub.Areas.Admin.Controllers
{
    public class DashboardController(AppDbContext db) : Controller
    {
        [Area("Admin"), Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {

            var counts = await db.Vouchers
                .GroupBy(v => v.Status)
                .Select(g => new {g.Key, N = g.Count() })
                .ToListAsync();


            int C(VoucherStatus s) => 
                counts.FirstOrDefault(x => x.Key == s)?.N ?? 0;
            var paid = db.Purchases
                .Where(p => p.PaymentStatus == PaymentStatus.Paid);
            var since = DateTime.UtcNow.AddDays(-6);

            var recentPaid = await paid.Where(p => p.PurchasedAt >=since)
                .Select(p => new
                {
                    p.PurchasedAt,
                    p.TotalAmount
                }).ToListAsync();

            var stockRaw = await db.Vouchers.GroupBy(v => new { v.ExamType, v.Status })
                .Select(g => new { g.Key.ExamType, g.Key.Status, N = g.Count() }).ToListAsync();
            int S(ExamType t, VoucherStatus s) => stockRaw.FirstOrDefault(x => x.ExamType == t && x.Status == s)
                ?.N ?? 0;



            var vm = new DashboardVm
            {
                Stock = Enum.GetValues<ExamType>().Select(
                    t => new StockRow(t, S(t, VoucherStatus.Available), S(t, VoucherStatus.Reserved), S(t, VoucherStatus.Sold))
                    ).ToList(),
                Available = C(VoucherStatus.Available),
                Reserved = C(VoucherStatus.Reserved),
                Sold = C(VoucherStatus.Sold),

                Sales = await paid.CountAsync(),
                Revenue = await paid.SumAsync(p => (decimal?)p.TotalAmount) ?? 0,
                Recent = await db.Purchases
                        .Include(p => p.Customer)
                        .OrderByDescending(p => p.PurchaseId)
                        .Take(5)
                        .ToListAsync(),
                Last7 = Enumerable.Range(0, 7).Select(i => since.AddDays(i))
                       .Select(
                    d => new DayRevenue(d.ToString("ddd"), recentPaid.Where(x => x.PurchasedAt.Date == d).Sum(x => x.TotalAmount))).ToList()


            };

            vm.Total = vm.Available + vm.Reserved + vm.Sold;
            return View(vm);
        }
    }
}
