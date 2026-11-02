using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using VoucherHub.Models;
using VoucherHub.Services;

namespace VoucherHub.Controllers
{
    public class WebhookController(PurchaseService svc, IOptions<PaystackOptions> opt) : Controller
    {
        [HttpPost("/paystack/webhook"), IgnoreAntiforgeryToken]

        public async Task<IActionResult>  Receive()
        {

            using var sr = new StreamReader(Request.Body);
            var body = await sr.ReadToEndAsync();
            var sig = Request.Headers["x-paystack-signature"].ToString();
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(opt.Value.SecretKey));
            var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected), Encoding.UTF8.GetBytes(sig)))
                return Unauthorized();

            using var doc = JsonDocument.Parse(body);
            if(doc.RootElement.GetProperty("event").GetString() == "Charge.success")
            {
                var reference = doc.RootElement.GetProperty("data").GetProperty("reference").GetString();
                if (reference != null)
                    await svc.VerifyAndFulfillAsync(reference);
            }
            return Ok();
        }
    }
}
