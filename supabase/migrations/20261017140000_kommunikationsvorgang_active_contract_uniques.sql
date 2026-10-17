-- K3: Pro fachlichem Vertrag darf es nur einen aktiven Signaturvorgang geben.
create unique index ux_kommunikationsvorgang_active_membership_application
  on public.kommunikationsvorgang (mitglied_id)
  where vorgangstyp = 'vertrag_signatur' and dokumenttyp = 'mitgliedsantrag'
    and status not in ('erledigt', 'abgebrochen');

create unique index ux_kommunikationsvorgang_active_lease_contract
  on public.kommunikationsvorgang (mitglied_id, parzelle_id)
  where vorgangstyp = 'vertrag_signatur' and dokumenttyp = 'pachtvertrag'
    and status not in ('erledigt', 'abgebrochen');
