using System;

namespace Backend.Dtos.Responses
{
    public class AuditLogResponse
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string ActorName { get; set; } = "System";
        public string Target { get; set; } = string.Empty;
        public string? Details { get; set; }
        public string? IpAddress { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
