namespace Backend.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int? TimeSlotId { get; set; } // Deprecated, kept for backward compat if needed
        public int DoctorId { get; set; } // New direct link
        public DateTime StartTime { get; set; } // New direct time
        public DateTime EndTime { get; set; } // New direct time
        public int CreatedBy { get; set; } // References AccountId
        public string Status { get; set; } = "scheduled";
        public string? Reason { get; set; }
        public int? ParentAppointmentId { get; set; } // for follow-up chain
        public string? DoctorReminder { get; set; }
        public DateTime BookedAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CanceledAt { get; set; }
        public bool IsConfirmed { get; set; } = false;
    }
}