CREATE TABLE public.patient
(
    patient_id INTEGER GENERATED ALWAYS AS IDENTITY,

    account_id INTEGER,

    first_name VARCHAR(100),
    last_name  VARCHAR(100),
    phone      VARCHAR(20),
    gender     VARCHAR(10),
    date_of_birth DATE,

    CONSTRAINT patient_pkey PRIMARY KEY (patient_id),
    CONSTRAINT patient_account_unique UNIQUE (account_id),
    CONSTRAINT patient_account_fkey
        FOREIGN KEY (account_id)
        REFERENCES public.account (account_id)
        ON DELETE CASCADE
);
