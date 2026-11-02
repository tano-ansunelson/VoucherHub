using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoucherHub.Data;
using VoucherHub.Models;
using VoucherHub.ViewModels;

namespace VoucherHub.Services
{
    public class PurchaseService(AppDbContext db, PaystackService paystack, EmailService mail,
        IOptions<ShopOptions> shopOpt, ILogger<PurchaseService> log)
    {
        readonly ShopOptions shop = shopOpt.Value;

        public async Task<(Purchase? purchase, string? Error)> CreateAsync(CheckoutVm vm)
        {
            if (vm.Quantity < 1 || vm.Quantity > shop.MaxQuantity)
                return (null, $"Quantity must be between 1 and {shop.MaxQuantity}.");

            await ReleaseExpiredAsync();


            await using var tx = await db.Database.BeginTransactionAsync();
            var vouchers = await ReserveAsync(vm.ExamType, vm.Quantity);
            if (vouchers == null)
            {
                await tx.RollbackAsync();
                return (null, $"Not enough {Helpers.Fmt.Exam(vm.ExamType)}vouchers are available right now. Please reduce the quantity or try again later.");
            }

            var email = vm.Email.Trim().ToLowerInvariant();

            var customer = new Customer
            {
                Email = email,
                FullName = vm.FullName.Trim(),
                PhoneNumber = vm.PhoneNumber.Trim(),

            };
            //var customer = await db.Customers.FirstOrDefaultAsync(c => c.Email == email) ?? new Customer { Email = email };
            //customer.FullName = vm.FullName.Trim();
            //customer.PhoneNumber = vm.PhoneNumber.Trim();



            var p = new Purchase
            {
                Customer = customer,
                Quantity = vm.Quantity,
                ExamType = vm.ExamType,

                TotalAmount = vm.Quantity * shop.PriceFor(vm.ExamType),
                PaystackReference = "VH-" + Guid.NewGuid().ToString("N")[..16].ToUpperInvariant(),
                PaymentStatus = PaymentStatus.Pending,
                PurchasedAt = DateTime.UtcNow
            };

            foreach (var v in vouchers) p.PurchaseVouchers.Add(new PurchaseVoucher { Voucher = v });

            db.Purchases.Add(p);
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            return (p, null);
        }


        public Task<string> InitializeAsync(Purchase p, string callbackUrl) =>
            paystack.InitializeAsync( p.Customer!.Email, (long)(p.TotalAmount * 100), p.PaystackReference, callbackUrl);


        public async Task CancelAsync(string reference)
        {
            var p = await LoadAsync(reference);
            if(p is { PaymentStatus: PaymentStatus.Pending }) 
            { 
                ReleaseVouchers(p, PaymentStatus.Failed);
                await db.SaveChangesAsync();
            }
        }

        //verify with paystack and update status accordingly


        public async Task<PaymentStatus?> VerifyAndFulfillAsync(string reference)
        {
            var p = await LoadAsync(reference);
            if (p == null) return null;



            if (p.PaymentStatus == PaymentStatus.Paid) 
            {
                await SendEmailIfNeededAsync(p);
                return PaymentStatus.Paid; 
            }
            if (p.PaymentStatus == PaymentStatus.NeedsAttention)
                return PaymentStatus.Pending;



            PaystackVerifyResult r;
            try
            {
                r = await paystack.VerifyAsync(reference); 
            } 
            catch (Exception ex)
            {
                log.LogWarning(ex, "Paystack verify failed for {Ref}", reference);
                return p.PaymentStatus == PaymentStatus.Failed ? PaymentStatus.Failed : PaymentStatus.Pending;
            }

            if (r.Status == "success")
            {
                if (r.Currency != "GHS" || r.AmountPesewas != (long)(p.TotalAmount * 100))
                {
                    log.LogError("Amount/currency mismatch on {Ref} : {Amt} {Cur}", reference, r.AmountPesewas, r.Currency);
                    await SetStatusAsync(p.PurchaseId, PaymentStatus.NeedsAttention);
                    return PaymentStatus.Pending;
                }
                await FulfillAsync(p);
                return p.PaymentStatus == PaymentStatus.NeedsAttention ? PaymentStatus.Pending :p.PaymentStatus;
            }

            if (r.Status is "failed" or "reversed")
            {
                if (p.PaymentStatus == PaymentStatus.Pending) { ReleaseVouchers(p, PaymentStatus.Failed);
                    
                 await db.SaveChangesAsync(); }
                return PaymentStatus.Failed;

            }
            return p.PaymentStatus == PaymentStatus.Failed ? PaymentStatus.Failed : PaymentStatus.Pending;
        }


       async Task FulfillAsync(Purchase p)
            {
                var id = p.PurchaseId;
                var now = DateTime.UtcNow;
                await using var tx = await db.Database.BeginTransactionAsync();


                var won = await db.Purchases.Where(x => x.PurchaseId == id && x.PaymentStatus != PaymentStatus.Paid)
                     .ExecuteUpdateAsync(s => s.SetProperty(x => x.PaymentStatus, PaymentStatus.Paid).SetProperty(x => x.PaidAt, now));
                if(won == 0)
                {
                    await tx.RollbackAsync();
                    await db.Entry(p)
                        .ReloadAsync();
                    return;
                }
                // Reservation expired before the customer paid? Try to grab fresh vouchers.
                if(p.PurchaseVouchers.Count < p.Quantity)
                {
                    var extra = await ReserveAsync(p.ExamType, p.Quantity);
                    if(extra == null)
                    {
                        await tx.RollbackAsync();
                        await SetStatusAsync(id, PaymentStatus.NeedsAttention);
                        p.PaymentStatus = PaymentStatus.NeedsAttention;
                        return;
                    }
                    foreach (var v in extra) p.PurchaseVouchers.Add(new PurchaseVoucher { Voucher = v });
                }
                foreach(var pv in p.PurchaseVouchers)
                 {
                   pv.Voucher.Status = VoucherStatus.Sold; pv.Voucher.SoldAt = now;
                 }
            await db.SaveChangesAsync();
            await tx.CommitAsync();
            p.PaymentStatus = PaymentStatus.Paid;
            p.PaidAt = now;
            await SendEmailIfNeededAsync(p);

       }


        async Task SendEmailIfNeededAsync(Purchase p)
        {
            var id = p.PurchaseId;
            var claimed = await db.Purchases.Where(x => x.PurchaseId == id && x.EmailSentAt == null)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailSentAt, DateTime.UtcNow));


