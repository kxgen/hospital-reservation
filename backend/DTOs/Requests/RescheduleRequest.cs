namespace Backend.Dtos.Requests
{
    public class RescheduleRequest
    {
        public int? NewTimeSlotId { get; set; } // Deprecated
        public DateTime NewStartTime { get; set; }
        public DateTime NewEndTime { get; set; }
    }
}
