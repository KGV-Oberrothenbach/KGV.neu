-- Reparaturblock 03: safe account deletion across PostgreSQL and Supabase Auth.
-- No existing account is changed by running this migration.
-- The application must deploy the matching kgv-delete-user code before using this API.
BEGIN;

-- A durable deletion reservation prevents an Auth user from being reattached
-- after the member is unlinked but before the remote Auth API has completed.
CREATE TABLE public.auth_user_delete_lock (
  user_id uuid PRIMARY KEY,
  mitglied_id bigint NOT NULL REFERENCES public.mitglied(id) ON DELETE RESTRICT,
  operation_id uuid NOT NULL UNIQUE,
  requested_by uuid NOT NULL,
  role text NOT NULL,
  permission_grants bigint NOT NULL,
  permission_revocations bigint NOT NULL,
  is_protected boolean NOT NULL,
  app_user_created_at timestamptz NOT NULL,
  app_user_updated_at timestamptz NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);
ALTER TABLE public.auth_user_delete_lock ENABLE ROW LEVEL SECURITY;
REVOKE ALL ON TABLE public.auth_user_delete_lock FROM PUBLIC, anon, authenticated;
GRANT SELECT ON TABLE public.auth_user_delete_lock TO service_role;

CREATE FUNCTION public.guard_pending_auth_user_deletion()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
BEGIN
  IF NEW.auth_user_id IS NULL THEN
    RETURN NEW;
  END IF;
  IF TG_OP = 'UPDATE' THEN
    IF NEW.auth_user_id IS NOT DISTINCT FROM OLD.auth_user_id THEN
      RETURN NEW;
    END IF;
  END IF;

  -- Uses the same per-Auth-user transaction lock as begin_auth_user_deletion.
  -- Serializes concurrent links with a pending deletion reservation.
  PERFORM pg_advisory_xact_lock(hashtextextended(NEW.auth_user_id::text, 0));
  IF EXISTS (
    SELECT 1 FROM public.auth_user_delete_lock d
    WHERE d.user_id = NEW.auth_user_id
  ) THEN
    RAISE EXCEPTION 'Auth account deletion in progress'
      USING ERRCODE = '42501';
  END IF;
  RETURN NEW;
END;
$$;

CREATE TRIGGER trg_guard_pending_auth_user_deletion
BEFORE INSERT OR UPDATE OF auth_user_id ON public.mitglied
FOR EACH ROW EXECUTE FUNCTION public.guard_pending_auth_user_deletion();

REVOKE ALL ON FUNCTION public.guard_pending_auth_user_deletion() FROM PUBLIC, anon, authenticated;

CREATE FUNCTION public.begin_auth_user_deletion(p_user_id uuid, p_mitglied_id bigint, p_operation_id uuid)
RETURNS boolean
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
DECLARE
  v_link uuid;
  v_target public.app_user%rowtype;
  v_effective bigint;
BEGIN
  IF auth.uid() IS NULL OR public.has_effective_permission(512) IS NOT TRUE THEN
    RAISE EXCEPTION 'CanManageRoles required' USING ERRCODE = '42501';
  END IF;
  IF p_user_id IS NULL OR p_operation_id IS NULL OR p_mitglied_id IS NULL OR p_mitglied_id <= 0
     OR p_user_id = auth.uid() THEN
    RAISE EXCEPTION 'Invalid or self deletion target' USING ERRCODE = '42501';
  END IF;

  SELECT m.auth_user_id INTO v_link
  FROM public.mitglied m WHERE m.id = p_mitglied_id FOR UPDATE;
  IF NOT FOUND OR v_link IS DISTINCT FROM p_user_id THEN
    RAISE EXCEPTION 'Member/Auth relationship changed' USING ERRCODE = '42501';
  END IF;

  -- Serialize any concurrent linking operation, including a different member.
  PERFORM pg_advisory_xact_lock(hashtextextended(p_user_id::text, 0));
  IF EXISTS (
    SELECT 1 FROM public.mitglied m
    WHERE m.auth_user_id = p_user_id AND m.id <> p_mitglied_id
  ) THEN
    RAISE EXCEPTION 'Auth user linked to another member' USING ERRCODE = '42501';
  END IF;

  SELECT * INTO v_target
  FROM public.app_user WHERE user_id = p_user_id FOR UPDATE;
  IF NOT FOUND OR v_target.mitglied_id IS DISTINCT FROM p_mitglied_id THEN
    RAISE EXCEPTION 'App user/member relationship changed' USING ERRCODE = '42501';
  END IF;
  IF v_target.is_protected THEN
    RAISE EXCEPTION 'Protected user cannot be deleted' USING ERRCODE = '42501';
  END IF;
  v_effective :=
    (public.permission_role_base(v_target.role) | v_target.permission_grants)
    & ~v_target.permission_revocations;
  IF (v_effective & 512) = 512 THEN
    RAISE EXCEPTION 'Role managers cannot be deleted with this endpoint'
      USING ERRCODE = '42501';
  END IF;

  INSERT INTO public.auth_user_delete_lock
    (user_id, mitglied_id, operation_id, requested_by, role, permission_grants,
     permission_revocations, is_protected, app_user_created_at, app_user_updated_at)
  VALUES
    (p_user_id, p_mitglied_id, p_operation_id, auth.uid(), v_target.role,
     v_target.permission_grants, v_target.permission_revocations,
     v_target.is_protected, v_target.created_at, v_target.updated_at);

  UPDATE public.mitglied
  SET auth_user_id = NULL
  WHERE id = p_mitglied_id AND auth_user_id = p_user_id;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Member link changed during deletion' USING ERRCODE = '42501';
  END IF;
  -- sync_app_user_from_mitglied() deletes the app_user row on unlink.
  IF EXISTS (SELECT 1 FROM public.app_user WHERE user_id = p_user_id) THEN
    RAISE EXCEPTION 'App user was not unlinked' USING ERRCODE = '42501';
  END IF;
  RETURN true;
