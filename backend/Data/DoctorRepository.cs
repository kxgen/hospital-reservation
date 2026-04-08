using Npgsql;
using Backend.Models;
using Backend.Dtos.Requests;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Backend.Data
{
    public class DoctorRepository
    {
        private readonly string _connectionString = string.Empty;

        public DoctorRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected DoctorRepository() { }

        public virtual Doctor? GetDoctorByAccountId(int accountId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT d.*, s.specialty_name 
                FROM doctor d 
                JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("id", accountId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapDoctor(reader);
            }
            return null;
        }

        public virtual Doctor? GetDoctorById(int doctorId)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                SELECT d.*, s.specialty_name 
                FROM doctor d 
                JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE doctor_id = @id", conn);
            cmd.Parameters.AddWithValue("id", doctorId);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapDoctor(reader);
            }
            return null;
        }

        public virtual List<Specialty> GetAllSpecialties()
        {
            var list = new List<Specialty>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT * FROM specialty ORDER BY specialty_name", conn);
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new Specialty 
                { 
                    SpecialtyId = (int)reader["specialty_id"], 
                    SpecialtyName = (string)reader["specialty_name"] 
                });
            }
            return list;
        }

        public virtual List<Doctor> GetAllDoctors(string? search = null, string? specialties = null, string? gender = null)
        {
            var list = new List<Doctor>();
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            
            var queryBuilder = new System.Text.StringBuilder(@"
                SELECT d.*, s.specialty_name 
                FROM doctor d 
                JOIN specialty s ON d.specialty_id = s.specialty_id
                WHERE 1=1");

            if (!string.IsNullOrEmpty(search))
            {
                queryBuilder.Append(" AND (LOWER(d.first_name) || ' ' || LOWER(d.last_name) LIKE @search OR LOWER(s.specialty_name) LIKE @search)");
            }

            if (!string.IsNullOrEmpty(gender) && gender.ToLower() != "both")
            {
                queryBuilder.Append(" AND LOWER(d.gender) = @gender");
            }

            if (!string.IsNullOrEmpty(specialties))
            {
                queryBuilder.Append(" AND s.specialty_name = ANY(@specs)");
            }

            using var cmd = new NpgsqlCommand(queryBuilder.ToString(), conn);

            if (!string.IsNullOrEmpty(search))
            {
                cmd.Parameters.AddWithValue("search", $"%{search.ToLower()}%");
            }

            if (!string.IsNullOrEmpty(gender) && gender.ToLower() != "both")
            {
                cmd.Parameters.AddWithValue("gender", gender.ToLower());
            }

            if (!string.IsNullOrEmpty(specialties))
            {
                var specList = specialties.Split(',').Select(s => s.Trim()).ToArray();
                cmd.Parameters.AddWithValue("specs", specList);
            }

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(MapDoctor(reader));
            }
            return list;
        }

        public virtual int CreateDoctor(Doctor doctor)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                INSERT INTO doctor 
                (account_id, first_name, last_name, phone, gender, specialty_id, bio, photo_url) 
                VALUES 
                (@aid, @fname, @lname, @phone, @gender, @sid, @bio, @photo) 
                RETURNING doctor_id", conn);

            cmd.Parameters.AddWithValue("aid", doctor.AccountId);
            cmd.Parameters.AddWithValue("fname", doctor.FirstName);
            cmd.Parameters.AddWithValue("lname", doctor.LastName);
            cmd.Parameters.AddWithValue("phone", doctor.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gender", doctor.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("sid", doctor.SpecialtyId);
            cmd.Parameters.AddWithValue("bio", doctor.Bio ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("photo", doctor.PhotoUrl ?? (object)DBNull.Value);

            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public virtual bool UpdateDoctor(int accountId, UpdateProfileRequest dto)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            using var cmd = new NpgsqlCommand(@"
                UPDATE doctor 
                SET first_name = @fname, last_name = @lname, phone = @phone, gender = @gender, bio = @bio, specialty_id = @sid
                WHERE account_id = @id", conn);
            cmd.Parameters.AddWithValue("fname", dto.FirstName);
            cmd.Parameters.AddWithValue("lname", dto.LastName);
            cmd.Parameters.AddWithValue("phone", dto.Phone ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("gender", dto.Gender ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("bio", dto.Bio ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("sid", dto.SpecialtyId ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("id", accountId);
            return cmd.ExecuteNonQuery() > 0;
        }

        private Doctor MapDoctor(NpgsqlDataReader reader)
        {
            return new Doctor
            {
                DoctorId = (int)reader["doctor_id"],
                AccountId = (int)reader["account_id"],
                FirstName = (string)reader["first_name"],
                LastName = (string)reader["last_name"],
                Phone = reader["phone"] as string,
                Gender = reader["gender"] as string,
                SpecialtyId = (int)reader["specialty_id"],
                Bio = reader["bio"] as string,
                PhotoUrl = reader["photo_url"] as string,
                SpecialtyName = (string)reader["specialty_name"]
            };
        }
    }
}