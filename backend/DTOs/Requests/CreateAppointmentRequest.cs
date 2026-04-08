namespace Backend.Dtos.Requests
{
    public class CreateAppointmentRequest
    {
        public int? TimeSlotId { get; set; } // Deprecated
        public int DoctorId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Reason { get; set; }
        public int? ParentAppointmentId { get; set; }
        public int? PatientId { get; set; } // For staff-side bookings
    }
}
