CREATE TABLE public.doctor_unavailability
(
    unavailability_id INTEGER GENERATED ALWAYS AS IDENTITY,
    doctor_id INTEGER NOT NULL,
    start_time TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    end_time TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    reason VARCHAR(255),
    created_at TIMESTAMP WITHOUT TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT doctor_unavailability_pkey PRIMARY KEY (unavailability_id),
    CONSTRAINT doctor_unavailability_doctor_fkey FOREIGN KEY (doctor_id)
        REFERENCES public.doctor(doctor_id)
        ON DELETE CASCADE
);

CREATE INDEX idx_doctor_unavailability_doctor ON public.doctor_unavailability(doctor_id);
CREATE INDEX idx_doctor_unavailability_times ON public.doctor_unavailability(start_time, end_time);
