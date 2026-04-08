using System;

namespace Backend.Dtos.Responses
{
    public class FollowupDetailsResponse
    {
        public AppointmentResponse CurrentAppointment { get; set; } = new();
        public AppointmentResponse? ParentAppointment { get; set; }
        public PatientResponse Patient { get; set; } = new();
        public ReliabilityStats Reliability { get; set; } = new();
        public List<AppointmentResponse> AppointmentHistory { get; set; } = new();
    }

    public class ReliabilityStats
    {
        public int Attended { get; set; }
        public int Missed { get; set; }
        public string Rate { get; set; } = "0%";
    }
}
