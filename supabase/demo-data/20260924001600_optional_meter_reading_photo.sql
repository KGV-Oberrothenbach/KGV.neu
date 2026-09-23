-- Demo only: allow practical meter-reading tests without cloud photo uploads.
INSERT INTO public.app_setting (setting_key, bool_value, updated_at)
VALUES ('meter_reading_photo_required', false, now())
ON CONFLICT (setting_key) DO UPDATE
SET bool_value = EXCLUDED.bool_value,
    updated_at = EXCLUDED.updated_at;
