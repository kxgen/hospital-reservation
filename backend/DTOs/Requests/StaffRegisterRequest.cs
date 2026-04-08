namespace Backend.Dtos.Requests
{
    public class StaffRegisterRequest
    {
        public required string FirstName { get; set; }
        public required string LastName { get; set; }
        public required string Gender { get; set; }
        public required string Email { get; set; }
        public required string Phone { get; set; }
        public required string Password { get; set; }

        public int Role { get; set; } 
        public int SpecialtyId { get; set; } 
    }
}
