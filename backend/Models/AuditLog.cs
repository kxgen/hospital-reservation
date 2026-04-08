using System;

namespace Backend.Models
{
    public class AuditLog
    {
        public int Id { get; set; }
        public int? AccountId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string TargetTable { get; set; } = string.Empty;
        public int TargetId { get; set; }
        public string? Details { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
