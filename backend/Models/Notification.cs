using System;

namespace Backend.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int PatientId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "General"; // Appointment, System, General
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? CreatedBy { get; set; } // AccountId of the staff who sent it
        public int? AppointmentId { get; set; }
        public string? SenderName { get; set; }
        public bool IsConfirmed { get; set; }
        public string? AppointmentStatus { get; set; }
        public DateTime? AppointmentStartTime { get; set; }
    }
}