            if (claimed == 0) return;
            try {
                await mail.SendVouchersAsync(p);
            } catch (Exception ex)
            {
                log.LogError(ex, "Email failed {Ref}", p.PaystackReference);
                await db.Purchases.Where(x => x.PurchaseId == id)
                    .ExecuteUpdateAsync(s => s.SetProperty(x => x.EmailSentAt, (DateTime?)null));
            }
        }





        public Task ResendAsync(Purchase p) => mail.SendVouchersAsync(p);




      async Task<List<Voucher>?> ReserveAsync(ExamType type, int qty)
        {
            var t = type.ToString();
            var list = await db.Vouchers.FromSqlInterpolated(
                $"SELECT TOP ({qty}) * FROM Vouchers WITH(UPDLOCK,READPAST, ROWLOCK) WHERE Status = 'Available' AND ExamType={t} ORDER BY VoucherId").ToListAsync();
            if(list.Count < qty) return null;
            var now = DateTime.UtcNow;
            foreach(var v in list)
            {
                v.Status = VoucherStatus.Reserved;
                v.ReservedAt = now;
            }
            return list;
        }
          


      void ReleaseVouchers(Purchase p, PaymentStatus newStatus)
        {
            foreach(var pv in p.PurchaseVouchers)
            {
               pv.Voucher.Status = VoucherStatus.Available;
                pv.Voucher.ReservedAt = null;
            }
            db.PurchaseVouchers.RemoveRange(p.PurchaseVouchers.ToList());
            p.PurchaseVouchers.Clear();
            p.PaymentStatus = newStatus;
        }


            


       async Task ReleaseExpiredAsync()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-shop.ReservationMinutes);
            var stale = await db.Purchases.Include(p => p.PurchaseVouchers)
                                .ThenInclude(pv => pv.Voucher)
                                .Where(p => p.PaymentStatus == PaymentStatus.Pending && p.PurchasedAt < cutoff)
                                .ToListAsync();

            foreach(var p in stale) ReleaseVouchers(p, PaymentStatus.Failed);
            if(stale.Count> 0) await db.SaveChangesAsync();
        }


        Task SetStatusAsync(int id, PaymentStatus s) =>
            db.Purchases.Where(x => x.PurchaseId == id).ExecuteUpdateAsync(u => u.SetProperty(x => x.PaymentStatus, s));

        Task<Purchase?> LoadAsync(string reference)=>
            db.Purchases.Include(p => p.Customer)
                        .Include(p => p.PurchaseVouchers)
                            .ThenInclude(pv => pv.Voucher)
                        .FirstOrDefaultAsync(p => p.PaystackReference == reference);


    }
}
