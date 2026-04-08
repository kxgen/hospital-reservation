using Backend.Data;
using Backend.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Backend.Services
{
    public class AppointmentStatusService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEmailService _emailService;
        private readonly ILogger<AppointmentStatusService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(5);
        private DateTime _lastReminderCheck = DateTime.MinValue;

        public AppointmentStatusService(
            IServiceScopeFactory scopeFactory,
            IEmailService emailService,
            ILogger<AppointmentStatusService> logger)
        {
            _scopeFactory = scopeFactory;
            _emailService = emailService;
            _logger = logger;
            _lastReminderCheck = DateTime.Today; // Prevent immediate check on startup
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Appointment Status Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Checking for passed appointments at: {time}", DateTimeOffset.Now);

                try
                {
                    DoWork();
                    await CheckAndSendReminders();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during background service execution.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("Appointment Status Background Service is stopping.");
        }

        private async Task CheckAndSendReminders()
        {
            if (_lastReminderCheck.Date == DateTime.Today) return;

            _logger.LogInformation("Checking for appointment reminders to send for tomorrow.");

            using var scope = _scopeFactory.CreateScope();
            var appointmentRepo = scope.ServiceProvider.GetRequiredService<AppointmentRepository>();
            var tomorrow = DateTime.Today.AddDays(1);
            var appointments = appointmentRepo.GetAppointmentsForReminders(tomorrow);

            foreach (var appt in appointments)
            {
                if (string.IsNullOrEmpty(appt.Email)) continue;

                try
                {
                    await _emailService.SendAppointmentReminderAsync(appt.Email, appt.PatientName ?? "Patient", appt.StartTime ?? tomorrow, appt.DoctorName ?? "Doctor");
                    _logger.LogInformation("Sent reminder to {email} for appointment {id}", appt.Email, appt.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email reminder for appointment {id}", appt.Id);
                }
            }

            _lastReminderCheck = DateTime.Today;
        }

        private void DoWork()
        {
            using var scope = _scopeFactory.CreateScope();
            var appointmentRepo = scope.ServiceProvider.GetRequiredService<AppointmentRepository>();
            var notificationRepo = scope.ServiceProvider.GetRequiredService<NotificationRepository>();

            var affectedAppointments = appointmentRepo.UpdatePassedAppointmentsToPending();

            if (affectedAppointments.Count > 0)
            {
                _logger.LogInformation("Updated {count} appointments to 'pending'.", affectedAppointments.Count);

                foreach (var appt in affectedAppointments)
                {
                    try
                    {
                        notificationRepo.CreateNotification(new Notification
                        {
                            PatientId = appt.PatientDbId ?? 0,
                            Title = "Appointment Time Passed",
                            Message = $"Your appointment with Dr. {appt.DoctorName} on {appt.StartTime:MMM dd, hh:mm tt} has passed. Please contact the medical center or check with reception for next steps.",
                            Type = "System",
                            AppointmentId = appt.Id
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send notification for appointment {id}", appt.Id);
                    }
                }
            }
        }
    }
}
