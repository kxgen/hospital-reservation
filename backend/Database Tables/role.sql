CREATE TABLE public.role
(
    role_id INTEGER GENERATED ALWAYS AS IDENTITY,
    role_name VARCHAR(50) NOT NULL UNIQUE,
    CONSTRAINT role_pkey PRIMARY KEY (role_id)
);
