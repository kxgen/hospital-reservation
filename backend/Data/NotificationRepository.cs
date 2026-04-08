using Npgsql;
using Backend.Models;
using System.Collections.Generic;

namespace Backend.Data
{
    public class NotificationRepository
    {
        private readonly string _connectionString = string.Empty;

        public NotificationRepository(string connectionString)
        {
            _connectionString = connectionString;
            EnsureTableExists();
        }

        protected NotificationRepository() { }

        private void EnsureTableExists()
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    CREATE TABLE IF NOT EXISTS notification (
                        notification_id SERIAL PRIMARY KEY,
                        patient_id INT NOT NULL REFERENCES patient(patient_id),
                        title TEXT NOT NULL,
                        message TEXT NOT NULL,
                        type TEXT DEFAULT 'General',
                        is_read BOOLEAN DEFAULT FALSE,
                        created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
                        created_by INT REFERENCES account(account_id),
                        appointment_id INT REFERENCES appointment(appointment_id)
                    );", conn))
                {
                    cmd.ExecuteNonQuery();
                }

                // Migration: Ensure column exists if table was already there
                using (var cmdMigrate = new NpgsqlCommand(@"
                    DO $$ 
                    BEGIN 
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='notification' AND column_name='appointment_id') THEN
                            ALTER TABLE notification ADD COLUMN appointment_id INT REFERENCES appointment(appointment_id);
                        END IF;
                    END $$;", conn))
                {
                    cmdMigrate.ExecuteNonQuery();
                }
            }
        }

        public virtual void CreateNotification(Notification notification)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO notification (patient_id, title, message, type, created_by, appointment_id)
                    VALUES (@pid, @title, @msg, @type, @cby, @aid);", conn))
                {
                    cmd.Parameters.AddWithValue("pid", notification.PatientId);
                    cmd.Parameters.AddWithValue("title", notification.Title);
                    cmd.Parameters.AddWithValue("msg", notification.Message);
                    cmd.Parameters.AddWithValue("type", notification.Type);
                    cmd.Parameters.AddWithValue("cby", (object?)notification.CreatedBy ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("aid", (object?)notification.AppointmentId ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }

        public List<Notification> GetUnreadNotifications(int patientId)
        {
            return GetFilteredNotifications(patientId, "AND n.is_read = FALSE");
        }

        public List<Notification> GetReadNotifications(int patientId)
        {
            return GetFilteredNotifications(patientId, "AND n.is_read = TRUE");
        }

        private List<Notification> GetFilteredNotifications(int patientId, string extraFilter)
        {
            var results = new List<Notification>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand($@"
                SELECT n.notification_id, n.title, n.message, n.type, n.is_read, n.created_at, n.created_by, n.appointment_id, 
                       COALESCE(a.is_confirmed, FALSE),
                       r.role_name as sender_role,
                       a.status,
                       a.start_time
                FROM notification n
                LEFT JOIN appointment a ON n.appointment_id = a.appointment_id
                LEFT JOIN account acc ON n.created_by = acc.account_id
                LEFT JOIN role r ON acc.role_id = r.role_id
                WHERE n.patient_id = @pid {extraFilter}
                ORDER BY n.created_at DESC;", conn);
            
            cmd.Parameters.AddWithValue("pid", patientId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string role = reader.IsDBNull(9) ? "System" : reader.GetString(9);
                string senderName = "Trinity Health System";
                if (role == "admin" || role == "receptionist") senderName = "Hospital Staff";
                else if (role == "doctor") senderName = "Clinical Team";

                results.Add(new Notification
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Message = reader.GetString(2),
                    Type = reader.GetString(3),
                    IsRead = reader.GetBoolean(4),
                    CreatedAt = reader.GetDateTime(5),
                    CreatedBy = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                    AppointmentId = reader.IsDBNull(7) ? null : reader.GetInt32(7),
                    IsConfirmed = reader.GetBoolean(8),
                    SenderName = senderName,
                    AppointmentStatus = reader.IsDBNull(10) ? null : reader.GetString(10),
                    AppointmentStartTime = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                    PatientId = patientId
                });
            }
            return results;
        }

        public List<Notification> GetPatientNotifications(int patientId)
        {
            return GetFilteredNotifications(patientId, "");
        }

        public int GetUnreadCount(int patientId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM notification WHERE patient_id = @pid AND is_read = FALSE", conn);
            cmd.Parameters.AddWithValue("pid", patientId);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public void MarkAsRead(int notificationId, int patientId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE notification SET is_read = TRUE 
                WHERE notification_id = @nid AND patient_id = @pid;", conn);
            cmd.Parameters.AddWithValue("nid", notificationId);
            cmd.Parameters.AddWithValue("pid", patientId);
            cmd.ExecuteNonQuery();
        }

        public void MarkAllAsRead(int patientId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE notification SET is_read = TRUE 
                WHERE patient_id = @pid;", conn);
            cmd.Parameters.AddWithValue("pid", patientId);
            cmd.ExecuteNonQuery();
        }
    }
}
