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
    [Route("api/appointments")]
    [Authorize]
    public class AppointmentsController : ControllerBase
    {
        private readonly AppointmentRepository _appointmentRepo;
        private readonly PatientRepository _patientRepo;
        private readonly AuditLogRepository _auditRepo;
        private readonly IEmailService _emailService;
        private readonly DoctorRepository _doctorRepo;

        public AppointmentsController(
            AppointmentRepository appointmentRepo, 
            PatientRepository patientRepo, 
            AuditLogRepository auditRepo,
            IEmailService emailService,
            DoctorRepository doctorRepo)
        {
            _appointmentRepo = appointmentRepo;
            _patientRepo = patientRepo;
            _auditRepo = auditRepo;
            _emailService = emailService;
            _doctorRepo = doctorRepo;
        }

        
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAppointmentRequest dto)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            
            int patientId = 0;
            if (User.IsInRole("doctor") || User.IsInRole("receptionist") || User.IsInRole("admin"))
            {
                
                patientId = dto.PatientId ?? 0;
            }
            else
            {
                
                patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            }

            if (patientId == 0) return BadRequest("Patient profile not found.");

            var appointment = new Appointment
            {
                PatientId = patientId,
                CreatedBy = accountId,
                DoctorId = dto.DoctorId,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                TimeSlotId = dto.TimeSlotId,
                Reason = dto.Reason,
                ParentAppointmentId = dto.ParentAppointmentId,
                Status = "scheduled"
            };

            try
            {
                var result = _appointmentRepo.CreateAppointment(appointment);
                
                
                if (!User.IsInRole("doctor") && !User.IsInRole("receptionist") && !User.IsInRole("admin"))
                {
                    var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                    if (!string.IsNullOrEmpty(email))
                    {
                        var patient = _patientRepo.GetPatientByAccountId(accountId);
                        var doctor = _doctorRepo.GetDoctorById(dto.DoctorId);
                        var doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "Specialist";
                        var patientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Patient";
                        
                        await _emailService.SendPatientConfirmationRequestAsync(email, patientName, dto.StartTime, doctorName);
                    }
                }

                _auditRepo.LogAction(accountId, "BOOK_APPOINTMENT", "Appointment", result.AppointmentId, $"Booked appointment with Doctor ID {dto.DoctorId} for {dto.StartTime}", HttpContext.Connection.RemoteIpAddress?.ToString());
                return Ok(new { result.AppointmentId });
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        
        [HttpGet("{id}/followup-details")]
        [Authorize(Roles = "doctor")]
        public IActionResult GetFollowupDetails(int id)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var result = _appointmentRepo.GetFollowupDetails(id, accountId);
            if (result == null) return NotFound("Appointment or follow-up context not found");

            
            

            return Ok(result);
        }

        
        [HttpPost("{id}/finalize")]
        [Authorize(Roles = "doctor")]
        public IActionResult Finalize(int id, [FromBody] FinalizeAppointmentRequest dto)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var success = _appointmentRepo.FinalizeAppointmentOutcome(id, accountId, dto.DoctorReminder);
            if (!success) return BadRequest("Could not finalize appointment.");

            _auditRepo.LogAction(accountId, "FINALIZE_APPOINTMENT", "Appointment", id, "Doctor finalized appointment outcome", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok();
        }

        
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var appointment = _appointmentRepo.GetAppointmentById(id, accountId);
            if (appointment == null) return NotFound("Appointment not found");

            return Ok(appointment);
        }

        
        [HttpGet("doctor-schedule")]
        public IActionResult GetDoctorSchedule()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var appointments = _appointmentRepo.GetDoctorAppointments(accountId);
            return Ok(appointments);
        }

        
        [HttpGet("managed")]
        [Authorize(Roles = "receptionist,admin")]
        public IActionResult GetManagedAppointments()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var appointments = _appointmentRepo.GetCreatedAppointments(accountId);
            return Ok(appointments);
        }

        
        [HttpPut("{id}/cancel")]
        public IActionResult Cancel(int id)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            bool isStaff = User.IsInRole("receptionist") || User.IsInRole("admin");
            var success = _appointmentRepo.CancelAppointment(id, accountId, isStaff);
            if (!success) return BadRequest("Unable to cancel appointment");

            _auditRepo.LogAction(accountId, "CANCEL_APPOINTMENT", "Appointment", id, "User cancelled appointment", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok();
        }

        
        [HttpPut("{id}/reschedule")]
        public IActionResult Reschedule(int id, [FromBody] RescheduleRequest dto)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            bool isStaff = User.IsInRole("receptionist") || User.IsInRole("admin");
            var error = _appointmentRepo.RescheduleAppointment(id, dto.NewStartTime, dto.NewEndTime, accountId, isStaff);
            if (error != null) return BadRequest(error);

            _auditRepo.LogAction(accountId, "RESCHEDULE_APPOINTMENT", "Appointment", id, $"Rescheduled to {dto.NewStartTime}", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok();
        }

        
        [HttpPut("{id}/update-reason")]
        public IActionResult UpdateReason(int id, [FromBody] UpdateAppointmentRequest dto)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            if (string.IsNullOrEmpty(dto.Reason)) return BadRequest("Reason is required");

            var success = _appointmentRepo.UpdateAppointmentReason(id, dto.Reason, accountId);
            if (!success) return BadRequest("Unable to update appointment reason");

            _auditRepo.LogAction(accountId, "UPDATE_APPOINTMENT_REASON", "Appointment", id, "User updated appointment reason", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok();
        }

        
        [HttpGet("{id}/available-slots")]
        [Authorize(Roles = "patient")]
        public IActionResult GetAvailableSlotsForReschedule(int id)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var slots = _appointmentRepo.GetAvailableSlotsForReschedule(id, accountId);
            if (slots == null) return NotFound("Appointment not found");

            return Ok(slots);
        }
        
        [HttpPost("{id}/confirm")]
        [Authorize(Roles = "patient")]
        public async Task<IActionResult> Confirm(int id)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            
            var apt = _appointmentRepo.GetAppointmentById(id, accountId);
            if (apt == null) return NotFound("Appointment not found");

            
            var success = _appointmentRepo.ConfirmAppointment(id, accountId);
            if (!success) return BadRequest("Unable to confirm appointment. It might already be confirmed or not belong to you.");

            
            var patient = _patientRepo.GetPatientByAccountId(accountId);
            var doctor = apt.DoctorId.HasValue ? _doctorRepo.GetDoctorById(apt.DoctorId.Value) : null;
            if (patient != null && !string.IsNullOrEmpty(User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value))
            {
                var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
                var doctorName = doctor != null ? $"{doctor.FirstName} {doctor.LastName}" : "Specialist";
                var patientName = $"{patient.FirstName} {patient.LastName}";
                
                await _emailService.SendAppointmentConfirmedFinalAsync(email, patientName, apt.StartTime ?? DateTime.Now, doctorName);
            }

            _auditRepo.LogAction(accountId, "CONFIRM_APPOINTMENT", "Appointment", id, "Patient confirmed appointment", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { Message = "Appointment confirmed successfully." });
        }
    }
}
