namespace Backend.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml = true);
        Task SendPasswordResetOtpAsync(string to, string otp);
        Task SendAppointmentReminderAsync(string to, string patientName, DateTime appointmentDate, string doctorName);
        Task SendTemporaryPasswordAsync(string to, string tempPassword);
        Task SendPatientConfirmationRequestAsync(string to, string patientName, DateTime appointmentDate, string doctorName);
        Task SendAppointmentConfirmedFinalAsync(string to, string patientName, DateTime appointmentDate, string doctorName);
    }
}
