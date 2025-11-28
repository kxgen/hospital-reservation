-- Roles lookup table
CREATE TABLE role (
    role_id SERIAL PRIMARY KEY,
    role_name VARCHAR(50) UNIQUE NOT NULL -- e.g., 'patient', 'doctor', 'receptionist', 'admin'
);

-- Users table (central identity)
CREATE TABLE users (
    user_id SERIAL PRIMARY KEY,
    email VARCHAR(150) UNIQUE NOT NULL,
    phone VARCHAR(20),
    password_hash VARCHAR(255) NOT NULL,
    role_id INT REFERENCES role(role_id) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Patient table
CREATE TABLE patient (
    patient_id SERIAL PRIMARY KEY,
    user_id INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
    gender VARCHAR(10),
    date_of_birth DATE,
    medical_record_number VARCHAR(50) UNIQUE
);

-- Doctor table
CREATE TABLE doctor (
    doctor_id SERIAL PRIMARY KEY,
    user_id INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE,
    specialization VARCHAR(100),
    licence_no VARCHAR(50) UNIQUE
);

-- Receptionist table
CREATE TABLE receptionist (
    receptionist_id SERIAL PRIMARY KEY,
    user_id INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE
);

-- Hospital Admin table
CREATE TABLE hospital_admin (
    admin_id SERIAL PRIMARY KEY,
    user_id INT UNIQUE REFERENCES users(user_id) ON DELETE CASCADE
);

-- Doctor's time slots for appointments
CREATE TABLE time_slot (
    slot_id SERIAL PRIMARY KEY,
    doctor_id INT REFERENCES doctor(doctor_id) ON DELETE CASCADE,
    start_time TIMESTAMP NOT NULL,
    end_time TIMESTAMP NOT NULL,
    is_booked BOOLEAN DEFAULT FALSE,
    CONSTRAINT no_overlap UNIQUE (doctor_id, start_time, end_time) -- prevents overlapping slots
);

-- Appointments table
CREATE TABLE appointment (
    appointment_id SERIAL PRIMARY KEY,
    patient_id INT REFERENCES patient(patient_id) ON DELETE CASCADE,
    time_slot_id INT UNIQUE REFERENCES time_slot(slot_id) ON DELETE CASCADE,
    created_by INT REFERENCES receptionist(receptionist_id), -- nullable if booked online
    booked_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    status VARCHAR(20) DEFAULT 'pending' CHECK (status IN ('pending', 'confirmed', 'cancelled', 'completed'))
);

-- Messages table
CREATE TABLE message (
    message_id SERIAL PRIMARY KEY,
    sender_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    receiver_id INT REFERENCES users(user_id) ON DELETE SET NULL,
    message TEXT NOT NULL,
    type VARCHAR(50),
    time_sent TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    is_read BOOLEAN DEFAULT FALSE
);

-- Logs table
CREATE TABLE log (
    log_id SERIAL PRIMARY KEY,
    performed_by INT REFERENCES users(user_id) ON DELETE SET NULL,
    performed_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    action VARCHAR(100),
    details TEXT
);

-- Sessions table (tracks logins, supports multi-device)
CREATE TABLE session (
    session_id SERIAL PRIMARY KEY,
    user_id INT REFERENCES users(user_id) ON DELETE CASCADE,
    token VARCHAR(255) UNIQUE NOT NULL, -- JWT or session token
    device_info VARCHAR(255), -- optional info about browser/device
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    expires_at TIMESTAMP NOT NULL,
    revoked BOOLEAN DEFAULT FALSE
);
