DO $$
BEGIN
    CREATE TYPE public.ablesung_pruefstatus AS ENUM ('eingereicht', 'freigegeben', 'abgelehnt');
EXCEPTION
    WHEN duplicate_object THEN NULL;
END $$;

ALTER TABLE public.zaehler_ablesung
    ADD COLUMN IF NOT EXISTS pruefstatus public.ablesung_pruefstatus,
    ADD COLUMN IF NOT EXISTS pruefkommentar text,
    ADD COLUMN IF NOT EXISTS geprueft_von bigint,
    ADD COLUMN IF NOT EXISTS geprueft_am timestamp without time zone;

UPDATE public.zaehler_ablesung
SET pruefstatus = CASE
    WHEN freigegeben THEN 'freigegeben'::public.ablesung_pruefstatus
    ELSE 'eingereicht'::public.ablesung_pruefstatus
END
WHERE pruefstatus IS NULL;

ALTER TABLE public.zaehler_ablesung
    ALTER COLUMN pruefstatus SET DEFAULT 'eingereicht'::public.ablesung_pruefstatus,
    ALTER COLUMN pruefstatus SET NOT NULL;

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'zaehler_ablesung_geprueft_von_fkey') THEN
        ALTER TABLE public.zaehler_ablesung
            ADD CONSTRAINT zaehler_ablesung_geprueft_von_fkey
            FOREIGN KEY (geprueft_von) REFERENCES public.mitglied(id);
    END IF;
END $$;

CREATE TABLE IF NOT EXISTS public.app_setting (
    setting_key text PRIMARY KEY,
    bool_value boolean NOT NULL DEFAULT false,
    updated_at timestamp with time zone NOT NULL DEFAULT now()
);

INSERT INTO public.app_setting (setting_key, bool_value)
VALUES ('allow_user_meter_reading_submissions', false)
ON CONFLICT (setting_key) DO NOTHING;

GRANT SELECT, INSERT, UPDATE ON TABLE public.app_setting TO authenticated;
GRANT ALL ON TABLE public.app_setting TO service_role;

ALTER TABLE public.app_setting ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS app_setting_read_authenticated ON public.app_setting;
CREATE POLICY app_setting_read_authenticated
    ON public.app_setting
    FOR SELECT
    TO authenticated
    USING (true);

DROP POLICY IF EXISTS app_setting_write_internal ON public.app_setting;
CREATE POLICY app_setting_write_internal
    ON public.app_setting
    FOR INSERT
    TO authenticated
    WITH CHECK (public.is_admin_or_vorstand());

DROP POLICY IF EXISTS app_setting_update_internal ON public.app_setting;
CREATE POLICY app_setting_update_internal
    ON public.app_setting
    FOR UPDATE
    TO authenticated
    USING (public.is_admin_or_vorstand())
    WITH CHECK (public.is_admin_or_vorstand());
