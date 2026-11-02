using System.ComponentModel.DataAnnotations;

namespace VoucherHub.ViewModels
{
    public class LoginVm
    {

        [Required]
        public string Username { get; set; } = "";
        [Required, DataType(DataType.Password)]
        public string Password { get; set; } = "";
    }
}
