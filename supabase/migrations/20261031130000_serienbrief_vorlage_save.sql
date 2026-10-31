create or replace function public.speichere_serienbrief_vorlage(
  p_id bigint, p_name text, p_betreff_vorlage text, p_html_inhalt text, p_aktiv boolean,
  p_exportfelder jsonb default '[]'::jsonb, p_freifelder jsonb default '[]'::jsonb
) returns public.serienbrief_vorlage language plpgsql security invoker set search_path=public,pg_temp as $$
declare v public.serienbrief_vorlage;
begin
  if not public.is_admin_or_vorstand() then raise exception 'Nicht berechtigt'; end if;
  if p_id is null then
    insert into serienbrief_vorlage(name,betreff_vorlage,html_inhalt,aktiv) values(trim(p_name),p_betreff_vorlage,p_html_inhalt,p_aktiv) returning * into v;
  else
    update serienbrief_vorlage set name=trim(p_name),betreff_vorlage=p_betreff_vorlage,html_inhalt=p_html_inhalt,aktiv=p_aktiv where id=p_id returning * into v;
    if not found then raise exception 'Vorlage nicht gefunden'; end if;
    delete from serienbrief_vorlage_exportfeld where serienbrief_vorlage_id=v.id;
    delete from serienbrief_vorlage_freifeld where serienbrief_vorlage_id=v.id;
  end if;
  insert into serienbrief_vorlage_exportfeld(serienbrief_vorlage_id,feldschluessel)
  select v.id, trim(value) from jsonb_array_elements_text(p_exportfelder) value;
  insert into serienbrief_vorlage_freifeld(serienbrief_vorlage_id,schluessel,anzeigename,typ,pflicht,sortierung)
  select v.id,trim(x.schluessel),trim(x.anzeigename),x.typ,x.pflicht,x.sortierung from jsonb_to_recordset(p_freifelder) x(schluessel text,anzeigename text,typ text,pflicht boolean,sortierung integer);
  return v;
end $$;
revoke all on function public.speichere_serienbrief_vorlage(bigint,text,text,text,boolean,jsonb,jsonb) from public,anon;
grant execute on function public.speichere_serienbrief_vorlage(bigint,text,text,text,boolean,jsonb,jsonb) to authenticated;
