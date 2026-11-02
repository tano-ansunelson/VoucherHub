using System.ComponentModel.DataAnnotations;
using VoucherHub.Models;

namespace VoucherHub.ViewModels
{
    public class VoucherFormVm
    {
        public int Id { get; set; }

        [Required, StringLength(60), Display(Name = "Serial number")] 
        public string SerialNumber { get; set; } = "";

        [Required, StringLength(60)]
        public string Pin { get; set; } = "";

        [Display(Name = "Voucher type")]
        public ExamType ExamType { get; set; } = ExamType.Wassce;


        [Range(1, 100000), Display(Name = "Price (GH₵)")] 
        public decimal Price { get; set; }
    }
}

