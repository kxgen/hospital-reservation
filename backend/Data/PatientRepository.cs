using Npgsql;
using Backend.Models;
using Backend.Dtos.Requests;
using Backend.Dtos.Responses;

namespace Backend.Data
{
    public class PatientRepository
    {
        private readonly string _connectionString = string.Empty;

        public PatientRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected PatientRepository() { }

        public virtual int GetPatientIdByPhone(string phone)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT patient_id FROM patient WHERE phone = @p LIMIT 1", conn);
            cmd.Parameters.AddWithValue("p", phone);
            var result = cmd.ExecuteScalar();
            return result != null ? Convert.ToInt32(result) : 0;
        }

        public Patient? GetPatientByAccountId(int accountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT * FROM patient WHERE account_id = @uid", conn);
            cmd.Parameters.AddWithValue("uid", accountId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return null;

            return MapPatient(reader);
        }

        public Patient? GetPatientById(int patientId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM patient WHERE patient_id = @pid", conn);
            cmd.Parameters.AddWithValue("pid", patientId);

            using var reader = cmd.ExecuteReader();
            if(!reader.Read()) return null;
            return MapPatient(reader);
        }

        public int GetPatientIdByAccountId(int accountId)
        {
            using (var conn = new NpgsqlConnection(_connectionString))
            {
                conn.Open();
                using (var cmd = new NpgsqlCommand(
                    "SELECT patient_id FROM patient WHERE account_id = @id", conn))
                {
                    cmd.Parameters.AddWithValue("id", accountId);
                    var result = cmd.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
        }

        public int CreateLinkPatient(Patient patient)
        {
            // For registered users
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO patient (account_id, first_name, last_name, phone, gender, date_of_birth) 
                VALUES (@aid, @fname, @lname, @phone, @gender, @dob) 
                RETURNING patient_id", conn);
            
            // Allow duplicates for linked patients? No, should restrict too.
            if (!string.IsNullOrEmpty(patient.Phone))
            {
                 using var checkCmd = new NpgsqlCommand("SELECT 1 FROM patient WHERE phone = @p LIMIT 1", conn);
                 checkCmd.Parameters.AddWithValue("p", patient.Phone);
                 var exists = checkCmd.ExecuteScalar();
                 if (exists != null) throw new Exception("Multiple profiles cannot share the same phone number.");
            }
            
            cmd.Parameters.AddWithValue("aid", (object?)patient.AccountId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("fname", patient.FirstName);
            cmd.Parameters.AddWithValue("lname", patient.LastName);
            cmd.Parameters.AddWithValue("phone", patient.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gender", patient.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("dob", patient.DateOfBirth.HasValue ? (object)patient.DateOfBirth.Value : DBNull.Value);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public int CreateGuestPatient(string firstName, string lastName, string phone, string gender, DateTime? dob)
        {
            // Guest -> AccountId is NULL
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO patient (account_id, first_name, last_name, phone, gender, date_of_birth) 
                VALUES (NULL, @fname, @lname, @phone, @gender, @dob) 
                RETURNING patient_id", conn);

            if (!string.IsNullOrEmpty(phone))
            {
                 using var checkCmd = new NpgsqlCommand("SELECT 1 FROM patient WHERE phone = @p LIMIT 1", conn);
                 checkCmd.Parameters.AddWithValue("p", phone);
                 var exists = checkCmd.ExecuteScalar();
                 if (exists != null) throw new Exception("This phone number is already registered to another patient.");
            }
            
            cmd.Parameters.AddWithValue("fname", firstName);
            cmd.Parameters.AddWithValue("lname", lastName);
            cmd.Parameters.AddWithValue("phone", phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gender", gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("dob", dob.HasValue ? (object)dob.Value : DBNull.Value);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public List<Patient> GetAllPatients()
        {
             var list = new List<Patient>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM patient", conn); // TODO: Join with account if we need email/status
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapPatient(reader));
            }
            return list;
        }

        public bool UpdatePatient(int accountId, UpdateProfileRequest dto)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            DateTime? dobAsDate = null;
            if (!string.IsNullOrEmpty(dto.DateOfBirth))
            {
                if (DateTime.TryParse(dto.DateOfBirth, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                {
                    dobAsDate = d;
                }
            }
            
            if (!string.IsNullOrEmpty(dto.Phone))
            {
                using var checkCmd = new NpgsqlCommand(@"
                    SELECT 1 FROM patient 
                    WHERE phone = @p AND account_id != @id 
                    LIMIT 1", conn);
                checkCmd.Parameters.AddWithValue("p", dto.Phone);
                checkCmd.Parameters.AddWithValue("id", accountId);
                if (checkCmd.ExecuteScalar() != null) throw new Exception("This phone number is already registered to another patient.");
            }

            using var cmd = new NpgsqlCommand(@"
                UPDATE patient 
                SET first_name = @fname, last_name = @lname, phone = @phone, gender = @gender, date_of_birth = @dob
                WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("fname", dto.FirstName);
            cmd.Parameters.AddWithValue("lname", dto.LastName);
            cmd.Parameters.AddWithValue("phone", dto.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gender", dto.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("dob", dobAsDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("id", accountId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool UpdatePatientById(int patientId, UpdateProfileRequest dto)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();

            DateTime? dobAsDate = null;
            if (!string.IsNullOrEmpty(dto.DateOfBirth))
            {
                if (DateTime.TryParse(dto.DateOfBirth, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d))
                {
                    dobAsDate = d;
                }
            }

            if (!string.IsNullOrEmpty(dto.Phone))
            {
                using var checkCmd = new NpgsqlCommand(@"
                    SELECT 1 FROM patient 
                    WHERE phone = @p AND patient_id != @id 
                    LIMIT 1", conn);
                checkCmd.Parameters.AddWithValue("p", dto.Phone);
                checkCmd.Parameters.AddWithValue("id", patientId);
                if (checkCmd.ExecuteScalar() != null) throw new Exception("This phone number is already registered to another patient.");
            }

            using var cmd = new NpgsqlCommand(@"
                UPDATE patient 
                SET first_name = @fname, last_name = @lname, phone = @phone, gender = @gender, date_of_birth = @dob
                WHERE patient_id = @id", conn);
            cmd.Parameters.AddWithValue("fname", dto.FirstName);
            cmd.Parameters.AddWithValue("lname", dto.LastName);
            cmd.Parameters.AddWithValue("phone", dto.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gender", dto.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("dob", dobAsDate ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("id", patientId);
            return cmd.ExecuteNonQuery() > 0;
        }

        private Patient MapPatient(NpgsqlDataReader reader)
        {
            var patient = new Patient
            {
                PatientId = (int)reader["patient_id"],
                AccountId = reader["account_id"] as int?,
                FirstName = (string)reader["first_name"],
                LastName = (string)reader["last_name"],
                Phone = reader["phone"] as string,
                Gender = reader["gender"] as string
            };

            var dobObj = reader["date_of_birth"];
            if (dobObj != null && dobObj != DBNull.Value)
            {
                if (dobObj is DateTime dt)
                {
                    patient.DateOfBirth = dt;
                }
                else if (dobObj.GetType().Name == "DateOnly") // Handle .NET 6+ DateOnly type from Npgsql
                {
                    // Use reflection-less approach if possible, but for broad compatibility:
                    patient.DateOfBirth = reader.GetDateTime(reader.GetOrdinal("date_of_birth"));
                }
                else
                {
                    patient.DateOfBirth = Convert.ToDateTime(dobObj);
                }
            }

            return patient;
        }
    }
}
