"use client";
import { readSupabaseRest } from "../lib/supabase/rest-client";
import { callSupabaseRpc } from "../lib/supabase/rpc-client";
import { useAppVersion } from "../lib/app-version";

import Image from "next/image";
import { useCallback, useEffect, useMemo, useState } from "react";
import {
  AppUserContext, BrowserSession } from "../lib/supabase-auth";
import { inviteAppUser, sendPasswordReset } from "../services/users/app-user-service";
import { type ClubContext } from "../models/auth/club";
import { AuthProvider, useAuth, useSelectedClub } from "../features/auth/AuthProvider";
import { ChangeClubAction } from "../features/auth/ChangeClubAction";
import { ClubSelection } from "../features/auth/ClubSelection";
import { LoginForm } from "../features/auth/LoginForm";
import { MemberDocumentWorkspace } from "../features/documents/MemberDocumentWorkspace";
import type { DocumentAccessContext } from "../services/documents/document-access-service";
import { OtpFlow } from "../features/auth/OtpFlow";
import { RfidBrowserScanner } from "../features/meters/RfidScanner";
import { subscribeConnectivityChanges } from "../lib/browser/connectivity";
import { getWifiOnlyPreference, setWifiOnlyPreference } from "../lib/browser/storage";
import { useEditLock } from "../lib/use-edit-lock";
import { MemberGardensWorkspace, ParcelProtocolsWorkspace, ParcelWorkspace } from "./parcel-workspaces";
import Navigation from "../features/navigation/Navigation";
import SeasonPicker from "../features/navigation/SeasonPicker";
import MobileNavigation from "../features/navigation/MobileNavigation";
import WorkspaceHeader from "../features/navigation/WorkspaceHeader";
import { buildNavigation } from "../services/workspace/navigation-service";
import { useWorkspaceContext, WorkspaceContextProvider } from "../contexts/WorkspaceContext";
import { getSeasons } from "../services/seasons/season-service";
import type { Season } from "../models/seasons/season";
import SeasonManagement from "../features/seasons/SeasonManagement";
import { selectInitialSeasonFromList } from "../services/workspace/workspace-service";
import HomeDashboard from "../features/home/HomeDashboard";
import ImprintPage from "../features/imprint/ImprintPage";
import { MemberSearch } from "../features/members/MemberSearch";
import { MemberStammdaten } from "../features/members/MemberStammdaten";
import { type Member } from "../models/members/member";
import { loadMemberWorkspaceInfo } from "../services/members/member-service";
import { loadMeterOverview, loadMeterReferenceData } from "../services/meters/meter-service";
import { createMeterInstallationForRfid, installationContextFromResolution, type MeterInstallationContext } from "../services/meters/meter-installation-service";
import { completeMeterRemoval } from "../services/meters/meter-removal-service";
import { type Meter } from "../repositories/meters/meter-repository";
import { assignRfid, checkRfidAssignment, listAvailableRfidMediumOptions, listRfidSetupParcels, resolveRfidScanContext, type RfidAssignmentCheck, type RfidMediumOption, type RfidScanResolution } from "../services/rfid/rfid-service";
import { getMeterInstallationPhotoRequired, loadReadingCaptureContext, saveMeterInstallationReading, saveMeterRemovalReading, saveMeterReading, type ReadingCaptureContext, type ReadingPermissions } from "../services/readings/reading-service";
import { countPendingReadingPhotos, handleReadingPhoto, openStoredReadingPhoto, retryAllPendingReadingPhotos } from "../services/readings/reading-photo-service";
import { loadReadingReviewData, reviewMeterReading, type ReadingReviewData, type ReadingReviewItem } from "../services/readings/reading-review-service";
import { MemberWorkHours } from "../features/work-hours/MemberWorkHours";
import { WorkHoursReview } from "../features/work-hours/WorkHoursReview";
import { WorkAssignmentsManagement } from "../features/work-assignments/WorkAssignmentsManagement";
import { AppointmentManagement } from "../features/appointments/AppointmentManagement";
import { AnnouncementManagement } from "../features/announcements/AnnouncementManagement";
import { type MaintenanceContract, type MaintenanceContractDraft } from "../models/maintenance/maintenance-contract";
import { type MaintenanceAssignment } from "../models/maintenance/maintenance-assignment";
import { loadMaintenanceContracts, normalizeAndValidateMaintenanceContract, saveMaintenanceContract } from "../services/maintenance/maintenance-contract-service";
import { endMaintenanceAssignmentForContract, loadMaintenanceAssignments } from "../services/maintenance/maintenance-assignment-service";
import { buildMaintenanceContractOverview } from "../services/maintenance/maintenance-contract-service";
import { classifyMaintenanceAssignments } from "../services/maintenance/maintenance-assignment-service";
import { MaintenanceContractOverview } from "../features/maintenance/MaintenanceContractOverview";
import { MaintenanceContractEditor } from "../features/maintenance/MaintenanceContractEditor";
import { AssignMembersToContract } from "../features/maintenance/assignments/AssignMembersToContract";
import { MemberMaintenanceContracts } from "../features/maintenance/MemberMaintenanceContracts";
import { ExpiringMaintenanceContracts } from "../features/maintenance/ExpiringMaintenanceContracts";
import { maintenanceAccess } from "../services/maintenance/maintenance-access-service";
import { getEffectivePermissions, getRolePermissions, hasPermission, Permission, PermissionAreas, preserveUncontrolledPermissions } from "../security/permissions";
import { canArchiveDocuments, canExportData, canManageClubConfiguration, canManageClubSeasonConfiguration, canManageDocuments, canPerformYearEndClosing, canReadDocuments, canRegisterHistoricalContracts, createWebCapabilityContext } from "../security/capabilities";
import { HistoricalContractRegistrationWorkflow } from "../features/documents/HistoricalContractRegistrationWorkflow";
import { ClubConfiguration } from "../features/configuration/ClubConfiguration";
import { ExportCenter } from "../features/export/ExportCenter";
import { CommunicationCenter } from "../features/communication/CommunicationCenter";
import { ClubDocumentsManagement } from "../features/club-documents/ClubDocumentsManagement";
import { ClubFunctionManagement } from "../features/club-functions/ClubFunctionManagement";
import { DocumentTypeManagement } from "../features/documents-management/DocumentTypeManagement";
import { listCommunicationItems } from "../repositories/communication/communication-repository";
import { isOpenCommunicationWorkItem } from "../services/communication/communication-service";

export default function Home() {
  return <AuthProvider><HomeContent /></AuthProvider>;
}
function HomeContent() {
  const appVersion = useAppVersion();
  const { status, session, appUserContext, club, message, selectClub, login, loginWithPasskey, logout, changeClub } = useAuth();

  if (status === "club-selection") return <ClubSelection onSelect={selectClub} />;
  if (status === "signed-in" && session && appUserContext && club) {
    return <Workspace session={session} email={session.user.email ?? "KGV-Konto"} club={club} context={appUserContext} onLogout={() => logout()} onChangeClub={changeClub} />;
  }

  const configError = status === "configuration-error";
  return (
    <main className="login-page">
      <section className="login-card" aria-labelledby="login-title">
        <div className="brand-mark" aria-hidden="true">K</div>
        <p className="eyebrow">Kleingartenverein</p>
        <h1 id="login-title">KGV Verwaltung</h1>
        <p className="intro">Melde dich bei <strong>{club?.vereinsname ?? "deinem Verein"}</strong> mit deinem bestehenden KGV-Konto an.</p>
        {configError ? (
          <p className="notice" role="alert">Die Verbindung zu Supabase ist noch nicht eingerichtet.</p>
        ) : (
          <>
            <LoginForm onSignIn={login} onPasskeySignIn={loginWithPasskey} disabled={status === "checking"} message={message} />
            <OtpFlow club={club!} disabled={status === "checking"} />
          </>
        )}
        <p className="fine-print">Der Zugriff wird nach der Anmeldung anhand deiner hinterlegten Vereinsrolle geprüft.</p>
        <p className="app-version">Version {appVersion}</p>
        <ChangeClubAction className="change-club" label="Anderen Verein auswählen" onConfirm={changeClub} />
      </section>
    </main>
  );
}
// Navigation types moved to features/navigation/Navigation.tsx
type Parcel = { id: number; garten_nr: string; Anlage: string; flaeche_qm: number | null; hat_strom: boolean; hat_wasser: boolean; aktiv: boolean };
type ParcelAssignment = { id: number; parzelle_id: number; mitglied_id: number; von_datum: string | null; bis_datum: string | null };
type Reading = { id: number; zaehler_id: number; stand: number; ablesedatum: string; art: string; freigegeben: boolean; pruefstatus: string; pruefkommentar: string | null; geprueft_von: number | null; geprueft_am: string | null; foto_pfad?: string | null; foto_drive_file_id?: string | null; foto_dateiname?: string | null };
type AppUserAdmin = { user_id: string; mitglied_id: number | null; role: "admin" | "vorstand" | "user"; permission_grants: number; permission_revocations: number; is_protected: boolean; updated_at: string | null };

