"use client";

import { useEffect, useMemo, useState } from "react";
import {
  AppUserContext,
  BrowserSession,
  callSupabaseRpc,
  deleteSupabase,
  generateContract,
  inviteAppUser,
  readSupabase,
  sendPasswordReset,
  writeSupabase,
} from "../lib/supabase-auth";
import { type ClubContext } from "../models/auth/club";
import { memberDocumentOwner } from "../models/documents/document";
import { AuthProvider, useAuth } from "../features/auth/AuthProvider";
import { ChangeClubAction } from "../features/auth/ChangeClubAction";
import { ClubSelection } from "../features/auth/ClubSelection";
import { LoginForm } from "../features/auth/LoginForm";
import { DocumentList } from "../features/documents/DocumentList";
import type { DocumentAccessContext } from "../services/documents/document-access-service";
import { OtpFlow } from "../features/auth/OtpFlow";
import { ndefReaderConstructor } from "../lib/browser-media";
import { useEditLock } from "../lib/use-edit-lock";
import { MemberGardensWorkspace, ParcelProtocolsWorkspace, ParcelWorkspace } from "./parcel-workspaces";
import Navigation from "../features/navigation/Navigation";
import SeasonPicker from "../features/navigation/SeasonPicker";
import MobileNavigation from "../features/navigation/MobileNavigation";
import WorkspaceHeader from "../features/navigation/WorkspaceHeader";
import { buildNavigation } from "../services/workspace/navigation-service";
import { useWorkspaceContext, WorkspaceContextProvider } from "../contexts/WorkspaceContext";
import { listSeasons } from "../repositories/seasons/season-repository";
import { selectInitialSeasonFromList } from "../services/workspace/workspace-service";
import HomeDashboard from "../features/home/HomeDashboard";
import ImprintPage from "../features/imprint/ImprintPage";
import { MemberSearch } from "../features/members/MemberSearch";
import { MemberStammdaten } from "../features/members/MemberStammdaten";
import { type Member } from "../models/members/member";
import { loadMemberWorkspaceInfo } from "../services/members/member-service";
import { SignaturePad } from "../components/signature/SignaturePad";
import { MembershipApplicationFlow } from "../features/contracts/MembershipApplicationFlow";
import { LeaseContractFlow } from "../features/contracts/LeaseContractFlow";
import { loadMeterOverview, loadMeterReferenceData, meterDueStatusLabel } from "../services/meters/meter-service";
import { createMeterInstallationForRfid, installationContextFromResolution, type MeterInstallationContext } from "../services/meters/meter-installation-service";
import { completeMeterRemoval } from "../services/meters/meter-removal-service";
import { type Meter } from "../repositories/meters/meter-repository";
import { assignRfid, checkRfidAssignment, listAvailableRfidMediumOptions, listRfidSetupParcels, resolveRfidScanContext, type RfidAssignmentCheck, type RfidMediumOption, type RfidScanResolution } from "../services/rfid/rfid-service";
import { getMeterInstallationPhotoRequired, loadReadingCaptureContext, saveMeterInstallationReading, saveMeterRemovalReading, saveMeterReading, type ReadingCaptureContext, type ReadingPermissions } from "../services/readings/reading-service";
import { handleReadingPhoto, listPendingReadingPhotos, openStoredReadingPhoto, retryAllPendingReadingPhotos, retryPendingReadingPhoto, type PendingMeterPhoto } from "../services/readings/reading-photo-service";
import { loadReadingReviewData, reviewMeterReading, type ReadingReviewData, type ReadingReviewItem } from "../services/readings/reading-review-service";
import { MemberWorkHours } from "../features/work-hours/MemberWorkHours";
import { WorkHoursReview } from "../features/work-hours/WorkHoursReview";
import { WorkAssignmentsManagement } from "../features/work-assignments/WorkAssignmentsManagement";
import { AppointmentManagement } from "../features/appointments/AppointmentManagement";
import { AnnouncementManagement } from "../features/announcements/AnnouncementManagement";
import { type MaintenanceContract, type MaintenanceContractDraft } from "../models/maintenance/maintenance-contract";
import { type MaintenanceAssignment } from "../models/maintenance/maintenance-assignment";
import { loadMaintenanceContracts, normalizeAndValidateMaintenanceContract, saveMaintenanceContract } from "../services/maintenance/maintenance-contract-service";
import { createMaintenanceAssignmentForContract, endMaintenanceAssignmentForContract, loadMaintenanceAssignments } from "../services/maintenance/maintenance-assignment-service";

export default function Home() {
  return <AuthProvider><HomeContent /></AuthProvider>;
}
function HomeContent() {
  const { status, session, appUserContext, club, message, selectClub, login, logout, changeClub } = useAuth();

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
            <LoginForm onSignIn={login} disabled={status === "checking"} message={message} />
            <OtpFlow club={club!} disabled={status === "checking"} />
          </>
        )}
        <p className="fine-print">Der Zugriff wird nach der Anmeldung anhand deiner hinterlegten Vereinsrolle geprüft.</p>
        <ChangeClubAction className="change-club" label="Anderen Verein auswählen" onConfirm={changeClub} />
      </section>
    </main>
  );
}
// Navigation types moved to features/navigation/Navigation.tsx
type Season = { id: number; jahr: number };
type SeasonAdmin = Season & { pflichtstunden_soll: number; euro_pro_fehlstunde: number; bemerkung: string | null; pacht_pro_qm: number | null; mitgliedsbeitrag: number | null; mitgliedsbeitrag_nebenmitglied: number | null; aufnahmegebuehr: number | null; gebuehr_bauantrag: number | null };
type Parcel = { id: number; garten_nr: string; Anlage: string; flaeche_qm: number | null; hat_strom: boolean; hat_wasser: boolean; aktiv: boolean };
type ParcelAssignment = { id: number; parzelle_id: number; mitglied_id: number; von_datum: string | null; bis_datum: string | null };
type Reading = { id: number; zaehler_id: number; stand: number; ablesedatum: string; art: string; freigegeben: boolean; pruefstatus: string; pruefkommentar: string | null; geprueft_von: number | null; geprueft_am: string | null; foto_pfad?: string | null; foto_drive_file_id?: string | null; foto_dateiname?: string | null };
type AppUserAdmin = { user_id: string; mitglied_id: number | null; role: "admin" | "vorstand" | "user"; permission_grants: number; permission_revocations: number; updated_at: string | null };
type ClubConfigurationRecord = { id: number; vereinsname: string | null; kurzname: string | null; registerangabe: string | null; strasse: string | null; plz: string | null; ort: string | null; standard_email: string | null; standard_telefon: string | null; website: string | null; kontoinhaber: string | null; bankname: string | null; iban: string | null; bic: string | null; verwendungszweck_mitgliedsantrag: string | null; verwendungszweck_pachtvertrag: string | null; dokument_ort: string | null; standard_hinweistext: string | null; datenschutz_text: string | null; datenschutz_version: string | null; datenschutz_stand: string | null; aktiv: boolean };
type ExportDefinition = { export_key: string; titel: string | null; beschreibung: string | null; quelle_typ: string | null; quelle_name: string | null; erlaubt_csv: boolean; erlaubt_pdf: boolean };
type ExportFilterDefinition = { export_key: string; filter_key: string; label: string | null; typ: string | null; optionen_json: unknown; pflicht: boolean; sortierung: number };
type ExportColumnDefinition = { export_key: string; column_key: string; label_kurz: string | null; label_lang: string | null; sortierung: number; standard_sichtbar: boolean };

const Permission = {
  searchMembers: 1 << 0,
  viewMembers: 1 << 1,
  editAllMembers: 1 << 2,
  seeOwnData: 1 << 3,
  manageDocuments: 1 << 4,
  readMeters: 1 << 5,
  manageMeterChanges: 1 << 6,
  approveMeterReadings: 1 << 7,
  manageWorkHours: 1 << 8,
  manageRoles: 1 << 9,
  showStammdaten: 1 << 10,
  readStammdaten: 1 << 11,
  writeStammdaten: 1 << 12,
  showParzellen: 1 << 13,
  readParzellen: 1 << 14,
  writeParzellen: 1 << 15,
  readDocuments: 1 << 16,
  readWorkHours: 1 << 17,
  readRoles: 1 << 18,
  createMember: 1 << 19,
  manageWorkAssignments: 1 << 20,
  manageAppointments: 1 << 21,
  manageAnnouncements: 1 << 22,
} as const;

const VorstandPermissions = Permission.searchMembers | Permission.viewMembers | Permission.editAllMembers | Permission.manageDocuments | Permission.readMeters | Permission.manageMeterChanges | Permission.approveMeterReadings | Permission.manageWorkHours | Permission.showStammdaten | Permission.readStammdaten | Permission.writeStammdaten | Permission.readParzellen | Permission.writeParzellen | Permission.readDocuments | Permission.readWorkHours | Permission.readRoles | Permission.manageWorkAssignments | Permission.manageAppointments | Permission.manageAnnouncements;
const AdminPermissions = VorstandPermissions | Permission.manageRoles | Permission.createMember;
const UserPermissions = Permission.viewMembers | Permission.seeOwnData;

function permissionsFor(context: AppUserContext) {
  const base = context.role === "admin"
    ? AdminPermissions
    : context.role === "vorstand"
      ? VorstandPermissions
      : UserPermissions;
  return (base | context.permissionGrants) & ~context.permissionRevocations;
}

function Workspace(props: { session: BrowserSession; email: string; club: ClubContext; context: AppUserContext; onLogout: () => void; onChangeClub: () => Promise<void> }) {
  return <WorkspaceContextProvider><WorkspaceContent {...props} /></WorkspaceContextProvider>;
}

