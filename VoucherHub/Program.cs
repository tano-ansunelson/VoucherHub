using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VoucherHub.Data;
using VoucherHub.Models;
using VoucherHub.Services;

var builder = WebApplication.CreateBuilder(args);


var cfg = builder.Configuration;
// Add services to the container.
builder.Services.AddControllersWithViews( o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(cfg.GetConnectionString("Default"));
});
builder.Services.Configure<ShopOptions>(cfg.GetSection("Shop"));
builder.Services.Configure<PaystackOptions>(cfg.GetSection("Paystack"));
builder.Services.Configure<SmtpOptions>(cfg.GetSection("Smtp"));
builder.Services.Configure<AdminOptions>(cfg.GetSection("Admin"));
builder.Services.AddHttpClient<PaystackService>();
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<PurchaseService>();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o => { o.LoginPath = "/account/login"; o.Cookie.HttpOnly = true; o.Cookie.SecurePolicy = CookieSecurePolicy.Always; o.ExpireTimeSpan = TimeSpan.FromHours(8); });
builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapControllerRoute("areas",
    "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}"
    );
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
