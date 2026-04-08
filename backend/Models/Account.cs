namespace Backend.Models
{
    public class Account
    {
        public int AccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public int RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public bool IsPasswordChangeRequired { get; set; } = false;
        public string? PasswordResetOtpHash { get; set; }
        public DateTime? PasswordResetOtpExpiry { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
