using Npgsql;
using Backend.Models;
using Backend.Dtos.Requests;

namespace Backend.Data
{
    public class ReceptionistRepository
    {
        private readonly string _connectionString;

        public ReceptionistRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public Receptionist? GetReceptionistByAccountId(int accountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM receptionist WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("id", accountId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new Receptionist
                {
                    ReceptionistId = (int)reader["receptionist_id"],
                    AccountId = (int)reader["account_id"],
                    FirstName = (string)reader["first_name"],
                    LastName = (string)reader["last_name"],
                    Phone = reader["phone"] as string,
                    Gender = reader["gender"] as string
                };
            }
            return null;
        }

        public int CreateReceptionist(Receptionist r)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO receptionist (account_id, first_name, last_name, phone, gender)
                VALUES (@aid, @fname, @lname, @phone, @gender)
                RETURNING receptionist_id", conn);

            cmd.Parameters.AddWithValue("aid", r.AccountId);
            cmd.Parameters.AddWithValue("fname", r.FirstName);
            cmd.Parameters.AddWithValue("lname", r.LastName);
            cmd.Parameters.AddWithValue("phone", r.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gender", r.Gender ?? (object)DBNull.Value);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }
        public bool UpdateReceptionist(int accountId, UpdateProfileRequest dto)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE receptionist 
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
