namespace Backend.Dtos.Responses
{
    public class ReceptionDashboardStatsResponse
    {
        public int Doctors { get; set; }
        public int Receptionists { get; set; }
        public int Patients { get; set; }
        public TodayDetailedStats? Today { get; set; }
    }
}
