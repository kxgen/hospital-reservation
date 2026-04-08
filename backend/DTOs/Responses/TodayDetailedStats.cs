namespace Backend.Dtos.Responses
{
    public class TodayDetailedStats
    {
        public int Total { get; set; }
        public int Confirmed { get; set; }
        public int Unconfirmed { get; set; }
        public int CheckedIn { get; set; }
    }
}
