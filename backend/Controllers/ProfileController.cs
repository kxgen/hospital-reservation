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
    [Route("api/profile")]
    [Authorize]
    public class ProfileController : ControllerBase
    {
        private readonly AccountRepository _accountRepo;
        private readonly PatientRepository _patientRepo;
        private readonly DoctorRepository _doctorRepo;
        private readonly AdminRepository _adminRepo;
        private readonly ReceptionistRepository _receptionistRepo;
        private readonly AuditLogRepository _auditRepo;

        public ProfileController(
            AccountRepository accountRepo,
            PatientRepository patientRepo,
            DoctorRepository doctorRepo,
            AdminRepository adminRepo,
            ReceptionistRepository receptionistRepo,
            IConfiguration config)
        {
            _accountRepo = accountRepo;
            _patientRepo = patientRepo;
            _doctorRepo = doctorRepo;
            _adminRepo = adminRepo;
            _receptionistRepo = receptionistRepo;
            _auditRepo = new AuditLogRepository(config.GetConnectionString("DefaultConnection") ?? "");
        }

        [HttpGet]
        public IActionResult GetProfile()
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var account = _accountRepo.GetAccountById(accountId);
            if (account == null) return NotFound("Account not found");

            string rName = account.RoleName.ToLower();
            var response = new ProfileResponse
            {
                Id = account.AccountId,
                Email = account.Email,
                Role = rName
            };

            if (rName == "patient")
            {
                var p = _patientRepo.GetPatientByAccountId(accountId);
                if (p != null)
                {
                    response.FirstName = p.FirstName;
                    response.LastName = p.LastName;
                    response.Phone = p.Phone;
                    response.Gender = p.Gender;
                    response.DateOfBirth = p.DateOfBirth;
                }
            }
            else if (rName == "doctor")
            {
                var d = _doctorRepo.GetDoctorByAccountId(accountId);
                if (d != null)
                {
                    response.FirstName = d.FirstName;
                    response.LastName = d.LastName;
                    response.Phone = d.Phone;
                    response.Gender = d.Gender;
                    response.Bio = d.Bio;
                    response.SpecialtyId = d.SpecialtyId;
                    response.SpecialtyName = d.SpecialtyName;
                }
            }
            else if (rName == "receptionist")
            {
                var r = _receptionistRepo.GetReceptionistByAccountId(accountId);
                if (r != null)
                {
                    response.FirstName = r.FirstName;
                    response.LastName = r.LastName;
                    response.Phone = r.Phone;
                    response.Gender = r.Gender;
                }
            }
            else if (rName == "admin")
            {
                var a = _adminRepo.GetAdminByAccountId(accountId);
                if (a != null)
                {
                    response.FirstName = a.FirstName;
                    response.LastName = a.LastName;
                    response.Phone = a.Phone;
                    response.Gender = a.Gender;
                }
            }

            return Ok(response);
        }

        [HttpPut]
        public IActionResult UpdateProfile([FromBody] UpdateProfileRequest dto)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var account = _accountRepo.GetAccountById(accountId);
            if (account == null) return NotFound();

            string rName = account.RoleName.ToLower();
            bool success = false;

            if (rName == "patient")
                success = _patientRepo.UpdatePatient(accountId, dto);
            else if (rName == "doctor")
                success = _doctorRepo.UpdateDoctor(accountId, dto);
            else if (rName == "receptionist")
                success = _receptionistRepo.UpdateReceptionist(accountId, dto);
            else if (rName == "admin")
                success = _adminRepo.UpdateAdmin(accountId, dto);

            if (!success) return BadRequest("Failed to update profile");

            _auditRepo.LogAction(accountId, "UPDATE_PROFILE", "Account", accountId, "User updated their profile");

            return Ok(new { Message = "Profile updated successfully" });
        }

        [HttpPost("change-password")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest dto)
        {
            var accountId = int.Parse(User.FindFirst("id")?.Value ?? "0");
            if (accountId == 0) return Unauthorized();

            var account = _accountRepo.GetAccountById(accountId);
            if (account == null) return NotFound();

            if (!PasswordHasher.Verify(dto.Current, account.PasswordHash))
            {
                return BadRequest(new { Message = "Current password is incorrect" });
            }

            var newHash = PasswordHasher.Hash(dto.New);
            _accountRepo.UpdatePassword(accountId, newHash);

            _auditRepo.LogAction(accountId, "CHANGE_PASSWORD", "Account", accountId, "User changed their password");

            return Ok(new { Message = "Password changed successfully" });
        }
    }
}
