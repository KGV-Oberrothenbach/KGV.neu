export type Json =
  | string
  | number
  | boolean
  | null
  | { [key: string]: Json | undefined }
  | Json[]

export type Database = {
  // Allows to automatically instantiate createClient with right options
  // instead of createClient<Database, { PostgrestVersion: 'XX' }>(URL, KEY)
  __InternalSupabase: {
    PostgrestVersion: "14.1"
  }
  public: {
    Tables: {
      app_user: {
        Row: {
          created_at: string
          is_demo_account: boolean
          mitglied_id: number | null
          permission_grants: number
          permission_revocations: number
          role: string
          updated_at: string
          user_id: string
        }
        Insert: {
          created_at?: string
          is_demo_account?: boolean
          mitglied_id?: number | null
          permission_grants?: number
          permission_revocations?: number
          role?: string
          updated_at?: string
          user_id: string
        }
        Update: {
          created_at?: string
          is_demo_account?: boolean
          mitglied_id?: number | null
          permission_grants?: number
          permission_revocations?: number
          role?: string
          updated_at?: string
          user_id?: string
        }
        Relationships: [
          {
            foreignKeyName: "app_user_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "app_user_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
        ]
      }
      arbeitseinsatz: {
        Row: {
          aktiv: boolean
          anmeldung_bis: string | null
          beschreibung: string | null
          created_at: string
          datum: string
          end_uhrzeit: string | null
          id: number
          is_demo: boolean
          max_teilnehmer: number | null
          sichtbar_ab: string | null
          sichtbar_bis: string | null
          start_uhrzeit: string | null
          stunden_wert: number
          titel: string
          treffpunkt: string | null
          updated_at: string
        }
        Insert: {
          aktiv?: boolean
          anmeldung_bis?: string | null
          beschreibung?: string | null
          created_at?: string
          datum: string
          end_uhrzeit?: string | null
          id?: number
          is_demo?: boolean
          max_teilnehmer?: number | null
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          start_uhrzeit?: string | null
          stunden_wert?: number
          titel: string
          treffpunkt?: string | null
          updated_at?: string
        }
        Update: {
          aktiv?: boolean
          anmeldung_bis?: string | null
          beschreibung?: string | null
          created_at?: string
          datum?: string
          end_uhrzeit?: string | null
          id?: number
          is_demo?: boolean
          max_teilnehmer?: number | null
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          start_uhrzeit?: string | null
          stunden_wert?: number
          titel?: string
          treffpunkt?: string | null
          updated_at?: string
        }
        Relationships: []
      }
      arbeitseinsatz_anmeldung: {
        Row: {
          angemeldet_am: string
          arbeitseinsatz_id: number
          bemerkung: string | null
          id: number
          mitglied_id: number
          status: Database["public"]["Enums"]["arbeitseinsatz_anmeldung_status"]
          updated_at: string
        }
        Insert: {
          angemeldet_am?: string
          arbeitseinsatz_id: number
          bemerkung?: string | null
          id?: number
          mitglied_id: number
          status?: Database["public"]["Enums"]["arbeitseinsatz_anmeldung_status"]
          updated_at?: string
        }
        Update: {
          angemeldet_am?: string
          arbeitseinsatz_id?: number
          bemerkung?: string | null
          id?: number
          mitglied_id?: number
          status?: Database["public"]["Enums"]["arbeitseinsatz_anmeldung_status"]
          updated_at?: string
        }
        Relationships: [
          {
            foreignKeyName: "arbeitseinsatz_anmeldung_arbeitseinsatz_id_fkey"
            columns: ["arbeitseinsatz_id"]
            isOneToOne: false
            referencedRelation: "arbeitseinsatz"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "arbeitseinsatz_anmeldung_arbeitseinsatz_id_fkey"
            columns: ["arbeitseinsatz_id"]
            isOneToOne: false
            referencedRelation: "v_startseite_arbeitseinsatz"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "arbeitseinsatz_anmeldung_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "arbeitseinsatz_anmeldung_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
        ]
      }
      app_setting: {
        Row: {
          bool_value: boolean
          setting_key: string
          updated_at: string
        }
        Insert: {
          bool_value?: boolean
          setting_key: string
          updated_at?: string
        }
        Update: {
          bool_value?: boolean
          setting_key?: string
          updated_at?: string
        }
        Relationships: []
      }
      arbeitsstunde: {
        Row: {
          art_der_arbeit: string
          datum: string
          freigegeben: boolean
          genehmigt_am: string | null
          genehmigt_von: number | null
          id: number
          lockat: string | null
          lockedbyuserid: string | null
          mitglied_id: number
          saison_id: number
          status: string | null
          stunden: number
        }
        Insert: {
          art_der_arbeit: string
          datum: string
          freigegeben?: boolean
          genehmigt_am?: string | null
          genehmigt_von?: number | null
          id?: number
          lockat?: string | null
          lockedbyuserid?: string | null
          mitglied_id: number
          saison_id: number
          status?: string | null
          stunden: number
        }
        Update: {
          art_der_arbeit?: string
          datum?: string
          freigegeben?: boolean
          genehmigt_am?: string | null
          genehmigt_von?: number | null
          id?: number
          lockat?: string | null
          lockedbyuserid?: string | null
          mitglied_id?: number
          saison_id?: number
          status?: string | null
          stunden?: number
        }
        Relationships: [
          {
            foreignKeyName: "arbeitsstunde_genehmigt_von_fkey"
            columns: ["genehmigt_von"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "arbeitsstunde_genehmigt_von_fkey"
            columns: ["genehmigt_von"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
          {
            foreignKeyName: "arbeitsstunde_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "arbeitsstunde_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
          {
            foreignKeyName: "arbeitsstunde_saison_id_fkey"
            columns: ["saison_id"]
            isOneToOne: false
            referencedRelation: "saison"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "arbeitsstunde_saison_id_fkey"
            columns: ["saison_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["saison_id"]
          },
        ]
      }
      auth_allowlist: {
        Row: {
          allow_demo_access: boolean
          allow_email_otp: boolean
          allow_google: boolean
          allow_playstore_review: boolean
          created_at: string
          email: string
          is_active: boolean
          note: string | null
        }
        Insert: {
          allow_demo_access?: boolean
          allow_email_otp?: boolean
          allow_google?: boolean
          allow_playstore_review?: boolean
          created_at?: string
          email: string
          is_active?: boolean
          note?: string | null
        }
        Update: {
          allow_demo_access?: boolean
          allow_email_otp?: boolean
          allow_google?: boolean
          allow_playstore_review?: boolean
          created_at?: string
          email?: string
          is_active?: boolean
          note?: string | null
        }
        Relationships: []
      }
      bekanntmachung: {
        Row: {
          aktiv: boolean
          created_at: string
          id: number
          inhalt_html: string
          sichtbar_ab: string | null
          sichtbar_bis: string | null
          sort_order: number | null
          titel: string
          updated_at: string
        }
        Insert: {
          aktiv?: boolean
          created_at?: string
          id?: number
          inhalt_html: string
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          sort_order?: number | null
          titel: string
          updated_at?: string
        }
        Update: {
          aktiv?: boolean
          created_at?: string
          id?: number
          inhalt_html?: string
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          sort_order?: number | null
          titel?: string
          updated_at?: string
        }
        Relationships: []
      }
      client_diagnostics_log: {
        Row: {
          app: string
          category: string
          client_request_id: string | null
          created_at: string
          environment: string
          extra: Json
          has_access_token: boolean | null
          http_status: number | null
          id: string
          level: string
          message: string
          raw_body: string | null
          retry_attempted: boolean | null
          token_length: number | null
          user_id: string | null
        }
        Insert: {
          app: string
          category: string
          client_request_id?: string | null
          created_at?: string
          environment?: string
          extra?: Json
          has_access_token?: boolean | null
          http_status?: number | null
          id?: string
          level?: string
          message: string
          raw_body?: string | null
          retry_attempted?: boolean | null
          token_length?: number | null
          user_id?: string | null
        }
        Update: {
          app?: string
          category?: string
          client_request_id?: string | null
          created_at?: string
          environment?: string
          extra?: Json
          has_access_token?: boolean | null
          http_status?: number | null
          id?: string
          level?: string
          message?: string
          raw_body?: string | null
          retry_attempted?: boolean | null
          token_length?: number | null
          user_id?: string | null
        }
        Relationships: []
      }
      dokument: {
        Row: {
          bucket: string
          created_at: string
          created_by: string | null
          dateiname: string | null
          drive_file_id: string | null
          id: number
          mime_type: string | null
          mitglied_id: number | null
          parzelle_id: number | null
          size_bytes: number | null
          storage_path: string
          titel: string | null
          updated_at: string
        }
        Insert: {
          bucket?: string
          created_at?: string
          created_by?: string | null
          dateiname?: string | null
          drive_file_id?: string | null
          id?: number
          mime_type?: string | null
          mitglied_id?: number | null
          parzelle_id?: number | null
          size_bytes?: number | null
          storage_path: string
          titel?: string | null
          updated_at?: string
        }
        Update: {
          bucket?: string
          created_at?: string
          created_by?: string | null
          dateiname?: string | null
          drive_file_id?: string | null
          id?: number
          mime_type?: string | null
          mitglied_id?: number | null
          parzelle_id?: number | null
          size_bytes?: number | null
          storage_path?: string
          titel?: string | null
          updated_at?: string
        }
        Relationships: [
          {
            foreignKeyName: "dokument_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "dokument_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
          {
            foreignKeyName: "dokument_parzelle_id_fkey"
            columns: ["parzelle_id"]
            isOneToOne: false
            referencedRelation: "parzelle"
            referencedColumns: ["id"]
          },
        ]
      }
      impressum_funktion_slot: {
        Row: {
          created_at: string
          funktion: string
          id: number
          mitglied_id: number | null
          slot_key: string
          sort_order: number
          updated_at: string
        }
        Insert: {
          created_at?: string
          funktion: string
          id?: number
          mitglied_id?: number | null
          slot_key: string
          sort_order: number
          updated_at?: string
        }
        Update: {
          created_at?: string
          funktion?: string
          id?: number
          mitglied_id?: number | null
          slot_key?: string
          sort_order?: number
          updated_at?: string
        }
        Relationships: [
          {
            foreignKeyName: "fk_impressum_funktion_slot_mitglied"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "fk_impressum_funktion_slot_mitglied"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
        ]
      }
      mitglied: {
        Row: {
          adresse: string | null
          aktiv: boolean
          arbeitsstunden_altersregel_typ: string
          auth_user_id: string | null
          bemerkung: string | null
          email: string | null
          email_info_einwilligung: boolean
          email_rechnung_einwilligung: boolean
          geburtsdatum: string | null
          handy: string | null
          hauptmitglied_id: number | null
          id: number
          is_demo: boolean
          ist_kgv: boolean
          lockat: string | null
          lockedbyuserid: string | null
          mitglied_ende: string | null
          mitglied_seit: string
          name: string
          ort: string | null
          plz: string | null
          role: string
          telefon: string | null
          vorname: string
          whatsapp_einwilligung: boolean
        }
        Insert: {
          adresse?: string | null
          aktiv?: boolean
          arbeitsstunden_altersregel_typ?: string
          auth_user_id?: string | null
          bemerkung?: string | null
          email?: string | null
          email_info_einwilligung?: boolean
          email_rechnung_einwilligung?: boolean
          geburtsdatum?: string | null
          handy?: string | null
          hauptmitglied_id?: number | null
          id?: number
          is_demo?: boolean
          ist_kgv?: boolean
          lockat?: string | null
          lockedbyuserid?: string | null
          mitglied_ende?: string | null
          mitglied_seit?: string
          name: string
          ort?: string | null
          plz?: string | null
          role?: string
          telefon?: string | null
          vorname: string
          whatsapp_einwilligung?: boolean
        }
        Update: {
          adresse?: string | null
          aktiv?: boolean
          arbeitsstunden_altersregel_typ?: string
          auth_user_id?: string | null
          bemerkung?: string | null
          email?: string | null
          email_info_einwilligung?: boolean
          email_rechnung_einwilligung?: boolean
          geburtsdatum?: string | null
          handy?: string | null
          hauptmitglied_id?: number | null
          id?: number
          is_demo?: boolean
          ist_kgv?: boolean
          lockat?: string | null
          lockedbyuserid?: string | null
          mitglied_ende?: string | null
          mitglied_seit?: string
          name?: string
          ort?: string | null
          plz?: string | null
          role?: string
          telefon?: string | null
          vorname?: string
          whatsapp_einwilligung?: boolean
        }
        Relationships: [
          {
            foreignKeyName: "mitglied_hauptmitglied_id_fkey"
            columns: ["hauptmitglied_id"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "mitglied_hauptmitglied_id_fkey"
            columns: ["hauptmitglied_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
        ]
      }
      mitglied_saison: {
        Row: {
          beitrag: number
          id: number
          mitglied_id: number
          pflichtstunden: number
          saison_id: number
          status: number
        }
        Insert: {
          beitrag?: number
          id?: number
          mitglied_id: number
          pflichtstunden?: number
          saison_id: number
          status?: number
        }
        Update: {
          beitrag?: number
          id?: number
          mitglied_id?: number
          pflichtstunden?: number
          saison_id?: number
          status?: number
        }
        Relationships: [
          {
            foreignKeyName: "mitglied_saison_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "mitglied_saison_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
          {
            foreignKeyName: "mitglied_saison_saison_id_fkey"
            columns: ["saison_id"]
            isOneToOne: false
            referencedRelation: "saison"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "mitglied_saison_saison_id_fkey"
            columns: ["saison_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["saison_id"]
          },
        ]
      }
      parzelle: {
        Row: {
          aktiv: boolean
          Anlage: string
          flaeche_qm: number | null
          garten_nr: string
          hat_strom: boolean
          hat_wasser: boolean
          id: number
          is_demo: boolean
          rfid_strom: string | null
          rfid_wasser: string | null
        }
        Insert: {
          aktiv?: boolean
          Anlage: string
          flaeche_qm?: number | null
          garten_nr: string
          hat_strom?: boolean
          hat_wasser?: boolean
          id?: number
          is_demo?: boolean
          rfid_strom?: string | null
          rfid_wasser?: string | null
        }
        Update: {
          aktiv?: boolean
          Anlage?: string
          flaeche_qm?: number | null
          garten_nr?: string
          hat_strom?: boolean
          hat_wasser?: boolean
          id?: number
          is_demo?: boolean
          rfid_strom?: string | null
          rfid_wasser?: string | null
        }
        Relationships: []
      }
      parzellen_belegung: {
        Row: {
          bis_datum: string | null
          id: number
          mitglied_id: number
          parzelle_id: number
          von_datum: string
        }
        Insert: {
          bis_datum?: string | null
          id?: number
          mitglied_id: number
          parzelle_id: number
          von_datum: string
        }
        Update: {
          bis_datum?: string | null
          id?: number
          mitglied_id?: number
          parzelle_id?: number
          von_datum?: string
        }
        Relationships: [
          {
            foreignKeyName: "parzellen_belegung_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "parzellen_belegung_mitglied_id_fkey"
            columns: ["mitglied_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
          {
            foreignKeyName: "parzellen_belegung_parzelle_id_fkey"
            columns: ["parzelle_id"]
            isOneToOne: false
            referencedRelation: "parzelle"
            referencedColumns: ["id"]
          },
        ]
      }
      saison: {
        Row: {
          bemerkung: string | null
          euro_pro_fehlstunde: number
          id: number
          jahr: number | null
          pflichtstunden_soll: number
        }
        Insert: {
          bemerkung?: string | null
          euro_pro_fehlstunde?: number
          id?: number
          jahr?: number | null
          pflichtstunden_soll?: number
        }
        Update: {
          bemerkung?: string | null
          euro_pro_fehlstunde?: number
          id?: number
          jahr?: number | null
          pflichtstunden_soll?: number
        }
        Relationships: []
      }
      termin: {
        Row: {
          aktiv: boolean
          beschreibung: string | null
          created_at: string
          datum: string
          end_uhrzeit: string | null
          id: number
          sichtbar_ab: string | null
          sichtbar_bis: string | null
          start_uhrzeit: string | null
          titel: string
          updated_at: string
        }
        Insert: {
          aktiv?: boolean
          beschreibung?: string | null
          created_at?: string
          datum: string
          end_uhrzeit?: string | null
          id?: number
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          start_uhrzeit?: string | null
          titel: string
          updated_at?: string
        }
        Update: {
          aktiv?: boolean
          beschreibung?: string | null
          created_at?: string
          datum?: string
          end_uhrzeit?: string | null
          id?: number
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          start_uhrzeit?: string | null
          titel?: string
          updated_at?: string
        }
        Relationships: []
      }
      wartungsvertraege: {
        Row: {
          aktiv: boolean
          befreit_von_pflichtstunden: boolean
          bemerkung: string | null
          bereich: string | null
          beschreibung: string | null
          created_at: string
          id: number
          is_demo: boolean
          max_aktive_zuordnungen: number
          titel: string
          updated_at: string
        }
        Insert: {
          aktiv?: boolean
          befreit_von_pflichtstunden?: boolean
          bemerkung?: string | null
          bereich?: string | null
          beschreibung?: string | null
          created_at?: string
          id?: number
          is_demo?: boolean
          max_aktive_zuordnungen?: number
          titel: string
          updated_at?: string
        }
        Update: {
          aktiv?: boolean
          befreit_von_pflichtstunden?: boolean
          bemerkung?: string | null
          bereich?: string | null
          beschreibung?: string | null
          created_at?: string
          id?: number
          is_demo?: boolean
          max_aktive_zuordnungen?: number
          titel?: string
          updated_at?: string
        }
        Relationships: []
      }
      wartungsvertrag_zuordnungen: {
        Row: {
          bemerkung: string | null
          created_at: string
          gueltig_ab: string
          gueltig_bis: string | null
          hauptmitglied_id: number
          id: number
          updated_at: string
          wartungsvertrag_id: number
        }
        Insert: {
          bemerkung?: string | null
          created_at?: string
          gueltig_ab: string
          gueltig_bis?: string | null
          hauptmitglied_id: number
          id?: number
          updated_at?: string
          wartungsvertrag_id: number
        }
        Update: {
          bemerkung?: string | null
          created_at?: string
          gueltig_ab?: string
          gueltig_bis?: string | null
          hauptmitglied_id?: number
          id?: number
          updated_at?: string
          wartungsvertrag_id?: number
        }
        Relationships: [
          {
            foreignKeyName: "fk_wvz_hauptmitglied"
            columns: ["hauptmitglied_id"]
            isOneToOne: false
            referencedRelation: "mitglied"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "fk_wvz_hauptmitglied"
            columns: ["hauptmitglied_id"]
            isOneToOne: false
            referencedRelation: "v_pflichtstunden_uebersicht"
            referencedColumns: ["hauptmitglied_id"]
          },
          {
            foreignKeyName: "fk_wvz_wartungsvertrag"
            columns: ["wartungsvertrag_id"]
            isOneToOne: false
            referencedRelation: "wartungsvertraege"
            referencedColumns: ["id"]
          },
        ]
      }
      zaehler: {
        Row: {
          ausgebaut_am: string | null
          created_at: string
          eichdatum: string
          eichfaellig_am: string
          einbau_foto_dateiname: string | null
          einbau_foto_drive_file_id: string | null
          einbau_foto_pfad: string | null
          eingebaut_am: string
          id: number
          medium: Database["public"]["Enums"]["zaehler_medium"]
          parzelle_id: number
          status: Database["public"]["Enums"]["zaehler_status"]
          updated_at: string
          zaehlernummer: string
        }
        Insert: {
          ausgebaut_am?: string | null
          created_at?: string
          eichdatum: string
          eichfaellig_am: string
          einbau_foto_dateiname?: string | null
          einbau_foto_drive_file_id?: string | null
          einbau_foto_pfad?: string | null
          eingebaut_am?: string
          id?: number
          medium: Database["public"]["Enums"]["zaehler_medium"]
          parzelle_id: number
          status?: Database["public"]["Enums"]["zaehler_status"]
          updated_at?: string
          zaehlernummer: string
        }
        Update: {
          ausgebaut_am?: string | null
          created_at?: string
          eichdatum?: string
          eichfaellig_am?: string
          einbau_foto_dateiname?: string | null
          einbau_foto_drive_file_id?: string | null
          einbau_foto_pfad?: string | null
          eingebaut_am?: string
          id?: number
          medium?: Database["public"]["Enums"]["zaehler_medium"]
          parzelle_id?: number
          status?: Database["public"]["Enums"]["zaehler_status"]
          updated_at?: string
          zaehlernummer?: string
        }
        Relationships: [
          {
            foreignKeyName: "zaehler_parzelle_id_fkey"
            columns: ["parzelle_id"]
            isOneToOne: false
            referencedRelation: "parzelle"
            referencedColumns: ["id"]
          },
        ]
      }
      zaehler_ablesung: {
        Row: {
          ablesedatum: string
          art: Database["public"]["Enums"]["ablesung_art"]
          created_at: string
          foto_dateiname: string | null
          foto_drive_file_id: string | null
          foto_pfad: string | null
          freigegeben: boolean
          id: number
          stand: number
          zaehler_id: number
        }
        Insert: {
          ablesedatum?: string
          art?: Database["public"]["Enums"]["ablesung_art"]
          created_at?: string
          foto_dateiname?: string | null
          foto_drive_file_id?: string | null
          foto_pfad?: string | null
          freigegeben?: boolean
          id?: number
          stand: number
          zaehler_id: number
        }
        Update: {
          ablesedatum?: string
          art?: Database["public"]["Enums"]["ablesung_art"]
          created_at?: string
          foto_dateiname?: string | null
          foto_drive_file_id?: string | null
          foto_pfad?: string | null
          freigegeben?: boolean
          id?: number
          stand?: number
          zaehler_id?: number
        }
        Relationships: [
          {
            foreignKeyName: "zaehler_ablesung_zaehler_id_fkey"
            columns: ["zaehler_id"]
            isOneToOne: false
            referencedRelation: "v_aktive_zaehler"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "zaehler_ablesung_zaehler_id_fkey"
            columns: ["zaehler_id"]
            isOneToOne: false
            referencedRelation: "v_zaehler_eichstatus"
            referencedColumns: ["id"]
          },
          {
            foreignKeyName: "zaehler_ablesung_zaehler_id_fkey"
            columns: ["zaehler_id"]
            isOneToOne: false
            referencedRelation: "zaehler"
            referencedColumns: ["id"]
          },
        ]
      }
    }
    Views: {
      v_aktive_zaehler: {
        Row: {
          anlage: string | null
          eichdatum: string | null
          eichfaellig_am: string | null
          eingebaut_am: string | null
          garten_nr: string | null
          id: number | null
          medium: Database["public"]["Enums"]["zaehler_medium"] | null
          parzelle_id: number | null
          rfid_tag_uid: string | null
          zaehlernummer: string | null
        }
        Relationships: [
          {
            foreignKeyName: "zaehler_parzelle_id_fkey"
            columns: ["parzelle_id"]
            isOneToOne: false
            referencedRelation: "parzelle"
            referencedColumns: ["id"]
          },
        ]
      }
      v_pflichtstunden_uebersicht: {
        Row: {
          altersbefreit: boolean | null
          eintritt_im_saisonjahr: boolean | null
          eintritt_zweites_halbjahr: boolean | null
          euro_pro_fehlstunde: number | null
          fehlbetrag: number | null
          geleistete_stunden: number | null
          hat_wartungsvertrag: boolean | null
          hauptmitglied_id: number | null
          ist_befreit: boolean | null
          name: string | null
          offene_stunden: number | null
          pflichtstunden_soll: number | null
          regelgrund: string | null
          saison_id: number | null
          saison_jahr: number | null
          vorname: string | null
        }
        Relationships: []
      }
      v_rfid_scan_context: {
        Row: {
          aktiver_zaehler_id: number | null
          anlage: string | null
          ausgebaut_am: string | null
          eichdatum: string | null
          eichfaellig_am: string | null
          eingebaut_am: string | null
          garten_nr: string | null
          medium: Database["public"]["Enums"]["zaehler_medium"] | null
          parzelle_id: number | null
          rfid_tag_uid: string | null
          status: Database["public"]["Enums"]["zaehler_status"] | null
          zaehlernummer: string | null
        }
        Relationships: []
      }
      v_startseite_arbeitseinsatz: {
        Row: {
          angemeldet_count: number | null
          anmeldung_bis: string | null
          beschreibung: string | null
          datum: string | null
          end_uhrzeit: string | null
          freie_plaetze: number | null
          id: number | null
          max_teilnehmer: number | null
          sichtbar_ab: string | null
          sichtbar_bis: string | null
          start_uhrzeit: string | null
          stunden_wert: number | null
          titel: string | null
          treffpunkt: string | null
        }
        Relationships: []
      }
      v_startseite_bekanntmachungen: {
        Row: {
          created_at: string | null
          id: number | null
          inhalt_html: string | null
          sichtbar_ab: string | null
          sichtbar_bis: string | null
          sort_order: number | null
          titel: string | null
        }
        Insert: {
          created_at?: string | null
          id?: number | null
          inhalt_html?: string | null
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          sort_order?: number | null
          titel?: string | null
        }
        Update: {
          created_at?: string | null
          id?: number | null
          inhalt_html?: string | null
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          sort_order?: number | null
          titel?: string | null
        }
        Relationships: []
      }
      v_startseite_termine: {
        Row: {
          beschreibung: string | null
          datum: string | null
          end_uhrzeit: string | null
          id: number | null
          sichtbar_ab: string | null
          sichtbar_bis: string | null
          start_uhrzeit: string | null
          titel: string | null
        }
        Insert: {
          beschreibung?: string | null
          datum?: string | null
          end_uhrzeit?: string | null
          id?: number | null
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          start_uhrzeit?: string | null
          titel?: string | null
        }
        Update: {
          beschreibung?: string | null
          datum?: string | null
          end_uhrzeit?: string | null
          id?: number | null
          sichtbar_ab?: string | null
          sichtbar_bis?: string | null
          start_uhrzeit?: string | null
          titel?: string | null
        }
        Relationships: []
      }
      v_zaehler_eichstatus: {
        Row: {
          anlage: string | null
          eichdatum: string | null
          eichfaellig_am: string | null
          eichstatus: string | null
          eingebaut_am: string | null
          garten_nr: string | null
          id: number | null
          medium: Database["public"]["Enums"]["zaehler_medium"] | null
          parzelle_id: number | null
          status: Database["public"]["Enums"]["zaehler_status"] | null
          tage_bis_faellig: number | null
          zaehlernummer: string | null
        }
        Relationships: [
          {
            foreignKeyName: "zaehler_parzelle_id_fkey"
            columns: ["parzelle_id"]
            isOneToOne: false
            referencedRelation: "parzelle"
            referencedColumns: ["id"]
          },
        ]
      }
    }
    Functions: {
      assign_parzelle_rfid: {
        Args: {
          p_medium: Database["public"]["Enums"]["zaehler_medium"]
          p_parzelle_id: number
          p_rfid_tag_uid: string
        }
        Returns: {
          aktiv: boolean
          Anlage: string
          flaeche_qm: number | null
          garten_nr: string
          hat_strom: boolean
          hat_wasser: boolean
          id: number
          is_demo: boolean
          rfid_strom: string | null
          rfid_wasser: string | null
        }
        SetofOptions: {
          from: "*"
          to: "parzelle"
          isOneToOne: true
          isSetofReturn: false
        }
      }
      before_user_created_allowlist: { Args: { event: Json }; Returns: Json }
      calc_eichfaellig_am: {
        Args: {
          p_eichdatum: string
          p_medium: Database["public"]["Enums"]["zaehler_medium"]
        }
        Returns: string
      }
      can_access_demo_scope: { Args: never; Returns: boolean }
      can_access_live_internal_data: { Args: never; Returns: boolean }
      create_meter_installation: {
        Args: {
          p_eichdatum: string
          p_eingebaut_am?: string
          p_medium: Database["public"]["Enums"]["zaehler_medium"]
          p_parzelle_id: number
          p_zaehlernummer: string
        }
        Returns: {
          ausgebaut_am: string | null
          created_at: string
          eichdatum: string
          eichfaellig_am: string
          einbau_foto_dateiname: string | null
          einbau_foto_drive_file_id: string | null
          einbau_foto_pfad: string | null
          eingebaut_am: string
          id: number
          medium: Database["public"]["Enums"]["zaehler_medium"]
          parzelle_id: number
          status: Database["public"]["Enums"]["zaehler_status"]
          updated_at: string
          zaehlernummer: string
        }
        SetofOptions: {
          from: "*"
          to: "zaehler"
          isOneToOne: true
          isSetofReturn: false
        }
      }
      create_meter_reading: {
        Args: {
          p_ablesedatum?: string
          p_foto_pfad?: string
          p_stand: number
          p_zaehler_id: number
        }
        Returns: {
          ablesedatum: string
          art: Database["public"]["Enums"]["ablesung_art"]
          created_at: string
          foto_dateiname: string | null
          foto_drive_file_id: string | null
          foto_pfad: string | null
          freigegeben: boolean
          id: number
          stand: number
          zaehler_id: number
        }
        SetofOptions: {
          from: "*"
          to: "zaehler_ablesung"
          isOneToOne: true
          isSetofReturn: false
        }
      }
      current_app_role: { Args: never; Returns: string }
      current_mitglied_id: { Args: never; Returns: number }
      current_user_email: { Args: never; Returns: string }
      find_scan_context: {
        Args: { p_rfid_tag_uid: string }
        Returns: {
          aktiver_zaehler_id: number
          anlage: string
          ausgebaut_am: string
          eichdatum: string
          eichfaellig_am: string
          eingebaut_am: string
          garten_nr: string
          medium: Database["public"]["Enums"]["zaehler_medium"]
          parzelle_id: number
          rfid_tag_uid: string
          status: Database["public"]["Enums"]["zaehler_status"]
          zaehlernummer: string
        }[]
      }
      fn_berechne_pflichtstunden_status: {
        Args: { p_mitglied_id: number; p_saison_id: number }
        Returns: {
          altersbefreit: boolean
          eintritt_im_saisonjahr: boolean
          eintritt_zweites_halbjahr: boolean
          euro_pro_fehlstunde: number
          fehlbetrag: number
          geleistete_stunden: number
          hat_wartungsvertrag: boolean
          hauptmitglied_id: number
          ist_befreit: boolean
          offene_stunden: number
          pflichtstunden_soll: number
          regelgrund: string
          saison_id: number
          saison_jahr: number
        }[]
      }
      get_active_meter: {
        Args: {
          p_medium: Database["public"]["Enums"]["zaehler_medium"]
          p_parzelle_id: number
        }
        Returns: {
          ausgebaut_am: string | null
          created_at: string
          eichdatum: string
          eichfaellig_am: string
          einbau_foto_dateiname: string | null
          einbau_foto_drive_file_id: string | null
          einbau_foto_pfad: string | null
          eingebaut_am: string
          id: number
          medium: Database["public"]["Enums"]["zaehler_medium"]
          parzelle_id: number
          status: Database["public"]["Enums"]["zaehler_status"]
          updated_at: string
          zaehlernummer: string
        }
        SetofOptions: {
          from: "*"
          to: "zaehler"
          isOneToOne: true
          isSetofReturn: false
        }
      }
      get_hauptmitglied_id: { Args: { p_mitglied_id: number }; Returns: number }
      get_user_role: { Args: never; Returns: string }
      is_admin: { Args: never; Returns: boolean }
      is_admin_or_vorstand: { Args: never; Returns: boolean }
      is_demo_user: { Args: never; Returns: boolean }
      is_playstore_reviewer: { Args: never; Returns: boolean }
      remove_meter: {
        Args: {
          p_ablesedatum?: string
          p_ausgebaut_am: string
          p_endstand: number
          p_foto_pfad?: string
          p_zaehler_id: number
        }
        Returns: {
          ausgebaut_am: string | null
          created_at: string
          eichdatum: string
          eichfaellig_am: string
          einbau_foto_dateiname: string | null
          einbau_foto_drive_file_id: string | null
          einbau_foto_pfad: string | null
          eingebaut_am: string
          id: number
          medium: Database["public"]["Enums"]["zaehler_medium"]
          parzelle_id: number
          status: Database["public"]["Enums"]["zaehler_status"]
          updated_at: string
          zaehlernummer: string
        }
        SetofOptions: {
          from: "*"
          to: "zaehler"
          isOneToOne: true
          isSetofReturn: false
        }
      }
      sign_off_from_arbeitseinsatz: {
        Args: { p_arbeitseinsatz_id: number; p_mitglied_id: number }
        Returns: {
          angemeldet_am: string
          arbeitseinsatz_id: number
          bemerkung: string | null
          id: number
          mitglied_id: number
          status: Database["public"]["Enums"]["arbeitseinsatz_anmeldung_status"]
          updated_at: string
        }
        SetofOptions: {
          from: "*"
          to: "arbeitseinsatz_anmeldung"
          isOneToOne: true
          isSetofReturn: false
        }
      }
      sign_up_for_arbeitseinsatz: {
        Args: { p_arbeitseinsatz_id: number; p_mitglied_id: number }
        Returns: {
          angemeldet_am: string
          arbeitseinsatz_id: number
          bemerkung: string | null
          id: number
          mitglied_id: number
          status: Database["public"]["Enums"]["arbeitseinsatz_anmeldung_status"]
          updated_at: string
        }
        SetofOptions: {
          from: "*"
          to: "arbeitseinsatz_anmeldung"
          isOneToOne: true
          isSetofReturn: false
        }
      }
      try_lock_mitglied: {
        Args: { p_id: number; p_timeout_minutes?: number; p_user_id: string }
        Returns: boolean
      }
      who_am_i: {
        Args: never
        Returns: {
          role: string
          vorname: string
        }[]
      }
    }
    Enums: {
      ablesung_art: "normal" | "ausbau"
      arbeitseinsatz_anmeldung_status:
        | "angemeldet"
        | "abgesagt"
        | "teilgenommen"
        | "nicht_erschienen"
      ablesung_pruefstatus: "eingereicht" | "freigegeben" | "abgelehnt"
      zaehler_medium: "wasser" | "strom"
      zaehler_status: "aktiv" | "ausgebaut"
    }
    CompositeTypes: {
      [_ in never]: never
    }
  }
}

type DatabaseWithoutInternals = Omit<Database, "__InternalSupabase">

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
    : never = never,
> = DefaultSchemaTableNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
  ? (DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Tables"] &
      DatabaseWithoutInternals[DefaultSchemaTableNameOrOptions["schema"]]["Views"])[TableName] extends {
      Row: infer R
    }
    ? R
    : never
  : DefaultSchemaTableNameOrOptions extends keyof (DefaultSchema["Tables"] &
        DefaultSchema["Views"])
    ? (DefaultSchema["Tables"] &
        DefaultSchema["Views"])[DefaultSchemaTableNameOrOptions] extends {
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
    : never = never,
> = DefaultSchemaTableNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
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
    : never = never,
> = DefaultSchemaTableNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
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
    : never = never,
> = DefaultSchemaEnumNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
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
    : never = never,
> = PublicCompositeTypeNameOrOptions extends {
  schema: keyof DatabaseWithoutInternals
}
  ? DatabaseWithoutInternals[PublicCompositeTypeNameOrOptions["schema"]]["CompositeTypes"][CompositeTypeName]
  : PublicCompositeTypeNameOrOptions extends keyof DefaultSchema["CompositeTypes"]
    ? DefaultSchema["CompositeTypes"][PublicCompositeTypeNameOrOptions]
    : never

export const Constants = {
  public: {
    Enums: {
      ablesung_art: ["normal", "ausbau"],
      ablesung_pruefstatus: ["eingereicht", "freigegeben", "abgelehnt"],
      arbeitseinsatz_anmeldung_status: [
        "angemeldet",
        "abgesagt",
        "teilgenommen",
        "nicht_erschienen",
      ],
      zaehler_medium: ["wasser", "strom"],
      zaehler_status: ["aktiv", "ausgebaut"],
    },
  },
} as const