function Workspace(props: { session: BrowserSession; email: string; club: ClubContext; context: AppUserContext; onLogout: () => void; onChangeClub: () => Promise<void> }) {
  return <WorkspaceContextProvider><WorkspaceContent {...props} /></WorkspaceContextProvider>;
}
function WorkspaceContent({ session, email, club, context, onLogout, onChangeClub }: { session: BrowserSession; email: string; club: ClubContext; context: AppUserContext; onLogout: () => void; onChangeClub: () => Promise<void> }) {
  const [seasons, setSeasons] = useState<Season[]>([]);
  const [activeId, setActiveId] = useState("start");
  const [pendingPhotoCount, setPendingPhotoCount] = useState(0);
  const [openProcessCount, setOpenProcessCount] = useState(0);
  const [openProcessCounts, setOpenProcessCounts] = useState({ signature: 0, mail: 0, print: 0 });
  const [seasonError, setSeasonError] = useState("");
  const { workspaceContext, selectedMember, creatingMember, setSelectedMember, setCreatingMember, updateWorkspaceContext, selectMember, selectParcel, selectSeason: selectWorkspaceSeason } = useWorkspaceContext();
  const permissions = useMemo(() => getEffectivePermissions(context.role, context.permissionGrants, context.permissionRevocations), [context]);
  const capabilityContext = useMemo(() => createWebCapabilityContext(context.role, context.permissionGrants, context.permissionRevocations), [context]);
  const has = (permission: number) => hasPermission(permissions, permission);
  const documentAccessContext: DocumentAccessContext = {
    memberId: context.mitgliedId,
    canReadDocuments: canReadDocuments(capabilityContext),
    canManageDocuments: canManageDocuments(capabilityContext),
    canSeeOwnDataOnly: has(Permission.seeOwnData),
    canArchiveDocuments: canArchiveDocuments(capabilityContext),
  };
  const canReadStammdaten = has(Permission.showStammdaten) || has(Permission.readStammdaten) || has(Permission.writeStammdaten);
  const ownContext = context.mitgliedId !== null && has(Permission.seeOwnData);
  const selectedMemberId = workspaceContext.mitgliedId ?? (ownContext ? context.mitgliedId : null);
  const selectedMemberName = selectedMember ? [selectedMember.vorname, selectedMember.name].filter(Boolean).join(" ") : "";
  const selectedMemberNumber = selectedMember?.mitgliedsnummer;
  const selectedMemberLabel = selectedMember
    ? selectedMemberName ? `${selectedMemberName} · Mgl.-Nr. ${selectedMemberNumber ?? "–"}` : `Mgl.-Nr. ${selectedMemberNumber ?? "–"}`
    : selectedMemberId ? "Mgl.-Nr. –" : "nicht gewählt";
  const selectedParcelId = workspaceContext.parzelleId;
  const selectedSeason = seasons.find((item) => item.id === workspaceContext.saisonId) ?? null;
  const season = selectedSeason?.jahr ?? workspaceContext.saisonJahr ?? new Date().getFullYear();
  const canAccessCommunication = context.role === "admin" || context.role === "vorstand";
  const refreshOpenProcessCount = useCallback(async () => {
    if (!canAccessCommunication) { setOpenProcessCount(0); setOpenProcessCounts({ signature: 0, mail: 0, print: 0 }); return; }
    try {
      const activeStatuses = "in.(entwurf,vorbereitet,zur_signatur,letzte_pruefung,mail_bereit,wird_versendet,druck_bereit,fehler)";
      const items = await listCommunicationItems(club, session, { status: activeStatuses });
      const counts = { signature: 0, mail: 0, print: 0 };
      for (const item of items.filter(isOpenCommunicationWorkItem)) {
        if (item.vorgangstyp === "vertrag_signatur") counts.signature += 1;
        if (item.vorgangstyp === "mail") counts.mail += 1;
        if (item.vorgangstyp === "druck") counts.print += 1;
      }
      setOpenProcessCounts(counts);
      setOpenProcessCount(counts.signature + counts.mail + counts.print);
    } catch { setOpenProcessCount(0); setOpenProcessCounts({ signature: 0, mail: 0, print: 0 }); }
  }, [canAccessCommunication, club, session]);

  useEffect(() => {
    const openCommunication = () => setActiveId("signaturen");
    window.addEventListener("kgv:open-communication", openCommunication);
    return () => window.removeEventListener("kgv:open-communication", openCommunication);
  }, []);

  useEffect(() => {
    let active = true;
    getSeasons(club, session)
      .then((items) => {
        if (!active) return;
        setSeasons(items);
        updateWorkspaceContext((current) => {
          const season = selectInitialSeasonFromList(items, current.saisonId);
          return season ? { ...current, saisonId: season.id, saisonJahr: season.jahr } : current;
        });
      })
      .catch(() => { if (active) setSeasonError("Saisons konnten nicht geladen werden."); });
    return () => { active = false; };
  }, [session, updateWorkspaceContext, club]);

  useEffect(() => {
    let active = true;
    countPendingReadingPhotos(session, club.vereinId)
      .then((count) => { if (active) setPendingPhotoCount(count); })
      .catch(() => { if (active) setPendingPhotoCount(0); });
    return () => { active = false; };
  }, [session, club.vereinId]);

  useEffect(() => { void refreshOpenProcessCount(); }, [refreshOpenProcessCount]);

  useEffect(() => {
    let active = true;
    if (selectedMemberId === null) {
      setSelectedMember(null);
      return () => { active = false; };
    }
    setSelectedMember(null);
    loadMemberWorkspaceInfo(club, session, selectedMemberId).then((member) => {
      if (active) setSelectedMember(member);
    }).catch(() => {
      if (active) setSelectedMember(null);
    });
    return () => { active = false; };
  }, [session, selectedMemberId, setSelectedMember, club]);

  function selectSeason(saisonId: number) {
    const nextSeason = seasons.find((item) => item.id === saisonId);
    if (nextSeason) selectWorkspaceSeason(nextSeason);
  }

  function openOwnWorkHours() {
    if (context.mitgliedId === null) return;
    updateWorkspaceContext((current) => ({ ...current, mitgliedId: context.mitgliedId, parzelleId: null }));
    setActiveId("mitglied-arbeitsstunden");
  }
  const hasMemberContext = creatingMember || (selectedMemberId !== null && (ownContext || has(Permission.viewMembers) || has(Permission.searchMembers)));
  const { navigationGroups } = buildNavigation({
    season,
    creatingMember,
    hasMemberContext,
    canReadStammdaten,
    canReadMemberDocuments: has(Permission.readDocuments) || has(Permission.manageDocuments) || ownContext,
    canEditMemberProtocols: has(Permission.editAllMembers),
    canReadMemberGardens: has(Permission.showParzellen) || has(Permission.readParzellen),
    canManageMemberRoles: has(Permission.readRoles) || has(Permission.manageRoles),
    canReadMemberWorkHours: has(Permission.readWorkHours) || has(Permission.manageWorkHours) || ownContext,
    canAccessMeters: has(Permission.readMeters) || has(Permission.manageMeterChanges) || has(Permission.approveMeterReadings) || ownContext,
    canReadMeterPhotos: has(Permission.readMeters) || ownContext,
    canAccessMeterChanges: has(Permission.manageMeterChanges),
    canApproveMeterReadings: has(Permission.approveMeterReadings),
    canAccessParcels: has(Permission.searchMembers) && has(Permission.readParzellen),
    canManageMaintenance: has(Permission.editAllMembers),
    canApproveWorkHours: has(Permission.manageWorkHours),
    canExport: canExportData(capabilityContext),
    canManageAdministration: context.role === "admin",
    canManageProcesses: canAccessCommunication,
    canManageSeasonConfiguration: canManageClubSeasonConfiguration(capabilityContext),
    canPerformYearEndClosing: canPerformYearEndClosing(capabilityContext),
    canManageClubConfiguration: canManageClubConfiguration(capabilityContext),
    canManageClubDocuments: context.role === "admin" || context.role === "vorstand",
    canReadDocumentTypes: context.role === "admin" || context.role === "vorstand",
    canManageClubFunctions: context.role === "admin" || context.role === "vorstand",
    canRegisterHistoricalContracts: canRegisterHistoricalContracts(capabilityContext),
    canSearchMembers: has(Permission.searchMembers),
    canAccessCommunication,
    openProcessCount,
  });
  const allNavigationItems = navigationGroups.flatMap((group) => [...(group.target ? [group.target] : []), ...group.items]);
  const allowedNavigationIds = new Set(allNavigationItems.map((item) => item.id));
  const fallbackActiveId = allowedNavigationIds.has("start") ? "start" : allNavigationItems[0]?.id ?? "";
  const effectiveActiveId = allowedNavigationIds.has(activeId) ? activeId : fallbackActiveId;
  const active = allNavigationItems.find((item) => item.id === effectiveActiveId) ?? allNavigationItems[0];
  return (
    <main className="workspace">
      <WorkspaceHeader clubName={club.kurzname || club.vereinsname} email={email} role={context.role} onLogout={onLogout} onChangeClub={onChangeClub} pendingPhotoCount={pendingPhotoCount} />
      <section className="workspace-body">
        <aside className="sidebar" aria-label="Hauptnavigation">
          <SeasonPicker seasons={seasons} selectedSeasonId={workspaceContext.saisonId} seasonError={seasonError} onChange={selectSeason} />
          <Navigation groups={navigationGroups} activeId={effectiveActiveId} onNavigate={setActiveId} memberLabel={creatingMember ? "Neues Mitglied" : selectedMemberId ? selectedMemberLabel : null} />
        </aside>
        <article className="content-area">
          <MobileNavigation groups={navigationGroups} activeId={effectiveActiveId} onNavigate={setActiveId} selectedMemberLabel={selectedMemberId ? selectedMemberLabel : null} />
          <p className="eyebrow">Saison {season}</p><h1>{active.label}</h1><p className="content-intro">{active.detail}</p>
          {effectiveActiveId === "vertraege-nacherfassen" && <HistoricalContractRegistrationWorkflow session={session} />}
          {!new Set(["start", "impressum", "mitglieder", "parzellen", "ablesen-erfassen", "jae-erfassen", "ablesungen-freigeben", "rfid-einrichten", "zaehlerwechsel", "arbeitsstunden-pruefen", "arbeitseinsaetze", "wartung", "termine", "bekanntmachungen", "export", "signaturen", "dokumente", "vorstand", "mailausgang", "versendet", "druckausgabe", "verlauf", "saisons", "verein", "vereinsunterlagen", "dokumenttypen", "vereinsfunktionen", "vertraege-nacherfassen", "mitglied-arbeitsstunden", "mitglied-wartung", "mitglied-dokumente", "mitglied-admin", "mitglied-gaerten", "mitglied-protokolle", "mitglied-stammdaten"]).has(effectiveActiveId) && <section className="coming-soon"><span aria-hidden="true">◌</span><div><strong>Bereich vorbereitet</strong><p>Die Navigation und Zugriffsrechte stehen. Die fachliche Oberfläche wird in den nächsten Umsetzungsschritten ergänzt.</p></div></section>}
          {effectiveActiveId === "start" && <HomeDashboard session={session} isManager={has(Permission.manageWorkAssignments) || has(Permission.manageAppointments) || has(Permission.manageAnnouncements)} canManageWorkAssignments={has(Permission.manageWorkAssignments)} canManageAppointments={has(Permission.manageAppointments)} canManageAnnouncements={has(Permission.manageAnnouncements)} memberId={context.mitgliedId} saisonId={workspaceContext.saisonId} season={season} onNavigate={setActiveId} onOpenWorkHours={openOwnWorkHours} />}
          {effectiveActiveId === "mitglieder" && <MemberSearch session={session} selectedMemberId={selectedMemberId} onSelect={(memberId) => { selectMember(memberId); setActiveId("mitglied-stammdaten"); }} canCreate={has(Permission.createMember)} onCreate={() => { setCreatingMember(true); setActiveId("mitglied-stammdaten"); }} />}
          {effectiveActiveId === "parzellen" && <ParcelWorkspace key={`${club.vereinId}:${session.user.id}`} session={session} selectedParcelId={selectedParcelId} onSelect={selectParcel} canEdit={has(Permission.writeParzellen)} documentAccessContext={documentAccessContext} onOpenMember={(memberId) => { selectMember(memberId); setActiveId("mitglied-gaerten"); }} />}
          {effectiveActiveId === "ablesen-erfassen" && <MeterReadingCaptureView session={session} clubId={club.vereinId} reviewerMemberId={context.mitgliedId} seasonYear={season} canReadMeters={has(Permission.readMeters)} canSubmitOwnMeterReadings={context.mitgliedId !== null && has(Permission.seeOwnData)} />}
          {effectiveActiveId === "jae-erfassen" && <AnnualMeterReadingCaptureView session={session} clubId={club.vereinId} reviewerMemberId={context.mitgliedId} seasonYear={season} canReadMeters={has(Permission.readMeters)} canSubmitOwnMeterReadings={context.mitgliedId !== null && has(Permission.seeOwnData)} />}
          {effectiveActiveId === "ablesungen-freigeben" && <MeterReadingReviewView session={session} clubId={club.vereinId} reviewerMemberId={context.mitgliedId} />}
          {effectiveActiveId === "rfid-einrichten" && has(Permission.manageMeterChanges) && <RfidAssignmentPanel session={session} selectedParcelId={selectedParcelId} />}
          {effectiveActiveId === "zaehlerwechsel" && has(Permission.manageMeterChanges) && <MeterChange session={session} clubId={club.vereinId} canManageMeterChanges={has(Permission.manageMeterChanges)} />}
          {effectiveActiveId === "arbeitsstunden-pruefen" && <WorkHoursReview session={session} saisonId={workspaceContext.saisonId} canManageWorkHours={has(Permission.manageWorkHours)} />}
          {effectiveActiveId === "arbeitseinsaetze" && <WorkAssignmentsManagement session={session} canEdit={has(Permission.manageWorkAssignments)} canManageWorkHours={has(Permission.manageWorkHours)} onBack={() => setActiveId("start")} />}
          {effectiveActiveId === "wartung" && <MaintenanceContracts session={session} canManage={has(Permission.editAllMembers)} />}
          {effectiveActiveId === "termine" && <AppointmentManagement session={session} canEdit={has(Permission.manageAppointments)} onBack={() => setActiveId("start")} />}
          {effectiveActiveId === "bekanntmachungen" && <AnnouncementManagement session={session} canEdit={has(Permission.manageAnnouncements)} onBack={() => setActiveId("start")} />}
          {effectiveActiveId === "impressum" && <ImprintPage session={session} />}
          {effectiveActiveId === "export" && <ExportCenter session={session} season={season} canExport={canExportData(capabilityContext)} />}
          {effectiveActiveId === "signaturen" && canAccessCommunication && <CommunicationCenter session={session} userId={session.user.id} area="signature" onChanged={() => void refreshOpenProcessCount()} />}
          {effectiveActiveId === "dokumente" && canAccessCommunication && <CommunicationCenter session={session} userId={session.user.id} area="documents" onChanged={() => void refreshOpenProcessCount()} />}
          {effectiveActiveId === "vorstand" && canAccessCommunication && <CommunicationCenter session={session} userId={session.user.id} area="board" onChanged={() => void refreshOpenProcessCount()} />}
          {effectiveActiveId === "mailausgang" && canAccessCommunication && <CommunicationCenter session={session} userId={session.user.id} area="mail" onChanged={() => void refreshOpenProcessCount()} />}
          {effectiveActiveId === "versendet" && canAccessCommunication && <CommunicationCenter session={session} userId={session.user.id} area="sent" onChanged={() => void refreshOpenProcessCount()} />}
          {effectiveActiveId === "druckausgabe" && canAccessCommunication && <CommunicationCenter session={session} userId={session.user.id} area="print" onChanged={() => void refreshOpenProcessCount()} />}
          {effectiveActiveId === "verlauf" && canAccessCommunication && <CommunicationCenter session={session} userId={session.user.id} area="history" onChanged={() => void refreshOpenProcessCount()} />}
          {effectiveActiveId === "saisons" && <SeasonManagement session={session} onSeasonSaved={(saved) => { setSeasons((current) => [...current.filter((item) => item.id !== saved.id), saved].sort((a, b) => b.jahr - a.jahr)); selectWorkspaceSeason(saved); }} />}
          {effectiveActiveId === "verein" && <ClubConfiguration key={club.vereinId} session={session} canManage={canManageClubConfiguration(capabilityContext)} />}
          {effectiveActiveId === "vereinsunterlagen" && (context.role === "admin" || context.role === "vorstand") && <ClubDocumentsManagement key={club.vereinId} session={session} />}
          {effectiveActiveId === "dokumenttypen" && (context.role === "admin" || context.role === "vorstand") && <DocumentTypeManagement key={club.vereinId} session={session} canManage={context.role === "admin"} />}
          {effectiveActiveId === "vereinsfunktionen" && (context.role === "admin" || context.role === "vorstand") && <ClubFunctionManagement key={club.vereinId} session={session} />}
          {effectiveActiveId === "mitglied-arbeitsstunden" && selectedMemberId && <MemberWorkHours key={`${club.vereinId}:${session.user.id}:${selectedMemberId}:${workspaceContext.saisonId ?? "none"}`} session={session} memberId={selectedMemberId} saisonId={workspaceContext.saisonId} canEditOwn={selectedMemberId === context.mitgliedId} canManageWorkHours={has(Permission.manageWorkHours)} />}
          {effectiveActiveId === "mitglied-wartung" && selectedMemberId && <MaintenanceContracts session={session} memberId={selectedMemberId} canManage={has(Permission.editAllMembers)} />}
          {effectiveActiveId === "mitglied-dokumente" && selectedMemberId && <MemberDocumentWorkspace key={`${club.vereinId}:${selectedMemberId}`} session={session} memberId={selectedMemberId} accessContext={documentAccessContext} />}
          {effectiveActiveId === "mitglied-admin" && selectedMemberId && <UserRightsAdministration key={`${club.vereinId}:${session.user.id}:${selectedMemberId}`} session={session} fixedMemberId={selectedMemberId} />}
          {effectiveActiveId === "mitglied-gaerten" && selectedMemberId && <MemberGardensWorkspace key={`${club.vereinId}:${selectedMemberId}`} session={session} memberId={selectedMemberId} selectedParcelId={selectedParcelId} onSelect={selectParcel} canAssign={has(Permission.createMember)} canCreateLeaseContract={has(Permission.manageDocuments)} />}
          {effectiveActiveId === "mitglied-protokolle" && selectedMemberId && <ParcelProtocolsWorkspace session={session} memberId={selectedMemberId} currentBoardMemberId={context.mitgliedId} />}
          {effectiveActiveId === "mitglied-stammdaten" && (selectedMemberId || creatingMember) && <MemberStammdaten session={session} memberId={creatingMember ? null : selectedMemberId} canEdit={creatingMember || has(Permission.editAllMembers) || (selectedMemberId === context.mitgliedId && has(Permission.seeOwnData))} canCreate={creatingMember} canManageSecondary={has(Permission.editAllMembers)} canManageDocuments={has(Permission.manageDocuments)} canManageClubDocuments={context.role === "admin" || context.role === "vorstand"} editPermissions={{ actorMemberId: context.mitgliedId, canEditOwnMember: has(Permission.seeOwnData), canEditAllMembers: has(Permission.editAllMembers) }} createPermissions={{ canCreateMember: has(Permission.createMember) }} onSaved={(member) => { setSelectedMember({ id: member.id, mitgliedsnummer: member.mitgliedsnummer, vorname: member.vorname, name: member.name }); selectMember(member.id); setActiveId("mitglied-stammdaten"); }} onCancel={() => { setCreatingMember(false); setActiveId("mitglieder"); }} />}
        </article>
      </section>
    </main>
  );
}

