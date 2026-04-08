namespace Backend.Models
{
    public class Admin
    {
        public int AdminId { get; set; }
        public int AccountId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
    }
}
