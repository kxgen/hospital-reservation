using Backend.Data;
using Backend.Dtos.Requests;
using Backend.Dtos.Responses;
using Backend.Models;
using Backend.Utils;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly AccountRepository _accountRepo;
        private readonly PatientRepository _patientRepo;
        private readonly DoctorRepository _doctorRepo;
        private readonly AdminRepository _adminRepo;
        private readonly ReceptionistRepository _receptionistRepo;
        private readonly JwtGenerator _jwtGenerator;
        private readonly AuditLogRepository _auditRepo;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _config;

        public AccountController(
            AccountRepository accountRepo, 
            PatientRepository patientRepo, 
            DoctorRepository doctorRepo,
            AdminRepository adminRepo,
            ReceptionistRepository receptionistRepo,
            JwtGenerator jwtGenerator, 
            IEmailService emailService,
            IConfiguration config)
        {
            _accountRepo = accountRepo;
            _patientRepo = patientRepo;
            _doctorRepo = doctorRepo;
            _adminRepo = adminRepo;
            _receptionistRepo = receptionistRepo;
            _jwtGenerator = jwtGenerator;
            _emailService = emailService;
            _config = config;
            _auditRepo = new AuditLogRepository(_config.GetConnectionString("DefaultConnection") ?? "");
        }

        [HttpPost("register")]
        public IActionResult Register([FromBody] RegisterRequest dto)
        {
            if (dto.Password != dto.ConfirmPassword)
                return BadRequest(new { Message = "Passwords do not match." });

            if (_accountRepo.EmailExists(dto.Email))
                return Conflict(new { Message = "Email already exists." });

            
            var roleId = _accountRepo.GetRoleIdByName("patient");
            if (roleId == null)
            {
                return StatusCode(500, new { Message = "Patient role not configured in database." });
            }

            var account = new Account
            {
                Email = dto.Email,
                PasswordHash = PasswordHasher.Hash(dto.Password),
                RoleId = roleId.Value,
                IsActive = true
            };
            
            int accountId = _accountRepo.CreateAccount(account);
            account.AccountId = accountId;

            
            DateTime? dob = null;
            if (!string.IsNullOrEmpty(dto.DateOfBirth)) 
            {
                if (DateTime.TryParse(dto.DateOfBirth, out var parsedDob))
                {
                    dob = parsedDob;
                }
            }
            
            var patient = new Patient
            {
                AccountId = accountId,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Phone = dto.Phone,
                Gender = dto.Gender,
                DateOfBirth = dob
            };
            _patientRepo.CreateLinkPatient(patient);

            _auditRepo.LogAction(accountId, "REGISTER", "Account", accountId, "Registered as patient", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { Message = "User registered successfully.", UserId = accountId });
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest dto)
        {
            var account = _accountRepo.GetAccountByEmail(dto.Email);
            
            if (account == null || !PasswordHasher.Verify(dto.Password, account.PasswordHash))
                return Unauthorized(new { Message = "Invalid email or password." });

            if (!account.IsActive)
                return Unauthorized(new { Message = "Account is inactive." });

            
            string firstName = "User";
            string lastName = "";
            string? phone = null;
            string? gender = null;
            int profileId = 0; 

            var rName = account.RoleName.ToLower();
            if (rName == "patient")
            {
                var p = _patientRepo.GetPatientByAccountId(account.AccountId);
                if (p != null) { firstName = p.FirstName; lastName = p.LastName; phone = p.Phone; gender = p.Gender; profileId = p.PatientId; }
            }
            else if (rName == "doctor")
            {
                var d = _doctorRepo.GetDoctorByAccountId(account.AccountId);
                if (d != null) { firstName = d.FirstName; lastName = d.LastName; phone = d.Phone; gender = d.Gender; profileId = d.DoctorId; }
            }
            else if (rName == "receptionist")
            {
                var r = _receptionistRepo.GetReceptionistByAccountId(account.AccountId);
                if (r != null) { firstName = r.FirstName; lastName = r.LastName; phone = r.Phone; gender = r.Gender; profileId = r.ReceptionistId; }
            }
            else if (rName == "admin")
            {
                var a = _adminRepo.GetAdminByAccountId(account.AccountId);
                if (a != null) { firstName = a.FirstName; lastName = a.LastName; phone = a.Phone; gender = a.Gender; profileId = a.AdminId; }
            }

            var token = _jwtGenerator.GenerateJwtToken(account, $"{firstName} {lastName}");

            var authUser = new AuthResponse
            {
                Id = account.AccountId, 
                FirstName = firstName,
                LastName = lastName,
                Email = account.Email,
                Role = rName, 
                Phone = phone,
                Gender = gender,
                IsPasswordChangeRequired = account.IsPasswordChangeRequired
            };

            _auditRepo.LogAction(account.AccountId, "LOGIN", "Account", account.AccountId, "User logged in", HttpContext.Connection.RemoteIpAddress?.ToString());

            return Ok(new { Token = token, User = authUser });
        }

        [HttpPost("change-password")]
        [Authorize]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest dto)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var account = _accountRepo.GetAccountById(accountId);
            if (account == null) return NotFound();

            
            if (!account.IsPasswordChangeRequired)
            {
                 if (string.IsNullOrEmpty(dto.Current) || !PasswordHasher.Verify(dto.Current, account.PasswordHash))
                    return BadRequest(new { Message = "Current password is incorrect." });
            }

            var newHash = PasswordHasher.Hash(dto.New);
            
            _accountRepo.UpdatePassword(accountId, newHash, false);

            _auditRepo.LogAction(accountId, "CHANGE_PASSWORD", "Account", accountId, "User changed password (mandatory reset cleared)");

            return Ok(new { Message = "Password updated successfully." });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] RequestOtpRequest dto)
        {
            var account = _accountRepo.GetAccountByEmail(dto.Email);
            if (account == null)
            {
                
                return Ok(new { Message = "If an account with that email exists, an OTP has been sent." });
            }

            
            var otp = new Random().Next(100000, 999999).ToString();
            var otpHash = PasswordHasher.Hash(otp);
            var expiry = DateTime.UtcNow.AddMinutes(15);

            _accountRepo.UpdateOtp(account.AccountId, otpHash, expiry);

            try
            {
                await _emailService.SendPasswordResetOtpAsync(account.Email, otp);
                _auditRepo.LogAction(account.AccountId, "OTP_SENT", "Account", account.AccountId, "Password reset OTP sent to email", HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Failed to send email. Please try again later.", Detail = ex.Message });
            }

            return Ok(new { Message = "If an account with that email exists, an OTP has been sent." });
        }

        [HttpPost("reset-password-otp")]
        public async Task<IActionResult> ResetPasswordWithOtp([FromBody] ResetPasswordWithOtpRequest dto)
        {
            var account = _accountRepo.GetAccountByEmail(dto.Email);
            if (account == null)
                return BadRequest(new { Message = "Invalid request." });

            if (string.IsNullOrEmpty(account.PasswordResetOtpHash) || account.PasswordResetOtpExpiry == null)
                return BadRequest(new { Message = "No active reset request found." });

            if (account.PasswordResetOtpExpiry < DateTime.UtcNow)
                return BadRequest(new { Message = "OTP has expired." });

            if (!PasswordHasher.Verify(dto.Otp, account.PasswordResetOtpHash))
            {
                _auditRepo.LogAction(account.AccountId, "OTP_FAILED", "Account", account.AccountId, "Invalid OTP attempt", HttpContext.Connection.RemoteIpAddress?.ToString());
                return BadRequest(new { Message = "Invalid OTP." });
            }

            
            var tempPassword = Guid.NewGuid().ToString("N").Substring(0, 10);
            var newHash = PasswordHasher.Hash(tempPassword);
            
            
            _accountRepo.UpdatePassword(account.AccountId, newHash, true);
            _accountRepo.ClearOtp(account.AccountId);

            try
            {
                await _emailService.SendTemporaryPasswordAsync(account.Email, tempPassword);
                _auditRepo.LogAction(account.AccountId, "TEMP_PASSWORD_SENT", "Account", account.AccountId, "Temporary password sent to email after OTP verification", HttpContext.Connection.RemoteIpAddress?.ToString());
            }
            catch (Exception ex)
            {
                
                
                
                return StatusCode(500, new { Message = "OTP verified but failed to send temporary password email.", Detail = ex.Message });
            }

            return Ok(new { Message = "Your identity has been verified. A temporary password has been sent to your email." });
        }
    }
}
