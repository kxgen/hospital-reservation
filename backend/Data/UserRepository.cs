using Backend.Models;
using Npgsql;

namespace Backend.Data
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public bool EmailExists(string email)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT COUNT(*) FROM users WHERE email = @Email",
                conn
            );

            cmd.Parameters.AddWithValue("@Email", email);

            return (long)cmd.ExecuteScalar() > 0;
        }

        public int CreateUser(User user)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var tran = conn.BeginTransaction();

            try
            {
                using var userCmd = new NpgsqlCommand(@"
                    INSERT INTO users 
                    (first_name, last_name, email, phone, password_hash, role_id)
                    VALUES 
                    (@FirstName, @LastName, @Email, @Phone, @PasswordHash,
                        (SELECT role_id FROM role WHERE role_name = @Role))
                    RETURNING user_id;
                ", conn, tran);

                userCmd.Parameters.AddWithValue("@FirstName", user.FirstName);
                userCmd.Parameters.AddWithValue("@LastName", user.LastName);
                userCmd.Parameters.AddWithValue("@Email", user.Email);
                userCmd.Parameters.AddWithValue("@Phone", (object?)user.Phone ?? DBNull.Value);
                userCmd.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
                userCmd.Parameters.AddWithValue("@Role", user.Role);

                int userId = (int)userCmd.ExecuteScalar();

                // If patient → insert into patient table
                if (user.Role.Equals("patient", StringComparison.OrdinalIgnoreCase))
                {
                    using var patientCmd = new NpgsqlCommand(@"
                        INSERT INTO patient (user_id, gender, date_of_birth)
                        VALUES (@UserId, @Gender, @DateOfBirth);
                    ", conn, tran);

                    patientCmd.Parameters.AddWithValue("@UserId", userId);
                    patientCmd.Parameters.AddWithValue("@Gender", (object?)user.Gender ?? DBNull.Value);
                    patientCmd.Parameters.AddWithValue("@DateOfBirth", (object?)user.DateOfBirth ?? DBNull.Value);

                    patientCmd.ExecuteNonQuery();
                }

                tran.Commit();
                return userId;
            }
            catch
            {
                tran.Rollback();
                throw;
            }
        }

        public User? GetUserByEmail(string email)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT u.user_id, u.first_name, u.last_name, u.email, 
                       u.phone, u.password_hash, r.role_name
                FROM users u
                JOIN role r ON u.role_id = r.role_id
                WHERE u.email = @Email
            ", conn);

            cmd.Parameters.AddWithValue("@Email", email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new User
            {
                Id = reader.GetInt32(0),
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                Email = reader.GetString(3),
                Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
                PasswordHash = reader.GetString(5),
                Role = reader.GetString(6)
            };
        }
    }
}