END;
$$;

CREATE FUNCTION public.restore_auth_user_deletion(p_user_id uuid, p_operation_id uuid)
RETURNS boolean
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, auth, pg_temp
AS $$
DECLARE
  v_lock public.auth_user_delete_lock%rowtype;
BEGIN
  IF auth.role() IS DISTINCT FROM 'service_role' THEN
    RAISE EXCEPTION 'Service role required' USING ERRCODE = '42501';
  END IF;
  SELECT * INTO v_lock
  FROM public.auth_user_delete_lock
  WHERE user_id = p_user_id AND operation_id = p_operation_id FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Deletion reservation not found' USING ERRCODE = 'P0002';
  END IF;
  IF NOT EXISTS (SELECT 1 FROM auth.users WHERE id = p_user_id) THEN
    RAISE EXCEPTION 'Auth user no longer exists; cannot restore'
      USING ERRCODE = '42501';
  END IF;

  -- Clearing and reattaching run in one transaction. Any failure rolls back
  -- the removal of the reservation and keeps the account unrelinkable.
  DELETE FROM public.auth_user_delete_lock WHERE user_id = p_user_id AND operation_id = p_operation_id;
  UPDATE public.mitglied
  SET auth_user_id = p_user_id
  WHERE id = v_lock.mitglied_id AND auth_user_id IS NULL;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Member cannot safely be reattached'
      USING ERRCODE = '42501';
  END IF;
  UPDATE public.app_user
  SET role = v_lock.role,
      permission_grants = v_lock.permission_grants,
      permission_revocations = v_lock.permission_revocations,
      is_protected = v_lock.is_protected,
      created_at = v_lock.app_user_created_at,
      updated_at = v_lock.app_user_updated_at
  WHERE user_id = p_user_id AND mitglied_id = v_lock.mitglied_id;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'App user restoration failed' USING ERRCODE = '42501';
  END IF;
  RETURN true;
END;
$$;

CREATE FUNCTION public.finish_auth_user_deletion(p_user_id uuid, p_operation_id uuid)
RETURNS boolean
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, auth, pg_temp
AS $$
BEGIN
  IF auth.role() IS DISTINCT FROM 'service_role' THEN
    RAISE EXCEPTION 'Service role required' USING ERRCODE = '42501';
  END IF;
  IF EXISTS (SELECT 1 FROM auth.users WHERE id = p_user_id) THEN
    RAISE EXCEPTION 'Auth user still exists; cannot finalize'
      USING ERRCODE = '42501';
  END IF;
  DELETE FROM public.auth_user_delete_lock WHERE user_id = p_user_id AND operation_id = p_operation_id;
  RETURN FOUND;
END;
$$;

-- Legacy direct unlink is not a safe account deletion API. Retire its client
-- grant; do not silently rewrite historical migrations or other functions.
REVOKE ALL ON FUNCTION public.unlink_app_user(uuid) FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.unlink_app_user(uuid) TO service_role;

REVOKE ALL ON FUNCTION public.begin_auth_user_deletion(uuid, bigint, uuid)
  FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.begin_auth_user_deletion(uuid, bigint, uuid)
  TO authenticated;
REVOKE ALL ON FUNCTION public.begin_auth_user_deletion(uuid, bigint, uuid)
  FROM service_role;

REVOKE ALL ON FUNCTION public.restore_auth_user_deletion(uuid, uuid)
  FROM PUBLIC, anon, authenticated;
REVOKE ALL ON FUNCTION public.finish_auth_user_deletion(uuid, uuid)
  FROM PUBLIC, anon, authenticated;
GRANT EXECUTE ON FUNCTION public.restore_auth_user_deletion(uuid, uuid)
  TO service_role;
GRANT EXECUTE ON FUNCTION public.finish_auth_user_deletion(uuid, uuid)
  TO service_role;

COMMENT ON TABLE public.auth_user_delete_lock IS
  'Reparaturblock 03: prevents re-linking an Auth account while its deletion or restoration remains unresolved.';
COMMIT;
