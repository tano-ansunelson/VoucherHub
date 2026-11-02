using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoucherHub.Data;
using VoucherHub.Services;
using VoucherHub.ViewModels;
using VoucherHub.Models;

namespace VoucherHub.Areas.Admin.Controllers
{
    [Area("Admin"), Authorize(Roles = "Admin")]
    public class PurchasesController(AppDbContext db, PurchaseService svc) : Controller
    {
        const int pageSize = 10;
        public async Task<IActionResult> Index(int page = 1)
        {
            var pages = Math.Max(1, (int)Math.Ceiling(await db.Purchases.CountAsync() / (double)pageSize));
            page = Math.Clamp(page, 1, pages);
            var items = await db.Purchases
                .Include(p =>p.Customer)
                .OrderByDescending(p =>p.PurchaseId)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
            

            return View( new PurchaseListVm
            {
                Items = items,
                Page = page,
                Pages = pages
            });
        }



        public  async Task<IActionResult> Details(int id)
        {
            var p = await db.Purchases
                .Include(x => x.Customer)
                .Include(x=> x.PurchaseVouchers)
                .ThenInclude(pv=> pv.Voucher)
                .FirstOrDefaultAsync(x => x.PurchaseId == id);
            return p == null? NotFound() : View(p);
        }


        [HttpPost]
        public async Task<IActionResult> Resend(int id)
        {
            var p = await db.Purchases
                .Include(x => x.Customer)
                .Include(x => x.PurchaseVouchers)
                    .ThenInclude(pv => pv.Voucher)
                .FirstOrDefaultAsync(x => x.PurchaseId == id);

            if (p == null) return NotFound();

            if(p.PaymentStatus != PaymentStatus.Paid)
            {
                TempData["Err"] = "Only paid purchases can be sent.";
                return RedirectToAction(nameof(Details), new { id });
            }
            try
            {
                await svc.ResendAsync(p);
                TempData["ok"] = "Purchase email sent successfully.";
            }
            catch
            {
                TempData["Err"] = "Failed to send purchase email. Check SMTP settings.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
