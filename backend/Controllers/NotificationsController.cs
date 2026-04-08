using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Linq;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly NotificationRepository _notificationRepo;
        private readonly PatientRepository _patientRepo;
        private readonly AppointmentRepository _appointmentRepo;

        public NotificationsController(NotificationRepository notificationRepo, PatientRepository patientRepo, AppointmentRepository appointmentRepo)
        {
            _notificationRepo = notificationRepo;
            _patientRepo = patientRepo;
            _appointmentRepo = appointmentRepo;
        }

        // GET: api/notifications/unread
        [HttpGet("unread")]
        public IActionResult GetUnreadNotifications()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return BadRequest("Patient profile not found");

            // Auto-trigger confirmation check
            try { RunConfirmationCheck(); } catch { }

            var notifications = _notificationRepo.GetUnreadNotifications(patientId);
            return Ok(notifications);
        }

        // GET: api/notifications/history
        [HttpGet("history")]
        public IActionResult GetReadNotifications()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return BadRequest("Patient profile not found");

            var notifications = _notificationRepo.GetReadNotifications(patientId);
            return Ok(notifications);
        }

        // GET: api/notifications/my
        [HttpGet("my")]
        public IActionResult GetAllNotifications()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return BadRequest("Patient profile not found");

            var notifications = _notificationRepo.GetPatientNotifications(patientId);
            return Ok(notifications);
        }

        [HttpGet("unread-count")]
        public IActionResult GetUnreadCount()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return Ok(new { count = 0 });

            var count = _notificationRepo.GetUnreadCount(patientId);
            return Ok(new { count });
        }

        [HttpPost("confirm-appointment/{appointmentId}")]
        public IActionResult ConfirmAppointment(int appointmentId)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return Unauthorized();

            var success = _appointmentRepo.ConfirmAppointment(appointmentId, accountId);
            if (!success) return BadRequest("Unable to confirm appointment");

            // Mark the notification as read too if we find it
            var notifications = _notificationRepo.GetPatientNotifications(patientId);
            var note = notifications.FirstOrDefault(n => n.AppointmentId == appointmentId);
            if (note != null) _notificationRepo.MarkAsRead(note.Id, patientId);

            return Ok(new { message = "Appointment confirmed successfully" });
        }

        private void RunConfirmationCheck()
        {
            var upcoming = _appointmentRepo.GetAppointmentsForConfirmation();
            foreach (var appt in upcoming)
            {
                _notificationRepo.CreateNotification(new Notification
                {
                    PatientId = appt.PatientDbId ?? 0,
                    Title = "Appointment Confirmation Required",
                    Message = $"Regarding your upcoming visit: You have an appointment with Dr. {appt.DoctorName} ({appt.Specialty}) scheduled for {appt.StartTime?.ToLocalTime():MMM dd, HH:mm}. Please confirm your attendance to secure your time slot.",
                    Type = "Appointment",
                    AppointmentId = appt.Id
                });
            }
        }

        // POST: api/notifications/send
        [HttpPost("send")]
        [Authorize(Roles = "receptionist,admin,doctor")]
        public IActionResult SendNotification([FromBody] NotificationRequest dto)
        {
            var senderId = int.Parse(User.FindFirst("id")?.Value ?? "0");

            var notification = new Notification
            {
                PatientId = dto.PatientId,
                Title = dto.Title,
                Message = dto.Message,
                Type = dto.Type ?? "General",
                CreatedBy = senderId
            };

            _notificationRepo.CreateNotification(notification);
            return Ok(new { message = "Notification sent successfully" });
        }

        // PUT: api/notifications/{id}/read
        [HttpPut("{id}/read")]
        public IActionResult MarkAsRead(int id)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return Unauthorized();

            _notificationRepo.MarkAsRead(id, patientId);
            return Ok();
        }

        // PUT: api/notifications/read-all
        [HttpPut("read-all")]
        public IActionResult MarkAllAsRead()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return Unauthorized();

            _notificationRepo.MarkAllAsRead(patientId);
            return Ok();
        }
    }

    public class NotificationRequest
    {
        public int PatientId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? Type { get; set; }
    }
}
