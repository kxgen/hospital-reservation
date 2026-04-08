using Backend.Models;

namespace Backend.Dtos.Requests
{
    public class UpdateAppointmentRequest
    {
        public AppointmentStatus? Status { get; set; }
        public string? Reason { get; set; }
        public DateTime? CheckedInAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime? CanceledAt { get; set; }
    }
}
