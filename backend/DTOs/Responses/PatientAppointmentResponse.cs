using System;
using Backend.Models;

namespace Backend.Dtos.Responses
{
    public class PatientAppointmentResponse
    {
        public int Id { get; set; }
        public AppointmentStatus Status { get; set; }
        public string? Reason { get; set; }
        public DateTime BookedAt { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string DoctorName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public int DoctorId { get; set; }
    }
}