function WorkspaceContent({ session, email, club, context, onLogout, onChangeClub }: { session: BrowserSession; email: string; club: ClubContext; context: AppUserContext; onLogout: () => void; onChangeClub: () => Promise<void> }) {
  const [seasons, setSeasons] = useState<Season[]>([]);
  const [activeId, setActiveId] = useState("start");
  const [leaseContractIntent, setLeaseContractIntent] = useState<{ parcelId: number; startDate: string } | null>(null);
  const [seasonError, setSeasonError] = useState("");
  const { workspaceContext, selectedMember, creatingMember, setSelectedMember, setCreatingMember, updateWorkspaceContext, selectMember, selectParcel, selectSeason: selectWorkspaceSeason } = useWorkspaceContext();
  const permissions = useMemo(() => permissionsFor(context), [context]);
  const has = (permission: number) => (permissions & permission) === permission;
  const documentAccessContext: DocumentAccessContext = {
    role: context.role,
    memberId: context.mitgliedId,
    canReadDocuments: has(Permission.readDocuments),
    canManageDocuments: has(Permission.manageDocuments),
    canSeeOwnDataOnly: has(Permission.seeOwnData),
  };
  useEffect(() => { if (activeId === "mitglied-dokumente" && leaseContractIntent) queueMicrotask(() => setLeaseContractIntent(null)); }, [activeId, leaseContractIntent]);
  const canReadStammdaten = has(Permission.showStammdaten) || has(Permission.readStammdaten) || has(Permission.writeStammdaten);
  const ownContext = context.mitgliedId !== null && has(Permission.seeOwnData);
  const selectedMemberId = workspaceContext.mitgliedId ?? (ownContext ? context.mitgliedId : null);
  const selectedMemberLabel = selectedMember
    ? [selectedMember.vorname, selectedMember.name].filter(Boolean).join(" ") || `Mitglied #${selectedMember.id}`
    : selectedMemberId ? `Mitglied #${selectedMemberId}` : "nicht gewählt";
  const selectedParcelId = workspaceContext.parzelleId;
  const selectedSeason = seasons.find((item) => item.id === workspaceContext.saisonId) ?? null;
  const season = selectedSeason?.jahr ?? workspaceContext.saisonJahr ?? new Date().getFullYear();

  useEffect(() => {
    let active = true;
    listSeasons(session)
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
  }, [session, updateWorkspaceContext]);

  useEffect(() => {
    let active = true;
    if (selectedMemberId === null) {
      setSelectedMember(null);
      return () => { active = false; };
    }
    setSelectedMember(null);
    loadMemberWorkspaceInfo(session, selectedMemberId).then((member) => {
      if (active) setSelectedMember(member);
    }).catch(() => {
      if (active) setSelectedMember(null);
    });
    return () => { active = false; };
  }, [session, selectedMemberId, setSelectedMember]);

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
    canAccessParcels: has(Permission.searchMembers) && has(Permission.readParzellen),
    canManageMaintenance: has(Permission.editAllMembers),
    canApproveWorkHours: has(Permission.manageWorkHours),
    canExport: has(Permission.searchMembers),
    canManageAdministration: context.role === "admin",
    canSearchMembers: has(Permission.searchMembers),
  });
  const allNavigationItems = navigationGroups.flatMap((group) => [...(group.target ? [group.target] : []), ...group.items]);
  const active = allNavigationItems.find((item) => item.id === activeId) ?? allNavigationItems[0];

  return (
    <main className="workspace">
      <WorkspaceHeader clubName={club.kurzname || club.vereinsname} email={email} role={context.role} onLogout={onLogout} onChangeClub={onChangeClub} />
      <section className="workspace-body">
        <aside className="sidebar" aria-label="Hauptnavigation">
          <SeasonPicker seasons={seasons} selectedSeasonId={workspaceContext.saisonId} seasonError={seasonError} onChange={selectSeason} />
          <Navigation groups={navigationGroups} activeId={activeId} onNavigate={setActiveId} memberLabel={creatingMember ? "Neues Mitglied" : selectedMemberId ? selectedMemberLabel : null} />
        </aside>
        <article className="content-area">
          <MobileNavigation groups={navigationGroups} activeId={activeId} onNavigate={setActiveId} selectedMemberLabel={selectedMemberId ? selectedMemberLabel : null} />
          <p className="eyebrow">Saison {season}</p><h1>{active.label}</h1><p className="content-intro">{active.detail}</p>
          {!new Set(["start", "impressum", "mitglieder", "parzellen", "ablesen", "foto-uploads", "zaehlerwechsel", "arbeitsstunden-pruefen", "arbeitseinsaetze", "wartung", "termine", "bekanntmachungen", "export", "benutzer", "saisons", "verein", "mitglied-arbeitsstunden", "mitglied-wartung", "mitglied-dokumente", "mitglied-admin", "mitglied-gaerten", "mitglied-protokolle", "mitglied-stammdaten"]).has(activeId) && <section className="coming-soon"><span aria-hidden="true">◌</span><div><strong>Bereich vorbereitet</strong><p>Die Navigation und Zugriffsrechte stehen. Die fachliche Oberfläche wird in den nächsten Umsetzungsschritten ergänzt.</p></div></section>}
          {activeId === "start" && <HomeDashboard session={session} isManager={context.role !== "user"} canManageWorkAssignments={has(Permission.manageWorkAssignments)} canManageAppointments={has(Permission.manageAppointments)} canManageAnnouncements={has(Permission.manageAnnouncements)} memberId={context.mitgliedId} saisonId={workspaceContext.saisonId} season={season} onNavigate={setActiveId} onOpenWorkHours={openOwnWorkHours} />}
          {activeId === "mitglieder" && <MemberSearch session={session} selectedMemberId={selectedMemberId} onSelect={selectMember} canCreate={has(Permission.createMember)} onCreate={() => { setCreatingMember(true); setActiveId("mitglied-stammdaten"); }} />}
          {activeId === "parzellen" && <ParcelWorkspace session={session} selectedParcelId={selectedParcelId} onSelect={selectParcel} canEdit={has(Permission.writeParzellen)} documentAccessContext={documentAccessContext} onOpenMember={(memberId) => { selectMember(memberId); setActiveId("mitglied-gaerten"); }} />}
          {activeId === "ablesen" && <MeterOverview session={session} clubId={club.vereinId} reviewerMemberId={context.mitgliedId} selectedParcelId={selectedParcelId} seasonYear={season} canReadMeters={has(Permission.readMeters)} canSubmitOwnMeterReadings={context.mitgliedId !== null && has(Permission.seeOwnData)} canApprove={has(Permission.approveMeterReadings)} canManageMeterChanges={has(Permission.manageMeterChanges)} onNavigate={setActiveId} />}
          {activeId === "ablesen" && has(Permission.manageMeterChanges) && <RfidAssignmentPanel session={session} selectedParcelId={selectedParcelId} />}
          {activeId === "foto-uploads" && <PendingPhotoUploads session={session} clubId={club.vereinId} />}
          {activeId === "zaehlerwechsel" && has(Permission.manageMeterChanges) && <MeterChange session={session} clubId={club.vereinId} canManageMeterChanges={has(Permission.manageMeterChanges)} />}
          {activeId === "arbeitsstunden-pruefen" && <WorkHoursReview session={session} canManageWorkHours={has(Permission.manageWorkHours)} />}
          {activeId === "arbeitseinsaetze" && <WorkAssignmentsManagement session={session} canEdit={has(Permission.manageWorkAssignments)} canManageWorkHours={has(Permission.manageWorkHours)} onBack={() => setActiveId("start")} />}
          {activeId === "wartung" && <MaintenanceContracts session={session} canManage={context.role !== "user"} />}
          {activeId === "termine" && <AppointmentManagement session={session} canEdit={has(Permission.manageAppointments)} onBack={() => setActiveId("start")} />}
          {activeId === "bekanntmachungen" && <AnnouncementManagement session={session} canEdit={has(Permission.manageAnnouncements)} onBack={() => setActiveId("start")} />}
          {activeId === "impressum" && <ImprintPage session={session} />}
          {activeId === "export" && <ExportCenter session={session} canExport={context.role !== "user"} />}
          {activeId === "benutzer" && <UserRightsAdministration session={session} />}
          {activeId === "saisons" && <SeasonAdministration session={session} onSeasonSaved={(saved) => { setSeasons((current) => [...current.filter((item) => item.id !== saved.id), saved].sort((a, b) => b.jahr - a.jahr)); selectWorkspaceSeason(saved); }} />}
          {activeId === "verein" && <ClubConfigurationAdministration session={session} />}
          {activeId === "mitglied-arbeitsstunden" && selectedMemberId && <MemberWorkHours session={session} memberId={selectedMemberId} saisonId={workspaceContext.saisonId} canEditOwn={selectedMemberId === context.mitgliedId} canManageWorkHours={has(Permission.manageWorkHours)} />}
          {activeId === "mitglied-wartung" && selectedMemberId && <MaintenanceContracts session={session} memberId={selectedMemberId} canManage={context.role !== "user"} />}
          {activeId === "mitglied-dokumente" && selectedMemberId && <DocumentList session={session} owner={memberDocumentOwner(selectedMemberId)} accessContext={documentAccessContext} beforeList={(reload) => has(Permission.manageDocuments) ? <ContractComposer session={session} memberId={selectedMemberId} onSaved={reload} leaseContractIntent={leaseContractIntent} /> : null} />}
          {activeId === "mitglied-admin" && selectedMemberId && <UserRightsAdministration session={session} fixedMemberId={selectedMemberId} />}
          {activeId === "mitglied-gaerten" && selectedMemberId && <MemberGardensWorkspace session={session} memberId={selectedMemberId} selectedParcelId={selectedParcelId} onSelect={selectParcel} canAssign={has(Permission.createMember)} onOpenDocuments={() => setActiveId("mitglied-dokumente")} onOpenMeters={() => setActiveId("ablesen")} onOpenLeaseContract={(intent) => { if (!has(Permission.manageDocuments)) return false; setLeaseContractIntent(intent); setActiveId("mitglied-dokumente"); return true; }} />}
          {activeId === "mitglied-protokolle" && selectedMemberId && <ParcelProtocolsWorkspace session={session} memberId={selectedMemberId} currentBoardMemberId={context.mitgliedId} />}
          {activeId === "mitglied-stammdaten" && (selectedMemberId || creatingMember) && <MemberStammdaten session={session} memberId={creatingMember ? null : selectedMemberId} canEdit={creatingMember || has(Permission.editAllMembers) || (selectedMemberId === context.mitgliedId && has(Permission.seeOwnData))} canCreate={creatingMember} canManageSecondary={has(Permission.editAllMembers)} editPermissions={{ actorMemberId: context.mitgliedId, canEditOwnMember: has(Permission.seeOwnData), canEditAllMembers: has(Permission.editAllMembers) }} createPermissions={{ canCreateMember: has(Permission.createMember) }} onSaved={(member) => { setSelectedMember({ id: member.id, vorname: member.vorname, name: member.name }); selectMember(member.id); setActiveId("mitglied-stammdaten"); }} onCancel={() => { setCreatingMember(false); setActiveId("mitglieder"); }} />}
        </article>
      </section>
    </main>
  );
}

