namespace Backend.Dtos.Responses
{
    public class SystemStatsResponse
    {
        public int Doctors { get; set; }
        public int Receptionists { get; set; }
        public int Patients { get; set; }
        public int AppointmentsToday { get; set; }
    }
}
