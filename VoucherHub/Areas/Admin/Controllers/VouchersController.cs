using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoucherHub.Data;
using VoucherHub.Models;
using VoucherHub.ViewModels;

namespace VoucherHub.Areas.Admin.Controllers
{
    [Area("Admin"), Authorize(Roles ="Admin")]
    public class VouchersController(AppDbContext db, IOptions<ShopOptions> shop) : Controller
    {

        const int PageSize = 20;
        public async Task<IActionResult> Index(string? search, string? status, string? type, int page =1)
        {
            var q = db.Vouchers.AsQueryable();
            if (!string.IsNullOrWhiteSpace(search)) q = q.Where(v => v.SerialNumber.Contains(search.Trim()));
            if (Enum.TryParse<VoucherStatus>(status, out var st)) q = q.Where(v => v.Status == st);

            if (Enum.TryParse<ExamType>(type, out var et)) q = q.Where(v => v.ExamType == et);
            var total = await q.CountAsync();
            var pages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
            page = Math.Clamp(page, 1, pages);
            var items = await q.OrderByDescending(v => v.VoucherId)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            return View(new VoucherListVm

            {
                Items = items,
                Search = search,
                Type = type,
                Status = status,
                Page = page,
                Pages = pages
            });
        }


       public IActionResult Create() => View(new VoucherFormVm { Price = shop.Value.WasscePrice });


        [HttpPost]
        public async Task<IActionResult> Create(VoucherFormVm vm)
        {
            vm.SerialNumber = vm.SerialNumber?.Trim() ?? "";
            if (await db.Vouchers.AnyAsync(v => v.SerialNumber == vm.SerialNumber))
                ModelState.AddModelError(nameof(vm.SerialNumber), "This serial number already exists.");
            if (!ModelState.IsValid)
                return View(vm);
            db.Vouchers.Add(new Voucher

            {
                SerialNumber = vm.SerialNumber,
                Pin = vm.Pin.Trim(),
                price = vm.Price,
                ExamType=vm.ExamType,
            });
            await db.SaveChangesAsync();
            TempData["ok"] = "Voucher saved.";
            return RedirectToAction(nameof(Index));
        }



        public async Task<IActionResult>  Edit(int id)
        {
            var v = await db.Vouchers.FindAsync(id);
            if (v == null) return NotFound();
            if (v.Status != VoucherStatus.Available)
                return RedirectToAction(nameof(Details), new { id });

            return View(new VoucherFormVm
            {
                Id = v.VoucherId,
                SerialNumber = v.SerialNumber,
                Pin = v.Pin,
                Price = v.price,
                ExamType=v.ExamType,
                

            });
        }


        [HttpPost]
        public async Task<IActionResult> Edit(VoucherFormVm vm)
        {
            var v = await db.Vouchers.FindAsync(vm.Id);
            if (v == null) return NotFound();


            if (v.Status != VoucherStatus.Available)
                return RedirectToAction(nameof(Details), new { id = v.VoucherId });

            vm.SerialNumber = vm.SerialNumber?.Trim() ?? "";
            if (await db.Vouchers.AnyAsync(
                    x => x.SerialNumber == vm.SerialNumber && x.VoucherId != vm.Id
                )) 
                ModelState.AddModelError(nameof(vm.SerialNumber), "This serial number already exists.");
            if (!ModelState.IsValid)
                return View(vm);
            v.SerialNumber = vm.SerialNumber;
            v.Pin = vm.Pin.Trim(); 
            v.price = vm.Price;
            v.ExamType = vm.ExamType;
            await db.SaveChangesAsync();
            TempData["Ok"] = "Voucher updated.";
            return RedirectToAction(nameof(Index));
        }


        public async Task<IActionResult> Details (int id)
        {
            var v = await db.Vouchers.FindAsync(id);
            if (v == null) return NotFound();
            ViewBag.PurchaseId = await db.PurchaseVouchers
                .Where(x => x.VoucherId == id)
                .Select(x => (int?)x.PurchaseId)
                .FirstOrDefaultAsync();

            return View(v);
        }



        public IActionResult Import() => View(new ImportResultVm());

        [HttpPost]
        public async Task<IActionResult> Import(IFormFile? file, ExamType examType)
        {
            if (file == null || file.Length == 0 || file.Length > 2_000_000)
            {
                ModelState.AddModelError("", "Choose a .csv or .xlsx file under 2 MB.");
                return View(new ImportResultVm());
            }

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".csv" or ".xlsx"))
            {
                ModelState.AddModelError("", "Only .csv and .xlsx files are supported.");
                return View(new ImportResultVm());
            }
                var rows = new List<(string Serial, string Pin)>();
                using (var s = file.OpenReadStream())
                {
                    if(ext == ".xlsx")
                    {
                        using var wb = new XLWorkbook(s);
                        foreach (var r in wb.Worksheet(1).RowsUsed())
                            rows.Add((r.Cell(1).GetString().Trim(), r.Cell(2).GetString().Trim()));

                    }
                    else
                    {
                        using var sr = new StreamReader(s);
                        string? line;
                        while((line =sr.ReadLine()) != null)
                        {
                            var parts = line.Split(',');
                            rows.Add((parts[0].Trim().Trim('"'), parts.Length > 1 ? parts[1].Trim().Trim('"') : ""));
                        }
                    }
                    if(rows.Count > 5000)
                    {
                        ModelState.AddModelError("", "Maximum 5,000 rows per import.");
                        return View(new ImportResultVm());
                    }

                    var start = rows.Count > 0 && rows[0].Serial.Equals("SerialNumber", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
                    var serials = rows.Skip(start).Select(r => r.Serial).Where(x => x != "").ToList();
                    var existing = (await db.Vouchers.Where(v => serials.Contains(v.SerialNumber)).Select(v => v.SerialNumber).ToListAsync()).ToHashSet(
                        StringComparer.OrdinalIgnoreCase
                        );

                    var result = new ImportResultVm();
                    var seen = new HashSet<String>(StringComparer.OrdinalIgnoreCase);
                    for (int i= start; i < rows.Count; i++)
                    {
                        var (serial, pin) = rows[i]; int rowNo = i + 1;
                        if (serial == "" && pin == "") continue;
                        if (serial == "" || pin == "")
                            result.Skipped.Add(new SkippedRow(rowNo, serial, "Misssing serial number of PIN"));

                        else if (serial.Length > 60 || pin.Length > 60)
                            result.Skipped.Add(new(rowNo, serial, "Value too long (max 60 characters)"));

                        else if (existing.Contains(serial) || !seen.Add(serial))
                            result.Skipped.Add(new(rowNo, serial, "Duplicate serail number."));
                        else
                        {
                        db.Vouchers.Add(new Voucher
                        {
                            SerialNumber = serial,
                            Pin = pin,
                            price = shop.Value.PriceFor(examType),
                            ExamType=examType,

                        });
                            result.Imported++; 
                        
                        }

                    }
                    await db.SaveChangesAsync();
                    return View(result);

                }
            }
        }




    
}
