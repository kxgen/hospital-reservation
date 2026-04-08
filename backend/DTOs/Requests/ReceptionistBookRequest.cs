namespace Backend.Dtos.Requests
{
    public class ReceptionistBookRequest
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string? DateOfBirth { get; set; }
        public int? TimeSlotId { get; set; } // Deprecated
        public int DoctorId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Reason { get; set; } = string.Empty;
        public int? PatientId { get; set; }
    }
}
