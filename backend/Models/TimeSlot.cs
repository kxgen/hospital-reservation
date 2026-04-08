using System;

namespace Backend.Models
{
    public class TimeSlot
    {
        public int SlotId { get; set; }
        public int DoctorId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsAvailable { get; set; } = true;
    }
}
