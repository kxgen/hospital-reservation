CREATE TABLE public.admin
(
    admin_id INTEGER GENERATED ALWAYS AS IDENTITY,

    account_id INTEGER NOT NULL,

    first_name VARCHAR(100) NOT NULL,
    last_name  VARCHAR(100) NOT NULL,
    phone      VARCHAR(20),
    gender     VARCHAR(10),

    CONSTRAINT admin_pkey
        PRIMARY KEY (admin_id),

    CONSTRAINT admin_account_unique
        UNIQUE (account_id),

    CONSTRAINT admin_account_fkey
        FOREIGN KEY (account_id)
        REFERENCES public.account (account_id)
        ON DELETE CASCADE
);
