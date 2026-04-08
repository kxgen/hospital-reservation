using Npgsql;
using Backend.Models;
using Backend.Dtos.Responses;

namespace Backend.Data
{
    public class AccountRepository
    {
        private readonly string _connectionString = string.Empty;

        public AccountRepository(string connectionString)
        {
            _connectionString = connectionString;
            EnsureColumnExists();
        }

        protected AccountRepository() { }

        private void EnsureColumnExists()
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    DO $$ 
                    BEGIN 
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                       WHERE table_name='account' AND column_name='is_password_change_required') THEN 
                            ALTER TABLE account ADD COLUMN is_password_change_required BOOLEAN NOT NULL DEFAULT false; 
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                       WHERE table_name='account' AND column_name='password_reset_otp_hash') THEN 
                            ALTER TABLE account ADD COLUMN password_reset_otp_hash TEXT; 
                        END IF;
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns 
                                       WHERE table_name='account' AND column_name='password_reset_otp_expiry') THEN 
                            ALTER TABLE account ADD COLUMN password_reset_otp_expiry TIMESTAMP; 
                        END IF;
                    END $$;", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public Account? GetAccountByEmail(string email)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(@"
                    SELECT a.*, r.role_name 
                    FROM account a 
                    JOIN role r ON a.role_id = r.role_id 
                    WHERE a.email = @email", conn))
                {
                    cmd.Parameters.AddWithValue("email", email);

                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new Account
                            {
                                AccountId = (int)reader["account_id"],
                                Email = (string)reader["email"],
                                PasswordHash = (string)reader["password_hash"],
                                RoleId = (int)reader["role_id"],
                                RoleName = (string)reader["role_name"],
                                IsActive = (bool)reader["is_active"],
                                IsPasswordChangeRequired = (bool)reader["is_password_change_required"],
                                PasswordResetOtpHash = reader["password_reset_otp_hash"] as string,
                                PasswordResetOtpExpiry = reader["password_reset_otp_expiry"] as DateTime?,
                                CreatedAt = (DateTime)reader["created_at"],
                                UpdatedAt = (DateTime)reader["updated_at"]
                            };
                        }
                    }
                }
            }
            return null;
        }

        public virtual bool EmailExists(string email)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand("SELECT count(*) FROM account WHERE email = @email", conn))
                {
                    cmd.Parameters.AddWithValue("email", email);
                    return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
                }
            }
        }

        public int? GetRoleIdByName(string roleName)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT role_id FROM role WHERE LOWER(role_name) = LOWER(@name)", conn);
            cmd.Parameters.AddWithValue("name", roleName);
            var result = cmd.ExecuteScalar();
            return result != null ? (int)result : null;
        }

        public int CreateAccount(Account account)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO account (email, password_hash, role_id, is_password_change_required) VALUES (@e, @p, @r, @f) RETURNING account_id", conn);
            cmd.Parameters.AddWithValue("e", account.Email);
            cmd.Parameters.AddWithValue("p", account.PasswordHash);
            cmd.Parameters.AddWithValue("r", account.RoleId);
            cmd.Parameters.AddWithValue("f", account.IsPasswordChangeRequired);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        public (int doctors, int receptionists, int patients) GetSystemStats()
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            
            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    (SELECT COUNT(*) FROM doctor) as doctors,
                    (SELECT COUNT(*) FROM receptionist) as receptionists,
                    (SELECT COUNT(*) FROM patient) as patients", conn);
            
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return (
                    Convert.ToInt32(reader["doctors"]),
                    Convert.ToInt32(reader["receptionists"]),
                    Convert.ToInt32(reader["patients"])
                );
            }
            return (0, 0, 0);
        }

        public List<Account> GetAllAccounts()
        {
            var list = new List<Account>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT a.*, r.role_name 
                FROM account a 
                JOIN role r ON a.role_id = r.role_id 
                ORDER BY a.created_at DESC", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Account
                {
                    AccountId = (int)reader["account_id"],
                    Email = (string)reader["email"],
                    PasswordHash = (string)reader["password_hash"],
                    RoleId = (int)reader["role_id"],
                    RoleName = (string)reader["role_name"],
                    IsActive = (bool)reader["is_active"],
                    IsPasswordChangeRequired = (bool)reader["is_password_change_required"],
                    PasswordResetOtpHash = reader["password_reset_otp_hash"] as string,
                    PasswordResetOtpExpiry = reader["password_reset_otp_expiry"] as DateTime?,
                    CreatedAt = (DateTime)reader["created_at"],
                    UpdatedAt = (DateTime)reader["updated_at"]
                });
            }
            return list;
        }

        public bool UpdateStatus(int accountId, bool isActive)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE account SET is_active = @active, updated_at = CURRENT_TIMESTAMP WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("active", isActive);
            cmd.Parameters.AddWithValue("id", accountId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdatePassword(int accountId, string newHash, bool forceReset = false)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE account 
                SET password_hash = @hash, 
                    is_password_change_required = @force,
                    updated_at = CURRENT_TIMESTAMP 
                WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("hash", newHash);
            cmd.Parameters.AddWithValue("force", forceReset);
            cmd.Parameters.AddWithValue("id", accountId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public virtual Account? GetAccountById(int accountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT a.*, r.role_name 
                FROM account a 
                JOIN role r ON a.role_id = r.role_id 
                WHERE a.account_id = @id", conn);
            cmd.Parameters.AddWithValue("id", accountId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Account
                {
                    AccountId = (int)reader["account_id"],
                    Email = (string)reader["email"],
                    PasswordHash = (string)reader["password_hash"],
                    RoleId = (int)reader["role_id"],
                    RoleName = (string)reader["role_name"],
                    IsActive = (bool)reader["is_active"],
                    IsPasswordChangeRequired = (bool)reader["is_password_change_required"],
                    PasswordResetOtpHash = reader["password_reset_otp_hash"] as string,
                    PasswordResetOtpExpiry = reader["password_reset_otp_expiry"] as DateTime?,
                    CreatedAt = (DateTime)reader["created_at"],
                    UpdatedAt = (DateTime)reader["updated_at"]
                };
            }
            return null;
        }

        public bool UpdateOtp(int accountId, string hash, DateTime expiry)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE account 
                SET password_reset_otp_hash = @hash, 
                    password_reset_otp_expiry = @expiry,
                    updated_at = CURRENT_TIMESTAMP 
                WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("hash", hash);
            cmd.Parameters.AddWithValue("expiry", expiry);
            cmd.Parameters.AddWithValue("id", accountId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool ClearOtp(int accountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE account 
                SET password_reset_otp_hash = NULL, 
                    password_reset_otp_expiry = NULL,
                    updated_at = CURRENT_TIMESTAMP 
                WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("id", accountId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public List<UserRegistrationResponse> GetRegistrationStats()
        {
            var stats = new List<UserRegistrationResponse>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            // Fetch registration counts grouped by date for the last 30 days
            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    to_char(created_at::date, 'YYYY-MM-DD') as reg_date,
                    COUNT(*) as count
                FROM account
                WHERE created_at >= CURRENT_DATE - INTERVAL '30 days'
                GROUP BY created_at::date
                ORDER BY created_at::date", conn);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                stats.Add(new UserRegistrationResponse
                {
                    Date = reader.GetString(0),
                    Count = Convert.ToInt32(reader.GetInt64(1))
                });
            }
            return stats;
        }
    }
}
