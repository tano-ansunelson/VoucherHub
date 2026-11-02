
using Microsoft.EntityFrameworkCore;
using VoucherHub.Models;

namespace VoucherHub.Data
{
    public class AppDbContext: DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<Customer> Customers => Set<Customer>();
        public DbSet<Voucher> Vouchers => Set<Voucher>();
        public DbSet<Purchase> Purchases => Set<Purchase>();
        public DbSet<PurchaseVoucher> PurchaseVouchers => Set<PurchaseVoucher>();


        protected override void OnModelCreating(ModelBuilder m)
        {
            m.Entity<Customer>(e =>
            {
                e.Property(x => x.FullName).HasMaxLength(100);
                e.Property(x => x.Email).HasMaxLength(100);
                e.Property(x => x.PhoneNumber).HasMaxLength(20);
                e.HasIndex(x => x.Email);
            });


            m.Entity<Voucher>(e =>
            {
                e.Property(x => x.SerialNumber).HasMaxLength(50);
                e.Property(x => x.Pin).HasMaxLength(50);
                e.Property(x => x.price).HasPrecision(18, 2);
                e.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
                e.HasIndex(x => x.SerialNumber).IsUnique();
                e.Property(x => x.ExamType).HasConversion<string>().HasMaxLength(20);
                e.HasIndex(x => new { x.ExamType, x.Status });
            });
            m.Entity<Purchase>(e =>
            {
                e.Property(x => x.TotalAmount).HasPrecision(18, 2);
                e.Property(x => x.PaystackReference).HasMaxLength(100);
                e.Property(x => x.ExamType).HasConversion<string>().HasMaxLength(20);
                e.Property(x => x.PaymentStatus).HasConversion<string>().HasMaxLength(20);
                e.HasIndex(x => x.PaystackReference).IsUnique();

            });

            m.Entity<PurchaseVoucher>(e =>
            {
                e.HasKey(x => new { x.PurchaseId, x.VoucherId });
                e.HasIndex(x => x.VoucherId).IsUnique();
            
            });
        }




    }
}
