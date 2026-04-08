using Backend.Data;
using Backend.Dtos.Requests;
using Backend.Dtos.Responses;
using Backend.Models;
using Backend.Utils;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "admin")]
    public class AdminController : ControllerBase
    {
        private readonly AccountRepository _accountRepo;
        private readonly AppointmentRepository _appointmentRepo;
        private readonly AuditLogRepository _auditRepo;
        private readonly DoctorRepository _doctorRepo;
        private readonly PatientRepository _patientRepo;
        private readonly ReceptionistRepository _receptionistRepo;
        private readonly AdminRepository _adminOneRepo;
        private readonly DoctorAvailabilityRepository _availabilityRepo;

        public AdminController(
            AccountRepository accountRepo, 
            DoctorRepository doctorRepo, 
            PatientRepository patientRepo,
            ReceptionistRepository receptionistRepo,
            AdminRepository adminRepo,
            IConfiguration config)
        {
            _accountRepo = accountRepo;
            _doctorRepo = doctorRepo;
            _patientRepo = patientRepo;
            _receptionistRepo = receptionistRepo;
            _adminOneRepo = adminRepo;
            
            var connStr = config.GetConnectionString("DefaultConnection") ?? "";
            _appointmentRepo = new AppointmentRepository(connStr);
            _auditRepo = new AuditLogRepository(connStr);
            _availabilityRepo = new DoctorAvailabilityRepository(connStr);
        }

        [HttpGet("dashboard/stats")]
        public IActionResult GetStats()
        {
            var (doctors, receptionists, patients) = _accountRepo.GetSystemStats();
            var appointmentsToday = _appointmentRepo.GetTodayAppointmentsCount();

            return Ok(new SystemStatsResponse
            {
                Doctors = doctors,
                Receptionists = receptionists,
                Patients = patients,
                AppointmentsToday = appointmentsToday
            });
        }

        [HttpGet("dashboard/logs")]
        public IActionResult GetDashboardLogs()
        {
            return GetLogsInternal(5);
        }

        [HttpGet("dashboard/weekly-stats")]
        public IActionResult GetWeeklyStats()
        {
            var stats = _appointmentRepo.GetWeeklyAppointmentsCount();
            return Ok(stats);
        }

        [HttpGet("dashboard/registration-stats")]
        public IActionResult GetRegistrationStats()
        {
            var stats = _accountRepo.GetRegistrationStats();
            return Ok(stats);
        }

        [HttpGet("logs")]
        public IActionResult GetAllLogs(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 10, 
            [FromQuery] int? accountId = null,
            [FromQuery] DateTime? date = null)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100;

            var totalLogs = _auditRepo.GetTotalLogsCount(accountId, date);
            var logs = _auditRepo.GetLogsPaginated(page, pageSize, accountId, date);

            var totalPages = (int)Math.Ceiling(totalLogs / (double)pageSize);

            return Ok(new PaginatedResponse<AuditLogResponse>
            {
                Items = logs,
                TotalCount = totalLogs,
                Page = page,
                PageSize = pageSize,
                TotalPages = totalPages
            });
        }

        private IActionResult GetLogsInternal(int limit)
        {
            var logs = _auditRepo.GetRecentLogs(limit);
            return Ok(logs);
        }

        [HttpGet("staff")]
        public IActionResult GetStaff()
        {
            var accounts = _accountRepo.GetAllAccounts().Where(a => a.RoleName != "patient");
            var result = accounts.Select(a => {
                var response = new StaffResponse
                {
                    Id = a.AccountId,
                    Email = a.Email,
                    Role = a.RoleName,
                    IsSuspended = !a.IsActive
                };

                if (a.RoleName == "doctor") {
                    var d = _doctorRepo.GetDoctorByAccountId(a.AccountId);
                    if (d != null) { response.FirstName = d.FirstName; response.LastName = d.LastName; response.Phone = d.Phone; response.SpecialtyId = d.SpecialtyId; }
                } else if (a.RoleName == "receptionist") {
                    var r = _receptionistRepo.GetReceptionistByAccountId(a.AccountId);
                    if (r != null) { response.FirstName = r.FirstName; response.LastName = r.LastName; response.Phone = r.Phone; }
                } else if (a.RoleName == "admin") {
                    var ad = _adminOneRepo.GetAdminByAccountId(a.AccountId);
                    if (ad != null) { response.FirstName = ad.FirstName; response.LastName = ad.LastName; response.Phone = ad.Phone; }
                }

                return response;
            });
            return Ok(result);
        }

        [HttpGet("staff/{id}")]
        public IActionResult GetStaffById(int id)
        {
            var account = _accountRepo.GetAccountById(id);
            if (account == null) return NotFound();

            var response = new StaffResponse
            {
                Id = account.AccountId,
                Email = account.Email,
                Role = account.RoleName,
                IsSuspended = !account.IsActive
            };

            if (account.RoleName == "doctor") {
                var d = _doctorRepo.GetDoctorByAccountId(account.AccountId);
                if (d != null) { response.FirstName = d.FirstName; response.LastName = d.LastName; response.Phone = d.Phone; response.Bio = d.Bio; response.SpecialtyId = d.SpecialtyId; }
            } else if (account.RoleName == "receptionist") {
                var r = _receptionistRepo.GetReceptionistByAccountId(account.AccountId);
                if (r != null) { response.FirstName = r.FirstName; response.LastName = r.LastName; response.Phone = r.Phone; }
            } else if (account.RoleName == "admin") {
                var ad = _adminOneRepo.GetAdminByAccountId(account.AccountId);
                if (ad != null) { response.FirstName = ad.FirstName; response.LastName = ad.LastName; response.Phone = ad.Phone; }
            }

            return Ok(response);
        }

        [HttpPut("staff/{id}")]
        public IActionResult UpdateStaff(int id, [FromBody] UpdateStaffRequest dto)
        {
            var account = _accountRepo.GetAccountById(id);
            if (account == null) return NotFound();

            
            _accountRepo.UpdateStatus(id, !dto.IsSuspended);

            
            var profileUpdate = new UpdateProfileRequest
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Bio = dto.Bio,
                SpecialtyId = dto.SpecialtyId
                
            };

            if (account.RoleName == "doctor")
            {
                _doctorRepo.UpdateDoctor(id, profileUpdate);
            }
            else if (account.RoleName == "receptionist")
            {
                _receptionistRepo.UpdateReceptionist(id, profileUpdate);
            }
            else if (account.RoleName == "admin")
            {
                _adminOneRepo.UpdateAdmin(id, profileUpdate);
            }

            _auditRepo.LogAction(int.Parse(User.FindFirst("id")?.Value ?? "0"), "UPDATE_ACCOUNT", "Account", id, $"Updated account details for {dto.Email}");

            return Ok(new { Message = "Staff member updated successfully" });
        }

        [HttpPost("staff/{id}/toggle-status")]
        public IActionResult ToggleStaffStatus(int id)
        {
            var account = _accountRepo.GetAccountById(id);
            if (account == null) return NotFound();
            
            _accountRepo.UpdateStatus(id, !account.IsActive);
            
            _auditRepo.LogAction(int.Parse(User.FindFirst("id")?.Value ?? "0"), "TOGGLE_STAFF_STATUS", "Account", id, $"Toggled status to {!account.IsActive}");
            return Ok();
        }

        [HttpPost("staff/{id}/reset-password")]
        public IActionResult ResetPassword(int id, [FromBody] SetPasswordRequest dto)
        {
            var account = _accountRepo.GetAccountById(id);
            if (account == null) return NotFound();

            var newHash = PasswordHasher.Hash(dto.Password);
            
            _accountRepo.UpdatePassword(id, newHash, true);

            _auditRepo.LogAction(int.Parse(User.FindFirst("id")?.Value ?? "0"), "ADMIN_RESET_PASSWORD", "Account", id, $"Admin reset password for {account.Email}");
            return Ok(new { Message = "Password reset successfully. User will be forced to change it on next login." });
        }

        [HttpGet("patients")]
        public IActionResult GetPatients()
        {
            var patients = _patientRepo.GetAllPatients();
            var result = patients.Select(p => {
                bool isActive = true;
                string email = "N/A";
                if (p.AccountId.HasValue)
                {
                    var acc = _accountRepo.GetAccountById(p.AccountId.Value);
                    if (acc != null) {
                        isActive = acc.IsActive;
                        email = acc.Email;
                    }
                }
                return new PatientResponse
                {
                    PatientId = p.PatientId,
                    AccountId = p.AccountId,
                    FirstName = p.FirstName,
                    LastName = p.LastName,
                    Email = email,
                    Phone = p.Phone,
                    Gender = p.Gender,
                    DateOfBirth = p.DateOfBirth,
                    IsDisabled = !isActive
                };
            });
            return Ok(result);
        }

        [HttpGet("patients/{id}")]
        public IActionResult GetPatientById(int id)
        {
            var p = _patientRepo.GetPatientById(id);
            if (p == null) return NotFound();

            bool isActive = true;
            string email = "N/A";
            if (p.AccountId.HasValue)
            {
                var acc = _accountRepo.GetAccountById(p.AccountId.Value);
                if (acc != null) {
                    isActive = acc.IsActive;
                    email = acc.Email;
                }
            }

            return Ok(new PatientResponse
            {
                PatientId = p.PatientId,
                AccountId = p.AccountId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Email = email,
                Phone = p.Phone,
                Gender = p.Gender,
                DateOfBirth = p.DateOfBirth,
                IsDisabled = !isActive
            });
        }

        [HttpPut("patients/{id}")]
        public IActionResult UpdatePatient(int id, [FromBody] UpdatePatientAdminRequest dto)
        {
            var patient = _patientRepo.GetPatientById(id);
            if (patient == null) return NotFound();

            
            _patientRepo.UpdatePatientById(id, new UpdateProfileRequest 
            {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth?.ToString("yyyy-MM-dd")
            });

            _auditRepo.LogAction(int.Parse(User.FindFirst("id")?.Value ?? "0"), "UPDATE_ACCOUNT", "Patient", id, $"Updated account details for {dto.Email}");

            return Ok(new { Message = "Patient updated successfully" });
        }

        [HttpPost("patients/{id}/toggle-disable")]
        public IActionResult TogglePatientStatus(int id)
        {
            var patient = _patientRepo.GetPatientById(id);
            if (patient == null) return NotFound();

            if (patient.AccountId.HasValue)
            {
                var account = _accountRepo.GetAccountById(patient.AccountId.Value);
                if (account != null)
                {
                    _accountRepo.UpdateStatus(account.AccountId, !account.IsActive);
                    _auditRepo.LogAction(int.Parse(User.FindFirst("id")?.Value ?? "0"), "TOGGLE_PATIENT_STATUS", "Account", account.AccountId, $"Toggled patient account status to {!account.IsActive}");
                }
            }
            else
            {
                
                 _auditRepo.LogAction(int.Parse(User.FindFirst("id")?.Value ?? "0"), "TOGGLE_PATIENT_STATUS", "Patient", id, "Attempted to toggle status of a guest patient (No account)");
            }
            return Ok();
        }

        [HttpPost("patients/{id}/reset-password")]
        public IActionResult ResetPatientPassword(int id, [FromBody] SetPasswordRequest dto)
        {
            var patient = _patientRepo.GetPatientById(id);
            if (patient == null) return NotFound();

            if (!patient.AccountId.HasValue) return BadRequest(new { Message = "This patient does not have an account." });

            var newHash = PasswordHasher.Hash(dto.Password);
            _accountRepo.UpdatePassword(patient.AccountId.Value, newHash, true);

            _auditRepo.LogAction(int.Parse(User.FindFirst("id")?.Value ?? "0"), "ADMIN_RESET_PATIENT_PASSWORD", "Account", patient.AccountId.Value, $"Admin reset password for patient {patient.FirstName}");
            return Ok(new { Message = "Password reset successfully." });
        }



        [HttpPost("createStaff")]
        public IActionResult RegisterStaff([FromBody] StaffRegisterRequest dto)
        {
            if (_accountRepo.EmailExists(dto.Email))
            {
                return Conflict(new { Message = "Email already exists." });
            }

            string roleName = dto.Role switch
            {
                2 => "doctor",
                3 => "receptionist",
                _ => "unknown"
            };

            if (roleName == "unknown")
            {
                return BadRequest(new { Message = "Invalid Role ID provided. Only Doctor and Receptionist can be created." });
            }

            var account = new Account
            {
                Email = dto.Email,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                RoleId = dto.Role,
                IsActive = true,
                IsPasswordChangeRequired = true
            };

            int accountId = _accountRepo.CreateAccount(account);

            if (roleName == "doctor")
            {
                if (dto.SpecialtyId <= 0) return BadRequest(new { Message = "Specialty required for doctor." });
                var doc = new Doctor {
                    AccountId = accountId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Gender = dto.Gender,
                    Phone = dto.Phone,
                    SpecialtyId = dto.SpecialtyId
                };
                _doctorRepo.CreateDoctor(doc);
            }
            else if (roleName == "receptionist")
            {
                 var rec = new Receptionist {
                    AccountId = accountId,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    Gender = dto.Gender,
                    Phone = dto.Phone
                };
                _receptionistRepo.CreateReceptionist(rec);
            }

            return Ok(new { Message = $"{roleName} registered successfully.", UserId = accountId });
        }

        [HttpGet("doctors/{doctorId}/availability")]
        public IActionResult GetDoctorAvailability(int doctorId)
        {
            
            var doctor = _doctorRepo.GetDoctorByAccountId(doctorId);
            
            if (doctor == null) doctor = _doctorRepo.GetDoctorById(doctorId);
            if (doctor == null) return NotFound(new { Message = "Doctor not found" });

            var schedule = _availabilityRepo.GetDoctorAvailability(doctor.DoctorId);
            return Ok(new DoctorAvailabilityResponse { Schedule = schedule });
        }

        [HttpPost("doctors/{doctorId}/availability")]
        public IActionResult SaveDoctorAvailability(int doctorId, [FromBody] DoctorAvailabilityRequest request)
        {
            try 
            {
                var doctor = _doctorRepo.GetDoctorByAccountId(doctorId);
                if (doctor == null) doctor = _doctorRepo.GetDoctorById(doctorId);
                if (doctor == null) return NotFound(new { Message = "Doctor not found" });

                _availabilityRepo.SaveDoctorAvailability(doctor.DoctorId, request.Schedule);
                
                // Get the account ID of the admin performing the action
                var adminAccountIdStr = User.FindFirst("id")?.Value;
                int? adminAccountId = string.IsNullOrEmpty(adminAccountIdStr) ? null : int.Parse(adminAccountIdStr);

                _auditRepo.LogAction(
                    adminAccountId, 
                    "UPDATE_DOCTOR_AVAILABILITY", 
                    "DoctorAvailability", 
                    doctor.DoctorId, 
                    $"Updated weekly availability for doctor {doctor.FirstName} {doctor.LastName}"
                );

                return Ok(new { Message = "Availability saved successfully" });
            }
            catch (Exception ex)
            {
                // Log the actual error for the developer (in a real app we'd use a logger)
                Console.WriteLine($"Error saving availability: {ex.Message}");
                return StatusCode(500, new { Message = "Failed to save availability", Detail = ex.Message });
            }
        }

        [HttpPost("doctors/{doctorId}/generate-slots")]
        public IActionResult GenerateTimeSlots(int doctorId, [FromBody] GenerateTimeSlotsRequest request)
        {
            
            var doctor = _doctorRepo.GetDoctorByAccountId(doctorId);
            
            if (doctor == null) doctor = _doctorRepo.GetDoctorById(doctorId);
            if (doctor == null) return NotFound(new { Message = "Doctor not found" });

            
            
            
            return Ok(new { Message = "System now uses dynamic slot generation based on availability. No need to manual generate." });
        }
    }
}
