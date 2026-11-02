using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using VoucherHub.Models;
using VoucherHub.Services;
using VoucherHub.ViewModels;

namespace VoucherHub.Controllers
{
    [Route("purchases")]
    public class PurchaseController(PurchaseService svc, IOptions<ShopOptions> shop, ILogger<PurchaseController> log) : Controller
    {
        CheckoutVm Fill(CheckoutVm vm)
        {
            vm.WasscePrice = shop.Value.WasscePrice;
            vm.BecePrice = shop.Value.BecePrice;
           // vm.Price = shop.Value.PricePerVoucher;
            vm.MaxQuantity = shop.Value.MaxQuantity;
            return vm;
        }

        [HttpGet]
        public IActionResult Create() => View(Fill(new CheckoutVm()));

        [HttpPost]
        public async Task<IActionResult> Create(CheckoutVm vm)
        {
            Fill(vm);
            if (!ModelState.IsValid) return View(vm);

            var (p, error) = await svc.CreateAsync(vm);
            if (p == null)
            {
                ModelState.AddModelError("", error!);
                return View(vm);
            }


            try
            {
                var callback = Url.Action(
                    "Callback",
                    "Payment", 
                    new
                    {
                        reference = p.PaystackReference 
                    },
                    Request.Scheme
                    )!;
                return Redirect(await svc.InitializeAsync(p, callback));
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Paystack initialize failed");
                await svc.CancelAsync(p.PaystackReference);
                ModelState.AddModelError("", "We couldn't start the payment. Please try again.");
                return View(vm);
            }

        }

    }


}

