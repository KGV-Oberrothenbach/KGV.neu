export type Announcement = {
  id: number;
  titel: string | null;
  inhalt_html: string | null;
  sichtbar_ab: string | null;
  sichtbar_bis: string | null;
  sort_order: number | null;
  aktiv: boolean;
  is_demo?: boolean;
};

export type AnnouncementDraft = Partial<Announcement>;