function MeterOverview({ session, clubId, reviewerMemberId, selectedParcelId, seasonYear, canReadMeters, canSubmitOwnMeterReadings, canApprove, canManageMeterChanges, onNavigate: navigate }: { session: BrowserSession; clubId: string; reviewerMemberId: number | null; selectedParcelId: number | null; seasonYear: number; canReadMeters: boolean; canSubmitOwnMeterReadings: boolean; canApprove: boolean; canManageMeterChanges: boolean; onNavigate: (id: string) => void }) {
  const [meters, setMeters] = useState<Meter[]>([]);
  const [parcels, setParcels] = useState<Parcel[]>([]);
  const [readings, setReadings] = useState<Reading[]>([]);
  const [dueStatuses, setDueStatuses] = useState<Awaited<ReturnType<typeof loadMeterOverview>>["dueStatuses"]>([]);
  const [reviewData, setReviewData] = useState<ReadingReviewData>({ open: [], reviewed: [], historyByReadingId: new Map() });
  const [activeTab, setActiveTab] = useState<"meters" | "due" | "open" | "reviewed" | "entry">(canReadMeters ? "meters" : canApprove ? "open" : "entry");
  const [selectedReadingId, setSelectedReadingId] = useState<number | null>(null);
  const [wifiOnly, setWifiOnly] = useState(() => typeof window !== "undefined" && window.localStorage.getItem("kgv-meter-photo-wifi-only") === "true");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const onNavigate = (id: string) => { if (id !== "zaehlerwechsel" || canManageMeterChanges) navigate(id); };
  function changeWifiOnly(value: boolean) { setWifiOnly(value); window.localStorage.setItem("kgv-meter-photo-wifi-only", String(value)); }
  async function load() {
    setLoading(true); setError("");
    try {
      const [meterOverview, loadedReadings, nextReviewData] = await Promise.all([
        canReadMeters ? loadMeterOverview(session) : canApprove ? loadMeterReferenceData(session).then((data) => ({ ...data, dueStatuses: [] })) : Promise.resolve({ meters: [], parcels: [], dueStatuses: [] }),
        canReadMeters ? readSupabase<Reading>(session, "zaehler_ablesung", { select: "id,zaehler_id,stand,ablesedatum,art,freigegeben,pruefstatus,pruefkommentar,geprueft_von,geprueft_am,foto_pfad,foto_drive_file_id,foto_dateiname", order: "ablesedatum.desc", limit: "1000" }) : Promise.resolve([]),
        canApprove ? loadReadingReviewData(session, { canApproveMeterReadings: canApprove, reviewerMemberId }) : Promise.resolve({ open: [], reviewed: [], historyByReadingId: new Map() }),
      ]);
      setMeters(meterOverview.meters); setParcels(meterOverview.parcels); setDueStatuses(meterOverview.dueStatuses); setReadings(loadedReadings); setReviewData(nextReviewData);
    } catch (cause) { setError(cause instanceof Error ? cause.message : "Ablesungen konnten nicht geladen werden."); } finally { setLoading(false); }
  }
  useEffect(() => { void load(); }, [session, canReadMeters, canApprove, reviewerMemberId]);
  useEffect(() => {
    const sync = () => { void retryAllPendingReadingPhotos(session, clubId, wifiOnly); };
    sync(); window.addEventListener("online", sync); return () => window.removeEventListener("online", sync);
  }, [session, clubId, wifiOnly]);

  const parcelName = (parcelId: number) => { const parcel = parcels.find((item) => item.id === parcelId); return parcel ? `${parcel.garten_nr} – ${parcel.Anlage}` : "Unbekannte Parzelle"; };
  const meterFor = (meterId: number) => meters.find((item) => item.id === meterId);
  const scopedMeters = selectedParcelId ? meters.filter((item) => item.parzelle_id === selectedParcelId) : meters;
  const openReadings = reviewData.open.filter((item) => !selectedParcelId || item.parcelId === selectedParcelId);
  const reviewedReadings = reviewData.reviewed.filter((item) => !selectedParcelId || item.parcelId === selectedParcelId);
  const dueMeters = dueStatuses.filter((item) => !selectedParcelId || item.parzelle_id === selectedParcelId);
  const selectedReading = openReadings.find((item) => item.readingId === selectedReadingId) ?? null;
  const selectedHistory = selectedReading ? reviewData.historyByReadingId.get(selectedReading.readingId) ?? [] : [];
  const scopedReadings = readings.filter((item) => !selectedParcelId || meterFor(item.zaehler_id)?.parzelle_id === selectedParcelId);

  return <section className="data-workspace" aria-label="Zähler und Ablesungen">
    <section className="meter-overview-actions"><div><strong>Ablesen</strong><p>Erfassung, Jahresendablesung, Eichfristen und Prüfung in einem PC-Arbeitsbereich.</p></div><label className="wifi-setting"><input type="checkbox" checked={wifiOnly} onChange={(event) => changeWifiOnly(event.target.checked)} /> Fotos nur bei erkanntem WLAN direkt hochladen</label><div><button className="secondary-action" onClick={() => onNavigate("foto-uploads")}>Foto-Uploads</button><button className="secondary-action" onClick={() => onNavigate("zaehlerwechsel")}>Zählerwechsel</button></div></section>
    <MeterReadingEntry session={session} clubId={clubId} seasonYear={seasonYear} permissions={{ canReadMeters, canSubmitOwnMeterReadings, memberId: reviewerMemberId }} wifiOnly={wifiOnly} onSaved={load} />
    {selectedParcelId && <p className="context-note">Parzellenfilter aktiv: #{selectedParcelId}</p>}
    {canReadMeters && <div className="meter-summary"><div><span>Aktive Zähler</span><strong>{scopedMeters.filter((item) => !item.ausgebaut_am).length}</strong></div><div><span>Offene Prüfungen</span><strong>{openReadings.length}</strong></div><div><span>Aktive Eichstatus</span><strong>{dueMeters.length}</strong></div></div>}
    <div className="tabs" role="tablist">{canReadMeters && <><button className={activeTab === "meters" ? "tab active" : "tab"} onClick={() => setActiveTab("meters")}>Zähler und Historie</button><button className={activeTab === "due" ? "tab active" : "tab"} onClick={() => setActiveTab("due")}>Eichfällige Zähler <span>{dueMeters.length}</span></button></>}{canApprove && <><button className={activeTab === "open" ? "tab active" : "tab"} onClick={() => setActiveTab("open")}>Eingereichte Ablesungen <span>{openReadings.length}</span></button><button className={activeTab === "reviewed" ? "tab active" : "tab"} onClick={() => setActiveTab("reviewed")}>Prüfverlauf</button></>}</div>
    {loading ? <p>Lädt …</p> : error ? <p className="notice" role="alert">{error}</p> : canReadMeters && activeTab === "meters" ? <>
      <div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Medium</th><th>Zähler</th><th>Einbau</th><th>Eichfällig</th><th>Status</th></tr></thead><tbody>{scopedMeters.map((meter) => <tr key={meter.id}><td>{parcelName(meter.parzelle_id)}</td><td>{meter.medium}</td><td><strong>{meter.zaehlernummer}</strong></td><td>{formatDate(meter.eingebaut_am)}</td><td>{formatDate(meter.eichfaellig_am)}</td><td>{meter.ausgebaut_am ? "ausgebaut" : meter.status ?? "aktiv"}</td></tr>)}</tbody></table></div>
      <section className="detail-panel reading-history"><h2>Ablesehistorie</h2><div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Zähler</th><th>Art</th><th>Datum</th><th>Stand</th><th>Status</th><th>Foto</th></tr></thead><tbody>{scopedReadings.map((reading) => { const meter = meterFor(reading.zaehler_id); return <tr key={reading.id}><td>{meter ? parcelName(meter.parzelle_id) : "–"}</td><td>{meter?.zaehlernummer ?? "–"}</td><td>{reading.art.toUpperCase()}</td><td>{formatDate(reading.ablesedatum)}</td><td>{reading.stand}</td><td>{reading.freigegeben ? "freigegeben" : reading.pruefstatus}</td><td>{reading.foto_drive_file_id || reading.foto_pfad ? <PhotoOpenButton session={session} reading={reading} /> : "–"}</td></tr>; })}</tbody></table></div></section>
    </> : canReadMeters && activeTab === "due" ? <div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Medium</th><th>Zähler</th><th>Eichfällig</th><th>Tage</th><th>Status</th></tr></thead><tbody>{dueMeters.map((meter) => <tr key={meter.id}><td>{meter.garten_nr ? `${meter.garten_nr} – ${meter.anlage ?? "–"}` : parcelName(meter.parzelle_id)}</td><td>{meter.medium ?? "–"}</td><td><strong>{meter.zaehlernummer ?? "–"}</strong></td><td>{formatDate(meter.eichfaellig_am)}</td><td>{meter.tage_bis_faellig ?? "–"}</td><td>{meterDueStatusLabel(meter.eichstatus)}</td></tr>)}</tbody></table>{dueMeters.length === 0 && <p className="empty-state">Keine aktiven Zählerdaten aus dem zentralen Eichstatus vorhanden.</p>}</div> : canApprove && activeTab === "reviewed" ? <div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Mitglied</th><th>Zähler</th><th>Art</th><th>Datum</th><th>Stand</th><th>Entscheidung</th><th>Geprüft von</th><th>Geprüft am</th><th>Kommentar</th></tr></thead><tbody>{reviewedReadings.map((reading) => <tr key={reading.readingId}><td>{reading.gartenNr}{reading.anlage ? ` – ${reading.anlage}` : ""}</td><td>{reading.mitgliedName}</td><td>{reading.zaehlernummer}</td><td>{reading.art.toUpperCase()}</td><td>{formatDate(reading.ablesedatum)}</td><td>{reading.stand}</td><td>{reading.decisionLabel}</td><td>{reading.geprueftVonName}</td><td>{formatDate(reading.geprueftAm)}</td><td>{reading.pruefkommentar || "–"}</td></tr>)}</tbody></table>{reviewedReadings.length === 0 && <p className="empty-state">Noch kein Prüfverlauf vorhanden.</p>}</div> : canApprove && activeTab === "open" ? <div className="split-view review-workspace"><div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Mitglied</th><th>Medium / Zähler</th><th>Art</th><th>Datum</th><th>Stand</th><th>Foto</th></tr></thead><tbody>{openReadings.map((reading) => <tr key={reading.readingId} className={selectedReading?.readingId === reading.readingId ? "selected-row" : ""} onClick={() => setSelectedReadingId(reading.readingId)}><td>{reading.gartenNr}{reading.anlage ? ` – ${reading.anlage}` : ""}</td><td>{reading.mitgliedName}</td><td>{`${reading.medium} · ${reading.zaehlernummer}`}</td><td>{reading.art.toUpperCase()}</td><td>{formatDate(reading.ablesedatum)}</td><td>{reading.stand}</td><td>{reading.fotoDriveFileId || reading.fotoPfad ? "vorhanden" : "–"}</td></tr>)}</tbody></table>{openReadings.length === 0 && <p className="empty-state">Keine offenen Ablesungen zur Prüfung.</p>}</div><ReadingReview session={session} reading={selectedReading} history={selectedHistory} permissions={{ canApproveMeterReadings: canApprove, reviewerMemberId }} onSaved={async () => { setSelectedReadingId(null); await load(); }} /></div> : null}
  </section>;
}
function RfidAssignmentPanel({ session, selectedParcelId }: { session: BrowserSession; selectedParcelId: number | null }) {
  const [parcels, setParcels] = useState<Awaited<ReturnType<typeof listRfidSetupParcels>>>([]);
  const [parcelId, setParcelId] = useState(selectedParcelId ? String(selectedParcelId) : "");
  const [mediumOptions, setMediumOptions] = useState<RfidMediumOption[]>([]);
  const [medium, setMedium] = useState("");
  const [uid, setUid] = useState("");
  const [resolution, setResolution] = useState<RfidScanResolution | null>(null);
  const [check, setCheck] = useState<RfidAssignmentCheck | null>(null);
  const [message, setMessage] = useState("");
  const [busy, setBusy] = useState(false);
  useEffect(() => { listRfidSetupParcels(session).then(setParcels).catch((cause) => setMessage(cause instanceof Error ? cause.message : "Parzellen konnten nicht geladen werden.")); }, [session]);
  useEffect(() => { let active = true; const id = Number(parcelId); if (!id) return () => { active = false; }; listAvailableRfidMediumOptions(session, id).then((items) => { if (!active) return; setMediumOptions(items); setMedium((current) => items.some((item) => item.key === current) ? current : items[0]?.key ?? ""); }).catch((cause) => { if (active) setMessage(cause instanceof Error ? cause.message : "Medien konnten nicht geladen werden."); }); return () => { active = false; }; }, [session, parcelId]);
  async function resolve() { setBusy(true); setCheck(null); try { const result = await resolveRfidScanContext(session, uid); setResolution(result); setUid(result.normalizedUid); setMessage(result.message); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "RFID-Kontext konnte nicht geladen werden."); } finally { setBusy(false); } }
  async function verify() { setBusy(true); try { const result = await checkRfidAssignment(session, Number(parcelId), medium, uid); setCheck(result); setMessage(result.message); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "RFID-Zuordnung konnte nicht geprüft werden."); } finally { setBusy(false); } }
  async function save(overwriteExisting = false) { setBusy(true); try { const result = await assignRfid(session, Number(parcelId), medium, uid, overwriteExisting); if (result.requiresOverwriteConfirmation) { if (window.confirm(`${result.message}\n\nBestehende RFID-Zuordnung am Ziel wirklich überschreiben?`)) { await save(true); return; } setMessage("Überschreiben wurde abgelehnt."); return; } setMessage(result.message); if (result.success) { setCheck(null); setResolution(null); setUid(""); } } catch (cause) { setMessage(cause instanceof Error ? cause.message : "RFID konnte nicht gespeichert werden."); } finally { setBusy(false); } }
  const showAssignment = resolution?.state === "Unknown";
  return <section className="detail-panel member-editor" aria-label="RFID einrichten"><div className="detail-title"><div><h2>RFID einrichten</h2><p>RFID-UID prüfen und kontrolliert einer Parzelle zuordnen.</p></div></div>{message && <p className="notice" role="status">{message}</p>}<fieldset disabled={busy}><label className="wide">RFID-UID<input value={uid} onChange={(event) => { setUid(event.target.value); setResolution(null); setCheck(null); }} placeholder="RFID-UID eingeben" /></label></fieldset><div className="editor-actions"><button className="secondary-action" disabled={busy || !uid.trim()} onClick={() => void resolve()}>{busy ? "Prüft …" : "RFID-Kontext auflösen"}</button></div>{resolution && <section className="parcel-section"><h3>{resolution.state}</h3>{resolution.context ? <p><strong>{resolution.context.garten_nr ?? "–"} – {resolution.context.anlage ?? "–"}</strong><br />{resolution.context.medium ?? "–"} · {resolution.context.zaehlernummer ?? "Kein aktiver Zähler"}<br />Status: {resolution.context.status ?? "Kein aktiver Zähler"}</p> : <p>Die UID ist noch keiner Parzelle zugeordnet.</p>}</section>}{showAssignment && <section className="parcel-section"><h3>Neue RFID-Zuordnung</h3><fieldset disabled={busy}><label>Parzelle<select value={parcelId} onChange={(event) => { setParcelId(event.target.value); setCheck(null); }}><option value="">Parzelle wählen</option>{parcels.map((parcel) => <option key={parcel.id} value={parcel.id}>{parcel.garten_nr} – {parcel.Anlage}</option>)}</select></label><label>Medium<select value={medium} onChange={(event) => { setMedium(event.target.value); setCheck(null); }} disabled={!parcelId}><option value="">Medium wählen</option>{mediumOptions.map((item) => <option key={item.key} value={item.key}>{item.displayName}</option>)}</select></label></fieldset><div className="editor-actions"><button className="secondary-action" disabled={busy || !parcelId || !medium} onClick={() => void verify()}>Zuordnung prüfen</button><button disabled={busy || !check?.isValid} onClick={() => void save()}>{busy ? "Speichert …" : "RFID speichern"}</button></div>{check?.conflict && <p className="context-note">Konflikt: bereits bei {check.conflict.garten_nr ?? "–"} – {check.conflict.anlage ?? "–"} für {check.conflict.medium ?? "–"} hinterlegt. Diese Zuordnung wird nicht automatisch geändert.</p>}</section>}</section>;
}

