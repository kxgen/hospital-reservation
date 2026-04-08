namespace Backend.Dtos.Requests
{
    public class DoctorAvailabilityRequest
    {
        public required Dictionary<string, List<AvailabilityInterval>> Schedule { get; set; }
    }

    public class AvailabilityInterval
    {
        public required string Start { get; set; }
        public required string End { get; set; }
    }
}
