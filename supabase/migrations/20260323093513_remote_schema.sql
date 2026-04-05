


SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;


COMMENT ON SCHEMA "public" IS 'standard public schema';



CREATE EXTENSION IF NOT EXISTS "pg_graphql" WITH SCHEMA "graphql";






CREATE EXTENSION IF NOT EXISTS "pg_stat_statements" WITH SCHEMA "extensions";






CREATE EXTENSION IF NOT EXISTS "pgcrypto" WITH SCHEMA "extensions";






CREATE EXTENSION IF NOT EXISTS "supabase_vault" WITH SCHEMA "vault";






CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA "extensions";






CREATE TYPE "public"."ablesung_art" AS ENUM (
    'normal',
    'ausbau'
);


ALTER TYPE "public"."ablesung_art" OWNER TO "postgres";


CREATE TYPE "public"."arbeitseinsatz_anmeldung_status" AS ENUM (
    'angemeldet',
    'abgesagt',
    'teilgenommen',
    'nicht_erschienen'
);


ALTER TYPE "public"."arbeitseinsatz_anmeldung_status" OWNER TO "postgres";


CREATE TYPE "public"."zaehler_medium" AS ENUM (
    'wasser',
    'strom'
);


ALTER TYPE "public"."zaehler_medium" OWNER TO "postgres";


CREATE TYPE "public"."zaehler_status" AS ENUM (
    'aktiv',
    'ausgebaut'
);


ALTER TYPE "public"."zaehler_status" OWNER TO "postgres";

SET default_tablespace = '';

SET default_table_access_method = "heap";


CREATE TABLE IF NOT EXISTS "public"."parzelle" (
    "id" bigint NOT NULL,
    "garten_nr" "text" NOT NULL,
    "flaeche_qm" numeric,
    "hat_wasser" boolean DEFAULT false NOT NULL,
    "hat_strom" boolean DEFAULT false NOT NULL,
    "rfid_wasser" "text",
    "rfid_strom" "text",
    "Anlage" "text" NOT NULL,
    "aktiv" boolean DEFAULT true NOT NULL,
    "is_demo" boolean DEFAULT false NOT NULL
);


ALTER TABLE "public"."parzelle" OWNER TO "postgres";


COMMENT ON COLUMN "public"."parzelle"."is_demo" IS 'Kennzeichnet Demo-/Test-Parzellen. Diese Datensätze müssen aus fachlichen Berechnungen und Auswertungen ausgeschlossen werden.';



CREATE OR REPLACE FUNCTION "public"."assign_parzelle_rfid"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_rfid_tag_uid" "text") RETURNS "public"."parzelle"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_conflict_parzelle_id bigint;
  v_row public.parzelle;
begin
  if p_rfid_tag_uid is null or btrim(p_rfid_tag_uid) = '' then
    raise exception 'RFID darf nicht leer sein.';
  end if;

  if p_medium = 'wasser' then
    select p.id
      into v_conflict_parzelle_id
    from public.parzelle p
    where p.rfid_wasser = btrim(p_rfid_tag_uid)
      and p.id <> p_parzelle_id
    limit 1;

    if v_conflict_parzelle_id is not null then
      raise exception 'RFID % ist bereits bei Parzelle % für Wasser hinterlegt.',
        p_rfid_tag_uid, v_conflict_parzelle_id;
    end if;

    update public.parzelle
       set rfid_wasser = btrim(p_rfid_tag_uid)
     where id = p_parzelle_id
     returning * into v_row;
  else
    select p.id
      into v_conflict_parzelle_id
    from public.parzelle p
    where p.rfid_strom = btrim(p_rfid_tag_uid)
      and p.id <> p_parzelle_id
    limit 1;

    if v_conflict_parzelle_id is not null then
      raise exception 'RFID % ist bereits bei Parzelle % für Strom hinterlegt.',
        p_rfid_tag_uid, v_conflict_parzelle_id;
    end if;

    update public.parzelle
       set rfid_strom = btrim(p_rfid_tag_uid)
     where id = p_parzelle_id
     returning * into v_row;
  end if;

  if v_row.id is null then
    raise exception 'Parzelle % wurde nicht gefunden.', p_parzelle_id;
  end if;

  return v_row;
end;
$$;


ALTER FUNCTION "public"."assign_parzelle_rfid"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_rfid_tag_uid" "text") OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."before_user_created_allowlist"("event" "jsonb") RETURNS "jsonb"
    LANGUAGE "plpgsql" SECURITY DEFINER
    SET "search_path" TO 'public'
    AS $$
declare
    v_email text := lower(coalesce(event->'user'->>'email', ''));
    v_provider text := lower(coalesce(event->'user'->'app_metadata'->>'provider', ''));
    v_allowed boolean := false;
begin
    if v_email = '' then
        return jsonb_build_object(
            'error',
            jsonb_build_object(
                'http_code', 400,
                'message', 'Email address is required.'
            )
        );
    end if;

    select case
        when v_provider = 'google' then allow_google
        when v_provider = 'email' then allow_email_otp
        else false
    end
    into v_allowed
    from public.auth_allowlist
    where lower(email) = v_email
      and is_active = true;

    if coalesce(v_allowed, false) then
        return '{}'::jsonb;
    end if;

    return jsonb_build_object(
        'error',
        jsonb_build_object(
            'http_code', 403,
            'message', 'This email address is not enabled for this sign-in method.'
        )
    );
end;
$$;


ALTER FUNCTION "public"."before_user_created_allowlist"("event" "jsonb") OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."calc_eichfaellig_am"("p_medium" "public"."zaehler_medium", "p_eichdatum" "date") RETURNS "date"
    LANGUAGE "plpgsql" IMMUTABLE
    AS $$
begin
  if p_eichdatum is null then
    return null;
  end if;

  case p_medium
    when 'wasser' then
      return (p_eichdatum + interval '6 years')::date;
    when 'strom' then
      return (p_eichdatum + interval '6 years')::date;
    else
      return null;
  end case;
end;
$$;


ALTER FUNCTION "public"."calc_eichfaellig_am"("p_medium" "public"."zaehler_medium", "p_eichdatum" "date") OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."can_access_demo_scope"() RETURNS boolean
    LANGUAGE "sql" STABLE SECURITY DEFINER
    SET "search_path" TO 'public', 'pg_temp'
    AS $$
    select public.is_admin_or_vorstand()
        or public.is_demo_user()
        or public.is_playstore_reviewer();
$$;


ALTER FUNCTION "public"."can_access_demo_scope"() OWNER TO "postgres";


COMMENT ON FUNCTION "public"."can_access_demo_scope"() IS 'TRUE für interne Rollen und Demo-/Reviewer-Konten; Grundlage für spätere Demo-Policies.';



CREATE OR REPLACE FUNCTION "public"."can_access_live_internal_data"() RETURNS boolean
    LANGUAGE "sql" STABLE SECURITY DEFINER
    SET "search_path" TO 'public', 'pg_temp'
    AS $$
    select public.is_admin_or_vorstand();
$$;


ALTER FUNCTION "public"."can_access_live_internal_data"() OWNER TO "postgres";


COMMENT ON FUNCTION "public"."can_access_live_internal_data"() IS 'TRUE nur für interne privilegierte Rollen; für Policies auf echte Vereinsdaten.';



CREATE TABLE IF NOT EXISTS "public"."zaehler" (
    "id" bigint NOT NULL,
    "parzelle_id" bigint NOT NULL,
    "medium" "public"."zaehler_medium" NOT NULL,
    "zaehlernummer" "text" NOT NULL,
    "eichdatum" "date" NOT NULL,
    "eichfaellig_am" "date" NOT NULL,
    "eingebaut_am" "date" DEFAULT CURRENT_DATE NOT NULL,
    "ausgebaut_am" "date",
    "status" "public"."zaehler_status" DEFAULT 'aktiv'::"public"."zaehler_status" NOT NULL,
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    CONSTRAINT "ck_zaehler_dates" CHECK ((("ausgebaut_am" IS NULL) OR ("ausgebaut_am" >= "eingebaut_am"))),
    CONSTRAINT "ck_zaehlernummer_not_blank" CHECK (("btrim"("zaehlernummer") <> ''::"text"))
);


ALTER TABLE "public"."zaehler" OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."create_meter_installation"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_zaehlernummer" "text", "p_eichdatum" "date", "p_eingebaut_am" "date" DEFAULT CURRENT_DATE) RETURNS "public"."zaehler"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_exists bigint;
  v_rfid text;
  v_row public.zaehler;
begin
  if p_zaehlernummer is null or btrim(p_zaehlernummer) = '' then
    raise exception 'Zählernummer darf nicht leer sein.';
  end if;

  -- Prüfen, ob für Parzelle+Medium bereits ein aktiver Zähler existiert
  select z.id
    into v_exists
  from public.zaehler z
  where z.parzelle_id = p_parzelle_id
    and z.medium = p_medium
    and z.status = 'aktiv'
    and z.ausgebaut_am is null
  limit 1;

  if v_exists is not null then
    raise exception 'Für Parzelle % und Medium % existiert bereits ein aktiver Zähler (%).',
      p_parzelle_id, p_medium, v_exists;
  end if;

  -- Passende RFID an der Parzelle prüfen
  if p_medium = 'wasser' then
    select p.rfid_wasser into v_rfid
    from public.parzelle p
    where p.id = p_parzelle_id;
  else
    select p.rfid_strom into v_rfid
    from public.parzelle p
    where p.id = p_parzelle_id;
  end if;

  if v_rfid is null or btrim(v_rfid) = '' then
    raise exception 'Vor Einbau muss für Parzelle % und Medium % eine RFID an der Parzelle hinterlegt sein.',
      p_parzelle_id, p_medium;
  end if;

  insert into public.zaehler (
    parzelle_id,
    medium,
    zaehlernummer,
    eichdatum,
    eingebaut_am,
    status
  )
  values (
    p_parzelle_id,
    p_medium,
    btrim(p_zaehlernummer),
    p_eichdatum,
    coalesce(p_eingebaut_am, current_date),
    'aktiv'
  )
  returning * into v_row;

  return v_row;
end;
$$;


ALTER FUNCTION "public"."create_meter_installation"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_zaehlernummer" "text", "p_eichdatum" "date", "p_eingebaut_am" "date") OWNER TO "postgres";


CREATE TABLE IF NOT EXISTS "public"."zaehler_ablesung" (
    "id" bigint NOT NULL,
    "zaehler_id" bigint NOT NULL,
    "ablesedatum" timestamp without time zone DEFAULT "now"() NOT NULL,
    "stand" numeric NOT NULL,
    "foto_pfad" "text",
    "freigegeben" boolean DEFAULT false NOT NULL,
    "art" "public"."ablesung_art" DEFAULT 'normal'::"public"."ablesung_art" NOT NULL,
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    CONSTRAINT "ck_zaehler_ablesung_stand_nonnegative" CHECK (("stand" >= (0)::numeric))
);


ALTER TABLE "public"."zaehler_ablesung" OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."create_meter_reading"("p_zaehler_id" bigint, "p_stand" numeric, "p_ablesedatum" timestamp without time zone DEFAULT "now"(), "p_foto_pfad" "text" DEFAULT NULL::"text") RETURNS "public"."zaehler_ablesung"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_zaehler public.zaehler;
  v_last_stand numeric;
  v_row public.zaehler_ablesung;
begin
  select *
    into v_zaehler
  from public.zaehler
  where id = p_zaehler_id;

  if v_zaehler.id is null then
    raise exception 'Zähler % wurde nicht gefunden.', p_zaehler_id;
  end if;

  if v_zaehler.status <> 'aktiv' or v_zaehler.ausgebaut_am is not null then
    raise exception 'Für Zähler % können nur im aktiven Zustand normale Ablesungen erfasst werden.', p_zaehler_id;
  end if;

  select a.stand
    into v_last_stand
  from public.zaehler_ablesung a
  where a.zaehler_id = p_zaehler_id
  order by a.ablesedatum desc
  limit 1;

  if v_last_stand is not null and p_stand < v_last_stand then
    raise exception 'Stand (%) darf nicht kleiner als letzter bekannter Stand (%) sein.',
      p_stand, v_last_stand;
  end if;

  insert into public.zaehler_ablesung (
    zaehler_id,
    ablesedatum,
    stand,
    foto_pfad,
    freigegeben,
    art
  )
  values (
    p_zaehler_id,
    coalesce(p_ablesedatum, now()),
    p_stand,
    p_foto_pfad,
    false,
    'normal'
  )
  returning * into v_row;

  return v_row;
end;
$$;


ALTER FUNCTION "public"."create_meter_reading"("p_zaehler_id" bigint, "p_stand" numeric, "p_ablesedatum" timestamp without time zone, "p_foto_pfad" "text") OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."current_app_role"() RETURNS "text"
    LANGUAGE "sql" STABLE SECURITY DEFINER
    SET "search_path" TO 'public', 'pg_temp'
    AS $$
    select coalesce(
        (
            select au.role
            from public.app_user au
            where au.user_id = auth.uid()
            limit 1
        ),
        case
            when auth.uid() is not null then 'user'
            else 'anon'
        end
    );
$$;


ALTER FUNCTION "public"."current_app_role"() OWNER TO "postgres";


COMMENT ON FUNCTION "public"."current_app_role"() IS 'Liefert die App-Rolle des aktuellen Users aus public.app_user oder anon/user als Fallback.';



CREATE OR REPLACE FUNCTION "public"."current_mitglied_id"() RETURNS integer
    LANGUAGE "sql" STABLE SECURITY DEFINER
    SET "search_path" TO 'public', 'pg_temp'
    AS $$
            select au.mitglied_id::integer
            from public.app_user au
            where au.user_id = auth.uid()
            limit 1;
        $$;


ALTER FUNCTION "public"."current_mitglied_id"() OWNER TO "postgres";


COMMENT ON FUNCTION "public"."current_mitglied_id"() IS 'Kanonischer Helfer: liefert die mit dem aktuellen User verknüpfte mitglied_id.';



CREATE OR REPLACE FUNCTION "public"."current_user_email"() RETURNS "text"
    LANGUAGE "sql" STABLE SECURITY DEFINER
    SET "search_path" TO 'public', 'pg_temp'
    AS $$
    select nullif(lower(coalesce(auth.jwt() ->> 'email', '')), '');
$$;


ALTER FUNCTION "public"."current_user_email"() OWNER TO "postgres";


COMMENT ON FUNCTION "public"."current_user_email"() IS 'Liest die aktuelle Benutzer-E-Mail aus dem JWT. Grundlage für Allowlist-/Demo-Prüfungen.';



CREATE OR REPLACE FUNCTION "public"."find_scan_context"("p_rfid_tag_uid" "text") RETURNS TABLE("parzelle_id" bigint, "anlage" "text", "garten_nr" "text", "medium" "public"."zaehler_medium", "rfid_tag_uid" "text", "aktiver_zaehler_id" bigint, "zaehlernummer" "text", "eichdatum" "date", "eichfaellig_am" "date", "eingebaut_am" "date", "ausgebaut_am" "date", "status" "public"."zaehler_status")
    LANGUAGE "sql" STABLE
    AS $$
  select
    v.parzelle_id,
    v.anlage,
    v.garten_nr,
    v.medium,
    v.rfid_tag_uid,
    v.aktiver_zaehler_id,
    v.zaehlernummer,
    v.eichdatum,
    v.eichfaellig_am,
    v.eingebaut_am,
    v.ausgebaut_am,
    v.status
  from public.v_rfid_scan_context v
  where v.rfid_tag_uid = p_rfid_tag_uid;
