DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='appointment' AND column_name='doctor_id') THEN
        ALTER TABLE public.appointment ADD COLUMN doctor_id INTEGER;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='appointment' AND column_name='start_time') THEN
        ALTER TABLE public.appointment ADD COLUMN start_time TIMESTAMPTZ;
    END IF;
END $$;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_name='appointment' AND column_name='end_time') THEN
        ALTER TABLE public.appointment ADD COLUMN end_time TIMESTAMPTZ;
    END IF;
END $$;

UPDATE public.appointment a
SET 
    doctor_id = ts.doctor_id,
    start_time = ts.start_time,
    end_time = ts.end_time
FROM public.time_slot ts
WHERE a.time_slot_id = ts.slot_id
AND a.start_time IS NULL;

-- Make time_slot_id nullable
ALTER TABLE public.appointment ALTER COLUMN time_slot_id DROP NOT NULL;

-- Add Foreign Key for doctor_id (drop if exists to avoid dupes or use DO block)
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name='appointment_doctor_fkey') THEN
        ALTER TABLE public.appointment
            ADD CONSTRAINT appointment_doctor_fkey
            FOREIGN KEY (doctor_id)
            REFERENCES public.doctor (doctor_id)
            ON DELETE CASCADE;
    END IF;
END $$;
