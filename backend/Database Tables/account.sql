CREATE TABLE public.account
(
    account_id INTEGER GENERATED ALWAYS AS IDENTITY,
    email VARCHAR(150) NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    role_id INTEGER NOT NULL,
    is_active BOOLEAN NOT NULL DEFAULT true,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT account_pkey PRIMARY KEY (account_id),
    CONSTRAINT account_email_key UNIQUE (email),
    CONSTRAINT account_role_fkey FOREIGN KEY (role_id)
        REFERENCES public.role(role_id)
        ON DELETE RESTRICT
);
