CREATE TABLE public.doctor_availability
(
    availability_id INTEGER GENERATED ALWAYS AS IDENTITY,
    doctor_id INTEGER NOT NULL,
    day_of_week VARCHAR(10) NOT NULL CHECK (day_of_week IN ('monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday', 'sunday')),
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    
    CONSTRAINT doctor_availability_pkey PRIMARY KEY (availability_id),
    CONSTRAINT doctor_availability_doctor_fkey FOREIGN KEY (doctor_id)
        REFERENCES public.doctor(doctor_id)
        ON DELETE CASCADE
);

CREATE INDEX idx_doctor_availability_doctor ON public.doctor_availability(doctor_id);
