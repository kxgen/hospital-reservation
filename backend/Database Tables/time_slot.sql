
CREATE TABLE public.time_slot
(
    slot_id INTEGER GENERATED ALWAYS AS IDENTITY,
    doctor_id INTEGER NOT NULL,
    start_time TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    end_time   TIMESTAMP WITHOUT TIME ZONE NOT NULL,
    is_available BOOLEAN NOT NULL DEFAULT true,

    CONSTRAINT slot_pkey PRIMARY KEY (slot_id),

    CONSTRAINT slot_fkey
        FOREIGN KEY (doctor_id)
        REFERENCES public.doctor (doctor_id)
        ON DELETE CASCADE,

    CONSTRAINT prevent_overlapping_slots
        EXCLUDE USING gist (
            doctor_id WITH =,
            tsrange(start_time, end_time) WITH &&
        )
);