using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Dtos.Requests;
using Backend.Dtos.Responses;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Backend.Services;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/receptionist")]
    [Authorize(Roles = "receptionist")]
    public class ReceptionistController : ControllerBase
    {
        private readonly ReceptionistRepository _receptionistRepo;
        private readonly AccountRepository _accountRepo;
        private readonly AppointmentRepository _appointmentRepo;
        private readonly DoctorRepository _doctorRepo;
        private readonly PatientRepository _patientRepo;
        private readonly NotificationRepository _notificationRepo;
        private readonly IEmailService _emailService;
        private readonly AuditLogRepository _auditRepo;

        public ReceptionistController(
            ReceptionistRepository receptionistRepo, 
            AccountRepository accountRepo,
            IEmailService emailService,
            AuditLogRepository auditRepo,
            IConfiguration config)
        {
            var connStr = config.GetConnectionString("DefaultConnection") ?? "";
            _receptionistRepo = receptionistRepo;
            _accountRepo = accountRepo;
            _emailService = emailService;
            _auditRepo = auditRepo;
            _appointmentRepo = new AppointmentRepository(connStr);
            _doctorRepo = new DoctorRepository(connStr);
            _patientRepo = new PatientRepository(connStr);
            _notificationRepo = new NotificationRepository(connStr);
        }

        [HttpGet("dashboard/stats")]
        public IActionResult GetDashboardStats()
        {
            var (doctors, receptionists, patients) = _accountRepo.GetSystemStats();
            var todayStats = _appointmentRepo.GetTodayDetailedStats();

            return Ok(new ReceptionDashboardStatsResponse
            {
                Doctors = doctors,
                Receptionists = receptionists,
                Patients = patients,
                Today = todayStats
            });
        }

        [HttpGet("dashboard/weekly-stats")]
        public IActionResult GetWeeklyStats()
        {
            var stats = _appointmentRepo.GetWeeklyAppointmentsCount();
            return Ok(stats);
        }



        [HttpGet("appointments/upcoming")]
        public IActionResult GetUpcomingAppointments()
        {
            var appointments = _appointmentRepo.GetAllUpcomingAppointments();
            return Ok(appointments);
        }

        [HttpGet("appointments/escalations")]
        public IActionResult GetEscalations()
        {
            var escalations = _appointmentRepo.GetUnconfirmedEscalations();
            return Ok(escalations);
        }

        [HttpGet("appointments/pending")]
        public IActionResult GetPendingAppointments()
        {
            var pending = _appointmentRepo.GetAllPendingAppointments();
            return Ok(pending);
        }

        [HttpPost("appointments/{id}/no-show")]
        public IActionResult MarkNoShow(int id)
        {
            var receptionistUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            var success = _appointmentRepo.MarkAsNoShow(id);
            if (!success) return BadRequest("Unable to mark as no-show.");

            _auditRepo.LogAction(receptionistUserId, "MARK_NO_SHOW", "Appointment", id, "Receptionist marked as no-show", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { Message = "Marked as no-show successfully." });
        }



        [HttpPost("book")]
        public IActionResult BookForPatient([FromBody] ReceptionistBookRequest dto)
        {
            var receptionistUserId = int.Parse(User.FindFirst("id")?.Value ?? "0");

            // 1. Explicit Link or Create New
            int patientId = 0;
            if (dto.PatientId.HasValue && dto.PatientId.Value > 0)
            {
                patientId = dto.PatientId.Value;
            }
            
            if (patientId == 0)
            {
                // 2. Create new Guest Patient (No User account)
                DateTime? dob = null;
                if (!string.IsNullOrEmpty(dto.DateOfBirth))
                {
                    DateTime.TryParse(dto.DateOfBirth, out DateTime parsedDob);
                    if (parsedDob != DateTime.MinValue) dob = parsedDob;
                }

                patientId = _patientRepo.CreateGuestPatient(
                    dto.FirstName, 
                    dto.LastName, 
                    dto.Phone, 
                    dto.Gender, 
                    dob
                );
            }

            // 3. Create Appointment
            var appointment = new Models.Appointment
            {
                PatientId = patientId,
                CreatedBy = receptionistUserId,
                DoctorId = dto.DoctorId, // New
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                TimeSlotId = dto.TimeSlotId,
                Reason = dto.Reason,
                Status = "scheduled"
            };

            try
            {
                var result = _appointmentRepo.CreateAppointment(appointment);

                return Ok(new { Message = "Appointment booked successfully", AppointmentId = result.AppointmentId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpGet("my-bookings")]
        public IActionResult GetMyBookings()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var appointments = _appointmentRepo.GetAppointmentsByCreator(accountId);
            return Ok(appointments);
        }

        [HttpPost("check-in")]
        public IActionResult CheckIn([FromBody] CheckIn dto)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var success = _appointmentRepo.CheckInAppointment(dto.Id);

            if (!success)
                return BadRequest("Cannot check in. Either appointment not found, already checked in, or too early.");

            return Ok("Patient successfully checked in.");
        }



        [HttpGet("search-patient")]
        public IActionResult SearchPatient([FromQuery] string phone)
        {
            if (string.IsNullOrEmpty(phone)) return BadRequest("Phone number required.");

            int patientId = _patientRepo.GetPatientIdByPhone(phone);
            if (patientId == 0) return NotFound("Patient not found.");

            var patient = _patientRepo.GetPatientById(patientId);
            if (patient == null) return NotFound("Patient record missing.");

            return Ok(new
            {
                patient.PatientId,
                patient.FirstName,
                patient.LastName,
                patient.Phone,
                patient.Gender,
                patient.DateOfBirth
            });
        }
    }
}
