namespace Backend.Dtos.Responses
{
    public class PatientDashboardResponse
    {
        public string PatientName { get; set; } = string.Empty;
        public AppointmentResponse? NextAppointment { get; set; }
    }
}
