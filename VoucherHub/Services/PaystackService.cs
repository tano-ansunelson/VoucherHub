using VoucherHub.Models;
using System.Net.Http.Headers;
using System.Net;
   

using Microsoft.Extensions.Options;
using System.Text.Json;
namespace VoucherHub.Services
{
    public record PaystackVerifyResult(string Status, long AmountPesewas, string Currency);
    public class PaystackService
    {

        readonly HttpClient _http;
        public PaystackService(HttpClient http, IOptions<PaystackOptions> opt)
        {
            _http = http;
            _http.BaseAddress = new Uri("https://api.paystack.co/");
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opt.Value.SecretKey);
        }


        public async Task<string> InitializeAsync (string email, long amountPesewas, string reference, string callbackUrl)
        {
            var res = await _http.PostAsJsonAsync("transaction/initialize",
                new
                {
                    email,
                    amount = amountPesewas,
                    currency = "GHS",
                    reference,
                    callback_url = callbackUrl,
               
                
                 });
            var body = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
                throw new Exception($"Paystack {(int)res.StatusCode}: {body}");
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.GetProperty("data").GetProperty("authorization_url").GetString()!;
            //    res.EnsureSuccessStatusCode();

            //    //using var doc = JsonDocument.Parse(
            //    //    await res.Content.ReadAsStringAsync());
            //    var body = await res.Content.ReadAsStringAsync();
            //    if (!res.IsSuccessStatusCode)
            //        throw new Exception($"Paystack {(int)res.StatusCode}: {body}");
            //    using var doc = JsonDocument.Parse(body);
            //    return doc.RootElement.GetProperty("data").GetProperty("authorization_url").GetString()!;
        }


        public async Task<PaystackVerifyResult> VerifyAsync(string reference)
        {
            var res = await _http.GetAsync("transaction/verify/" + Uri.EscapeDataString(reference));
            var body = await res.Content.ReadAsStringAsync();
            if (res.StatusCode == HttpStatusCode.NotFound) return new("abandoned", 0, "");
            if (!res.IsSuccessStatusCode)
                throw new Exception($"Paystack {(int)res.StatusCode}: {body}");

            res.EnsureSuccessStatusCode();


            using var doc = JsonDocument.Parse(body);
            // using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            if (!doc.RootElement.TryGetProperty("data", out var d) || d.ValueKind != JsonValueKind.Object)
                return new("pending", 0, "");
            // var d = doc.RootElement.GetProperty("data");
            var status = d.TryGetProperty("status", out var st) ? st.GetString() ?? "" : "";
            long amount = d.TryGetProperty("amount", out var a) && a.ValueKind == JsonValueKind.Number ? a.GetInt64() : 0;
            var currency = d.TryGetProperty("currency", out var c) ? c.GetString() ?? "" : "";

            return new(status, amount, currency); 
        }

    }
}
