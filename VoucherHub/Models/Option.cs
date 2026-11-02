namespace VoucherHub.Models
{
  public class ShopOptions
    {
        public decimal WasscePrice { get; set; } = 17m;
        public decimal BecePrice { get; set; } = 17m;
        public int MaxQuantity { get; set; } = 50;
        public int ReservationMinutes { get; set; } = 30;
        public decimal PriceFor(ExamType t) => t == ExamType.Bece ? BecePrice : WasscePrice;
    }


    public class PaystackOptions
    {
       // public string PublicKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        //public string CallbackUrl { get; set; } = "";
    }

    public class SmtpOptions { 
        public string Host { get; set; } = ""; 
        public int Port { get; set; } = 587;
        public string User { get; set; } = ""; 
        public string Password { get; set; } = "";
        public string FromName { get; set; } = "";
        public string FromAddress { get; set; } = "";
    }
    public class AdminOptions {
        public string Username { get; set; } = ""; 
        public string Password { get; set; } = ""; 
    }

}
