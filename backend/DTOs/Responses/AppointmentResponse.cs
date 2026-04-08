using System;
using Backend.Models;

namespace Backend.Dtos.Responses
{
    public class AppointmentResponse
    {
        public int Id { get; set; }
        public int TimeSlotId { get; set; }
        public string? Reason { get; set; }
        public string Status { get; set; } = "scheduled";
        public DateTime BookedAt { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CanceledAt { get; set; }

        public string? DoctorName { get; set; }
        public string? Specialty { get; set; }
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public string? PatientName { get; set; }
        public string? PatientPhone { get; set; }
        public int? PatientDbId { get; set; }
        public int? PatientAccountId { get; set; }
        public string? DoctorReminder { get; set; }
        public int? ParentAppointmentId { get; set; }
        public int? DoctorId { get; set; }
        public string? AppointmentType { get; set; }
        public int CreatedBy { get; set; }
        public bool IsConfirmed { get; set; }
        public string? Email { get; set; }
    }
}
