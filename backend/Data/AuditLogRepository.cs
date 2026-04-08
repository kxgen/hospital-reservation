using Npgsql;
using Backend.Models;
using Backend.Dtos.Responses;
using System;
using System.Collections.Generic;

namespace Backend.Data
{
    public class AuditLogRepository
    {
        private readonly string _connectionString;

        public AuditLogRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void LogAction(int? userId, string action, string targetTable, int targetId, string? details, string? ipAddress = null)
        {
            // We use a safe synchronous approach with explicit disposal.
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO audit_logs (account_id, action, target_table, target_id, details, created_at)
                    VALUES (@uid, @action, @targetTable, @targetId, @details, CURRENT_TIMESTAMP)", conn))
                {
                    cmd.Parameters.AddWithValue("uid", (object?)userId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("action", action);
                    cmd.Parameters.AddWithValue("targetTable", targetTable);
                    cmd.Parameters.AddWithValue("targetId", targetId);
                    cmd.Parameters.AddWithValue("details", (object?)details ?? DBNull.Value);

                    cmd.ExecuteNonQuery();
                }
            }
        }
        public int GetTotalLogsCount(int? accountId = null, DateTime? date = null)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            var sql = "SELECT COUNT(*) FROM audit_logs WHERE 1=1";
            if (accountId.HasValue) sql += " AND account_id = @aid";
            if (date.HasValue) sql += " AND created_at::date = @date";

            using var cmd = new NpgsqlCommand(sql, conn);
            if (accountId.HasValue) cmd.Parameters.AddWithValue("aid", accountId.Value);
            if (date.HasValue) cmd.Parameters.AddWithValue("date", date.Value.Date);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<AuditLogResponse> GetLogsPaginated(int page, int pageSize, int? accountId = null, DateTime? date = null)
        {
            var logs = new List<AuditLogResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            int offset = (page - 1) * pageSize;

            var sql = @"
                SELECT a.id, a.action, a.target_table, a.target_id, a.details, a.created_at,
                       acc.email as actor_email
                FROM audit_logs a
                LEFT JOIN account acc ON a.account_id = acc.account_id
                WHERE 1=1";
            
            if (accountId.HasValue) sql += " AND a.account_id = @aid";
            if (date.HasValue) sql += " AND a.created_at::date = @date";

            sql += " ORDER BY a.created_at DESC LIMIT @limit OFFSET @offset";

            using var cmd = new NpgsqlCommand(sql, conn);
            if (accountId.HasValue) cmd.Parameters.AddWithValue("aid", accountId.Value);
            if (date.HasValue) cmd.Parameters.AddWithValue("date", date.Value.Date);
            cmd.Parameters.AddWithValue("limit", pageSize);
            cmd.Parameters.AddWithValue("offset", offset);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new AuditLogResponse
                {
                    Id = reader.GetInt32(0),
                    Action = reader.GetString(1),
                    Target = $"{reader.GetString(2)} #{reader.GetInt32(3)}",
                    Details = reader.IsDBNull(4) ? null : reader.GetString(4),
                    IpAddress = null,
                    Timestamp = reader.GetDateTime(5),
                    ActorName = reader.IsDBNull(6) ? "System" : reader.GetString(6)
                });
            }
            return logs;
        }

        public List<AuditLogResponse> GetRecentLogs(int limit = 10)
        {
            var logs = new List<AuditLogResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT a.id, a.action, a.target_table, a.target_id, a.details, a.created_at,
                       COALESCE(d.first_name, r.first_name, ad.first_name, p.first_name) as fname,
                       COALESCE(d.last_name, r.last_name, ad.last_name, p.last_name) as lname
                FROM audit_logs a
                LEFT JOIN doctor d ON a.account_id = d.account_id
                LEFT JOIN receptionist r ON a.account_id = r.account_id
                LEFT JOIN admin ad ON a.account_id = ad.account_id
                LEFT JOIN patient p ON a.account_id = p.account_id
                ORDER BY a.created_at DESC
                LIMIT @limit", conn);
            
            cmd.Parameters.AddWithValue("limit", limit);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var firstName = reader.IsDBNull(6) ? null : reader.GetString(6);
                var lastName = reader.IsDBNull(7) ? null : reader.GetString(7);
                
                logs.Add(new AuditLogResponse
                {
                    Id = reader.GetInt32(0),
                    Action = reader.GetString(1),
                    Target = $"{reader.GetString(2)} #{reader.GetInt32(3)}",
                    Details = reader.IsDBNull(4) ? null : reader.GetString(4),
                    IpAddress = null,
                    Timestamp = reader.GetDateTime(5),
                    ActorName = (firstName != null || lastName != null) 
                                ? $"{firstName} {lastName}".Trim() 
                                : "System"
                });
            }
            return logs;
        }
    }
}
