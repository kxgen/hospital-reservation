using Npgsql;
using Backend.Models;
using Backend.Dtos.Responses;

namespace Backend.Data
{
    public class DoctorAvailabilityRepository
    {
        private readonly string _connectionString;

        public DoctorAvailabilityRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Dictionary<string, List<Dtos.Responses.AvailabilityInterval>> GetDoctorAvailability(int doctorId)
        {
            var schedule = new Dictionary<string, List<Dtos.Responses.AvailabilityInterval>>();
            var days = new[] { "monday", "tuesday", "wednesday", "thursday", "friday", "saturday", "sunday" };
            
            foreach (var day in days)
            {
                schedule[day] = new List<Dtos.Responses.AvailabilityInterval>();
            }

            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT day_of_week, start_time, end_time 
                FROM doctor_availability 
                WHERE doctor_id = @doctorId AND is_active = true
                ORDER BY day_of_week, start_time", conn);
            cmd.Parameters.AddWithValue("doctorId", doctorId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var day = reader.GetString(0);
                var start = reader.GetTimeSpan(1);
                var end = reader.GetTimeSpan(2);

                schedule[day].Add(new Dtos.Responses.AvailabilityInterval
                {
                    Start = $"{start.Hours:D2}:{start.Minutes:D2}",
                    End = $"{end.Hours:D2}:{end.Minutes:D2}"
                });
            }

            return schedule;
        }

        public void SaveDoctorAvailability(int doctorId, Dictionary<string, List<Dtos.Requests.AvailabilityInterval>> schedule)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        // Delete existing availability for this doctor
                        using (var deleteCmd = new NpgsqlCommand("DELETE FROM doctor_availability WHERE doctor_id = @doctorId", conn, transaction))
                        {
                            deleteCmd.Parameters.AddWithValue("doctorId", doctorId);
                            deleteCmd.ExecuteNonQuery();
                        }

                        // Insert new availability rules
                        foreach (var daySchedule in schedule)
                        {
                            var day = daySchedule.Key.ToLower();
                            
                            // Defensive parsing of slots
                            var intervals = new List<(TimeSpan Start, TimeSpan End)>();
                            foreach (var s in daySchedule.Value)
                            {
                                if (!TimeSpan.TryParse(s.Start, out var start) || !TimeSpan.TryParse(s.End, out var end))
                                {
                                    throw new Exception($"Invalid time format on {day}: '{s.Start}' or '{s.End}'. Expected HH:mm.");
                                }
                                intervals.Add((start, end));
                            }

                            // Sort slots by start time to check for overlaps
                            var sortedSlots = intervals
                                .OrderBy(s => s.Start)
                                .ToList();

                            TimeSpan? lastEnd = null;

                            foreach (var slot in sortedSlots)
                            {
                                if (slot.End <= slot.Start)
                                {
                                    throw new Exception($"Invalid time range on {day}: {slot.Start} to {slot.End}. End time must be after start time.");
                                }

                                if (lastEnd.HasValue && slot.Start < lastEnd.Value)
                                {
                                    throw new Exception($"Overlapping time slots detected on {day}: {slot.Start} starts before previous slot ends at {lastEnd.Value}.");
                                }

                                using (var insertCmd = new NpgsqlCommand(@"
                                    INSERT INTO doctor_availability (doctor_id, day_of_week, start_time, end_time, is_active)
                                    VALUES (@doctorId, @day, @start, @end, true)", conn, transaction))
                                {
                                    insertCmd.Parameters.AddWithValue("doctorId", doctorId);
                                    insertCmd.Parameters.AddWithValue("day", day);
                                    insertCmd.Parameters.AddWithValue("start", slot.Start);
                                    insertCmd.Parameters.AddWithValue("end", slot.End);
                                    insertCmd.ExecuteNonQuery();
                                }

                                lastEnd = slot.End;
                            }
                        }

                        transaction.Commit();
                    }
                    catch (Exception)
                    {
                        try { transaction.Rollback(); } catch { /* Ignore rollback failures if connection already lost */ }
                        throw;
                    }
                }
            }
        }
    }
}
