namespace Backend.Dtos.Requests
{
    public class UpdateProfileRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        
        // Role specific fields
        public string? DateOfBirth { get; set; } // For Patient
        public string? Bio { get; set; }        // For Doctor
        public int? SpecialtyId { get; set; }   // For Doctor
    }
}
