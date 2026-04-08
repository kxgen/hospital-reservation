CREATE TABLE public.doctor
(
    doctor_id INTEGER GENERATED ALWAYS AS IDENTITY,
    account_id INTEGER NOT NULL,

    first_name VARCHAR(100) NOT NULL,
    last_name  VARCHAR(100) NOT NULL,
    phone      VARCHAR(20),
    gender     VARCHAR(10),

    specialty_id INTEGER NOT NULL DEFAULT 1,
    bio          TEXT,
    photo_url    VARCHAR(255),

    CONSTRAINT doctor_pkey PRIMARY KEY (doctor_id),
    CONSTRAINT doctor_account_unique UNIQUE (account_id),
    CONSTRAINT doctor_account_fkey
        FOREIGN KEY (account_id)
        REFERENCES public.account (account_id)
        ON DELETE CASCADE,
    CONSTRAINT doctor_specialty_fkey
        FOREIGN KEY (specialty_id)
        REFERENCES public.specialty (specialty_id)
        ON UPDATE CASCADE
        ON DELETE RESTRICT
);