$$;


ALTER FUNCTION "public"."find_scan_context"("p_rfid_tag_uid" "text") OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."fn_berechne_pflichtstunden_status"("p_mitglied_id" bigint, "p_saison_id" bigint) RETURNS TABLE("hauptmitglied_id" bigint, "saison_id" bigint, "saison_jahr" integer, "regelgrund" "text", "ist_befreit" boolean, "hat_wartungsvertrag" boolean, "altersbefreit" boolean, "eintritt_im_saisonjahr" boolean, "eintritt_zweites_halbjahr" boolean, "pflichtstunden_soll" numeric, "geleistete_stunden" numeric, "offene_stunden" numeric, "euro_pro_fehlstunde" numeric, "fehlbetrag" numeric)
    LANGUAGE "plpgsql" STABLE
    AS $$
declare
    v_hauptmitglied_id bigint;
    v_mitglied record;
    v_saison record;
    v_saison_start date;
    v_saison_ende date;
    v_regelgrund text := 'standard';
    v_ist_befreit boolean := false;
    v_hat_wartungsvertrag boolean := false;
    v_altersbefreit boolean := false;
    v_eintritt_im_saisonjahr boolean := false;
    v_eintritt_zweites_halbjahr boolean := false;
    v_pflichtstunden_soll numeric := 0;
    v_geleistete_stunden numeric := 0;
begin
    v_hauptmitglied_id := public.get_hauptmitglied_id(p_mitglied_id);

    if v_hauptmitglied_id is null then
        raise exception 'Mitglied % nicht gefunden.', p_mitglied_id;
    end if;

    select *
      into v_mitglied
    from public.mitglied
    where id = v_hauptmitglied_id;

    select *
      into v_saison
    from public.saison
    where id = p_saison_id;

    if not found then
        raise exception 'Saison % nicht gefunden.', p_saison_id;
    end if;

    if v_saison.jahr is null then
        raise exception 'Saison % hat kein Jahr gesetzt.', p_saison_id;
    end if;

    v_saison_start := make_date(v_saison.jahr, 1, 1);
    v_saison_ende  := make_date(v_saison.jahr, 12, 31);

    v_pflichtstunden_soll := coalesce(v_saison.pflichtstunden_soll, 0);

    -- Eintrittsregeln
    v_eintritt_im_saisonjahr :=
        extract(year from v_mitglied.mitglied_seit)::int = v_saison.jahr;

    v_eintritt_zweites_halbjahr :=
        v_eintritt_im_saisonjahr
        and v_mitglied.mitglied_seit >= make_date(v_saison.jahr, 7, 1);

    -- Mitglied in dieser Saison überhaupt aktiv?
    if v_mitglied.mitglied_seit > v_saison_ende
       or (v_mitglied.mitglied_ende is not null and v_mitglied.mitglied_ende < v_saison_start)
    then
        v_pflichtstunden_soll := 0;
        v_ist_befreit := true;
        v_regelgrund := 'keine_aktive_mitgliedschaft';
    else
        -- Altersbefreiung
        if v_mitglied.geburtsdatum is not null then
            v_altersbefreit :=
                case v_mitglied.arbeitsstunden_altersregel_typ
                    when 'frau75' then extract(year from v_mitglied.geburtsdatum)::int <= v_saison.jahr - 75
                    when 'mann80' then extract(year from v_mitglied.geburtsdatum)::int <= v_saison.jahr - 80
                    else false
                end;
        else
            v_altersbefreit := false;
        end if;

        -- Wartungsvertrag aktiv in Saison?
        select exists (
            select 1
            from public.wartungsvertrag_zuordnungen z
            join public.wartungsvertraege w
              on w.id = z.wartungsvertrag_id
            where z.hauptmitglied_id = v_hauptmitglied_id
              and w.aktiv = true
              and w.befreit_von_pflichtstunden = true
              and z.gueltig_ab <= v_saison_ende
              and (z.gueltig_bis is null or z.gueltig_bis >= v_saison_start)
        )
        into v_hat_wartungsvertrag;

        if v_hat_wartungsvertrag then
            v_pflichtstunden_soll := 0;
            v_ist_befreit := true;
            v_regelgrund := 'wartungsvertrag';
        elsif v_altersbefreit then
            v_pflichtstunden_soll := 0;
            v_ist_befreit := true;
            v_regelgrund := 'altersbefreiung';
        elsif v_eintritt_zweites_halbjahr then
            v_pflichtstunden_soll := round(v_pflichtstunden_soll / 2.0, 2);
            v_regelgrund := 'eintritt_2_halbjahr';
        else
            v_regelgrund := 'standard';
        end if;
    end if;

    -- geleistete Stunden: Hauptmitglied + zugehörige Nebenmitglieder
    select coalesce(sum(a.stunden), 0)
      into v_geleistete_stunden
    from public.arbeitsstunde a
    join public.mitglied m
      on m.id = a.mitglied_id
    where a.saison_id = p_saison_id
      and a.freigegeben = true
      and coalesce(m.hauptmitglied_id, m.id) = v_hauptmitglied_id;

    return query
    select
        v_hauptmitglied_id,
        p_saison_id,
        v_saison.jahr,
        v_regelgrund,
        v_ist_befreit,
        v_hat_wartungsvertrag,
        v_altersbefreit,
        v_eintritt_im_saisonjahr,
        v_eintritt_zweites_halbjahr,
        v_pflichtstunden_soll,
        v_geleistete_stunden,
        greatest(v_pflichtstunden_soll - v_geleistete_stunden, 0),
        coalesce(v_saison.euro_pro_fehlstunde, 0),
        greatest(v_pflichtstunden_soll - v_geleistete_stunden, 0) * coalesce(v_saison.euro_pro_fehlstunde, 0);
end;
$$;


ALTER FUNCTION "public"."fn_berechne_pflichtstunden_status"("p_mitglied_id" bigint, "p_saison_id" bigint) OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."get_active_meter"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium") RETURNS "public"."zaehler"
    LANGUAGE "sql" STABLE
    AS $$
  select z.*
  from public.zaehler z
  where z.parzelle_id = p_parzelle_id
    and z.medium = p_medium
    and z.status = 'aktiv'
    and z.ausgebaut_am is null
  limit 1;
$$;


ALTER FUNCTION "public"."get_active_meter"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium") OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."get_hauptmitglied_id"("p_mitglied_id" bigint) RETURNS bigint
    LANGUAGE "sql" STABLE
    AS $$
    select case
             when m.hauptmitglied_id is null then m.id
             else m.hauptmitglied_id
           end
    from public.mitglied m
    where m.id = p_mitglied_id
$$;


ALTER FUNCTION "public"."get_hauptmitglied_id"("p_mitglied_id" bigint) OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."get_user_role"() RETURNS "text"
    LANGUAGE "sql" STABLE
    AS $$
  select public.current_app_role()
$$;


ALTER FUNCTION "public"."get_user_role"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."is_admin"() RETURNS boolean
    LANGUAGE "sql" STABLE SECURITY DEFINER
    SET "search_path" TO 'public', 'pg_temp'
    AS $$
    select public.current_app_role() = 'admin';
$$;


ALTER FUNCTION "public"."is_admin"() OWNER TO "postgres";


COMMENT ON FUNCTION "public"."is_admin"() IS 'TRUE für App-Rolle admin.';



CREATE OR REPLACE FUNCTION "public"."is_admin_or_vorstand"() RETURNS boolean
    LANGUAGE "sql" STABLE SECURITY DEFINER
    SET "search_path" TO 'public', 'pg_temp'
    AS $$
    select public.current_app_role() = any (array['admin', 'vorstand']);
$$;


ALTER FUNCTION "public"."is_admin_or_vorstand"() OWNER TO "postgres";


COMMENT ON FUNCTION "public"."is_admin_or_vorstand"() IS 'Kanonischer Rollen-Helfer: TRUE für admin oder vorstand.';



CREATE OR REPLACE FUNCTION "public"."is_demo_user"() RETURNS boolean
    LANGUAGE "sql" STABLE SECURITY DEFINER
    SET "search_path" TO 'public', 'pg_temp'
    AS $$
    select exists (
        select 1
        from public.auth_allowlist al
        where lower(al.email) = public.current_user_email()
          and al.allow_demo_access = true
    );
$$;


ALTER FUNCTION "public"."is_demo_user"() OWNER TO "postgres";


COMMENT ON FUNCTION "public"."is_demo_user"() IS 'TRUE, wenn der aktuelle User in der Allowlist als Demo-User freigeschaltet ist.';



CREATE OR REPLACE FUNCTION "public"."is_playstore_reviewer"() RETURNS boolean
    LANGUAGE "sql" STABLE SECURITY DEFINER
    SET "search_path" TO 'public', 'pg_temp'
    AS $$
    select exists (
        select 1
        from public.auth_allowlist al
        where lower(al.email) = public.current_user_email()
          and al.allow_playstore_review = true
    );
$$;


ALTER FUNCTION "public"."is_playstore_reviewer"() OWNER TO "postgres";


COMMENT ON FUNCTION "public"."is_playstore_reviewer"() IS 'TRUE, wenn der aktuelle User als Google-Play-Reviewer markiert ist.';



CREATE OR REPLACE FUNCTION "public"."remove_meter"("p_zaehler_id" bigint, "p_ausgebaut_am" "date", "p_endstand" numeric, "p_ablesedatum" timestamp without time zone DEFAULT "now"(), "p_foto_pfad" "text" DEFAULT NULL::"text") RETURNS "public"."zaehler"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_row public.zaehler;
  v_last_stand numeric;
begin
  select *
    into v_row
  from public.zaehler
  where id = p_zaehler_id
  for update;

  if v_row.id is null then
    raise exception 'Zähler % wurde nicht gefunden.', p_zaehler_id;
  end if;

  if v_row.status <> 'aktiv' or v_row.ausgebaut_am is not null then
    raise exception 'Zähler % ist nicht aktiv und kann nicht ausgebaut werden.', p_zaehler_id;
  end if;

  if p_ausgebaut_am is null then
    raise exception 'Ausbaudatum ist erforderlich.';
  end if;

  if p_ausgebaut_am < v_row.eingebaut_am then
    raise exception 'Ausbaudatum darf nicht vor Einbaudatum liegen.';
  end if;

  select a.stand
    into v_last_stand
  from public.zaehler_ablesung a
  where a.zaehler_id = p_zaehler_id
  order by a.ablesedatum desc
  limit 1;

  if v_last_stand is not null and p_endstand < v_last_stand then
    raise exception 'Endstand (%) darf nicht kleiner als letzter bekannter Stand (%) sein.',
      p_endstand, v_last_stand;
  end if;

  insert into public.zaehler_ablesung (
    zaehler_id,
    ablesedatum,
    stand,
    foto_pfad,
    freigegeben,
    art
  )
  values (
    p_zaehler_id,
    coalesce(p_ablesedatum, now()),
    p_endstand,
    p_foto_pfad,
    false,
    'ausbau'
  );

  update public.zaehler
     set ausgebaut_am = p_ausgebaut_am,
         status = 'ausgebaut'
   where id = p_zaehler_id
   returning * into v_row;

  return v_row;
end;
$$;


ALTER FUNCTION "public"."remove_meter"("p_zaehler_id" bigint, "p_ausgebaut_am" "date", "p_endstand" numeric, "p_ablesedatum" timestamp without time zone, "p_foto_pfad" "text") OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."set_jahr_from_id"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
BEGIN
    NEW.jahr := NEW.id;
    RETURN NEW;
END;
$$;


ALTER FUNCTION "public"."set_jahr_from_id"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."set_updated_at"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
begin
    new.updated_at := now();
    return new;
end;
$$;


ALTER FUNCTION "public"."set_updated_at"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."set_updated_at_impressum_funktion"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
begin
    new.updated_at = now();
    return new;
end;
$$;


ALTER FUNCTION "public"."set_updated_at_impressum_funktion"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."set_updated_at_impressum_funktion_slot"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
begin
    new.updated_at = now();
    return new;
end;
$$;


ALTER FUNCTION "public"."set_updated_at_impressum_funktion_slot"() OWNER TO "postgres";


CREATE TABLE IF NOT EXISTS "public"."arbeitseinsatz_anmeldung" (
    "id" bigint NOT NULL,
    "arbeitseinsatz_id" bigint NOT NULL,
    "mitglied_id" bigint NOT NULL,
    "status" "public"."arbeitseinsatz_anmeldung_status" DEFAULT 'angemeldet'::"public"."arbeitseinsatz_anmeldung_status" NOT NULL,
    "bemerkung" "text",
    "angemeldet_am" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL
);


ALTER TABLE "public"."arbeitseinsatz_anmeldung" OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."sign_off_from_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) RETURNS "public"."arbeitseinsatz_anmeldung"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_row public.arbeitseinsatz_anmeldung;
begin
  insert into public.arbeitseinsatz_anmeldung (
    arbeitseinsatz_id,
    mitglied_id,
    status
  )
  values (
    p_arbeitseinsatz_id,
    p_mitglied_id,
    'abgesagt'
  )
  on conflict (arbeitseinsatz_id, mitglied_id)
  do update
     set status = 'abgesagt',
         updated_at = now()
  returning * into v_row;

  return v_row;
end;
$$;


ALTER FUNCTION "public"."sign_off_from_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."sign_up_for_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) RETURNS "public"."arbeitseinsatz_anmeldung"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_row public.arbeitseinsatz_anmeldung;
begin
  insert into public.arbeitseinsatz_anmeldung (
    arbeitseinsatz_id,
    mitglied_id,
    status
  )
  values (
    p_arbeitseinsatz_id,
    p_mitglied_id,
    'angemeldet'
  )
  on conflict (arbeitseinsatz_id, mitglied_id)
  do update
     set status = 'angemeldet',
         updated_at = now()
  returning * into v_row;

  return v_row;
end;
$$;


ALTER FUNCTION "public"."sign_up_for_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."sync_app_user_from_mitglied"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
begin
  -- Wenn auth_user_id entfernt wurde: app_user Zeile löschen
  if new.auth_user_id is null then
    if old.auth_user_id is not null then
      delete from public.app_user where user_id = old.auth_user_id;
    end if;
    return new;
  end if;

  -- auth_user_id existiert: app_user-Verknüpfung sicherstellen, Rolle bleibt führend in app_user
  insert into public.app_user (user_id, mitglied_id, updated_at)
  values (new.auth_user_id, new.id, now())
  on conflict (user_id) do update
    set mitglied_id = excluded.mitglied_id,
        updated_at  = now();

  return new;
end;
$$;


ALTER FUNCTION "public"."sync_app_user_from_mitglied"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."trg_arbeitseinsatz_anmeldung_validate"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_aktiv boolean;
  v_max_teilnehmer integer;
  v_angemeldet_count integer;
  v_anmeldung_bis timestamp without time zone;
  v_datum date;
