namespace Backend.Dtos.Requests
{
    public class FinalizeAppointmentRequest
    {
        public string DoctorReminder { get; set; } = string.Empty;
        public bool IsResolved { get; set; }
    }
}
