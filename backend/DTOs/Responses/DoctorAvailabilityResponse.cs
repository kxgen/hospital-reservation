namespace Backend.Dtos.Responses
{
    public class DoctorAvailabilityResponse
    {
        public Dictionary<string, List<AvailabilityInterval>> Schedule { get; set; } = new();
    }

    public class AvailabilityInterval
    {
        public string Start { get; set; } = string.Empty;
        public string End { get; set; } = string.Empty;
    }
}
