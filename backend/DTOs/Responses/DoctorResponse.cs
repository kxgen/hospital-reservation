using System.Collections.Generic;

namespace Backend.Dtos.Responses
{
    public class DoctorResponse
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string SpecialtyName { get; set; } = string.Empty;
        public int SpecialtyId { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string Bio { get; set; } = string.Empty;
        public string PhotoUrl { get; set; } = string.Empty;
        public List<TimeSlotResponse> Timeslots { get; set; } = new List<TimeSlotResponse>();
        
        public string FullName => $"Dr. {FirstName} {LastName}";
    }
}
