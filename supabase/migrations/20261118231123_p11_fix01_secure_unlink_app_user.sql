-- P11 FIX-01 – harden the existing caller-bound unlink RPC.
-- This migration does not delete or modify any accounts by itself.
-- Each invocation locks the target app_user row and rejects any account
-- with *effective* CanManageRoles. This blocks races with concurrent
-- update_app_user_access calls (which lock the same target row).
BEGIN;

CREATE OR REPLACE FUNCTION public.unlink_app_user(p_user_id uuid)
RETURNS void
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public, pg_temp
AS $$
DECLARE
  v_target public.app_user%rowtype;
  v_effective bigint;
BEGIN
  IF auth.uid() IS NULL OR NOT public.has_effective_permission(512) THEN
    RAISE EXCEPTION 'CanManageRoles required' USING ERRCODE = '42501';
  END IF;
  IF p_user_id IS NULL OR p_user_id = auth.uid() THEN
    RAISE EXCEPTION 'Own account cannot be unlinked here' USING ERRCODE = '42501';
  END IF;

  SELECT * INTO v_target
  FROM public.app_user
  WHERE user_id = p_user_id
  FOR UPDATE;
  IF NOT FOUND THEN
    RAISE EXCEPTION 'Target app_user not found' USING ERRCODE = 'P0002';
  END IF;
  IF v_target.is_protected THEN
    RAISE EXCEPTION 'Protected app_user cannot be unlinked' USING ERRCODE = '42501';
  END IF;

  -- Fail closed: privileged accounts cannot be deleted by this endpoint.
  -- It guarantees that the last managing user cannot be removed here.
  v_effective :=
    (public.permission_role_base(v_target.role) | COALESCE(v_target.permission_grants, 0))
    & ~COALESCE(v_target.permission_revocations, 0);
  IF (v_effective & 512) = 512 THEN
    RAISE EXCEPTION 'Accounts with CanManageRoles require separate administration'
      USING ERRCODE = '42501';
  END IF;

  UPDATE public.mitglied
  SET auth_user_id = NULL
  WHERE auth_user_id = v_target.user_id;

  DELETE FROM public.app_user WHERE user_id = v_target.user_id;
END;
$$;

REVOKE ALL ON FUNCTION public.unlink_app_user(uuid) FROM PUBLIC, anon;
GRANT EXECUTE ON FUNCTION public.unlink_app_user(uuid) TO authenticated, service_role;

COMMIT;