type MeterViewProps = { session: BrowserSession; clubId: string; reviewerMemberId: number | null; seasonYear: number; canReadMeters: boolean; canSubmitOwnMeterReadings: boolean };
function MeterReadingCaptureView(props: MeterViewProps) { return <MeterWorkspace {...props} mode="capture" />; }
function AnnualMeterReadingCaptureView(props: MeterViewProps) { return <MeterWorkspace {...props} mode="annual" />; }
function MeterReadingReviewView({ session, clubId, reviewerMemberId }: Pick<MeterViewProps, "session" | "clubId" | "reviewerMemberId">) { return <MeterWorkspace session={session} clubId={clubId} reviewerMemberId={reviewerMemberId} seasonYear={new Date().getFullYear()} canReadMeters={false} canSubmitOwnMeterReadings={false} mode="review" />; }
function MeterWorkspace({ session, clubId, reviewerMemberId, seasonYear, canReadMeters, canSubmitOwnMeterReadings, mode }: MeterViewProps & { mode: "capture" | "annual" | "review" }) {
  const canApprove = mode === "review";
  const club = useSelectedClub();
  const [meters, setMeters] = useState<Meter[]>([]);
  const [parcels, setParcels] = useState<Parcel[]>([]);
  const [readings, setReadings] = useState<Reading[]>([]);
  const [dueStatuses, setDueStatuses] = useState<Awaited<ReturnType<typeof loadMeterOverview>>["dueStatuses"]>([]);
  const [reviewData, setReviewData] = useState<ReadingReviewData>({ open: [], historyByReadingId: new Map() });
  const [selectedReadingId, setSelectedReadingId] = useState<number | null>(null);
  const [wifiOnly, setWifiOnly] = useState(getWifiOnlyPreference);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [loadedScope, setLoadedScope] = useState("");
  const scope = `${club.vereinId}:${session.accessToken}:${canReadMeters}:${canApprove}:${reviewerMemberId ?? ""}`;
  function changeWifiOnly(value: boolean) { setWifiOnly(value); setWifiOnlyPreference(value); }
  const loadData = useCallback(async () => Promise.all([
    canReadMeters ? loadMeterOverview(club, session) : canApprove ? loadMeterReferenceData(club, session).then((data) => ({ ...data, dueStatuses: [] })) : Promise.resolve({ meters: [], parcels: [], dueStatuses: [] }),
    canReadMeters ? readSupabaseRest<Reading>({ club, session }, "zaehler_ablesung", { select: "id,zaehler_id,stand,ablesedatum,art,freigegeben,pruefstatus,pruefkommentar,geprueft_von,geprueft_am,foto_pfad,foto_drive_file_id,foto_dateiname", order: "ablesedatum.desc", limit: "1000" }) : Promise.resolve([]),
    canApprove ? loadReadingReviewData(club, session, { canApproveMeterReadings: canApprove, reviewerMemberId }) : Promise.resolve({ open: [], historyByReadingId: new Map() }),
  ]), [club, session, canReadMeters, canApprove, reviewerMemberId]);
  const applyLoadedData = useCallback(([meterOverview, loadedReadings, nextReviewData]: Awaited<ReturnType<typeof loadData>>) => {
    setMeters(meterOverview.meters);
    setParcels(meterOverview.parcels);
    setDueStatuses(meterOverview.dueStatuses);
    setReadings(loadedReadings);
    setReviewData(nextReviewData);
    setLoadedScope(scope);
  }, [scope]);
  async function load() {
    setLoading(true); setError("");
    try { applyLoadedData(await loadData()); }
    catch (cause) { setError(cause instanceof Error ? cause.message : "Ablesungen konnten nicht geladen werden."); setLoadedScope(scope); }
    finally { setLoading(false); }
  }
  useEffect(() => {
    let active = true;
    loadData().then((data) => {
      if (!active) return;
      applyLoadedData(data);
      setError("");
      setLoading(false);
    }).catch((cause: unknown) => {
      if (!active) return;
      setError(cause instanceof Error ? cause.message : "Ablesungen konnten nicht geladen werden.");
      setLoadedScope(scope);
      setLoading(false);
    });
    return () => { active = false; };
  }, [applyLoadedData, loadData, scope]);
  useEffect(() => {
    const sync = () => { void retryAllPendingReadingPhotos(club, session, clubId, wifiOnly); };
    sync(); return subscribeConnectivityChanges(sync);
  }, [session, clubId, wifiOnly, club]);

  const parcelName = (parcelId: number) => { const parcel = parcels.find((item) => item.id === parcelId); return parcel ? `${parcel.garten_nr} – ${parcel.Anlage}` : "Unbekannte Parzelle"; };
  const meterFor = (meterId: number) => meters.find((item) => item.id === meterId);
  const openReadings = reviewData.open;
  const selectedReading = openReadings.find((item) => item.readingId === selectedReadingId) ?? null;
  const selectedHistory = selectedReading ? reviewData.historyByReadingId.get(selectedReading.readingId) ?? [] : [];

  return <section className="data-workspace" aria-label="Zähler und Ablesungen">
    <section className="meter-preferences"><label className="wifi-setting"><input type="checkbox" checked={wifiOnly} onChange={(event) => changeWifiOnly(event.target.checked)} /> Fotos nur bei erkanntem WLAN direkt hochladen</label></section>
    {mode !== "review" && <MeterReadingEntry session={session} clubId={clubId} seasonYear={seasonYear} initialKind={mode === "annual" ? "jea" : "normal"} permissions={{ canReadMeters, canSubmitOwnMeterReadings, memberId: reviewerMemberId }} wifiOnly={wifiOnly} onSaved={load} />}
    {canReadMeters && <div className="meter-summary"><div><span>Aktive Zähler</span><strong>{meters.filter((item) => !item.ausgebaut_am).length}</strong></div><div><span>Offene Prüfungen</span><strong>{openReadings.length}</strong></div><div><span>Aktive Eichstatus</span><strong>{dueStatuses.length}</strong></div></div>}
    {mode === "review" && <h2>Eingereichte Ablesungen</h2>}
    {loading || loadedScope !== scope ? <p>Lädt …</p> : error ? <p className="notice" role="alert">{error}</p> : canReadMeters ? <>
      <div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Medium</th><th>Zähler</th><th>Einbau</th><th>Eichfällig</th><th>Status</th></tr></thead><tbody>{meters.map((meter) => <tr key={meter.id}><td>{parcelName(meter.parzelle_id)}</td><td>{meter.medium}</td><td><strong>{meter.zaehlernummer}</strong></td><td>{formatDate(meter.eingebaut_am)}</td><td>{formatDate(meter.eichfaellig_am)}</td><td>{meter.ausgebaut_am ? "ausgebaut" : meter.status ?? "aktiv"}</td></tr>)}</tbody></table></div>
      <section className="detail-panel reading-history"><h2>Ablesehistorie</h2><div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Zähler</th><th>Art</th><th>Datum</th><th>Stand</th><th>Status</th><th>Foto</th></tr></thead><tbody>{readings.map((reading) => { const meter = meterFor(reading.zaehler_id); return <tr key={reading.id}><td>{meter ? parcelName(meter.parzelle_id) : "–"}</td><td>{meter?.zaehlernummer ?? "–"}</td><td>{reading.art.toUpperCase()}</td><td>{formatDate(reading.ablesedatum)}</td><td>{reading.stand}</td><td>{reading.freigegeben ? "freigegeben" : reading.pruefstatus}</td><td>{reading.foto_drive_file_id || reading.foto_pfad ? <PhotoOpenButton session={session} reading={reading} /> : "–"}</td></tr>; })}</tbody></table></div></section>
    </> : canApprove ? <div className="split-view review-workspace"><div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Mitglied</th><th>Zähler</th><th>Art</th><th>Datum</th><th>Stand</th><th>Foto</th></tr></thead><tbody>{openReadings.map((reading) => <tr key={reading.readingId} className={selectedReading?.readingId === reading.readingId ? "selected-row" : ""} onClick={() => setSelectedReadingId(reading.readingId)}><td>{reading.gartenNr}{reading.anlage ? ` – ${reading.anlage}` : ""}</td><td>{reading.mitgliedName}</td><td>{`${reading.medium} · ${reading.zaehlernummer}`}</td><td>{reading.art.toUpperCase()}</td><td>{formatDate(reading.ablesedatum)}</td><td>{reading.stand}</td><td>{reading.fotoDriveFileId || reading.fotoPfad ? "vorhanden" : "–"}</td></tr>)}</tbody></table>{openReadings.length === 0 && <p className="empty-state">Keine offenen Ablesungen zur Prüfung.</p>}</div><ReadingReview key={selectedReading?.readingId ?? "empty"} session={session} reading={selectedReading} history={selectedHistory} permissions={{ canApproveMeterReadings: canApprove, reviewerMemberId }} onSaved={async () => { setSelectedReadingId(null); await load(); }} /></div> : null}
  </section>;
}
function RfidAssignmentPanel({ session, selectedParcelId }: { session: BrowserSession; selectedParcelId: number | null }) {
  const club = useSelectedClub();
  const [parcels, setParcels] = useState<Awaited<ReturnType<typeof listRfidSetupParcels>>>([]);
  const [parcelId, setParcelId] = useState(selectedParcelId ? String(selectedParcelId) : "");
  const [mediumOptions, setMediumOptions] = useState<RfidMediumOption[]>([]);
  const [medium, setMedium] = useState("");
  const [uid, setUid] = useState("");
  const [resolution, setResolution] = useState<RfidScanResolution | null>(null);
  const [check, setCheck] = useState<RfidAssignmentCheck | null>(null);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => { listRfidSetupParcels(club, session).then(setParcels).catch((cause) => setMessage(cause instanceof Error ? cause.message : "Parzellen konnten nicht geladen werden.")); }, [session, club]);
  useEffect(() => { let active = true; const id = Number(parcelId); if (!id) return () => { active = false; }; listAvailableRfidMediumOptions(club, session, id).then((items) => { if (!active) return; setMediumOptions(items); setMedium((current) => items.some((item) => item.key === current) ? current : items[0]?.key ?? ""); }).catch((cause) => { if (active) setMessage(cause instanceof Error ? cause.message : "Medien konnten nicht geladen werden."); }); return () => { active = false; }; }, [session, parcelId, club]);
  async function resolve() { setBusy(true); setCheck(null); try { const result = await resolveRfidScanContext(club, session, uid); setResolution(result); setUid(result.normalizedUid); setMessage(result.message); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "RFID-Kontext konnte nicht geladen werden."); } finally { setBusy(false); } }
  async function verify() { setBusy(true); try { const result = await checkRfidAssignment(club, session, Number(parcelId), medium, uid); setCheck(result); setMessage(result.message); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "RFID-Zuordnung konnte nicht geprüft werden."); } finally { setBusy(false); } }
  async function save(overwriteExisting = false) { setBusy(true); try { const result = await assignRfid(club, session, Number(parcelId), medium, uid, overwriteExisting); if (result.requiresOverwriteConfirmation) { if (window.confirm(`${result.message}\n\nBestehende RFID-Zuordnung am Ziel wirklich überschreiben?`)) { await save(true); return; } setMessage("Überschreiben wurde abgelehnt."); return; } setMessage(result.message); if (result.success) { setCheck(null); setResolution(null); setUid(""); } } catch (cause) { setMessage(cause instanceof Error ? cause.message : "RFID konnte nicht gespeichert werden."); } finally { setBusy(false); } }
  const showAssignment = resolution?.state === "Unknown";
  return <section className="detail-panel member-editor" aria-label="RFID einrichten"><div className="detail-title"><div><h2>RFID einrichten</h2><p>RFID-UID prüfen und kontrolliert einer Parzelle zuordnen.</p></div></div>{message && <p className="notice" role="status">{message}</p>}<fieldset disabled={busy}><label className="wide">RFID-UID<input value={uid} onChange={(event) => { setUid(event.target.value); setResolution(null); setCheck(null); }} placeholder="RFID-UID eingeben" /></label></fieldset><div className="editor-actions"><button className="secondary-action" disabled={busy || !uid.trim()} onClick={() => void resolve()}>{busy ? "Prüft …" : "RFID-Kontext auflösen"}</button></div>{resolution && <section className="parcel-section"><h3>{resolution.state}</h3>{resolution.context ? <p><strong>{resolution.context.garten_nr ?? "–"} – {resolution.context.anlage ?? "–"}</strong><br />{resolution.context.medium ?? "–"} · {resolution.context.zaehlernummer ?? "Kein aktiver Zähler"}<br />Status: {resolution.context.status ?? "Kein aktiver Zähler"}</p> : <p>Die UID ist noch keiner Parzelle zugeordnet.</p>}</section>}{showAssignment && <section className="parcel-section"><h3>Neue RFID-Zuordnung</h3><fieldset disabled={busy}><label>Parzelle<select value={parcelId} onChange={(event) => { setParcelId(event.target.value); setCheck(null); }}><option value="">Parzelle wählen</option>{parcels.map((parcel) => <option key={parcel.id} value={parcel.id}>{parcel.garten_nr} – {parcel.Anlage}</option>)}</select></label><label>Medium<select value={medium} onChange={(event) => { setMedium(event.target.value); setCheck(null); }} disabled={!parcelId}><option value="">Medium wählen</option>{mediumOptions.map((item) => <option key={item.key} value={item.key}>{item.displayName}</option>)}</select></label></fieldset><div className="editor-actions"><button className="secondary-action" disabled={busy || !parcelId || !medium} onClick={() => void verify()}>Zuordnung prüfen</button><button disabled={busy || !check?.isValid} onClick={() => void save()}>{busy ? "Speichert …" : "RFID speichern"}</button></div>{check?.conflict && <p className="context-note">Konflikt: bereits bei {check.conflict.garten_nr ?? "–"} – {check.conflict.anlage ?? "–"} für {check.conflict.medium ?? "–"} hinterlegt. Diese Zuordnung wird nicht automatisch geändert.</p>}</section>}</section>;
}