begin
  select
    a.aktiv,
    a.max_teilnehmer,
    a.anmeldung_bis,
    a.datum
  into
    v_aktiv,
    v_max_teilnehmer,
    v_anmeldung_bis,
    v_datum
  from public.arbeitseinsatz a
  where a.id = new.arbeitseinsatz_id;

  if v_aktiv is distinct from true then
    raise exception 'Anmeldung nicht möglich: Arbeitseinsatz ist nicht aktiv.';
  end if;

  if new.status = 'angemeldet' then
    if v_anmeldung_bis is not null and now()::timestamp > v_anmeldung_bis then
      raise exception 'Anmeldung nicht möglich: Anmeldeschluss ist erreicht.';
    end if;

    if v_anmeldung_bis is null and current_date > v_datum then
      raise exception 'Anmeldung nicht möglich: Arbeitseinsatz liegt in der Vergangenheit.';
    end if;

    if v_max_teilnehmer is not null then
      select count(*)
        into v_angemeldet_count
      from public.arbeitseinsatz_anmeldung x
      where x.arbeitseinsatz_id = new.arbeitseinsatz_id
        and x.status = 'angemeldet'
        and (tg_op = 'INSERT' or x.id <> new.id);

      if v_angemeldet_count >= v_max_teilnehmer then
        raise exception 'Anmeldung nicht möglich: maximale Teilnehmerzahl erreicht.';
      end if;
    end if;
  end if;

  return new;
end;
$$;


ALTER FUNCTION "public"."trg_arbeitseinsatz_anmeldung_validate"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."trg_parzelle_validate_rfid_global_unique"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_conflict_id bigint;
begin
  -- Neue Wasser-RFID prüfen
  if new.rfid_wasser is not null and btrim(new.rfid_wasser) <> '' then
    select p.id
      into v_conflict_id
    from public.parzelle p
    where p.id <> new.id
      and (
        p.rfid_wasser = new.rfid_wasser
        or p.rfid_strom = new.rfid_wasser
      )
    limit 1;

    if v_conflict_id is not null then
      raise exception 'RFID % ist bereits in einer anderen Parzelle vorhanden (Parzelle %).', new.rfid_wasser, v_conflict_id;
    end if;
  end if;

  -- Neue Strom-RFID prüfen
  if new.rfid_strom is not null and btrim(new.rfid_strom) <> '' then
    select p.id
      into v_conflict_id
    from public.parzelle p
    where p.id <> new.id
      and (
        p.rfid_wasser = new.rfid_strom
        or p.rfid_strom = new.rfid_strom
      )
    limit 1;

    if v_conflict_id is not null then
      raise exception 'RFID % ist bereits in einer anderen Parzelle vorhanden (Parzelle %).', new.rfid_strom, v_conflict_id;
    end if;
  end if;

  -- gleiche UID nicht gleichzeitig in Wasser und Strom derselben Parzelle
  if new.rfid_wasser is not null
     and new.rfid_strom is not null
     and btrim(new.rfid_wasser) <> ''
     and btrim(new.rfid_strom) <> ''
     and new.rfid_wasser = new.rfid_strom then
    raise exception 'Dieselbe RFID darf nicht gleichzeitig als Wasser- und Strom-RFID in derselben Parzelle gespeichert werden.';
  end if;

  return new;
end;
$$;


ALTER FUNCTION "public"."trg_parzelle_validate_rfid_global_unique"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."trg_zaehler_ablesung_validate"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_ausgebaut_am date;
begin
  select z.ausgebaut_am
    into v_ausgebaut_am
  from public.zaehler z
  where z.id = new.zaehler_id;

  if v_ausgebaut_am is not null and new.ablesedatum::date > v_ausgebaut_am then
    raise exception 'Ablesedatum liegt nach dem Ausbau des Zählers.';
  end if;

  return new;
end;
$$;


ALTER FUNCTION "public"."trg_zaehler_ablesung_validate"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."trg_zaehler_set_eichfaellig"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
begin
  new.eichfaellig_am := public.calc_eichfaellig_am(new.medium, new.eichdatum);
  return new;
end;
$$;


ALTER FUNCTION "public"."trg_zaehler_set_eichfaellig"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."trg_zaehler_validate_medium_allowed"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_hat_wasser boolean;
  v_hat_strom boolean;
begin
  select p.hat_wasser, p.hat_strom
    into v_hat_wasser, v_hat_strom
  from public.parzelle p
  where p.id = new.parzelle_id;

  if new.medium = 'wasser' and coalesce(v_hat_wasser, false) = false then
    raise exception 'Parzelle % ist nicht als wasserführend markiert.', new.parzelle_id;
  end if;

  if new.medium = 'strom' and coalesce(v_hat_strom, false) = false then
    raise exception 'Parzelle % ist nicht als stromführend markiert.', new.parzelle_id;
  end if;

  return new;
end;
$$;


ALTER FUNCTION "public"."trg_zaehler_validate_medium_allowed"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."trg_zaehler_validate_parzelle_rfid"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
declare
  v_rfid text;
begin
  if new.medium = 'wasser' then
    select p.rfid_wasser into v_rfid
    from public.parzelle p
    where p.id = new.parzelle_id;
  else
    select p.rfid_strom into v_rfid
    from public.parzelle p
    where p.id = new.parzelle_id;
  end if;

  if v_rfid is null or btrim(v_rfid) = '' then
    raise exception 'Für Parzelle % ist keine RFID für Medium % hinterlegt.', new.parzelle_id, new.medium;
  end if;

  return new;
end;
$$;


ALTER FUNCTION "public"."trg_zaehler_validate_parzelle_rfid"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."try_lock_mitglied"("p_id" integer, "p_user_id" "uuid", "p_timeout_minutes" integer DEFAULT 10) RETURNS boolean
    LANGUAGE "plpgsql"
    AS $$
declare
    v_now timestamptz := now();
begin
    update mitglied
    set 
        locked_by_user_id = p_user_id,
        locked_at = v_now
    where id = p_id
    and (
        locked_by_user_id is null
        or locked_by_user_id = p_user_id
        or locked_at < (v_now - (p_timeout_minutes || ' minutes')::interval)
    );

    return found;
end;
$$;


ALTER FUNCTION "public"."try_lock_mitglied"("p_id" integer, "p_user_id" "uuid", "p_timeout_minutes" integer) OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."validate_wartungsvertrag_zuordnung"() RETURNS "trigger"
    LANGUAGE "plpgsql"
    AS $$
declare
    v_is_hauptmitglied boolean;
    v_max_aktive_zuordnungen integer;
    v_spitzenbelegung integer;
begin
    -- nur Hauptmitglieder zulassen
    select (m.hauptmitglied_id is null)
      into v_is_hauptmitglied
    from public.mitglied m
    where m.id = new.hauptmitglied_id;

    if v_is_hauptmitglied is null then
        raise exception 'Mitglied % existiert nicht.', new.hauptmitglied_id;
    end if;

    if v_is_hauptmitglied is not true then
        raise exception 'Wartungsverträge dürfen nur Hauptmitgliedern zugeordnet werden. Mitglied % ist kein Hauptmitglied.',
            new.hauptmitglied_id;
    end if;

    -- max_aktive_zuordnungen laden
    select w.max_aktive_zuordnungen
      into v_max_aktive_zuordnungen
    from public.wartungsvertraege w
    where w.id = new.wartungsvertrag_id;

    if v_max_aktive_zuordnungen is null then
        raise exception 'Wartungsvertrag % existiert nicht.', new.wartungsvertrag_id;
    end if;

    -- gleiche Kombination Vertrag + Hauptmitglied darf sich zeitlich nicht überschneiden
    if exists (
        select 1
        from public.wartungsvertrag_zuordnungen z
        where z.wartungsvertrag_id = new.wartungsvertrag_id
          and z.hauptmitglied_id = new.hauptmitglied_id
          and z.id <> coalesce(new.id, -1)
          and daterange(
                z.gueltig_ab,
                coalesce(z.gueltig_bis, 'infinity'::date),
                '[]'
              )
              &&
              daterange(
                new.gueltig_ab,
                coalesce(new.gueltig_bis, 'infinity'::date),
                '[]'
              )
    ) then
        raise exception
            'Das Hauptmitglied % hat den Wartungsvertrag % im angegebenen Zeitraum bereits zugeordnet.',
            new.hauptmitglied_id,
            new.wartungsvertrag_id;
    end if;

    -- prüfen, ob max_aktive_zuordnungen irgendwo innerhalb des Zeitraums überschritten würde
    with existing_rows as (
        select z.gueltig_ab, z.gueltig_bis
        from public.wartungsvertrag_zuordnungen z
        where z.wartungsvertrag_id = new.wartungsvertrag_id
          and z.id <> coalesce(new.id, -1)
          and daterange(
                z.gueltig_ab,
                coalesce(z.gueltig_bis, 'infinity'::date),
                '[]'
              )
              &&
              daterange(
                new.gueltig_ab,
                coalesce(new.gueltig_bis, 'infinity'::date),
                '[]'
              )
    ),
    candidate_dates as (
        select new.gueltig_ab as d
        union
        select new.gueltig_bis
        where new.gueltig_bis is not null
        union
        select e.gueltig_ab
        from existing_rows e
        where e.gueltig_ab between new.gueltig_ab and coalesce(new.gueltig_bis, 'infinity'::date)
        union
        select e.gueltig_bis
        from existing_rows e
        where e.gueltig_bis is not null
          and e.gueltig_bis between new.gueltig_ab and coalesce(new.gueltig_bis, 'infinity'::date)
    )
    select coalesce(
               max(
                   (
                       select count(*)
                       from public.wartungsvertrag_zuordnungen z
                       where z.wartungsvertrag_id = new.wartungsvertrag_id
                         and z.id <> coalesce(new.id, -1)
                         and c.d between z.gueltig_ab and coalesce(z.gueltig_bis, 'infinity'::date)
                   ) + 1
               ),
               1
           )
      into v_spitzenbelegung
    from candidate_dates c;

    if v_spitzenbelegung > v_max_aktive_zuordnungen then
        raise exception
            'Maximale Anzahl aktiver Zuordnungen (% ) für Wartungsvertrag % würde überschritten.',
            v_max_aktive_zuordnungen,
            new.wartungsvertrag_id;
    end if;

    return new;
end;
$$;


ALTER FUNCTION "public"."validate_wartungsvertrag_zuordnung"() OWNER TO "postgres";


CREATE OR REPLACE FUNCTION "public"."who_am_i"() RETURNS TABLE("vorname" "text", "role" "text")
    LANGUAGE "sql" STABLE
    AS $$
  select
    m.vorname,
    public.current_app_role() as role
  from public.mitglied m
  where m.auth_user_id = auth.uid()
  limit 1
$$;


ALTER FUNCTION "public"."who_am_i"() OWNER TO "postgres";


CREATE TABLE IF NOT EXISTS "public"."app_user" (
    "user_id" "uuid" NOT NULL,
    "mitglied_id" bigint,
    "role" "text" DEFAULT 'user'::"text" NOT NULL,
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "is_demo_account" boolean DEFAULT false NOT NULL,
    CONSTRAINT "app_user_role_chk" CHECK (("role" = ANY (ARRAY['admin'::"text", 'vorstand'::"text", 'user'::"text"])))
);


ALTER TABLE "public"."app_user" OWNER TO "postgres";


COMMENT ON COLUMN "public"."app_user"."is_demo_account" IS 'Kennzeichnet Demo-/Play-Store-/Test-Logins. Dient der Steuerung von Testzugängen, nicht fachlichen Berechnungen.';



CREATE TABLE IF NOT EXISTS "public"."arbeitseinsatz" (
    "id" bigint NOT NULL,
    "titel" "text" NOT NULL,
    "beschreibung" "text",
    "datum" "date" NOT NULL,
    "start_uhrzeit" time without time zone,
    "end_uhrzeit" time without time zone,
    "treffpunkt" "text",
    "max_teilnehmer" integer,
    "stunden_wert" numeric DEFAULT 0 NOT NULL,
    "sichtbar_ab" timestamp without time zone,
    "sichtbar_bis" timestamp without time zone,
    "anmeldung_bis" timestamp without time zone,
    "aktiv" boolean DEFAULT true NOT NULL,
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "is_demo" boolean DEFAULT false NOT NULL,
    CONSTRAINT "ck_arbeitseinsatz_max_teilnehmer_positive" CHECK ((("max_teilnehmer" IS NULL) OR ("max_teilnehmer" > 0))),
    CONSTRAINT "ck_arbeitseinsatz_sichtbarkeit" CHECK ((("sichtbar_ab" IS NULL) OR ("sichtbar_bis" IS NULL) OR ("sichtbar_bis" >= "sichtbar_ab"))),
    CONSTRAINT "ck_arbeitseinsatz_stunden_wert_nonnegative" CHECK (("stunden_wert" >= (0)::numeric)),
    CONSTRAINT "ck_arbeitseinsatz_titel_not_blank" CHECK (("btrim"("titel") <> ''::"text")),
    CONSTRAINT "ck_arbeitseinsatz_zeitraum" CHECK ((("start_uhrzeit" IS NULL) OR ("end_uhrzeit" IS NULL) OR ("end_uhrzeit" >= "start_uhrzeit")))
);


ALTER TABLE "public"."arbeitseinsatz" OWNER TO "postgres";


COMMENT ON COLUMN "public"."arbeitseinsatz"."is_demo" IS 'Kennzeichnet Demo-/Test-Arbeitseinsätze. Nicht in echte fachliche Auswertungen einbeziehen.';



ALTER TABLE "public"."arbeitseinsatz_anmeldung" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."arbeitseinsatz_anmeldung_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



ALTER TABLE "public"."arbeitseinsatz" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."arbeitseinsatz_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE TABLE IF NOT EXISTS "public"."arbeitsstunde" (
    "id" integer NOT NULL,
    "mitglied_id" bigint NOT NULL,
    "saison_id" bigint NOT NULL,
    "datum" "date" NOT NULL,
    "stunden" numeric NOT NULL,
    "art_der_arbeit" "text" NOT NULL,
    "freigegeben" boolean DEFAULT false NOT NULL,
    "status" "text" DEFAULT 'offen'::"text",
    "genehmigt_von" bigint,
    "genehmigt_am" timestamp without time zone,
    "lockedbyuserid" "uuid",
    "lockat" timestamp without time zone,
    CONSTRAINT "status_check" CHECK (("status" = ANY (ARRAY['offen'::"text", 'genehmigt'::"text", 'abgelehnt'::"text"])))
);


ALTER TABLE "public"."arbeitsstunde" OWNER TO "postgres";


ALTER TABLE "public"."arbeitsstunde" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."arbeitsstunde_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE TABLE IF NOT EXISTS "public"."auth_allowlist" (
    "email" "text" NOT NULL,
    "allow_email_otp" boolean DEFAULT true NOT NULL,
    "allow_google" boolean DEFAULT false NOT NULL,
    "is_active" boolean DEFAULT true NOT NULL,
    "note" "text",
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "allow_demo_access" boolean DEFAULT false NOT NULL,
    "allow_playstore_review" boolean DEFAULT false NOT NULL
);


ALTER TABLE "public"."auth_allowlist" OWNER TO "postgres";


COMMENT ON COLUMN "public"."auth_allowlist"."allow_demo_access" IS 'Erlaubt dem Konto ausschließlich den späteren Demo-/Review-Zugriff ohne echte Produktivdaten.';



COMMENT ON COLUMN "public"."auth_allowlist"."allow_playstore_review" IS 'Kennzeichnet ein Konto als Google-Play-Review-/Prüfkonto.';



