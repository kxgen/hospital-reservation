using System;

namespace Backend.Dtos.Responses
{
    public class PatientResponse
    {
        public int PatientId { get; set; }
        public int? AccountId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string FullName => $"{FirstName} {LastName}";
        public string? Phone { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public int? Age => DateOfBirth.HasValue ? DateTime.Today.Year - DateOfBirth.Value.Year - (DateTime.Today < DateOfBirth.Value.AddYears(DateTime.Today.Year - DateOfBirth.Value.Year) ? 1 : 0) : (int?)null;
        public bool IsDisabled { get; set; }
    }
}
