alter table public.app_user
    add column if not exists permission_grants bigint not null default 0,
    add column if not exists permission_revocations bigint not null default 0;

comment on column public.app_user.permission_grants is 'Zusätzliche benutzerspezifische Fachrechte als Bitmaske über der Rollenbasis.';
comment on column public.app_user.permission_revocations is 'Gezielt entzogene Fachrechte als Bitmaske über der Rollenbasis.';