CREATE TABLE IF NOT EXISTS "public"."bekanntmachung" (
    "id" bigint NOT NULL,
    "titel" "text" NOT NULL,
    "inhalt_html" "text" NOT NULL,
    "sichtbar_ab" timestamp without time zone,
    "sichtbar_bis" timestamp without time zone,
    "sort_order" integer,
    "aktiv" boolean DEFAULT true NOT NULL,
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    CONSTRAINT "ck_bekanntmachung_inhalt_not_blank" CHECK (("btrim"("inhalt_html") <> ''::"text")),
    CONSTRAINT "ck_bekanntmachung_sichtbarkeit" CHECK ((("sichtbar_ab" IS NULL) OR ("sichtbar_bis" IS NULL) OR ("sichtbar_bis" >= "sichtbar_ab"))),
    CONSTRAINT "ck_bekanntmachung_titel_not_blank" CHECK (("btrim"("titel") <> ''::"text"))
);


ALTER TABLE "public"."bekanntmachung" OWNER TO "postgres";


ALTER TABLE "public"."bekanntmachung" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."bekanntmachung_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE TABLE IF NOT EXISTS "public"."client_diagnostics_log" (
    "id" "uuid" DEFAULT "gen_random_uuid"() NOT NULL,
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "app" "text" NOT NULL,
    "environment" "text" DEFAULT 'prod'::"text" NOT NULL,
    "user_id" "uuid",
    "client_request_id" "text",
    "category" "text" NOT NULL,
    "level" "text" DEFAULT 'info'::"text" NOT NULL,
    "message" "text" NOT NULL,
    "has_access_token" boolean,
    "token_length" integer,
    "retry_attempted" boolean,
    "http_status" integer,
    "raw_body" "text",
    "extra" "jsonb" DEFAULT '{}'::"jsonb" NOT NULL
);


ALTER TABLE "public"."client_diagnostics_log" OWNER TO "postgres";


CREATE TABLE IF NOT EXISTS "public"."dokument" (
    "id" bigint NOT NULL,
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "mitglied_id" integer,
    "parzelle_id" integer,
    "bucket" "text" DEFAULT 'dokumente'::"text" NOT NULL,
    "storage_path" "text" NOT NULL,
    "titel" "text",
    "dateiname" "text",
    "mime_type" "text",
    "size_bytes" bigint,
    "created_by" "uuid",
    CONSTRAINT "dokument_owner_chk" CHECK ((("mitglied_id" IS NOT NULL) OR ("parzelle_id" IS NOT NULL)))
);


ALTER TABLE "public"."dokument" OWNER TO "postgres";


CREATE SEQUENCE IF NOT EXISTS "public"."dokument_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;


ALTER SEQUENCE "public"."dokument_id_seq" OWNER TO "postgres";


ALTER SEQUENCE "public"."dokument_id_seq" OWNED BY "public"."dokument"."id";



CREATE TABLE IF NOT EXISTS "public"."impressum_funktion_slot" (
    "id" bigint NOT NULL,
    "slot_key" "text" NOT NULL,
    "funktion" "text" NOT NULL,
    "sort_order" smallint NOT NULL,
    "mitglied_id" bigint,
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    CONSTRAINT "ck_impressum_funktion_slot_sort_order" CHECK ((("sort_order" >= 1) AND ("sort_order" <= 7)))
);


ALTER TABLE "public"."impressum_funktion_slot" OWNER TO "postgres";


ALTER TABLE "public"."impressum_funktion_slot" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."impressum_funktion_slot_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE TABLE IF NOT EXISTS "public"."mitglied" (
    "id" bigint NOT NULL,
    "hauptmitglied_id" bigint,
    "name" "text" NOT NULL,
    "vorname" "text" NOT NULL,
    "geburtsdatum" "date",
    "adresse" "text",
    "plz" character varying(5),
    "ort" "text",
    "telefon" "text",
    "email" "text",
    "ist_kgv" boolean DEFAULT false NOT NULL,
    "bemerkung" "text",
    "whatsapp_einwilligung" boolean DEFAULT false NOT NULL,
    "auth_user_id" "uuid",
    "role" "text" DEFAULT 'user'::"text" NOT NULL,
    "aktiv" boolean DEFAULT true NOT NULL,
    "lockedbyuserid" "uuid",
    "lockat" timestamp without time zone,
    "mitglied_seit" "date" DEFAULT CURRENT_DATE NOT NULL,
    "mitglied_ende" "date",
    "handy" "text",
    "arbeitsstunden_altersregel_typ" "text" DEFAULT 'keine'::"text" NOT NULL,
    "email_info_einwilligung" boolean DEFAULT false NOT NULL,
    "email_rechnung_einwilligung" boolean DEFAULT false NOT NULL,
    "is_demo" boolean DEFAULT false NOT NULL,
    CONSTRAINT "ck_mitglied_arbeitsstunden_altersregel_typ" CHECK (("arbeitsstunden_altersregel_typ" = ANY (ARRAY['keine'::"text", 'frau75'::"text", 'mann80'::"text"]))),
    CONSTRAINT "role_check" CHECK (("role" = ANY (ARRAY['admin'::"text", 'vorstand'::"text", 'user'::"text"])))
);


ALTER TABLE "public"."mitglied" OWNER TO "postgres";


COMMENT ON COLUMN "public"."mitglied"."handy" IS 'Mobilnummer/Handynummer des Mitglieds (optional).';



COMMENT ON COLUMN "public"."mitglied"."is_demo" IS 'Kennzeichnet Demo-/Play-Store-/Test-Mitglieder. Diese Datensätze müssen aus fachlichen Berechnungen und Auswertungen ausgeschlossen werden.';



ALTER TABLE "public"."mitglied" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."mitglied_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE TABLE IF NOT EXISTS "public"."mitglied_saison" (
    "id" bigint NOT NULL,
    "mitglied_id" bigint NOT NULL,
    "saison_id" bigint NOT NULL,
    "pflichtstunden" numeric DEFAULT 0 NOT NULL,
    "status" smallint DEFAULT 1 NOT NULL,
    "beitrag" numeric DEFAULT 0 NOT NULL,
    CONSTRAINT "mitglied_saison_status_check" CHECK (("status" = ANY (ARRAY[1, 2, 3])))
);


ALTER TABLE "public"."mitglied_saison" OWNER TO "postgres";


ALTER TABLE "public"."mitglied_saison" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."mitglied_saison_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



ALTER TABLE "public"."parzelle" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."parzelle_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE TABLE IF NOT EXISTS "public"."parzellen_belegung" (
    "id" bigint NOT NULL,
    "parzelle_id" bigint NOT NULL,
    "mitglied_id" bigint NOT NULL,
    "von_datum" "date" NOT NULL,
    "bis_datum" "date"
);


ALTER TABLE "public"."parzellen_belegung" OWNER TO "postgres";


ALTER TABLE "public"."parzellen_belegung" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."parzellen_belegung_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE TABLE IF NOT EXISTS "public"."saison" (
    "id" integer NOT NULL,
    "jahr" integer,
    "pflichtstunden_soll" numeric(6,2) DEFAULT 0 NOT NULL,
    "euro_pro_fehlstunde" numeric(10,2) DEFAULT 25.00 NOT NULL,
    "bemerkung" "text"
);


ALTER TABLE "public"."saison" OWNER TO "postgres";


ALTER TABLE "public"."saison" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."saison_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE TABLE IF NOT EXISTS "public"."termin" (
    "id" bigint NOT NULL,
    "titel" "text" NOT NULL,
    "beschreibung" "text",
    "datum" "date" NOT NULL,
    "start_uhrzeit" time without time zone,
    "end_uhrzeit" time without time zone,
    "sichtbar_ab" timestamp without time zone,
    "sichtbar_bis" timestamp without time zone,
    "aktiv" boolean DEFAULT true NOT NULL,
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    CONSTRAINT "ck_termin_sichtbarkeit" CHECK ((("sichtbar_ab" IS NULL) OR ("sichtbar_bis" IS NULL) OR ("sichtbar_bis" >= "sichtbar_ab"))),
    CONSTRAINT "ck_termin_titel_not_blank" CHECK (("btrim"("titel") <> ''::"text")),
    CONSTRAINT "ck_termin_zeitraum" CHECK ((("start_uhrzeit" IS NULL) OR ("end_uhrzeit" IS NULL) OR ("end_uhrzeit" >= "start_uhrzeit")))
);


ALTER TABLE "public"."termin" OWNER TO "postgres";


ALTER TABLE "public"."termin" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."termin_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE OR REPLACE VIEW "public"."v_aktive_zaehler" WITH ("security_invoker"='true') AS
 SELECT "z"."id",
    "z"."parzelle_id",
    "p"."Anlage" AS "anlage",
    "p"."garten_nr",
    "z"."medium",
        CASE
            WHEN ("z"."medium" = 'wasser'::"public"."zaehler_medium") THEN "p"."rfid_wasser"
            ELSE "p"."rfid_strom"
        END AS "rfid_tag_uid",
    "z"."zaehlernummer",
    "z"."eichdatum",
    "z"."eichfaellig_am",
    "z"."eingebaut_am"
   FROM ("public"."zaehler" "z"
     JOIN "public"."parzelle" "p" ON (("p"."id" = "z"."parzelle_id")))
  WHERE (("z"."status" = 'aktiv'::"public"."zaehler_status") AND ("z"."ausgebaut_am" IS NULL));


ALTER VIEW "public"."v_aktive_zaehler" OWNER TO "postgres";


CREATE OR REPLACE VIEW "public"."v_pflichtstunden_uebersicht" AS
 SELECT "s"."id" AS "saison_id",
    "s"."jahr" AS "saison_jahr",
    "m"."id" AS "hauptmitglied_id",
    "m"."name",
    "m"."vorname",
    "x"."regelgrund",
    "x"."ist_befreit",
    "x"."hat_wartungsvertrag",
    "x"."altersbefreit",
    "x"."eintritt_im_saisonjahr",
    "x"."eintritt_zweites_halbjahr",
    "x"."pflichtstunden_soll",
    "x"."geleistete_stunden",
    "x"."offene_stunden",
    "x"."euro_pro_fehlstunde",
    "x"."fehlbetrag"
   FROM (("public"."saison" "s"
     CROSS JOIN "public"."mitglied" "m")
     CROSS JOIN LATERAL "public"."fn_berechne_pflichtstunden_status"("m"."id", ("s"."id")::bigint) "x"("hauptmitglied_id", "saison_id", "saison_jahr", "regelgrund", "ist_befreit", "hat_wartungsvertrag", "altersbefreit", "eintritt_im_saisonjahr", "eintritt_zweites_halbjahr", "pflichtstunden_soll", "geleistete_stunden", "offene_stunden", "euro_pro_fehlstunde", "fehlbetrag"))
  WHERE ("m"."hauptmitglied_id" IS NULL);


ALTER VIEW "public"."v_pflichtstunden_uebersicht" OWNER TO "postgres";


CREATE OR REPLACE VIEW "public"."v_rfid_scan_context" WITH ("security_invoker"='true') AS
 SELECT "p"."id" AS "parzelle_id",
    "p"."Anlage" AS "anlage",
    "p"."garten_nr",
    'wasser'::"public"."zaehler_medium" AS "medium",
    "p"."rfid_wasser" AS "rfid_tag_uid",
    "z"."id" AS "aktiver_zaehler_id",
    "z"."zaehlernummer",
    "z"."eichdatum",
    "z"."eichfaellig_am",
    "z"."eingebaut_am",
    "z"."ausgebaut_am",
    "z"."status"
   FROM ("public"."parzelle" "p"
     LEFT JOIN "public"."zaehler" "z" ON ((("z"."parzelle_id" = "p"."id") AND ("z"."medium" = 'wasser'::"public"."zaehler_medium") AND ("z"."status" = 'aktiv'::"public"."zaehler_status") AND ("z"."ausgebaut_am" IS NULL))))
  WHERE ("p"."rfid_wasser" IS NOT NULL)
UNION ALL
 SELECT "p"."id" AS "parzelle_id",
    "p"."Anlage" AS "anlage",
    "p"."garten_nr",
    'strom'::"public"."zaehler_medium" AS "medium",
    "p"."rfid_strom" AS "rfid_tag_uid",
    "z"."id" AS "aktiver_zaehler_id",
    "z"."zaehlernummer",
    "z"."eichdatum",
    "z"."eichfaellig_am",
    "z"."eingebaut_am",
    "z"."ausgebaut_am",
    "z"."status"
   FROM ("public"."parzelle" "p"
     LEFT JOIN "public"."zaehler" "z" ON ((("z"."parzelle_id" = "p"."id") AND ("z"."medium" = 'strom'::"public"."zaehler_medium") AND ("z"."status" = 'aktiv'::"public"."zaehler_status") AND ("z"."ausgebaut_am" IS NULL))))
  WHERE ("p"."rfid_strom" IS NOT NULL);


ALTER VIEW "public"."v_rfid_scan_context" OWNER TO "postgres";


CREATE OR REPLACE VIEW "public"."v_startseite_arbeitseinsatz" AS
 SELECT "a"."id",
    "a"."titel",
    "a"."beschreibung",
    "a"."datum",
    "a"."start_uhrzeit",
    "a"."end_uhrzeit",
    "a"."treffpunkt",
    "a"."max_teilnehmer",
    "a"."stunden_wert",
    "a"."sichtbar_ab",
    "a"."sichtbar_bis",
    "a"."anmeldung_bis",
    (COALESCE("sum"(
        CASE
            WHEN ("aa"."status" = 'angemeldet'::"public"."arbeitseinsatz_anmeldung_status") THEN 1
            ELSE 0
        END), (0)::bigint))::integer AS "angemeldet_count",
        CASE
            WHEN ("a"."max_teilnehmer" IS NULL) THEN NULL::integer
            ELSE (GREATEST(("a"."max_teilnehmer" - COALESCE("sum"(
            CASE
                WHEN ("aa"."status" = 'angemeldet'::"public"."arbeitseinsatz_anmeldung_status") THEN 1
                ELSE 0
            END), (0)::bigint)), (0)::bigint))::integer
        END AS "freie_plaetze"
   FROM ("public"."arbeitseinsatz" "a"
     LEFT JOIN "public"."arbeitseinsatz_anmeldung" "aa" ON (("aa"."arbeitseinsatz_id" = "a"."id")))
  WHERE (("a"."aktiv" = true) AND (("a"."sichtbar_ab" IS NULL) OR ("a"."sichtbar_ab" <= ("now"())::timestamp without time zone)) AND (("a"."sichtbar_bis" IS NULL) OR ("a"."sichtbar_bis" >= ("now"())::timestamp without time zone)))
  GROUP BY "a"."id", "a"."titel", "a"."beschreibung", "a"."datum", "a"."start_uhrzeit", "a"."end_uhrzeit", "a"."treffpunkt", "a"."max_teilnehmer", "a"."stunden_wert", "a"."sichtbar_ab", "a"."sichtbar_bis", "a"."anmeldung_bis"
  ORDER BY "a"."datum", "a"."start_uhrzeit" NULLS FIRST, "a"."id";


ALTER VIEW "public"."v_startseite_arbeitseinsatz" OWNER TO "postgres";


