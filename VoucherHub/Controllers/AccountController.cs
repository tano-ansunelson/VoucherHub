using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using VoucherHub.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using VoucherHub.ViewModels;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System.Security.Claims;

namespace VoucherHub.Controllers
{

    public class AccountController(IOptions<AdminOptions> opt) : Controller
    {
        static bool Eq(string a, string b) => CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

        [HttpGet("/account/login")] 
        public IActionResult Login(string? returnUrl) 
        {
            ViewData["ReturnUrl"] = returnUrl; 
          return  View(new LoginVm()); 
        }

        [HttpPost("/account/login")]
        public async Task<IActionResult> Login(LoginVm vm, string? returnUrl)
        {
            if (!ModelState.IsValid) return View(vm);
            var a = opt.Value;
            if (!(Eq(vm.Username, a.Username) & Eq(vm.Password, a.Password)))
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(vm);
            }
            var id = new ClaimsIdentity(new[] 
            { 
                new Claim(ClaimTypes.Name, a.Username), 
                new Claim(ClaimTypes.Role, "Admin")
            }, 
            CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(new ClaimsPrincipal(id));
            return Url.IsLocalUrl(returnUrl) ? Redirect(returnUrl!) : RedirectToAction("Index", "Dashboard", new { area = "Admin" });
        }

        [HttpPost("/account/logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(); 
            return RedirectToAction("Login");
        
        }
    }
    
}
