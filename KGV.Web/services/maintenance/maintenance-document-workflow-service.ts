import type { ClubContext } from "../../models/auth/club";
import type { BrowserSession } from "../../lib/supabase-auth";
import { ensureMaintenanceDocumentWorkflow } from "../../repositories/maintenance/maintenance-document-workflow-repository";

export const prepareMaintenanceDocumentWorkflow = (club: ClubContext, session: BrowserSession, assignmentId: number) =>
  ensureMaintenanceDocumentWorkflow(club, session, assignmentId);
