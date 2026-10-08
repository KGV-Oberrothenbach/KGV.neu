import { openMeterPhoto, type BrowserSession, type MeterPhotoKind, uploadMeterPhoto, writeSupabase } from "../../lib/supabase-auth";

export type ReadingPhotoKind = MeterPhotoKind;
export type ReadingPhotoDetails = { datum: string; medium: string; anlage: string; garten: string; zaehlernummer: string; kind?: ReadingPhotoKind };
export type UploadedReadingPhoto = { fileId: string; fileName: string };

export const uploadReadingPhoto = (session: BrowserSession, file: File, details: ReadingPhotoDetails): Promise<UploadedReadingPhoto> => uploadMeterPhoto(session, file, details);
export const linkReadingPhoto = (session: BrowserSession, readingId: number, photo: UploadedReadingPhoto) =>
  writeSupabase(session, "zaehler_ablesung", "PATCH", { foto_drive_file_id: photo.fileId, foto_dateiname: photo.fileName }, { id: `eq.${readingId}` });
export const openReadingPhoto = openMeterPhoto;
