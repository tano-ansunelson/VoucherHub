namespace VoucherHub.Models
{
    public class Customer
    {

        public int CustomerId { get; set; }
        public string FullName { get; set; } = "";
        public string Email { get; set; } = "";
        public string PhoneNumber { get; set; } = " ";
        public DateTime CreateAt { get; set; } = DateTime.Now;
        //public string Address { get; set; }
    }
}