function MeterChange({ session, clubId, canManageMeterChanges }: { session: BrowserSession; clubId: string; canManageMeterChanges: boolean }) {
  const [uid,setUid]=useState(""); const [resolution,setResolution]=useState<RfidScanResolution|null>(null); const [context,setContext]=useState<MeterInstallationContext|null>(null); const [installationDate,setInstallationDate]=useState(currentLocalDate); const [meterNumber,setMeterNumber]=useState(""); const [calibrationYear,setCalibrationYear]=useState(()=>String(new Date().getFullYear())); const [installedMeter,setInstalledMeter]=useState<Meter|null>(null); const [initialValue,setInitialValue]=useState(""); const [file,setFile]=useState<File|null>(null); const [installationPhotoRequired,setInstallationPhotoRequired]=useState(true); const [removedAt,setRemovedAt]=useState(currentLocalDate); const [endValue,setEndValue]=useState(""); const [progress,setProgress]=useState<{readingId:number;removedAt:string;endValue:number;photoHandled:boolean;photoMessage:string}|null>(null); const [message,setMessage]=useState(""); const [saving,setSaving]=useState(false);
  const wifiOnly=()=>typeof window!=="undefined"&&window.localStorage.getItem("kgv-meter-photo-wifi-only")==="true";
  function reset(){setResolution(null);setContext(null);setInstalledMeter(null);setInitialValue("");setFile(null);setMeterNumber("");setCalibrationYear(String(new Date().getFullYear()));setInstallationDate(currentLocalDate());setRemovedAt(currentLocalDate());setEndValue("");setProgress(null);}
  async function resolve(){setSaving(true);try{const next=await resolveRfidScanContext(session,uid);setResolution(next);setUid(next.normalizedUid);setContext(installationContextFromResolution(next));setInstallationPhotoRequired(await getMeterInstallationPhotoRequired(session));setMessage(next.message);}catch(e){setMessage(e instanceof Error?e.message:"RFID-Kontext konnte nicht geladen werden.");}finally{setSaving(false);}}
  async function install(){if(!context)return;setSaving(true);try{const m=await createMeterInstallationForRfid(session,{canManageMeterChanges},context,{meterNumber,installationDate,calibrationYear});setInstalledMeter(m);setMessage("Zähler angelegt. Jetzt folgt die Anfangsablesung.");}catch(e){setMessage(e instanceof Error?e.message:"Zähler konnte nicht angelegt werden.");}finally{setSaving(false);}}
  async function initial(){if(!installedMeter||!context||!initialValue.trim()||(installationPhotoRequired&&!file)){setMessage(installationPhotoRequired&&!file?"Für die Anfangsablesung ist ein Foto erforderlich.":"Bitte einen Anfangsstand eingeben.");return;}setSaving(true);try{const r=await saveMeterInstallationReading(session,{canManageMeterChanges},{meterId:installedMeter.id,date:installationDate,value:Number(initialValue.replace(",","."))});const p=await handleReadingPhoto(session,{clubId,readingId:r.id,file,details:{kind:"einbau",datum:installationDate,medium:installedMeter.medium,anlage:context.anlage,garten:context.gardenNr,zaehlernummer:installedMeter.zaehlernummer},wifiOnly:wifiOnly()});reset();setUid("");setMessage(`Einbau erfolgreich abgeschlossen. ${p.message} Bitte RFID erneut scannen.`);}catch(e){setMessage(e instanceof Error?e.message:"Einbauablesung konnte nicht gespeichert werden.");}finally{setSaving(false);}}
  async function remove(){if(!resolution?.context?.aktiver_zaehler_id)return;if(!progress&&!window.confirm("Wollen Sie den Zähler ausbauen?"))return;if(!progress&&!file){setMessage("Für die Schlussablesung ist ein Foto erforderlich.");return;}setSaving(true);try{let next=progress;if(!next){const r=await saveMeterRemovalReading(session,{canManageMeterChanges},{meterId:resolution.context.aktiver_zaehler_id,date:removedAt,value:Number(endValue.replace(",","."))});next={readingId:r.id,removedAt,endValue:Number(endValue.replace(",",".")),photoHandled:false,photoMessage:""};setProgress(next);}if(!next.photoHandled){const p=await handleReadingPhoto(session,{clubId,readingId:next.readingId,file,details:{kind:"ausbau",datum:next.removedAt,medium:resolution.context.medium??"",anlage:resolution.context.anlage??"",garten:resolution.context.garten_nr??"",zaehlernummer:resolution.context.zaehlernummer??""},wifiOnly:wifiOnly()});next={...next,photoHandled:true,photoMessage:p.message};setProgress(next);}await completeMeterRemoval(session,{canManageMeterChanges},{uid:resolution.normalizedUid,meterId:resolution.context.aktiver_zaehler_id,parcelId:resolution.context.parzelle_id,medium:resolution.context.medium??"",removedAt:next.removedAt});const text=next.photoMessage;reset();setUid("");setMessage(`Zähler ausgebaut. ${text} Bitte RFID erneut scannen.`);}catch(e){setMessage(e instanceof Error?e.message:"Zähler konnte nicht ausgebaut werden.");}finally{setSaving(false);}}
  const active=resolution?.state==="KnownWithActiveMeter"&&resolution.context; const locked=Boolean(installedMeter||progress);
  return <section className="data-workspace"><section className="detail-panel member-editor"><h2>Zählerwechsel</h2>{message&&<p className="notice">{message}</p>}<label>RFID-UID<input value={uid} disabled={locked} onChange={e=>{reset();setUid(e.target.value);}} /></label><button disabled={saving||locked||!uid.trim()} onClick={()=>void resolve()}>RFID-Kontext auflösen</button>{active?<><p>{active.garten_nr} – {active.anlage}<br />{active.medium} · {active.zaehlernummer}</p><fieldset disabled={saving||Boolean(progress)}><label>Ausbaudatum<input type="date" value={progress?.removedAt??removedAt} onChange={e=>setRemovedAt(e.target.value)} /></label><label>Endstand<input value={progress?String(progress.endValue):endValue} onChange={e=>setEndValue(e.target.value)} /></label><label>Foto *<input type="file" disabled={Boolean(progress?.photoHandled)} onChange={e=>setFile(e.target.files?.[0]??null)} /></label></fieldset><button disabled={saving||(!progress&&!endValue.trim())} onClick={()=>void remove()}>{progress?"Ausbau fortsetzen":"Zähler ausbauen"}</button></>:context&&!installedMeter?<><label>Einbaudatum<input type="date" value={installationDate} onChange={e=>setInstallationDate(e.target.value)} /></label><label>Zählernummer<input value={meterNumber} onChange={e=>setMeterNumber(e.target.value)} /></label><label>Eichjahr<input value={calibrationYear} onChange={e=>setCalibrationYear(e.target.value)} /></label><button onClick={()=>void install()}>Zähler anlegen</button></>:installedMeter?<><p>Zähler angelegt: {installedMeter.zaehlernummer}</p><label>Anfangsstand<input value={initialValue} onChange={e=>setInitialValue(e.target.value)} /></label><label>Foto{installationPhotoRequired?" *":""}<input type="file" onChange={e=>setFile(e.target.files?.[0]??null)} /></label><button onClick={()=>void initial()}>Anfangsablesung speichern</button></>:null}</section></section>;
}
function MeterReadingEntry({ session, clubId, seasonYear, permissions: { canReadMeters, canSubmitOwnMeterReadings, memberId }, wifiOnly, onSaved }: { session: BrowserSession; clubId: string; seasonYear: number; permissions: ReadingPermissions; wifiOnly: boolean; onSaved: () => void | Promise<void> }) {
  const [context, setContext] = useState<ReadingCaptureContext | null>(null); const [meterId, setMeterId] = useState(""); const [parcelId, setParcelId] = useState(""); const [medium, setMedium] = useState(""); const [kind, setKind] = useState<"normal" | "jea">("normal"); const [value, setValue] = useState(""); const [date, setDate] = useState(currentLocalDate); const [file, setFile] = useState<File | null>(null); const [previewUrl, setPreviewUrl] = useState(""); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const permissions = useMemo<ReadingPermissions>(() => ({ canReadMeters, canSubmitOwnMeterReadings, memberId }), [canReadMeters, canSubmitOwnMeterReadings, memberId]);
  useEffect(() => { let active = true; loadReadingCaptureContext(session, permissions).then((next) => { if (active) { setContext(next); setMessage(next.message ?? ""); } }).catch((cause) => { if (active) setMessage(cause instanceof Error ? cause.message : "Ablesekontext konnte nicht geladen werden."); }); return () => { active = false; }; }, [session, permissions]);
  useEffect(() => { if (!file) { setPreviewUrl(""); return; } const url = URL.createObjectURL(file); setPreviewUrl(url); return () => URL.revokeObjectURL(url); }, [file]);
  const meters = context?.meters ?? []; const parcels = context?.parcels ?? []; const selectedParcel = parcels.find((item) => item.id === Number(parcelId)); const filteredMeters = meters.filter((item) => (!parcelId || item.parzelle_id === Number(parcelId)) && (!medium || item.medium.toLocaleLowerCase("de") === medium));
  async function save() { const stand = Number(value.replace(",", ".")); if (context?.photoRequired && !file) { setMessage("Für diese Ablesung ist ein Foto erforderlich."); return; } setSaving(true); setMessage(""); try { const result = await saveMeterReading(session, permissions, { meterId: Number(meterId), date, value: stand, art: kind }); const meter = meters.find((item) => item.id === result.reading.zaehler_id); const parcel = meter && parcels.find((item) => item.id === meter.parzelle_id); const readingMessage = result.isSubmission ? "Ablesung wurde zur Prüfung eingereicht." : "Ablesung wurde direkt freigegeben gespeichert."; if (file && meter && parcel) { const photo = await handleReadingPhoto(session, { clubId, readingId: result.reading.id, file, details: { datum: date, medium: meter.medium, anlage: parcel.Anlage, garten: parcel.garten_nr, zaehlernummer: meter.zaehlernummer }, wifiOnly }); setMessage(`${readingMessage} ${photo.message}`); } else setMessage(readingMessage); setMeterId(""); setValue(""); setFile(null); await onSaved(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Ablesung konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  const enabled = Boolean(context && !context.message); const selectKind = (next: "normal" | "jea") => setKind(next);
  return <section className="detail-panel member-editor meter-entry"><div className="detail-title"><div><h2>{kind === "jea" ? "Jahresendablesung erfassen" : "Ablesung erfassen"}</h2><p>{context?.photoRequired ? "Foto ist erforderlich" : "Foto ist optional"} · {context?.isDirectCapture ? "wird direkt freigegeben" : "wird zur Prüfung eingereicht"}</p></div><div className="reading-kind"><button className={kind === "normal" ? "active" : "secondary-action"} onClick={() => selectKind("normal")}>Normal</button><button className={kind === "jea" ? "active" : "secondary-action"} onClick={() => selectKind("jea")}>JEA {seasonYear}</button></div></div>{message && <p className="notice" role="status">{message}</p>}{context?.isDirectCapture && <RfidBrowserScanner session={session} onResolved={(resolution) => { if (resolution.state === "KnownWithActiveMeter" && resolution.context?.aktiver_zaehler_id) { setParcelId(String(resolution.context.parzelle_id)); setMedium(String(resolution.context.medium ?? "").toLocaleLowerCase("de")); setMeterId(String(resolution.context.aktiver_zaehler_id)); setMessage(`RFID erkannt: Garten ${resolution.context.garten_nr ?? "–"}, ${resolution.context.medium ?? "–"}.`); } else setMessage(resolution.message); }} />}<fieldset disabled={!enabled || saving}><label>Parzelle<select value={parcelId} onChange={(event) => { setParcelId(event.target.value); setMedium(""); setMeterId(""); }}><option value="">Parzelle wählen</option>{parcels.map((parcel) => <option key={parcel.id} value={parcel.id}>{parcel.garten_nr} – {parcel.Anlage}</option>)}</select></label><label>Medium<select value={medium} onChange={(event) => { setMedium(event.target.value); setMeterId(""); }}><option value="">Medium wählen</option>{selectedParcel?.hat_strom && <option value="strom">Strom</option>}{selectedParcel?.hat_wasser && <option value="wasser">Wasser</option>}</select></label><label className="wide">Aktiver Zähler *<select value={meterId} onChange={(event) => setMeterId(event.target.value)}><option value="">Zähler wählen</option>{filteredMeters.map((item) => { const parcel = parcels.find((entry) => entry.id === item.parzelle_id); return <option key={item.id} value={item.id}>Garten {parcel?.garten_nr ?? "?"} · {item.medium} · {item.zaehlernummer}</option>; })}</select></label><label>Datum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Stand<input inputMode="decimal" value={value} onChange={(event) => setValue(event.target.value)} /></label><label className="wide">Foto aufnehmen oder auswählen<input type="file" accept="image/*" capture="environment" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /></label></fieldset>{previewUrl && <div className="photo-preview"><img src={previewUrl} alt="Vorschau des ausgewählten Ablesefotos" /><button className="secondary-action" onClick={() => setFile(null)}>Foto entfernen</button></div>}<div className="editor-actions"><button disabled={saving || !enabled} onClick={save}>{saving ? "Speichert …" : context?.isDirectCapture ? "Ablesung speichern" : "Ablesung einreichen"}</button></div></section>;
}

function RfidBrowserScanner({ session, onResolved }: { session: BrowserSession; onResolved: (resolution: RfidScanResolution) => void }) {
  const supported = typeof window !== "undefined" && Boolean(ndefReaderConstructor()) && window.isSecureContext;
  const [scanning, setScanning] = useState(false); const [message, setMessage] = useState(supported ? "Web-NFC ist auf diesem Gerät verfügbar." : "Dieser Browser bietet kein nutzbares Web-NFC. RFID bleibt auf diesem Gerät ein MAUI-Spezialweg; die Zählerauswahl funktioniert weiterhin manuell.");
  async function start() {
    const Reader = ndefReaderConstructor(); if (!Reader || !window.isSecureContext) return;
    setScanning(true); setMessage("RFID-Scan aktiv. Tag an das Gerät halten.");
    try {
      const reader = new Reader();
      reader.onreadingerror = () => { setMessage("RFID-Tag konnte nicht gelesen werden."); setScanning(false); };
      reader.onreading = async (event) => { const uid = (event.serialNumber ?? "").trim(); if (!uid) { setMessage("Der Browser hat keine RFID-UID geliefert. Bitte MAUI verwenden."); setScanning(false); return; } try { const resolution = await resolveRfidScanContext(session, uid); onResolved(resolution); setMessage(`RFID ${resolution.normalizedUid} wurde gelesen.`); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "RFID-Kontext konnte nicht geladen werden."); } finally { setScanning(false); } };
      await reader.scan();
    } catch (cause) { setMessage(cause instanceof Error ? `Web-NFC konnte nicht gestartet werden: ${cause.message}` : "Web-NFC konnte nicht gestartet werden."); setScanning(false); }
  }
  return <div className={supported ? "rfid-browser supported" : "rfid-browser"}><div><strong>{supported ? "RFID im Browser" : "RFID nicht verfügbar"}</strong><p>{message}</p></div>{supported && <button type="button" className="secondary-action" disabled={scanning} onClick={start}>{scanning ? "Scan läuft …" : "RFID scannen"}</button>}</div>;
}

function PhotoOpenButton({ session, reading }: { session: BrowserSession; reading: { id?: number; readingId?: number; foto_pfad?: string | null; foto_drive_file_id?: string | null; fotoPfad?: string | null; fotoDriveFileId?: string | null } }) {
  const [opening, setOpening] = useState(false); const [error, setError] = useState("");
  async function open() { setOpening(true); setError(""); try { const path = reading.foto_pfad ?? reading.fotoPfad; const driveFileId = reading.foto_drive_file_id ?? reading.fotoDriveFileId; if (!driveFileId && path && /^https?:\/\//i.test(path)) { window.open(path, "_blank", "noopener,noreferrer"); return; } const readingId = reading.id ?? reading.readingId; if (!readingId) throw new Error("Die Ablesung ist ungültig."); const url = await openStoredReadingPhoto(session, readingId); window.open(url, "_blank", "noopener,noreferrer"); window.setTimeout(() => URL.revokeObjectURL(url), 60_000); } catch (cause) { setError(cause instanceof Error ? cause.message : "Foto konnte nicht geöffnet werden."); } finally { setOpening(false); } }
  return <span className="photo-open"><button className="table-action" disabled={opening} onClick={open}>{opening ? "Öffnet …" : "Foto anzeigen"}</button>{error && <small>{error}</small>}</span>;
}

function PendingPhotoUploads({ session, clubId }: { session: BrowserSession; clubId: string }) {
  const [items, setItems] = useState<PendingMeterPhoto[]>([]); const [busy, setBusy] = useState(false); const [message, setMessage] = useState(""); const [wifiOnly] = useState(() => typeof window !== "undefined" && window.localStorage.getItem("kgv-meter-photo-wifi-only") === "true");
  async function load() { try { setItems(await listPendingReadingPhotos(clubId)); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Lokale Foto-Warteschlange konnte nicht geladen werden."); } }
  async function retryAll() { if (busy) return; setBusy(true); try { const result = await retryAllPendingReadingPhotos(session, clubId, wifiOnly); await load(); setMessage(result.uploaded || result.failed ? `${result.uploaded} Foto(s) hochgeladen, ${result.failed} fehlgeschlagen.` : result.queued ? "Das aktuelle Netz erlaubt keinen Upload. Fotos bleiben lokal." : "Keine offenen Foto-Uploads vorhanden."); } finally { setBusy(false); } }
  async function retryOne(item: PendingMeterPhoto) { if (busy) return; setBusy(true); try { const result = await retryPendingReadingPhoto(session, clubId, wifiOnly, item); await load(); setMessage(result.message); } finally { setBusy(false); } }
  useEffect(() => { void load(); const onOnline = () => { void retryAll(); }; window.addEventListener("online", onOnline); return () => window.removeEventListener("online", onOnline); }, [clubId, wifiOnly]);
  function preview(item: PendingMeterPhoto) { const url = URL.createObjectURL(item.content); window.open(url, "_blank", "noopener,noreferrer"); window.setTimeout(() => URL.revokeObjectURL(url), 60_000); }
  return <section className="data-workspace pending-photos"><div className="data-toolbar"><span>{items.length ? `${items.length} offene Foto-Uploads` : "Keine offenen Foto-Uploads"}</span><button disabled={!items.length || busy} onClick={() => void retryAll()}>{busy ? "Uploads laufen …" : "Alle erneut versuchen"}</button></div><p className="context-note">Fotos bleiben bis zum erfolgreichen Upload ausschließlich in diesem Browser und diesem Vereinskontext gespeichert.</p>{message && <p className="notice" role="status">{message}</p>}<div className="pending-photo-list">{items.map((item) => <article key={item.id}><LocalPhotoThumbnail item={item} /><div><strong>{item.fileName}</strong><span>Garten {item.details.garten} · {item.details.medium} · Ablesung #{item.readingId}</span><span>{new Intl.DateTimeFormat("de-DE", { dateStyle: "short", timeStyle: "short" }).format(new Date(item.createdAt))} · Versuch {item.attemptCount}</span><small>{item.status === "failed" ? `Fehlgeschlagen: ${item.lastError ?? "erneuter Versuch möglich"}` : item.status === "uploading" ? "Wird hochgeladen …" : "Lokal gespeichert, Upload ausstehend"}</small></div><div><button className="secondary-action" onClick={() => preview(item)}>Anzeigen</button><button disabled={busy} onClick={() => void retryOne(item)}>Erneut versuchen</button></div></article>)}{items.length === 0 && <p className="empty-state">Es sind keine lokalen Ablesefotos vorgemerkt.</p>}</div></section>;
}
function LocalPhotoThumbnail({ item }: { item: PendingMeterPhoto }) {
  const [url, setUrl] = useState("");
  useEffect(() => { const next = URL.createObjectURL(item.content); setUrl(next); return () => URL.revokeObjectURL(next); }, [item.id, item.content]);
  return <div className="pending-photo-thumb">{url && <img src={url} alt="Lokale Vorschau des Ablesefotos" />}</div>;
}

function ReadingReview({ session, reading, history, permissions, onSaved }: { session: BrowserSession; reading: ReadingReviewItem | null; history: ReadingReviewItem[]; permissions: { canApproveMeterReadings: boolean; reviewerMemberId: number | null }; onSaved: () => void }) {
  const [comment, setComment] = useState(""); const [date, setDate] = useState(""); const [value, setValue] = useState(""); const [saving, setSaving] = useState(false); const [error, setError] = useState("");
  const editLock = useEditLock(session, "zaehler_ablesung", reading?.readingId, Boolean(reading));
  useEffect(() => { setComment(""); setDate(reading?.ablesedatum?.slice(0, 10) ?? ""); setValue(reading ? String(reading.stand) : ""); setError(""); }, [reading?.readingId]);
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
      await reviewMeterReading(session, permissions, { readingId: reading.readingId, action, comment, date, value: stand });
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

function formatTimeRange(start: string | null | undefined, end: string | null | undefined) {
  const short = (value: string | null | undefined) => value ? value.slice(0, 5) : "";
  const from = short(start);
  const to = short(end);
  if (from && to) return `${from}–${to} Uhr`;
  if (from) return `${from} Uhr`;
  if (to) return `bis ${to} Uhr`;
  return "–";
}

function currentLocalDate() {
  const date = new Date();
  const year = date.getFullYear();
  const month = String(date.getMonth() + 1).padStart(2, "0");
  const day = String(date.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function MaintenanceContracts({ session, memberId, canManage }: { session: BrowserSession; memberId?: number; canManage: boolean }) {
  const [contracts, setContracts] = useState<MaintenanceContract[]>([]); const [assignments, setAssignments] = useState<MaintenanceAssignment[]>([]); const [members, setMembers] = useState<Member[]>([]); const [parcels, setParcels] = useState<Parcel[]>([]); const [parcelAssignments, setParcelAssignments] = useState<ParcelAssignment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null); const [editing, setEditing] = useState(false); const [editingId, setEditingId] = useState<number | null>(null); const [draft, setDraft] = useState<MaintenanceContractDraft | null>(null); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const [assignMemberId, setAssignMemberId] = useState(memberId ? String(memberId) : ""); const [assignContractId, setAssignContractId] = useState(""); const [validFrom, setValidFrom] = useState(new Date().toISOString().slice(0, 10)); const [assignmentNote, setAssignmentNote] = useState(""); const [endDates, setEndDates] = useState<Record<number, string>>({});
  const today = new Date().toISOString().slice(0, 10);
  async function load() {
    setMessage("");
    try {
      const [loadedContracts, loadedAssignments, loadedMembers, loadedParcels, loadedParcelAssignments] = await Promise.all([
        loadMaintenanceContracts(session),
        loadMaintenanceAssignments(session, memberId && !canManage ? memberId : undefined),
        readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", aktiv: "eq.true", order: "name.asc,vorname.asc", limit: "1000" }),
        readSupabase<Parcel>(session, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", limit: "1000" }),
        readSupabase<ParcelAssignment>(session, "parzellen_belegung", { select: "id,parzelle_id,mitglied_id,von_datum,bis_datum", limit: "2000" }),
      ]);
      setContracts(loadedContracts); setAssignments(loadedAssignments); setMembers(loadedMembers); setParcels(loadedParcels); setParcelAssignments(loadedParcelAssignments);
      const permittedIds = memberId ? new Set(loadedAssignments.filter((item) => item.principalMemberId === memberId).map((item) => item.maintenanceContractId)) : null;
      const selectable = permittedIds ? loadedContracts.filter((item) => permittedIds.has(item.id)) : loadedContracts;
      setSelectedId((current) => current && selectable.some((item) => item.id === current) ? current : selectable[0]?.id ?? null);
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Wartungsverträge konnten nicht geladen werden."); }
  }
  useEffect(() => { setAssignMemberId(memberId ? String(memberId) : ""); void load(); }, [session, memberId]);
  const isActive = (item: MaintenanceAssignment) => item.validFrom <= today && (!item.validUntil || item.validUntil >= today);
  const activeAssignments = assignments.filter(isActive);
  const memberAssignments = memberId ? assignments.filter((assignment) => assignment.principalMemberId === memberId) : assignments;
  const visibleContracts = memberId ? contracts.filter((contract) => memberAssignments.some((assignment) => assignment.maintenanceContractId === contract.id)) : contracts;
  const selected = contracts.find((item) => item.id === selectedId) ?? null;
  const editLock = useEditLock(session, "wartungsvertraege", editingId, Boolean(editing && editingId));
  const selectedAssignments = selected ? memberAssignments.filter((item) => item.maintenanceContractId === selected.id) : [];
  const occupancy = (contractId: number) => activeAssignments.filter((item) => item.maintenanceContractId === contractId).length;
  const assignableForMember = memberId ? contracts.filter((contract) => contract.active && occupancy(contract.id) < contract.maxActiveAssignments && !activeAssignments.some((item) => item.principalMemberId === memberId && item.maintenanceContractId === contract.id)) : [];
  const memberName = (id: number) => { const member = members.find((item) => item.id === id); return member ? `${member.name ?? ""}, ${member.vorname ?? ""}`.replace(/^, |, $/g, "") || `Mitglied #${id}` : `Mitglied #${id}`; };
  const memberContext = (id: number) => members.find((item) => item.id === id)?.hauptmitglied_id ? "Nebenmitglied" : "Hauptmitglied";
  const gardenNumbers = (id: number) => { const parcelIds = parcelAssignments.filter((item) => item.mitglied_id === id && (!item.bis_datum || item.bis_datum >= today)).map((item) => item.parzelle_id); return parcels.filter((item) => parcelIds.includes(item.id)).map((item) => item.garten_nr).filter(Boolean).join(", ") || "–"; };
  function startNew() { setEditingId(null); setDraft({ title: "", description: "", area: "", maxActiveAssignments: 1, exemptsFromDutyHours: true, workHoursCredit: 0, active: true, note: "" }); setEditing(true); setMessage(""); }
  function startEdit() { if (selected) { const { id: _id, ...nextDraft } = selected; setEditingId(selected.id); setDraft(nextDraft); setEditing(true); setMessage(""); } }
  useEffect(() => { if (editing && editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editing, editLock.message]);
  async function saveContract() { if (!draft) return; const validation = normalizeAndValidateMaintenanceContract(draft); if (!validation.payload) { setMessage(validation.error ?? "Wartungsvertrag ist ungültig."); return; } if (editingId && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; } setSaving(true); setMessage(""); try { const saved = await saveMaintenanceContract(session, editingId, draft); setSelectedId(saved.id); setEditingId(null); setEditing(false); await load(); setMessage("Wartungsvertrag gespeichert."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Wartungsvertrag konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  async function assign() { const contractId = memberId ? Number(assignContractId) : selected?.id; const targetMember = Number(assignMemberId); if (!contractId || !targetMember || !validFrom) { setMessage("Vertrag, Mitglied und Gültigkeitsbeginn sind erforderlich."); return; } setSaving(true); setMessage(""); try { await createMaintenanceAssignmentForContract(session, { maintenanceContractId: contractId, principalMemberId: targetMember, validFrom, validUntil: null, note: assignmentNote }); setAssignmentNote(""); setAssignContractId(""); await load(); setMessage("Wartungsvertrag wurde zugeordnet."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Zuordnung konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  async function endAssignment(item: MaintenanceAssignment) { const endDate = endDates[item.id] || today; setSaving(true); setMessage(""); try { await endMaintenanceAssignmentForContract(session, item, endDate); await load(); setMessage("Wartungsvertrag wurde beendet."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Zuordnung konnte nicht beendet werden."); } finally { setSaving(false); } }
  if (editing && draft) return <section className="data-workspace maintenance-editor"><div className="detail-title"><div><h2>{editingId ? "Wartungsvertrag bearbeiten" : "Wartungsvertrag anlegen"}</h2><p>Titel, Kontingent, Arbeitsstundenwirkung und Aktivstatus</p></div></div>{message && <p className="notice">{message}</p>}<fieldset disabled={saving}><label>Titel<input value={draft.title} onChange={(event) => setDraft({ ...draft, title: event.target.value })} /></label><label>Bereich<input value={draft.area ?? ""} onChange={(event) => setDraft({ ...draft, area: event.target.value })} placeholder="z. B. Wasseranlage" /></label><label>Max. aktive Zuordnungen<input type="number" min="1" value={draft.maxActiveAssignments} onChange={(event) => setDraft({ ...draft, maxActiveAssignments: Math.max(1, Number(event.target.value) || 1) })} /></label><label>Arbeitsstunden-Gutschrift pro Saison<input type="number" min="0" step="0.25" value={draft.workHoursCredit} onChange={(event) => setDraft({ ...draft, workHoursCredit: Math.max(0, Number(event.target.value) || 0) })} /></label><label className="check"><input type="checkbox" checked={draft.exemptsFromDutyHours} onChange={(event) => setDraft({ ...draft, exemptsFromDutyHours: event.target.checked })} /> Befreit von Pflichtstunden</label><label className="wide">Beschreibung<textarea value={draft.description ?? ""} onChange={(event) => setDraft({ ...draft, description: event.target.value })} /></label><label className="wide">Bemerkung<textarea value={draft.note ?? ""} onChange={(event) => setDraft({ ...draft, note: event.target.value })} /></label><label className="check"><input type="checkbox" checked={draft.active} onChange={(event) => setDraft({ ...draft, active: event.target.checked })} /> Vertrag aktiv</label></fieldset><div className="editor-actions"><button disabled={saving} onClick={saveContract}>{saving ? "Speichert …" : "Speichern"}</button><button className="secondary-action" onClick={() => { setEditing(false); setEditingId(null); setDraft(null); }}>Abbrechen</button></div></section>;
  return <section className="data-workspace maintenance-workspace" aria-label={memberId ? "Wartungsverträge des Mitglieds" : "Wartungsvertragsverwaltung"}>{message && <p className="notice" role="status">{message}</p>}<div className="data-toolbar"><span>{memberId ? `${visibleContracts.length} zugeordnete Verträge` : `${contracts.length} Wartungsverträge`}</span>{canManage && !memberId && <button onClick={startNew}>Wartungsvertrag anlegen</button>}</div>{canManage && memberId && <div className="maintenance-member-assign"><strong>Wartungsvertrag zuweisen</strong><label>Freier Vertrag<select value={assignContractId} onChange={(event) => setAssignContractId(event.target.value)}><option value="">Vertrag wählen</option>{assignableForMember.map((contract) => <option key={contract.id} value={contract.id}>{contract.title} · {occupancy(contract.id)} von {contract.maxActiveAssignments}</option>)}</select></label><label>Gültig ab<input type="date" value={validFrom} onChange={(event) => setValidFrom(event.target.value)} /></label><label>Bemerkung<input value={assignmentNote} onChange={(event) => setAssignmentNote(event.target.value)} /></label><button disabled={saving || !assignContractId} onClick={assign}>Zuordnung speichern</button></div>}<div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Titel</th><th>Bereich</th><th>Belegung</th><th>Status</th></tr></thead><tbody>{visibleContracts.map((contract) => { const occupied = occupancy(contract.id); return <tr key={contract.id} className={selectedId === contract.id ? "selected-row" : ""} onClick={() => setSelectedId(contract.id)}><td><strong>{contract.title}</strong><small>{contract.description || "Keine Beschreibung"}</small></td><td>{contract.area || "–"}</td><td>{occupied} von {contract.maxActiveAssignments}</td><td>{contract.active ? "aktiv" : "inaktiv"}</td></tr>; })}</tbody></table>{visibleContracts.length === 0 && <p className="empty-state">{memberId ? "Diesem Mitglied ist kein Wartungsvertrag zugeordnet." : "Keine Wartungsverträge vorhanden."}</p>}</div>{selected ? <aside className="detail-panel maintenance-detail"><div className="detail-title"><div><h2>{selected.title}</h2><p>{selected.area || "Kein Bereich"}</p></div>{canManage && <button onClick={startEdit}>Bearbeiten</button>}</div><p>{selected.description || "Keine Beschreibung hinterlegt."}</p><div className="maintenance-summary"><div><span>Kontingent</span><strong>{selected.maxActiveAssignments}</strong></div><div><span>Belegt</span><strong>{occupancy(selected.id)}</strong></div><div><span>Frei</span><strong>{Math.max(0, selected.maxActiveAssignments - occupancy(selected.id))}</strong></div></div><p className="context-note">{selected.exemptsFromDutyHours ? "Zuordnung befreit von Pflichtstunden." : selected.workHoursCredit > 0 ? `${selected.workHoursCredit} Arbeitsstunden pro Saison gutgeschrieben.` : "Keine Arbeitsstundenanrechnung."}</p>{selected.note && <p>{selected.note}</p>}<h3>Zuordnungen</h3><div className="maintenance-assignments">{selectedAssignments.map((item) => <article key={item.id}><div><strong>{memberName(item.principalMemberId)}</strong><span>{memberContext(item.principalMemberId)} · Garten {gardenNumbers(item.principalMemberId)}</span><span>{formatDate(item.validFrom)} – {item.validUntil ? formatDate(item.validUntil) : "aktiv"}</span>{item.note && <small>{item.note}</small>}</div>{canManage && isActive(item) && <div><input type="date" value={endDates[item.id] || today} min={item.validFrom} onChange={(event) => setEndDates({ ...endDates, [item.id]: event.target.value })} /><button className="secondary-action" disabled={saving} onClick={() => endAssignment(item)}>Beenden</button></div>}</article>)}{selectedAssignments.length === 0 && <p>Keine Zuordnungen vorhanden.</p>}</div>{canManage && selected.active && !memberId && <div className="maintenance-assignment-form"><h3>Mitglied zuordnen</h3><label>Mitglied<select value={assignMemberId} onChange={(event) => setAssignMemberId(event.target.value)}><option value="">Mitglied wählen</option>{members.map((member) => <option key={member.id} value={member.id}>{member.name}, {member.vorname} · #{member.id}</option>)}</select></label><label>Gültig ab<input type="date" value={validFrom} onChange={(event) => setValidFrom(event.target.value)} /></label><label className="wide">Bemerkung<input value={assignmentNote} onChange={(event) => setAssignmentNote(event.target.value)} /></label><button disabled={saving || occupancy(selected.id) >= selected.maxActiveAssignments} onClick={assign}>{occupancy(selected.id) >= selected.maxActiveAssignments ? "Kontingent belegt" : "Zuordnung speichern"}</button></div>}</aside> : <aside className="detail-panel"><h2>Wartungsvertrag</h2><p>Wähle links einen Vertrag aus.</p></aside>}</div></section>;
}

function ContractComposer({ session, memberId, onSaved, leaseContractIntent }: { session: BrowserSession; memberId: number; onSaved: () => Promise<void>; leaseContractIntent?: { parcelId: number; startDate: string } | null }) {
  const [type, setType] = useState<"mitgliedsantrag" | "mitgliedsvertrag" | "pachtvertrag">(leaseContractIntent ? "pachtvertrag" : "mitgliedsantrag");
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [memberSignature, setMemberSignature] = useState(""); const [secondarySignature, setSecondarySignature] = useState(""); const [boardSignature, setBoardSignature] = useState("");
  const [message, setMessage] = useState(""); const [busy, setBusy] = useState(false);
  function request(action: "preview" | "finalize") { return { action, type: "mitgliedsvertrag" as const, member_id: memberId, start_date: startDate, signature_member: memberSignature || undefined, signature_secondary: secondarySignature || undefined, signature_board: boardSignature || undefined }; }
  async function preview() { setBusy(true); setMessage(""); try { const result = await generateContract(session, request("preview")); if (result.previewUrl) window.open(result.previewUrl, "_blank", "noopener,noreferrer"); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Vorschau konnte nicht erstellt werden."); } finally { setBusy(false); } }
  async function finalize() { if (!memberSignature || !boardSignature) { setMessage("Die Unterschriften des Mitglieds und des Vereins sind erforderlich."); return; } setBusy(true); setMessage(""); try { const result = await generateContract(session, request("finalize")); setMessage(result.message ?? "Das signierte Dokument wurde gespeichert."); setMemberSignature(""); setSecondarySignature(""); setBoardSignature(""); await onSaved(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Vertrag konnte nicht gespeichert werden."); } finally { setBusy(false); } }
  if (type === "mitgliedsantrag") return <MembershipApplicationFlow session={session} memberId={memberId} onSaved={onSaved} onChangeDocumentType={setType} />;
  if (type === "pachtvertrag") return <LeaseContractFlow session={session} memberId={memberId} onSaved={onSaved} onChangeDocumentType={setType} initialParcelId={leaseContractIntent?.parcelId} initialStartDate={leaseContractIntent?.startDate} />;
  return <details className="contract-composer"><summary>Vertragsdokument erstellen und unterschreiben</summary>{message && <p className="notice" role="status">{message}</p>}<fieldset disabled={busy}><label>Dokumenttyp<select value={type} onChange={(event) => setType(event.target.value as typeof type)}><option value="mitgliedsantrag">Mitgliedsantrag</option><option value="mitgliedsvertrag">Mitgliedsvertrag</option><option value="pachtvertrag">Pachtvertrag</option></select></label><label>Beginn<input type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} /></label></fieldset><div className="signature-grid"><SignaturePad label="Unterschrift Mitglied" value={memberSignature} onChange={setMemberSignature} /><SignaturePad label="Gesetzlicher Vertreter (optional)" value={secondarySignature} onChange={setSecondarySignature} /><SignaturePad label="Unterschrift Verein" value={boardSignature} onChange={setBoardSignature} /></div><div className="editor-actions"><button type="button" className="secondary-action" disabled={busy} onClick={preview}>PDF-Vorschau</button><button type="button" disabled={busy} onClick={finalize}>{busy ? "Verarbeitet …" : "Signiert sicher ablegen"}</button></div></details>;
}

const permissionAreas = [
  { key: "mitgliedaufnahme", label: "Mitglieder aufnehmen / verpachten", all: Permission.createMember, read: Permission.createMember, write: Permission.createMember },
  { key: "stammdaten", label: "Stammdaten", all: Permission.showStammdaten | Permission.readStammdaten | Permission.writeStammdaten, read: Permission.readStammdaten, write: Permission.writeStammdaten },
  { key: "parzellen", label: "Parzellen", all: Permission.showParzellen | Permission.readParzellen | Permission.writeParzellen, read: Permission.readParzellen, write: Permission.writeParzellen },
  { key: "dokumente", label: "Dokumente", all: Permission.readDocuments | Permission.manageDocuments, read: Permission.readDocuments, write: Permission.manageDocuments },
  { key: "arbeitsstunden", label: "Arbeitsstunden", all: Permission.readWorkHours | Permission.manageWorkHours, read: Permission.readWorkHours, write: Permission.manageWorkHours },
  { key: "zaehlerwechsel", label: "Zählerwechsel", all: Permission.readMeters | Permission.manageMeterChanges, read: Permission.readMeters, write: Permission.manageMeterChanges },
  { key: "ablesungsfreigaben", label: "Ablesungsfreigaben", all: Permission.readMeters | Permission.approveMeterReadings, read: Permission.readMeters, write: Permission.approveMeterReadings },
  { key: "rollen", label: "Rollen / Rechte", all: Permission.readRoles | Permission.manageRoles, read: Permission.readRoles, write: Permission.manageRoles },
] as const;
type PermissionLevel = "none" | "read" | "write";

function rolePermissions(role: AppUserAdmin["role"]) {
  if (role === "admin") return 1048575;
  if (role === "vorstand") return Permission.searchMembers | Permission.viewMembers | Permission.editAllMembers | Permission.showStammdaten | Permission.readStammdaten | Permission.writeStammdaten | Permission.showParzellen | Permission.readParzellen | Permission.writeParzellen | Permission.readDocuments | Permission.manageDocuments | Permission.readWorkHours | Permission.manageWorkHours | Permission.readMeters | Permission.manageMeterChanges | Permission.approveMeterReadings | Permission.readRoles;
  return Permission.viewMembers | Permission.seeOwnData;
}

function permissionLevel(mask: number, area: (typeof permissionAreas)[number]): PermissionLevel {
  if ((mask & area.write) === area.write) return "write";
  if ((mask & area.read) === area.read) return "read";
  return "none";
}

function UserRightsAdministration({ session, fixedMemberId }: { session: BrowserSession; fixedMemberId?: number }) {
  const [members, setMembers] = useState<Member[]>([]);
  const [users, setUsers] = useState<AppUserAdmin[]>([]);
  const [selectedMemberId, setSelectedMemberId] = useState<number | null>(fixedMemberId ?? null);
  const [role, setRole] = useState<AppUserAdmin["role"]>("user");
  const [levels, setLevels] = useState<Record<string, PermissionLevel>>({});
  const [query, setQuery] = useState("");
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  async function load() {
    try {
      const [loadedMembers, loadedUsers] = await Promise.all([
        readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,auth_user_id", order: "name.asc", limit: "3000" }),
        readSupabase<AppUserAdmin>(session, "app_user", { select: "user_id,mitglied_id,role,permission_grants,permission_revocations,updated_at", order: "mitglied_id.asc", limit: "3000" }),
      ]);
      setMembers(loadedMembers); setUsers(loadedUsers);
      setSelectedMemberId((current) => fixedMemberId ?? current ?? loadedMembers[0]?.id ?? null);
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Benutzer konnten nicht geladen werden."); }
  }
  useEffect(() => { void load(); }, [session, fixedMemberId]);
  const selectedMember = members.find((item) => item.id === selectedMemberId) ?? null;
  const selectedUser = users.find((item) => item.mitglied_id === selectedMemberId) ?? null;
  const editLock = useEditLock(session, "app_user", selectedUser?.user_id, Boolean(selectedUser));
  useEffect(() => {
    const nextRole = selectedUser?.role ?? "user";
    setRole(nextRole);
    const effective = (rolePermissions(nextRole) | (selectedUser?.permission_grants ?? 0)) & ~(selectedUser?.permission_revocations ?? 0);
    setLevels(Object.fromEntries(permissionAreas.map((area) => [area.key, permissionLevel(effective, area)])));
    setMessage("");
  }, [selectedMemberId, selectedUser?.user_id, selectedUser?.role, selectedUser?.permission_grants, selectedUser?.permission_revocations]);
  function changeRole(nextRole: AppUserAdmin["role"]) {
    setRole(nextRole);
    const base = rolePermissions(nextRole);
    setLevels(Object.fromEntries(permissionAreas.map((area) => [area.key, permissionLevel(base, area)])));
  }
  async function save() {
    if (!selectedUser) { setMessage("Für dieses Mitglied existiert noch kein verknüpfter App-Benutzer. Die Erstanmeldung muss zuerst abgeschlossen werden."); return; }
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    const base = rolePermissions(role);
    let required = 0; let controllable = 0;
    for (const area of permissionAreas) {
      controllable |= area.all;
      const level = levels[area.key] ?? "none";
      if (level === "read") required |= area.read;
      if (level === "write") required |= area.write;
    }
    const grants = required & ~base;
    const revocations = (base & controllable) & ~required;
    setSaving(true); setMessage("");
    try {
      await writeSupabase<AppUserAdmin>(session, "app_user", "PATCH", { mitglied_id: selectedMemberId, role, permission_grants: grants, permission_revocations: revocations, updated_at: new Date().toISOString() }, { user_id: `eq.${selectedUser.user_id}` });
      await load(); setMessage("Rolle und Fachrechte wurden gespeichert.");
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Rolle und Rechte konnten nicht gespeichert werden."); }
    finally { setSaving(false); }
  }
  async function invite() {
    if (!selectedMember?.email || !selectedMemberId) { setMessage("Für die Einladung fehlt eine E-Mail-Adresse."); return; }
    setSaving(true); setMessage("");
    try { const result = await inviteAppUser(session, selectedMemberId, role); await load(); setMessage(result.message ?? (result.linkPrepared ? "Nutzerkonto wurde vorbereitet." : "Einladung wurde angestoßen.")); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Einladung konnte nicht gesendet werden."); }
    finally { setSaving(false); }
  }
  async function resetPassword() {
    if (!selectedMember?.email) { setMessage("Für den Passwort-Reset fehlt eine E-Mail-Adresse."); return; }
    setSaving(true); setMessage("");
    try { const result = await sendPasswordReset(session, selectedMember.email); setMessage(result.message ?? "Passwort-Reset wurde versendet."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Passwort-Reset konnte nicht versendet werden."); }
    finally { setSaving(false); }
  }
  async function removeUser() {
    if (!selectedUser || !selectedMemberId || !window.confirm(`App-Benutzer von ${selectedMember?.vorname ?? ""} ${selectedMember?.name ?? ""} wirklich entfernen?`)) return;
    setSaving(true); setMessage("");
    try { await writeSupabase<Member>(session, "mitglied", "PATCH", { auth_user_id: null }, { id: `eq.${selectedMemberId}` }); await deleteSupabase(session, "app_user", { user_id: `eq.${selectedUser.user_id}` }); await load(); setMessage("Die App-Benutzerzuordnung wurde entfernt. Das Auth-Konto bleibt aus Sicherheitsgründen serverseitig bestehen."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "App-Benutzer konnte nicht entfernt werden."); }
    finally { setSaving(false); }
  }
  const visibleMembers = fixedMemberId ? members.filter((item) => item.id === fixedMemberId) : members.filter((item) => `${item.name ?? ""} ${item.vorname ?? ""} ${item.email ?? ""} ${item.id}`.toLowerCase().includes(query.toLowerCase()));
  return <section className="data-workspace admin-workspace" aria-label="Benutzer und Rechte">{message && <p className="notice" role="status">{message}</p>}<div className={fixedMemberId ? "" : "split-view"}>{!fixedMemberId && <div><div className="data-toolbar"><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Mitglied suchen" /><span>{users.length} verknüpfte Benutzer</span></div><div className="data-table-wrap"><table><thead><tr><th>Mitglied</th><th>Konto</th><th>Rolle</th></tr></thead><tbody>{visibleMembers.map((member) => { const user = users.find((item) => item.mitglied_id === member.id); return <tr key={member.id} className={selectedMemberId === member.id ? "selected-row" : ""} onClick={() => setSelectedMemberId(member.id)}><td><strong>{member.name}, {member.vorname}</strong><small>#{member.id} · {member.email || "keine E-Mail"}</small></td><td>{user ? "verknüpft" : "offen"}</td><td>{user?.role ?? "–"}</td></tr>; })}</tbody></table></div></div>}<aside className="detail-panel rights-editor">{selectedMember ? <><div className="detail-title"><div><h2>{selectedMember.name}, {selectedMember.vorname}</h2><p>{selectedUser ? `App-Benutzer ${selectedUser.user_id.slice(0, 8)}…` : "Noch kein App-Benutzer verknüpft"}</p></div></div><div className="account-actions">{!selectedUser && <button disabled={saving || !selectedMember.email} onClick={invite}>Nutzer hinzufügen</button>}{selectedUser && <button className="secondary-action" disabled={saving || !selectedMember.email} onClick={resetPassword}>Passwort-Reset senden</button>}{selectedUser && fixedMemberId && <button className="danger-action" disabled={saving} onClick={removeUser}>Nutzer entfernen</button>}</div>{!selectedUser && <p className="context-note">Mit „Nutzer hinzufügen“ wird der bestehende OTP-/Erstlogin-Ablauf gestartet. Rollen und Fachrechte werden nach erfolgreicher Verknüpfung speicherbar.</p>}<label>Rollenbasis<select value={role} disabled={!selectedUser || saving} onChange={(event) => changeRole(event.target.value as AppUserAdmin["role"])}><option value="user">Mitglied</option><option value="vorstand">Vorstand</option><option value="admin">Administrator</option></select></label><h3>Fachrechte</h3><div className="permission-grid">{permissionAreas.map((area) => <label key={area.key}><span>{area.label}<small>Rollenstandard: {permissionLevel(rolePermissions(role), area) === "write" ? "Bearbeiten" : permissionLevel(rolePermissions(role), area) === "read" ? "Lesen" : "Aus"}</small></span><select value={levels[area.key] ?? "none"} disabled={!selectedUser || saving} onChange={(event) => setLevels({ ...levels, [area.key]: event.target.value as PermissionLevel })}><option value="none">Aus</option><option value="read">Lesen</option><option value="write">Bearbeiten</option></select></label>)}</div><div className="editor-actions"><button disabled={!selectedUser || saving} onClick={save}>{saving ? "Speichert …" : "Rolle und Rechte speichern"}</button><button className="secondary-action" disabled={!selectedUser || saving} onClick={() => changeRole(role)}>Auf Rollenstandard setzen</button></div></> : <><h2>Benutzer auswählen</h2><p>Wähle links ein Mitglied aus.</p></>}</aside></div></section>;
}

function SeasonAdministration({ session, onSeasonSaved }: { session: BrowserSession; onSeasonSaved: (season: Season) => void }) {
  const empty: SeasonAdmin = { id: new Date().getFullYear(), jahr: new Date().getFullYear(), pflichtstunden_soll: 0, euro_pro_fehlstunde: 25, bemerkung: "", pacht_pro_qm: null, mitgliedsbeitrag: null, mitgliedsbeitrag_nebenmitglied: null, aufnahmegebuehr: null, gebuehr_bauantrag: null };
  const [items, setItems] = useState<SeasonAdmin[]>([]); const [draft, setDraft] = useState<SeasonAdmin>(empty); const [isNew, setIsNew] = useState(false); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  async function load(preferredYear?: number) { try { const loaded = await readSupabase<SeasonAdmin>(session, "saison", { select: "id,jahr,pflichtstunden_soll,euro_pro_fehlstunde,bemerkung,pacht_pro_qm,mitgliedsbeitrag,mitgliedsbeitrag_nebenmitglied,aufnahmegebuehr,gebuehr_bauantrag", order: "jahr.desc" }); setItems(loaded); const selected = loaded.find((item) => item.jahr === preferredYear) ?? loaded[0]; if (selected) { setDraft(selected); setIsNew(false); } else suggest(loaded); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Saisons konnten nicht geladen werden."); } }
  useEffect(() => { void load(); }, [session]);
  function suggest(source = items) { const previous = [...source].sort((a, b) => b.jahr - a.jahr)[0]; const year = Math.max((previous?.jahr ?? new Date().getFullYear() - 1) + 1, new Date().getFullYear()); setDraft({ ...(previous ?? empty), id: year, jahr: year }); setIsNew(true); setMessage("Neue Saison wurde auf Basis des Vorjahres vorgeschlagen."); }
  function numberField(key: keyof SeasonAdmin, value: string, nullable = false) { setDraft({ ...draft, [key]: value === "" && nullable ? null : Number(value.replace(",", ".")) }); }
  const editable = draft.jahr >= new Date().getFullYear();
  const editLock = useEditLock(session, "saison", draft.id, Boolean(!isNew && editable));
  useEffect(() => { if (!isNew && editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [isNew, editLock.message]);
  async function save() { if (draft.jahr < new Date().getFullYear() || draft.jahr < 1900 || draft.jahr > 3000) { setMessage("Vergangene Jahre können nicht bearbeitet werden."); return; } if (!isNew && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; } const payload = { ...draft, id: draft.jahr, jahr: draft.jahr, bemerkung: draft.bemerkung?.trim() || null }; setSaving(true); setMessage(""); try { const saved = isNew ? await writeSupabase<SeasonAdmin>(session, "saison", "POST", payload) : await writeSupabase<SeasonAdmin>(session, "saison", "PATCH", payload, { id: `eq.${draft.id}` }); const result = saved[0] ?? payload; await load(result.jahr); onSeasonSaved({ id: result.id, jahr: result.jahr }); setMessage(`Saison ${result.jahr} wurde gespeichert.`); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Saison konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  return <section className="data-workspace admin-workspace">{message && <p className="notice" role="status">{message}</p>}<div className="data-toolbar"><span>{items.length} Saisons</span><button onClick={() => suggest()}>Neue Saison vorschlagen</button></div><div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Jahr</th><th>Pflichtstunden</th><th>Fehlstunde</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id} className={!isNew && draft.id === item.id ? "selected-row" : ""} onClick={() => { setDraft(item); setIsNew(false); setMessage(""); }}><td><strong>{item.jahr}</strong></td><td>{item.pflichtstunden_soll}</td><td>{formatEuro(item.euro_pro_fehlstunde)}</td><td>{item.jahr < new Date().getFullYear() ? "schreibgeschützt" : "bearbeitbar"}</td></tr>)}</tbody></table></div><aside className="detail-panel admin-form"><div className="detail-title"><div><h2>{isNew ? "Neue Saison" : `Saison ${draft.jahr}`}</h2><p>{editable ? "Preise und Pflichtstunden pflegen" : "Vergangene Saisons bleiben unverändert"}</p></div></div><fieldset disabled={!editable || saving}><label>Kalenderjahr<input type="number" min="1900" max="3000" value={draft.jahr} onChange={(event) => { const year = Number(event.target.value); setDraft({ ...draft, id: year, jahr: year }); }} /></label><label>Pacht je m² (€)<input inputMode="decimal" value={draft.pacht_pro_qm ?? ""} onChange={(event) => numberField("pacht_pro_qm", event.target.value, true)} /></label><label>Mitgliedsbeitrag (€)<input inputMode="decimal" value={draft.mitgliedsbeitrag ?? ""} onChange={(event) => numberField("mitgliedsbeitrag", event.target.value, true)} /></label><label>Beitrag Nebenmitglied (€)<input inputMode="decimal" value={draft.mitgliedsbeitrag_nebenmitglied ?? ""} onChange={(event) => numberField("mitgliedsbeitrag_nebenmitglied", event.target.value, true)} /></label><label>Aufnahmegebühr (€)<input inputMode="decimal" value={draft.aufnahmegebuehr ?? ""} onChange={(event) => numberField("aufnahmegebuehr", event.target.value, true)} /></label><label>Gebühr Bauantrag (€)<input inputMode="decimal" value={draft.gebuehr_bauantrag ?? ""} onChange={(event) => numberField("gebuehr_bauantrag", event.target.value, true)} /></label><label>Pflichtstunden Soll<input inputMode="decimal" value={draft.pflichtstunden_soll} onChange={(event) => numberField("pflichtstunden_soll", event.target.value)} /></label><label>Euro je Fehlstunde<input inputMode="decimal" value={draft.euro_pro_fehlstunde} onChange={(event) => numberField("euro_pro_fehlstunde", event.target.value)} /></label><label className="wide">Bemerkung<textarea value={draft.bemerkung ?? ""} onChange={(event) => setDraft({ ...draft, bemerkung: event.target.value })} /></label></fieldset><div className="editor-actions"><button disabled={!editable || saving} onClick={save}>{saving ? "Speichert …" : "Saison speichern"}</button></div></aside></div></section>;
}

function formatEuro(value: number | null | undefined) { return value == null ? "–" : new Intl.NumberFormat("de-DE", { style: "currency", currency: "EUR" }).format(value); }

const emptyClubConfiguration: ClubConfigurationRecord = { id: 0, vereinsname: "", kurzname: "", registerangabe: "", strasse: "", plz: "", ort: "", standard_email: "", standard_telefon: "", website: "", kontoinhaber: "", bankname: "", iban: "", bic: "", verwendungszweck_mitgliedsantrag: "", verwendungszweck_pachtvertrag: "", dokument_ort: "", standard_hinweistext: "", datenschutz_text: "", datenschutz_version: "", datenschutz_stand: null, aktiv: true };
function ClubConfigurationAdministration({ session }: { session: BrowserSession }) {
  const [draft, setDraft] = useState<ClubConfigurationRecord>(emptyClubConfiguration); const [exists, setExists] = useState(false); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const editLock = useEditLock(session, "vereinskonfiguration", draft.id, Boolean(exists && draft.id));
  async function load() { try { const rows = await readSupabase<ClubConfigurationRecord>(session, "vereinskonfiguration", { select: "id,vereinsname,kurzname,registerangabe,strasse,plz,ort,standard_email,standard_telefon,website,kontoinhaber,bankname,iban,bic,verwendungszweck_mitgliedsantrag,verwendungszweck_pachtvertrag,dokument_ort,standard_hinweistext,datenschutz_text,datenschutz_version,datenschutz_stand,aktiv", aktiv: "eq.true", order: "updated_at.desc", limit: "1" }); setDraft(rows[0] ?? emptyClubConfiguration); setExists(Boolean(rows[0])); if (!rows[0]) setMessage("Noch keine aktive Vereinskonfiguration vorhanden. Sie wird beim ersten Speichern angelegt."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Vereinskonfiguration konnte nicht geladen werden."); } }
  useEffect(() => { void load(); }, [session]);
  function update(key: keyof ClubConfigurationRecord, value: string | boolean) { setDraft({ ...draft, [key]: value }); }
  useEffect(() => { if (exists && editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [exists, editLock.message]);
  async function save() { if (exists && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; } const payload = { ...draft, aktiv: true, datenschutz_stand: draft.datenschutz_stand || null }; if (!exists) delete (payload as Partial<ClubConfigurationRecord>).id; setSaving(true); setMessage(""); try { const rows = exists ? await writeSupabase<ClubConfigurationRecord>(session, "vereinskonfiguration", "PATCH", payload, { id: `eq.${draft.id}` }) : await writeSupabase<ClubConfigurationRecord>(session, "vereinskonfiguration", "POST", payload); setDraft(rows[0] ?? draft); setExists(true); setMessage("Vereinskonfiguration wurde gespeichert."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Vereinskonfiguration konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  const fields: Array<[keyof ClubConfigurationRecord, string, string?]> = [["vereinsname", "Vereinsname"], ["kurzname", "Kurzname"], ["registerangabe", "Registerangabe"], ["strasse", "Straße"], ["plz", "PLZ"], ["ort", "Ort"], ["standard_email", "Standard-E-Mail", "email"], ["standard_telefon", "Standard-Telefon"], ["website", "Website", "url"], ["kontoinhaber", "Kontoinhaber"], ["bankname", "Bankname"], ["iban", "IBAN"], ["bic", "BIC"], ["verwendungszweck_mitgliedsantrag", "Verwendungszweck Mitgliedsantrag"], ["verwendungszweck_pachtvertrag", "Verwendungszweck Pachtvertrag"], ["dokument_ort", "Dokumentort"], ["datenschutz_version", "Datenschutz-Version"], ["datenschutz_stand", "Datenschutz-Stand", "date"]];
  return <section className="data-workspace club-configuration"><p className="document-intro">Diese Angaben werden zentral für Verträge, Anschreiben und Dokumentmetadaten verwendet.</p>{message && <p className="notice" role="status">{message}</p>}<fieldset disabled={saving} className="admin-form-grid">{fields.map(([key, label, type]) => <label key={key}>{label}<input type={type ?? "text"} value={String(draft[key] ?? "")} onChange={(event) => update(key, event.target.value)} /></label>)}<label className="wide">Standard-Hinweistext<textarea value={draft.standard_hinweistext ?? ""} onChange={(event) => update("standard_hinweistext", event.target.value)} /></label><label className="wide">Datenschutztext<textarea value={draft.datenschutz_text ?? ""} onChange={(event) => update("datenschutz_text", event.target.value)} /></label></fieldset><div className="editor-actions"><button disabled={saving} onClick={save}>{saving ? "Speichert …" : "Vereinskonfiguration speichern"}</button></div></section>;
}

function parseExportOptions(value: unknown): Array<{ label: string; value: string }> {
  if (Array.isArray(value)) return value.map((item) => typeof item === "object" && item !== null ? { label: String((item as { label?: unknown }).label ?? (item as { value?: unknown }).value ?? ""), value: String((item as { value?: unknown }).value ?? (item as { label?: unknown }).label ?? "") } : { label: String(item), value: String(item) });
  if (typeof value === "string" && value.trim().startsWith("[")) { try { return parseExportOptions(JSON.parse(value)); } catch { return []; } }
  return [];
}
function downloadCsv(fileName: string, columns: ExportColumnDefinition[], rows: Array<Record<string, unknown>>) { const escape = (value: unknown) => { const text = value == null ? "" : typeof value === "boolean" ? value ? "Ja" : "Nein" : String(value); return /[;"\r\n]/.test(text) ? `"${text.replaceAll('"', '""')}"` : text; }; const content = [columns.map((column) => escape(column.label_lang ?? column.label_kurz ?? column.column_key)).join(";"), ...rows.map((row) => columns.map((column) => escape(row[column.column_key])).join(";"))].join("\r\n"); const url = URL.createObjectURL(new Blob(["\ufeff", content], { type: "text/csv;charset=utf-8" })); const link = document.createElement("a"); link.href = url; link.download = fileName; link.click(); URL.revokeObjectURL(url); }

function ExportCenter({ session, canExport }: { session: BrowserSession; canExport: boolean }) {
  const [definitions, setDefinitions] = useState<ExportDefinition[]>([]); const [selectedKey, setSelectedKey] = useState(""); const [filters, setFilters] = useState<ExportFilterDefinition[]>([]); const [columns, setColumns] = useState<ExportColumnDefinition[]>([]); const [values, setValues] = useState<Record<string, string | boolean>>({}); const [options, setOptions] = useState<Record<string, Array<{ label: string; value: string }>>>({}); const [rows, setRows] = useState<Array<Record<string, unknown>>>([]); const [message, setMessage] = useState(""); const [busy, setBusy] = useState(false);
  useEffect(() => { if (!canExport) return; readSupabase<ExportDefinition>(session, "app_export_definition", { select: "export_key,titel,beschreibung,quelle_typ,quelle_name,erlaubt_csv,erlaubt_pdf", aktiv: "eq.true", order: "titel.asc" }).then((items) => { setDefinitions(items); setSelectedKey((current) => current || items[0]?.export_key || ""); }).catch((cause: Error) => setMessage(cause.message)); }, [session, canExport]);
  useEffect(() => { if (!selectedKey) return; setRows([]); setMessage(""); Promise.all([readSupabase<ExportFilterDefinition>(session, "app_export_filter_definition", { select: "export_key,filter_key,label,typ,optionen_json,pflicht,sortierung", export_key: `eq.${selectedKey}`, order: "sortierung.asc" }), readSupabase<ExportColumnDefinition>(session, "app_export_column_definition", { select: "export_key,column_key,label_kurz,label_lang,sortierung,standard_sichtbar", export_key: `eq.${selectedKey}`, order: "sortierung.asc" })]).then(async ([nextFilters, nextColumns]) => { setFilters(nextFilters); setColumns(nextColumns); const defaults: Record<string, string | boolean> = {}; const nextOptions: Record<string, Array<{ label: string; value: string }>> = {}; for (const filter of nextFilters) { if (filter.typ === "boolean") defaults[filter.filter_key] = false; else if (filter.filter_key.includes("jahr")) defaults[filter.filter_key] = String(new Date().getFullYear()); else defaults[filter.filter_key] = filter.filter_key === "ansicht" ? "zusammenfassung" : ""; const inline = parseExportOptions(filter.optionen_json); if (inline.length) nextOptions[filter.filter_key] = inline; else if (typeof filter.optionen_json === "string" && filter.optionen_json.startsWith("rpc_")) { try { const result = await callSupabaseRpc<Array<{ label: string; value: string }>>(session, filter.optionen_json, {}); nextOptions[filter.filter_key] = result; } catch { nextOptions[filter.filter_key] = []; } } } setValues(defaults); setOptions(nextOptions); }).catch((cause: Error) => setMessage(cause.message)); }, [session, selectedKey]);
  const selected = definitions.find((item) => item.export_key === selectedKey) ?? null;
  const preferredColumns = columns.filter((column) => column.standard_sichtbar);
  const visibleColumns = preferredColumns.length ? preferredColumns : columns;
  function rpcPayload() { const payload: Record<string, unknown> = {}; for (const filter of filters) { const raw = values[filter.filter_key]; let key = filter.filter_key; if (selectedKey === "arbeitsstunden_uebersicht") key = ({ jahr: "p_jahr", stunden_offen: "p_stunden_offen", stunden_fertig: "p_stunden_fertig", wartungsvertraege: "p_wartungsvertraege", ansicht: "p_ansicht" } as Record<string, string>)[key] ?? key; const normalized = typeof raw === "string" && raw === "" ? null : key === "p_jahr" || key === "p_jahr" ? Number(raw) : raw; payload[key] = normalized; } return payload; }
  async function run() {
    if (!selected) return;
    setBusy(true); setMessage("");
    try {
      let result: Array<Record<string, unknown>>;
      if (selected.export_key === "mitgliederliste") {
        const [memberRows, parcelRows, assignmentRows] = await Promise.all([
          readSupabase<Record<string, unknown>>(session, "mitglied", { select: "*", order: "name.asc", limit: "5000" }),
          readSupabase<Parcel>(session, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", limit: "5000" }),
          readSupabase<ParcelAssignment>(session, "parzellen_belegung", { select: "id,parzelle_id,mitglied_id,von_datum,bis_datum", limit: "5000" }),
        ]);
        const today = new Date().toISOString().slice(0, 10);
        result = memberRows.map((member) => {
          const memberId = Number(member.id);
          const parcelIds = assignmentRows.filter((item) => item.mitglied_id === memberId && (!item.bis_datum || item.bis_datum >= today)).map((item) => item.parzelle_id);
          const gardens = parcelRows.filter((item) => parcelIds.includes(item.id)).map((item) => item.garten_nr).join(", ");
          return { ...member, mitgliedsnr: member.mitgliedsnummer ?? member.id, strasse_hsnr: member.adresse ?? "", gaerten: gardens, gartennummern: gardens, pachtgaerten: gardens, re: member.email_rechnung_einwilligung, info: member.email_info_einwilligung, wa: member.whatsapp_einwilligung };
        });
      } else if (selected.export_key === "rfid_status") {
        result = await readSupabase<Record<string, unknown>>(session, "v_rfid_scan_context", { select: "*", limit: "5000" });
      } else if (selected.quelle_typ === "table" && selected.quelle_name) {
        result = await readSupabase<Record<string, unknown>>(session, selected.quelle_name, { select: "*", limit: "5000" });
      } else {
        result = await callSupabaseRpc<Array<Record<string, unknown>>>(session, selected.quelle_name || selected.export_key, rpcPayload());
      }
      setRows(result); setMessage(`${result.length} Datensätze geladen.`);
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Export konnte nicht ausgeführt werden."); }
    finally { setBusy(false); }
  }
  if (!canExport) return <section className="data-workspace"><p className="notice">Exporte stehen mobil nur Vorstand und Administratoren zur Verfügung.</p></section>;
  return <section className="data-workspace export-center">{message && <p className="notice" role="status">{message}</p>}<div className="export-controls"><label>Exportdefinition<select value={selectedKey} onChange={(event) => setSelectedKey(event.target.value)}>{definitions.map((item) => <option key={item.export_key} value={item.export_key}>{item.titel ?? item.export_key}</option>)}</select></label>{selected?.beschreibung && <p>{selected.beschreibung}</p>}<div className="export-filters">{filters.map((filter) => <label key={filter.filter_key}>{filter.label ?? filter.filter_key}{filter.typ === "boolean" ? <input type="checkbox" checked={Boolean(values[filter.filter_key])} onChange={(event) => setValues({ ...values, [filter.filter_key]: event.target.checked })} /> : options[filter.filter_key]?.length ? <select value={String(values[filter.filter_key] ?? "")} onChange={(event) => setValues({ ...values, [filter.filter_key]: event.target.value })}><option value="">Alle</option>{options[filter.filter_key].map((option) => <option key={option.value} value={option.value}>{option.label}</option>)}</select> : <input type={filter.filter_key.includes("jahr") ? "number" : "text"} value={String(values[filter.filter_key] ?? "")} onChange={(event) => setValues({ ...values, [filter.filter_key]: event.target.value })} />}</label>)}</div><div className="editor-actions"><button disabled={busy || !selected} onClick={run}>{busy ? "Wird ausgeführt …" : "Ausführen"}</button><button className="secondary-action" disabled={!rows.length || !selected?.erlaubt_csv} onClick={() => downloadCsv(`${new Date().toISOString().slice(0, 10)}_${selectedKey}.csv`, visibleColumns, rows)}>Als CSV speichern</button><button className="secondary-action" disabled={!rows.length || !selected?.erlaubt_pdf} onClick={() => window.print()}>Drucken / als PDF speichern</button></div></div>{rows.length > 0 && <div className="data-table-wrap export-result"><table><thead><tr>{visibleColumns.map((column) => <th key={column.column_key}>{column.label_kurz ?? column.label_lang ?? column.column_key}</th>)}</tr></thead><tbody>{rows.slice(0, 500).map((row, index) => <tr key={index}>{visibleColumns.map((column) => <td key={column.column_key}>{row[column.column_key] == null ? "" : typeof row[column.column_key] === "boolean" ? row[column.column_key] ? "Ja" : "Nein" : String(row[column.column_key])}</td>)}</tr>)}</tbody></table>{rows.length > 500 && <p className="empty-state">Vorschau zeigt 500 von {rows.length} Datensätzen. CSV enthält alle Datensätze.</p>}</div>}</section>;
}
