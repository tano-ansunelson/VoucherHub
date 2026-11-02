using Microsoft.Extensions.Options;
using System.Net;
using System.Text;
using VoucherHub.Helpers;
using VoucherHub.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace VoucherHub.Services
{
    public class EmailService(IOptions<SmtpOptions> opt)
    {     

        public async Task SendVouchersAsync (Purchase p)
        {
            var s = opt.Value;
            var exam = Fmt.Exam(p.ExamType);
            var first = p.Customer!.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "Customer";
            var text = new StringBuilder($"Dear {first},\n\nThank you for your purchase.\nYour {exam} Result Checker vouchers are:\n\n");
            var html = new StringBuilder($"<p>Dear {WebUtility.HtmlEncode(first)},</p><p>Thank you for your purchase.<br>Your {exam} Result Checker vouchers are:</p>");
            int i = 1;
            foreach (var v in p.PurchaseVouchers.Select(x => x.Voucher))
            {
                text.Append($"Voucher {i}\nSerial Number: {v.SerialNumber}\nPIN: {v.Pin}\n\n");
                html.Append($"<div style='border:1px solid #e1e6f0;border-radius:8px;padding:12px;margin:8px 0'><b>Voucher" +
                    $" {i}</b><br>Serial Number: <b>{WebUtility.HtmlEncode(v.SerialNumber)}</b><br>PIN: " +
                    $"<b>{WebUtility.HtmlEncode(v.Pin)}</b></div>");
                i++;
            }

            var tail = $"Quantity: {p.Quantity}\nAmount Paid: {Fmt.Money(p.TotalAmount)}\nTransaction Reference: {p.PaystackReference}\n\nPlease keep these voucher details safe.";
            text.Append(tail);
            html.Append("<p>" + WebUtility.HtmlEncode(tail).Replace("\n\n", "<br><br>").Replace("\n", "<br>") + "</p>");

            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress(s.FromName, s.FromAddress));
            msg.To.Add(new MailboxAddress(p.Customer.FullName, p.Customer.Email));
            msg.Subject = $"Your {exam} Result Checker Vouchers";
            msg.Body = new BodyBuilder { TextBody = text.ToString(), HtmlBody = html.ToString() }.ToMessageBody();



            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(s.Host, s.Port, SecureSocketOptions.StartTls);
            if (!string.IsNullOrEmpty(s.User)) await smtp.AuthenticateAsync(s.User, s.Password);
            await smtp.SendAsync(msg);
            await smtp.DisconnectAsync(true);


        }




    }
}
