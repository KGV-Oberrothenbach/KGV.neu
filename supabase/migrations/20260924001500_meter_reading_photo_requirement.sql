-- This migration is self-contained because early installations may not have the
-- originally planned app_setting table yet.
CREATE TABLE IF NOT EXISTS public.app_setting (
    setting_key text PRIMARY KEY,
    bool_value boolean NOT NULL DEFAULT false,
    updated_at timestamp with time zone NOT NULL DEFAULT now()
);

GRANT SELECT, INSERT, UPDATE ON TABLE public.app_setting TO authenticated;
GRANT ALL ON TABLE public.app_setting TO service_role;

ALTER TABLE public.app_setting ENABLE ROW LEVEL SECURITY;

DROP POLICY IF EXISTS app_setting_read_authenticated ON public.app_setting;
CREATE POLICY app_setting_read_authenticated
    ON public.app_setting FOR SELECT TO authenticated USING (true);

DROP POLICY IF EXISTS app_setting_write_internal ON public.app_setting;
CREATE POLICY app_setting_write_internal
    ON public.app_setting FOR INSERT TO authenticated
    WITH CHECK (public.is_admin_or_vorstand());

DROP POLICY IF EXISTS app_setting_update_internal ON public.app_setting;
CREATE POLICY app_setting_update_internal
    ON public.app_setting FOR UPDATE TO authenticated
    USING (public.is_admin_or_vorstand())
    WITH CHECK (public.is_admin_or_vorstand());

-- Photos remain mandatory by default. Individual non-production environments can opt out via app_setting.
INSERT INTO public.app_setting (setting_key, bool_value)
VALUES ('meter_reading_photo_required', true)
ON CONFLICT (setting_key) DO NOTHING;
