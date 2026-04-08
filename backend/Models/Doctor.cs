using Backend.Dtos.Responses;

namespace Backend.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        public int AccountId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Gender { get; set; }
        public int SpecialtyId { get; set; }
        public string? Bio { get; set; }
        public string? PhotoUrl { get; set; }
        public string SpecialtyName { get; set; } = string.Empty;
    }
}