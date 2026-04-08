using Microsoft.AspNetCore.Mvc;
using Backend.Data;
using Backend.Dtos.Requests;
using Backend.Dtos.Responses;
using Backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Linq;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/patient")]
    public class PatientController : ControllerBase
    {
        private readonly PatientRepository _patientRepo;
        private readonly AppointmentRepository _appointmentRepo;

        public PatientController(PatientRepository patientRepo, AppointmentRepository appointmentRepo)
        {
            _patientRepo = patientRepo;
            _appointmentRepo = appointmentRepo;
        }

        // GET: api/patients
        [HttpGet]
        [Authorize(Roles = "receptionist,admin,doctor")]
        public IActionResult GetAll()
        {
            var patients = _patientRepo.GetAllPatients();
            var responses = patients.Select(p => new PatientResponse
            {
                PatientId = p.PatientId,
                AccountId = p.AccountId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Phone = p.Phone,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth
            }).ToList();

            return Ok(responses);
        }

        // Dashboard: returns next + recent appointments for logged-in patient
        [Authorize(Roles = "patient")] 
        [HttpGet("dashboard")]
        public IActionResult GetDashboard()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var patient = _patientRepo.GetPatientByAccountId(accountId);
            if (patient == null) return NotFound("Patient not found");

            var allAppointments = _appointmentRepo.GetAppointmentsByPatientId(patient.PatientId);
            var now = DateTime.UtcNow;

            // Find the single "Hero" appointment (Next one > now [with 1hr grace] and in active statuses)
            var next_appt = allAppointments
                .Where(a => (a.Status.ToLower() == "scheduled" || a.Status.ToLower() == "confirmed") && a.StartTime >= now.AddHours(-1))
                .OrderBy(a => a.StartTime)
                .FirstOrDefault();

            var dashboard = new PatientDashboardResponse
            {
                PatientName = $"{patient.FirstName} {patient.LastName}",
                NextAppointment = next_appt
            };
            return Ok(dashboard);
        }

        // GET: api/patient/appointments
        [HttpGet("appointments")]
        public IActionResult GetMyAppointments()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            int patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return Ok(new List<AppointmentResponse>());

            var appointments = _appointmentRepo.GetAppointmentsByPatientId(patientId)
                                .OrderByDescending(a => a.StartTime) 
                                .ToList();

            return Ok(appointments);
        }

        [Authorize(Roles = "patient")]
        [HttpGet("appointments/upcoming")]
        public IActionResult GetUpcomingAppointments()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            int patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return Ok(new List<AppointmentResponse>());

            var appointments = _appointmentRepo.GetUpcomingAppointmentsByPatientId(patientId);
            return Ok(appointments);
        }

        [Authorize(Roles = "patient")]
        [HttpGet("appointments/history")]
        public IActionResult GetHistoryAppointments()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            int patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return Ok(new List<AppointmentResponse>());

            var appointments = _appointmentRepo.GetHistoryAppointmentsByPatientId(patientId);
            return Ok(appointments);
        }

        [Authorize(Roles = "patient")]
        [HttpGet("appointments/pending")]
        public IActionResult GetPendingAppointments()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            int patientId = _patientRepo.GetPatientIdByAccountId(accountId);
            if (patientId == 0) return Ok(new List<AppointmentResponse>());

            var appointments = _appointmentRepo.GetPendingAppointmentsByPatientId(patientId);
            return Ok(appointments);
        }



        [HttpGet("debug-claims")]
        [Authorize] 
        public IActionResult GetClaims()
        {
            var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
            return Ok(claims);
        }
    }
}
