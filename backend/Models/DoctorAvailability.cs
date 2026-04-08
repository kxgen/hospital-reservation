namespace Backend.Models
{
    public class DoctorAvailability
    {
        public int AvailabilityId { get; set; }
        public int DoctorId { get; set; }
        public string DayOfWeek { get; set; } = string.Empty; // monday, tuesday, etc.
        public TimeOnly StartTime { get; set; }
        public TimeOnly EndTime { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