CREATE OR REPLACE VIEW "public"."v_startseite_bekanntmachungen" WITH ("security_invoker"='true') AS
 SELECT "id",
    "titel",
    "inhalt_html",
    "sichtbar_ab",
    "sichtbar_bis",
    "sort_order",
    "created_at"
   FROM "public"."bekanntmachung" "b"
  WHERE (("aktiv" = true) AND (("sichtbar_ab" IS NULL) OR ("sichtbar_ab" <= ("now"())::timestamp without time zone)) AND (("sichtbar_bis" IS NULL) OR ("sichtbar_bis" >= ("now"())::timestamp without time zone)))
  ORDER BY "sort_order", "created_at" DESC, "id" DESC;


ALTER VIEW "public"."v_startseite_bekanntmachungen" OWNER TO "postgres";


CREATE OR REPLACE VIEW "public"."v_startseite_termine" WITH ("security_invoker"='true') AS
 SELECT "id",
    "titel",
    "beschreibung",
    "datum",
    "start_uhrzeit",
    "end_uhrzeit",
    "sichtbar_ab",
    "sichtbar_bis"
   FROM "public"."termin" "t"
  WHERE (("aktiv" = true) AND (("sichtbar_ab" IS NULL) OR ("sichtbar_ab" <= ("now"())::timestamp without time zone)) AND (("sichtbar_bis" IS NULL) OR ("sichtbar_bis" >= ("now"())::timestamp without time zone)))
  ORDER BY "datum", "start_uhrzeit" NULLS FIRST, "id";


ALTER VIEW "public"."v_startseite_termine" OWNER TO "postgres";


CREATE OR REPLACE VIEW "public"."v_zaehler_eichstatus" WITH ("security_invoker"='true') AS
 SELECT "z"."id",
    "z"."parzelle_id",
    "p"."Anlage" AS "anlage",
    "p"."garten_nr",
    "z"."medium",
    "z"."zaehlernummer",
    "z"."eichdatum",
    "z"."eichfaellig_am",
    "z"."eingebaut_am",
    "z"."status",
    ("z"."eichfaellig_am" - CURRENT_DATE) AS "tage_bis_faellig",
        CASE
            WHEN ("z"."eichfaellig_am" < CURRENT_DATE) THEN 'ueberfaellig'::"text"
            WHEN ("z"."eichfaellig_am" <= (CURRENT_DATE + 180)) THEN 'bald_faellig'::"text"
            ELSE 'ok'::"text"
        END AS "eichstatus"
   FROM ("public"."zaehler" "z"
     JOIN "public"."parzelle" "p" ON (("p"."id" = "z"."parzelle_id")))
  WHERE (("z"."status" = 'aktiv'::"public"."zaehler_status") AND ("z"."ausgebaut_am" IS NULL));


ALTER VIEW "public"."v_zaehler_eichstatus" OWNER TO "postgres";


CREATE TABLE IF NOT EXISTS "public"."wartungsvertraege" (
    "id" bigint NOT NULL,
    "titel" "text" NOT NULL,
    "beschreibung" "text",
    "bereich" "text",
    "max_aktive_zuordnungen" integer DEFAULT 1 NOT NULL,
    "befreit_von_pflichtstunden" boolean DEFAULT true NOT NULL,
    "aktiv" boolean DEFAULT true NOT NULL,
    "bemerkung" "text",
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "is_demo" boolean DEFAULT false NOT NULL,
    CONSTRAINT "ck_wartungsvertraege_max_aktive_zuordnungen" CHECK (("max_aktive_zuordnungen" >= 1))
);


ALTER TABLE "public"."wartungsvertraege" OWNER TO "postgres";


COMMENT ON COLUMN "public"."wartungsvertraege"."is_demo" IS 'Kennzeichnet Demo-/Test-Wartungsverträge. Nicht in echte fachliche Auswertungen einbeziehen.';



ALTER TABLE "public"."wartungsvertraege" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."wartungsvertraege_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



CREATE TABLE IF NOT EXISTS "public"."wartungsvertrag_zuordnungen" (
    "id" bigint NOT NULL,
    "wartungsvertrag_id" bigint NOT NULL,
    "hauptmitglied_id" bigint NOT NULL,
    "gueltig_ab" "date" NOT NULL,
    "gueltig_bis" "date",
    "bemerkung" "text",
    "created_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    "updated_at" timestamp with time zone DEFAULT "now"() NOT NULL,
    CONSTRAINT "ck_wvz_gueltigkeit" CHECK ((("gueltig_bis" IS NULL) OR ("gueltig_bis" >= "gueltig_ab")))
);


ALTER TABLE "public"."wartungsvertrag_zuordnungen" OWNER TO "postgres";


ALTER TABLE "public"."wartungsvertrag_zuordnungen" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."wartungsvertrag_zuordnungen_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



ALTER TABLE "public"."zaehler_ablesung" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."zaehler_ablesung_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



ALTER TABLE "public"."zaehler" ALTER COLUMN "id" ADD GENERATED BY DEFAULT AS IDENTITY (
    SEQUENCE NAME "public"."zaehler_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);



ALTER TABLE ONLY "public"."dokument" ALTER COLUMN "id" SET DEFAULT "nextval"('"public"."dokument_id_seq"'::"regclass");



ALTER TABLE ONLY "public"."app_user"
    ADD CONSTRAINT "app_user_pkey" PRIMARY KEY ("user_id");



ALTER TABLE ONLY "public"."arbeitseinsatz_anmeldung"
    ADD CONSTRAINT "arbeitseinsatz_anmeldung_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."arbeitseinsatz"
    ADD CONSTRAINT "arbeitseinsatz_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."arbeitsstunde"
    ADD CONSTRAINT "arbeitsstunde_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."auth_allowlist"
    ADD CONSTRAINT "auth_allowlist_pkey" PRIMARY KEY ("email");



ALTER TABLE ONLY "public"."bekanntmachung"
    ADD CONSTRAINT "bekanntmachung_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."client_diagnostics_log"
    ADD CONSTRAINT "client_diagnostics_log_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."dokument"
    ADD CONSTRAINT "dokument_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."impressum_funktion_slot"
    ADD CONSTRAINT "impressum_funktion_slot_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."impressum_funktion_slot"
    ADD CONSTRAINT "impressum_funktion_slot_slot_key_key" UNIQUE ("slot_key");



ALTER TABLE ONLY "public"."impressum_funktion_slot"
    ADD CONSTRAINT "impressum_funktion_slot_sort_order_key" UNIQUE ("sort_order");



ALTER TABLE ONLY "public"."mitglied"
    ADD CONSTRAINT "mitglied_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."mitglied_saison"
    ADD CONSTRAINT "mitglied_saison_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."parzelle"
    ADD CONSTRAINT "parzelle_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."parzelle"
    ADD CONSTRAINT "parzelle_rfid_strom_key" UNIQUE ("rfid_strom");



ALTER TABLE ONLY "public"."parzelle"
    ADD CONSTRAINT "parzelle_rfid_wasser_key" UNIQUE ("rfid_wasser");



ALTER TABLE ONLY "public"."parzellen_belegung"
    ADD CONSTRAINT "parzellen_belegung_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."saison"
    ADD CONSTRAINT "saison_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."termin"
    ADD CONSTRAINT "termin_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."arbeitseinsatz_anmeldung"
    ADD CONSTRAINT "uq_arbeitseinsatz_anmeldung" UNIQUE ("arbeitseinsatz_id", "mitglied_id");



ALTER TABLE ONLY "public"."wartungsvertraege"
    ADD CONSTRAINT "wartungsvertraege_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."wartungsvertrag_zuordnungen"
    ADD CONSTRAINT "wartungsvertrag_zuordnungen_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."zaehler_ablesung"
    ADD CONSTRAINT "zaehler_ablesung_pkey" PRIMARY KEY ("id");



ALTER TABLE ONLY "public"."zaehler"
    ADD CONSTRAINT "zaehler_pkey" PRIMARY KEY ("id");



CREATE UNIQUE INDEX "app_user_mitglied_id_uq" ON "public"."app_user" USING "btree" ("mitglied_id") WHERE ("mitglied_id" IS NOT NULL);



CREATE INDEX "app_user_user_id_idx" ON "public"."app_user" USING "btree" ("user_id");



CREATE INDEX "ix_arbeitseinsatz_aktiv_datum" ON "public"."arbeitseinsatz" USING "btree" ("aktiv", "datum");



CREATE INDEX "ix_arbeitseinsatz_anmeldung_einsatz" ON "public"."arbeitseinsatz_anmeldung" USING "btree" ("arbeitseinsatz_id");



CREATE INDEX "ix_arbeitseinsatz_anmeldung_mitglied" ON "public"."arbeitseinsatz_anmeldung" USING "btree" ("mitglied_id");



CREATE INDEX "ix_arbeitseinsatz_anmeldung_status" ON "public"."arbeitseinsatz_anmeldung" USING "btree" ("status");



CREATE INDEX "ix_arbeitseinsatz_datum" ON "public"."arbeitseinsatz" USING "btree" ("datum");



CREATE INDEX "ix_arbeitseinsatz_sichtbar_bis" ON "public"."arbeitseinsatz" USING "btree" ("sichtbar_bis");



CREATE INDEX "ix_bekanntmachung_aktiv" ON "public"."bekanntmachung" USING "btree" ("aktiv");



CREATE INDEX "ix_bekanntmachung_sichtbar_bis" ON "public"."bekanntmachung" USING "btree" ("sichtbar_bis");



CREATE INDEX "ix_bekanntmachung_sort_order" ON "public"."bekanntmachung" USING "btree" ("sort_order");



CREATE INDEX "ix_client_diagnostics_log_category" ON "public"."client_diagnostics_log" USING "btree" ("category");



CREATE INDEX "ix_client_diagnostics_log_client_request_id" ON "public"."client_diagnostics_log" USING "btree" ("client_request_id");



CREATE INDEX "ix_client_diagnostics_log_created_at" ON "public"."client_diagnostics_log" USING "btree" ("created_at" DESC);



CREATE INDEX "ix_dokument_mitglied" ON "public"."dokument" USING "btree" ("mitglied_id");



CREATE INDEX "ix_dokument_parzelle" ON "public"."dokument" USING "btree" ("parzelle_id");



CREATE INDEX "ix_dokument_storage_path" ON "public"."dokument" USING "btree" ("storage_path");



CREATE INDEX "ix_impressum_funktion_slot_mitglied_id" ON "public"."impressum_funktion_slot" USING "btree" ("mitglied_id");



CREATE INDEX "ix_mitglied_arbeitsstunden_altersregel_typ" ON "public"."mitglied" USING "btree" ("arbeitsstunden_altersregel_typ");



CREATE INDEX "ix_mitglied_email" ON "public"."mitglied" USING "btree" ("email");



CREATE INDEX "ix_termin_aktiv_datum" ON "public"."termin" USING "btree" ("aktiv", "datum");



CREATE INDEX "ix_termin_datum" ON "public"."termin" USING "btree" ("datum");



CREATE INDEX "ix_termin_sichtbar_bis" ON "public"."termin" USING "btree" ("sichtbar_bis");



CREATE INDEX "ix_wartungsvertraege_aktiv" ON "public"."wartungsvertraege" USING "btree" ("aktiv");



CREATE INDEX "ix_wartungsvertraege_titel" ON "public"."wartungsvertraege" USING "btree" ("titel");



CREATE INDEX "ix_wvz_hauptmitglied" ON "public"."wartungsvertrag_zuordnungen" USING "btree" ("hauptmitglied_id");



CREATE INDEX "ix_wvz_hauptmitglied_zeitraum" ON "public"."wartungsvertrag_zuordnungen" USING "btree" ("hauptmitglied_id", "gueltig_ab", "gueltig_bis");



CREATE INDEX "ix_wvz_wartungsvertrag" ON "public"."wartungsvertrag_zuordnungen" USING "btree" ("wartungsvertrag_id");



CREATE INDEX "ix_wvz_wartungsvertrag_zeitraum" ON "public"."wartungsvertrag_zuordnungen" USING "btree" ("wartungsvertrag_id", "gueltig_ab", "gueltig_bis");



CREATE INDEX "ix_zaehler_ablesung_art" ON "public"."zaehler_ablesung" USING "btree" ("art");



CREATE INDEX "ix_zaehler_ablesung_zaehler_datum" ON "public"."zaehler_ablesung" USING "btree" ("zaehler_id", "ablesedatum" DESC);



CREATE INDEX "ix_zaehler_eichfaellig_am" ON "public"."zaehler" USING "btree" ("eichfaellig_am");



CREATE INDEX "ix_zaehler_parzelle_medium" ON "public"."zaehler" USING "btree" ("parzelle_id", "medium");



CREATE INDEX "ix_zaehler_parzelle_medium_status" ON "public"."zaehler" USING "btree" ("parzelle_id", "medium", "status");



CREATE INDEX "ix_zaehler_status" ON "public"."zaehler" USING "btree" ("status");



CREATE UNIQUE INDEX "uq_zaehler_ablesung_ausbau_once" ON "public"."zaehler_ablesung" USING "btree" ("zaehler_id") WHERE ("art" = 'ausbau'::"public"."ablesung_art");



CREATE UNIQUE INDEX "uq_zaehler_active_per_parzelle_medium" ON "public"."zaehler" USING "btree" ("parzelle_id", "medium") WHERE (("status" = 'aktiv'::"public"."zaehler_status") AND ("ausgebaut_am" IS NULL));



CREATE UNIQUE INDEX "ux_app_user_mitglied_id" ON "public"."app_user" USING "btree" ("mitglied_id") WHERE ("mitglied_id" IS NOT NULL);



CREATE UNIQUE INDEX "ux_app_user_user_id" ON "public"."app_user" USING "btree" ("user_id");



CREATE UNIQUE INDEX "ux_mitglied_auth_user_id" ON "public"."mitglied" USING "btree" ("auth_user_id") WHERE ("auth_user_id" IS NOT NULL);



CREATE UNIQUE INDEX "ux_saison_jahr" ON "public"."saison" USING "btree" ("jahr") WHERE ("jahr" IS NOT NULL);



CREATE UNIQUE INDEX "ux_wvz_eindeutig" ON "public"."wartungsvertrag_zuordnungen" USING "btree" ("wartungsvertrag_id", "hauptmitglied_id", "gueltig_ab");



CREATE OR REPLACE TRIGGER "trg_arbeitseinsatz_anmeldung_set_updated_at" BEFORE UPDATE ON "public"."arbeitseinsatz_anmeldung" FOR EACH ROW EXECUTE FUNCTION "public"."set_updated_at"();



CREATE OR REPLACE TRIGGER "trg_arbeitseinsatz_anmeldung_validate" BEFORE INSERT OR UPDATE OF "status", "arbeitseinsatz_id" ON "public"."arbeitseinsatz_anmeldung" FOR EACH ROW EXECUTE FUNCTION "public"."trg_arbeitseinsatz_anmeldung_validate"();



CREATE OR REPLACE TRIGGER "trg_arbeitseinsatz_set_updated_at" BEFORE UPDATE ON "public"."arbeitseinsatz" FOR EACH ROW EXECUTE FUNCTION "public"."set_updated_at"();



CREATE OR REPLACE TRIGGER "trg_bekanntmachung_set_updated_at" BEFORE UPDATE ON "public"."bekanntmachung" FOR EACH ROW EXECUTE FUNCTION "public"."set_updated_at"();



CREATE OR REPLACE TRIGGER "trg_impressum_funktion_slot_updated_at" BEFORE UPDATE ON "public"."impressum_funktion_slot" FOR EACH ROW EXECUTE FUNCTION "public"."set_updated_at_impressum_funktion_slot"();



CREATE OR REPLACE TRIGGER "trg_parzelle_validate_rfid_global_unique" BEFORE INSERT OR UPDATE OF "rfid_wasser", "rfid_strom" ON "public"."parzelle" FOR EACH ROW EXECUTE FUNCTION "public"."trg_parzelle_validate_rfid_global_unique"();



CREATE OR REPLACE TRIGGER "trg_set_jahr" BEFORE INSERT ON "public"."saison" FOR EACH ROW EXECUTE FUNCTION "public"."set_jahr_from_id"();



CREATE OR REPLACE TRIGGER "trg_sync_app_user_from_mitglied" AFTER INSERT OR UPDATE OF "auth_user_id" ON "public"."mitglied" FOR EACH ROW EXECUTE FUNCTION "public"."sync_app_user_from_mitglied"();



CREATE OR REPLACE TRIGGER "trg_termin_set_updated_at" BEFORE UPDATE ON "public"."termin" FOR EACH ROW EXECUTE FUNCTION "public"."set_updated_at"();



CREATE OR REPLACE TRIGGER "trg_validate_wartungsvertrag_zuordnung" BEFORE INSERT OR UPDATE ON "public"."wartungsvertrag_zuordnungen" FOR EACH ROW EXECUTE FUNCTION "public"."validate_wartungsvertrag_zuordnung"();



CREATE OR REPLACE TRIGGER "trg_wartungsvertraege_set_updated_at" BEFORE UPDATE ON "public"."wartungsvertraege" FOR EACH ROW EXECUTE FUNCTION "public"."set_updated_at"();



CREATE OR REPLACE TRIGGER "trg_wvz_set_updated_at" BEFORE UPDATE ON "public"."wartungsvertrag_zuordnungen" FOR EACH ROW EXECUTE FUNCTION "public"."set_updated_at"();



CREATE OR REPLACE TRIGGER "trg_zaehler_ablesung_validate" BEFORE INSERT OR UPDATE ON "public"."zaehler_ablesung" FOR EACH ROW EXECUTE FUNCTION "public"."trg_zaehler_ablesung_validate"();



CREATE OR REPLACE TRIGGER "trg_zaehler_set_eichfaellig" BEFORE INSERT OR UPDATE OF "medium", "eichdatum" ON "public"."zaehler" FOR EACH ROW EXECUTE FUNCTION "public"."trg_zaehler_set_eichfaellig"();



CREATE OR REPLACE TRIGGER "trg_zaehler_set_updated_at" BEFORE UPDATE ON "public"."zaehler" FOR EACH ROW EXECUTE FUNCTION "public"."set_updated_at"();



CREATE OR REPLACE TRIGGER "trg_zaehler_validate_medium_allowed" BEFORE INSERT OR UPDATE OF "parzelle_id", "medium" ON "public"."zaehler" FOR EACH ROW EXECUTE FUNCTION "public"."trg_zaehler_validate_medium_allowed"();



CREATE OR REPLACE TRIGGER "trg_zaehler_validate_parzelle_rfid" BEFORE INSERT OR UPDATE OF "parzelle_id", "medium" ON "public"."zaehler" FOR EACH ROW EXECUTE FUNCTION "public"."trg_zaehler_validate_parzelle_rfid"();



ALTER TABLE ONLY "public"."app_user"
    ADD CONSTRAINT "app_user_mitglied_id_fkey" FOREIGN KEY ("mitglied_id") REFERENCES "public"."mitglied"("id") ON DELETE SET NULL;



ALTER TABLE ONLY "public"."arbeitseinsatz_anmeldung"
    ADD CONSTRAINT "arbeitseinsatz_anmeldung_arbeitseinsatz_id_fkey" FOREIGN KEY ("arbeitseinsatz_id") REFERENCES "public"."arbeitseinsatz"("id") ON DELETE CASCADE;



ALTER TABLE ONLY "public"."arbeitseinsatz_anmeldung"
    ADD CONSTRAINT "arbeitseinsatz_anmeldung_mitglied_id_fkey" FOREIGN KEY ("mitglied_id") REFERENCES "public"."mitglied"("id") ON DELETE CASCADE;



ALTER TABLE ONLY "public"."arbeitsstunde"
    ADD CONSTRAINT "arbeitsstunde_genehmigt_von_fkey" FOREIGN KEY ("genehmigt_von") REFERENCES "public"."mitglied"("id");



ALTER TABLE ONLY "public"."arbeitsstunde"
    ADD CONSTRAINT "arbeitsstunde_mitglied_id_fkey" FOREIGN KEY ("mitglied_id") REFERENCES "public"."mitglied"("id");



ALTER TABLE ONLY "public"."arbeitsstunde"
    ADD CONSTRAINT "arbeitsstunde_saison_id_fkey" FOREIGN KEY ("saison_id") REFERENCES "public"."saison"("id") ON DELETE CASCADE;



ALTER TABLE ONLY "public"."dokument"
    ADD CONSTRAINT "dokument_created_by_fkey" FOREIGN KEY ("created_by") REFERENCES "auth"."users"("id");



ALTER TABLE ONLY "public"."dokument"
    ADD CONSTRAINT "dokument_mitglied_id_fkey" FOREIGN KEY ("mitglied_id") REFERENCES "public"."mitglied"("id") ON DELETE CASCADE;



ALTER TABLE ONLY "public"."dokument"
    ADD CONSTRAINT "dokument_parzelle_id_fkey" FOREIGN KEY ("parzelle_id") REFERENCES "public"."parzelle"("id") ON DELETE CASCADE;



ALTER TABLE ONLY "public"."impressum_funktion_slot"
    ADD CONSTRAINT "fk_impressum_funktion_slot_mitglied" FOREIGN KEY ("mitglied_id") REFERENCES "public"."mitglied"("id") ON UPDATE CASCADE ON DELETE SET NULL;



ALTER TABLE ONLY "public"."wartungsvertrag_zuordnungen"
    ADD CONSTRAINT "fk_wvz_hauptmitglied" FOREIGN KEY ("hauptmitglied_id") REFERENCES "public"."mitglied"("id") ON DELETE RESTRICT;



ALTER TABLE ONLY "public"."wartungsvertrag_zuordnungen"
    ADD CONSTRAINT "fk_wvz_wartungsvertrag" FOREIGN KEY ("wartungsvertrag_id") REFERENCES "public"."wartungsvertraege"("id") ON DELETE RESTRICT;



ALTER TABLE ONLY "public"."mitglied"
    ADD CONSTRAINT "mitglied_auth_user_id_fkey" FOREIGN KEY ("auth_user_id") REFERENCES "auth"."users"("id") ON DELETE CASCADE;



ALTER TABLE ONLY "public"."mitglied"
    ADD CONSTRAINT "mitglied_hauptmitglied_id_fkey" FOREIGN KEY ("hauptmitglied_id") REFERENCES "public"."mitglied"("id");



ALTER TABLE ONLY "public"."mitglied_saison"
    ADD CONSTRAINT "mitglied_saison_mitglied_id_fkey" FOREIGN KEY ("mitglied_id") REFERENCES "public"."mitglied"("id");



ALTER TABLE ONLY "public"."mitglied_saison"
    ADD CONSTRAINT "mitglied_saison_saison_id_fkey" FOREIGN KEY ("saison_id") REFERENCES "public"."saison"("id") ON DELETE CASCADE;



ALTER TABLE ONLY "public"."parzellen_belegung"
    ADD CONSTRAINT "parzellen_belegung_mitglied_id_fkey" FOREIGN KEY ("mitglied_id") REFERENCES "public"."mitglied"("id");



ALTER TABLE ONLY "public"."parzellen_belegung"
    ADD CONSTRAINT "parzellen_belegung_parzelle_id_fkey" FOREIGN KEY ("parzelle_id") REFERENCES "public"."parzelle"("id");



ALTER TABLE ONLY "public"."zaehler_ablesung"
    ADD CONSTRAINT "zaehler_ablesung_zaehler_id_fkey" FOREIGN KEY ("zaehler_id") REFERENCES "public"."zaehler"("id") ON DELETE RESTRICT;



ALTER TABLE ONLY "public"."zaehler"
    ADD CONSTRAINT "zaehler_parzelle_id_fkey" FOREIGN KEY ("parzelle_id") REFERENCES "public"."parzelle"("id") ON DELETE RESTRICT;



ALTER TABLE "public"."app_user" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "app_user_admin_full" ON "public"."app_user" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "app_user_select_own_or_admin" ON "public"."app_user" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("user_id" = "auth"."uid"())));



