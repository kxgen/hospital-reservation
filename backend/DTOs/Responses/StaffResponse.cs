namespace Backend.Dtos.Responses
{
    public class StaffResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = string.Empty;
        public bool IsSuspended { get; set; }
        public string? Bio { get; set; }
        public int SpecialtyId { get; set; }
    }
}
