using System;

namespace Backend.Dtos.Responses
{
    public class ProfileResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public string Role { get; set; } = string.Empty;

        // Role specific fields
        public DateTime? DateOfBirth { get; set; } // For Patient
        public string? Bio { get; set; }           // For Doctor
        public string? SpecialtyName { get; set; } // For Doctor
        public int? SpecialtyId { get; set; }      // For Doctor
    }
}
