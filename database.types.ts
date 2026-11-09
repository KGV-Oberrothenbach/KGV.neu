
export type Json = string | number | boolean | null | { [key: string]: Json | undefined } | Json[]

export type Database = {

  "graphql_public": {
          Tables: {
            [_ in never]: never
          }
          Views: {
            [_ in never]: never
          }
          Functions: {
            "graphql":
{ Args: { "extensions"?: Json,"operationName"?: string,"query"?: string,"variables"?: Json }; Returns: Json
                           }
          }
          Enums: {
            [_ in never]: never
          }
          CompositeTypes: {
            [_ in never]: never
          }
        },"public": {
          Tables: {
            "app_export_column_definition": {
                  Row: {
                    "column_key": string,"einblendbar": boolean,"export_key": string,"id": number,"ist_sortierspalte": boolean,"label_kurz": string,"label_lang": string,"pdf_darstellung": string,"serienbrief_freigegeben": boolean,"serienbrief_gruppe": string | null,"sortierung": number,"standard_sichtbar": boolean
                  }
                  ComputedFields: never
                  Insert: {
                    "column_key": string,"einblendbar"?: boolean,"export_key": string,"id"?: number,"ist_sortierspalte"?: boolean,"label_kurz": string,"label_lang": string,"pdf_darstellung"?: string,"serienbrief_freigegeben"?: boolean,"serienbrief_gruppe"?: string | null,"sortierung"?: number,"standard_sichtbar"?: boolean
                  }
                  Update: {
                    "column_key"?: string,"einblendbar"?: boolean,"export_key"?: string,"id"?: number,"ist_sortierspalte"?: boolean,"label_kurz"?: string,"label_lang"?: string,"pdf_darstellung"?: string,"serienbrief_freigegeben"?: boolean,"serienbrief_gruppe"?: string | null,"sortierung"?: number,"standard_sichtbar"?: boolean
                  }
                  Relationships: [
                    {
      foreignKeyName: "app_export_column_definition_export_key_fkey"
      columns: ["export_key"]
isOneToOne: false
      referencedRelation: "app_export_definition"
      referencedColumns: ["export_key"]
    }
                  ]
                },"app_export_definition": {
                  Row: {
                    "aktiv": boolean,"beschreibung": string | null,"created_at": string,"erlaubt_anzeige": boolean,"erlaubt_csv": boolean,"erlaubt_excel": boolean,"erlaubt_pdf": boolean,"export_key": string,"pdf_format": string | null,"pdf_orientation": string | null,"quelle_name": string,"quelle_typ": string,"serienbrief_erlaubt": boolean,"serienbrief_mitglied_id_feld": string | null,"serienbrief_parzelle_id_feld": string | null,"sortierung": number,"standard_ausgabe": string | null,"standard_sortierung": string | null,"titel": string,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"beschreibung"?: string | null,"created_at"?: string,"erlaubt_anzeige"?: boolean,"erlaubt_csv"?: boolean,"erlaubt_excel"?: boolean,"erlaubt_pdf"?: boolean,"export_key": string,"pdf_format"?: string | null,"pdf_orientation"?: string | null,"quelle_name": string,"quelle_typ": string,"serienbrief_erlaubt"?: boolean,"serienbrief_mitglied_id_feld"?: string | null,"serienbrief_parzelle_id_feld"?: string | null,"sortierung"?: number,"standard_ausgabe"?: string | null,"standard_sortierung"?: string | null,"titel": string,"updated_at"?: string
                  }
                  Update: {
                    "aktiv"?: boolean,"beschreibung"?: string | null,"created_at"?: string,"erlaubt_anzeige"?: boolean,"erlaubt_csv"?: boolean,"erlaubt_excel"?: boolean,"erlaubt_pdf"?: boolean,"export_key"?: string,"pdf_format"?: string | null,"pdf_orientation"?: string | null,"quelle_name"?: string,"quelle_typ"?: string,"serienbrief_erlaubt"?: boolean,"serienbrief_mitglied_id_feld"?: string | null,"serienbrief_parzelle_id_feld"?: string | null,"sortierung"?: number,"standard_ausgabe"?: string | null,"standard_sortierung"?: string | null,"titel"?: string,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"app_export_filter_definition": {
                  Row: {
                    "aktiv": boolean,"export_key": string,"filter_key": string,"id": number,"label": string,"optionen_json": Json | null,"parameter_name": string | null,"pflicht": boolean,"sortierung": number,"standardwert": string | null,"standardwert_json": Json | null,"typ": string,"wert_typ": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"export_key": string,"filter_key": string,"id"?: number,"label": string,"optionen_json"?: Json | null,"parameter_name"?: string | null,"pflicht"?: boolean,"sortierung"?: number,"standardwert"?: string | null,"standardwert_json"?: Json | null,"typ": string,"wert_typ"?: string | null
                  }
                  Update: {
                    "aktiv"?: boolean,"export_key"?: string,"filter_key"?: string,"id"?: number,"label"?: string,"optionen_json"?: Json | null,"parameter_name"?: string | null,"pflicht"?: boolean,"sortierung"?: number,"standardwert"?: string | null,"standardwert_json"?: Json | null,"typ"?: string,"wert_typ"?: string | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "app_export_filter_definition_export_key_fkey"
      columns: ["export_key"]
isOneToOne: false
      referencedRelation: "app_export_definition"
      referencedColumns: ["export_key"]
    }
                  ]
                },"app_setting": {
                  Row: {
                    "bool_value": boolean,"setting_key": string,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "bool_value"?: boolean,"setting_key": string,"updated_at"?: string
                  }
                  Update: {
                    "bool_value"?: boolean,"setting_key"?: string,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"app_user": {
                  Row: {
                    "created_at": string,"is_protected": boolean,"mitglied_id": number | null,"permission_grants": number,"permission_revocations": number,"role": string,"updated_at": string,"user_id": string
                  }
                  ComputedFields: never
                  Insert: {
                    "created_at"?: string,"is_protected"?: boolean,"mitglied_id"?: number | null,"permission_grants"?: number,"permission_revocations"?: number,"role"?: string,"updated_at"?: string,"user_id": string
                  }
                  Update: {
                    "created_at"?: string,"is_protected"?: boolean,"mitglied_id"?: number | null,"permission_grants"?: number,"permission_revocations"?: number,"role"?: string,"updated_at"?: string,"user_id"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "app_user_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "app_user_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "app_user_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"arbeitseinsatz": {
                  Row: {
                    "aktiv": boolean,"anmeldung_bis": string | null,"beschreibung": string | null,"created_at": string,"datum": string,"end_uhrzeit": string | null,"id": number,"max_teilnehmer": number | null,"sichtbar_ab": string | null,"sichtbar_bis": string | null,"start_uhrzeit": string | null,"stunden_wert": number,"titel": string,"treffpunkt": string | null,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"anmeldung_bis"?: string | null,"beschreibung"?: string | null,"created_at"?: string,"datum": string,"end_uhrzeit"?: string | null,"id"?: number,"max_teilnehmer"?: number | null,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"start_uhrzeit"?: string | null,"stunden_wert"?: number,"titel": string,"treffpunkt"?: string | null,"updated_at"?: string
                  }
                  Update: {
                    "aktiv"?: boolean,"anmeldung_bis"?: string | null,"beschreibung"?: string | null,"created_at"?: string,"datum"?: string,"end_uhrzeit"?: string | null,"id"?: number,"max_teilnehmer"?: number | null,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"start_uhrzeit"?: string | null,"stunden_wert"?: number,"titel"?: string,"treffpunkt"?: string | null,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"arbeitseinsatz_anmeldung": {
                  Row: {
                    "angemeldet_am": string,"arbeitseinsatz_id": number,"bemerkung": string | null,"id": number,"mitglied_id": number,"status": Database["public"]['Enums']["arbeitseinsatz_anmeldung_status"],"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "angemeldet_am"?: string,"arbeitseinsatz_id": number,"bemerkung"?: string | null,"id"?: number,"mitglied_id": number,"status"?: Database["public"]['Enums']["arbeitseinsatz_anmeldung_status"],"updated_at"?: string
                  }
                  Update: {
                    "angemeldet_am"?: string,"arbeitseinsatz_id"?: number,"bemerkung"?: string | null,"id"?: number,"mitglied_id"?: number,"status"?: Database["public"]['Enums']["arbeitseinsatz_anmeldung_status"],"updated_at"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "arbeitseinsatz_anmeldung_arbeitseinsatz_id_fkey"
      columns: ["arbeitseinsatz_id"]
isOneToOne: false
      referencedRelation: "arbeitseinsatz"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "arbeitseinsatz_anmeldung_arbeitseinsatz_id_fkey"
      columns: ["arbeitseinsatz_id"]
isOneToOne: false
      referencedRelation: "v_startseite_arbeitseinsatz"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "arbeitseinsatz_anmeldung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "arbeitseinsatz_anmeldung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "arbeitseinsatz_anmeldung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"arbeitsstunde": {
                  Row: {
                    "arbeitseinsatz_anmeldung_id": number | null,"art_der_arbeit": string,"datum": string,"freigegeben": boolean,"genehmigt_am": string | null,"genehmigt_von": number | null,"id": number,"lockat": string | null,"lockedbyuserid": string | null,"mitglied_id": number,"saison_id": number,"status": string | null,"stunden": number
                  }
                  ComputedFields: never
                  Insert: {
                    "arbeitseinsatz_anmeldung_id"?: number | null,"art_der_arbeit": string,"datum": string,"freigegeben"?: boolean,"genehmigt_am"?: string | null,"genehmigt_von"?: number | null,"id"?: number,"lockat"?: string | null,"lockedbyuserid"?: string | null,"mitglied_id": number,"saison_id": number,"status"?: string | null,"stunden": number
                  }
                  Update: {
                    "arbeitseinsatz_anmeldung_id"?: number | null,"art_der_arbeit"?: string,"datum"?: string,"freigegeben"?: boolean,"genehmigt_am"?: string | null,"genehmigt_von"?: number | null,"id"?: number,"lockat"?: string | null,"lockedbyuserid"?: string | null,"mitglied_id"?: number,"saison_id"?: number,"status"?: string | null,"stunden"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "arbeitsstunde_arbeitseinsatz_anmeldung_id_fkey"
      columns: ["arbeitseinsatz_anmeldung_id"]
isOneToOne: false
      referencedRelation: "arbeitseinsatz_anmeldung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "arbeitsstunde_genehmigt_von_fkey"
      columns: ["genehmigt_von"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "arbeitsstunde_genehmigt_von_fkey"
      columns: ["genehmigt_von"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "arbeitsstunde_genehmigt_von_fkey"
      columns: ["genehmigt_von"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "arbeitsstunde_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "arbeitsstunde_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "arbeitsstunde_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "arbeitsstunde_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "saison"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "arbeitsstunde_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["saison_id"]
    }
                  ]
                },"arbeitsstunde_pruefverlauf": {
                  Row: {
                    "aktion": string,"arbeitsstunde_id": number | null,"begruendung": string,"geprueft_am": string,"geprueft_von": number,"id": number,"nachher_snapshot": Json | null,"vorher_snapshot": NonNullable<Json>
                  }
                  ComputedFields: never
                  Insert: {
                    "aktion"?: string,"arbeitsstunde_id"?: number | null,"begruendung"?: string,"geprueft_am"?: string,"geprueft_von"?: number,"id"?: number,"nachher_snapshot"?: Json | null,"vorher_snapshot"?: NonNullable<Json>
                  }
                  Update: {
                    "aktion"?: string,"arbeitsstunde_id"?: number | null,"begruendung"?: string,"geprueft_am"?: string,"geprueft_von"?: number,"id"?: number,"nachher_snapshot"?: Json | null,"vorher_snapshot"?: NonNullable<Json>
                  }
                  Relationships: [

                  ]
                },"auth_allowlist": {
                  Row: {
                    "allow_email_otp": boolean,"allow_google": boolean,"allow_playstore_review": boolean,"created_at": string,"email": string,"is_active": boolean,"note": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "allow_email_otp"?: boolean,"allow_google"?: boolean,"allow_playstore_review"?: boolean,"created_at"?: string,"email": string,"is_active"?: boolean,"note"?: string | null
                  }
                  Update: {
                    "allow_email_otp"?: boolean,"allow_google"?: boolean,"allow_playstore_review"?: boolean,"created_at"?: string,"email"?: string,"is_active"?: boolean,"note"?: string | null
                  }
                  Relationships: [

                  ]
                },"bekanntmachung": {
                  Row: {
                    "aktiv": boolean,"created_at": string,"id": number,"inhalt_html": string,"sichtbar_ab": string | null,"sichtbar_bis": string | null,"sort_order": number | null,"titel": string,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"created_at"?: string,"id"?: number,"inhalt_html": string,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"sort_order"?: number | null,"titel": string,"updated_at"?: string
                  }
                  Update: {
                    "aktiv"?: boolean,"created_at"?: string,"id"?: number,"inhalt_html"?: string,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"sort_order"?: number | null,"titel"?: string,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"browser_edit_lock": {
                  Row: {
                    "client_instance_id": string | null,"entity_id": string,"entity_type": string,"expires_at": string,"locked_at": string,"locked_by_user_id": string
                  }
                  ComputedFields: never
                  Insert: {
                    "client_instance_id"?: string | null,"entity_id": string,"entity_type": string,"expires_at": string,"locked_at"?: string,"locked_by_user_id": string
                  }
                  Update: {
                    "client_instance_id"?: string | null,"entity_id"?: string,"entity_type"?: string,"expires_at"?: string,"locked_at"?: string,"locked_by_user_id"?: string
                  }
                  Relationships: [

                  ]
                },"client_diagnostics_log": {
                  Row: {
                    "app": string,"category": string,"client_request_id": string | null,"created_at": string,"environment": string,"extra": NonNullable<Json>,"has_access_token": boolean | null,"http_status": number | null,"id": string,"level": string,"message": string,"raw_body": string | null,"retry_attempted": boolean | null,"token_length": number | null,"user_id": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "app": string,"category": string,"client_request_id"?: string | null,"created_at"?: string,"environment"?: string,"extra"?: NonNullable<Json>,"has_access_token"?: boolean | null,"http_status"?: number | null,"id"?: string,"level"?: string,"message": string,"raw_body"?: string | null,"retry_attempted"?: boolean | null,"token_length"?: number | null,"user_id"?: string | null
                  }
                  Update: {
                    "app"?: string,"category"?: string,"client_request_id"?: string | null,"created_at"?: string,"environment"?: string,"extra"?: NonNullable<Json>,"has_access_token"?: boolean | null,"http_status"?: number | null,"id"?: string,"level"?: string,"message"?: string,"raw_body"?: string | null,"retry_attempted"?: boolean | null,"token_length"?: number | null,"user_id"?: string | null
                  }
                  Relationships: [

                  ]
                },"dokument": {
                  Row: {
                    "archiviert_at": string | null,"archiviert_begruendung": string | null,"archiviert_by": string | null,"bucket": string,"created_at": string,"created_by": string | null,"dateiname": string | null,"dokumentdatum": string | null,"dokumenttyp_id": number | null,"drive_file_id": string | null,"historical_contract_type": string | null,"historische_vertragsnummer": number | null,"historische_vertragsnummer_pdf_gestempelt_am": string | null,"id": number,"mime_type": string | null,"mitglied_id": number | null,"parzelle_id": number | null,"size_bytes": number | null,"source_kommunikationsvorgang_id": number | null,"source_revision": number | null,"source_wartungsvertrag_zuordnung_id": number | null,"storage_path": string,"titel": string | null,"unterzeichnet": boolean | null,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "archiviert_at"?: string | null,"archiviert_begruendung"?: string | null,"archiviert_by"?: string | null,"bucket"?: string,"created_at"?: string,"created_by"?: string | null,"dateiname"?: string | null,"dokumentdatum"?: string | null,"dokumenttyp_id"?: number | null,"drive_file_id"?: string | null,"historical_contract_type"?: string | null,"historische_vertragsnummer"?: number | null,"historische_vertragsnummer_pdf_gestempelt_am"?: string | null,"id"?: number,"mime_type"?: string | null,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"size_bytes"?: number | null,"source_kommunikationsvorgang_id"?: number | null,"source_revision"?: number | null,"source_wartungsvertrag_zuordnung_id"?: number | null,"storage_path": string,"titel"?: string | null,"unterzeichnet"?: boolean | null,"updated_at"?: string
                  }
                  Update: {
                    "archiviert_at"?: string | null,"archiviert_begruendung"?: string | null,"archiviert_by"?: string | null,"bucket"?: string,"created_at"?: string,"created_by"?: string | null,"dateiname"?: string | null,"dokumentdatum"?: string | null,"dokumenttyp_id"?: number | null,"drive_file_id"?: string | null,"historical_contract_type"?: string | null,"historische_vertragsnummer"?: number | null,"historische_vertragsnummer_pdf_gestempelt_am"?: string | null,"id"?: number,"mime_type"?: string | null,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"size_bytes"?: number | null,"source_kommunikationsvorgang_id"?: number | null,"source_revision"?: number | null,"source_wartungsvertrag_zuordnung_id"?: number | null,"storage_path"?: string,"titel"?: string | null,"unterzeichnet"?: boolean | null,"updated_at"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "dokument_dokumenttyp_id_fkey"
      columns: ["dokumenttyp_id"]
isOneToOne: false
      referencedRelation: "dokumenttyp"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "dokument_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "dokument_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "dokument_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "dokument_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "dokument_source_kommunikationsvorgang_id_fkey"
      columns: ["source_kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "dokument_source_kommunikationsvorgang_id_fkey"
      columns: ["source_kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    },{
      foreignKeyName: "dokument_source_wvz_fk"
      columns: ["source_wartungsvertrag_zuordnung_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["wartungsvertrag_zuordnung_id"]
    },{
      foreignKeyName: "dokument_source_wvz_fk"
      columns: ["source_wartungsvertrag_zuordnung_id"]
isOneToOne: false
      referencedRelation: "wartungsvertrag_zuordnungen"
      referencedColumns: ["id"]
    }
                  ]
                },"dokument_wartungsvertrag_zuordnung": {
                  Row: {
                    "dokument_id": number,"wartungsvertrag_zuordnung_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "dokument_id": number,"wartungsvertrag_zuordnung_id": number
                  }
                  Update: {
                    "dokument_id"?: number,"wartungsvertrag_zuordnung_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "dokument_wartungsvertrag_zuor_wartungsvertrag_zuordnung_id_fkey"
      columns: ["wartungsvertrag_zuordnung_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["wartungsvertrag_zuordnung_id"]
    },{
      foreignKeyName: "dokument_wartungsvertrag_zuor_wartungsvertrag_zuordnung_id_fkey"
      columns: ["wartungsvertrag_zuordnung_id"]
isOneToOne: false
      referencedRelation: "wartungsvertrag_zuordnungen"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "dokument_wartungsvertrag_zuordnung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "dokument_wartungsvertrag_zuordnung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "dokument_wartungsvertrag_zuordnung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "dokument_wartungsvertrag_zuordnung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    }
                  ]
                },"dokumenttyp": {
                  Row: {
                    "aktiv": boolean,"bezeichnung": string,"code": string,"created_at": string,"created_by": string | null,"id": number,"sortierung": number,"updated_at": string,"updated_by": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"bezeichnung": string,"code": string,"created_at"?: string,"created_by"?: string | null,"id"?: number,"sortierung"?: number,"updated_at"?: string,"updated_by"?: string | null
                  }
                  Update: {
                    "aktiv"?: boolean,"bezeichnung"?: string,"code"?: string,"created_at"?: string,"created_by"?: string | null,"id"?: number,"sortierung"?: number,"updated_at"?: string,"updated_by"?: string | null
                  }
                  Relationships: [

                  ]
                },"dokumenttyp_wirkung": {
                  Row: {
                    "dokumenttyp_id": number,"wirkung": string
                  }
                  ComputedFields: never
                  Insert: {
                    "dokumenttyp_id": number,"wirkung": string
                  }
                  Update: {
                    "dokumenttyp_id"?: number,"wirkung"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "dokumenttyp_wirkung_dokumenttyp_id_fkey"
      columns: ["dokumenttyp_id"]
isOneToOne: false
      referencedRelation: "dokumenttyp"
      referencedColumns: ["id"]
    }
                  ]
                },"fachnummer_reservierung": {
                  Row: {
                    "art": string,"nummer": number,"quelle_art": string,"quelle_id": number,"reserviert_am": string,"reserviert_von": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "art": string,"nummer": number,"quelle_art": string,"quelle_id": number,"reserviert_am"?: string,"reserviert_von"?: string | null
                  }
                  Update: {
                    "art"?: string,"nummer"?: number,"quelle_art"?: string,"quelle_id"?: number,"reserviert_am"?: string,"reserviert_von"?: string | null
                  }
                  Relationships: [

                  ]
                },"hauptzaehler_messstelle": {
                  Row: {
                    "anlagenbereich": string,"created_at": string,"id": number,"medium": string,"rfid_tag_uid": string | null,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "anlagenbereich": string,"created_at"?: string,"id"?: number,"medium": string,"rfid_tag_uid"?: string | null,"updated_at"?: string
                  }
                  Update: {
                    "anlagenbereich"?: string,"created_at"?: string,"id"?: number,"medium"?: string,"rfid_tag_uid"?: string | null,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"historical_contract_operation": {
                  Row: {
                    "contract_type": string,"created_at": string,"created_by": string,"document_id": number | null,"drive_file_id": string,"file_name": string,"mitglied_id": number,"operation_id": string,"registration_request": Json | null,"registration_result": Json | null,"upload_request": NonNullable<Json>
                  }
                  ComputedFields: never
                  Insert: {
                    "contract_type": string,"created_at"?: string,"created_by": string,"document_id"?: number | null,"drive_file_id": string,"file_name": string,"mitglied_id": number,"operation_id": string,"registration_request"?: Json | null,"registration_result"?: Json | null,"upload_request": NonNullable<Json>
                  }
                  Update: {
                    "contract_type"?: string,"created_at"?: string,"created_by"?: string,"document_id"?: number | null,"drive_file_id"?: string,"file_name"?: string,"mitglied_id"?: number,"operation_id"?: string,"registration_request"?: Json | null,"registration_result"?: Json | null,"upload_request"?: NonNullable<Json>
                  }
                  Relationships: [
                    {
      foreignKeyName: "historical_contract_operation_document_id_fkey"
      columns: ["document_id"]
isOneToOne: true
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "historical_contract_operation_document_id_fkey"
      columns: ["document_id"]
isOneToOne: true
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "historical_contract_operation_document_id_fkey"
      columns: ["document_id"]
isOneToOne: true
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "historical_contract_operation_document_id_fkey"
      columns: ["document_id"]
isOneToOne: true
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "historical_contract_operation_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "historical_contract_operation_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "historical_contract_operation_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"historical_contract_stamp_lease": {
                  Row: {
                    "document_id": number,"expires_at": string,"stamp_marker": string,"token": string
                  }
                  ComputedFields: never
                  Insert: {
                    "document_id": number,"expires_at": string,"stamp_marker"?: string,"token": string
                  }
                  Update: {
                    "document_id"?: number,"expires_at"?: string,"stamp_marker"?: string,"token"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "historical_contract_stamp_lease_document_id_fkey"
      columns: ["document_id"]
isOneToOne: true
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "historical_contract_stamp_lease_document_id_fkey"
      columns: ["document_id"]
isOneToOne: true
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "historical_contract_stamp_lease_document_id_fkey"
      columns: ["document_id"]
isOneToOne: true
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "historical_contract_stamp_lease_document_id_fkey"
      columns: ["document_id"]
isOneToOne: true
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    }
                  ]
                },"impressum_funktion_slot": {
                  Row: {
                    "created_at": string,"funktion": string,"id": number,"mitglied_id": number | null,"slot_key": string,"sort_order": number,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "created_at"?: string,"funktion": string,"id"?: number,"mitglied_id"?: number | null,"slot_key": string,"sort_order": number,"updated_at"?: string
                  }
                  Update: {
                    "created_at"?: string,"funktion"?: string,"id"?: number,"mitglied_id"?: number | null,"slot_key"?: string,"sort_order"?: number,"updated_at"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "fk_impressum_funktion_slot_mitglied"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "fk_impressum_funktion_slot_mitglied"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "fk_impressum_funktion_slot_mitglied"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"jahresabschluss": {
                  Row: {
                    "abgeschlossen_am": string | null,"abgeschlossen_von": number | null,"bemerkung": string | null,"berechnung_version": string,"erstellt_am": string,"erstellt_von": number | null,"id": number,"saison_id": number,"status": string
                  }
                  ComputedFields: never
                  Insert: {
                    "abgeschlossen_am"?: string | null,"abgeschlossen_von"?: number | null,"bemerkung"?: string | null,"berechnung_version"?: string,"erstellt_am"?: string,"erstellt_von"?: number | null,"id"?: number,"saison_id": number,"status"?: string
                  }
                  Update: {
                    "abgeschlossen_am"?: string | null,"abgeschlossen_von"?: number | null,"bemerkung"?: string | null,"berechnung_version"?: string,"erstellt_am"?: string,"erstellt_von"?: number | null,"id"?: number,"saison_id"?: number,"status"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "jahresabschluss_abgeschlossen_von_fkey"
      columns: ["abgeschlossen_von"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_abgeschlossen_von_fkey"
      columns: ["abgeschlossen_von"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_abgeschlossen_von_fkey"
      columns: ["abgeschlossen_von"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: true
      referencedRelation: "saison"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: true
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["saison_id"]
    }
                  ]
                },"jahresabschluss_korrektur": {
                  Row: {
                    "abgeschlossen_am": string | null,"abgeschlossen_von": number | null,"begruendung": string,"erstellt_am": string,"erstellt_von": number | null,"id": number,"saison_id": number,"status": string,"ursprungsabschluss_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "abgeschlossen_am"?: string | null,"abgeschlossen_von"?: number | null,"begruendung": string,"erstellt_am"?: string,"erstellt_von"?: number | null,"id"?: number,"saison_id": number,"status"?: string,"ursprungsabschluss_id": number
                  }
                  Update: {
                    "abgeschlossen_am"?: string | null,"abgeschlossen_von"?: number | null,"begruendung"?: string,"erstellt_am"?: string,"erstellt_von"?: number | null,"id"?: number,"saison_id"?: number,"status"?: string,"ursprungsabschluss_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "jahresabschluss_korrektur_abgeschlossen_von_fkey"
      columns: ["abgeschlossen_von"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_abgeschlossen_von_fkey"
      columns: ["abgeschlossen_von"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_abgeschlossen_von_fkey"
      columns: ["abgeschlossen_von"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "saison"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["saison_id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_ursprungsabschluss_id_fkey"
      columns: ["ursprungsabschluss_id"]
isOneToOne: false
      referencedRelation: "jahresabschluss"
      referencedColumns: ["id"]
    }
                  ]
                },"jahresabschluss_korrektur_position": {
                  Row: {
                    "bemerkung": string | null,"betrag_differenz": number,"id": number,"korrektur_id": number,"mitglied_id": number | null,"parzelle_id": number | null,"position_typ": string,"ursprungsposition_id": number | null
                  }
                  ComputedFields: never
                  Insert: {
                    "bemerkung"?: string | null,"betrag_differenz": number,"id"?: number,"korrektur_id": number,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"position_typ": string,"ursprungsposition_id"?: number | null
                  }
                  Update: {
                    "bemerkung"?: string | null,"betrag_differenz"?: number,"id"?: number,"korrektur_id"?: number,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"position_typ"?: string,"ursprungsposition_id"?: number | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "jahresabschluss_korrektur_position_korrektur_id_fkey"
      columns: ["korrektur_id"]
isOneToOne: false
      referencedRelation: "jahresabschluss_korrektur"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_position_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_position_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_position_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_position_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_korrektur_position_ursprungsposition_id_fkey"
      columns: ["ursprungsposition_id"]
isOneToOne: false
      referencedRelation: "jahresabschluss_position"
      referencedColumns: ["id"]
    }
                  ]
                },"jahresabschluss_position": {
                  Row: {
                    "bemerkung": string | null,"betrag": number,"created_at": string,"einheit": string | null,"id": number,"jahresabschluss_id": number,"menge": number | null,"mitglied_id": number | null,"parzelle_id": number | null,"position_typ": string,"preis": number | null
                  }
                  ComputedFields: never
                  Insert: {
                    "bemerkung"?: string | null,"betrag": number,"created_at"?: string,"einheit"?: string | null,"id"?: number,"jahresabschluss_id": number,"menge"?: number | null,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"position_typ": string,"preis"?: number | null
                  }
                  Update: {
                    "bemerkung"?: string | null,"betrag"?: number,"created_at"?: string,"einheit"?: string | null,"id"?: number,"jahresabschluss_id"?: number,"menge"?: number | null,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"position_typ"?: string,"preis"?: number | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "jahresabschluss_position_jahresabschluss_id_fkey"
      columns: ["jahresabschluss_id"]
isOneToOne: false
      referencedRelation: "jahresabschluss"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_position_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_position_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_position_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_position_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    }
                  ]
                },"jahresabschluss_rechnung": {
                  Row: {
                    "bemerkung": string | null,"erstellt_am": string,"erstellt_von": number | null,"gesamtbetrag": number,"id": number,"leistungs_bis": string | null,"leistungs_von": string | null,"lieferant": string,"rechnungsdatum": string,"rechnungsnummer": string | null,"saison_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "bemerkung"?: string | null,"erstellt_am"?: string,"erstellt_von"?: number | null,"gesamtbetrag": number,"id"?: number,"leistungs_bis"?: string | null,"leistungs_von"?: string | null,"lieferant": string,"rechnungsdatum": string,"rechnungsnummer"?: string | null,"saison_id": number
                  }
                  Update: {
                    "bemerkung"?: string | null,"erstellt_am"?: string,"erstellt_von"?: number | null,"gesamtbetrag"?: number,"id"?: number,"leistungs_bis"?: string | null,"leistungs_von"?: string | null,"lieferant"?: string,"rechnungsdatum"?: string,"rechnungsnummer"?: string | null,"saison_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "jahresabschluss_rechnung_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "saison"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["saison_id"]
    }
                  ]
                },"jahresabschluss_rechnung_zuordnung": {
                  Row: {
                    "bemerkung": string | null,"betrag": number,"created_at": string,"id": number,"kostenart_id": number,"mitglied_id": number | null,"parzelle_id": number | null,"rechnung_id": number,"umlageart_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "bemerkung"?: string | null,"betrag": number,"created_at"?: string,"id"?: number,"kostenart_id": number,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"rechnung_id": number,"umlageart_id": number
                  }
                  Update: {
                    "bemerkung"?: string | null,"betrag"?: number,"created_at"?: string,"id"?: number,"kostenart_id"?: number,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"rechnung_id"?: number,"umlageart_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "jahresabschluss_rechnung_zuordnung_kostenart_id_fkey"
      columns: ["kostenart_id"]
isOneToOne: false
      referencedRelation: "kostenart"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_zuordnung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_zuordnung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_zuordnung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_zuordnung_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_zuordnung_rechnung_id_fkey"
      columns: ["rechnung_id"]
isOneToOne: false
      referencedRelation: "jahresabschluss_rechnung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_rechnung_zuordnung_umlageart_id_fkey"
      columns: ["umlageart_id"]
isOneToOne: false
      referencedRelation: "umlageart"
      referencedColumns: ["id"]
    }
                  ]
                },"jahresabschluss_verbrauch": {
                  Row: {
                    "bis_datum": string,"grund": string,"id": number,"jahresabschluss_position_id": number,"mitglied_id": number | null,"parzelle_id": number,"verbrauch": number,"verbrauchsart": string,"von_datum": string,"zaehler_id": number,"zaehlerstand_ende": number,"zaehlerstand_start": number
                  }
                  ComputedFields: never
                  Insert: {
                    "bis_datum": string,"grund": string,"id"?: number,"jahresabschluss_position_id": number,"mitglied_id"?: number | null,"parzelle_id": number,"verbrauch": number,"verbrauchsart": string,"von_datum": string,"zaehler_id": number,"zaehlerstand_ende": number,"zaehlerstand_start": number
                  }
                  Update: {
                    "bis_datum"?: string,"grund"?: string,"id"?: number,"jahresabschluss_position_id"?: number,"mitglied_id"?: number | null,"parzelle_id"?: number,"verbrauch"?: number,"verbrauchsart"?: string,"von_datum"?: string,"zaehler_id"?: number,"zaehlerstand_ende"?: number,"zaehlerstand_start"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "jahresabschluss_verbrauch_jahresabschluss_position_id_fkey"
      columns: ["jahresabschluss_position_id"]
isOneToOne: false
      referencedRelation: "jahresabschluss_position"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_verbrauch_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_verbrauch_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_verbrauch_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_verbrauch_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_verbrauch_zaehler_id_fkey"
      columns: ["zaehler_id"]
isOneToOne: false
      referencedRelation: "v_aktive_zaehler"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_verbrauch_zaehler_id_fkey"
      columns: ["zaehler_id"]
isOneToOne: false
      referencedRelation: "v_zaehler_eichstatus"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_verbrauch_zaehler_id_fkey"
      columns: ["zaehler_id"]
isOneToOne: false
      referencedRelation: "zaehler"
      referencedColumns: ["id"]
    }
                  ]
                },"jahresabschluss_vorauszahlung": {
                  Row: {
                    "betrag": number,"erstellt_am": string,"erstellt_von": number | null,"id": number,"mitglied_id": number,"parzelle_id": number | null,"saison_id": number,"verwendungszweck": string | null,"zahlungsdatum": string
                  }
                  ComputedFields: never
                  Insert: {
                    "betrag": number,"erstellt_am"?: string,"erstellt_von"?: number | null,"id"?: number,"mitglied_id": number,"parzelle_id"?: number | null,"saison_id": number,"verwendungszweck"?: string | null,"zahlungsdatum": string
                  }
                  Update: {
                    "betrag"?: number,"erstellt_am"?: string,"erstellt_von"?: number | null,"id"?: number,"mitglied_id"?: number,"parzelle_id"?: number | null,"saison_id"?: number,"verwendungszweck"?: string | null,"zahlungsdatum"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "jahresabschluss_vorauszahlung_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_vorauszahlung_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_vorauszahlung_erstellt_von_fkey"
      columns: ["erstellt_von"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_vorauszahlung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_vorauszahlung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_vorauszahlung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "jahresabschluss_vorauszahlung_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_vorauszahlung_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "saison"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "jahresabschluss_vorauszahlung_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["saison_id"]
    }
                  ]
                },"kommunikationsvorgang": {
                  Row: {
                    "archiviert_am": string | null,"archiviert_von": string | null,"bearbeitet_am": string,"bearbeitet_von": string | null,"betreff": string | null,"dokumenttyp": string,"empfaenger_email": string | null,"entwurf_daten": NonNullable<Json>,"ersetzt_pachtvertragsfassung_id": number | null,"erstellt_am": string,"erstellt_von": string | null,"fehlertext": string | null,"final_dokument_id": number | null,"final_storage_bucket": string | null,"final_storage_path": string | null,"gedruckt_am": string | null,"gedruckt_von": string | null,"id": number,"mailtext": string | null,"mitglied_id": number | null,"parzelle_id": number | null,"revision": number,"signatur_gestartet_am": string | null,"signatur_revision": number | null,"status": string,"versendet_am": string | null,"versendet_von": string | null,"vorgangstyp": string,"vorschau_storage_bucket": string | null,"vorschau_storage_path": string | null,"wartungsvertrag_id": number | null,"wartungsvertrag_zuordnung_id": number | null,"zugewiesen_an": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "archiviert_am"?: string | null,"archiviert_von"?: string | null,"bearbeitet_am"?: string,"bearbeitet_von"?: string | null,"betreff"?: string | null,"dokumenttyp": string,"empfaenger_email"?: string | null,"entwurf_daten"?: NonNullable<Json>,"ersetzt_pachtvertragsfassung_id"?: number | null,"erstellt_am"?: string,"erstellt_von"?: string | null,"fehlertext"?: string | null,"final_dokument_id"?: number | null,"final_storage_bucket"?: string | null,"final_storage_path"?: string | null,"gedruckt_am"?: string | null,"gedruckt_von"?: string | null,"id"?: number,"mailtext"?: string | null,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"revision"?: number,"signatur_gestartet_am"?: string | null,"signatur_revision"?: number | null,"status"?: string,"versendet_am"?: string | null,"versendet_von"?: string | null,"vorgangstyp": string,"vorschau_storage_bucket"?: string | null,"vorschau_storage_path"?: string | null,"wartungsvertrag_id"?: number | null,"wartungsvertrag_zuordnung_id"?: number | null,"zugewiesen_an"?: string | null
                  }
                  Update: {
                    "archiviert_am"?: string | null,"archiviert_von"?: string | null,"bearbeitet_am"?: string,"bearbeitet_von"?: string | null,"betreff"?: string | null,"dokumenttyp"?: string,"empfaenger_email"?: string | null,"entwurf_daten"?: NonNullable<Json>,"ersetzt_pachtvertragsfassung_id"?: number | null,"erstellt_am"?: string,"erstellt_von"?: string | null,"fehlertext"?: string | null,"final_dokument_id"?: number | null,"final_storage_bucket"?: string | null,"final_storage_path"?: string | null,"gedruckt_am"?: string | null,"gedruckt_von"?: string | null,"id"?: number,"mailtext"?: string | null,"mitglied_id"?: number | null,"parzelle_id"?: number | null,"revision"?: number,"signatur_gestartet_am"?: string | null,"signatur_revision"?: number | null,"status"?: string,"versendet_am"?: string | null,"versendet_von"?: string | null,"vorgangstyp"?: string,"vorschau_storage_bucket"?: string | null,"vorschau_storage_path"?: string | null,"wartungsvertrag_id"?: number | null,"wartungsvertrag_zuordnung_id"?: number | null,"zugewiesen_an"?: string | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "kommunikationsvorgang_ersetzt_pachtvertragsfassung_id_fkey"
      columns: ["ersetzt_pachtvertragsfassung_id"]
isOneToOne: false
      referencedRelation: "pachtvertragsfassung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "kommunikationsvorgang_ersetzt_pachtvertragsfassung_id_fkey"
      columns: ["ersetzt_pachtvertragsfassung_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["pachtvertragsfassung_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_ersetzt_pachtvertragsfassung_id_fkey"
      columns: ["ersetzt_pachtvertragsfassung_id"]
isOneToOne: false
      referencedRelation: "v_pachtvertragsfassung_uebersicht"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "kommunikationsvorgang_ersetzt_pachtvertragsfassung_id_fkey"
      columns: ["ersetzt_pachtvertragsfassung_id"]
isOneToOne: false
      referencedRelation: "v_pachtvertragsfassung_uebersicht"
      referencedColumns: ["nachfolger_fassung_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["final_dokument_id"]
isOneToOne: false
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["final_dokument_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["final_dokument_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["final_dokument_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "kommunikationsvorgang_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "kommunikationsvorgang_wartungsvertrag_id_fkey"
      columns: ["wartungsvertrag_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_fachnummer"
      referencedColumns: ["wartungsvertrag_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_wartungsvertrag_id_fkey"
      columns: ["wartungsvertrag_id"]
isOneToOne: false
      referencedRelation: "wartungsvertraege"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "kommunikationsvorgang_wvz_fk"
      columns: ["wartungsvertrag_zuordnung_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["wartungsvertrag_zuordnung_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_wvz_fk"
      columns: ["wartungsvertrag_zuordnung_id"]
isOneToOne: false
      referencedRelation: "wartungsvertrag_zuordnungen"
      referencedColumns: ["id"]
    }
                  ]
                },"kostenart": {
                  Row: {
                    "aktiv": boolean,"beschreibung": string | null,"bezeichnung": string,"created_at": string,"id": number,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"beschreibung"?: string | null,"bezeichnung": string,"created_at"?: string,"id"?: number,"updated_at"?: string
                  }
                  Update: {
                    "aktiv"?: boolean,"beschreibung"?: string | null,"bezeichnung"?: string,"created_at"?: string,"id"?: number,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"mitglied": {
                  Row: {
                    "adresse": string | null,"aktiv": boolean,"arbeitsstunden_altersregel_typ": string,"aufnahme_status": string,"auth_user_id": string | null,"bemerkung": string | null,"email": string | null,"email_info_einwilligung": boolean,"email_rechnung_einwilligung": boolean,"geburtsdatum": string | null,"handy": string | null,"hauptmitglied_id": number | null,"id": number,"ist_kgv": boolean,"lockat": string | null,"lockedbyuserid": string | null,"mitglied_ende": string | null,"mitglied_seit": string | null,"mitgliedsnummer": number | null,"name": string,"ort": string | null,"plz": string | null,"regelwerke_versandt_am": string | null,"role": string,"telefon": string | null,"vorname": string,"whatsapp_einwilligung": boolean
                  }
                  ComputedFields: never
                  Insert: {
                    "adresse"?: string | null,"aktiv"?: boolean,"arbeitsstunden_altersregel_typ"?: string,"aufnahme_status"?: string,"auth_user_id"?: string | null,"bemerkung"?: string | null,"email"?: string | null,"email_info_einwilligung"?: boolean,"email_rechnung_einwilligung"?: boolean,"geburtsdatum"?: string | null,"handy"?: string | null,"hauptmitglied_id"?: number | null,"id"?: number,"ist_kgv"?: boolean,"lockat"?: string | null,"lockedbyuserid"?: string | null,"mitglied_ende"?: string | null,"mitglied_seit"?: string | null,"mitgliedsnummer"?: never,"name": string,"ort"?: string | null,"plz"?: string | null,"regelwerke_versandt_am"?: string | null,"role"?: string,"telefon"?: string | null,"vorname": string,"whatsapp_einwilligung"?: boolean
                  }
                  Update: {
                    "adresse"?: string | null,"aktiv"?: boolean,"arbeitsstunden_altersregel_typ"?: string,"aufnahme_status"?: string,"auth_user_id"?: string | null,"bemerkung"?: string | null,"email"?: string | null,"email_info_einwilligung"?: boolean,"email_rechnung_einwilligung"?: boolean,"geburtsdatum"?: string | null,"handy"?: string | null,"hauptmitglied_id"?: number | null,"id"?: number,"ist_kgv"?: boolean,"lockat"?: string | null,"lockedbyuserid"?: string | null,"mitglied_ende"?: string | null,"mitglied_seit"?: string | null,"mitgliedsnummer"?: never,"name"?: string,"ort"?: string | null,"plz"?: string | null,"regelwerke_versandt_am"?: string | null,"role"?: string,"telefon"?: string | null,"vorname"?: string,"whatsapp_einwilligung"?: boolean
                  }
                  Relationships: [
                    {
      foreignKeyName: "mitglied_hauptmitglied_id_fkey"
      columns: ["hauptmitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitglied_hauptmitglied_id_fkey"
      columns: ["hauptmitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "mitglied_hauptmitglied_id_fkey"
      columns: ["hauptmitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"mitglied_historische_korrektur": {
                  Row: {
                    "bisheriger_wert": string | null,"dokument_id": number,"feld": string,"id": number,"korrigiert_am": string,"korrigiert_von": string | null,"korrigierter_wert": string,"mitglied_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "bisheriger_wert"?: string | null,"dokument_id": number,"feld": string,"id"?: number,"korrigiert_am"?: string,"korrigiert_von"?: string | null,"korrigierter_wert": string,"mitglied_id": number
                  }
                  Update: {
                    "bisheriger_wert"?: string | null,"dokument_id"?: number,"feld"?: string,"id"?: number,"korrigiert_am"?: string,"korrigiert_von"?: string | null,"korrigierter_wert"?: string,"mitglied_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "mitglied_historische_korrektur_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitglied_historische_korrektur_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "mitglied_historische_korrektur_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "mitglied_historische_korrektur_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "mitglied_historische_korrektur_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitglied_historische_korrektur_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "mitglied_historische_korrektur_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"mitglied_saison": {
                  Row: {
                    "beitrag": number,"id": number,"mitglied_id": number,"pflichtstunden": number,"saison_id": number,"status": number
                  }
                  ComputedFields: never
                  Insert: {
                    "beitrag"?: number,"id"?: number,"mitglied_id": number,"pflichtstunden"?: number,"saison_id": number,"status"?: number
                  }
                  Update: {
                    "beitrag"?: number,"id"?: number,"mitglied_id"?: number,"pflichtstunden"?: number,"saison_id"?: number,"status"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "mitglied_saison_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitglied_saison_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "mitglied_saison_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "mitglied_saison_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "saison"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitglied_saison_saison_id_fkey"
      columns: ["saison_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["saison_id"]
    }
                  ]
                },"mitglied_vereinsdokument_bereitstellung": {
                  Row: {
                    "bereitgestellt_am": string,"bereitgestellt_von": string | null,"bereitstellungsart": string,"created_at": string,"id": number,"mitglied_id": number,"storniert_am": string | null,"storniert_von": string | null,"storno_grund": string | null,"vereinsdokument_fassung_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "bereitgestellt_am": string,"bereitgestellt_von"?: string | null,"bereitstellungsart": string,"created_at"?: string,"id"?: number,"mitglied_id": number,"storniert_am"?: string | null,"storniert_von"?: string | null,"storno_grund"?: string | null,"vereinsdokument_fassung_id": number
                  }
                  Update: {
                    "bereitgestellt_am"?: string,"bereitgestellt_von"?: string | null,"bereitstellungsart"?: string,"created_at"?: string,"id"?: number,"mitglied_id"?: number,"storniert_am"?: string | null,"storniert_von"?: string | null,"storno_grund"?: string | null,"vereinsdokument_fassung_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "mitglied_vereinsdokument_bereit_vereinsdokument_fassung_id_fkey"
      columns: ["vereinsdokument_fassung_id"]
isOneToOne: false
      referencedRelation: "vereinsdokument_fassung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitglied_vereinsdokument_bereitstellung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitglied_vereinsdokument_bereitstellung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "mitglied_vereinsdokument_bereitstellung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"mitgliedsaufnahme_abstimmung_snapshot_berechtigung": {
                  Row: {
                    "berechtigungsart": string,"funktion_schluessel_snapshot": string,"funktionsbezeichnung_snapshot": string,"id": number,"snapshot_person_id": number,"vereinsfunktion_besetzung_id": number,"vereinsfunktion_id": number,"vertretene_besetzung_id": number | null,"vertretene_funktionsbezeichnung_snapshot": string | null,"vertretene_vereinsfunktion_id": number | null,"vertretung_id": number | null
                  }
                  ComputedFields: never
                  Insert: {
                    "berechtigungsart": string,"funktion_schluessel_snapshot": string,"funktionsbezeichnung_snapshot": string,"id"?: number,"snapshot_person_id": number,"vereinsfunktion_besetzung_id": number,"vereinsfunktion_id": number,"vertretene_besetzung_id"?: number | null,"vertretene_funktionsbezeichnung_snapshot"?: string | null,"vertretene_vereinsfunktion_id"?: number | null,"vertretung_id"?: number | null
                  }
                  Update: {
                    "berechtigungsart"?: string,"funktion_schluessel_snapshot"?: string,"funktionsbezeichnung_snapshot"?: string,"id"?: number,"snapshot_person_id"?: number,"vereinsfunktion_besetzung_id"?: number,"vereinsfunktion_id"?: number,"vertretene_besetzung_id"?: number | null,"vertretene_funktionsbezeichnung_snapshot"?: string | null,"vertretene_vereinsfunktion_id"?: number | null,"vertretung_id"?: number | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "mitgliedsaufnahme_abstimmung__vereinsfunktion_besetzung_id_fkey"
      columns: ["vereinsfunktion_besetzung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_besetzung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_abstimmung__vertretene_vereinsfunktion_i_fkey"
      columns: ["vertretene_vereinsfunktion_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_abstimmung_snaps_vertretene_besetzung_id_fkey"
      columns: ["vertretene_besetzung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_besetzung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_abstimmung_snapshot_b_snapshot_person_id_fkey"
      columns: ["snapshot_person_id"]
isOneToOne: false
      referencedRelation: "mitgliedsaufnahme_abstimmung_snapshot_person"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_abstimmung_snapshot_b_vereinsfunktion_id_fkey"
      columns: ["vereinsfunktion_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_abstimmung_snapshot_berech_vertretung_id_fkey"
      columns: ["vertretung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_vertretung"
      referencedColumns: ["id"]
    }
                  ]
                },"mitgliedsaufnahme_abstimmung_snapshot_person": {
                  Row: {
                    "id": number,"mitglied_id": number,"name_snapshot": string,"snapshot_am": string,"vorstandsbearbeitung_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "id"?: number,"mitglied_id": number,"name_snapshot": string,"snapshot_am"?: string,"vorstandsbearbeitung_id": number
                  }
                  Update: {
                    "id"?: number,"mitglied_id"?: number,"name_snapshot"?: string,"snapshot_am"?: string,"vorstandsbearbeitung_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "mitgliedsaufnahme_abstimmung_snaps_vorstandsbearbeitung_id_fkey"
      columns: ["vorstandsbearbeitung_id"]
isOneToOne: false
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["bearbeitung_id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_abstimmung_snaps_vorstandsbearbeitung_id_fkey"
      columns: ["vorstandsbearbeitung_id"]
isOneToOne: false
      referencedRelation: "mitgliedsaufnahme_vorstandsbearbeitung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_abstimmung_snapshot_person_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_abstimmung_snapshot_person_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_abstimmung_snapshot_person_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"mitgliedsaufnahme_abstimmung_stimme": {
                  Row: {
                    "abgegeben_am": string,"abgegeben_von_auth_user_id": string,"id": number,"nein_begruendung": string | null,"snapshot_person_id": number,"stimme": string,"version": number
                  }
                  ComputedFields: never
                  Insert: {
                    "abgegeben_am"?: string,"abgegeben_von_auth_user_id": string,"id"?: number,"nein_begruendung"?: string | null,"snapshot_person_id": number,"stimme": string,"version": number
                  }
                  Update: {
                    "abgegeben_am"?: string,"abgegeben_von_auth_user_id"?: string,"id"?: number,"nein_begruendung"?: string | null,"snapshot_person_id"?: number,"stimme"?: string,"version"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "mitgliedsaufnahme_abstimmung_stimme_snapshot_person_id_fkey"
      columns: ["snapshot_person_id"]
isOneToOne: false
      referencedRelation: "mitgliedsaufnahme_abstimmung_snapshot_person"
      referencedColumns: ["id"]
    }
                  ]
                },"mitgliedsaufnahme_vorstandsbearbeitung": {
                  Row: {
                    "created_at": string,"entscheidung": string,"entscheidungsbegruendung": string | null,"entscheidungsweg": string | null,"entschieden_am": string | null,"entschieden_von_auth_user_id": string | null,"gestartet_am": string,"gestartet_von_auth_user_id": string,"id": number,"kommunikationsvorgang_id": number,"sitzungsdatum": string | null,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "created_at"?: string,"entscheidung"?: string,"entscheidungsbegruendung"?: string | null,"entscheidungsweg"?: string | null,"entschieden_am"?: string | null,"entschieden_von_auth_user_id"?: string | null,"gestartet_am"?: string,"gestartet_von_auth_user_id": string,"id"?: number,"kommunikationsvorgang_id": number,"sitzungsdatum"?: string | null,"updated_at"?: string
                  }
                  Update: {
                    "created_at"?: string,"entscheidung"?: string,"entscheidungsbegruendung"?: string | null,"entscheidungsweg"?: string | null,"entschieden_am"?: string | null,"entschieden_von_auth_user_id"?: string | null,"gestartet_am"?: string,"gestartet_von_auth_user_id"?: string,"id"?: number,"kommunikationsvorgang_id"?: number,"sitzungsdatum"?: string | null,"updated_at"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "mitgliedsaufnahme_vorstandsbearbe_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_vorstandsbearbe_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    }
                  ]
                },"p11_ap01_test_fixture": {
                  Row: {
                    "data": NonNullable<Json>,"description": string,"fixture_id": string,"rule_ids": (string)[]
                  }
                  ComputedFields: never
                  Insert: {
                    "data": NonNullable<Json>,"description": string,"fixture_id": string,"rule_ids": (string)[]
                  }
                  Update: {
                    "data"?: NonNullable<Json>,"description"?: string,"fixture_id"?: string,"rule_ids"?: (string)[]
                  }
                  Relationships: [

                  ]
                },"pachtverhaeltnis": {
                  Row: {
                    "beendet_am": string | null,"beendigungsgrund": string | null,"created_at": string,"created_by": string | null,"id": number,"parzelle_id": number,"urspruenglicher_pachtbeginn": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "beendet_am"?: string | null,"beendigungsgrund"?: string | null,"created_at"?: string,"created_by"?: string | null,"id"?: number,"parzelle_id": number,"urspruenglicher_pachtbeginn"?: string | null
                  }
                  Update: {
                    "beendet_am"?: string | null,"beendigungsgrund"?: string | null,"created_at"?: string,"created_by"?: string | null,"id"?: number,"parzelle_id"?: number,"urspruenglicher_pachtbeginn"?: string | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "pachtverhaeltnis_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    }
                  ]
                },"pachtverhaeltnis_beteiligung": {
                  Row: {
                    "created_at": string,"created_by": string | null,"gueltig_ab": string,"gueltig_bis": string | null,"id": number,"mitglied_id": number,"pachtverhaeltnis_id": number,"rolle": string
                  }
                  ComputedFields: never
                  Insert: {
                    "created_at"?: string,"created_by"?: string | null,"gueltig_ab": string,"gueltig_bis"?: string | null,"id"?: number,"mitglied_id": number,"pachtverhaeltnis_id": number,"rolle": string
                  }
                  Update: {
                    "created_at"?: string,"created_by"?: string | null,"gueltig_ab"?: string,"gueltig_bis"?: string | null,"id"?: number,"mitglied_id"?: number,"pachtverhaeltnis_id"?: number,"rolle"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "pachtverhaeltnis_beteiligung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtverhaeltnis_beteiligung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "pachtverhaeltnis_beteiligung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "pachtverhaeltnis_beteiligung_pachtverhaeltnis_id_fkey"
      columns: ["pachtverhaeltnis_id"]
isOneToOne: false
      referencedRelation: "pachtverhaeltnis"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtverhaeltnis_beteiligung_pachtverhaeltnis_id_fkey"
      columns: ["pachtverhaeltnis_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["pachtverhaeltnis_id"]
    }
                  ]
                },"pachtvertragsfassung": {
                  Row: {
                    "created_at": string,"created_by": string | null,"dokument_id": number,"fassungstyp": string,"gueltig_bis": string | null,"id": number,"nicht_mehr_massgeblich_am": string | null,"pachtverhaeltnis_id": number,"vertragsdatum": string | null,"vorgaenger_fassung_id": number | null,"vorheriges_vertragsdatum": string | null,"wirksam_ab": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "created_at"?: string,"created_by"?: string | null,"dokument_id": number,"fassungstyp": string,"gueltig_bis"?: string | null,"id"?: number,"nicht_mehr_massgeblich_am"?: string | null,"pachtverhaeltnis_id": number,"vertragsdatum"?: string | null,"vorgaenger_fassung_id"?: number | null,"vorheriges_vertragsdatum"?: string | null,"wirksam_ab"?: string | null
                  }
                  Update: {
                    "created_at"?: string,"created_by"?: string | null,"dokument_id"?: number,"fassungstyp"?: string,"gueltig_bis"?: string | null,"id"?: number,"nicht_mehr_massgeblich_am"?: string | null,"pachtverhaeltnis_id"?: number,"vertragsdatum"?: string | null,"vorgaenger_fassung_id"?: number | null,"vorheriges_vertragsdatum"?: string | null,"wirksam_ab"?: string | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "pachtvertragsfassung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtvertragsfassung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_pachtverhaeltnis_id_fkey"
      columns: ["pachtverhaeltnis_id"]
isOneToOne: false
      referencedRelation: "pachtverhaeltnis"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtvertragsfassung_pachtverhaeltnis_id_fkey"
      columns: ["pachtverhaeltnis_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["pachtverhaeltnis_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_vorgaenger_fassung_id_fkey"
      columns: ["vorgaenger_fassung_id"]
isOneToOne: false
      referencedRelation: "pachtvertragsfassung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtvertragsfassung_vorgaenger_fassung_id_fkey"
      columns: ["vorgaenger_fassung_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["pachtvertragsfassung_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_vorgaenger_fassung_id_fkey"
      columns: ["vorgaenger_fassung_id"]
isOneToOne: false
      referencedRelation: "v_pachtvertragsfassung_uebersicht"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtvertragsfassung_vorgaenger_fassung_id_fkey"
      columns: ["vorgaenger_fassung_id"]
isOneToOne: false
      referencedRelation: "v_pachtvertragsfassung_uebersicht"
      referencedColumns: ["nachfolger_fassung_id"]
    }
                  ]
                },"parzelle": {
                  Row: {
                    "aktiv": boolean,"Anlage": string,"flaeche_qm": number | null,"garten_nr": string,"hat_strom": boolean,"hat_wasser": boolean,"id": number,"ist_vereinsgarten": boolean,"qr_code_strom": string | null,"qr_code_wasser": string | null,"rfid_strom": string | null,"rfid_wasser": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"Anlage": string,"flaeche_qm"?: number | null,"garten_nr": string,"hat_strom"?: boolean,"hat_wasser"?: boolean,"id"?: number,"ist_vereinsgarten"?: boolean,"qr_code_strom"?: string | null,"qr_code_wasser"?: string | null,"rfid_strom"?: string | null,"rfid_wasser"?: string | null
                  }
                  Update: {
                    "aktiv"?: boolean,"Anlage"?: string,"flaeche_qm"?: number | null,"garten_nr"?: string,"hat_strom"?: boolean,"hat_wasser"?: boolean,"id"?: number,"ist_vereinsgarten"?: boolean,"qr_code_strom"?: string | null,"qr_code_wasser"?: string | null,"rfid_strom"?: string | null,"rfid_wasser"?: string | null
                  }
                  Relationships: [

                  ]
                },"parzellen_begehung_feststellung": {
                  Row: {
                    "bemerkung": string | null,"bereich": string | null,"erledigt_am": string | null,"feststellung": string,"frist": string | null,"id": number,"protokoll_id": number,"status": string
                  }
                  ComputedFields: never
                  Insert: {
                    "bemerkung"?: string | null,"bereich"?: string | null,"erledigt_am"?: string | null,"feststellung": string,"frist"?: string | null,"id"?: number,"protokoll_id": number,"status"?: string
                  }
                  Update: {
                    "bemerkung"?: string | null,"bereich"?: string | null,"erledigt_am"?: string | null,"feststellung"?: string,"frist"?: string | null,"id"?: number,"protokoll_id"?: number,"status"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "parzellen_begehung_feststellung_protokoll_id_fkey"
      columns: ["protokoll_id"]
isOneToOne: false
      referencedRelation: "parzellen_protokoll"
      referencedColumns: ["id"]
    }
                  ]
                },"parzellen_belegung": {
                  Row: {
                    "beendigungsgrund": string | null,"bis_datum": string | null,"id": number,"mitglied_id": number,"parzelle_id": number,"von_datum": string
                  }
                  ComputedFields: never
                  Insert: {
                    "beendigungsgrund"?: string | null,"bis_datum"?: string | null,"id"?: number,"mitglied_id": number,"parzelle_id": number,"von_datum": string
                  }
                  Update: {
                    "beendigungsgrund"?: string | null,"bis_datum"?: string | null,"id"?: number,"mitglied_id"?: number,"parzelle_id"?: number,"von_datum"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "parzellen_belegung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_belegung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "parzellen_belegung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "parzellen_belegung_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    }
                  ]
                },"parzellen_protokoll": {
                  Row: {
                    "abgeschlossen_am": string | null,"abgeschlossen_von": string | null,"anlass": string | null,"begleitperson_mitglied_id": number | null,"begleitperson_name": string | null,"begleitperson_signiert_am": string | null,"dokument_id": number | null,"erstellt_am": string,"erstellt_von": string | null,"id": number,"mitglied_id": number | null,"paechter_signiert_am": string | null,"parzelle_id": number,"protokoll_datum": string,"protokoll_typ": string,"status": string,"updated_at": string,"vereinbarung": string | null,"vorstand_mitglied_id": number | null,"vorstand1_signiert_am": string | null,"vorstand2_mitglied_id": number | null,"vorstand2_signiert_am": string | null,"zustand_bemerkung": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "abgeschlossen_am"?: string | null,"abgeschlossen_von"?: string | null,"anlass"?: string | null,"begleitperson_mitglied_id"?: number | null,"begleitperson_name"?: string | null,"begleitperson_signiert_am"?: string | null,"dokument_id"?: number | null,"erstellt_am"?: string,"erstellt_von"?: string | null,"id"?: number,"mitglied_id"?: number | null,"paechter_signiert_am"?: string | null,"parzelle_id": number,"protokoll_datum"?: string,"protokoll_typ": string,"status"?: string,"updated_at"?: string,"vereinbarung"?: string | null,"vorstand_mitglied_id"?: number | null,"vorstand1_signiert_am"?: string | null,"vorstand2_mitglied_id"?: number | null,"vorstand2_signiert_am"?: string | null,"zustand_bemerkung"?: string | null
                  }
                  Update: {
                    "abgeschlossen_am"?: string | null,"abgeschlossen_von"?: string | null,"anlass"?: string | null,"begleitperson_mitglied_id"?: number | null,"begleitperson_name"?: string | null,"begleitperson_signiert_am"?: string | null,"dokument_id"?: number | null,"erstellt_am"?: string,"erstellt_von"?: string | null,"id"?: number,"mitglied_id"?: number | null,"paechter_signiert_am"?: string | null,"parzelle_id"?: number,"protokoll_datum"?: string,"protokoll_typ"?: string,"status"?: string,"updated_at"?: string,"vereinbarung"?: string | null,"vorstand_mitglied_id"?: number | null,"vorstand1_signiert_am"?: string | null,"vorstand2_mitglied_id"?: number | null,"vorstand2_signiert_am"?: string | null,"zustand_bemerkung"?: string | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "parzellen_protokoll_begleitperson_mitglied_id_fkey"
      columns: ["begleitperson_mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_begleitperson_mitglied_id_fkey"
      columns: ["begleitperson_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "parzellen_protokoll_begleitperson_mitglied_id_fkey"
      columns: ["begleitperson_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "parzellen_protokoll_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "parzellen_protokoll_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "parzellen_protokoll_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "parzellen_protokoll_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "parzellen_protokoll_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "parzellen_protokoll_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_vorstand_mitglied_id_fkey"
      columns: ["vorstand_mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_vorstand_mitglied_id_fkey"
      columns: ["vorstand_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "parzellen_protokoll_vorstand_mitglied_id_fkey"
      columns: ["vorstand_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "parzellen_protokoll_vorstand2_mitglied_id_fkey"
      columns: ["vorstand2_mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_vorstand2_mitglied_id_fkey"
      columns: ["vorstand2_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "parzellen_protokoll_vorstand2_mitglied_id_fkey"
      columns: ["vorstand2_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"parzellen_protokoll_ablesung": {
                  Row: {
                    "ablesedatum": string,"ablesung_id": number | null,"foto_dateiname": string | null,"foto_drive_file_id": string | null,"foto_pfad": string | null,"id": number,"medium": string,"protokoll_id": number,"quelle": string,"stand": number,"zaehler_id": number | null,"zaehlernummer": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "ablesedatum": string,"ablesung_id"?: number | null,"foto_dateiname"?: string | null,"foto_drive_file_id"?: string | null,"foto_pfad"?: string | null,"id"?: number,"medium": string,"protokoll_id": number,"quelle": string,"stand": number,"zaehler_id"?: number | null,"zaehlernummer"?: string | null
                  }
                  Update: {
                    "ablesedatum"?: string,"ablesung_id"?: number | null,"foto_dateiname"?: string | null,"foto_drive_file_id"?: string | null,"foto_pfad"?: string | null,"id"?: number,"medium"?: string,"protokoll_id"?: number,"quelle"?: string,"stand"?: number,"zaehler_id"?: number | null,"zaehlernummer"?: string | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "parzellen_protokoll_ablesung_ablesung_id_fkey"
      columns: ["ablesung_id"]
isOneToOne: false
      referencedRelation: "zaehler_ablesung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_ablesung_protokoll_id_fkey"
      columns: ["protokoll_id"]
isOneToOne: false
      referencedRelation: "parzellen_protokoll"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_ablesung_zaehler_id_fkey"
      columns: ["zaehler_id"]
isOneToOne: false
      referencedRelation: "v_aktive_zaehler"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_ablesung_zaehler_id_fkey"
      columns: ["zaehler_id"]
isOneToOne: false
      referencedRelation: "v_zaehler_eichstatus"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "parzellen_protokoll_ablesung_zaehler_id_fkey"
      columns: ["zaehler_id"]
isOneToOne: false
      referencedRelation: "zaehler"
      referencedColumns: ["id"]
    }
                  ]
                },"pdf_template_catalog": {
                  Row: {
                    "aktiv": boolean,"checksum": string,"content_type": string,"created_at": string,"created_by": string | null,"field_spec_checksum": string,"field_spec_path": string,"field_spec_version": string,"id": number,"published_at": string | null,"renderer_version": string,"status": string,"storage_bucket": string,"storage_path": string,"template_key": string,"version": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"checksum": string,"content_type": string,"created_at"?: string,"created_by"?: string | null,"field_spec_checksum": string,"field_spec_path": string,"field_spec_version": string,"id"?: number,"published_at"?: string | null,"renderer_version": string,"status"?: string,"storage_bucket"?: string,"storage_path": string,"template_key": string,"version": string
                  }
                  Update: {
                    "aktiv"?: boolean,"checksum"?: string,"content_type"?: string,"created_at"?: string,"created_by"?: string | null,"field_spec_checksum"?: string,"field_spec_path"?: string,"field_spec_version"?: string,"id"?: number,"published_at"?: string | null,"renderer_version"?: string,"status"?: string,"storage_bucket"?: string,"storage_path"?: string,"template_key"?: string,"version"?: string
                  }
                  Relationships: [

                  ]
                },"prepared_contract_signatur": {
                  Row: {
                    "created_at": string,"id": number,"kommunikationsvorgang_id": number,"mitglied_id": number | null,"pflicht": boolean,"signatur_revision": number,"signatur_schluessel": string,"signatur_sha256": string | null,"sort_order": number,"status": string,"storage_bucket": string | null,"storage_path": string | null,"unterzeichner_name_snapshot": string | null,"unterzeichner_typ": string,"unterzeichnet_am": string | null,"unterzeichnet_von_auth_user_id": string | null,"updated_at": string,"vereinsfunktion_besetzung_id": number | null,"vereinsfunktion_bezeichnung_snapshot": string | null,"vereinsfunktion_id": number | null,"vertretene_besetzung_id": number | null,"vertretene_funktion_bezeichnung_snapshot": string | null,"vertretene_vereinsfunktion_id": number | null,"vertretung_id": number | null
                  }
                  ComputedFields: never
                  Insert: {
                    "created_at"?: string,"id"?: number,"kommunikationsvorgang_id": number,"mitglied_id"?: number | null,"pflicht": boolean,"signatur_revision": number,"signatur_schluessel": string,"signatur_sha256"?: string | null,"sort_order": number,"status"?: string,"storage_bucket"?: string | null,"storage_path"?: string | null,"unterzeichner_name_snapshot"?: string | null,"unterzeichner_typ": string,"unterzeichnet_am"?: string | null,"unterzeichnet_von_auth_user_id"?: string | null,"updated_at"?: string,"vereinsfunktion_besetzung_id"?: number | null,"vereinsfunktion_bezeichnung_snapshot"?: string | null,"vereinsfunktion_id"?: number | null,"vertretene_besetzung_id"?: number | null,"vertretene_funktion_bezeichnung_snapshot"?: string | null,"vertretene_vereinsfunktion_id"?: number | null,"vertretung_id"?: number | null
                  }
                  Update: {
                    "created_at"?: string,"id"?: number,"kommunikationsvorgang_id"?: number,"mitglied_id"?: number | null,"pflicht"?: boolean,"signatur_revision"?: number,"signatur_schluessel"?: string,"signatur_sha256"?: string | null,"sort_order"?: number,"status"?: string,"storage_bucket"?: string | null,"storage_path"?: string | null,"unterzeichner_name_snapshot"?: string | null,"unterzeichner_typ"?: string,"unterzeichnet_am"?: string | null,"unterzeichnet_von_auth_user_id"?: string | null,"updated_at"?: string,"vereinsfunktion_besetzung_id"?: number | null,"vereinsfunktion_bezeichnung_snapshot"?: string | null,"vereinsfunktion_id"?: number | null,"vertretene_besetzung_id"?: number | null,"vertretene_funktion_bezeichnung_snapshot"?: string | null,"vertretene_vereinsfunktion_id"?: number | null,"vertretung_id"?: number | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "prepared_contract_signatur_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    },{
      foreignKeyName: "prepared_contract_signatur_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "prepared_contract_signatur_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "prepared_contract_signatur_vereinsfunktion_besetzung_id_fkey"
      columns: ["vereinsfunktion_besetzung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_besetzung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_vereinsfunktion_id_fkey"
      columns: ["vereinsfunktion_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_vertretene_besetzung_id_fkey"
      columns: ["vertretene_besetzung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_besetzung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_vertretene_vereinsfunktion_id_fkey"
      columns: ["vertretene_vereinsfunktion_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_vertretung_id_fkey"
      columns: ["vertretung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_vertretung"
      referencedColumns: ["id"]
    }
                  ]
                },"prepared_contract_signatur_snapshot_person": {
                  Row: {
                    "id": number,"kommunikationsvorgang_id": number,"mitglied_id": number,"signatur_revision": number,"snapshot_am": string,"unterzeichner_name_snapshot": string
                  }
                  ComputedFields: never
                  Insert: {
                    "id"?: number,"kommunikationsvorgang_id": number,"mitglied_id": number,"signatur_revision": number,"snapshot_am"?: string,"unterzeichner_name_snapshot": string
                  }
                  Update: {
                    "id"?: number,"kommunikationsvorgang_id"?: number,"mitglied_id"?: number,"signatur_revision"?: number,"snapshot_am"?: string,"unterzeichner_name_snapshot"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "prepared_contract_signatur_snapsh_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_snapsh_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    },{
      foreignKeyName: "prepared_contract_signatur_snapshot_person_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_snapshot_person_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "prepared_contract_signatur_snapshot_person_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"prepared_contract_signatur_snapshot_quelle": {
                  Row: {
                    "berechtigungsart": string,"besetzung_gueltig_ab_snapshot": string,"besetzung_gueltig_bis_snapshot": string | null,"id": number,"snapshot_person_id": number,"vereinsfunktion_besetzung_id": number,"vereinsfunktion_bezeichnung_snapshot": string,"vereinsfunktion_id": number,"vertretene_besetzung_gueltig_ab_snapshot": string | null,"vertretene_besetzung_gueltig_bis_snapshot": string | null,"vertretene_besetzung_id": number | null,"vertretene_funktion_bezeichnung_snapshot": string | null,"vertretene_vereinsfunktion_id": number | null,"vertretung_gueltig_ab_snapshot": string | null,"vertretung_gueltig_bis_snapshot": string | null,"vertretung_id": number | null
                  }
                  ComputedFields: never
                  Insert: {
                    "berechtigungsart": string,"besetzung_gueltig_ab_snapshot": string,"besetzung_gueltig_bis_snapshot"?: string | null,"id"?: number,"snapshot_person_id": number,"vereinsfunktion_besetzung_id": number,"vereinsfunktion_bezeichnung_snapshot": string,"vereinsfunktion_id": number,"vertretene_besetzung_gueltig_ab_snapshot"?: string | null,"vertretene_besetzung_gueltig_bis_snapshot"?: string | null,"vertretene_besetzung_id"?: number | null,"vertretene_funktion_bezeichnung_snapshot"?: string | null,"vertretene_vereinsfunktion_id"?: number | null,"vertretung_gueltig_ab_snapshot"?: string | null,"vertretung_gueltig_bis_snapshot"?: string | null,"vertretung_id"?: number | null
                  }
                  Update: {
                    "berechtigungsart"?: string,"besetzung_gueltig_ab_snapshot"?: string,"besetzung_gueltig_bis_snapshot"?: string | null,"id"?: number,"snapshot_person_id"?: number,"vereinsfunktion_besetzung_id"?: number,"vereinsfunktion_bezeichnung_snapshot"?: string,"vereinsfunktion_id"?: number,"vertretene_besetzung_gueltig_ab_snapshot"?: string | null,"vertretene_besetzung_gueltig_bis_snapshot"?: string | null,"vertretene_besetzung_id"?: number | null,"vertretene_funktion_bezeichnung_snapshot"?: string | null,"vertretene_vereinsfunktion_id"?: number | null,"vertretung_gueltig_ab_snapshot"?: string | null,"vertretung_gueltig_bis_snapshot"?: string | null,"vertretung_id"?: number | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "prepared_contract_signatur_sn_vereinsfunktion_besetzung_id_fkey"
      columns: ["vereinsfunktion_besetzung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_besetzung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_sn_vertretene_vereinsfunktion_i_fkey"
      columns: ["vertretene_vereinsfunktion_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_snapsho_vertretene_besetzung_id_fkey"
      columns: ["vertretene_besetzung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_besetzung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_snapshot_que_snapshot_person_id_fkey"
      columns: ["snapshot_person_id"]
isOneToOne: false
      referencedRelation: "prepared_contract_signatur_snapshot_person"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_snapshot_que_vereinsfunktion_id_fkey"
      columns: ["vereinsfunktion_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "prepared_contract_signatur_snapshot_quelle_vertretung_id_fkey"
      columns: ["vertretung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_vertretung"
      referencedColumns: ["id"]
    }
                  ]
                },"saison": {
                  Row: {
                    "aufnahmegebuehr": number,"bemerkung": string | null,"euro_pro_fehlstunde": number,"gebuehr_bauantrag": number,"id": number,"jahr": number | null,"mitgliedsbeitrag": number | null,"mitgliedsbeitrag_nebenmitglied": number,"pacht_faellig_am": string,"pacht_pro_qm": number,"pflichtstunden_soll": number
                  }
                  ComputedFields: never
                  Insert: {
                    "aufnahmegebuehr"?: number,"bemerkung"?: string | null,"euro_pro_fehlstunde"?: number,"gebuehr_bauantrag"?: number,"id"?: number,"jahr"?: number | null,"mitgliedsbeitrag"?: number | null,"mitgliedsbeitrag_nebenmitglied"?: number,"pacht_faellig_am": string,"pacht_pro_qm": number,"pflichtstunden_soll"?: number
                  }
                  Update: {
                    "aufnahmegebuehr"?: number,"bemerkung"?: string | null,"euro_pro_fehlstunde"?: number,"gebuehr_bauantrag"?: number,"id"?: number,"jahr"?: number | null,"mitgliedsbeitrag"?: number | null,"mitgliedsbeitrag_nebenmitglied"?: number,"pacht_faellig_am"?: string,"pacht_pro_qm"?: number,"pflichtstunden_soll"?: number
                  }
                  Relationships: [

                  ]
                },"serienbrief_vorlage": {
                  Row: {
                    "aktiv": boolean,"anrede_modus": string,"betreff_vorlage": string,"erstellt_am": string,"erstellt_von": string | null,"geaendert_am": string,"geaendert_von": string | null,"grundlayout_template_key": string,"grundlayout_version": string,"html_inhalt": string,"id": number,"name": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"anrede_modus"?: string,"betreff_vorlage"?: string,"erstellt_am"?: string,"erstellt_von"?: string | null,"geaendert_am"?: string,"geaendert_von"?: string | null,"grundlayout_template_key"?: string,"grundlayout_version"?: string,"html_inhalt"?: string,"id"?: number,"name": string
                  }
                  Update: {
                    "aktiv"?: boolean,"anrede_modus"?: string,"betreff_vorlage"?: string,"erstellt_am"?: string,"erstellt_von"?: string | null,"geaendert_am"?: string,"geaendert_von"?: string | null,"grundlayout_template_key"?: string,"grundlayout_version"?: string,"html_inhalt"?: string,"id"?: number,"name"?: string
                  }
                  Relationships: [

                  ]
                },"serienbrief_vorlage_exportfeld": {
                  Row: {
                    "feldschluessel": string,"serienbrief_vorlage_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "feldschluessel": string,"serienbrief_vorlage_id": number
                  }
                  Update: {
                    "feldschluessel"?: string,"serienbrief_vorlage_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "serienbrief_vorlage_exportfeld_serienbrief_vorlage_id_fkey"
      columns: ["serienbrief_vorlage_id"]
isOneToOne: false
      referencedRelation: "serienbrief_vorlage"
      referencedColumns: ["id"]
    }
                  ]
                },"serienbrief_vorlage_freifeld": {
                  Row: {
                    "anzeigename": string,"id": number,"pflicht": boolean,"schluessel": string,"serienbrief_vorlage_id": number,"sortierung": number,"typ": string
                  }
                  ComputedFields: never
                  Insert: {
                    "anzeigename": string,"id"?: number,"pflicht"?: boolean,"schluessel": string,"serienbrief_vorlage_id": number,"sortierung"?: number,"typ": string
                  }
                  Update: {
                    "anzeigename"?: string,"id"?: number,"pflicht"?: boolean,"schluessel"?: string,"serienbrief_vorlage_id"?: number,"sortierung"?: number,"typ"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "serienbrief_vorlage_freifeld_serienbrief_vorlage_id_fkey"
      columns: ["serienbrief_vorlage_id"]
isOneToOne: false
      referencedRelation: "serienbrief_vorlage"
      referencedColumns: ["id"]
    }
                  ]
                },"termin": {
                  Row: {
                    "aktiv": boolean,"beschreibung": string | null,"created_at": string,"datum": string,"end_uhrzeit": string | null,"id": number,"sichtbar_ab": string | null,"sichtbar_bis": string | null,"start_uhrzeit": string | null,"titel": string,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"beschreibung"?: string | null,"created_at"?: string,"datum": string,"end_uhrzeit"?: string | null,"id"?: number,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"start_uhrzeit"?: string | null,"titel": string,"updated_at"?: string
                  }
                  Update: {
                    "aktiv"?: boolean,"beschreibung"?: string | null,"created_at"?: string,"datum"?: string,"end_uhrzeit"?: string | null,"id"?: number,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"start_uhrzeit"?: string | null,"titel"?: string,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"umlageart": {
                  Row: {
                    "aktiv": boolean,"bezeichnung": string,"created_at": string,"id": number,"kuerzel": string,"updated_at": string,"verteilung": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"bezeichnung": string,"created_at"?: string,"id"?: number,"kuerzel": string,"updated_at"?: string,"verteilung": string
                  }
                  Update: {
                    "aktiv"?: boolean,"bezeichnung"?: string,"created_at"?: string,"id"?: number,"kuerzel"?: string,"updated_at"?: string,"verteilung"?: string
                  }
                  Relationships: [

                  ]
                },"vereinsdokument_fassung": {
                  Row: {
                    "dateiname": string | null,"dokumenttyp_code": string,"erstellt_am": string,"erstellt_von": string | null,"fassung": string,"gueltig_ab": string,"gueltig_bis": string | null,"id": number,"sha256": string | null,"storage_path": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "dateiname"?: string | null,"dokumenttyp_code": string,"erstellt_am"?: string,"erstellt_von"?: string | null,"fassung": string,"gueltig_ab": string,"gueltig_bis"?: string | null,"id"?: number,"sha256"?: string | null,"storage_path"?: string | null
                  }
                  Update: {
                    "dateiname"?: string | null,"dokumenttyp_code"?: string,"erstellt_am"?: string,"erstellt_von"?: string | null,"fassung"?: string,"gueltig_ab"?: string,"gueltig_bis"?: string | null,"id"?: number,"sha256"?: string | null,"storage_path"?: string | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "vereinsdokument_fassung_dokumenttyp_code_fkey"
      columns: ["dokumenttyp_code"]
isOneToOne: false
      referencedRelation: "vereinsdokument_typ"
      referencedColumns: ["code"]
    }
                  ]
                },"vereinsdokument_typ": {
                  Row: {
                    "bezeichnung": string,"code": string,"pflicht": boolean
                  }
                  ComputedFields: never
                  Insert: {
                    "bezeichnung": string,"code": string,"pflicht"?: boolean
                  }
                  Update: {
                    "bezeichnung"?: string,"code"?: string,"pflicht"?: boolean
                  }
                  Relationships: [

                  ]
                },"vereinsfunktion": {
                  Row: {
                    "aktiv": boolean,"bezeichnung": string,"created_at": string,"erlaubt_mehrfachbesetzung": boolean,"id": number,"schluessel": string,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"bezeichnung": string,"created_at"?: string,"erlaubt_mehrfachbesetzung"?: boolean,"id"?: number,"schluessel": string,"updated_at"?: string
                  }
                  Update: {
                    "aktiv"?: boolean,"bezeichnung"?: string,"created_at"?: string,"erlaubt_mehrfachbesetzung"?: boolean,"id"?: number,"schluessel"?: string,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"vereinsfunktion_besetzung": {
                  Row: {
                    "created_at": string,"gueltig_ab": string,"gueltig_bis": string | null,"id": number,"mitglied_id": number,"sort_order": number,"updated_at": string,"vereinsfunktion_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "created_at"?: string,"gueltig_ab": string,"gueltig_bis"?: string | null,"id"?: number,"mitglied_id": number,"sort_order"?: number,"updated_at"?: string,"vereinsfunktion_id": number
                  }
                  Update: {
                    "created_at"?: string,"gueltig_ab"?: string,"gueltig_bis"?: string | null,"id"?: number,"mitglied_id"?: number,"sort_order"?: number,"updated_at"?: string,"vereinsfunktion_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "vereinsfunktion_besetzung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "vereinsfunktion_besetzung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "vereinsfunktion_besetzung_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "vereinsfunktion_besetzung_vereinsfunktion_id_fkey"
      columns: ["vereinsfunktion_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion"
      referencedColumns: ["id"]
    }
                  ]
                },"vereinsfunktion_vertretung": {
                  Row: {
                    "created_at": string,"gueltig_ab": string,"gueltig_bis": string | null,"id": number,"updated_at": string,"vertretene_besetzung_id": number,"vertreter_besetzung_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "created_at"?: string,"gueltig_ab": string,"gueltig_bis"?: string | null,"id"?: number,"updated_at"?: string,"vertretene_besetzung_id": number,"vertreter_besetzung_id": number
                  }
                  Update: {
                    "created_at"?: string,"gueltig_ab"?: string,"gueltig_bis"?: string | null,"id"?: number,"updated_at"?: string,"vertretene_besetzung_id"?: number,"vertreter_besetzung_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "vereinsfunktion_vertretung_vertretene_besetzung_id_fkey"
      columns: ["vertretene_besetzung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_besetzung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "vereinsfunktion_vertretung_vertreter_besetzung_id_fkey"
      columns: ["vertreter_besetzung_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion_besetzung"
      referencedColumns: ["id"]
    }
                  ]
                },"vereinsfunktion_verwendung": {
                  Row: {
                    "created_at": string,"id": number,"impressum_gruppe": string,"impressum_gruppe_bezeichnung": string,"impressum_gruppen_sort_order": number,"impressum_ist_hauptkontakt": boolean,"impressum_zeige_ladungsfaehige_anschrift": boolean,"sort_order": number,"updated_at": string,"vereinsfunktion_id": number,"verwendungszweck": string
                  }
                  ComputedFields: never
                  Insert: {
                    "created_at"?: string,"id"?: number,"impressum_gruppe"?: string,"impressum_gruppe_bezeichnung"?: string,"impressum_gruppen_sort_order"?: number,"impressum_ist_hauptkontakt"?: boolean,"impressum_zeige_ladungsfaehige_anschrift"?: boolean,"sort_order"?: number,"updated_at"?: string,"vereinsfunktion_id": number,"verwendungszweck": string
                  }
                  Update: {
                    "created_at"?: string,"id"?: number,"impressum_gruppe"?: string,"impressum_gruppe_bezeichnung"?: string,"impressum_gruppen_sort_order"?: number,"impressum_ist_hauptkontakt"?: boolean,"impressum_zeige_ladungsfaehige_anschrift"?: boolean,"sort_order"?: number,"updated_at"?: string,"vereinsfunktion_id"?: number,"verwendungszweck"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "vereinsfunktion_verwendung_vereinsfunktion_id_fkey"
      columns: ["vereinsfunktion_id"]
isOneToOne: false
      referencedRelation: "vereinsfunktion"
      referencedColumns: ["id"]
    }
                  ]
                },"vereinsregelwerk": {
                  Row: {
                    "aktiv": boolean,"created_at": string,"dateiname": string,"mime_type": string,"schluessel": string,"stand": string,"storage_path": string,"titel": string,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"created_at"?: string,"dateiname": string,"mime_type"?: string,"schluessel": string,"stand": string,"storage_path": string,"titel": string,"updated_at"?: string
                  }
                  Update: {
                    "aktiv"?: boolean,"created_at"?: string,"dateiname"?: string,"mime_type"?: string,"schluessel"?: string,"stand"?: string,"storage_path"?: string,"titel"?: string,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"versandinstanz": {
                  Row: {
                    "beansprucht_am": string | null,"beansprucht_von": string | null,"betreff_snapshot": string,"erneuter_versand_von_id": number | null,"erstellt_am": string,"erstellt_von": string,"id": number,"idempotenzschluessel": string,"kommunikationsvorgang_id": number,"letzter_fehler": string | null,"revision": number,"status": string,"text_snapshot": string,"versendet_am": string | null,"vertragsart": string
                  }
                  ComputedFields: never
                  Insert: {
                    "beansprucht_am"?: string | null,"beansprucht_von"?: string | null,"betreff_snapshot": string,"erneuter_versand_von_id"?: number | null,"erstellt_am"?: string,"erstellt_von"?: string,"id"?: number,"idempotenzschluessel"?: string,"kommunikationsvorgang_id": number,"letzter_fehler"?: string | null,"revision": number,"status"?: string,"text_snapshot": string,"versendet_am"?: string | null,"vertragsart": string
                  }
                  Update: {
                    "beansprucht_am"?: string | null,"beansprucht_von"?: string | null,"betreff_snapshot"?: string,"erneuter_versand_von_id"?: number | null,"erstellt_am"?: string,"erstellt_von"?: string,"id"?: number,"idempotenzschluessel"?: string,"kommunikationsvorgang_id"?: number,"letzter_fehler"?: string | null,"revision"?: number,"status"?: string,"text_snapshot"?: string,"versendet_am"?: string | null,"vertragsart"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "versandinstanz_erneuter_versand_von_id_fkey"
      columns: ["erneuter_versand_von_id"]
isOneToOne: false
      referencedRelation: "versandinstanz"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "versandinstanz_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "versandinstanz_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    }
                  ]
                },"versandinstanz_dokument_snapshot": {
                  Row: {
                    "bemerkung_snapshot": string | null,"bereitgestellt_am_snapshot": string | null,"bezeichnung_snapshot": string,"bindungsstatus": string,"bucket_snapshot": string | null,"dateiname_snapshot": string | null,"dokumenttyp_code": string | null,"drive_file_id_snapshot": string | null,"mime_type_snapshot": string | null,"sha256_snapshot": string | null,"sortierung": number,"storage_path_snapshot": string | null,"versandinstanz_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "bemerkung_snapshot"?: string | null,"bereitgestellt_am_snapshot"?: string | null,"bezeichnung_snapshot": string,"bindungsstatus": string,"bucket_snapshot"?: string | null,"dateiname_snapshot"?: string | null,"dokumenttyp_code"?: string | null,"drive_file_id_snapshot"?: string | null,"mime_type_snapshot"?: string | null,"sha256_snapshot"?: string | null,"sortierung": number,"storage_path_snapshot"?: string | null,"versandinstanz_id": number
                  }
                  Update: {
                    "bemerkung_snapshot"?: string | null,"bereitgestellt_am_snapshot"?: string | null,"bezeichnung_snapshot"?: string,"bindungsstatus"?: string,"bucket_snapshot"?: string | null,"dateiname_snapshot"?: string | null,"dokumenttyp_code"?: string | null,"drive_file_id_snapshot"?: string | null,"mime_type_snapshot"?: string | null,"sha256_snapshot"?: string | null,"sortierung"?: number,"storage_path_snapshot"?: string | null,"versandinstanz_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "versandinstanz_dokument_snapshot_versandinstanz_id_fkey"
      columns: ["versandinstanz_id"]
isOneToOne: false
      referencedRelation: "versandinstanz"
      referencedColumns: ["id"]
    }
                  ]
                },"versandinstanz_empfaenger_snapshot": {
                  Row: {
                    "einwilligung_info_snapshot": boolean,"einwilligung_rechnung_snapshot": boolean,"email_normalisiert": string | null,"email_snapshot": string | null,"id": number,"letzter_fehler": string | null,"mitglied_id": number | null,"name_snapshot": string,"nicht_versandfaehig_grund": string | null,"rolle": string,"status": string,"versand_erlaubt": boolean,"versandinstanz_id": number,"versendet_am": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "einwilligung_info_snapshot"?: boolean,"einwilligung_rechnung_snapshot"?: boolean,"email_normalisiert"?: string | null,"email_snapshot"?: string | null,"id"?: number,"letzter_fehler"?: string | null,"mitglied_id"?: number | null,"name_snapshot": string,"nicht_versandfaehig_grund"?: string | null,"rolle": string,"status"?: string,"versand_erlaubt": boolean,"versandinstanz_id": number,"versendet_am"?: string | null
                  }
                  Update: {
                    "einwilligung_info_snapshot"?: boolean,"einwilligung_rechnung_snapshot"?: boolean,"email_normalisiert"?: string | null,"email_snapshot"?: string | null,"id"?: number,"letzter_fehler"?: string | null,"mitglied_id"?: number | null,"name_snapshot"?: string,"nicht_versandfaehig_grund"?: string | null,"rolle"?: string,"status"?: string,"versand_erlaubt"?: boolean,"versandinstanz_id"?: number,"versendet_am"?: string | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "versandinstanz_empfaenger_snapshot_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "versandinstanz_empfaenger_snapshot_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "versandinstanz_empfaenger_snapshot_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "versandinstanz_empfaenger_snapshot_versandinstanz_id_fkey"
      columns: ["versandinstanz_id"]
isOneToOne: false
      referencedRelation: "versandinstanz"
      referencedColumns: ["id"]
    }
                  ]
                },"versandinstanz_versuch": {
                  Row: {
                    "abgeschlossen_am": string | null,"empfaenger_snapshot_id": number,"fehlertext": string | null,"gestartet_am": string,"id": number,"smtp_antwort": Json | null,"smtp_message_id": string | null,"status": string,"versandinstanz_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "abgeschlossen_am"?: string | null,"empfaenger_snapshot_id": number,"fehlertext"?: string | null,"gestartet_am"?: string,"id"?: number,"smtp_antwort"?: Json | null,"smtp_message_id"?: string | null,"status": string,"versandinstanz_id": number
                  }
                  Update: {
                    "abgeschlossen_am"?: string | null,"empfaenger_snapshot_id"?: number,"fehlertext"?: string | null,"gestartet_am"?: string,"id"?: number,"smtp_antwort"?: Json | null,"smtp_message_id"?: string | null,"status"?: string,"versandinstanz_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "versandinstanz_versuch_empfaenger_snapshot_id_fkey"
      columns: ["empfaenger_snapshot_id"]
isOneToOne: false
      referencedRelation: "versandinstanz_empfaenger_snapshot"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "versandinstanz_versuch_versandinstanz_id_fkey"
      columns: ["versandinstanz_id"]
isOneToOne: false
      referencedRelation: "versandinstanz"
      referencedColumns: ["id"]
    }
                  ]
                },"vertragsversand_anlage_snapshot": {
                  Row: {
                    "art": string,"bemerkung_snapshot": string | null,"bereitgestellt_am_snapshot": string | null,"bezeichnung_snapshot": string,"bindungsstatus": string,"bucket_snapshot": string | null,"dateiname_snapshot": string | null,"dokument_id": number | null,"dokumenttyp_code": string | null,"drive_file_id_snapshot": string | null,"id": number,"kommunikationsvorgang_id": number,"mime_type_snapshot": string | null,"revision": number,"sha256_snapshot": string | null,"sortierung": number,"storage_path_snapshot": string | null,"vereinsdokument_fassung_id": number | null
                  }
                  ComputedFields: never
                  Insert: {
                    "art": string,"bemerkung_snapshot"?: string | null,"bereitgestellt_am_snapshot"?: string | null,"bezeichnung_snapshot": string,"bindungsstatus"?: string,"bucket_snapshot"?: string | null,"dateiname_snapshot"?: string | null,"dokument_id"?: number | null,"dokumenttyp_code"?: string | null,"drive_file_id_snapshot"?: string | null,"id"?: number,"kommunikationsvorgang_id": number,"mime_type_snapshot"?: string | null,"revision": number,"sha256_snapshot"?: string | null,"sortierung": number,"storage_path_snapshot"?: string | null,"vereinsdokument_fassung_id"?: number | null
                  }
                  Update: {
                    "art"?: string,"bemerkung_snapshot"?: string | null,"bereitgestellt_am_snapshot"?: string | null,"bezeichnung_snapshot"?: string,"bindungsstatus"?: string,"bucket_snapshot"?: string | null,"dateiname_snapshot"?: string | null,"dokument_id"?: number | null,"dokumenttyp_code"?: string | null,"drive_file_id_snapshot"?: string | null,"id"?: number,"kommunikationsvorgang_id"?: number,"mime_type_snapshot"?: string | null,"revision"?: number,"sha256_snapshot"?: string | null,"sortierung"?: number,"storage_path_snapshot"?: string | null,"vereinsdokument_fassung_id"?: number | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "vertragsversand_anlage_snapshot_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "vertragsversand_anlage_snapshot_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "vertragsversand_anlage_snapshot_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "vertragsversand_anlage_snapshot_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "vertragsversand_anlage_snapshot_vereinsdokument_fassung_id_fkey"
      columns: ["vereinsdokument_fassung_id"]
isOneToOne: false
      referencedRelation: "vereinsdokument_fassung"
      referencedColumns: ["id"]
    }
                  ]
                },"vertragsversand_vertragspartei_snapshot": {
                  Row: {
                    "kommunikationsvorgang_id": number,"mitglied_id": number | null,"name_snapshot": string,"quelle": string,"revision": number,"rolle": string
                  }
                  ComputedFields: never
                  Insert: {
                    "kommunikationsvorgang_id": number,"mitglied_id"?: number | null,"name_snapshot": string,"quelle": string,"revision": number,"rolle": string
                  }
                  Update: {
                    "kommunikationsvorgang_id"?: number,"mitglied_id"?: number | null,"name_snapshot"?: string,"quelle"?: string,"revision"?: number,"rolle"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "vertragsversand_vertragspartei_sn_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "vertragsversand_vertragspartei_sn_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: false
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    },{
      foreignKeyName: "vertragsversand_vertragspartei_snapshot_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "vertragsversand_vertragspartei_snapshot_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "vertragsversand_vertragspartei_snapshot_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    }
                  ]
                },"wartungsvertraege": {
                  Row: {
                    "aktiv": boolean,"arbeitsstunden_gutschrift": number,"befreit_von_pflichtstunden": boolean,"bemerkung": string | null,"bereich": string | null,"beschreibung": string | null,"created_at": string,"id": number,"max_aktive_zuordnungen": number,"titel": string,"updated_at": string
                  }
                  ComputedFields: never
                  Insert: {
                    "aktiv"?: boolean,"arbeitsstunden_gutschrift"?: number,"befreit_von_pflichtstunden"?: boolean,"bemerkung"?: string | null,"bereich"?: string | null,"beschreibung"?: string | null,"created_at"?: string,"id"?: number,"max_aktive_zuordnungen"?: number,"titel": string,"updated_at"?: string
                  }
                  Update: {
                    "aktiv"?: boolean,"arbeitsstunden_gutschrift"?: number,"befreit_von_pflichtstunden"?: boolean,"bemerkung"?: string | null,"bereich"?: string | null,"beschreibung"?: string | null,"created_at"?: string,"id"?: number,"max_aktive_zuordnungen"?: number,"titel"?: string,"updated_at"?: string
                  }
                  Relationships: [

                  ]
                },"wartungsvertrag_dokument_workflow": {
                  Row: {
                    "bearbeitet_am": string,"dokument_id": number | null,"erstellt_am": string,"fehlermeldung": string | null,"folgeentscheidung_id": number | null,"id": number,"kommunikationsvorgang_id": number | null,"status": string,"versuche": number,"wartungsvertrag_zuordnung_id": number,"workflow_art": string
                  }
                  ComputedFields: never
                  Insert: {
                    "bearbeitet_am"?: string,"dokument_id"?: number | null,"erstellt_am"?: string,"fehlermeldung"?: string | null,"folgeentscheidung_id"?: number | null,"id"?: number,"kommunikationsvorgang_id"?: number | null,"status"?: string,"versuche"?: number,"wartungsvertrag_zuordnung_id": number,"workflow_art"?: string
                  }
                  Update: {
                    "bearbeitet_am"?: string,"dokument_id"?: number | null,"erstellt_am"?: string,"fehlermeldung"?: string | null,"folgeentscheidung_id"?: number | null,"id"?: number,"kommunikationsvorgang_id"?: number | null,"status"?: string,"versuche"?: number,"wartungsvertrag_zuordnung_id"?: number,"workflow_art"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "wartungsvertrag_dokument_work_wartungsvertrag_zuordnung_id_fkey"
      columns: ["wartungsvertrag_zuordnung_id"]
isOneToOne: true
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["wartungsvertrag_zuordnung_id"]
    },{
      foreignKeyName: "wartungsvertrag_dokument_work_wartungsvertrag_zuordnung_id_fkey"
      columns: ["wartungsvertrag_zuordnung_id"]
isOneToOne: true
      referencedRelation: "wartungsvertrag_zuordnungen"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_dokument_workflow_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: true
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_dokument_workflow_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: true
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "wartungsvertrag_dokument_workflow_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: true
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "wartungsvertrag_dokument_workflow_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: true
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "wartungsvertrag_dokument_workflow_folgeentscheidung_id_fkey"
      columns: ["folgeentscheidung_id"]
isOneToOne: false
      referencedRelation: "wartungsvertrag_folgeentscheidung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_dokument_workflow_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_dokument_workflow_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    }
                  ]
                },"wartungsvertrag_folgeentscheidung": {
                  Row: {
                    "begruendung": string | null,"created_at": string,"entscheidung": string,"entscheidungsdatum": string,"entschieden_am": string,"entschieden_von_auth_user_id": string,"folgezuordnung_id": number | null,"id": number,"initiator": string | null,"kommunikationsvorgang_id": number | null,"ruecknahmebegruendung": string | null,"updated_at": string,"vorgaenger_zuordnung_id": number,"vorstandsbeschlussdatum": string | null,"zurueckgenommen_am": string | null,"zurueckgenommen_von_auth_user_id": string | null
                  }
                  ComputedFields: never
                  Insert: {
                    "begruendung"?: string | null,"created_at"?: string,"entscheidung": string,"entscheidungsdatum": string,"entschieden_am"?: string,"entschieden_von_auth_user_id": string,"folgezuordnung_id"?: number | null,"id"?: number,"initiator"?: string | null,"kommunikationsvorgang_id"?: number | null,"ruecknahmebegruendung"?: string | null,"updated_at"?: string,"vorgaenger_zuordnung_id": number,"vorstandsbeschlussdatum"?: string | null,"zurueckgenommen_am"?: string | null,"zurueckgenommen_von_auth_user_id"?: string | null
                  }
                  Update: {
                    "begruendung"?: string | null,"created_at"?: string,"entscheidung"?: string,"entscheidungsdatum"?: string,"entschieden_am"?: string,"entschieden_von_auth_user_id"?: string,"folgezuordnung_id"?: number | null,"id"?: number,"initiator"?: string | null,"kommunikationsvorgang_id"?: number | null,"ruecknahmebegruendung"?: string | null,"updated_at"?: string,"vorgaenger_zuordnung_id"?: number,"vorstandsbeschlussdatum"?: string | null,"zurueckgenommen_am"?: string | null,"zurueckgenommen_von_auth_user_id"?: string | null
                  }
                  Relationships: [
                    {
      foreignKeyName: "wartungsvertrag_folgeentscheidung_folgezuordnung_id_fkey"
      columns: ["folgezuordnung_id"]
isOneToOne: true
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["wartungsvertrag_zuordnung_id"]
    },{
      foreignKeyName: "wartungsvertrag_folgeentscheidung_folgezuordnung_id_fkey"
      columns: ["folgezuordnung_id"]
isOneToOne: true
      referencedRelation: "wartungsvertrag_zuordnungen"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_folgeentscheidung_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_folgeentscheidung_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    },{
      foreignKeyName: "wartungsvertrag_folgeentscheidung_vorgaenger_zuordnung_id_fkey"
      columns: ["vorgaenger_zuordnung_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["wartungsvertrag_zuordnung_id"]
    },{
      foreignKeyName: "wartungsvertrag_folgeentscheidung_vorgaenger_zuordnung_id_fkey"
      columns: ["vorgaenger_zuordnung_id"]
isOneToOne: false
      referencedRelation: "wartungsvertrag_zuordnungen"
      referencedColumns: ["id"]
    }
                  ]
                },"wartungsvertrag_grundlegende_aenderung": {
                  Row: {
                    "erledigt_am": string | null,"erstellt_am": string,"id": number,"kommunikationsvorgang_id": number | null,"neue_zuordnung_id": number | null,"status": string,"vorgaenger_zuordnung_id": number,"wartungsvertrag_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "erledigt_am"?: string | null,"erstellt_am"?: string,"id"?: number,"kommunikationsvorgang_id"?: number | null,"neue_zuordnung_id"?: number | null,"status"?: string,"vorgaenger_zuordnung_id": number,"wartungsvertrag_id": number
                  }
                  Update: {
                    "erledigt_am"?: string | null,"erstellt_am"?: string,"id"?: number,"kommunikationsvorgang_id"?: number | null,"neue_zuordnung_id"?: number | null,"status"?: string,"vorgaenger_zuordnung_id"?: number,"wartungsvertrag_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "wartungsvertrag_grundlegende_aend_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_grundlegende_aend_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    },{
      foreignKeyName: "wartungsvertrag_grundlegende_aende_vorgaenger_zuordnung_id_fkey"
      columns: ["vorgaenger_zuordnung_id"]
isOneToOne: true
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["wartungsvertrag_zuordnung_id"]
    },{
      foreignKeyName: "wartungsvertrag_grundlegende_aende_vorgaenger_zuordnung_id_fkey"
      columns: ["vorgaenger_zuordnung_id"]
isOneToOne: true
      referencedRelation: "wartungsvertrag_zuordnungen"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_grundlegende_aenderung_neue_zuordnung_id_fkey"
      columns: ["neue_zuordnung_id"]
isOneToOne: true
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["wartungsvertrag_zuordnung_id"]
    },{
      foreignKeyName: "wartungsvertrag_grundlegende_aenderung_neue_zuordnung_id_fkey"
      columns: ["neue_zuordnung_id"]
isOneToOne: true
      referencedRelation: "wartungsvertrag_zuordnungen"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_grundlegende_aenderung_wartungsvertrag_id_fkey"
      columns: ["wartungsvertrag_id"]
isOneToOne: true
      referencedRelation: "v_wartungsvertrag_fachnummer"
      referencedColumns: ["wartungsvertrag_id"]
    },{
      foreignKeyName: "wartungsvertrag_grundlegende_aenderung_wartungsvertrag_id_fkey"
      columns: ["wartungsvertrag_id"]
isOneToOne: true
      referencedRelation: "wartungsvertraege"
      referencedColumns: ["id"]
    }
                  ]
                },"wartungsvertrag_zuordnungen": {
                  Row: {
                    "bemerkung": string | null,"created_at": string,"gueltig_ab": string,"gueltig_bis": string | null,"hauptmitglied_id": number,"id": number,"updated_at": string,"vertragspartei_mitglied_id": number | null,"vorgaenger_zuordnung_id": number | null,"wartungsvertrag_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "bemerkung"?: string | null,"created_at"?: string,"gueltig_ab": string,"gueltig_bis"?: string | null,"hauptmitglied_id": number,"id"?: number,"updated_at"?: string,"vertragspartei_mitglied_id"?: number | null,"vorgaenger_zuordnung_id"?: number | null,"wartungsvertrag_id": number
                  }
                  Update: {
                    "bemerkung"?: string | null,"created_at"?: string,"gueltig_ab"?: string,"gueltig_bis"?: string | null,"hauptmitglied_id"?: number,"id"?: number,"updated_at"?: string,"vertragspartei_mitglied_id"?: number | null,"vorgaenger_zuordnung_id"?: number | null,"wartungsvertrag_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "fk_wvz_hauptmitglied"
      columns: ["hauptmitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "fk_wvz_hauptmitglied"
      columns: ["hauptmitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "fk_wvz_hauptmitglied"
      columns: ["hauptmitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "fk_wvz_wartungsvertrag"
      columns: ["wartungsvertrag_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_fachnummer"
      referencedColumns: ["wartungsvertrag_id"]
    },{
      foreignKeyName: "fk_wvz_wartungsvertrag"
      columns: ["wartungsvertrag_id"]
isOneToOne: false
      referencedRelation: "wartungsvertraege"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_zuordnungen_vertragspartei_mitglied_id_fkey"
      columns: ["vertragspartei_mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "wartungsvertrag_zuordnungen_vertragspartei_mitglied_id_fkey"
      columns: ["vertragspartei_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "wartungsvertrag_zuordnungen_vertragspartei_mitglied_id_fkey"
      columns: ["vertragspartei_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "wartungsvertrag_zuordnungen_vorgaenger_zuordnung_id_fkey"
      columns: ["vorgaenger_zuordnung_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["wartungsvertrag_zuordnung_id"]
    },{
      foreignKeyName: "wartungsvertrag_zuordnungen_vorgaenger_zuordnung_id_fkey"
      columns: ["vorgaenger_zuordnung_id"]
isOneToOne: false
      referencedRelation: "wartungsvertrag_zuordnungen"
      referencedColumns: ["id"]
    }
                  ]
                },"zaehler": {
                  Row: {
                    "anlagenbereich": string | null,"ausgebaut_am": string | null,"bauart": string,"created_at": string,"defekt": boolean,"eichdatum": string,"eichfaellig_am": string,"eingebaut_am": string,"id": number,"ist_hauptzaehler": boolean,"medium": Database["public"]['Enums']["zaehler_medium"],"parzelle_id": number | null,"status": Database["public"]['Enums']["zaehler_status"],"updated_at": string,"zaehlernummer": string
                  }
                  ComputedFields: never
                  Insert: {
                    "anlagenbereich"?: string | null,"ausgebaut_am"?: string | null,"bauart"?: string,"created_at"?: string,"defekt"?: boolean,"eichdatum": string,"eichfaellig_am": string,"eingebaut_am"?: string,"id"?: number,"ist_hauptzaehler"?: boolean,"medium": Database["public"]['Enums']["zaehler_medium"],"parzelle_id"?: number | null,"status"?: Database["public"]['Enums']["zaehler_status"],"updated_at"?: string,"zaehlernummer": string
                  }
                  Update: {
                    "anlagenbereich"?: string | null,"ausgebaut_am"?: string | null,"bauart"?: string,"created_at"?: string,"defekt"?: boolean,"eichdatum"?: string,"eichfaellig_am"?: string,"eingebaut_am"?: string,"id"?: number,"ist_hauptzaehler"?: boolean,"medium"?: Database["public"]['Enums']["zaehler_medium"],"parzelle_id"?: number | null,"status"?: Database["public"]['Enums']["zaehler_status"],"updated_at"?: string,"zaehlernummer"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "zaehler_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    }
                  ]
                },"zaehler_ablesung": {
                  Row: {
                    "ablesedatum": string,"art": Database["public"]['Enums']["ablesung_art"],"created_at": string,"foto_dateiname": string | null,"foto_drive_file_id": string | null,"foto_pfad": string | null,"freigegeben": boolean,"geprueft_am": string | null,"geprueft_von": number | null,"id": number,"pruefkommentar": string | null,"pruefstatus": Database["public"]['Enums']["ablesung_pruefstatus"],"stand": number,"zaehler_id": number
                  }
                  ComputedFields: never
                  Insert: {
                    "ablesedatum"?: string,"art"?: Database["public"]['Enums']["ablesung_art"],"created_at"?: string,"foto_dateiname"?: string | null,"foto_drive_file_id"?: string | null,"foto_pfad"?: string | null,"freigegeben"?: boolean,"geprueft_am"?: string | null,"geprueft_von"?: number | null,"id"?: number,"pruefkommentar"?: string | null,"pruefstatus"?: Database["public"]['Enums']["ablesung_pruefstatus"],"stand": number,"zaehler_id": number
                  }
                  Update: {
                    "ablesedatum"?: string,"art"?: Database["public"]['Enums']["ablesung_art"],"created_at"?: string,"foto_dateiname"?: string | null,"foto_drive_file_id"?: string | null,"foto_pfad"?: string | null,"freigegeben"?: boolean,"geprueft_am"?: string | null,"geprueft_von"?: number | null,"id"?: number,"pruefkommentar"?: string | null,"pruefstatus"?: Database["public"]['Enums']["ablesung_pruefstatus"],"stand"?: number,"zaehler_id"?: number
                  }
                  Relationships: [
                    {
      foreignKeyName: "zaehler_ablesung_geprueft_von_fkey"
      columns: ["geprueft_von"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehler_ablesung_geprueft_von_fkey"
      columns: ["geprueft_von"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "zaehler_ablesung_geprueft_von_fkey"
      columns: ["geprueft_von"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "zaehler_ablesung_zaehler_id_fkey"
      columns: ["zaehler_id"]
isOneToOne: false
      referencedRelation: "v_aktive_zaehler"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehler_ablesung_zaehler_id_fkey"
      columns: ["zaehler_id"]
isOneToOne: false
      referencedRelation: "v_zaehler_eichstatus"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehler_ablesung_zaehler_id_fkey"
      columns: ["zaehler_id"]
isOneToOne: false
      referencedRelation: "zaehler"
      referencedColumns: ["id"]
    }
                  ]
                },"zaehlerwechsel_antrag": {
                  Row: {
                    "alter_zaehler_id": number,"anfangsstand": number,"antragsteller_mitglied_id": number,"ausbau_foto_dateiname": string | null,"ausbau_foto_drive_file_id": string | null,"ausbau_foto_pfad": string | null,"ausbaudatum": string,"ausgefuehrter_neuer_zaehler_id": number | null,"eichdatum": string,"einbau_foto_dateiname": string | null,"einbau_foto_drive_file_id": string | null,"einbau_foto_pfad": string | null,"einbaudatum": string,"eingereicht_am": string,"endstand": number,"geprueft_am": string | null,"geprueft_von": number | null,"id": number,"medium": Database["public"]['Enums']["zaehler_medium"],"neue_zaehlernummer": string,"parzelle_id": number,"pruefkommentar": string | null,"status": string
                  }
                  ComputedFields: never
                  Insert: {
                    "alter_zaehler_id": number,"anfangsstand": number,"antragsteller_mitglied_id": number,"ausbau_foto_dateiname"?: string | null,"ausbau_foto_drive_file_id"?: string | null,"ausbau_foto_pfad"?: string | null,"ausbaudatum": string,"ausgefuehrter_neuer_zaehler_id"?: number | null,"eichdatum": string,"einbau_foto_dateiname"?: string | null,"einbau_foto_drive_file_id"?: string | null,"einbau_foto_pfad"?: string | null,"einbaudatum": string,"eingereicht_am"?: string,"endstand": number,"geprueft_am"?: string | null,"geprueft_von"?: number | null,"id"?: number,"medium": Database["public"]['Enums']["zaehler_medium"],"neue_zaehlernummer": string,"parzelle_id": number,"pruefkommentar"?: string | null,"status"?: string
                  }
                  Update: {
                    "alter_zaehler_id"?: number,"anfangsstand"?: number,"antragsteller_mitglied_id"?: number,"ausbau_foto_dateiname"?: string | null,"ausbau_foto_drive_file_id"?: string | null,"ausbau_foto_pfad"?: string | null,"ausbaudatum"?: string,"ausgefuehrter_neuer_zaehler_id"?: number | null,"eichdatum"?: string,"einbau_foto_dateiname"?: string | null,"einbau_foto_drive_file_id"?: string | null,"einbau_foto_pfad"?: string | null,"einbaudatum"?: string,"eingereicht_am"?: string,"endstand"?: number,"geprueft_am"?: string | null,"geprueft_von"?: number | null,"id"?: number,"medium"?: Database["public"]['Enums']["zaehler_medium"],"neue_zaehlernummer"?: string,"parzelle_id"?: number,"pruefkommentar"?: string | null,"status"?: string
                  }
                  Relationships: [
                    {
      foreignKeyName: "zaehlerwechsel_antrag_alter_zaehler_id_fkey"
      columns: ["alter_zaehler_id"]
isOneToOne: false
      referencedRelation: "v_aktive_zaehler"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_alter_zaehler_id_fkey"
      columns: ["alter_zaehler_id"]
isOneToOne: false
      referencedRelation: "v_zaehler_eichstatus"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_alter_zaehler_id_fkey"
      columns: ["alter_zaehler_id"]
isOneToOne: false
      referencedRelation: "zaehler"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_antragsteller_mitglied_id_fkey"
      columns: ["antragsteller_mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_antragsteller_mitglied_id_fkey"
      columns: ["antragsteller_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_antragsteller_mitglied_id_fkey"
      columns: ["antragsteller_mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_ausgefuehrter_neuer_zaehler_id_fkey"
      columns: ["ausgefuehrter_neuer_zaehler_id"]
isOneToOne: false
      referencedRelation: "v_aktive_zaehler"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_ausgefuehrter_neuer_zaehler_id_fkey"
      columns: ["ausgefuehrter_neuer_zaehler_id"]
isOneToOne: false
      referencedRelation: "v_zaehler_eichstatus"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_ausgefuehrter_neuer_zaehler_id_fkey"
      columns: ["ausgefuehrter_neuer_zaehler_id"]
isOneToOne: false
      referencedRelation: "zaehler"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_geprueft_von_fkey"
      columns: ["geprueft_von"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_geprueft_von_fkey"
      columns: ["geprueft_von"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_geprueft_von_fkey"
      columns: ["geprueft_von"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "zaehlerwechsel_antrag_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    }
                  ]
                }
          }
          Views: {
            "mitgliedsaufnahme_vorstand_liste": {
                  Row: {
                    "abgegebene_stimmen": number | null,"antragsteller": string | null,"aufnahmebestaetigung_dokument_id": number | null,"aufnahmebestaetigung_status": string | null,"aufnahmebestaetigung_vorgang_id": number | null,"bearbeitung_id": number | null,"einstimmig_ja": boolean | null,"entscheidung": string | null,"final_dokument_id": number | null,"gestartet_am": string | null,"ja_stimmen": number | null,"kommunikationsvorgang_id": number | null,"konflikt": boolean | null,"mitglied_id": number | null,"nein_stimmen": number | null,"snapshot_personen": number | null
                  }
                  ComputedFields: never
                  Relationships: [
                    {
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["aufnahmebestaetigung_dokument_id"]
isOneToOne: false
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["final_dokument_id"]
isOneToOne: false
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["aufnahmebestaetigung_dokument_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["final_dokument_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["aufnahmebestaetigung_dokument_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["final_dokument_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["aufnahmebestaetigung_dokument_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_final_dokument_id_fkey"
      columns: ["final_dokument_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "mitglied"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "kommunikationsvorgang_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "kommunikationsvorgang_mitglied_id_fkey"
      columns: ["mitglied_id"]
isOneToOne: false
      referencedRelation: "v_pflichtstunden_uebersicht"
      referencedColumns: ["mitglied_id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_vorstandsbearbe_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "kommunikationsvorgang"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "mitgliedsaufnahme_vorstandsbearbe_kommunikationsvorgang_id_fkey"
      columns: ["kommunikationsvorgang_id"]
isOneToOne: true
      referencedRelation: "mitgliedsaufnahme_vorstand_liste"
      referencedColumns: ["aufnahmebestaetigung_vorgang_id"]
    }
                  ]
                },"v_aktive_zaehler": {
                  Row: {
                    "anlage": string | null,"eichdatum": string | null,"eichfaellig_am": string | null,"eingebaut_am": string | null,"garten_nr": string | null,"id": number | null,"medium": Database["public"]['Enums']["zaehler_medium"] | null,"parzelle_id": number | null,"rfid_tag_uid": string | null,"zaehlernummer": string | null
                  }
                  ComputedFields: never
                  Relationships: [
                    {
      foreignKeyName: "zaehler_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    }
                  ]
                },"v_export_rfid_status": {
                  Row: {
                    "aktiver_zaehler_id": number | null,"anlage": string | null,"ausgebaut_am": string | null,"eichdatum": string | null,"eichfaellig_am": string | null,"eingebaut_am": string | null,"garten_nr": string | null,"medium": Database["public"]['Enums']["zaehler_medium"] | null,"parzelle_id": number | null,"rfid_tag_uid": string | null,"status": Database["public"]['Enums']["zaehler_status"] | null,"zaehlernummer": string | null
                  }
                  ComputedFields: never
                  Relationships: [

                  ]
                },"v_mitglied_mitgliedsantrag_nachweisstatus": {
                  Row: {
                    "dokument_id": number | null,"dokumentdatum": string | null,"mitglied_id": number | null,"nachweisstatus": string | null
                  }
                  ComputedFields: never
                  Relationships: [

                  ]
                },"v_pachtverhaeltnis_pachtvertrag_nachweisstatus": {
                  Row: {
                    "dokument_id": number | null,"nachweisstatus": string | null,"pachtverhaeltnis_id": number | null,"pachtvertragsfassung_id": number | null,"vertragsdatum": string | null
                  }
                  ComputedFields: never
                  Relationships: [

                  ]
                },"v_pachtvertragsfassung_uebersicht": {
                  Row: {
                    "dokument_id": number | null,"id": number | null,"massgeblichkeit": string | null,"nachfolger_fassung_id": number | null,"nachfolger_vertragsnummer": number | null,"nicht_mehr_massgeblich_am": string | null,"pachtverhaeltnis_id": number | null,"parzelle_id": number | null,"vertragsnummer": number | null,"vorgaenger_fassung_id": number | null,"vorgaenger_vertragsnummer": number | null
                  }
                  ComputedFields: never
                  Relationships: [
                    {
      foreignKeyName: "pachtverhaeltnis_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtvertragsfassung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "dokument"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtvertragsfassung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_mitglied_mitgliedsantrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_dokument_id_fkey"
      columns: ["dokument_id"]
isOneToOne: false
      referencedRelation: "v_wartungsvertrag_zuordnung_nachweisstatus"
      referencedColumns: ["dokument_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_pachtverhaeltnis_id_fkey"
      columns: ["pachtverhaeltnis_id"]
isOneToOne: false
      referencedRelation: "pachtverhaeltnis"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtvertragsfassung_pachtverhaeltnis_id_fkey"
      columns: ["pachtverhaeltnis_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["pachtverhaeltnis_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_vorgaenger_fassung_id_fkey"
      columns: ["vorgaenger_fassung_id"]
isOneToOne: false
      referencedRelation: "pachtvertragsfassung"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtvertragsfassung_vorgaenger_fassung_id_fkey"
      columns: ["vorgaenger_fassung_id"]
isOneToOne: false
      referencedRelation: "v_pachtverhaeltnis_pachtvertrag_nachweisstatus"
      referencedColumns: ["pachtvertragsfassung_id"]
    },{
      foreignKeyName: "pachtvertragsfassung_vorgaenger_fassung_id_fkey"
      columns: ["vorgaenger_fassung_id"]
isOneToOne: false
      referencedRelation: "v_pachtvertragsfassung_uebersicht"
      referencedColumns: ["id"]
    },{
      foreignKeyName: "pachtvertragsfassung_vorgaenger_fassung_id_fkey"
      columns: ["vorgaenger_fassung_id"]
isOneToOne: false
      referencedRelation: "v_pachtvertragsfassung_uebersicht"
      referencedColumns: ["nachfolger_fassung_id"]
    }
                  ]
                },"v_pflichtstunden_uebersicht": {
                  Row: {
                    "altersbefreit": boolean | null,"eintritt_im_saisonjahr": boolean | null,"eintritt_zweites_halbjahr": boolean | null,"euro_pro_fehlstunde": number | null,"fehlbetrag": number | null,"geleistete_stunden": number | null,"hat_wartungsvertrag": boolean | null,"hauptmitglied_id": number | null,"ist_befreit": boolean | null,"jahr": number | null,"mitglied_id": number | null,"name": string | null,"offene_stunden": number | null,"pflichtstunden_soll": number | null,"regelgrund": string | null,"saison_id": number | null,"saison_jahr": number | null,"vorname": string | null,"wartungsvertrag_gutschrift_stunden": number | null
                  }
                  ComputedFields: never
                  Relationships: [

                  ]
                },"v_rfid_scan_context": {
                  Row: {
                    "aktiver_zaehler_id": number | null,"anlage": string | null,"ausgebaut_am": string | null,"eichdatum": string | null,"eichfaellig_am": string | null,"eingebaut_am": string | null,"garten_nr": string | null,"medium": Database["public"]['Enums']["zaehler_medium"] | null,"parzelle_id": number | null,"rfid_tag_uid": string | null,"status": Database["public"]['Enums']["zaehler_status"] | null,"zaehlernummer": string | null
                  }
                  ComputedFields: never
                  Relationships: [

                  ]
                },"v_startseite_arbeitseinsatz": {
                  Row: {
                    "aktiv": boolean | null,"angemeldet_count": number | null,"anmeldung_bis": string | null,"beschreibung": string | null,"datum": string | null,"end_uhrzeit": string | null,"freie_plaetze": number | null,"id": number | null,"max_teilnehmer": number | null,"sichtbar_ab": string | null,"sichtbar_bis": string | null,"start_uhrzeit": string | null,"stunden_wert": number | null,"titel": string | null,"treffpunkt": string | null
                  }
                  ComputedFields: never
                  Relationships: [

                  ]
                },"v_startseite_bekanntmachungen": {
                  Row: {
                    "created_at": string | null,"id": number | null,"inhalt_html": string | null,"sichtbar_ab": string | null,"sichtbar_bis": string | null,"sort_order": number | null,"titel": string | null
                  }
                  ComputedFields: never
                  Insert: {
                           "created_at"?: string | null,"id"?: number | null,"inhalt_html"?: string | null,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"sort_order"?: number | null,"titel"?: string | null
                         }
                        Update: {
                           "created_at"?: string | null,"id"?: number | null,"inhalt_html"?: string | null,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"sort_order"?: number | null,"titel"?: string | null
                         }
                        Relationships: [

                  ]
                },"v_startseite_termine": {
                  Row: {
                    "beschreibung": string | null,"datum": string | null,"end_uhrzeit": string | null,"id": number | null,"sichtbar_ab": string | null,"sichtbar_bis": string | null,"start_uhrzeit": string | null,"titel": string | null
                  }
                  ComputedFields: never
                  Insert: {
                           "beschreibung"?: string | null,"datum"?: string | null,"end_uhrzeit"?: string | null,"id"?: number | null,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"start_uhrzeit"?: string | null,"titel"?: string | null
                         }
                        Update: {
                           "beschreibung"?: string | null,"datum"?: string | null,"end_uhrzeit"?: string | null,"id"?: number | null,"sichtbar_ab"?: string | null,"sichtbar_bis"?: string | null,"start_uhrzeit"?: string | null,"titel"?: string | null
                         }
                        Relationships: [

                  ]
                },"v_wartungsvertrag_fachnummer": {
                  Row: {
                    "vertragsnummer": number | null,"wartungsvertrag_id": number | null
                  }
                  ComputedFields: never
                  Relationships: [

                  ]
                },"v_wartungsvertrag_zuordnung_nachweisstatus": {
                  Row: {
                    "dokument_id": number | null,"dokumentdatum": string | null,"nachweisstatus": string | null,"wartungsvertrag_zuordnung_id": number | null
                  }
                  ComputedFields: never
                  Relationships: [

                  ]
                },"v_zaehler_eichstatus": {
                  Row: {
                    "anlage": string | null,"defekt": boolean | null,"eichdatum": string | null,"eichfaellig_am": string | null,"eichstatus": string | null,"eingebaut_am": string | null,"garten_nr": string | null,"id": number | null,"medium": Database["public"]['Enums']["zaehler_medium"] | null,"parzelle_id": number | null,"status": Database["public"]['Enums']["zaehler_status"] | null,"tage_bis_faellig": number | null,"zaehlernummer": string | null
                  }
                  ComputedFields: never
                  Relationships: [
                    {
      foreignKeyName: "zaehler_parzelle_id_fkey"
      columns: ["parzelle_id"]
isOneToOne: false
      referencedRelation: "parzelle"
      referencedColumns: ["id"]
    }
                  ]
                }
          }
          Functions: {
            "acquire_browser_edit_lock":
{ Args: { "p_entity_id": string,"p_entity_type": string,"p_timeout_seconds"?: number }; Returns: {
              "acquired": boolean,"expires_at": string,"locked_by_display_name": string,"locked_by_user_id": string
            }[]
                           },
"acquire_browser_edit_lock_core":
{ Args: { "p_client_instance_id": string,"p_entity_id": string,"p_entity_type": string,"p_timeout_seconds"?: number }; Returns: {
              "acquired": boolean,"expires_at": string,"locked_by_display_name": string,"locked_by_user_id": string
            }[]
                           },
"acquire_browser_edit_lock_for_client":
{ Args: { "p_client_instance_id": string,"p_entity_id": string,"p_entity_type": string,"p_timeout_seconds"?: number }; Returns: {
              "acquired": boolean,"expires_at": string,"locked_by_display_name": string,"locked_by_user_id": string
            }[]
                           },
"assert_export_access":
{ Args: Record<PropertyKey, never>; Returns: undefined
                           },
"assign_parzelle_rfid":
{ Args: { "p_medium": Database["public"]['Enums']["zaehler_medium"],"p_parzelle_id": number,"p_rfid_tag_uid": string }; Returns: {
              "aktiv": boolean,
"Anlage": string,
"flaeche_qm": number | null,
"garten_nr": string,
"hat_strom": boolean,
"hat_wasser": boolean,
"id": number,
"ist_vereinsgarten": boolean,
"qr_code_strom": string | null,
"qr_code_wasser": string | null,
"rfid_strom": string | null,
"rfid_wasser": string | null
            }
                          SetofOptions: {
        from: "*"
        to: "parzelle"
        isOneToOne: true
        isSetofReturn: false
      } },
"before_user_created_allowlist":
{ Args: { "event": Json }; Returns: Json
                           },
"begin_historical_contract_upload":
{ Args: { "p_actor": string,"p_contract_type": string,"p_drive_file_id": string,"p_file_name": string,"p_member_id": number,"p_operation_id": string,"p_upload_request": Json }; Returns: Json
                           },
"browser_edit_lock_display_name":
{ Args: { "p_user_id": string }; Returns: string
                           },
"calc_eichfaellig_am":
{ Args: { "p_eichdatum": string,"p_medium": Database["public"]['Enums']["zaehler_medium"] }; Returns: string
                           } |
{ Args: { "p_bauart": string,"p_eichdatum": string,"p_medium": Database["public"]['Enums']["zaehler_medium"] }; Returns: string
                           },
"can_access_historical_contract_document":
{ Args: { "p_dokument_id": number,"p_require_registered"?: boolean }; Returns: boolean
                           },
"can_access_historical_contract_member":
{ Args: { "p_mitglied_id": number }; Returns: boolean
                           },
"can_access_live_internal_data":
{ Args: Record<PropertyKey, never>; Returns: boolean
                           },
"can_access_mitgliedsaufnahme_vorstand_document":
{ Args: { "p_auth_user_id": string,"p_dokument_id": number }; Returns: boolean
                           },
"can_register_historical_contracts":
{ Args: Record<PropertyKey, never>; Returns: boolean
                           },
"capture_meter_reading":
{ Args: { "p_ablesedatum": string,"p_art": Database["public"]['Enums']["ablesung_art"],"p_foto_dateiname"?: string,"p_foto_drive_file_id"?: string,"p_foto_pfad"?: string,"p_stand": number,"p_zaehler_id": number }; Returns: {
              "ablesedatum": string,
"art": Database["public"]['Enums']["ablesung_art"],
"created_at": string,
"foto_dateiname": string | null,
"foto_drive_file_id": string | null,
"foto_pfad": string | null,
"freigegeben": boolean,
"geprueft_am": string | null,
"geprueft_von": number | null,
"id": number,
"pruefkommentar": string | null,
"pruefstatus": Database["public"]['Enums']["ablesung_pruefstatus"],
"stand": number,
"zaehler_id": number
            }
                          SetofOptions: {
        from: "*"
        to: "zaehler_ablesung"
        isOneToOne: true
        isSetofReturn: false
      } },
"claim_contract_dispatch":
{ Args: { "p_versandinstanz_id": number }; Returns: {
              "betreff": string,"id": number,"mailtext": string
            }[]
                           },
"claim_historical_contract_stamp":
{ Args: { "p_document_id": number,"p_token": string }; Returns: boolean
                           },
"complete_aufnahmebestaetigung":
{ Args: { "p_dokument_id": number,"p_kommunikationsvorgang_id": number }; Returns: undefined
                           },
"complete_prepared_mitgliedsantrag":
{ Args: { "p_created_by": string,"p_expected_revision": number,"p_kommunikationsvorgang_id": number }; Returns: {
              "bereits_abgeschlossen": boolean,"vertreter_mitglied_id": number
            }[]
                           },
"complete_prepared_mitgliedsantrag_aus_entscheidung":
{ Args: { "p_aufnahmedatum": string,"p_created_by": string,"p_expected_revision": number,"p_kommunikationsvorgang_id": number }; Returns: undefined
                           },
"complete_prepared_pachtvertrag":
{ Args: { "p_created_by": string,"p_expected_revision": number,"p_kommunikationsvorgang_id": number }; Returns: {
              "bereits_abgeschlossen": boolean,"pachtverhaeltnis_id": number,"pachtvertragsfassung_id": number
            }[]
                           },
"complete_prepared_pachtvertrag_vor_b3":
{ Args: { "p_created_by": string,"p_expected_revision": number,"p_kommunikationsvorgang_id": number }; Returns: {
              "bereits_abgeschlossen": boolean,"pachtverhaeltnis_id": number,"pachtvertragsfassung_id": number
            }[]
                           },
"complete_prepared_pachtvertrag_vor_phase9":
{ Args: { "p_created_by": string,"p_expected_revision": number,"p_kommunikationsvorgang_id": number }; Returns: {
              "bereits_abgeschlossen": boolean,"pachtverhaeltnis_id": number,"pachtvertragsfassung_id": number
            }[]
                           },
"complete_prepared_wartungsvertrag":
{ Args: { "p_dokument_id": number,"p_kommunikationsvorgang_id": number }; Returns: {
              "bereits_abgeschlossen": boolean,"wartungsvertrag_zuordnung_id": number
            }[]
                           },
"complete_wartungsvertrag_verlaengerungsmitteilung":
{ Args: { "p_dokument_id": number,"p_kommunikationsvorgang_id": number }; Returns: undefined
                           },
"confirm_arbeitseinsatz_arbeitsstunde":
{ Args: { "p_arbeitseinsatz_anmeldung_id": number,"p_art_der_arbeit"?: string,"p_stunden": number }; Returns: {
              "arbeitseinsatz_anmeldung_id": number | null,
"art_der_arbeit": string,
"datum": string,
"freigegeben": boolean,
"genehmigt_am": string | null,
"genehmigt_von": number | null,
"id": number,
"lockat": string | null,
"lockedbyuserid": string | null,
"mitglied_id": number,
"saison_id": number,
"status": string | null,
"stunden": number
            }
                          SetofOptions: {
        from: "*"
        to: "arbeitsstunde"
        isOneToOne: true
        isSetofReturn: false
      } },
"correct_meter_change_request":
{ Args: { "p_anfangsstand": number,"p_antrag_id": number,"p_ausbaudatum": string,"p_eichdatum": string,"p_einbaudatum": string,"p_endstand": number,"p_kommentar"?: string,"p_neue_zaehlernummer": string }; Returns: {
              "alter_zaehler_id": number,
"anfangsstand": number,
"antragsteller_mitglied_id": number,
"ausbau_foto_dateiname": string | null,
"ausbau_foto_drive_file_id": string | null,
"ausbau_foto_pfad": string | null,
"ausbaudatum": string,
"ausgefuehrter_neuer_zaehler_id": number | null,
"eichdatum": string,
"einbau_foto_dateiname": string | null,
"einbau_foto_drive_file_id": string | null,
"einbau_foto_pfad": string | null,
"einbaudatum": string,
"eingereicht_am": string,
"endstand": number,
"geprueft_am": string | null,
"geprueft_von": number | null,
"id": number,
"medium": Database["public"]['Enums']["zaehler_medium"],
"neue_zaehlernummer": string,
"parzelle_id": number,
"pruefkommentar": string | null,
"status": string
            }
                          SetofOptions: {
        from: "*"
        to: "zaehlerwechsel_antrag"
        isOneToOne: true
        isSetofReturn: false
      } },
"create_contract_redispatch":
{ Args: { "p_versandinstanz_id": number }; Returns: number
                           },
"create_meter_installation":
{ Args: { "p_eichdatum": string,"p_eingebaut_am"?: string,"p_medium": Database["public"]['Enums']["zaehler_medium"],"p_parzelle_id": number,"p_zaehlernummer": string }; Returns: {
              "anlagenbereich": string | null,
"ausgebaut_am": string | null,
"bauart": string,
"created_at": string,
"defekt": boolean,
"eichdatum": string,
"eichfaellig_am": string,
"eingebaut_am": string,
"id": number,
"ist_hauptzaehler": boolean,
"medium": Database["public"]['Enums']["zaehler_medium"],
"parzelle_id": number | null,
"status": Database["public"]['Enums']["zaehler_status"],
"updated_at": string,
"zaehlernummer": string
            }
                          SetofOptions: {
        from: "*"
        to: "zaehler"
        isOneToOne: true
        isSetofReturn: false
      } },
"create_meter_reading":
{ Args: { "p_ablesedatum"?: string,"p_foto_pfad"?: string,"p_stand": number,"p_zaehler_id": number }; Returns: {
              "ablesedatum": string,
"art": Database["public"]['Enums']["ablesung_art"],
"created_at": string,
"foto_dateiname": string | null,
"foto_drive_file_id": string | null,
"foto_pfad": string | null,
"freigegeben": boolean,
"geprueft_am": string | null,
"geprueft_von": number | null,
"id": number,
"pruefkommentar": string | null,
"pruefstatus": Database["public"]['Enums']["ablesung_pruefstatus"],
"stand": number,
"zaehler_id": number
            }
                          SetofOptions: {
        from: "*"
        to: "zaehler_ablesung"
        isOneToOne: true
        isSetofReturn: false
      } },
"create_mitglied_vereinsdokument_bereitstellungen_digital":
{ Args: { "p_bereitgestellt_von": string,"p_mitglied_id": number,"p_vereinsdokument_fassung_ids": (number)[] }; Returns: {
              "bereitgestellt_am": string,
"bereitgestellt_von": string | null,
"bereitstellungsart": string,
"created_at": string,
"id": number,
"mitglied_id": number,
"storniert_am": string | null,
"storniert_von": string | null,
"storno_grund": string | null,
"vereinsdokument_fassung_id": number
            }[]
                          SetofOptions: {
        from: "*"
        to: "mitglied_vereinsdokument_bereitstellung"
        isOneToOne: false
        isSetofReturn: true
      } },
"create_mitglied_vereinsdokument_bereitstellungen_papier":
{ Args: { "p_bereitgestellt_am": string,"p_mitglied_id": number,"p_vereinsdokument_fassung_ids": (number)[] }; Returns: {
              "bereitgestellt_am": string,
"bereitgestellt_von": string | null,
"bereitstellungsart": string,
"created_at": string,
"id": number,
"mitglied_id": number,
"storniert_am": string | null,
"storniert_von": string | null,
"storno_grund": string | null,
"vereinsdokument_fassung_id": number
            }[]
                          SetofOptions: {
        from: "*"
        to: "mitglied_vereinsdokument_bereitstellung"
        isOneToOne: false
        isSetofReturn: true
      } },
"create_vereinsdokument_fassung":
{ Args: { "p_dateiname": string,"p_dokumenttyp_code": string,"p_fassung": string,"p_gueltig_ab": string,"p_sha256": string,"p_storage_path": string }; Returns: {
              "dateiname": string | null,
"dokumenttyp_code": string,
"erstellt_am": string,
"erstellt_von": string | null,
"fassung": string,
"gueltig_ab": string,
"gueltig_bis": string | null,
"id": number,
"sha256": string | null,
"storage_path": string | null
            }
                          SetofOptions: {
        from: "*"
        to: "vereinsdokument_fassung"
        isOneToOne: true
        isSetofReturn: false
      } },
"current_app_role":
{ Args: Record<PropertyKey, never>; Returns: string
                           },
"current_app_user_context":
{ Args: Record<PropertyKey, never>; Returns: {
              "is_protected": boolean,"mitglied_id": number,"permission_grants": number,"permission_revocations": number,"role": string,"user_id": string
            }[]
                           },
"current_effective_permissions":
{ Args: Record<PropertyKey, never>; Returns: number
                           },
"current_mitglied_id":
{ Args: Record<PropertyKey, never>; Returns: number
                           },
"current_user_email":
{ Args: Record<PropertyKey, never>; Returns: string
                           },
"current_user_has_active_lease_for_parcel":
{ Args: { "p_on_date"?: string,"p_parzelle_id": number }; Returns: boolean
                           },
"darf_mitgliedsaufnahme_lesen":
{ Args: { "p_bearbeitung_id": number }; Returns: boolean
                           },
"discard_prepared_contract":
{ Args: { "p_discarded_by": string,"p_expected_revision": number,"p_kommunikationsvorgang_id": number }; Returns: {
              "final_dokument_id": number,"id": number,"status": string
            }[]
                           },
"dokumentiere_prepared_contract_papieruebergabe":
{ Args: { "p_bemerkung"?: string,"p_dokumenttyp": string,"p_fassung": string,"p_kommunikationsvorgang_id": number,"p_uebergabe_am": string }; Returns: {
              "archiviert_am": string | null,
"archiviert_von": string | null,
"bearbeitet_am": string,
"bearbeitet_von": string | null,
"betreff": string | null,
"dokumenttyp": string,
"empfaenger_email": string | null,
"entwurf_daten": NonNullable<Json>,
"ersetzt_pachtvertragsfassung_id": number | null,
"erstellt_am": string,
"erstellt_von": string | null,
"fehlertext": string | null,
"final_dokument_id": number | null,
"final_storage_bucket": string | null,
"final_storage_path": string | null,
"gedruckt_am": string | null,
"gedruckt_von": string | null,
"id": number,
"mailtext": string | null,
"mitglied_id": number | null,
"parzelle_id": number | null,
"revision": number,
"signatur_gestartet_am": string | null,
"signatur_revision": number | null,
"status": string,
"versendet_am": string | null,
"versendet_von": string | null,
"vorgangstyp": string,
"vorschau_storage_bucket": string | null,
"vorschau_storage_path": string | null,
"wartungsvertrag_id": number | null,
"wartungsvertrag_zuordnung_id": number | null,
"zugewiesen_an": string | null
            }
                          SetofOptions: {
        from: "*"
        to: "kommunikationsvorgang"
        isOneToOne: true
        isSetofReturn: false
      } },
"end_membership":
{ Args: { "p_main_member_id": number,"p_secondary_decision"?: string }; Returns: {
              "message": string,"secondary_decision": string,"success": boolean,"updated_main_member": Json,"updated_secondary_member": Json
            }[]
                           },
"ensure_aufnahmebestaetigung_dokumentvorgang":
{ Args: { "p_bearbeitung_id": number }; Returns: {
              "archiviert_am": string | null,
"archiviert_von": string | null,
"bearbeitet_am": string,
"bearbeitet_von": string | null,
"betreff": string | null,
"dokumenttyp": string,
"empfaenger_email": string | null,
"entwurf_daten": NonNullable<Json>,
"ersetzt_pachtvertragsfassung_id": number | null,
"erstellt_am": string,
"erstellt_von": string | null,
"fehlertext": string | null,
"final_dokument_id": number | null,
"final_storage_bucket": string | null,
"final_storage_path": string | null,
"gedruckt_am": string | null,
"gedruckt_von": string | null,
"id": number,
"mailtext": string | null,
"mitglied_id": number | null,
"parzelle_id": number | null,
"revision": number,
"signatur_gestartet_am": string | null,
"signatur_revision": number | null,
"status": string,
"versendet_am": string | null,
"versendet_von": string | null,
"vorgangstyp": string,
"vorschau_storage_bucket": string | null,
"vorschau_storage_path": string | null,
"wartungsvertrag_id": number | null,
"wartungsvertrag_zuordnung_id": number | null,
"zugewiesen_an": string | null
            }
                          SetofOptions: {
        from: "*"
        to: "kommunikationsvorgang"
        isOneToOne: true
        isSetofReturn: false
      } },
"ensure_prepared_contract_signatur_position":
{ Args: { "p_kommunikationsvorgang_id": number,"p_mitglied_id"?: number,"p_pflicht": boolean,"p_signatur_revision": number,"p_signatur_schluessel": string,"p_sort_order": number,"p_unterzeichner_name_snapshot"?: string,"p_unterzeichner_typ": string }; Returns: {
              "created_at": string,
"id": number,
"kommunikationsvorgang_id": number,
"mitglied_id": number | null,
"pflicht": boolean,
"signatur_revision": number,
"signatur_schluessel": string,
"signatur_sha256": string | null,
"sort_order": number,
"status": string,
"storage_bucket": string | null,
"storage_path": string | null,
"unterzeichner_name_snapshot": string | null,
"unterzeichner_typ": string,
"unterzeichnet_am": string | null,
"unterzeichnet_von_auth_user_id": string | null,
"updated_at": string,
"vereinsfunktion_besetzung_id": number | null,
"vereinsfunktion_bezeichnung_snapshot": string | null,
"vereinsfunktion_id": number | null,
"vertretene_besetzung_id": number | null,
"vertretene_funktion_bezeichnung_snapshot": string | null,
"vertretene_vereinsfunktion_id": number | null,
"vertretung_id": number | null
            }
                          SetofOptions: {
        from: "*"
        to: "prepared_contract_signatur"
        isOneToOne: true
        isSetofReturn: false
      } },
"ensure_vertragsversand_snapshot":
{ Args: { "p_kommunikationsvorgang_id": number }; Returns: undefined
                           },
"ensure_wartungsvertrag_dokument_workflow":
{ Args: { "p_zuordnung_id": number }; Returns: {
              "bearbeitet_am": string,
"dokument_id": number | null,
"erstellt_am": string,
"fehlermeldung": string | null,
"folgeentscheidung_id": number | null,
"id": number,
"kommunikationsvorgang_id": number | null,
"status": string,
"versuche": number,
"wartungsvertrag_zuordnung_id": number,
"workflow_art": string
            }[]
                          SetofOptions: {
        from: "*"
        to: "wartungsvertrag_dokument_workflow"
        isOneToOne: false
        isSetofReturn: true
      } },
"ensure_wartungsvertrag_prepared_contract":
{ Args: { "p_zuordnung_id": number }; Returns: {
              "archiviert_am": string | null,
"archiviert_von": string | null,
"bearbeitet_am": string,
"bearbeitet_von": string | null,
"betreff": string | null,
"dokumenttyp": string,
"empfaenger_email": string | null,
"entwurf_daten": NonNullable<Json>,
"ersetzt_pachtvertragsfassung_id": number | null,
"erstellt_am": string,
"erstellt_von": string | null,
"fehlertext": string | null,
"final_dokument_id": number | null,
"final_storage_bucket": string | null,
"final_storage_path": string | null,
"gedruckt_am": string | null,
"gedruckt_von": string | null,
"id": number,
"mailtext": string | null,
"mitglied_id": number | null,
"parzelle_id": number | null,
"revision": number,
"signatur_gestartet_am": string | null,
"signatur_revision": number | null,
"status": string,
"versendet_am": string | null,
"versendet_von": string | null,
"vorgangstyp": string,
"vorschau_storage_bucket": string | null,
"vorschau_storage_path": string | null,
"wartungsvertrag_id": number | null,
"wartungsvertrag_zuordnung_id": number | null,
"zugewiesen_an": string | null
            }
                          SetofOptions: {
        from: "*"
        to: "kommunikationsvorgang"
        isOneToOne: true
        isSetofReturn: false
      } },
"entscheide_mitgliedsaufnahme":
{ Args: { "p_aufnahmedatum"?: string,"p_bearbeitung_id": number,"p_begruendung"?: string,"p_ergebnis": string,"p_sitzungsdatum"?: string,"p_weg": string }; Returns: undefined
                           },
"entscheide_wartungsvertrag_nichtverlaengerung":
{ Args: { "p_begruendung"?: string,"p_entscheidungsdatum": string,"p_initiator": string,"p_vorgaenger_zuordnung_id": number,"p_vorstandsbeschlussdatum"?: string }; Returns: number
                           },
"execute_hauptzaehler_meter_change":
{ Args: { "p_alter_zaehler_id": number,"p_anfangsstand": number,"p_anlagenbereich": string,"p_ausbau_foto_pfad": string,"p_ausbaudatum": string,"p_eichdatum": string,"p_einbau_foto_pfad": string,"p_einbaudatum": string,"p_endstand": number,"p_medium": Database["public"]['Enums']["zaehler_medium"],"p_neue_zaehlernummer": string }; Returns: {
              "alter_zaehler_id": number,"neuer_zaehler_id": number
            }[]
                           },
"execute_meter_change":
{ Args: { "p_alter_zaehler_id": number,"p_anfangsstand": number,"p_ausbau_foto_pfad": string,"p_ausbaudatum": string,"p_eichdatum": string,"p_einbau_foto_pfad": string,"p_einbaudatum": string,"p_endstand": number,"p_medium": Database["public"]['Enums']["zaehler_medium"],"p_neue_zaehlernummer": string,"p_parzelle_id": number }; Returns: {
              "alter_zaehler_id": number,"neuer_zaehler_id": number
            }[]
                           },
"find_auth_user_id_by_email":
{ Args: { "p_email": string }; Returns: {
              "auth_user_id": string
            }[]
                           },
"find_scan_context":
{ Args: { "p_rfid_tag_uid": string }; Returns: {
              "aktiver_zaehler_id": number,"anlage": string,"ausgebaut_am": string,"eichdatum": string,"eichfaellig_am": string,"eingebaut_am": string,"garten_nr": string,"medium": Database["public"]['Enums']["zaehler_medium"],"parzelle_id": number,"rfid_tag_uid": string,"status": Database["public"]['Enums']["zaehler_status"],"zaehlernummer": string
            }[]
                           },
"finish_historical_contract_upload":
{ Args: { "p_operation_id": string,"p_storage_path": string }; Returns: Json
                           },
"fn_berechne_pflichtstunden_status":
{ Args: { "p_mitglied_id": number,"p_saison_id": number }; Returns: {
              "altersbefreit": boolean,"eintritt_im_saisonjahr": boolean,"eintritt_zweites_halbjahr": boolean,"euro_pro_fehlstunde": number,"fehlbetrag": number,"geleistete_stunden": number,"hat_wartungsvertrag": boolean,"hauptmitglied_id": number,"ist_befreit": boolean,"offene_stunden": number,"pflichtstunden_soll": number,"regelgrund": string,"saison_id": number,"saison_jahr": number,"wartungsvertrag_gutschrift_stunden": number
            }[]
                           },
"g7_safe_work_hour_snapshot":
{ Args: { "p_value": string }; Returns: Json
                           },
"get_active_meter":
{ Args: { "p_medium": Database["public"]['Enums']["zaehler_medium"],"p_parzelle_id": number }; Returns: {
              "anlagenbereich": string | null,
"ausgebaut_am": string | null,
"bauart": string,
"created_at": string,
"defekt": boolean,
"eichdatum": string,
"eichfaellig_am": string,
"eingebaut_am": string,
"id": number,
"ist_hauptzaehler": boolean,
"medium": Database["public"]['Enums']["zaehler_medium"],
"parzelle_id": number | null,
"status": Database["public"]['Enums']["zaehler_status"],
"updated_at": string,
"zaehlernummer": string
            }
                          SetofOptions: {
        from: "*"
        to: "zaehler"
        isOneToOne: true
        isSetofReturn: false
      } },
"get_auslaufende_wartungsvertrag_zuordnungen":
{ Args: Record<PropertyKey, never>; Returns: {
              "can_decide": boolean,"entscheidung": string,"folgeentscheidung_id": number,"folgezuordnung_id": number,"gueltig_ab": string,"gueltig_bis": string,"hauptmitglied_id": number,"mitglied_name": string,"wartungsvertrag_id": number,"wartungsvertrag_titel": string,"zuordnung_id": number,"zurueckgenommen_am": string
            }[]
                           },
"get_hauptmitglied_id":
{ Args: { "p_mitglied_id": number }; Returns: number
                           },
"get_historical_contract_documents":
{ Args: { "p_mitglied_id": number }; Returns: Json[]
                           },
"get_historical_contract_lease_relationships":
{ Args: { "p_mitglied_id": number }; Returns: Json[]
                           },
"get_historical_contract_members":
{ Args: Record<PropertyKey, never>; Returns: Json[]
                           },
"get_historical_contract_operation":
{ Args: { "p_operation_id": string }; Returns: Json
                           },
"get_historical_contract_stamp_marker":
{ Args: { "p_document_id": number }; Returns: string
                           },
"get_historische_pachtvertragsvorschau":
{ Args: { "p_pachtverhaeltnis_ids": (number)[],"p_vertragsdatum": string }; Returns: {
              "gueltig_bis": string,"historische_vertragsnummer": number,"ist_nachfolger": boolean,"ist_vorgaenger": boolean,"pachtverhaeltnis_id": number,"pachtvertragsfassung_id": number,"vertragspartei_mitglied_ids": (number)[],"vorgeschlagenes_vorgaengerende": string,"wirksam_ab": string
            }[]
                           },
"get_mitglied_vereinsdokument_bereitstellung_historie":
{ Args: { "p_mitglied_id": number }; Returns: {
              "bereitgestellt_am": string,"bereitgestellt_von": string,"bereitstellung_id": number,"bereitstellungsart": string,"bezeichnung": string,"dokumenttyp_code": string,"fassung": string,"mitglied_id": number,"storniert_am": string,"storniert_von": string,"storno_grund": string,"vereinsdokument_fassung_id": number
            }[]
                           },
"get_mitglied_vereinsdokument_status":
{ Args: { "p_mitglied_id": number,"p_stichtag": string }; Returns: {
              "bereitgestellt_am": string,"bereitgestellt_von": string,"bereitstellung_id": number,"bereitstellungsart": string,"bezeichnung": string,"dateiname": string,"dokumenttyp_code": string,"fassung": string,"gueltig_ab": string,"gueltig_bis": string,"pflicht": boolean,"status": string,"storage_path": string,"vereinsdokument_fassung_id": number
            }[]
                           },
"get_user_role":
{ Args: Record<PropertyKey, never>; Returns: string
                           },
"get_wartungsvertrag_folgeentscheidung_detail":
{ Args: { "p_vorgaenger_zuordnung_id": number }; Returns: {
              "begruendung": string,"can_decide": boolean,"can_withdraw_nichtverlaengerung": boolean,"dokument_id": number,"entscheidung": string,"entscheidungsdatum": string,"folge_gueltig_ab": string,"folge_gueltig_bis": string,"folge_wartungsvertrag_id": number,"folge_wartungsvertrag_titel": string,"folgeentscheidung_id": number,"folgezuordnung_id": number,"gueltig_ab": string,"gueltig_bis": string,"hauptmitglied_id": number,"initiator": string,"kommunikationsvorgang_id": number,"ruecknahmebegruendung": string,"vertragspartei_mitglied_id": number,"vorgaenger_zuordnung_id": number,"vorstandsbeschlussdatum": string,"wartungsvertrag_id": number,"workflow_art": string,"workflow_id": number,"workflow_status": string,"zurueckgenommen_am": string
            }[]
                           },
"has_any_effective_permission":
{ Args: { "p_permissions": number }; Returns: boolean
                           },
"has_effective_permission":
{ Args: { "p_permission": number }; Returns: boolean
                           },
"impressum_kontakte":
{ Args: Record<PropertyKey, never>; Returns: {
              "adresse": string,"besetzung_id": number,"bezeichnung": string,"gruppen_bezeichnung": string,"gruppen_schluessel": string,"gruppen_sort_order": number,"handy": string,"ist_hauptkontakt": boolean,"nachname": string,"ort": string,"plz": string,"vorname": string,"zeige_ladungsfaehige_anschrift": boolean
            }[]
                           },
"is_admin":
{ Args: Record<PropertyKey, never>; Returns: boolean
                           },
"is_admin_or_vorstand":
{ Args: Record<PropertyKey, never>; Returns: boolean
                           },
"is_productive_admin_or_vorstand":
{ Args: Record<PropertyKey, never>; Returns: boolean
                           },
"is_registered_historical_contract_document":
{ Args: { "p_dokument_id": number }; Returns: boolean
                           },
"ist_wartungsvertrag_entscheidung_berechtigt":
{ Args: { "p_auth_user_id"?: string,"p_stichtag"?: string }; Returns: boolean
                           },
"kann_mitgliedsaufnahme_entscheiden":
{ Args: Record<PropertyKey, never>; Returns: boolean
                           },
"kann_vertragsversand_ausfuehren":
{ Args: Record<PropertyKey, never>; Returns: boolean
                           },
"kgv_local_now":
{ Args: Record<PropertyKey, never>; Returns: string
                           },
"known_permission_mask":
{ Args: Record<PropertyKey, never>; Returns: number
                           },
"manage_arbeitseinsatz_anmeldung":
{ Args: { "p_action": string,"p_arbeitseinsatz_id": number,"p_mitglied_id": number }; Returns: {
              "angemeldet_am": string,
"arbeitseinsatz_id": number,
"bemerkung": string | null,
"id": number,
"mitglied_id": number,
"status": Database["public"]['Enums']["arbeitseinsatz_anmeldung_status"],
"updated_at": string
            }
                          SetofOptions: {
        from: "*"
        to: "arbeitseinsatz_anmeldung"
        isOneToOne: true
        isSetofReturn: false
      } },
"mark_historische_vertragsnummer_pdf_gestempelt":
{ Args: { "p_dokument_id": number,"p_nummer": number }; Returns: undefined
                           },
"mark_meter_defective":
{ Args: { "p_zaehler_id": number }; Returns: boolean
                           },
"mitgliedsaufnahme_berechtigungen":
{ Args: { "p_stichtag"?: string,"p_verwendungszweck": string }; Returns: {
              "berechtigungsart": string,"funktion_schluessel": string,"funktionsbezeichnung": string,"mitglied_id": number,"name_snapshot": string,"vereinsfunktion_besetzung_id": number,"vereinsfunktion_id": number,"vertretene_besetzung_id": number,"vertretene_funktionsbezeichnung": string,"vertretene_vereinsfunktion_id": number,"vertretung_id": number
            }[]
                           },
"nehme_wartungsvertrag_nichtverlaengerung_zurueck":
{ Args: { "p_folgeentscheidung_id": number,"p_ruecknahmebegruendung"?: string }; Returns: number
                           },
"permission_role_base":
{ Args: { "p_role": string }; Returns: number
                           },
"prepare_contract_dispatch":
{ Args: { "p_erneuter_versand_von_id"?: number,"p_kommunikationsvorgang_id": number }; Returns: number
                           },
"prepare_contract_dispatch_vor_phase12":
{ Args: { "p_erneuter_versand_von_id"?: number,"p_kommunikationsvorgang_id": number }; Returns: number
                           },
"prepare_pachtvertrag_ersatz":
{ Args: { "p_assigned_to": string,"p_vorgaenger_fassung_id": number }; Returns: {
              "kommunikationsvorgang_id": number
            }[]
                           },
"prepare_pachtvertrag_ersatz_vor_phase9":
{ Args: { "p_assigned_to": string,"p_vorgaenger_fassung_id": number }; Returns: {
              "kommunikationsvorgang_id": number
            }[]
                           },
"prepared_contract_signaturen_vollstaendig":
{ Args: { "p_kommunikationsvorgang_id": number,"p_signatur_revision": number }; Returns: boolean
                           },
"prepared_contract_snapshot_unterzeichner":
{ Args: { "p_kommunikationsvorgang_id": number,"p_signatur_revision": number }; Returns: {
              "berechtigungsart": string,"mitglied_id": number,"unterzeichner_name": string,"vereinsfunktion_besetzung_id": number,"vereinsfunktion_bezeichnung": string,"vereinsfunktion_id": number,"vertretene_besetzung_id": number,"vertretene_funktion_bezeichnung": string,"vertretene_vereinsfunktion_id": number,"vertretung_id": number
            }[]
                           },
"provision_app_user_link":
{ Args: { "p_mitglied_id": number,"p_role": string,"p_user_id": string }; Returns: undefined
                           },
"register_historischen_pachtvertrag":
{ Args: { "p_aktiv_status_aenderungen"?: Json,"p_dokument_id": number,"p_dokumenttyp_code": string,"p_mitglied_seit_korrektur_datum"?: string,"p_mitglied_seit_korrektur_mitglied_id"?: number,"p_mitgliedschaftsenden"?: Json,"p_pachtverhaeltnis_ids": (number)[],"p_unterzeichnet": boolean,"p_vertragsdatum": string,"p_vertragspartei_mitglied_ids"?: (number)[],"p_vorherige_enddaten"?: Json }; Returns: {
              "dokument_id": number,"dokumenttyp_id": number,"historische_vertragsnummer": number,"pachtverhaeltnis_id": number,"pachtvertragsfassung_id": number
            }[]
                           } |
{ Args: { "p_aktiv_status_aenderungen"?: Json,"p_dokument_id": number,"p_dokumenttyp_code": string,"p_mitglied_seit_korrektur_datum"?: string,"p_mitglied_seit_korrektur_mitglied_id"?: number,"p_mitgliedschaftsenden"?: Json,"p_operation_id": string,"p_pachtverhaeltnis_ids": (number)[],"p_unterzeichnet": boolean,"p_vertragsdatum": string,"p_vertragspartei_mitglied_ids"?: (number)[],"p_vorherige_enddaten"?: Json }; Returns: {
              "dokument_id": number,"dokumenttyp_id": number,"historische_vertragsnummer": number,"pachtverhaeltnis_id": number,"pachtvertragsfassung_id": number
            }[]
                           },
"register_historischen_pachtvertrag_g11b1":
{ Args: { "p_aktiv_status_aenderungen"?: Json,"p_dokument_id": number,"p_dokumenttyp_code": string,"p_mitglied_seit_korrektur_datum"?: string,"p_mitglied_seit_korrektur_mitglied_id"?: number,"p_mitgliedschaftsenden"?: Json,"p_pachtverhaeltnis_ids": (number)[],"p_unterzeichnet": boolean,"p_vertragsdatum": string,"p_vertragspartei_mitglied_ids"?: (number)[],"p_vorherige_enddaten"?: Json }; Returns: {
              "dokument_id": number,"dokumenttyp_id": number,"historische_vertragsnummer": number,"pachtverhaeltnis_id": number,"pachtvertragsfassung_id": number
            }[]
                           },
"register_historischen_pachtvertrag_g6a":
{ Args: { "p_dokument_id": number,"p_dokumenttyp_code": string,"p_mitglied_seit_korrektur_datum"?: string,"p_mitglied_seit_korrektur_mitglied_id"?: number,"p_pachtverhaeltnis_ids": (number)[],"p_unterzeichnet": boolean,"p_vertragsdatum": string,"p_vorherige_enddaten"?: Json }; Returns: {
              "dokument_id": number,"dokumenttyp_id": number,"historische_vertragsnummer": number,"pachtverhaeltnis_id": number,"pachtvertragsfassung_id": number
            }[]
                           },
"register_historisches_dokument":
{ Args: { "p_dokument_id": number,"p_dokumentdatum": string,"p_dokumenttyp_id": number,"p_pachtverhaeltnis_id"?: number,"p_unterzeichnet": boolean,"p_wartungsvertrag_zuordnung_id"?: number }; Returns: {
              "dokument_id": number,"dokumentdatum": string,"dokumenttyp_id": number,"pachtvertragsfassung_id": number,"unterzeichnet": boolean,"wartungsvertrag_zuordnung_id": number
            }[]
                           },
"register_pachtverhaeltnis_erstvertrag":
{ Args: { "p_created_by": string,"p_dokument_id": number,"p_hauptpaechter_mitglied_id": number,"p_mitpaechter_mitglied_id"?: number,"p_parzelle_id": number,"p_urspruenglicher_pachtbeginn": string,"p_vertragsdatum": string,"p_wirksam_ab": string }; Returns: {
              "pachtverhaeltnis_id": number,"pachtvertragsfassung_id": number
            }[]
                           },
"register_pachtvertrags_neufassung":
{ Args: { "p_created_by": string,"p_dokument_id": number,"p_pachtverhaeltnis_id": number,"p_vertragsdatum": string,"p_vorgaenger_fassung_id": number,"p_wirksam_ab": string }; Returns: {
              "pachtvertragsfassung_id": number
            }[]
                           },
"release_all_browser_edit_locks":
{ Args: Record<PropertyKey, never>; Returns: boolean
                           },
"release_all_browser_edit_locks_core":
{ Args: { "p_client_instance_id": string }; Returns: boolean
                           },
"release_all_browser_edit_locks_for_client":
{ Args: { "p_client_instance_id": string }; Returns: boolean
                           },
"release_browser_edit_lock":
{ Args: { "p_entity_id": string,"p_entity_type": string }; Returns: boolean
                           },
"release_browser_edit_lock_core":
{ Args: { "p_client_instance_id": string,"p_entity_id": string,"p_entity_type": string }; Returns: boolean
                           },
"release_browser_edit_lock_for_client":
{ Args: { "p_client_instance_id": string,"p_entity_id": string,"p_entity_type": string }; Returns: boolean
                           },
"release_historical_contract_stamp":
{ Args: { "p_document_id": number,"p_token": string }; Returns: undefined
                           },
"remove_meter":
{ Args: { "p_ablesedatum"?: string,"p_ausgebaut_am": string,"p_endstand": number,"p_foto_pfad"?: string,"p_zaehler_id": number }; Returns: {
              "anlagenbereich": string | null,
"ausgebaut_am": string | null,
"bauart": string,
"created_at": string,
"defekt": boolean,
"eichdatum": string,
"eichfaellig_am": string,
"eingebaut_am": string,
"id": number,
"ist_hauptzaehler": boolean,
"medium": Database["public"]['Enums']["zaehler_medium"],
"parzelle_id": number | null,
"status": Database["public"]['Enums']["zaehler_status"],
"updated_at": string,
"zaehlernummer": string
            }
                          SetofOptions: {
        from: "*"
        to: "zaehler"
        isOneToOne: true
        isSetofReturn: false
      } },
"reserve_fachnummer":
{ Args: { "p_art": string,"p_quelle_art": string,"p_quelle_id": number }; Returns: number
                           },
"reserve_historische_vertragsnummer":
{ Args: { "p_dokument_id": number }; Returns: {
              "mime_type": string,"nummer": number,"pdf_bereits_gestempelt": boolean,"titel": string
            }[]
                           },
"resolve_rfid_scan_context":
{ Args: { "p_rfid_tag_uid": string }; Returns: {
              "aktiver_zaehler_id": number,"anlage": string,"anlagenbereich": string,"ausgebaut_am": string,"eichdatum": string,"eichfaellig_am": string,"eingebaut_am": string,"garten_nr": string,"ist_hauptzaehler": boolean,"medium": string,"parzelle_id": number,"rfid_tag_uid": string,"status": string,"zaehlernummer": string
            }[]
                           },
"resolve_vertragsunterzeichner":
{ Args: { "p_stichtag": string }; Returns: {
              "berechtigungsart": string,"besetzung_gueltig_ab": string,"besetzung_gueltig_bis": string,"mitglied_id": number,"unterzeichner_name": string,"vereinsfunktion_besetzung_id": number,"vereinsfunktion_bezeichnung": string,"vereinsfunktion_id": number,"vertretene_besetzung_gueltig_ab": string,"vertretene_besetzung_gueltig_bis": string,"vertretene_besetzung_id": number,"vertretene_funktion_bezeichnung": string,"vertretene_vereinsfunktion_id": number,"vertretung_gueltig_ab": string,"vertretung_gueltig_bis": string,"vertretung_id": number
            }[]
                           },
"resolve_vertragsversand_berechtigte":
{ Args: { "p_stichtag"?: string }; Returns: {
              "mitglied_id": number
            }[]
                           },
"review_arbeitsstunde":
{ Args: { "p_aktion": string,"p_arbeitsstunde_id": number,"p_art_der_arbeit"?: string,"p_begruendung": string,"p_datum"?: string,"p_stunden"?: number }; Returns: Json
                           },
"review_meter_change_request":
{ Args: { "p_action": string,"p_antrag_id": number,"p_kommentar"?: string }; Returns: {
              "alter_zaehler_id": number,
"anfangsstand": number,
"antragsteller_mitglied_id": number,
"ausbau_foto_dateiname": string | null,
"ausbau_foto_drive_file_id": string | null,
"ausbau_foto_pfad": string | null,
"ausbaudatum": string,
"ausgefuehrter_neuer_zaehler_id": number | null,
"eichdatum": string,
"einbau_foto_dateiname": string | null,
"einbau_foto_drive_file_id": string | null,
"einbau_foto_pfad": string | null,
"einbaudatum": string,
"eingereicht_am": string,
"endstand": number,
"geprueft_am": string | null,
"geprueft_von": number | null,
"id": number,
"medium": Database["public"]['Enums']["zaehler_medium"],
"neue_zaehlernummer": string,
"parzelle_id": number,
"pruefkommentar": string | null,
"status": string
            }
                          SetofOptions: {
        from: "*"
        to: "zaehlerwechsel_antrag"
        isOneToOne: true
        isSetofReturn: false
      } },
"rpc_execute_export":
{ Args: { "p_export_key": string,"p_parameters"?: Json }; Returns: Json[]
                           },
"rpc_execute_table_or_view_export":
{ Args: { "p_source_name": string }; Returns: Json[]
                           },
"rpc_export_arbeitsstunden_jahre":
{ Args: Record<PropertyKey, never>; Returns: {
              "label": string,"value": string
            }[]
                           },
"rpc_export_arbeitsstunden_uebersicht":
{ Args: { "p_ansicht"?: string,"p_jahr"?: number,"p_stunden_fertig"?: boolean,"p_stunden_offen"?: boolean,"p_wartungsvertraege"?: boolean }; Returns: {
              "datum": string,"garten_nr": string,"geleistete_stunden": number,"hat_wartungsvertrag": boolean,"jahr": number,"leistendes_mitglied": string,"mitglied_id": number,"mitgliedsnummer": number,"nachname": string,"offene_stunden": number,"pflichtstunden_soll": number,"regelgrund": string,"sortierung": number,"status": string,"stunden_dieses_mitglieds": number,"taetigkeit": string,"vorname": string,"wartungsvertraege": string,"wartungsvertrag_gutschrift_stunden": number,"zeilentyp": string
            }[]
                           },
"rpc_export_arbeitsstunden_uebersicht_json":
{ Args: { "p_parameters"?: Json }; Returns: Json[]
                           },
"rpc_export_cx10_test":
{ Args: { "p_parameters"?: Json }; Returns: Json[]
                           },
"rpc_export_datenschutz_aktive_mitglieder":
{ Args: Record<PropertyKey, never>; Returns: {
              "email": string,"email_info": boolean,"email_rechnung": boolean,"garten_nr": string,"mitgliedsnummer": number,"name": string,"whatsapp": boolean
            }[]
                           },
"rpc_export_datenschutz_aktive_mitglieder_json":
{ Args: { "p_parameters"?: Json }; Returns: Json[]
                           },
"rpc_export_jahresablesung_jahre":
{ Args: Record<PropertyKey, never>; Returns: {
              "label": string,"value": string
            }[]
                           },
"rpc_export_jahresablesung_status":
{ Args: { "p_jahr"?: number }; Returns: {
              "eingebaut_am": string,"jae_ablesedatum": string,"jae_stand": number,"jahr": number,"medium": string,"pachtbezug": string,"pachtwechsel_im_jahr": boolean,"parzelle": string,"pruefstatus": string,"status": string,"zaehler_id": number,"zaehlernummer": string
            }[]
                           } |
{ Args: { "p_jahr"?: number,"p_nur_fehlend"?: boolean }; Returns: {
              "eingebaut_am": string,"jae_ablesedatum": string,"jae_stand": number,"jahr": number,"medium": string,"pachtbezug": string,"pachtwechsel_im_jahr": boolean,"parzelle": string,"pruefstatus": string,"status": string,"zaehler_id": number,"zaehlernummer": string
            }[]
                           },
"rpc_export_jahresablesung_status_json":
{ Args: { "p_parameters"?: Json }; Returns: Json[]
                           },
"rpc_export_mitgliederliste":
{ Args: Record<PropertyKey, never>; Returns: Json[]
                           } |
{ Args: { "p_aktiv_filter": string,"p_sortierung": string }; Returns: Json[]
                           },
"rpc_export_mitgliederliste_json":
{ Args: { "p_parameters"?: Json }; Returns: Json[]
                           },
"rpc_export_parzellenliste":
{ Args: { "p_parameters"?: Json }; Returns: Json[]
                           },
"rpc_export_rfid_status":
{ Args: { "p_parameters"?: Json }; Returns: Json[]
                           },
"rpc_export_zaehler_eichfaelligkeit":
{ Args: { "p_medium"?: string }; Returns: {
              "eichdatum": string,"eichfaellig_am": string,"eingebaut_am": string,"faelligkeitsstatus": string,"medium": string,"mitglied_id": number,"nachname": string,"parzelle": string,"parzelle_id": number,"rfid": string,"serienbrief_empfaenger_status": string,"tage_bis_faellig": number,"vorname": string,"zaehler_id": number,"zaehler_status": string,"zaehlernummer": string
            }[]
                           },
"rpc_export_zaehler_eichfaelligkeit_json":
{ Args: { "p_parameters"?: Json }; Returns: Json[]
                           },
"rpc_hauptzaehler_jahresverbrauch_segmente":
{ Args: { "p_jahr": number }; Returns: {
              "anlagenbereich": string,"berechenbar": boolean,"bis_datum": string,"end_ablesung_id": number,"endstand": number,"fehlercode": string,"fehlertext": string,"jahr": number,"medium": string,"start_ablesung_id": number,"startstand": number,"verbrauch": number,"von_datum": string,"zaehler_id": number,"zaehlernummer": string
            }[]
                           },
"rpc_jahresverbrauch_haupt_und_unterzaehler":
{ Args: { "p_jahr": number }; Returns: {
              "anlagenbereich": string,"hauptzaehler_berechenbar": boolean,"hauptzaehler_fehlercode": string,"hauptzaehler_fehlertext": string,"hauptzaehler_verbrauch": number,"jahr": number,"medium": string,"unterzaehler_berechenbar": boolean,"unterzaehler_fehlercode": string,"unterzaehler_fehlertext": string,"unterzaehler_verbrauch": number
            }[]
                           },
"rpc_jahresverbrauch_haupt_und_unterzaehler_g6_basis":
{ Args: { "p_jahr": number }; Returns: {
              "anlagenbereich": string,"hauptzaehler_berechenbar": boolean,"hauptzaehler_fehlercode": string,"hauptzaehler_fehlertext": string,"hauptzaehler_verbrauch": number,"jahr": number,"medium": string,"unterzaehler_berechenbar": boolean,"unterzaehler_fehlercode": string,"unterzaehler_fehlertext": string,"unterzaehler_verbrauch": number
            }[]
                           },
"rpc_jahresverbrauch_haupt_unter_differenz":
{ Args: { "p_jahr": number }; Returns: {
              "anlagenbereich": string,"berechenbar": boolean,"differenz": number,"einheit": string,"fehlercode": string,"fehlertext": string,"hauptzaehler_verbrauch": number,"jahr": number,"medium": string,"pruefbeduerftig": boolean,"unterzaehler_verbrauch": number
            }[]
                           },
"rpc_jahresverbrauch_segmente":
{ Args: { "p_jahr": number }; Returns: {
              "berechenbar": boolean,"bezug_typ": string,"bis_datum": string,"end_ablesung_id": number,"endstand": number,"fehlercode": string,"fehlertext": string,"grund": string,"ist_vereinsgarten": boolean,"jahr": number,"medium": string,"mitglied_id": number,"pachtbezug": string,"parzelle": string,"parzelle_id": number,"start_ablesung_id": number,"startstand": number,"umlagefaehig": boolean,"verbrauch": number,"von_datum": string,"zaehler_id": number,"zaehlernummer": string
            }[]
                           },
"rpc_jahresverbrauch_segmente_g12_basis":
{ Args: { "p_jahr": number }; Returns: {
              "berechenbar": boolean,"bis_datum": string,"end_ablesung_id": number,"endstand": number,"fehlercode": string,"fehlertext": string,"grund": string,"jahr": number,"medium": string,"mitglied_id": number,"pachtbezug": string,"parzelle": string,"parzelle_id": number,"start_ablesung_id": number,"startstand": number,"verbrauch": number,"von_datum": string,"zaehler_id": number,"zaehlernummer": string
            }[]
                           },
"save_dokumenttyp":
{ Args: { "p_aktiv": boolean,"p_bezeichnung": string,"p_dokumenttyp_id": number,"p_sortierung": number,"p_wirkungen"?: (string)[] }; Returns: {
              "aktiv": boolean,"bezeichnung": string,"id": number,"sortierung": number
            }[]
                           },
"sign_off_from_arbeitseinsatz":
{ Args: { "p_arbeitseinsatz_id": number,"p_mitglied_id": number }; Returns: {
              "angemeldet_am": string,
"arbeitseinsatz_id": number,
"bemerkung": string | null,
"id": number,
"mitglied_id": number,
"status": Database["public"]['Enums']["arbeitseinsatz_anmeldung_status"],
"updated_at": string
            }
                          SetofOptions: {
        from: "*"
        to: "arbeitseinsatz_anmeldung"
        isOneToOne: true
        isSetofReturn: false
      } },
"sign_up_for_arbeitseinsatz":
{ Args: { "p_arbeitseinsatz_id": number,"p_mitglied_id": number }; Returns: {
              "angemeldet_am": string,
"arbeitseinsatz_id": number,
"bemerkung": string | null,
"id": number,
"mitglied_id": number,
"status": Database["public"]['Enums']["arbeitseinsatz_anmeldung_status"],
"updated_at": string
            }
                          SetofOptions: {
        from: "*"
        to: "arbeitseinsatz_anmeldung"
        isOneToOne: true
        isSetofReturn: false
      } },
"speichere_prepared_contract_signatur":
{ Args: { "p_kommunikationsvorgang_id": number,"p_mitglied_id": number,"p_signatur_revision": number,"p_signatur_schluessel": string,"p_signatur_sha256": string,"p_storage_path": string,"p_unterzeichner_name_snapshot": string,"p_unterzeichnet_am"?: string,"p_vereinsfunktion_besetzung_id": number,"p_vertretene_besetzung_id": number,"p_vertretung_id": number }; Returns: {
              "created_at": string,
"id": number,
"kommunikationsvorgang_id": number,
"mitglied_id": number | null,
"pflicht": boolean,
"signatur_revision": number,
"signatur_schluessel": string,
"signatur_sha256": string | null,
"sort_order": number,
"status": string,
"storage_bucket": string | null,
"storage_path": string | null,
"unterzeichner_name_snapshot": string | null,
"unterzeichner_typ": string,
"unterzeichnet_am": string | null,
"unterzeichnet_von_auth_user_id": string | null,
"updated_at": string,
"vereinsfunktion_besetzung_id": number | null,
"vereinsfunktion_bezeichnung_snapshot": string | null,
"vereinsfunktion_id": number | null,
"vertretene_besetzung_id": number | null,
"vertretene_funktion_bezeichnung_snapshot": string | null,
"vertretene_vereinsfunktion_id": number | null,
"vertretung_id": number | null
            }
                          SetofOptions: {
        from: "*"
        to: "prepared_contract_signatur"
        isOneToOne: true
        isSetofReturn: false
      } },
"speichere_serienbrief_vorlage":
{ Args: { "p_aktiv": boolean,"p_betreff_vorlage": string,"p_exportfelder"?: Json,"p_freifelder"?: Json,"p_html_inhalt": string,"p_id": number,"p_name": string }; Returns: {
              "aktiv": boolean,
"anrede_modus": string,
"betreff_vorlage": string,
"erstellt_am": string,
"erstellt_von": string | null,
"geaendert_am": string,
"geaendert_von": string | null,
"grundlayout_template_key": string,
"grundlayout_version": string,
"html_inhalt": string,
"id": number,
"name": string
            }
                          SetofOptions: {
        from: "*"
        to: "serienbrief_vorlage"
        isOneToOne: true
        isSetofReturn: false
      } },
"start_mitgliedsaufnahme_vorstandsbearbeitung":
{ Args: { "p_kommunikationsvorgang_id": number }; Returns: number
                           },
"start_prepared_contract_signature":
{ Args: { "p_assigned_to": string,"p_expected_revision": number,"p_kommunikationsvorgang_id": number }; Returns: {
              "archiviert_am": string | null,
"archiviert_von": string | null,
"bearbeitet_am": string,
"bearbeitet_von": string | null,
"betreff": string | null,
"dokumenttyp": string,
"empfaenger_email": string | null,
"entwurf_daten": NonNullable<Json>,
"ersetzt_pachtvertragsfassung_id": number | null,
"erstellt_am": string,
"erstellt_von": string | null,
"fehlertext": string | null,
"final_dokument_id": number | null,
"final_storage_bucket": string | null,
"final_storage_path": string | null,
"gedruckt_am": string | null,
"gedruckt_von": string | null,
"id": number,
"mailtext": string | null,
"mitglied_id": number | null,
"parzelle_id": number | null,
"revision": number,
"signatur_gestartet_am": string | null,
"signatur_revision": number | null,
"status": string,
"versendet_am": string | null,
"versendet_von": string | null,
"vorgangstyp": string,
"vorschau_storage_bucket": string | null,
"vorschau_storage_path": string | null,
"wartungsvertrag_id": number | null,
"wartungsvertrag_zuordnung_id": number | null,
"zugewiesen_an": string | null
            }
                          SetofOptions: {
        from: "*"
        to: "kommunikationsvorgang"
        isOneToOne: true
        isSetofReturn: false
      } },
"start_wartungsvertrag_grundlegende_aenderung":
{ Args: { "p_arbeitsstunden_gutschrift": number,"p_befreit_von_pflichtstunden": boolean,"p_bemerkung"?: string,"p_bereich": string,"p_beschreibung": string,"p_gueltig_ab": string,"p_max_aktive_zuordnungen": number,"p_titel": string,"p_vorgaenger_zuordnung_id": number }; Returns: {
              "kommunikationsvorgang_id": number,"status": string,"wartungsvertrag_id": number
            }[]
                           },
"stimme_mitgliedsaufnahme_ab":
{ Args: { "p_bearbeitung_id": number,"p_nein_begruendung"?: string,"p_stimme": string }; Returns: number
                           },
"submit_arbeitseinsatz_arbeitsstunde":
{ Args: { "p_arbeitseinsatz_anmeldung_id": number,"p_art_der_arbeit"?: string,"p_stunden": number }; Returns: {
              "arbeitseinsatz_anmeldung_id": number | null,
"art_der_arbeit": string,
"datum": string,
"freigegeben": boolean,
"genehmigt_am": string | null,
"genehmigt_von": number | null,
"id": number,
"lockat": string | null,
"lockedbyuserid": string | null,
"mitglied_id": number,
"saison_id": number,
"status": string | null,
"stunden": number
            }
                          SetofOptions: {
        from: "*"
        to: "arbeitsstunde"
        isOneToOne: true
        isSetofReturn: false
      } },
"submit_own_meter_change_request":
{ Args: { "p_alter_zaehler_id": number,"p_anfangsstand": number,"p_ausbau_foto_pfad": string,"p_ausbaudatum": string,"p_eichdatum": string,"p_einbau_foto_pfad": string,"p_einbaudatum": string,"p_endstand": number,"p_neue_zaehlernummer": string }; Returns: {
              "alter_zaehler_id": number,
"anfangsstand": number,
"antragsteller_mitglied_id": number,
"ausbau_foto_dateiname": string | null,
"ausbau_foto_drive_file_id": string | null,
"ausbau_foto_pfad": string | null,
"ausbaudatum": string,
"ausgefuehrter_neuer_zaehler_id": number | null,
"eichdatum": string,
"einbau_foto_dateiname": string | null,
"einbau_foto_drive_file_id": string | null,
"einbau_foto_pfad": string | null,
"einbaudatum": string,
"eingereicht_am": string,
"endstand": number,
"geprueft_am": string | null,
"geprueft_von": number | null,
"id": number,
"medium": Database["public"]['Enums']["zaehler_medium"],
"neue_zaehlernummer": string,
"parzelle_id": number,
"pruefkommentar": string | null,
"status": string
            }
                          SetofOptions: {
        from: "*"
        to: "zaehlerwechsel_antrag"
        isOneToOne: true
        isSetofReturn: false
      } },
"submit_own_meter_reading":
{ Args: { "p_ablesedatum": string,"p_art": Database["public"]['Enums']["ablesung_art"],"p_foto_dateiname"?: string,"p_foto_drive_file_id"?: string,"p_foto_pfad"?: string,"p_stand": number,"p_zaehler_id": number }; Returns: {
              "ablesedatum": string,
"art": Database["public"]['Enums']["ablesung_art"],
"created_at": string,
"foto_dateiname": string | null,
"foto_drive_file_id": string | null,
"foto_pfad": string | null,
"freigegeben": boolean,
"geprueft_am": string | null,
"geprueft_von": number | null,
"id": number,
"pruefkommentar": string | null,
"pruefstatus": Database["public"]['Enums']["ablesung_pruefstatus"],
"stand": number,
"zaehler_id": number
            }
                          SetofOptions: {
        from: "*"
        to: "zaehler_ablesung"
        isOneToOne: true
        isSetofReturn: false
      } },
"sync_current_member_email":
{ Args: Record<PropertyKey, never>; Returns: string
                           },
"try_lock_mitglied":
{ Args: { "p_id": number,"p_timeout_minutes"?: number,"p_user_id": string }; Returns: boolean
                           },
"unlink_app_user":
{ Args: { "p_user_id": string }; Returns: undefined
                           },
"update_app_user_access":
{ Args: { "p_permission_grants"?: number,"p_permission_revocations"?: number,"p_role": string,"p_user_id": string }; Returns: undefined
                           },
"vereinsfunktion_besetzungen_fuer_verwendungszweck":
{ Args: { "p_stichtag"?: string,"p_verwendungszweck": string }; Returns: {
              "besetzung_id": number,"bezeichnung": string,"gueltig_ab": string,"gueltig_bis": string,"mitglied_id": number,"schluessel": string,"sort_order": number,"vereinsfunktion_id": number
            }[]
                           },
"vereinsfunktion_vertragsunterzeichner":
{ Args: { "p_stichtag"?: string }; Returns: {
              "berechtigungsart": string,"besetzung_sort_order": number,"funktions_sort_order": number,"mitglied_id": number,"unterzeichner_name": string,"vereinsfunktion_besetzung_id": number,"vereinsfunktion_bezeichnung": string,"vereinsfunktion_id": number,"vertretene_besetzung_id": number,"vertretene_funktion_bezeichnung": string,"vertretene_vereinsfunktion_id": number,"vertretung_id": number,"verwendungszweck_sort_order": number
            }[]
                           },
"verlaengere_wartungsvertrag":
{ Args: { "p_bemerkung"?: string,"p_neues_gueltig_bis": string,"p_vorgaenger_zuordnung_id": number,"p_vorstandsbeschlussdatum": string }; Returns: {
              "folgeentscheidung_id": number,"folgezuordnung_id": number,"kommunikationsvorgang_id": number,"vertragspartei_mitglied_id": number,"workflow_id": number
            }[]
                           },
"vertragsversand_mailtext_snapshot":
{ Args: Record<PropertyKey, never>; Returns: string
                           },
"who_am_i":
{ Args: Record<PropertyKey, never>; Returns: {
              "role": string,"vorname": string
            }[]
                           }
          }
          Enums: {
            "ablesung_art": "normal"|"ausbau"|"jae"|"einbau"|"jea","ablesung_pruefstatus": "eingereicht"|"freigegeben"|"abgelehnt","arbeitseinsatz_anmeldung_status": "angemeldet"|"abgesagt"|"teilgenommen"|"nicht_erschienen","zaehler_medium": "wasser"|"strom","zaehler_status": "aktiv"|"ausgebaut"
          }
          CompositeTypes: {
            [_ in never]: never
          }
        }
}

type DatabaseWithoutInternals = Omit<Database, '__InternalSupabase'>

type DefaultSchema = DatabaseWithoutInternals[Extract<keyof Database, "public">]

export type Tables<
  DefaultSchemaTableNameOrOptions extends
    | keyof (DefaultSchema["Tables"] & DefaultSchema["Views"])
    | { schema: keyof DatabaseWithoutInternals },
  TableName extends DefaultSchemaTableNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof (DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"] &
        DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Views"])
    : never = never
> = DefaultSchemaTableNameOrOptions extends { schema: keyof DatabaseWithoutInternals }
  ? (DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"] &
      DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Views"])[TableName] extends {
      Row: infer R
    }
    ? R
    : never
  : DefaultSchemaTableNameOrOptions extends keyof (DefaultSchema["Tables"] & DefaultSchema["Views"])
  ? (DefaultSchema["Tables"] & DefaultSchema["Views"])[DefaultSchemaTableNameOrOptions] extends {
      Row: infer R
    }
    ? R
    : never
  : never

export type TablesInsert<
  DefaultSchemaTableNameOrOptions extends
    | keyof DefaultSchema["Tables"]
    | { schema: keyof DatabaseWithoutInternals },
  TableName extends DefaultSchemaTableNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"]
    : never = never
> = DefaultSchemaTableNameOrOptions extends { schema: keyof DatabaseWithoutInternals }
  ? DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"][TableName] extends {
      Insert: infer I
    }
    ? I
    : never
  : DefaultSchemaTableNameOrOptions extends keyof DefaultSchema["Tables"]
  ? DefaultSchema["Tables"][DefaultSchemaTableNameOrOptions] extends {
      Insert: infer I
    }
    ? I
    : never
  : never

export type TablesUpdate<
  DefaultSchemaTableNameOrOptions extends
    | keyof DefaultSchema["Tables"]
    | { schema: keyof DatabaseWithoutInternals },
  TableName extends DefaultSchemaTableNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"]
    : never = never
> = DefaultSchemaTableNameOrOptions extends { schema: keyof DatabaseWithoutInternals }
  ? DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"][TableName] extends {
      Update: infer U
    }
    ? U
    : never
  : DefaultSchemaTableNameOrOptions extends keyof DefaultSchema["Tables"]
  ? DefaultSchema["Tables"][DefaultSchemaTableNameOrOptions] extends {
      Update: infer U
    }
    ? U
    : never
  : never

export type Enums<
  DefaultSchemaEnumNameOrOptions extends
    | keyof DefaultSchema["Enums"]
    | { schema: keyof DatabaseWithoutInternals },
  EnumName extends DefaultSchemaEnumNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof DatabaseWithoutInternals[DefaultSchemaEnumNameOrOptions["schema"]]["Enums"]
    : never = never
> = DefaultSchemaEnumNameOrOptions extends { schema: keyof DatabaseWithoutInternals }
  ? DatabaseWithoutInternals[DefaultSchemaEnumNameOrOptions["schema"]]["Enums"][EnumName]
  : DefaultSchemaEnumNameOrOptions extends keyof DefaultSchema["Enums"]
  ? DefaultSchema["Enums"][DefaultSchemaEnumNameOrOptions]
  : never

export type CompositeTypes<
  PublicCompositeTypeNameOrOptions extends
    | keyof DefaultSchema["CompositeTypes"]
    | { schema: keyof DatabaseWithoutInternals },
  CompositeTypeName extends PublicCompositeTypeNameOrOptions extends {
    schema: keyof DatabaseWithoutInternals
  }
    ? keyof DatabaseWithoutInternals[PublicCompositeTypeNameOrOptions["schema"]]["CompositeTypes"]
    : never = never
> = PublicCompositeTypeNameOrOptions extends { schema: keyof DatabaseWithoutInternals }
  ? DatabaseWithoutInternals[PublicCompositeTypeNameOrOptions["schema"]]["CompositeTypes"][CompositeTypeName]
  : PublicCompositeTypeNameOrOptions extends keyof DefaultSchema["CompositeTypes"]
  ? DefaultSchema["CompositeTypes"][PublicCompositeTypeNameOrOptions]
  : never

export const Constants = {
  "graphql_public": {
          Enums: {

          }
        },"public": {
          Enums: {
            "ablesung_art": ["normal", "ausbau", "jae", "einbau", "jea"],"ablesung_pruefstatus": ["eingereicht", "freigegeben", "abgelehnt"],"arbeitseinsatz_anmeldung_status": ["angemeldet", "abgesagt", "teilgenommen", "nicht_erschienen"],"zaehler_medium": ["wasser", "strom"],"zaehler_status": ["aktiv", "ausgebaut"]
          }
        }
} as const
