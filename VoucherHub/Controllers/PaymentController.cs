using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoucherHub.Data;
using VoucherHub.Models;
using VoucherHub.Services;

namespace VoucherHub.Controllers
{
    [Route("payment")]
    public class PaymentController(PurchaseService svc, AppDbContext db) : Controller
    {

        [HttpGet("callback")]
        public IActionResult Callback(string reference)=> View("Processing", reference);




        [HttpGet("status")]
        public async Task<IActionResult> Status(string reference)
        {
            var s = await svc.VerifyAndFulfillAsync(reference);
            var shown = s switch
            {
                PaymentStatus.Paid => "Paid",
                PaymentStatus.Failed => "Failed",
                _ => "Pending"
            };

            return Json(new { Status = shown });
        }
        [HttpGet("success")]
        public async  Task<IActionResult> Success(string reference)
        {
            var p = await db.Purchases.Include(x => x.Customer)
                            .FirstOrDefaultAsync(x => x.PaystackReference == reference);
            if (p == null || p.PaymentStatus != PaymentStatus.Paid) return RedirectToAction(nameof(Callback), new { reference }); 
            return View(p);
        }


        [HttpGet("failed")]
        public IActionResult Failed(string reference) => View("Failed", reference);
        [HttpGet("pending")] 
        public IActionResult Pending(string reference) => View("Pending", reference);
    }
}