ALTER TABLE "public"."arbeitseinsatz" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "arbeitseinsatz_admin_full" ON "public"."arbeitseinsatz" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



ALTER TABLE "public"."arbeitseinsatz_anmeldung" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "arbeitseinsatz_anmeldung_admin_full" ON "public"."arbeitseinsatz_anmeldung" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "arbeitseinsatz_anmeldung_delete_own_or_admin" ON "public"."arbeitseinsatz_anmeldung" FOR DELETE TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("mitglied_id" = "public"."current_mitglied_id"())));



CREATE POLICY "arbeitseinsatz_anmeldung_insert_own_open" ON "public"."arbeitseinsatz_anmeldung" FOR INSERT TO "authenticated" WITH CHECK (("public"."is_admin_or_vorstand"() OR (("public"."current_mitglied_id"() IS NOT NULL) AND ("mitglied_id" = "public"."current_mitglied_id"()) AND (EXISTS ( SELECT 1
   FROM "public"."arbeitseinsatz" "a"
  WHERE (("a"."id" = "arbeitseinsatz_anmeldung"."arbeitseinsatz_id") AND ("a"."aktiv" = true) AND (("a"."sichtbar_ab" IS NULL) OR ("a"."sichtbar_ab" <= ("now"())::timestamp without time zone)) AND (("a"."sichtbar_bis" IS NULL) OR ("a"."sichtbar_bis" >= ("now"())::timestamp without time zone)) AND (("a"."anmeldung_bis" IS NULL) OR ("a"."anmeldung_bis" >= ("now"())::timestamp without time zone))))))));



CREATE POLICY "arbeitseinsatz_anmeldung_select_own_or_admin" ON "public"."arbeitseinsatz_anmeldung" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("mitglied_id" = "public"."current_mitglied_id"())));



CREATE POLICY "arbeitseinsatz_anmeldung_update_own_open" ON "public"."arbeitseinsatz_anmeldung" FOR UPDATE TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("mitglied_id" = "public"."current_mitglied_id"()))) WITH CHECK (("public"."is_admin_or_vorstand"() OR (("public"."current_mitglied_id"() IS NOT NULL) AND ("mitglied_id" = "public"."current_mitglied_id"()) AND (EXISTS ( SELECT 1
   FROM "public"."arbeitseinsatz" "a"
  WHERE (("a"."id" = "arbeitseinsatz_anmeldung"."arbeitseinsatz_id") AND ("a"."aktiv" = true) AND (("a"."sichtbar_ab" IS NULL) OR ("a"."sichtbar_ab" <= ("now"())::timestamp without time zone)) AND (("a"."sichtbar_bis" IS NULL) OR ("a"."sichtbar_bis" >= ("now"())::timestamp without time zone)) AND (("a"."anmeldung_bis" IS NULL) OR ("a"."anmeldung_bis" >= ("now"())::timestamp without time zone))))))));



CREATE POLICY "arbeitseinsatz_select_visible_authenticated" ON "public"."arbeitseinsatz" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR (("aktiv" = true) AND (("sichtbar_ab" IS NULL) OR ("sichtbar_ab" <= ("now"())::timestamp without time zone)) AND (("sichtbar_bis" IS NULL) OR ("sichtbar_bis" >= ("now"())::timestamp without time zone)))));



ALTER TABLE "public"."arbeitsstunde" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "arbeitsstunde_admin_full" ON "public"."arbeitsstunde" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "arbeitsstunde_select_own_or_admin" ON "public"."arbeitsstunde" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("mitglied_id" = "public"."current_mitglied_id"())));



ALTER TABLE "public"."auth_allowlist" ENABLE ROW LEVEL SECURITY;


ALTER TABLE "public"."bekanntmachung" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "bekanntmachung_admin_full" ON "public"."bekanntmachung" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "bekanntmachung_select_visible_authenticated" ON "public"."bekanntmachung" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR (("aktiv" = true) AND (("sichtbar_ab" IS NULL) OR ("sichtbar_ab" <= ("now"())::timestamp without time zone)) AND (("sichtbar_bis" IS NULL) OR ("sichtbar_bis" >= ("now"())::timestamp without time zone)))));



ALTER TABLE "public"."client_diagnostics_log" ENABLE ROW LEVEL SECURITY;


ALTER TABLE "public"."dokument" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "dokument_select" ON "public"."dokument" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("mitglied_id" = "public"."current_mitglied_id"()) OR (("parzelle_id" IS NOT NULL) AND (EXISTS ( SELECT 1
   FROM "public"."parzellen_belegung" "b"
  WHERE (("b"."parzelle_id" = "dokument"."parzelle_id") AND ("b"."mitglied_id" = "public"."current_mitglied_id"()) AND (("b"."von_datum" IS NULL) OR ("b"."von_datum" <= ("now"())::"date")) AND (("b"."bis_datum" IS NULL) OR ("b"."bis_datum" >= ("now"())::"date"))))))));



CREATE POLICY "dokument_write" ON "public"."dokument" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



ALTER TABLE "public"."impressum_funktion_slot" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "impressum_funktion_slot_admin_full" ON "public"."impressum_funktion_slot" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "impressum_funktion_slot_select_authenticated" ON "public"."impressum_funktion_slot" FOR SELECT TO "authenticated" USING (true);



ALTER TABLE "public"."mitglied" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "mitglied_admin_full" ON "public"."mitglied" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



ALTER TABLE "public"."mitglied_saison" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "mitglied_saison_admin_full" ON "public"."mitglied_saison" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "mitglied_saison_select_own_or_admin" ON "public"."mitglied_saison" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("mitglied_id" = "public"."current_mitglied_id"())));



CREATE POLICY "mitglied_select_own_or_admin" ON "public"."mitglied" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("id" = "public"."current_mitglied_id"()) OR (("auth_user_id" IS NOT NULL) AND ("auth_user_id" = "auth"."uid"()))));



ALTER TABLE "public"."parzelle" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "parzelle_admin_full" ON "public"."parzelle" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "parzelle_select_assigned_or_admin" ON "public"."parzelle" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR (EXISTS ( SELECT 1
   FROM "public"."parzellen_belegung" "pb"
  WHERE (("pb"."parzelle_id" = "parzelle"."id") AND ("pb"."mitglied_id" = "public"."current_mitglied_id"()))))));



ALTER TABLE "public"."parzellen_belegung" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "parzellen_belegung_admin_full" ON "public"."parzellen_belegung" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "parzellen_belegung_select_own_or_admin" ON "public"."parzellen_belegung" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("mitglied_id" = "public"."current_mitglied_id"())));



ALTER TABLE "public"."saison" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "saison_admin_full" ON "public"."saison" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "saison_select_authenticated" ON "public"."saison" FOR SELECT TO "authenticated" USING (true);



