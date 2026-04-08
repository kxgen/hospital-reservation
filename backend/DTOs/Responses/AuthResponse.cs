namespace Backend.Dtos.Responses
{
    public class AuthResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Role { get; set; } = null!; 
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public bool IsPasswordChangeRequired { get; set; }
    }
}
