CREATE TABLE public.specialty
(
    specialty_id INTEGER GENERATED ALWAYS AS IDENTITY,

    specialty_name VARCHAR(100) NOT NULL,

    CONSTRAINT specialty_pkey PRIMARY KEY (specialty_id),
    CONSTRAINT specialty_name_unique UNIQUE (specialty_name)
);