CREATE POLICY "service_role_full_access_client_diagnostics_log" ON "public"."client_diagnostics_log" TO "service_role" USING (true) WITH CHECK (true);



ALTER TABLE "public"."termin" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "termin_admin_full" ON "public"."termin" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "termin_select_visible_authenticated" ON "public"."termin" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR (("aktiv" = true) AND (("sichtbar_ab" IS NULL) OR ("sichtbar_ab" <= ("now"())::timestamp without time zone)) AND (("sichtbar_bis" IS NULL) OR ("sichtbar_bis" >= ("now"())::timestamp without time zone)))));



ALTER TABLE "public"."wartungsvertraege" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "wartungsvertraege_admin_full" ON "public"."wartungsvertraege" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "wartungsvertraege_select_assigned_or_admin" ON "public"."wartungsvertraege" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR (EXISTS ( SELECT 1
   FROM "public"."wartungsvertrag_zuordnungen" "wz"
  WHERE (("wz"."wartungsvertrag_id" = "wartungsvertraege"."id") AND ("wz"."hauptmitglied_id" = "public"."current_mitglied_id"()))))));



ALTER TABLE "public"."wartungsvertrag_zuordnungen" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "wartungsvertrag_zuordnungen_admin_full" ON "public"."wartungsvertrag_zuordnungen" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "wartungsvertrag_zuordnungen_select_own_or_admin" ON "public"."wartungsvertrag_zuordnungen" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR ("hauptmitglied_id" = "public"."current_mitglied_id"())));



ALTER TABLE "public"."zaehler" ENABLE ROW LEVEL SECURITY;


ALTER TABLE "public"."zaehler_ablesung" ENABLE ROW LEVEL SECURITY;


CREATE POLICY "zaehler_ablesung_admin_full" ON "public"."zaehler_ablesung" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "zaehler_ablesung_select_assigned_or_admin" ON "public"."zaehler_ablesung" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR (EXISTS ( SELECT 1
   FROM ("public"."zaehler" "z"
     JOIN "public"."parzellen_belegung" "pb" ON (("pb"."parzelle_id" = "z"."parzelle_id")))
  WHERE (("z"."id" = "zaehler_ablesung"."zaehler_id") AND ("pb"."mitglied_id" = "public"."current_mitglied_id"()))))));



CREATE POLICY "zaehler_admin_full" ON "public"."zaehler" TO "authenticated" USING ("public"."is_admin_or_vorstand"()) WITH CHECK ("public"."is_admin_or_vorstand"());



CREATE POLICY "zaehler_select_assigned_or_admin" ON "public"."zaehler" FOR SELECT TO "authenticated" USING (("public"."is_admin_or_vorstand"() OR (EXISTS ( SELECT 1
   FROM "public"."parzellen_belegung" "pb"
  WHERE (("pb"."parzelle_id" = "zaehler"."parzelle_id") AND ("pb"."mitglied_id" = "public"."current_mitglied_id"()))))));





ALTER PUBLICATION "supabase_realtime" OWNER TO "postgres";


GRANT USAGE ON SCHEMA "public" TO "postgres";
GRANT USAGE ON SCHEMA "public" TO "anon";
GRANT USAGE ON SCHEMA "public" TO "authenticated";
GRANT USAGE ON SCHEMA "public" TO "service_role";
GRANT USAGE ON SCHEMA "public" TO "supabase_auth_admin";

























































































































































GRANT ALL ON TABLE "public"."parzelle" TO "authenticated";
GRANT ALL ON TABLE "public"."parzelle" TO "service_role";



GRANT ALL ON FUNCTION "public"."assign_parzelle_rfid"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_rfid_tag_uid" "text") TO "anon";
GRANT ALL ON FUNCTION "public"."assign_parzelle_rfid"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_rfid_tag_uid" "text") TO "authenticated";
GRANT ALL ON FUNCTION "public"."assign_parzelle_rfid"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_rfid_tag_uid" "text") TO "service_role";



REVOKE ALL ON FUNCTION "public"."before_user_created_allowlist"("event" "jsonb") FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."before_user_created_allowlist"("event" "jsonb") TO "service_role";
GRANT ALL ON FUNCTION "public"."before_user_created_allowlist"("event" "jsonb") TO "supabase_auth_admin";



GRANT ALL ON FUNCTION "public"."calc_eichfaellig_am"("p_medium" "public"."zaehler_medium", "p_eichdatum" "date") TO "anon";
GRANT ALL ON FUNCTION "public"."calc_eichfaellig_am"("p_medium" "public"."zaehler_medium", "p_eichdatum" "date") TO "authenticated";
GRANT ALL ON FUNCTION "public"."calc_eichfaellig_am"("p_medium" "public"."zaehler_medium", "p_eichdatum" "date") TO "service_role";



REVOKE ALL ON FUNCTION "public"."can_access_demo_scope"() FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."can_access_demo_scope"() TO "anon";
GRANT ALL ON FUNCTION "public"."can_access_demo_scope"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."can_access_demo_scope"() TO "service_role";



REVOKE ALL ON FUNCTION "public"."can_access_live_internal_data"() FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."can_access_live_internal_data"() TO "anon";
GRANT ALL ON FUNCTION "public"."can_access_live_internal_data"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."can_access_live_internal_data"() TO "service_role";



GRANT ALL ON TABLE "public"."zaehler" TO "authenticated";
GRANT ALL ON TABLE "public"."zaehler" TO "service_role";



GRANT ALL ON FUNCTION "public"."create_meter_installation"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_zaehlernummer" "text", "p_eichdatum" "date", "p_eingebaut_am" "date") TO "anon";
GRANT ALL ON FUNCTION "public"."create_meter_installation"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_zaehlernummer" "text", "p_eichdatum" "date", "p_eingebaut_am" "date") TO "authenticated";
GRANT ALL ON FUNCTION "public"."create_meter_installation"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium", "p_zaehlernummer" "text", "p_eichdatum" "date", "p_eingebaut_am" "date") TO "service_role";



GRANT ALL ON TABLE "public"."zaehler_ablesung" TO "authenticated";
GRANT ALL ON TABLE "public"."zaehler_ablesung" TO "service_role";



GRANT ALL ON FUNCTION "public"."create_meter_reading"("p_zaehler_id" bigint, "p_stand" numeric, "p_ablesedatum" timestamp without time zone, "p_foto_pfad" "text") TO "anon";
GRANT ALL ON FUNCTION "public"."create_meter_reading"("p_zaehler_id" bigint, "p_stand" numeric, "p_ablesedatum" timestamp without time zone, "p_foto_pfad" "text") TO "authenticated";
GRANT ALL ON FUNCTION "public"."create_meter_reading"("p_zaehler_id" bigint, "p_stand" numeric, "p_ablesedatum" timestamp without time zone, "p_foto_pfad" "text") TO "service_role";



REVOKE ALL ON FUNCTION "public"."current_app_role"() FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."current_app_role"() TO "anon";
GRANT ALL ON FUNCTION "public"."current_app_role"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."current_app_role"() TO "service_role";



REVOKE ALL ON FUNCTION "public"."current_mitglied_id"() FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."current_mitglied_id"() TO "anon";
GRANT ALL ON FUNCTION "public"."current_mitglied_id"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."current_mitglied_id"() TO "service_role";



REVOKE ALL ON FUNCTION "public"."current_user_email"() FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."current_user_email"() TO "anon";
GRANT ALL ON FUNCTION "public"."current_user_email"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."current_user_email"() TO "service_role";



GRANT ALL ON FUNCTION "public"."find_scan_context"("p_rfid_tag_uid" "text") TO "anon";
GRANT ALL ON FUNCTION "public"."find_scan_context"("p_rfid_tag_uid" "text") TO "authenticated";
GRANT ALL ON FUNCTION "public"."find_scan_context"("p_rfid_tag_uid" "text") TO "service_role";



GRANT ALL ON FUNCTION "public"."fn_berechne_pflichtstunden_status"("p_mitglied_id" bigint, "p_saison_id" bigint) TO "anon";
GRANT ALL ON FUNCTION "public"."fn_berechne_pflichtstunden_status"("p_mitglied_id" bigint, "p_saison_id" bigint) TO "authenticated";
GRANT ALL ON FUNCTION "public"."fn_berechne_pflichtstunden_status"("p_mitglied_id" bigint, "p_saison_id" bigint) TO "service_role";



GRANT ALL ON FUNCTION "public"."get_active_meter"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium") TO "anon";
GRANT ALL ON FUNCTION "public"."get_active_meter"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium") TO "authenticated";
GRANT ALL ON FUNCTION "public"."get_active_meter"("p_parzelle_id" bigint, "p_medium" "public"."zaehler_medium") TO "service_role";



GRANT ALL ON FUNCTION "public"."get_hauptmitglied_id"("p_mitglied_id" bigint) TO "anon";
GRANT ALL ON FUNCTION "public"."get_hauptmitglied_id"("p_mitglied_id" bigint) TO "authenticated";
GRANT ALL ON FUNCTION "public"."get_hauptmitglied_id"("p_mitglied_id" bigint) TO "service_role";



GRANT ALL ON FUNCTION "public"."get_user_role"() TO "anon";
GRANT ALL ON FUNCTION "public"."get_user_role"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."get_user_role"() TO "service_role";



REVOKE ALL ON FUNCTION "public"."is_admin"() FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."is_admin"() TO "anon";
GRANT ALL ON FUNCTION "public"."is_admin"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."is_admin"() TO "service_role";



REVOKE ALL ON FUNCTION "public"."is_admin_or_vorstand"() FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."is_admin_or_vorstand"() TO "anon";
GRANT ALL ON FUNCTION "public"."is_admin_or_vorstand"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."is_admin_or_vorstand"() TO "service_role";



REVOKE ALL ON FUNCTION "public"."is_demo_user"() FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."is_demo_user"() TO "anon";
GRANT ALL ON FUNCTION "public"."is_demo_user"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."is_demo_user"() TO "service_role";



REVOKE ALL ON FUNCTION "public"."is_playstore_reviewer"() FROM PUBLIC;
GRANT ALL ON FUNCTION "public"."is_playstore_reviewer"() TO "anon";
GRANT ALL ON FUNCTION "public"."is_playstore_reviewer"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."is_playstore_reviewer"() TO "service_role";



GRANT ALL ON FUNCTION "public"."remove_meter"("p_zaehler_id" bigint, "p_ausgebaut_am" "date", "p_endstand" numeric, "p_ablesedatum" timestamp without time zone, "p_foto_pfad" "text") TO "anon";
GRANT ALL ON FUNCTION "public"."remove_meter"("p_zaehler_id" bigint, "p_ausgebaut_am" "date", "p_endstand" numeric, "p_ablesedatum" timestamp without time zone, "p_foto_pfad" "text") TO "authenticated";
GRANT ALL ON FUNCTION "public"."remove_meter"("p_zaehler_id" bigint, "p_ausgebaut_am" "date", "p_endstand" numeric, "p_ablesedatum" timestamp without time zone, "p_foto_pfad" "text") TO "service_role";



GRANT ALL ON FUNCTION "public"."set_jahr_from_id"() TO "anon";
GRANT ALL ON FUNCTION "public"."set_jahr_from_id"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."set_jahr_from_id"() TO "service_role";



GRANT ALL ON FUNCTION "public"."set_updated_at"() TO "anon";
GRANT ALL ON FUNCTION "public"."set_updated_at"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."set_updated_at"() TO "service_role";



GRANT ALL ON FUNCTION "public"."set_updated_at_impressum_funktion"() TO "anon";
GRANT ALL ON FUNCTION "public"."set_updated_at_impressum_funktion"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."set_updated_at_impressum_funktion"() TO "service_role";



GRANT ALL ON FUNCTION "public"."set_updated_at_impressum_funktion_slot"() TO "anon";
GRANT ALL ON FUNCTION "public"."set_updated_at_impressum_funktion_slot"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."set_updated_at_impressum_funktion_slot"() TO "service_role";



GRANT ALL ON TABLE "public"."arbeitseinsatz_anmeldung" TO "authenticated";
GRANT ALL ON TABLE "public"."arbeitseinsatz_anmeldung" TO "service_role";



GRANT ALL ON FUNCTION "public"."sign_off_from_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) TO "anon";
GRANT ALL ON FUNCTION "public"."sign_off_from_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) TO "authenticated";
GRANT ALL ON FUNCTION "public"."sign_off_from_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) TO "service_role";



GRANT ALL ON FUNCTION "public"."sign_up_for_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) TO "anon";
GRANT ALL ON FUNCTION "public"."sign_up_for_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) TO "authenticated";
GRANT ALL ON FUNCTION "public"."sign_up_for_arbeitseinsatz"("p_arbeitseinsatz_id" bigint, "p_mitglied_id" bigint) TO "service_role";



GRANT ALL ON FUNCTION "public"."sync_app_user_from_mitglied"() TO "anon";
GRANT ALL ON FUNCTION "public"."sync_app_user_from_mitglied"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."sync_app_user_from_mitglied"() TO "service_role";



GRANT ALL ON FUNCTION "public"."trg_arbeitseinsatz_anmeldung_validate"() TO "anon";
GRANT ALL ON FUNCTION "public"."trg_arbeitseinsatz_anmeldung_validate"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."trg_arbeitseinsatz_anmeldung_validate"() TO "service_role";



GRANT ALL ON FUNCTION "public"."trg_parzelle_validate_rfid_global_unique"() TO "anon";
GRANT ALL ON FUNCTION "public"."trg_parzelle_validate_rfid_global_unique"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."trg_parzelle_validate_rfid_global_unique"() TO "service_role";



GRANT ALL ON FUNCTION "public"."trg_zaehler_ablesung_validate"() TO "anon";
GRANT ALL ON FUNCTION "public"."trg_zaehler_ablesung_validate"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."trg_zaehler_ablesung_validate"() TO "service_role";



GRANT ALL ON FUNCTION "public"."trg_zaehler_set_eichfaellig"() TO "anon";
GRANT ALL ON FUNCTION "public"."trg_zaehler_set_eichfaellig"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."trg_zaehler_set_eichfaellig"() TO "service_role";



GRANT ALL ON FUNCTION "public"."trg_zaehler_validate_medium_allowed"() TO "anon";
GRANT ALL ON FUNCTION "public"."trg_zaehler_validate_medium_allowed"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."trg_zaehler_validate_medium_allowed"() TO "service_role";



GRANT ALL ON FUNCTION "public"."trg_zaehler_validate_parzelle_rfid"() TO "anon";
GRANT ALL ON FUNCTION "public"."trg_zaehler_validate_parzelle_rfid"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."trg_zaehler_validate_parzelle_rfid"() TO "service_role";



GRANT ALL ON FUNCTION "public"."try_lock_mitglied"("p_id" integer, "p_user_id" "uuid", "p_timeout_minutes" integer) TO "anon";
GRANT ALL ON FUNCTION "public"."try_lock_mitglied"("p_id" integer, "p_user_id" "uuid", "p_timeout_minutes" integer) TO "authenticated";
GRANT ALL ON FUNCTION "public"."try_lock_mitglied"("p_id" integer, "p_user_id" "uuid", "p_timeout_minutes" integer) TO "service_role";



