namespace Backend.Dtos.Requests
{
    public class BlockDoctorRequest
    {
        public int DoctorId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Reason { get; set; }
    }
}
