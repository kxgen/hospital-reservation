namespace Backend.Dtos.Requests
{
    public class GenerateTimeSlotsRequest
    {
        public required string StartDate { get; set; } // "2026-01-10"
        public required string EndDate { get; set; }   // "2026-02-10"
        public int? SlotDurationMinutes { get; set; } = 30;
    }
}
