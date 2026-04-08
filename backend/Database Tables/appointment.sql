CREATE TABLE appointment(
    appointment_id integer GENERATED ALWAYS AS IDENTITY NOT NULL,
    patient_id integer NOT NULL,
    time_slot_id integer,
    created_by integer NOT NULL,
    parent_appointment_id integer,
    booked_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
    status varchar(20) NOT NULL DEFAULT 'scheduled'::character varying,
    reason varchar(255),
    doctor_reminder text,
    checked_in_at timestamp with time zone,
    completed_at timestamp with time zone,
    canceled_at timestamp with time zone,
    doctor_id integer,
    start_time timestamp with time zone,
    end_time timestamp with time zone,
    is_confirmed boolean DEFAULT false,
    PRIMARY KEY(appointment_id),
    CONSTRAINT appointment_created_by_fkey FOREIGN key(created_by) REFERENCES "account"(account_id),
    CONSTRAINT appointment_patient_fkey FOREIGN key(patient_id) REFERENCES patient(patient_id),
    CONSTRAINT appointment_parent_fkey FOREIGN key(parent_appointment_id) REFERENCES appointment(appointment_id),
    CONSTRAINT appointment_doctor_fkey FOREIGN key(doctor_id) REFERENCES doctor(doctor_id),
    CONSTRAINT chk_appointment_status CHECK ((status)::text = ANY ((ARRAY['scheduled'::character varying, 'confirmed'::character varying, 'completed'::character varying, 'cancelled'::character varying, 'pending'::character varying, 'no_show'::character varying])::text[]))
);
CREATE INDEX idx_appointment_patient_id ON public.appointment USING btree (patient_id);
CREATE INDEX idx_appointment_doctor_id ON public.appointment USING btree (doctor_id);
CREATE INDEX idx_appointment_times ON public.appointment USING btree (start_time, end_time);