GRANT ALL ON FUNCTION "public"."validate_wartungsvertrag_zuordnung"() TO "anon";
GRANT ALL ON FUNCTION "public"."validate_wartungsvertrag_zuordnung"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."validate_wartungsvertrag_zuordnung"() TO "service_role";



GRANT ALL ON FUNCTION "public"."who_am_i"() TO "anon";
GRANT ALL ON FUNCTION "public"."who_am_i"() TO "authenticated";
GRANT ALL ON FUNCTION "public"."who_am_i"() TO "service_role";


















GRANT ALL ON TABLE "public"."app_user" TO "authenticated";
GRANT ALL ON TABLE "public"."app_user" TO "service_role";



GRANT ALL ON TABLE "public"."arbeitseinsatz" TO "authenticated";
GRANT ALL ON TABLE "public"."arbeitseinsatz" TO "service_role";



GRANT ALL ON SEQUENCE "public"."arbeitseinsatz_anmeldung_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."arbeitseinsatz_anmeldung_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."arbeitseinsatz_anmeldung_id_seq" TO "service_role";



GRANT ALL ON SEQUENCE "public"."arbeitseinsatz_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."arbeitseinsatz_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."arbeitseinsatz_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."arbeitsstunde" TO "authenticated";
GRANT ALL ON TABLE "public"."arbeitsstunde" TO "service_role";



GRANT ALL ON SEQUENCE "public"."arbeitsstunde_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."arbeitsstunde_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."arbeitsstunde_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."auth_allowlist" TO "service_role";
GRANT SELECT ON TABLE "public"."auth_allowlist" TO "supabase_auth_admin";



GRANT ALL ON TABLE "public"."bekanntmachung" TO "authenticated";
GRANT ALL ON TABLE "public"."bekanntmachung" TO "service_role";



GRANT ALL ON SEQUENCE "public"."bekanntmachung_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."bekanntmachung_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."bekanntmachung_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."client_diagnostics_log" TO "authenticated";
GRANT ALL ON TABLE "public"."client_diagnostics_log" TO "service_role";



GRANT ALL ON TABLE "public"."dokument" TO "authenticated";
GRANT ALL ON TABLE "public"."dokument" TO "service_role";



GRANT ALL ON SEQUENCE "public"."dokument_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."dokument_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."dokument_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."impressum_funktion_slot" TO "authenticated";
GRANT ALL ON TABLE "public"."impressum_funktion_slot" TO "service_role";



GRANT ALL ON SEQUENCE "public"."impressum_funktion_slot_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."impressum_funktion_slot_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."impressum_funktion_slot_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."mitglied" TO "authenticated";
GRANT ALL ON TABLE "public"."mitglied" TO "service_role";



GRANT ALL ON SEQUENCE "public"."mitglied_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."mitglied_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."mitglied_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."mitglied_saison" TO "authenticated";
GRANT ALL ON TABLE "public"."mitglied_saison" TO "service_role";



GRANT ALL ON SEQUENCE "public"."mitglied_saison_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."mitglied_saison_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."mitglied_saison_id_seq" TO "service_role";



GRANT ALL ON SEQUENCE "public"."parzelle_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."parzelle_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."parzelle_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."parzellen_belegung" TO "authenticated";
GRANT ALL ON TABLE "public"."parzellen_belegung" TO "service_role";



GRANT ALL ON SEQUENCE "public"."parzellen_belegung_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."parzellen_belegung_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."parzellen_belegung_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."saison" TO "authenticated";
GRANT ALL ON TABLE "public"."saison" TO "service_role";



GRANT ALL ON SEQUENCE "public"."saison_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."saison_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."saison_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."termin" TO "authenticated";
GRANT ALL ON TABLE "public"."termin" TO "service_role";



GRANT ALL ON SEQUENCE "public"."termin_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."termin_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."termin_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."v_aktive_zaehler" TO "service_role";
GRANT SELECT ON TABLE "public"."v_aktive_zaehler" TO "authenticated";



GRANT ALL ON TABLE "public"."v_pflichtstunden_uebersicht" TO "authenticated";
GRANT ALL ON TABLE "public"."v_pflichtstunden_uebersicht" TO "service_role";



GRANT ALL ON TABLE "public"."v_rfid_scan_context" TO "service_role";
GRANT SELECT ON TABLE "public"."v_rfid_scan_context" TO "authenticated";



GRANT ALL ON TABLE "public"."v_startseite_arbeitseinsatz" TO "service_role";
GRANT SELECT ON TABLE "public"."v_startseite_arbeitseinsatz" TO "authenticated";



GRANT ALL ON TABLE "public"."v_startseite_bekanntmachungen" TO "service_role";
GRANT SELECT ON TABLE "public"."v_startseite_bekanntmachungen" TO "authenticated";



GRANT ALL ON TABLE "public"."v_startseite_termine" TO "service_role";
GRANT SELECT ON TABLE "public"."v_startseite_termine" TO "authenticated";



GRANT ALL ON TABLE "public"."v_zaehler_eichstatus" TO "service_role";
GRANT SELECT ON TABLE "public"."v_zaehler_eichstatus" TO "authenticated";



GRANT ALL ON TABLE "public"."wartungsvertraege" TO "authenticated";
GRANT ALL ON TABLE "public"."wartungsvertraege" TO "service_role";



GRANT ALL ON SEQUENCE "public"."wartungsvertraege_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."wartungsvertraege_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."wartungsvertraege_id_seq" TO "service_role";



GRANT ALL ON TABLE "public"."wartungsvertrag_zuordnungen" TO "authenticated";
GRANT ALL ON TABLE "public"."wartungsvertrag_zuordnungen" TO "service_role";



GRANT ALL ON SEQUENCE "public"."wartungsvertrag_zuordnungen_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."wartungsvertrag_zuordnungen_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."wartungsvertrag_zuordnungen_id_seq" TO "service_role";



GRANT ALL ON SEQUENCE "public"."zaehler_ablesung_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."zaehler_ablesung_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."zaehler_ablesung_id_seq" TO "service_role";



GRANT ALL ON SEQUENCE "public"."zaehler_id_seq" TO "anon";
GRANT ALL ON SEQUENCE "public"."zaehler_id_seq" TO "authenticated";
GRANT ALL ON SEQUENCE "public"."zaehler_id_seq" TO "service_role";









ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON SEQUENCES TO "postgres";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON SEQUENCES TO "anon";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON SEQUENCES TO "authenticated";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON SEQUENCES TO "service_role";






ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON FUNCTIONS TO "postgres";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON FUNCTIONS TO "anon";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON FUNCTIONS TO "authenticated";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON FUNCTIONS TO "service_role";






ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON TABLES TO "postgres";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON TABLES TO "anon";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON TABLES TO "authenticated";
ALTER DEFAULT PRIVILEGES FOR ROLE "postgres" IN SCHEMA "public" GRANT ALL ON TABLES TO "service_role";































drop extension if exists "pg_net";

revoke delete on table "public"."app_user" from "anon";

revoke insert on table "public"."app_user" from "anon";

revoke references on table "public"."app_user" from "anon";

revoke select on table "public"."app_user" from "anon";

revoke trigger on table "public"."app_user" from "anon";

revoke truncate on table "public"."app_user" from "anon";

revoke update on table "public"."app_user" from "anon";

revoke delete on table "public"."arbeitseinsatz" from "anon";

revoke insert on table "public"."arbeitseinsatz" from "anon";

revoke references on table "public"."arbeitseinsatz" from "anon";

revoke select on table "public"."arbeitseinsatz" from "anon";

revoke trigger on table "public"."arbeitseinsatz" from "anon";

revoke truncate on table "public"."arbeitseinsatz" from "anon";

revoke update on table "public"."arbeitseinsatz" from "anon";

revoke delete on table "public"."arbeitseinsatz_anmeldung" from "anon";

revoke insert on table "public"."arbeitseinsatz_anmeldung" from "anon";

revoke references on table "public"."arbeitseinsatz_anmeldung" from "anon";

revoke select on table "public"."arbeitseinsatz_anmeldung" from "anon";

revoke trigger on table "public"."arbeitseinsatz_anmeldung" from "anon";

revoke truncate on table "public"."arbeitseinsatz_anmeldung" from "anon";

revoke update on table "public"."arbeitseinsatz_anmeldung" from "anon";

revoke delete on table "public"."arbeitsstunde" from "anon";

revoke insert on table "public"."arbeitsstunde" from "anon";

revoke references on table "public"."arbeitsstunde" from "anon";

revoke select on table "public"."arbeitsstunde" from "anon";

revoke trigger on table "public"."arbeitsstunde" from "anon";

revoke truncate on table "public"."arbeitsstunde" from "anon";

revoke update on table "public"."arbeitsstunde" from "anon";

revoke delete on table "public"."auth_allowlist" from "anon";

revoke insert on table "public"."auth_allowlist" from "anon";

revoke references on table "public"."auth_allowlist" from "anon";

revoke select on table "public"."auth_allowlist" from "anon";

revoke trigger on table "public"."auth_allowlist" from "anon";

revoke truncate on table "public"."auth_allowlist" from "anon";

revoke update on table "public"."auth_allowlist" from "anon";

revoke delete on table "public"."auth_allowlist" from "authenticated";

revoke insert on table "public"."auth_allowlist" from "authenticated";

revoke references on table "public"."auth_allowlist" from "authenticated";

revoke select on table "public"."auth_allowlist" from "authenticated";

revoke trigger on table "public"."auth_allowlist" from "authenticated";

revoke truncate on table "public"."auth_allowlist" from "authenticated";

revoke update on table "public"."auth_allowlist" from "authenticated";

revoke delete on table "public"."bekanntmachung" from "anon";

revoke insert on table "public"."bekanntmachung" from "anon";

revoke references on table "public"."bekanntmachung" from "anon";

revoke select on table "public"."bekanntmachung" from "anon";

revoke trigger on table "public"."bekanntmachung" from "anon";

revoke truncate on table "public"."bekanntmachung" from "anon";

revoke update on table "public"."bekanntmachung" from "anon";

revoke delete on table "public"."client_diagnostics_log" from "anon";

revoke insert on table "public"."client_diagnostics_log" from "anon";

revoke references on table "public"."client_diagnostics_log" from "anon";

revoke select on table "public"."client_diagnostics_log" from "anon";

revoke trigger on table "public"."client_diagnostics_log" from "anon";

revoke truncate on table "public"."client_diagnostics_log" from "anon";

revoke update on table "public"."client_diagnostics_log" from "anon";

revoke delete on table "public"."dokument" from "anon";

revoke insert on table "public"."dokument" from "anon";

revoke references on table "public"."dokument" from "anon";

revoke select on table "public"."dokument" from "anon";

revoke trigger on table "public"."dokument" from "anon";

revoke truncate on table "public"."dokument" from "anon";

revoke update on table "public"."dokument" from "anon";

revoke delete on table "public"."impressum_funktion_slot" from "anon";

revoke insert on table "public"."impressum_funktion_slot" from "anon";

revoke references on table "public"."impressum_funktion_slot" from "anon";

revoke select on table "public"."impressum_funktion_slot" from "anon";

revoke trigger on table "public"."impressum_funktion_slot" from "anon";

revoke truncate on table "public"."impressum_funktion_slot" from "anon";

revoke update on table "public"."impressum_funktion_slot" from "anon";

revoke delete on table "public"."mitglied" from "anon";

revoke insert on table "public"."mitglied" from "anon";

revoke references on table "public"."mitglied" from "anon";

revoke select on table "public"."mitglied" from "anon";

revoke trigger on table "public"."mitglied" from "anon";

revoke truncate on table "public"."mitglied" from "anon";

revoke update on table "public"."mitglied" from "anon";

revoke delete on table "public"."mitglied_saison" from "anon";

revoke insert on table "public"."mitglied_saison" from "anon";

revoke references on table "public"."mitglied_saison" from "anon";

revoke select on table "public"."mitglied_saison" from "anon";

revoke trigger on table "public"."mitglied_saison" from "anon";

revoke truncate on table "public"."mitglied_saison" from "anon";

revoke update on table "public"."mitglied_saison" from "anon";

revoke delete on table "public"."parzelle" from "anon";

revoke insert on table "public"."parzelle" from "anon";

revoke references on table "public"."parzelle" from "anon";

revoke select on table "public"."parzelle" from "anon";

revoke trigger on table "public"."parzelle" from "anon";

revoke truncate on table "public"."parzelle" from "anon";

revoke update on table "public"."parzelle" from "anon";

revoke delete on table "public"."parzellen_belegung" from "anon";

revoke insert on table "public"."parzellen_belegung" from "anon";

revoke references on table "public"."parzellen_belegung" from "anon";

revoke select on table "public"."parzellen_belegung" from "anon";

revoke trigger on table "public"."parzellen_belegung" from "anon";

revoke truncate on table "public"."parzellen_belegung" from "anon";

revoke update on table "public"."parzellen_belegung" from "anon";

revoke delete on table "public"."saison" from "anon";

revoke insert on table "public"."saison" from "anon";

revoke references on table "public"."saison" from "anon";

revoke select on table "public"."saison" from "anon";

revoke trigger on table "public"."saison" from "anon";

revoke truncate on table "public"."saison" from "anon";

revoke update on table "public"."saison" from "anon";

revoke delete on table "public"."termin" from "anon";

revoke insert on table "public"."termin" from "anon";

revoke references on table "public"."termin" from "anon";

revoke select on table "public"."termin" from "anon";

revoke trigger on table "public"."termin" from "anon";

revoke truncate on table "public"."termin" from "anon";

revoke update on table "public"."termin" from "anon";

revoke delete on table "public"."wartungsvertraege" from "anon";

revoke insert on table "public"."wartungsvertraege" from "anon";

revoke references on table "public"."wartungsvertraege" from "anon";

revoke select on table "public"."wartungsvertraege" from "anon";

revoke trigger on table "public"."wartungsvertraege" from "anon";

revoke truncate on table "public"."wartungsvertraege" from "anon";

revoke update on table "public"."wartungsvertraege" from "anon";

revoke delete on table "public"."wartungsvertrag_zuordnungen" from "anon";

revoke insert on table "public"."wartungsvertrag_zuordnungen" from "anon";

revoke references on table "public"."wartungsvertrag_zuordnungen" from "anon";

revoke select on table "public"."wartungsvertrag_zuordnungen" from "anon";

revoke trigger on table "public"."wartungsvertrag_zuordnungen" from "anon";

revoke truncate on table "public"."wartungsvertrag_zuordnungen" from "anon";

revoke update on table "public"."wartungsvertrag_zuordnungen" from "anon";

revoke delete on table "public"."zaehler" from "anon";

revoke insert on table "public"."zaehler" from "anon";

revoke references on table "public"."zaehler" from "anon";

revoke select on table "public"."zaehler" from "anon";

revoke trigger on table "public"."zaehler" from "anon";

revoke truncate on table "public"."zaehler" from "anon";

revoke update on table "public"."zaehler" from "anon";

revoke delete on table "public"."zaehler_ablesung" from "anon";

revoke insert on table "public"."zaehler_ablesung" from "anon";

revoke references on table "public"."zaehler_ablesung" from "anon";

revoke select on table "public"."zaehler_ablesung" from "anon";

revoke trigger on table "public"."zaehler_ablesung" from "anon";

revoke truncate on table "public"."zaehler_ablesung" from "anon";

revoke update on table "public"."zaehler_ablesung" from "anon";


