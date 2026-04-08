namespace Backend.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public int? AccountId { get; set; } // Nullable for guest
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public DateTime? DateOfBirth { get; set; }
    }
}
