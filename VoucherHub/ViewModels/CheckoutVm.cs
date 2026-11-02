using System.ComponentModel.DataAnnotations;
using VoucherHub.Models;

namespace VoucherHub.ViewModels
{
    public class CheckoutVm
    {
        string _phone = "";
        [Required(ErrorMessage = "Full name is required"), StringLength(100, MinimumLength =3)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";


        [Required(ErrorMessage = "Phone number is required")]
        [RegularExpression(@"^(0|\+233)\d{9}$",  ErrorMessage = "Enter a valid Ghana number, e.g. 0240000000 or +233240000000")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get => _phone; set => _phone = (value ?? "").Replace(" ", "").Replace("-", ""); }


        [Required(ErrorMessage = "Email is required"), EmailAddress(ErrorMessage = "Invalid email address"), StringLength(200)]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = "";


        [Range(1, 100, ErrorMessage = "Choose at least 1 voucher")]
        [Display(Name = "Number of Vouchers")]
        public int Quantity { get; set; } = 1;


        [Display(Name = "voucher type")]
        public ExamType ExamType { get; set; } = ExamType.Wassce;
        //display only never trusted on post

        public decimal WasscePrice { get; set; }
        public decimal BecePrice { get; set; }

        public decimal PriceFor(ExamType t) => t == ExamType.Bece ? BecePrice : WasscePrice;

      //  public decimal Price { get; set; }
        public int MaxQuantity { get; set; } = 20;

    }
}
