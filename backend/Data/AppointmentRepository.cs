using Npgsql;
using Backend.Models;
using Backend.Dtos.Requests;
using Backend.Dtos.Responses;

namespace Backend.Data
{
    public class AppointmentRepository
    {
        private readonly string _connectionString = string.Empty;

        public AppointmentRepository(string connectionString)
        {
            _connectionString = connectionString;
            EnsureColumnExists();
            EnsureUnavailabilityTableExists();
        }

        protected AppointmentRepository() { }

        private void EnsureColumnExists()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                DO $$ 
                BEGIN 
                    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='appointment' AND column_name='is_confirmed') THEN
                        ALTER TABLE appointment ADD COLUMN is_confirmed BOOLEAN DEFAULT FALSE;
                    END IF;
                    
                    -- Performance Indexes
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'idx_appointment_patient_id') THEN
                        CREATE INDEX idx_appointment_patient_id ON appointment(patient_id);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'idx_appointment_doctor_id') THEN
                        CREATE INDEX idx_appointment_doctor_id ON appointment(doctor_id);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'idx_appointment_times') THEN
                        CREATE INDEX idx_appointment_times ON appointment(start_time, end_time);
                    END IF;
                END $$;", conn);
            cmd.ExecuteNonQuery();
        }

        private void EnsureUnavailabilityTableExists()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                DO $$ 
                BEGIN 
                    CREATE TABLE IF NOT EXISTS public.doctor_unavailability (
                        unavailability_id INTEGER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        doctor_id INTEGER NOT NULL REFERENCES doctor(doctor_id) ON DELETE CASCADE,
                        start_time TIMESTAMP WITHOUT TIME ZONE NOT NULL,
                        end_time TIMESTAMP WITHOUT TIME ZONE NOT NULL,
                        reason VARCHAR(255),
                        created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP
                    );
                    
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'idx_doctor_unavailability_doctor') THEN
                        CREATE INDEX idx_doctor_unavailability_doctor ON doctor_unavailability(doctor_id);
                    END IF;
                    IF NOT EXISTS (SELECT 1 FROM pg_indexes WHERE indexname = 'idx_doctor_unavailability_times') THEN
                        CREATE INDEX idx_doctor_unavailability_times ON doctor_unavailability(start_time, end_time);
                    END IF;
                END $$;", conn);
            cmd.ExecuteNonQuery();
        }

        public List<AppointmentResponse> GetAllUpcomingAppointments()
        {
            var appointments = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id,
                    a.time_slot_id,
                    a.status,
                    a.reason,
                    a.booked_at,
                    a.start_time,
                    a.end_time,
                    d.first_name || ' ' || d.last_name as doctor_name,
                    p.first_name || ' ' || p.last_name as patient_name,
                    s.specialty_name as specialty,
                    a.parent_appointment_id,
                    a.created_by,
                    a.checked_in_at
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                JOIN patient p ON a.patient_id = p.patient_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.status <> 'cancelled'
                AND a.start_time >= CURRENT_DATE
                ORDER BY a.start_time ASC;", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var parentId = reader.IsDBNull(10) ? (int?)null : reader.GetInt32(10);
                appointments.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1), // Handle nullable
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5),
                    EndTime = reader.GetDateTime(6),
                    DoctorName = reader.GetString(7),
                    PatientName = reader.GetString(8),
                    Specialty = reader.IsDBNull(9) ? "General" : reader.GetString(9),
                    ParentAppointmentId = parentId,
                    CreatedBy = reader.GetInt32(11),
                    CheckedInAt = reader.IsDBNull(12) ? (DateTime?)null : reader.GetDateTime(12),
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New"
                });
            }
            return appointments;
        }

        public List<AppointmentResponse> GetAppointmentsByCreator(int creatorId)
        {
            var appointments = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id,
                    a.status,
                    a.reason,
                    a.start_time,
                    a.end_time,
                    d.first_name || ' ' || d.last_name as doctor_name,
                    p.first_name || ' ' || p.last_name as patient_name,
                    s.specialty_name,
                    a.doctor_id
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                JOIN patient p ON a.patient_id = p.patient_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.created_by = @cid
                ORDER BY a.start_time DESC;", conn);
            
            cmd.Parameters.AddWithValue("cid", creatorId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                appointments.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    Status = reader.GetString(1),
                    Reason = reader.IsDBNull(2) ? null : reader.GetString(2),
                    StartTime = reader.GetDateTime(3),
                    EndTime = reader.GetDateTime(4),
                    DoctorName = reader.GetString(5),
                    PatientName = reader.GetString(6),
                    Specialty = reader.IsDBNull(7) ? "General" : reader.GetString(7),
                    DoctorId = reader.GetInt32(8)
                });
            }
            return appointments;
        }

        public Appointment CreateAppointment(Appointment appointment)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // Check if user is trying to book an unavailable slot (double booking check)
                        using (var checkCmd = new NpgsqlCommand(@"
                            SELECT 1 
                            FROM appointment 
                            WHERE doctor_id = @did 
                              AND status NOT IN ('cancelled', 'completed', 'no_show')
                              AND (
                                   (start_time < @end AND end_time > @start) -- Overlap logic
                              )
                            FOR UPDATE
                            LIMIT 1", conn, trans))
                        {
                            checkCmd.Parameters.AddWithValue("did", appointment.DoctorId);
                            checkCmd.Parameters.AddWithValue("start", appointment.StartTime);
                            checkCmd.Parameters.AddWithValue("end", appointment.EndTime);

                            var conflict = checkCmd.ExecuteScalar();
                            if (conflict != null) throw new Exception("This time slot is no longer available.");
                        }

                        // Insert Appointment
                        using (var insCmd = new NpgsqlCommand(@"
                            INSERT INTO appointment (patient_id, doctor_id, start_time, end_time, created_by, status, reason, parent_appointment_id, doctor_reminder, booked_at, is_confirmed)
                            VALUES (@pId, @dId, @start, @end, @cBy, @status, @reason, @pAppId, @dRem, @bookedAt, @isConf)
                            RETURNING appointment_id;", conn, trans))
                        {
                            var now = DateTime.UtcNow;
                            insCmd.Parameters.AddWithValue("pId", appointment.PatientId);
                            insCmd.Parameters.AddWithValue("dId", appointment.DoctorId);
                            insCmd.Parameters.AddWithValue("start", appointment.StartTime);
                            insCmd.Parameters.AddWithValue("end", appointment.EndTime);
                            insCmd.Parameters.AddWithValue("cBy", appointment.CreatedBy);
                            insCmd.Parameters.AddWithValue("status", appointment.Status.ToLower());
                            insCmd.Parameters.AddWithValue("reason", (object?)appointment.Reason ?? DBNull.Value);
                            insCmd.Parameters.AddWithValue("pAppId", (object?)appointment.ParentAppointmentId ?? DBNull.Value);
                            insCmd.Parameters.AddWithValue("dRem", (object?)appointment.DoctorReminder ?? DBNull.Value);
                            insCmd.Parameters.AddWithValue("bookedAt", now);
                            insCmd.Parameters.AddWithValue("isConf", appointment.IsConfirmed);

                            var result = insCmd.ExecuteScalar();
                            appointment.AppointmentId = result != null ? (int)result : 0;
                            appointment.BookedAt = now;
                        }

                        trans.Commit();
                        return appointment;
                    }
                    catch
                    {
                        try { trans.Rollback(); } catch { }
                        throw;
                    }
                }
            }
        }

        public AppointmentResponse? GetAppointmentById(int appointmentId, int accountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id,
                    a.time_slot_id,
                    a.status,
                    a.reason,
                    a.booked_at,
                    a.start_time,
                    a.end_time,
                    d.first_name || ' ' || d.last_name as doctor_full_name,
                    s.specialty_name,
                    a.parent_appointment_id
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.appointment_id = @aid
                AND (
                    a.patient_id = (SELECT patient_id FROM patient WHERE account_id = @uid)
                    OR a.created_by = @uid
                );", conn);
            
            cmd.Parameters.AddWithValue("aid", appointmentId);
            cmd.Parameters.AddWithValue("uid", accountId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var parentId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9);
                return new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5), 
                    EndTime = reader.GetDateTime(6),   
                    DoctorName = reader.GetString(7),
                    Specialty = reader.IsDBNull(8) ? "General" : reader.GetString(8),
                    ParentAppointmentId = parentId,
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New"
                };
            }

            return null;
        }

        public bool CheckInAppointment(int appointmentId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var trans = conn.BeginTransaction();

            using var cmd = new NpgsqlCommand(@"
                UPDATE appointment
                SET checked_in_at = CURRENT_TIMESTAMP,
                    status = 'confirmed',
                    is_confirmed = TRUE
                WHERE appointment_id = @aid
                AND start_time::date = CURRENT_DATE
                AND checked_in_at IS NULL
                RETURNING appointment_id;", conn, trans);

            cmd.Parameters.AddWithValue("@aid", appointmentId);
            var appointmentIdResult = cmd.ExecuteScalar();
            trans.Commit();

            return appointmentIdResult != null;
        }

        // Returns list of appointments mapped to DTO for a patient (by accountId)
        public List<AppointmentResponse> GetAppointmentsByPatientId(int patientId)
        {
            var appointments = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id,
                    a.time_slot_id,
                    a.status,
                    a.reason,
                    a.booked_at,
                    a.start_time,
                    a.end_time,
                    d.first_name || ' ' || d.last_name as doctor_full_name,
                    s.specialty_name,
                    a.parent_appointment_id
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.patient_id = @pid
                ORDER BY a.start_time DESC;", conn);
            
            cmd.Parameters.AddWithValue("pid", patientId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var parentId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9);
                appointments.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5), 
                    EndTime = reader.GetDateTime(6),   
                    DoctorName = reader.GetString(7),
                    Specialty = reader.IsDBNull(8) ? "Unknown" : reader.GetString(8),
                    ParentAppointmentId = parentId,
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New"
                });
            }

            return appointments;
        }

        public List<AppointmentResponse> GetUpcomingAppointmentsByPatientId(int patientId)
        {
            var appointments = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id, a.time_slot_id, a.status, a.reason, a.booked_at,
                    a.start_time, a.end_time,
                    d.first_name || ' ' || d.last_name as doctor_full_name,
                    s.specialty_name, a.parent_appointment_id
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.patient_id = @pid AND a.status IN ('scheduled', 'confirmed')
                AND a.start_time >= CURRENT_DATE
                ORDER BY a.start_time ASC;", conn);
            
            cmd.Parameters.AddWithValue("pid", patientId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var parentId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9);
                appointments.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5), 
                    EndTime = reader.GetDateTime(6),   
                    DoctorName = reader.GetString(7),
                    Specialty = reader.IsDBNull(8) ? "Unknown" : reader.GetString(8),
                    ParentAppointmentId = parentId,
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New"
                });
            }
            return appointments;
        }

        public List<AppointmentResponse> GetPendingAppointmentsByPatientId(int patientId)
        {
            var appointments = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id, a.time_slot_id, a.status, a.reason, a.booked_at,
                    a.start_time, a.end_time,
                    d.first_name || ' ' || d.last_name as doctor_full_name,
                    s.specialty_name, a.parent_appointment_id
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.patient_id = @pid AND a.status = 'pending'
                ORDER BY a.start_time DESC;", conn);
            
            cmd.Parameters.AddWithValue("pid", patientId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var parentId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9);
                appointments.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5), 
                    EndTime = reader.GetDateTime(6),   
                    DoctorName = reader.GetString(7),
                    Specialty = reader.IsDBNull(8) ? "Unknown" : reader.GetString(8),
                    ParentAppointmentId = parentId,
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New"
                });
            }
            return appointments;
        }

        public List<AppointmentResponse> GetHistoryAppointmentsByPatientId(int patientId)
        {
            var appointments = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id, a.time_slot_id, a.status, a.reason, a.booked_at,
                    a.start_time, a.end_time,
                    d.first_name || ' ' || d.last_name as doctor_full_name,
                    s.specialty_name, a.parent_appointment_id
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.patient_id = @pid AND a.status IN ('completed', 'cancelled')
                ORDER BY a.start_time DESC;", conn);
            
            cmd.Parameters.AddWithValue("pid", patientId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var parentId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9);
                appointments.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5), 
                    EndTime = reader.GetDateTime(6),   
                    DoctorName = reader.GetString(7),
                    Specialty = reader.IsDBNull(8) ? "Unknown" : reader.GetString(8),
                    ParentAppointmentId = parentId,
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New"
                });
            }
            return appointments;
        }

        // Returns list of appointments for a doctor (by accountId)
        public List<AppointmentResponse> GetDoctorAppointments(int accountId)
        {
            var appointments = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id,
                    a.time_slot_id,
                    a.status,
                    a.reason,
                    a.booked_at,
                    a.start_time,
                    a.end_time,
                    p.first_name || ' ' || p.last_name as patient_name,
                    p.patient_id,
                    a.parent_appointment_id,
                    a.checked_in_at,
                    a.is_confirmed
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                JOIN patient p ON a.patient_id = p.patient_id
                WHERE d.account_id = @uid
                ORDER BY a.start_time ASC;", conn);
            
            cmd.Parameters.AddWithValue("uid", accountId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var parentId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9);
                appointments.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5), 
                    EndTime = reader.GetDateTime(6),   
                    PatientName = reader.GetString(7), 
                    PatientDbId = reader.GetInt32(8),
                    DoctorName = "Self", 
                    Specialty = "General",
                    ParentAppointmentId = parentId,
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New",
                    CheckedInAt = reader.IsDBNull(10) ? (DateTime?)null : reader.GetDateTime(10),
                    IsConfirmed = reader.IsDBNull(11) ? false : reader.GetBoolean(11)
                });
            }

            return appointments;
        }

        public virtual int GetTodayAppointmentsCount()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT COUNT(*)
                FROM appointment
                WHERE start_time::date = CURRENT_DATE
                AND status NOT IN ('cancelled')", conn);
                
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public TodayDetailedStats GetTodayDetailedStats()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    COUNT(*) as Total,
                    COUNT(*) FILTER (WHERE is_confirmed = TRUE OR status = 'confirmed') as Confirmed,
                    COUNT(*) FILTER (WHERE is_confirmed = FALSE AND status = 'scheduled') as Unconfirmed,
                    COUNT(*) FILTER (WHERE checked_in_at IS NOT NULL) as CheckedIn
                FROM appointment
                WHERE start_time::date = CURRENT_DATE
                AND status NOT IN ('cancelled')", conn);
                
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new TodayDetailedStats
                {
                    Total = reader.GetInt32(0),
                    Confirmed = reader.GetInt32(1),
                    Unconfirmed = reader.GetInt32(2),
                    CheckedIn = reader.GetInt32(3)
                };
            }
            return new TodayDetailedStats();
        }

        public bool CancelAppointment(int appointmentId, int accountId, bool isStaff = false)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            try
            {
                var query = @"
                    UPDATE appointment
                    SET status = 'cancelled'
                    WHERE appointment_id = @aid
                    AND status <> 'cancelled'";

                if (!isStaff)
                {
                    query += @" AND (
                        created_by = @uid
                        OR patient_id = (SELECT patient_id FROM patient WHERE account_id = @uid LIMIT 1)
                    )";
                }

                query += " RETURNING appointment_id;";

                var cmd = new NpgsqlCommand(query, conn);
                cmd.Parameters.AddWithValue("aid", appointmentId);
                cmd.Parameters.AddWithValue("uid", accountId);

                var dbResult = cmd.ExecuteScalar();
                return dbResult != null && dbResult != DBNull.Value;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Cancel Error: {ex.Message}");
                return false;
            }
        }


        public List<TimeSlotResponse>? GetAvailableSlotsForReschedule(int appointmentId, int accountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // 1. Get doctor id of the appointment
            var cmd = new NpgsqlCommand(@"
                SELECT doctor_id
                FROM appointment a
                LEFT JOIN patient p ON a.patient_id = p.patient_id
                WHERE a.appointment_id = @aid
                AND (p.account_id = @uid OR a.created_by = @uid)
                AND a.status NOT IN ('cancelled', 'completed')
            ", conn);

            cmd.Parameters.AddWithValue("aid", appointmentId);
            cmd.Parameters.AddWithValue("uid", accountId);

            int doctorId = 0;
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read()) return null; 
                doctorId = reader.GetInt32(0);
            }

            // 2. Delegate to generic slot generator
            return GetAvailableSlotsByDoctorId(doctorId);
        }

        public string? RescheduleAppointment(int appointmentId, DateTime newStart, DateTime newEnd, int accountId, bool isStaff = false)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Get current appointment info and check permissions in one go
                        string query = @"
                            SELECT doctor_id
                            FROM appointment
                            WHERE appointment_id = @aid
                            AND status NOT IN ('cancelled', 'completed')";
                        
                        if (!isStaff)
                        {
                            query += @" AND (
                                created_by = @uid
                                OR patient_id = (SELECT patient_id FROM patient WHERE account_id = @uid LIMIT 1)
                            )";
                        }
                        
                        query += " FOR UPDATE";

                        using (var infoCmd = new NpgsqlCommand(query, conn, trans))
                        {
                            infoCmd.Parameters.AddWithValue("aid", appointmentId);
                            infoCmd.Parameters.AddWithValue("uid", accountId);
                            
                            var docIdObj = infoCmd.ExecuteScalar();
                            if (docIdObj == null)
                            {
                                trans.Rollback();
                                return "Appointment not found, already completed/cancelled, or permission denied.";
                            }
                            int doctorId = Convert.ToInt32(docIdObj);

                            // 2. Conflict check (Efficient overlap logic)
                            using (var checkConflictCmd = new NpgsqlCommand(@"
                                SELECT 1 
                                FROM appointment 
                                WHERE doctor_id = @did 
                                  AND status NOT IN ('cancelled', 'completed', 'no_show')
                                  AND appointment_id != @aid
                                  AND start_time < @end AND end_time > @start
                                LIMIT 1", conn, trans))
                            {
                                checkConflictCmd.Parameters.AddWithValue("did", doctorId);
                                checkConflictCmd.Parameters.AddWithValue("aid", appointmentId);
                                checkConflictCmd.Parameters.AddWithValue("start", newStart);
                                checkConflictCmd.Parameters.AddWithValue("end", newEnd);

                                if (checkConflictCmd.ExecuteScalar() != null)
                                {
                                    trans.Rollback();
                                    return "The selected time overlaps with another appointment.";
                                }
                            }

                            // 3. Perform update
                            using (var updateCmd = new NpgsqlCommand(@"
                                UPDATE appointment 
                                SET start_time = @start, 
                                    end_time = @end, 
                                    status = 'scheduled',
                                    is_confirmed = FALSE -- Reset confirmation if slot changed
                                WHERE appointment_id = @aid", conn, trans))
                            {
                                updateCmd.Parameters.AddWithValue("start", newStart);
                                updateCmd.Parameters.AddWithValue("end", newEnd);
                                updateCmd.Parameters.AddWithValue("aid", appointmentId);
                                
                                updateCmd.ExecuteNonQuery();
                            }
                        }

                        trans.Commit();
                        return null; 
                    }
                    catch (Exception ex)
                    {
                        try { trans.Rollback(); } catch { }
                        return $"Reschedule failed: {ex.Message}";
                    }
                }
            }
        }
        public bool UpdateAppointmentReason(int appointmentId, string reason, int accountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                UPDATE appointment
                SET reason = @reason
                WHERE appointment_id = @aid
                AND created_by = @uid
                AND status NOT IN ('cancelled', 'completed')
            ", conn);

            cmd.Parameters.AddWithValue("reason", reason);
            cmd.Parameters.AddWithValue("aid", appointmentId);
            cmd.Parameters.AddWithValue("uid", accountId);

            return cmd.ExecuteNonQuery() > 0;
        }

        public List<TimeSlotResponse> GetAvailableSlotsByDoctorId(int doctorId)
        {
            // 1. Get Availability Rules
            var availRepo = new DoctorAvailabilityRepository(_connectionString);
            var availability = availRepo.GetDoctorAvailability(doctorId); // Returns Dict<day, List<Interval>>
            
            // 2. Get Existing Appointments (Conflicts)
            var appointments = new List<(DateTime Start, DateTime End)>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"
                    SELECT start_time, end_time 
                    FROM appointment 
                    WHERE doctor_id = @did 
                    AND status NOT IN ('cancelled', 'completed', 'no_show') 
                    AND start_time >= @today", conn);
                cmd.Parameters.AddWithValue("did", doctorId);
                cmd.Parameters.AddWithValue("today", DateTime.Today);
                using (var reader = cmd.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        // Convert to local time to match the generated slots' kind
                        appointments.Add((reader.GetDateTime(0).ToLocalTime(), reader.GetDateTime(1).ToLocalTime()));
                    }
                } // Reader disposed here

                // 2.5 Get Unavailability Blocks
                using var unavailCmd = new NpgsqlCommand(@"
                    SELECT start_time, end_time 
                    FROM doctor_unavailability 
                    WHERE doctor_id = @did 
                    AND end_time >= @today", conn);
                unavailCmd.Parameters.AddWithValue("did", doctorId);
                unavailCmd.Parameters.AddWithValue("today", DateTime.Today);
                using var unavailReader = unavailCmd.ExecuteReader();
                while(unavailReader.Read())
                {
                    // Treat unavailability as a "booking" to block slots
                    appointments.Add((unavailReader.GetDateTime(0).ToLocalTime(), unavailReader.GetDateTime(1).ToLocalTime()));
                }
            }

            // 3. Generate Slots
            var slots = new List<TimeSlotResponse>();
            var now = DateTime.Now; 
            var startDate = DateTime.Today;
            var endDate = startDate.AddMonths(4);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayName = date.DayOfWeek.ToString().ToLower();
                if (!availability.ContainsKey(dayName)) continue;

                foreach (var interval in availability[dayName])
                {
                    if (!TimeSpan.TryParse(interval.Start, out var rStart) || !TimeSpan.TryParse(interval.End, out var rEnd))
                        continue;

                    var current = date.Add(rStart);
                    var endLimit = date.Add(rEnd);

                    while (current.AddMinutes(30) <= endLimit)
                    {
                        var slotStart = current;
                        var slotEnd = current.AddMinutes(30);
                        
                        // Check if in the past
                        if (slotStart < now) 
                        {
                            current = current.AddMinutes(30);
                            continue;
                        }

                        // Check conflict
                        bool isBooked = appointments.Any(a => 
                            a.Start < slotEnd && a.End > slotStart // Overlap check
                        );

                        if (!isBooked)
                        {
                            slots.Add(new TimeSlotResponse
                            {
                                Id = 0, // No DB ID anymore
                                StartTime = slotStart,
                                EndTime = slotEnd,
                                IsAvailable = true
                            });
                        }

                        current = current.AddMinutes(30);
                    }
                }
            }
            
            return slots.OrderBy(s => s.StartTime).ToList();
        }

        public List<TimeSlotResponse> GetAllSlotsByDoctorId(int doctorId)
        {
            // 1. Get Availability Rules
            var availRepo = new DoctorAvailabilityRepository(_connectionString);
            var availability = availRepo.GetDoctorAvailability(doctorId);
            
            // 2. Get Existing Appointments (Conflicts)
            var appointments = new List<(DateTime Start, DateTime End)>();
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using var cmd = new NpgsqlCommand(@"
                    SELECT start_time, end_time 
                    FROM appointment 
                    WHERE doctor_id = @did 
                    AND status NOT IN ('cancelled', 'completed', 'no_show') 
                    AND start_time >= @today", conn);
                cmd.Parameters.AddWithValue("did", doctorId);
                cmd.Parameters.AddWithValue("today", DateTime.Today);
                using (var reader = cmd.ExecuteReader())
                {
                    while(reader.Read())
                    {
                        appointments.Add((reader.GetDateTime(0).ToLocalTime(), reader.GetDateTime(1).ToLocalTime()));
                    }
                }

            }

            // 3. Generate Slots
            var slots = new List<TimeSlotResponse>();
            var now = DateTime.Now; 
            var startDate = DateTime.Today;
            var endDate = startDate.AddMonths(4);

            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                var dayName = date.DayOfWeek.ToString().ToLower();
                if (!availability.ContainsKey(dayName)) continue;

                foreach (var interval in availability[dayName])
                {
                    if (!TimeSpan.TryParse(interval.Start, out var rStart) || !TimeSpan.TryParse(interval.End, out var rEnd))
                        continue;

                    var current = date.Add(rStart);
                    var endLimit = date.Add(rEnd);

                    while (current.AddMinutes(30) <= endLimit)
                    {
                        var slotStart = current;
                        var slotEnd = current.AddMinutes(30);
                        
                        // Check if in the past
                        if (slotStart < now) 
                        {
                            current = current.AddMinutes(30);
                            continue;
                        }

                        // Check conflict
                        bool isBooked = appointments.Any(a => 
                            a.Start < slotEnd && a.End > slotStart
                        );

                        slots.Add(new TimeSlotResponse
                        {
                            Id = 0,
                            StartTime = slotStart,
                            EndTime = slotEnd,
                            IsAvailable = !isBooked
                        });
                        
                        current = current.AddMinutes(30);
                    }
                }
            }
            return slots.OrderBy(s => s.StartTime).ToList();
        }
        public Dictionary<int, List<TimeSlotResponse>> GetAllAvailableSlots()
        {
            var dict = new Dictionary<int, List<TimeSlotResponse>>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            var cmd = new NpgsqlCommand(@"
                SELECT slot_id, doctor_id, start_time, end_time
                FROM time_slot
                WHERE is_available = TRUE
                AND start_time > NOW()
                ORDER BY start_time", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                int doctorId = reader.GetInt32(1);
                if (!dict.ContainsKey(doctorId)) dict[doctorId] = new List<TimeSlotResponse>();
                
                dict[doctorId].Add(new TimeSlotResponse
                {
                    Id = reader.GetInt32(0),
                    StartTime = reader.GetDateTime(2),
                    EndTime = reader.GetDateTime(3),
                    IsAvailable = true
                });
            }
            return dict;
        }
        public FollowupDetailsResponse? GetFollowupDetails(int appointmentId, int doctorAccountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // 1. Get Doctor ID from Account ID
            var docIdCmd = new NpgsqlCommand("SELECT doctor_id FROM doctor WHERE account_id = @aid", conn);
            docIdCmd.Parameters.AddWithValue("aid", doctorAccountId);
            var doctorIdObj = docIdCmd.ExecuteScalar();
            if (doctorIdObj == null) return null;
            int doctorId = Convert.ToInt32(doctorIdObj);

            // 2. Get Current Appointment
            var currentApt = GetAppointmentDetailsInternal(appointmentId, conn, doctorId);
            if (currentApt == null) return null;

            // 3. Get Parent Appointment Context if exists
            AppointmentResponse? parentApt = null;
            if (currentApt.ParentAppointmentId.HasValue)
            {
                parentApt = GetAppointmentDetailsInternal(currentApt.ParentAppointmentId.Value, conn);
            }

            // 4. Get Patient Details for the banner
            var patientCmd = new NpgsqlCommand("SELECT patient_id, first_name, last_name, phone, gender, date_of_birth FROM patient WHERE patient_id = @pid", conn);
            patientCmd.Parameters.AddWithValue("pid", currentApt.PatientDbId ?? (object)DBNull.Value);
            PatientResponse? patient = null;
            using (var reader = patientCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    patient = new PatientResponse {
                        PatientId = reader.GetInt32(0),
                        FirstName = reader.GetString(1),
                        LastName = reader.GetString(2),
                        Phone = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Gender = reader.IsDBNull(4) ? null : reader.GetString(4),
                        DateOfBirth = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5)
                    };
                }
            }
            if (patient == null) return null;

            // 5. Reliability Stats (Missed vs Attended for THIS Doctor/Patient only)
            var statsCmd = new NpgsqlCommand(@"
                SELECT 
                    COUNT(*) FILTER (WHERE status = 'completed') as attended,
                    COUNT(*) FILTER (WHERE status = 'no_show') as missed
                FROM appointment 
                WHERE doctor_id = @did AND patient_id = @pid", conn);
            statsCmd.Parameters.AddWithValue("did", doctorId);
            statsCmd.Parameters.AddWithValue("pid", currentApt.PatientDbId ?? (object)DBNull.Value);

            var stats = new ReliabilityStats();
            using (var reader = statsCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    stats.Attended = (int)reader.GetInt64(0);
                    stats.Missed = (int)reader.GetInt64(1);
                    int total = stats.Attended + stats.Missed;
                    if (total > 0)
                    {
                        stats.Rate = $"{(int)((double)stats.Attended / total * 100)}%";
                    }
                }
            }

            return new FollowupDetailsResponse
            {
                CurrentAppointment = currentApt,
                ParentAppointment = parentApt,
                Patient = patient,
                Reliability = stats,
                AppointmentHistory = GetAppointmentsByPatientIdAndDoctorId(currentApt.PatientDbId ?? 0, doctorId)
            };
        }

        public List<AppointmentResponse> GetAppointmentsByPatientIdAndDoctorId(int patientId, int doctorId)
        {
            var appointments = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id,
                    a.time_slot_id,
                    a.status,
                    a.reason,
                    a.booked_at,
                    a.start_time,
                    a.end_time,
                    d.first_name || ' ' || d.last_name as doctor_full_name,
                    s.specialty_name,
                    a.parent_appointment_id,
                    a.doctor_reminder
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.patient_id = @pid AND a.doctor_id = @did
                ORDER BY a.start_time DESC;", conn);
            
            cmd.Parameters.AddWithValue("pid", patientId);
            cmd.Parameters.AddWithValue("did", doctorId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var parentId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9);
                appointments.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5), 
                    EndTime = reader.GetDateTime(6),   
                    DoctorName = reader.GetString(7),
                    Specialty = reader.IsDBNull(8) ? "Unknown" : reader.GetString(8),
                    ParentAppointmentId = parentId,
                    DoctorReminder = reader.IsDBNull(10) ? null : reader.GetString(10),
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New"
                });
            }

            return appointments;
        }

        private AppointmentResponse? GetAppointmentDetailsInternal(int id, NpgsqlConnection conn, int? doctorId = null)
        {
            var query = @"
                SELECT 
                    a.appointment_id, a.time_slot_id, a.status, a.reason, a.booked_at, 
                    a.start_time, a.end_time, a.doctor_reminder, a.parent_appointment_id,
                    d.first_name || ' ' || d.last_name as doctor_name,
                    s.specialty_name,
                    p.first_name || ' ' || p.last_name as patient_name,
                    p.patient_id,
                    a.doctor_id,
                    a.checked_in_at
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                JOIN patient p ON a.patient_id = p.patient_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.appointment_id = @id";
            
            if (doctorId.HasValue) query += " AND a.doctor_id = @did";

            using var cmd = new NpgsqlCommand(query, conn);
            cmd.Parameters.AddWithValue("id", id);
            if (doctorId.HasValue) cmd.Parameters.AddWithValue("did", doctorId.Value);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                var parentId = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8);
                return new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5),
                    EndTime = reader.GetDateTime(6),
                    DoctorReminder = reader.IsDBNull(7) ? null : reader.GetString(7),
                    ParentAppointmentId = parentId,
                    DoctorName = reader.GetString(9),
                    Specialty = reader.IsDBNull(10) ? "General" : reader.GetString(10),
                    PatientName = reader.GetString(11),
                    PatientDbId = reader.GetInt32(12),
                    DoctorId = reader.GetInt32(13),
                    CheckedInAt = reader.IsDBNull(14) ? (DateTime?)null : reader.GetDateTime(14),
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New"
                };
            }
            return null;
        }

        public List<AppointmentResponse> GetCreatedAppointments(int accountId)
        {
            var results = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    a.appointment_id, a.time_slot_id, a.status, a.reason, a.booked_at, 
                    a.start_time, a.end_time, 
                    d.first_name || ' ' || d.last_name as doctor_name,
                    p.first_name || ' ' || p.last_name as patient_name,
                    a.parent_appointment_id,
                    a.created_by
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                JOIN patient p ON a.patient_id = p.patient_id
                WHERE a.created_by = @uid
                ORDER BY a.start_time DESC;", conn);
            
            cmd.Parameters.AddWithValue("uid", accountId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var parentId = reader.IsDBNull(9) ? (int?)null : reader.GetInt32(9);
                results.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    TimeSlotId = reader.IsDBNull(1) ? 0 : reader.GetInt32(1),
                    Status = reader.GetString(2),
                    Reason = reader.IsDBNull(3) ? null : reader.GetString(3),
                    BookedAt = reader.GetDateTime(4),
                    StartTime = reader.GetDateTime(5),
                    EndTime = reader.GetDateTime(6),
                    DoctorName = reader.GetString(7),
                    PatientName = reader.GetString(8),
                    ParentAppointmentId = parentId,
                    CreatedBy = reader.GetInt32(10),
                    AppointmentType = parentId.HasValue ? "Follow-up" : "New"
                });
            }
            return results;
        }

        public bool FinalizeAppointmentOutcome(int appointmentId, int doctorAccountId, string reminder)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // Ensure doctor owns it
            using var verifyCmd = new NpgsqlCommand(@"
                SELECT 1 FROM appointment a 
                JOIN doctor d ON a.doctor_id = d.doctor_id 
                WHERE a.appointment_id = @aid AND d.account_id = @uid", conn);
            verifyCmd.Parameters.AddWithValue("aid", appointmentId);
            verifyCmd.Parameters.AddWithValue("uid", doctorAccountId);
            if (verifyCmd.ExecuteScalar() == null) return false;

            using var cmd = new NpgsqlCommand(@"
                UPDATE appointment 
                SET doctor_reminder = @rem, 
                    status = 'completed', 
                    completed_at = CURRENT_TIMESTAMP 
                WHERE appointment_id = @aid
                AND status NOT IN ('cancelled', 'completed', 'no_show')", conn);
            cmd.Parameters.AddWithValue("rem", reminder);
            cmd.Parameters.AddWithValue("aid", appointmentId);

            return cmd.ExecuteNonQuery() > 0;
        }


        public List<AppointmentResponse> GetAppointmentsForConfirmation()
        {
            var results = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            // Get appointments within 72 hours that are NOT confirmed and DON'T have a confirmation notification yet
            // 72 hours gives patients more time to react.
            using var cmd = new NpgsqlCommand(@"
                SELECT a.appointment_id, a.patient_id, a.start_time, a.end_time,
                       d.first_name || ' ' || d.last_name as doctor_name,
                       s.specialty_name
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.is_confirmed = FALSE
                  AND a.status = 'scheduled'
                  AND a.start_time > CURRENT_TIMESTAMP
                  AND a.start_time <= CURRENT_TIMESTAMP + INTERVAL '72 hours'
                  AND NOT EXISTS (
                      SELECT 1 FROM notification n 
                      WHERE n.appointment_id = a.appointment_id 
                      AND n.title = 'Appointment Confirmation Required'
                  )", conn);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new AppointmentResponse {
                    Id = reader.GetInt32(0),
                    PatientDbId = reader.GetInt32(1),
                    StartTime = reader.GetDateTime(2),
                    EndTime = reader.GetDateTime(3),
                    DoctorName = reader.GetString(4),
                    Specialty = reader.IsDBNull(5) ? "General" : reader.GetString(5)
                });
            }
            return results;
        }

        public List<AppointmentResponse> GetUnconfirmedEscalations()
        {
            var results = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            
            // Escalation: Scheduled, Not confirmed, and within 24 hours of starting.
            using var cmd = new NpgsqlCommand(@"
                SELECT a.appointment_id, a.patient_id, a.start_time, a.end_time,
                       d.first_name || ' ' || d.last_name as doctor_name,
                       s.specialty_name,
                       p.first_name || ' ' || p.last_name as patient_name,
                       p.phone as patient_phone,
                       p.account_id
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                JOIN patient p ON a.patient_id = p.patient_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.is_confirmed = FALSE
                  AND a.status = 'scheduled'
                  AND a.start_time > CURRENT_TIMESTAMP
                  AND a.start_time <= CURRENT_TIMESTAMP + INTERVAL '24 hours'
                ORDER BY a.start_time ASC;", conn);
            
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new AppointmentResponse {
                    Id = reader.GetInt32(0),
                    PatientDbId = reader.GetInt32(1),
                    StartTime = reader.GetDateTime(2),
                    EndTime = reader.GetDateTime(3),
                    DoctorName = reader.GetString(4),
                    Specialty = reader.IsDBNull(5) ? "General" : reader.GetString(5),
                    PatientName = reader.GetString(6),
                    PatientPhone = reader.IsDBNull(7) ? "N/A" : reader.GetString(7),
                    PatientAccountId = reader.IsDBNull(8) ? null : reader.GetInt32(8)
                });
            }
            return results;
        }

        public virtual List<AppointmentResponse> UpdatePassedAppointmentsToPending()
        {
            var affected = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // Select them first so we know who to notify
            using var selectCmd = new NpgsqlCommand(@"
                SELECT a.appointment_id, a.patient_id, a.start_time,
                       d.first_name || ' ' || d.last_name as doctor_name
                FROM appointment a
                JOIN doctor d ON a.doctor_id = d.doctor_id
                WHERE a.status IN ('scheduled', 'confirmed') 
                AND a.start_time < CURRENT_TIMESTAMP", conn);

            using (var reader = selectCmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    affected.Add(new AppointmentResponse {
                        Id = reader.GetInt32(0),
                        PatientDbId = reader.GetInt32(1),
                        StartTime = reader.GetDateTime(2),
                        DoctorName = reader.GetString(3)
                    });
                }
            }

            if (affected.Count > 0)
            {
                using var updateCmd = new NpgsqlCommand(@"
                    UPDATE appointment 
                    SET status = 'pending' 
                    WHERE status IN ('scheduled', 'confirmed') 
                    AND start_time < CURRENT_TIMESTAMP", conn);
                updateCmd.ExecuteNonQuery();
            }

            return affected;
        }

        public List<AppointmentResponse> GetAllPendingAppointments()
        {
            var list = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT a.appointment_id, a.start_time, a.end_time, a.status, a.reason,
                       p.first_name || ' ' || p.last_name as patient_name,
                       d.first_name || ' ' || d.last_name as doctor_name,
                       s.specialty_name
                FROM appointment a
                JOIN patient p ON a.patient_id = p.patient_id
                JOIN doctor d ON a.doctor_id = d.doctor_id
                LEFT JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE a.status = 'pending'
                ORDER BY a.start_time DESC", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new AppointmentResponse {
                    Id = reader.GetInt32(0),
                    StartTime = reader.GetDateTime(1).ToLocalTime(),
                    EndTime = reader.GetDateTime(2).ToLocalTime(),
                    Status = reader.GetString(3),
                    Reason = reader.IsDBNull(4) ? null : reader.GetString(4),
                    PatientName = reader.GetString(5),
                    DoctorName = reader.GetString(6),
                    Specialty = reader.IsDBNull(7) ? "General" : reader.GetString(7)
                });
            }
            return list;
        }

        public bool MarkAsNoShow(int appointmentId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE appointment SET status = 'no_show' WHERE appointment_id = @id", conn);
            cmd.Parameters.AddWithValue("id", appointmentId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ConfirmAppointment(int appointmentId, int patientAccountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            // Verify ownership and status
            using var verifyCmd = new NpgsqlCommand(@"
                SELECT a.appointment_id 
                FROM appointment a
                JOIN patient p ON a.patient_id = p.patient_id
                WHERE a.appointment_id = @id AND p.account_id = @aid AND a.status = 'scheduled'", conn);
            verifyCmd.Parameters.AddWithValue("id", appointmentId);
            verifyCmd.Parameters.AddWithValue("aid", patientAccountId);

            var exists = verifyCmd.ExecuteScalar() != null;
            if (!exists) return false;

            using var updateCmd = new NpgsqlCommand("UPDATE appointment SET status = 'confirmed' WHERE appointment_id = @id", conn);
            updateCmd.Parameters.AddWithValue("id", appointmentId);
            return updateCmd.ExecuteNonQuery() > 0;
        }
        public List<WeeklyAppointmentResponse> GetWeeklyAppointmentsCount()
        {
            var stats = new List<WeeklyAppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // Use date_trunc to get start of current week (Monday)
            // and fetch counts for each day.
            using var cmd = new NpgsqlCommand(@"
                WITH days AS (
                    SELECT generate_series(
                        date_trunc('week', CURRENT_DATE),
                        date_trunc('week', CURRENT_DATE) + '6 days'::interval,
                        '1 day'::interval
                    ) AS day
                )
                SELECT 
                    to_char(d.day, 'Day') as day_name,
                    COUNT(a.appointment_id) as count
                FROM days d
                LEFT JOIN appointment a ON date_trunc('day', a.start_time) = d.day AND a.status <> 'cancelled'
                GROUP BY d.day
                ORDER BY d.day;", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                stats.Add(new WeeklyAppointmentResponse
                {
                    Day = reader.GetString(0).Trim(),
                    Count = Convert.ToInt32(reader.GetInt64(1))
                });
            }
            return stats;
        }
        public List<AppointmentResponse> GetAppointmentsForReminders(DateTime date)
        {
            var list = new List<AppointmentResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT a.appointment_id, a.start_time, 
                       p.first_name || ' ' || p.last_name as patient_name,
                       acc.email as patient_email
                FROM appointment a
                JOIN patient p ON a.patient_id = p.patient_id
                JOIN account acc ON p.account_id = acc.account_id
                WHERE a.status = 'scheduled'
                AND a.start_time::date = @date", conn);
            
            cmd.Parameters.AddWithValue("date", date.Date);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new AppointmentResponse
                {
                    Id = reader.GetInt32(0),
                    StartTime = reader.GetDateTime(1),
                    PatientName = reader.GetString(2),
                    Email = reader.GetString(3)
                });
            }
            return list;
        }
    }
}
