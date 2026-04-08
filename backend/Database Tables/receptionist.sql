CREATE TABLE public.receptionist
(
    receptionist_id INTEGER GENERATED ALWAYS AS IDENTITY,

    account_id INTEGER NOT NULL,

    first_name VARCHAR(100) NOT NULL,
    last_name  VARCHAR(100) NOT NULL,
    phone      VARCHAR(20),
    gender     VARCHAR(10),

    CONSTRAINT receptionist_pkey PRIMARY KEY (receptionist_id),
    CONSTRAINT receptionist_account_unique UNIQUE (account_id),
    CONSTRAINT receptionist_account_fkey
        FOREIGN KEY (account_id)
        REFERENCES public.account (account_id)
        ON DELETE CASCADE
);
