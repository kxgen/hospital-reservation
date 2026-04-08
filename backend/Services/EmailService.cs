using Backend.Models;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using Microsoft.Extensions.Logging;

namespace Backend.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string body, bool isHtml = true)
        {
            _logger.LogInformation("Attempting to send email to {to} using sender {sender}", to, _emailSettings.SenderEmail);
            
            // Whitelist Check
            if (_emailSettings.AllowedEmails.Count > 0 && !_emailSettings.AllowedEmails.Contains(to, StringComparer.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Blocking email to {to} because it is not in the AllowedEmails whitelist.", to);
                return;
            }

            if (string.IsNullOrEmpty(_emailSettings.SenderEmail) || string.IsNullOrEmpty(_emailSettings.EmailPassword))
            {
                _logger.LogError("Email credentials are not configured! SenderEmail: {email}, Password set: {isPassSet}", 
                    _emailSettings.SenderEmail, !string.IsNullOrEmpty(_emailSettings.EmailPassword));
                throw new InvalidOperationException("Email service is not configured correctly.");
            }

            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart(isHtml ? TextFormat.Html : TextFormat.Plain) { Text = body };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_emailSettings.SenderEmail, _emailSettings.EmailPassword);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }

        public async Task SendPasswordResetOtpAsync(string to, string otp)
        {
            string subject = "Password Reset Request - Trinity Specialized Center";
            string body = $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #2c3e50;'>Password Reset</h2>
                    <p>You requested to reset your password. Use the following 6-digit code to proceed:</p>
                    <div style='background-color: #f8f9fa; padding: 15px; text-align: center; border-radius: 4px; margin: 20px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 5px; color: #3498db;'>{otp}</span>
                    </div>
                    <p>This code will expire in 15 minutes.</p>
                    <p style='color: #7f8c8d; font-size: 12px; margin-top: 30px;'>If you did not request this, please ignore this email or contact support.</p>
                    <hr style='border: 0; border-top: 1px solid #eeeeee; margin: 20px 0;'>
                    <p style='font-size: 14px; color: #2c3e50;'>Trinity Specialized Center</p>
                </div>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendAppointmentReminderAsync(string to, string patientName, DateTime appointmentDate, string doctorName)
        {
            string subject = "Appointment Reminder - Trinity Specialized Center";
            string body = $@"
                <div style='font-family: sans-serif;'>
                    <p>Hello {patientName}, we are looking forward to seeing you. This is a reminder of your upcoming appointment with <strong>Dr. {doctorName}</strong> scheduled for <strong>{appointmentDate:dddd, MMMM dd}</strong> at <strong>{appointmentDate:hh:mm tt}</strong>.</p>
                    <p>Please confirm your attendance through the hospital app at your earliest convenience.</p>
                </div>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendTemporaryPasswordAsync(string to, string tempPassword)
        {
            string subject = "Temporary Password - Trinity Specialized Center";
            string body = $@"
                <div style='font-family: sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                    <h2 style='color: #2c3e50;'>Temporary Password</h2>
                    <p>Your OTP has been verified. Here is your temporary password:</p>
                    <div style='background-color: #f8f9fa; padding: 15px; text-align: center; border-radius: 4px; margin: 20px 0;'>
                        <span style='font-size: 24px; font-weight: bold; color: #e67e22;'>{tempPassword}</span>
                    </div>
                    <p>For security reasons, you will be required to change this password upon your next login.</p>
                    <p style='color: #7f8c8d; font-size: 12px; margin-top: 30px;'>If you did not request this, please contact support immediately.</p>
                    <hr style='border: 0; border-top: 1px solid #eeeeee; margin: 20px 0;'>
                    <p style='font-size: 14px; color: #2c3e50;'>Trinity Specialized Center</p>
                </div>";

            await SendEmailAsync(to, subject, body);
        }
        public async Task SendPatientConfirmationRequestAsync(string to, string patientName, DateTime appointmentDate, string doctorName)
        {
            string subject = "Appointment Confirmation Request - Trinity Specialized Center";
            string body = $@"
                <div style='font-family: sans-serif;'>
                    <p>Hello {patientName}, thank you for choosing Trinity Specialized Center. You have a new appointment with <strong>Dr. {doctorName}</strong> on <strong>{appointmentDate:dddd, MMMM dd}</strong> at <strong>{appointmentDate:hh:mm tt}</strong>.</p>
                    <p>To help us prepare for your visit, please confirm your appointment through the hospital app.</p>
                </div>";

            await SendEmailAsync(to, subject, body);
        }

        public async Task SendAppointmentConfirmedFinalAsync(string to, string patientName, DateTime appointmentDate, string doctorName)
        {
            string subject = "Appointment Confirmed - Trinity Specialized Center";
            string body = $@"
                <div style='font-family: sans-serif;'>
                    <p>Hello {patientName}, your appointment with <strong>Dr. {doctorName}</strong> on <strong>{appointmentDate:dddd, MMMM dd}</strong> at <strong>{appointmentDate:hh:mm tt}</strong> has been successfully confirmed.</p>
                    <p>Everything is ready for your visit, and we look forward to seeing you soon!</p>
                </div>";

            await SendEmailAsync(to, subject, body);
        }
    }
}
