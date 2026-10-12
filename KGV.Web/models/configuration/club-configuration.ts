export type ClubConfigurationRecord = {
  id: number;
  vereinsname: string | null;
  kurzname: string | null;
  registerangabe: string | null;
  strasse: string | null;
  plz: string | null;
  ort: string | null;
  standard_email: string | null;
  standard_telefon: string | null;
  website: string | null;
  kontoinhaber: string | null;
  bankname: string | null;
  iban: string | null;
  bic: string | null;
  verwendungszweck_mitgliedsantrag: string | null;
  verwendungszweck_pachtvertrag: string | null;
  dokument_ort: string | null;
  standard_hinweistext: string | null;
  datenschutz_text: string | null;
  datenschutz_version: string | null;
  datenschutz_stand: string | null;
  aktiv: boolean;
};

export type ClubConfigurationDraft = Omit<ClubConfigurationRecord, "id"> & { id: number | null };

export const emptyClubConfiguration = (): ClubConfigurationDraft => ({
  id: null, vereinsname: "", kurzname: "", registerangabe: "", strasse: "", plz: "", ort: "", standard_email: "", standard_telefon: "", website: "", kontoinhaber: "", bankname: "", iban: "", bic: "", verwendungszweck_mitgliedsantrag: "", verwendungszweck_pachtvertrag: "", dokument_ort: "", standard_hinweistext: "", datenschutz_text: "", datenschutz_version: "", datenschutz_stand: null, aktiv: true,
});

export function clubConfigurationDraft(record: ClubConfigurationRecord): ClubConfigurationDraft { return { ...record }; }
