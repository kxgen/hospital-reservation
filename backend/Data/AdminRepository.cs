using Npgsql;
using Backend.Models;
using Backend.Dtos.Requests;

namespace Backend.Data
{
    public class AdminRepository
    {
        private readonly string _connectionString;

        public AdminRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Admin? GetAdminByAccountId(int accountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM admin WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("id", accountId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Admin
                {
                    AdminId = (int)reader["admin_id"],
                    AccountId = (int)reader["account_id"],
                    FirstName = (string)reader["first_name"],
                    LastName = (string)reader["last_name"],
                    Phone = reader["phone"] as string,
                    Gender = reader["gender"] as string
                };
            }
            return null;
        }

        public int CreateAdmin(Admin a)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO admin (account_id, first_name, last_name, phone, gender)
                VALUES (@aid, @fname, @lname, @phone, @gender)
                RETURNING admin_id", conn);

            cmd.Parameters.AddWithValue("aid", a.AccountId);
            cmd.Parameters.AddWithValue("fname", a.FirstName);
            cmd.Parameters.AddWithValue("lname", a.LastName);
            cmd.Parameters.AddWithValue("phone", a.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gender", a.Gender ?? (object)DBNull.Value);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        public bool UpdateAdmin(int accountId, UpdateProfileRequest dto)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE admin 
                SET first_name = @fname, last_name = @lname, phone = @phone, gender = @gender
                WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("fname", dto.FirstName);
            cmd.Parameters.AddWithValue("lname", dto.LastName);
            cmd.Parameters.AddWithValue("phone", dto.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gender", dto.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("id", accountId);
            return cmd.ExecuteNonQuery() > 0;
        }
    }
}