function confirmReadingWarnings(warnings: Array<{ message: string }>) {
  return window.confirm(`Plausibilitätswarnung:\n\n${warnings.map((warning) => warning.message).join("\n\n")}\n\nBitte den Zählerstand vor Ort prüfen. Trotzdem speichern?`);
}

function MeterChange({ session, clubId, canManageMeterChanges }: { session: BrowserSession; clubId: string; canManageMeterChanges: boolean }) {
  const club = useSelectedClub();
  const [uid,setUid]=useState(""); const [resolution,setResolution]=useState<RfidScanResolution|null>(null); const [context,setContext]=useState<MeterInstallationContext|null>(null); const [installationDate,setInstallationDate]=useState(currentLocalDate); const [meterNumber,setMeterNumber]=useState(""); const [calibrationYear,setCalibrationYear]=useState(()=>String(new Date().getFullYear())); const [installedMeter,setInstalledMeter]=useState<Meter|null>(null); const [initialValue,setInitialValue]=useState(""); const [file,setFile]=useState<File|null>(null); const [installationPhotoRequired,setInstallationPhotoRequired]=useState(true); const [removedAt,setRemovedAt]=useState(currentLocalDate); const [endValue,setEndValue]=useState(""); const [progress,setProgress]=useState<{readingId:number;removedAt:string;endValue:number;photoHandled:boolean;photoMessage:string}|null>(null); const [message,setMessage]=useState(""); const [saving,setSaving]=useState(false);
  const wifiOnly=getWifiOnlyPreference;
  function reset(){setResolution(null);setContext(null);setInstalledMeter(null);setInitialValue("");setFile(null);setMeterNumber("");setCalibrationYear(String(new Date().getFullYear()));setInstallationDate(currentLocalDate());setRemovedAt(currentLocalDate());setEndValue("");setProgress(null);}
  async function resolve(){setSaving(true);try{const next=await resolveRfidScanContext(club, session,uid);setResolution(next);setUid(next.normalizedUid);setContext(installationContextFromResolution(next));setInstallationPhotoRequired(await getMeterInstallationPhotoRequired(club, session));setMessage(next.message);}catch(e){setMessage(e instanceof Error?e.message:"RFID-Kontext konnte nicht geladen werden.");}finally{setSaving(false);}}
  async function install(){if(!context)return;setSaving(true);try{const m=await createMeterInstallationForRfid(club, session,{canManageMeterChanges},context,{meterNumber,installationDate,calibrationYear});setInstalledMeter(m);setMessage("Zähler angelegt. Jetzt folgt die Anfangsablesung.");}catch(e){setMessage(e instanceof Error?e.message:"Zähler konnte nicht angelegt werden.");}finally{setSaving(false);}}
  async function initial(){if(!installedMeter||!context||!initialValue.trim()||(installationPhotoRequired&&!file)){setMessage(installationPhotoRequired&&!file?"Für die Anfangsablesung ist ein Foto erforderlich.":"Bitte einen Anfangsstand eingeben.");return;}setSaving(true);try{let r=await saveMeterInstallationReading(club, session,{canManageMeterChanges},{meterId:installedMeter.id,date:installationDate,value:Number(initialValue.replace(",","."))});if("requiresConfirmation" in r){if(!confirmReadingWarnings(r.plausibility.warnings)){setMessage("Einbauablesung wurde nicht gespeichert. Bitte den Zählerstand vor Ort prüfen.");return;}r=await saveMeterInstallationReading(club, session,{canManageMeterChanges},{meterId:installedMeter.id,date:installationDate,value:Number(initialValue.replace(",",".")),confirmWarnings:true});}const p=await handleReadingPhoto(club, session,{clubId,readingId:r.id,file,details:{kind:"einbau",datum:installationDate,medium:installedMeter.medium,anlage:context.anlage,garten:context.gardenNr,zaehlernummer:installedMeter.zaehlernummer},wifiOnly:wifiOnly()});reset();setUid("");setMessage(`Einbau erfolgreich abgeschlossen. ${p.message} Bitte RFID erneut scannen.`);}catch(e){setMessage(e instanceof Error?e.message:"Einbauablesung konnte nicht gespeichert werden.");}finally{setSaving(false);}}
  async function remove(){if(!resolution?.context?.aktiver_zaehler_id)return;if(!progress&&!window.confirm("Wollen Sie den Zähler ausbauen?"))return;if(!progress&&!file){setMessage("Für die Schlussablesung ist ein Foto erforderlich.");return;}setSaving(true);try{let next=progress;if(!next){let r=await saveMeterRemovalReading(club, session,{canManageMeterChanges},{meterId:resolution.context.aktiver_zaehler_id,date:removedAt,value:Number(endValue.replace(",","."))});if("requiresConfirmation" in r){if(!confirmReadingWarnings(r.plausibility.warnings)){setMessage("Zählerausbau wurde abgebrochen. Bitte den Zählerstand vor Ort prüfen.");return;}r=await saveMeterRemovalReading(club, session,{canManageMeterChanges},{meterId:resolution.context.aktiver_zaehler_id,date:removedAt,value:Number(endValue.replace(",",".")),confirmWarnings:true});}next={readingId:r.id,removedAt,endValue:Number(endValue.replace(",",".")),photoHandled:false,photoMessage:""};setProgress(next);}if(!next.photoHandled){const p=await handleReadingPhoto(club, session,{clubId,readingId:next.readingId,file,details:{kind:"ausbau",datum:next.removedAt,medium:resolution.context.medium??"",anlage:resolution.context.anlage??"",garten:resolution.context.garten_nr??"",zaehlernummer:resolution.context.zaehlernummer??""},wifiOnly:wifiOnly()});next={...next,photoHandled:true,photoMessage:p.message};setProgress(next);}await completeMeterRemoval(club, session,{canManageMeterChanges},{uid:resolution.normalizedUid,meterId:resolution.context.aktiver_zaehler_id,parcelId:resolution.context.parzelle_id,medium:resolution.context.medium??"",removedAt:next.removedAt});const text=next.photoMessage;reset();setUid("");setMessage(`Zähler ausgebaut. ${text} Bitte RFID erneut scannen.`);}catch(e){setMessage(e instanceof Error?e.message:"Zähler konnte nicht ausgebaut werden.");}finally{setSaving(false);}}
  const active=resolution?.state==="KnownWithActiveMeter"&&resolution.context; const locked=Boolean(installedMeter||progress);
  return <section className="data-workspace"><section className="detail-panel member-editor"><h2>Zählerwechsel</h2>{message&&<p className="notice">{message}</p>}<label>RFID-UID<input value={uid} disabled={locked} onChange={e=>{reset();setUid(e.target.value);}} /></label><button disabled={saving||locked||!uid.trim()} onClick={()=>void resolve()}>RFID-Kontext auflösen</button>{active?<><p>{active.garten_nr} – {active.anlage}<br />{active.medium} · {active.zaehlernummer}</p><fieldset disabled={saving||Boolean(progress)}><label>Ausbaudatum<input type="date" value={progress?.removedAt??removedAt} onChange={e=>setRemovedAt(e.target.value)} /></label><label>Endstand<input value={progress?String(progress.endValue):endValue} onChange={e=>setEndValue(e.target.value)} /></label><label>Foto *<input type="file" disabled={Boolean(progress?.photoHandled)} onChange={e=>setFile(e.target.files?.[0]??null)} /></label></fieldset><button disabled={saving||(!progress&&!endValue.trim())} onClick={()=>void remove()}>{progress?"Ausbau fortsetzen":"Zähler ausbauen"}</button></>:context&&!installedMeter?<><label>Einbaudatum<input type="date" value={installationDate} onChange={e=>setInstallationDate(e.target.value)} /></label><label>Zählernummer<input value={meterNumber} onChange={e=>setMeterNumber(e.target.value)} /></label><label>Eichjahr<input value={calibrationYear} onChange={e=>setCalibrationYear(e.target.value)} /></label><button onClick={()=>void install()}>Zähler anlegen</button></>:installedMeter?<><p>Zähler angelegt: {installedMeter.zaehlernummer}</p><label>Anfangsstand<input value={initialValue} onChange={e=>setInitialValue(e.target.value)} /></label><label>Foto{installationPhotoRequired?" *":""}<input type="file" onChange={e=>setFile(e.target.files?.[0]??null)} /></label><button onClick={()=>void initial()}>Anfangsablesung speichern</button></>:null}</section></section>;
}
function MeterReadingEntry({ session, clubId, seasonYear, initialKind, permissions: { canReadMeters, canSubmitOwnMeterReadings, memberId }, wifiOnly, onSaved }: { session: BrowserSession; clubId: string; seasonYear: number; initialKind: "normal" | "jea"; permissions: ReadingPermissions; wifiOnly: boolean; onSaved: () => void | Promise<void> }) {
  const club = useSelectedClub();
  const [context, setContext] = useState<ReadingCaptureContext | null>(null); const [meterId, setMeterId] = useState(""); const [parcelId, setParcelId] = useState(""); const [medium, setMedium] = useState(""); const kind = initialKind; const [value, setValue] = useState(""); const [date, setDate] = useState(currentLocalDate); const [file, setFile] = useState<File | null>(null); const [previewUrl, setPreviewUrl] = useState(""); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const permissions = useMemo<ReadingPermissions>(() => ({ canReadMeters, canSubmitOwnMeterReadings, memberId }), [canReadMeters, canSubmitOwnMeterReadings, memberId]);
  useEffect(() => { let active = true; loadReadingCaptureContext(club, session, permissions).then((next) => { if (active) { setContext(next); setMessage(next.message ?? ""); } }).catch((cause) => { if (active) setMessage(cause instanceof Error ? cause.message : "Ablesekontext konnte nicht geladen werden."); }); return () => { active = false; }; }, [session, permissions, club]);
  useEffect(() => { if (!previewUrl) return; return () => URL.revokeObjectURL(previewUrl); }, [previewUrl]);
  const meters = context?.meters ?? []; const parcels = context?.parcels ?? []; const selectedParcel = parcels.find((item) => item.id === Number(parcelId)); const filteredMeters = meters.filter((item) => (!parcelId || item.parzelle_id === Number(parcelId)) && (!medium || item.medium.toLocaleLowerCase("de") === medium));
  async function save() { const stand = Number(value.replace(",", ".")); if (context?.photoRequired && !file) { setMessage("Für diese Ablesung ist ein Foto erforderlich."); return; } setSaving(true); setMessage(""); try { let result = await saveMeterReading(club, session, permissions, { meterId: Number(meterId), date, value: stand, art: kind }); if ("requiresConfirmation" in result) { if (!confirmReadingWarnings(result.plausibility.warnings)) { setMessage("Ablesung wurde nicht gespeichert. Bitte den Zählerstand vor Ort prüfen."); return; } result = await saveMeterReading(club, session, permissions, { meterId: Number(meterId), date, value: stand, art: kind, confirmWarnings: true }); } const meter = meters.find((item) => item.id === result.reading.zaehler_id); const parcel = meter && parcels.find((item) => item.id === meter.parzelle_id); const readingMessage = result.isSubmission ? "Ablesung wurde zur Prüfung eingereicht." : "Ablesung wurde direkt freigegeben gespeichert."; if (file && meter && parcel) { const photo = await handleReadingPhoto(club, session, { clubId, readingId: result.reading.id, file, details: { datum: date, medium: meter.medium, anlage: parcel.Anlage, garten: parcel.garten_nr, zaehlernummer: meter.zaehlernummer }, wifiOnly }); setMessage(`${readingMessage} ${photo.message}`); } else setMessage(readingMessage); setMeterId(""); setValue(""); setFile(null); setPreviewUrl(""); await onSaved(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Ablesung konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  const enabled = Boolean(context && !context.message);
  return <section className="detail-panel member-editor meter-entry"><div className="detail-title"><div><h2>{kind === "jea" ? `Jahresendablesung ${seasonYear} erfassen` : "Ablesung erfassen"}</h2><p>{context?.photoRequired ? "Foto ist erforderlich" : "Foto ist optional"} · {context?.isDirectCapture ? "wird direkt freigegeben" : "wird zur Prüfung eingereicht"}</p></div></div>{message && <p className="notice" role="status">{message}</p>}{context?.isDirectCapture && <RfidBrowserScanner session={session} onResolved={(resolution) => { if (resolution.state === "KnownWithActiveMeter" && resolution.context?.aktiver_zaehler_id) { setParcelId(String(resolution.context.parzelle_id)); setMedium(String(resolution.context.medium ?? "").toLocaleLowerCase("de")); setMeterId(String(resolution.context.aktiver_zaehler_id)); setMessage(`RFID erkannt: Garten ${resolution.context.garten_nr ?? "–"}, ${resolution.context.medium ?? "–"}.`); } else setMessage(resolution.message); }} />}<fieldset disabled={!enabled || saving}><label>Parzelle<select value={parcelId} onChange={(event) => { setParcelId(event.target.value); setMedium(""); setMeterId(""); }}><option value="">Parzelle wählen</option>{parcels.map((parcel) => <option key={parcel.id} value={parcel.id}>{parcel.garten_nr} – {parcel.Anlage}</option>)}</select></label><label>Medium<select value={medium} onChange={(event) => { setMedium(event.target.value); setMeterId(""); }}><option value="">Medium wählen</option>{selectedParcel?.hat_strom && <option value="strom">Strom</option>}{selectedParcel?.hat_wasser && <option value="wasser">Wasser</option>}</select></label><label className="wide">Aktiver Zähler *<select value={meterId} onChange={(event) => setMeterId(event.target.value)}><option value="">Zähler wählen</option>{filteredMeters.map((item) => { const parcel = parcels.find((entry) => entry.id === item.parzelle_id); return <option key={item.id} value={item.id}>Garten {parcel?.garten_nr ?? "?"} · {item.medium} · {item.zaehlernummer}</option>; })}</select></label><label>Datum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Stand<input inputMode="decimal" value={value} onChange={(event) => setValue(event.target.value)} /></label><label className="wide">Foto aufnehmen oder auswählen<input type="file" accept="image/*" capture="environment" onChange={(event) => { const nextFile = event.target.files?.[0] ?? null; setFile(nextFile); setPreviewUrl(nextFile ? URL.createObjectURL(nextFile) : ""); }} /></label></fieldset>{file && previewUrl && <div className="photo-preview"><Image src={previewUrl} alt="Vorschau des ausgewählten Ablesefotos" width={640} height={480} unoptimized /><button className="secondary-action" onClick={() => { setFile(null); setPreviewUrl(""); }}>Foto entfernen</button></div>}<div className="editor-actions"><button disabled={saving || !enabled} onClick={save}>{saving ? "Speichert …" : context?.isDirectCapture ? "Ablesung speichern" : "Ablesung einreichen"}</button></div></section>;
}

function PhotoOpenButton({ session, reading }: { session: BrowserSession; reading: { id?: number; readingId?: number; foto_pfad?: string | null; foto_drive_file_id?: string | null; fotoPfad?: string | null; fotoDriveFileId?: string | null } }) {
  const club = useSelectedClub();
  const [opening, setOpening] = useState(false); const [error, setError] = useState("");
  async function open() { setOpening(true); setError(""); try { const path = reading.foto_pfad ?? reading.fotoPfad; const driveFileId = reading.foto_drive_file_id ?? reading.fotoDriveFileId; if (!driveFileId && path && /^https?:\/\//i.test(path)) { window.open(path, "_blank", "noopener,noreferrer"); return; } const readingId = reading.id ?? reading.readingId; if (!readingId) throw new Error("Die Ablesung ist ungültig."); const url = await openStoredReadingPhoto(club, session, readingId); window.open(url, "_blank", "noopener,noreferrer"); window.setTimeout(() => URL.revokeObjectURL(url), 60_000); } catch (cause) { setError(cause instanceof Error ? cause.message : "Foto konnte nicht geöffnet werden."); } finally { setOpening(false); } }
  return <span className="photo-open"><button className="table-action" disabled={opening} onClick={open}>{opening ? "Öffnet …" : "Foto anzeigen"}</button>{error && <small>{error}</small>}</span>;
}

function ReadingReview({ session, reading, history, permissions, onSaved }: { session: BrowserSession; reading: ReadingReviewItem | null; history: ReadingReviewItem[]; permissions: { canApproveMeterReadings: boolean; reviewerMemberId: number | null }; onSaved: () => void }) {
  const club = useSelectedClub();
  const [comment, setComment] = useState(""); const [date, setDate] = useState(() => reading?.ablesedatum?.slice(0, 10) ?? ""); const [value, setValue] = useState(() => reading ? String(reading.stand) : ""); const [saving, setSaving] = useState(false); const [error, setError] = useState("");
  const editLock = useEditLock(club, session, "zaehler_ablesung", reading?.readingId, Boolean(reading));
  useEffect(() => { if (editLock.message) queueMicrotask(() => setError(editLock.message)); }, [editLock.message]);
  async function decide(action: "freigeben" | "ablehnen" | "korrigieren" | "entfernen") {
    if (!reading) return;
    if (!editLock.acquired) { setError(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!comment.trim()) { setError(action === "entfernen" ? "Eine Löschbegründung ist erforderlich." : "Ein Prüfkommentar ist erforderlich."); return; }
    const stand = Number(value.replace(",", "."));
    if (action === "korrigieren" && (!date || !Number.isFinite(stand) || stand < 0)) { setError("Für die Korrektur müssen Datum und Zählerstand gültig sein."); return; }
    if (action === "korrigieren" && !window.confirm("Die Ablesung mit den geänderten Werten korrigieren und direkt freigeben?")) return;
    if (action === "entfernen" && !window.confirm("Die Ablesung mit Begründung aus dem offenen Prüfprozess entfernen?")) return;
    setSaving(true); setError("");
    try {
      await reviewMeterReading(club, session, permissions, { readingId: reading.readingId, action, comment, date, value: stand });
      await onSaved();
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Die Entscheidung konnte nicht gespeichert werden."); } finally { setSaving(false); }
  }
  return <aside className="detail-panel review-panel"><h2>Ablesung prüfen</h2>{reading ? <><p><strong>{reading.mitgliedName}</strong><br />{formatDate(reading.ablesedatum)} · Stand <strong>{reading.stand}</strong> · {reading.art.toUpperCase()}</p>{reading.fotoDriveFileId || reading.fotoPfad ? <PhotoOpenButton session={session} reading={reading} /> : <p className="context-note">Zu dieser Ablesung ist kein Foto hinterlegt.</p>}{!permissions.reviewerMemberId && <p className="notice">Für die Prüfung ist ein verknüpftes Mitgliedskonto erforderlich.</p>}{error && <p className="notice">{error}</p>}<label>Prüfkommentar / Begründung *<textarea value={comment} onChange={(event) => setComment(event.target.value)} /></label><details><summary>Korrekturwerte</summary><label>Datum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Zählerstand<input inputMode="decimal" value={value} onChange={(event) => setValue(event.target.value)} /></label></details><div className="review-actions meter-review-actions"><button disabled={saving || !permissions.reviewerMemberId} onClick={() => decide("freigeben")}>Freigeben</button><button disabled={saving || !permissions.reviewerMemberId} className="secondary-action" onClick={() => decide("korrigieren")}>Korrigieren</button><button disabled={saving || !permissions.reviewerMemberId} className="reject-action" onClick={() => decide("ablehnen")}>Ablehnen</button><button disabled={saving || !permissions.reviewerMemberId} className="danger-action" onClick={() => decide("entfernen")}>Entfernen</button></div><h3>Letzte Ablesungen dieses Zählers</h3>{history.length ? <ul className="review-history">{history.map((item) => <li key={item.readingId}><strong>{item.stand} · {item.art.toUpperCase()}</strong><span>{formatDate(item.ablesedatum)} · {item.decisionLabel}</span>{item.pruefkommentar && <p>{item.pruefkommentar}</p>}</li>)}</ul> : <p>Noch keine vorherige Ablesung vorhanden.</p>}</> : <p>Wähle links eine eingereichte Ablesung aus.</p>}</aside>;
}
function formatDate(value: string | null | undefined) {
  if (!value) return "–";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "–" : new Intl.DateTimeFormat("de-DE").format(date);
}

function currentLocalDate() {
  const date = new Date();
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function MaintenanceContracts({ session, memberId, canManage }: { session: BrowserSession; memberId?: number; canManage: boolean }) {
  const access = maintenanceAccess(canManage);
  return memberId ? <MemberMaintenanceContracts session={session} memberId={memberId} canManage={access.canManageAssignments} /> : <GlobalMaintenanceContracts session={session} canManage={access.canManageContracts} />;
}

function GlobalMaintenanceContracts({ session, canManage }: { session: BrowserSession; canManage: boolean }) {
  const club = useSelectedClub();
  const [contracts, setContracts] = useState<MaintenanceContract[]>([]); const [assignments, setAssignments] = useState<MaintenanceAssignment[]>([]); const [members, setMembers] = useState<Member[]>([]); const [parcels, setParcels] = useState<Parcel[]>([]); const [parcelAssignments, setParcelAssignments] = useState<ParcelAssignment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null); const [editing, setEditing] = useState(false); const [editingId, setEditingId] = useState<number | null>(null); const [draft, setDraft] = useState<MaintenanceContractDraft | null>(null); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const [assigningMembers, setAssigningMembers] = useState(false); const [endDates, setEndDates] = useState<Record<number, string>>({});
  const today = currentLocalDate();
  async function load() {
    setMessage("");
    try {
      const [loadedContracts, loadedAssignments, loadedMembers, loadedParcels, loadedParcelAssignments] = await Promise.all([
        loadMaintenanceContracts(club, session),
        loadMaintenanceAssignments(club, session),
        readSupabaseRest<Member>({ club, session }, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", aktiv: "eq.true", aufnahme_status: "eq.aufgenommen", order: "name.asc,vorname.asc", limit: "1000" }),
        readSupabaseRest<Parcel>({ club, session }, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", limit: "1000" }),
        readSupabaseRest<ParcelAssignment>({ club, session }, "parzellen_belegung", { select: "id,parzelle_id,mitglied_id,von_datum,bis_datum", limit: "2000" }),
      ]);
      setContracts(loadedContracts); setAssignments(loadedAssignments); setMembers(loadedMembers); setParcels(loadedParcels); setParcelAssignments(loadedParcelAssignments);
      setSelectedId((current) => current && loadedContracts.some((item) => item.id === current) ? current : loadedContracts[0]?.id ?? null);
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Wartungsverträge konnten nicht geladen werden."); }
  }
  useEffect(() => {
    let active = true;
    Promise.all([
      loadMaintenanceContracts(club, session),
      loadMaintenanceAssignments(club, session),
      readSupabaseRest<Member>({ club, session }, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", aktiv: "eq.true", aufnahme_status: "eq.aufgenommen", order: "name.asc,vorname.asc", limit: "1000" }),
      readSupabaseRest<Parcel>({ club, session }, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", limit: "1000" }),
      readSupabaseRest<ParcelAssignment>({ club, session }, "parzellen_belegung", { select: "id,parzelle_id,mitglied_id,von_datum,bis_datum", limit: "2000" }),
    ]).then(([loadedContracts, loadedAssignments, loadedMembers, loadedParcels, loadedParcelAssignments]) => {
      if (!active) return;
      setContracts(loadedContracts); setAssignments(loadedAssignments); setMembers(loadedMembers); setParcels(loadedParcels); setParcelAssignments(loadedParcelAssignments); setMessage("");
      setSelectedId((current) => current && loadedContracts.some((item) => item.id === current) ? current : loadedContracts[0]?.id ?? null);
    }).catch((cause) => {
      if (active) setMessage(cause instanceof Error ? cause.message : "Wartungsverträge konnten nicht geladen werden.");
    });
    return () => { active = false; };
  }, [session, club]);
  const selected = contracts.find((item) => item.id === selectedId) ?? null;
  const editLock = useEditLock(club, session, "wartungsvertraege", editingId, Boolean(editing && editingId));
  const selectedAssignments = selected ? assignments.filter((item) => item.maintenanceContractId === selected.id) : [];
  const memberName = (id: number) => { const member = members.find((item) => item.id === id); return member ? `${member.name ?? ""}, ${member.vorname ?? ""}`.replace(/^, |, $/g, "") || (member.mitgliedsnummer ? `Mgl.-Nr. ${member.mitgliedsnummer}` : "Mitglied") : "Mitglied"; };
  const memberContext = (id: number) => members.find((item) => item.id === id)?.hauptmitglied_id ? "Nebenmitglied" : "Hauptmitglied";
  const gardenNumbers = (id: number) => { const parcelIds = parcelAssignments.filter((item) => item.mitglied_id === id && (!item.bis_datum || item.bis_datum >= today)).map((item) => item.parzelle_id); return parcels.filter((item) => parcelIds.includes(item.id)).map((item) => item.garten_nr).filter(Boolean).join(", ") || "–"; };
  function startNew() { setEditingId(null); setDraft({ title: "", description: "", area: "", maxActiveAssignments: 1, exemptsFromDutyHours: false, workHoursCredit: 0, active: true, note: "" }); setEditing(true); setMessage(""); }
  function startEdit() { if (selected) { const { id, ...nextDraft } = selected; setEditingId(id); setDraft(nextDraft); setEditing(true); setMessage(""); } }
  useEffect(() => { if (editing && editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editing, editLock.message]);
  async function saveContractWithDraft(nextDraft: MaintenanceContractDraft, fundamentalChange = false) { const validation = normalizeAndValidateMaintenanceContract(nextDraft); if (!validation.payload) { setMessage(validation.error ?? "Wartungsvertrag ist ungültig."); return; } if (editingId && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; } setSaving(true); setMessage(""); try { const saved = await saveMaintenanceContract(club, session, editingId, nextDraft, fundamentalChange); setSelectedId(saved.id); setEditingId(null); setEditing(false); await load(); setMessage(fundamentalChange ? "Neue Wartungsvertragsfassung angelegt. Neue Zuordnungen benötigen einen neuen unterschriebenen Erstvertrag." : "Wartungsvertrag gespeichert."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Wartungsvertrag konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  async function endAssignment(item: MaintenanceAssignment) { const endDate = endDates[item.id] || today; setSaving(true); setMessage(""); try { await endMaintenanceAssignmentForContract(club, session, item, endDate); await load(); setMessage("Wartungsvertrag wurde beendet."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Zuordnung konnte nicht beendet werden."); } finally { setSaving(false); } }
  const overview = buildMaintenanceContractOverview(contracts, assignments, today);
  if (editing && draft) return <MaintenanceContractEditor draft={draft} existing={Boolean(editingId)} saving={saving} message={message} onSave={(nextDraft, fundamentalChange) => { setDraft(nextDraft); void saveContractWithDraft(nextDraft, fundamentalChange); }} onCancel={() => { setEditing(false); setEditingId(null); setDraft(null); }} />;
  return <><ExpiringMaintenanceContracts session={session} /><MaintenanceContractOverview items={overview} selectedId={selectedId} onSelect={setSelectedId} assignments={classifyMaintenanceAssignments(selectedAssignments, today)} memberLabel={memberName} memberContextLabel={memberContext} gardenLabel={gardenNumbers} formatDate={formatDate} endDateForAssignment={(id) => endDates[id] || today} onEndDateChange={(id, value) => setEndDates({ ...endDates, [id]: value })} canManage={canManage} onEdit={startEdit} onEnd={endAssignment} onCreate={startNew} message={message} assignmentPanel={canManage && selected?.active ? assigningMembers ? <AssignMembersToContract session={session} contract={selected} onCancel={() => setAssigningMembers(false)} onSaved={async (result) => { setAssigningMembers(false); await load(); const statuses = [...result.failures.map((failure) => `${memberName(failure.targetId)}: ${failure.message}`), ...result.documentWorkflows.map((workflow) => { const member = result.created.find((assignment) => assignment.id === workflow.wartungsvertrag_zuordnung_id); return `${member ? memberName(member.principalMemberId) : "Mitglied"}: ${workflow.fehlermeldung ?? `Dokumentworkflow ${workflow.status}`}`; }), ...result.documentFailures.map((failure) => `Zuordnung gespeichert, das Wartungsvertragsdokument konnte jedoch nicht erstellt werden: ${failure.message}`)].join(" "); setMessage(`${result.created.length} ${result.created.length === 1 ? "Mitglied wurde" : "Mitglieder wurden"} zugeordnet.${statuses ? ` ${statuses}` : ""}`); }} /> : <div className="maintenance-assignment-form"><button onClick={() => setAssigningMembers(true)}>Mitglieder zuweisen</button></div> : undefined} /></>;
}

type PermissionLevel = "none" | "read" | "write";

function permissionLevel(mask: number, area: (typeof PermissionAreas)[number]): PermissionLevel {
  if ((mask & area.write) === area.write) return "write";
  if ((mask & area.read) === area.read) return "read";
  return "none";
}

function UserRightsAdministration({ session, fixedMemberId }: { session: BrowserSession; fixedMemberId?: number }) {
  const club = useSelectedClub();
  const [members, setMembers] = useState<Member[]>([]);
  const [users, setUsers] = useState<AppUserAdmin[]>([]);
  const [selectedMemberId, setSelectedMemberId] = useState<number | null>(fixedMemberId ?? null);
  const [role, setRole] = useState<AppUserAdmin["role"]>("user");
  const [levels, setLevels] = useState<Record<string, PermissionLevel>>({});
  const [query, setQuery] = useState("");
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const loadAdminData = useCallback(async () => {
    return Promise.all([
      readSupabaseRest<Member>({ club, session }, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,auth_user_id", order: "name.asc", limit: "3000" }),
      readSupabaseRest<AppUserAdmin>({ club, session }, "app_user", { select: "user_id,mitglied_id,role,permission_grants,permission_revocations,is_protected,updated_at", order: "mitglied_id.asc", limit: "3000" }),
    ]);
  }, [club, session]);
  const initializeSelectedUser = useCallback((user: AppUserAdmin | undefined) => {
    const nextRole = user?.role ?? "user";
    setRole(nextRole);
    const effective = getEffectivePermissions(nextRole, user?.permission_grants ?? 0, user?.permission_revocations ?? 0);
    setLevels(Object.fromEntries(PermissionAreas.map((area) => [area.key, permissionLevel(effective, area)])));
    setMessage("");
  }, []);
  const applyLoadedAdminData = useCallback((loadedMembers: Member[], loadedUsers: AppUserAdmin[], selectedId: number | null) => {
    setMembers(loadedMembers);
    setUsers(loadedUsers);
    setSelectedMemberId(selectedId);
    initializeSelectedUser(loadedUsers.find((item) => item.mitglied_id === selectedId));
  }, [initializeSelectedUser]);
  async function load() {
    try {
      const [loadedMembers, loadedUsers] = await loadAdminData();
      const nextSelectedId = fixedMemberId ?? selectedMemberId ?? loadedMembers[0]?.id ?? null;
      applyLoadedAdminData(loadedMembers, loadedUsers, nextSelectedId);
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Benutzer konnten nicht geladen werden."); }
  }
  useEffect(() => {
    let active = true;
    loadAdminData().then(([loadedMembers, loadedUsers]) => {
      if (!active) return;
      const nextSelectedId = fixedMemberId ?? loadedMembers[0]?.id ?? null;
      applyLoadedAdminData(loadedMembers, loadedUsers, nextSelectedId);
    }).catch((cause: unknown) => {
      if (active) setMessage(cause instanceof Error ? cause.message : "Benutzer konnten nicht geladen werden.");
    });
    return () => { active = false; };
  }, [loadAdminData, applyLoadedAdminData, fixedMemberId]);
  const selectedMember = members.find((item) => item.id === selectedMemberId) ?? null;
  const selectedUser = users.find((item) => item.mitglied_id === selectedMemberId) ?? null;
  const editLock = useEditLock(club, session, "app_user", selectedUser?.user_id, Boolean(selectedUser));
  function selectMember(memberId: number) {
    setSelectedMemberId(memberId);
    initializeSelectedUser(users.find((item) => item.mitglied_id === memberId));
  }
  function changeRole(nextRole: AppUserAdmin["role"]) {
    setRole(nextRole);
    const base = getRolePermissions(nextRole);
    setLevels(Object.fromEntries(PermissionAreas.map((area) => [area.key, permissionLevel(base, area)])));
  }
  async function save() {
    if (!selectedUser) { setMessage("Für dieses Mitglied existiert noch kein verknüpfter App-Benutzer. Die Erstanmeldung muss zuerst abgeschlossen werden."); return; }
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    const base = getRolePermissions(role);
    let required = 0; let controllable = 0;
    for (const area of PermissionAreas) {
      controllable |= area.all;
      const level = levels[area.key] ?? "none";
      if (level === "read") required |= area.read;
      if (level === "write") required |= area.write;
    }
    const calculatedGrants = required & ~base;
    const calculatedRevocations = (base & controllable) & ~required;
    const grants = preserveUncontrolledPermissions(selectedUser.permission_grants, calculatedGrants);
    const revocations = preserveUncontrolledPermissions(selectedUser.permission_revocations, calculatedRevocations);
    setSaving(true); setMessage("");
    try {
      await callSupabaseRpc<void>({ club, session }, "update_app_user_access", { p_user_id: selectedUser.user_id, p_role: role, p_permission_grants: grants, p_permission_revocations: revocations });
      await load(); setMessage("Rolle und Fachrechte wurden gespeichert.");
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Rolle und Rechte konnten nicht gespeichert werden."); }
    finally { setSaving(false); }
  }
  async function invite() {
    if (!selectedMember?.email || !selectedMemberId) { setMessage("Für die Einladung fehlt eine E-Mail-Adresse."); return; }
    setSaving(true); setMessage("");
    try { const result = await inviteAppUser(club, session, selectedMemberId, role); await load(); setMessage(result.mailSent === false || result.outcome === "prepared_mail_failed" ? "Nutzerkonto wurde angelegt, die Willkommensmail konnte jedoch nicht versendet werden." : "Nutzerkonto wurde angelegt und die Willkommensmail versendet."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Einladung konnte nicht gesendet werden."); }
    finally { setSaving(false); }
  }
  async function resetPassword() {
    if (!selectedMember?.email) { setMessage("Für den Passwort-Reset fehlt eine E-Mail-Adresse."); return; }
    setSaving(true); setMessage("");
    try { const result = await sendPasswordReset(club, session, selectedMember.email); setMessage(result.message ?? "Passwort-Reset wurde versendet."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Passwort-Reset konnte nicht versendet werden."); }
    finally { setSaving(false); }
  }
  async function removeUser() {
    if (!selectedUser || !selectedMemberId || !window.confirm(`App-Benutzer von ${selectedMember?.vorname ?? ""} ${selectedMember?.name ?? ""} wirklich entfernen?`)) return;
    setSaving(true); setMessage("");
    try { await callSupabaseRpc<void>({ club, session }, "unlink_app_user", { p_user_id: selectedUser.user_id }); await load(); setMessage("Die App-Benutzerzuordnung wurde entfernt. Das Auth-Konto bleibt aus Sicherheitsgründen serverseitig bestehen."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "App-Benutzer konnte nicht entfernt werden."); }
    finally { setSaving(false); }
  }
  const visibleMembers = fixedMemberId ? members.filter((item) => item.id === fixedMemberId) : members.filter((item) => `${item.name ?? ""} ${item.vorname ?? ""} ${item.email ?? ""} ${item.id}`.toLowerCase().includes(query.toLowerCase()));
  return <section className="data-workspace admin-workspace" aria-label="Benutzer und Rechte">{message && <p className="notice" role="status">{message}</p>}<div className={fixedMemberId ? "" : "split-view"}>{!fixedMemberId && <div><div className="data-toolbar"><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Mitglied suchen" /><span>{users.length} verknüpfte Benutzer</span></div><div className="data-table-wrap"><table><thead><tr><th>Mitglied</th><th>Konto</th><th>Rolle</th></tr></thead><tbody>{visibleMembers.map((member) => { const user = users.find((item) => item.mitglied_id === member.id); return <tr key={member.id} className={selectedMemberId === member.id ? "selected-row" : ""} onClick={() => selectMember(member.id)}><td><strong>{member.name}, {member.vorname}</strong><small>Mgl.-Nr. {member.mitgliedsnummer ?? "–"} · {member.email || "keine E-Mail"}</small></td><td>{user ? "verknüpft" : "offen"}</td><td>{user?.role ?? "–"}</td></tr>; })}</tbody></table></div></div>}<aside className="detail-panel rights-editor">{selectedMember ? <><div className="detail-title"><div><h2>{selectedMember.name}, {selectedMember.vorname}</h2><p>{selectedUser ? `App-Benutzer ${selectedUser.user_id.slice(0, 8)}…` : "Noch kein App-Benutzer verknüpft"}</p></div></div><div className="account-actions">{!selectedUser && <button disabled={saving || !selectedMember.email} onClick={invite}>Nutzer hinzufügen</button>}{selectedUser && <button className="secondary-action" disabled={saving || !selectedMember.email} onClick={resetPassword}>Passwort-Reset senden</button>}{selectedUser && fixedMemberId && <button className="danger-action" disabled={saving} onClick={removeUser}>Nutzer entfernen</button>}</div>{!selectedUser && <p className="context-note">Mit „Nutzer hinzufügen“ wird der bestehende OTP-/Erstlogin-Ablauf gestartet. Rollen und Fachrechte werden nach erfolgreicher Verknüpfung speicherbar.</p>}<label>Rollenbasis<select value={role} disabled={!selectedUser || saving} onChange={(event) => changeRole(event.target.value as AppUserAdmin["role"])}><option value="user">Mitglied</option><option value="vorstand">Vorstand</option><option value="admin">Administrator</option></select></label><h3>Fachrechte</h3><div className="permission-grid">{PermissionAreas.map((area) => <label key={area.key}><span>{area.label}<small>Rollenstandard: {permissionLevel(getRolePermissions(role), area) === "write" ? "Bearbeiten" : permissionLevel(getRolePermissions(role), area) === "read" ? "Lesen" : "Aus"}</small></span><select value={levels[area.key] ?? "none"} disabled={!selectedUser || saving} onChange={(event) => setLevels({ ...levels, [area.key]: event.target.value as PermissionLevel })}><option value="none">Aus</option><option value="read">Lesen</option><option value="write">Bearbeiten</option></select></label>)}</div><div className="editor-actions"><button disabled={!selectedUser || saving} onClick={save}>{saving ? "Speichert …" : "Rolle und Fachrechte speichern"}</button><button className="secondary-action" disabled={!selectedUser || saving} onClick={() => changeRole(role)}>Auf Rollenstandard setzen</button></div></> : <><h2>Benutzer auswählen</h2><p>Wähle links ein Mitglied aus.</p></>}</aside></div></section>;
}
