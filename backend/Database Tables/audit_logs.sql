CREATE TABLE public.audit_logs
(
    id INTEGER GENERATED ALWAYS AS IDENTITY,
    
    account_id INTEGER,
    action VARCHAR(50) NOT NULL,
    target_table VARCHAR(50) NOT NULL,
    target_id INTEGER NOT NULL,
    details TEXT,
    created_at TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT audit_logs_pkey PRIMARY KEY (id),
    CONSTRAINT audit_logs_account_fkey
        FOREIGN KEY (account_id)
        REFERENCES public.account (account_id)
        ON DELETE SET NULL
);

-- Indexes for faster lookup
CREATE INDEX IF NOT EXISTS idx_audit_logs_target
    ON public.audit_logs (target_table, target_id);

CREATE INDEX IF NOT EXISTS idx_audit_logs_account
    ON public.audit_logs (account_id);
