"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import {
  AppUserContext,
  BrowserSession,
  archiveDocument,
  callSupabaseRpc,
  createDocumentOpenUrl,
  deleteSupabase,
  generateContract,
  inviteAppUser,
  openDriveDocument,
  readSupabase,
  sendPasswordReset,
  uploadDocument,
  writeSupabase,
} from "../lib/supabase-auth";
import { type ClubContext } from "../models/auth/club";
import { AuthProvider, useAuth } from "../features/auth/AuthProvider";
import { ChangeClubAction } from "../features/auth/ChangeClubAction";
import { ClubSelection } from "../features/auth/ClubSelection";
import { LoginForm } from "../features/auth/LoginForm";
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
type WorkAssignment = { id: number; titel: string | null; beschreibung: string | null; datum: string; start_uhrzeit: string | null; end_uhrzeit?: string | null; treffpunkt: string | null; max_teilnehmer?: number | string | null; stunden_wert: number; sichtbar_ab?: string | null; sichtbar_bis?: string | null; anmeldung_bis?: string | null; aktiv: boolean };
type Appointment = { id: number; titel: string | null; beschreibung: string | null; datum: string; start_uhrzeit: string | null; end_uhrzeit?: string | null; sichtbar_ab?: string | null; sichtbar_bis?: string | null; aktiv: boolean };
type Announcement = { id: number; titel: string | null; inhalt_html: string | null; sichtbar_ab: string | null; sichtbar_bis: string | null; sort_order?: number | null; aktiv: boolean };
type DocumentRecord = { id: number; mitglied_id: number | null; parzelle_id: number | null; bucket: string | null; storage_path: string | null; drive_file_id: string | null; titel: string | null; dateiname: string | null; mime_type: string | null; size_bytes: number | null; updated_at: string; archiviert_at: string | null };
type MaintenanceContract = { id: number; titel: string; beschreibung: string | null; bereich: string | null; max_aktive_zuordnungen: number; befreit_von_pflichtstunden: boolean; aktiv: boolean; bemerkung: string | null; is_demo: boolean };
type MaintenanceAssignment = { id: number; wartungsvertrag_id: number; hauptmitglied_id: number; gueltig_ab: string; gueltig_bis: string | null; bemerkung: string | null };
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
} as const;

function permissionsFor(context: AppUserContext) {
  const base = context.role === "admin"
    ? 1048575
    : context.role === "vorstand"
      ? Permission.searchMembers | Permission.viewMembers | Permission.editAllMembers | Permission.showStammdaten | Permission.readStammdaten | Permission.writeStammdaten | Permission.readParzellen | Permission.readDocuments | Permission.manageDocuments | Permission.readWorkHours | Permission.manageWorkHours | Permission.readMeters | Permission.manageMeterChanges | Permission.approveMeterReadings | Permission.readRoles
      : Permission.viewMembers | Permission.seeOwnData;
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
          {activeId === "start" && <HomeDashboard session={session} isManager={context.role !== "user"} memberId={context.mitgliedId} saisonId={workspaceContext.saisonId} season={season} onNavigate={setActiveId} onOpenWorkHours={openOwnWorkHours} />}
          {activeId === "mitglieder" && <MemberSearch session={session} selectedMemberId={selectedMemberId} onSelect={selectMember} canCreate={has(Permission.createMember)} onCreate={() => { setCreatingMember(true); setActiveId("mitglied-stammdaten"); }} />}
          {activeId === "parzellen" && <ParcelWorkspace session={session} selectedParcelId={selectedParcelId} onSelect={selectParcel} canEdit={has(Permission.writeParzellen)} onOpenMember={(memberId) => { selectMember(memberId); setActiveId("mitglied-gaerten"); }} />}
          {activeId === "ablesen" && <MeterOverview session={session} clubId={club.vereinId} reviewerMemberId={context.mitgliedId} selectedParcelId={selectedParcelId} seasonYear={season} canReadMeters={has(Permission.readMeters)} canSubmitOwnMeterReadings={context.mitgliedId !== null && has(Permission.seeOwnData)} canApprove={has(Permission.approveMeterReadings)} canManageMeterChanges={has(Permission.manageMeterChanges)} onNavigate={setActiveId} />}
          {activeId === "ablesen" && has(Permission.manageMeterChanges) && <RfidAssignmentPanel session={session} selectedParcelId={selectedParcelId} />}
          {activeId === "foto-uploads" && <PendingPhotoUploads session={session} clubId={club.vereinId} />}
          {activeId === "zaehlerwechsel" && has(Permission.manageMeterChanges) && <MeterChange session={session} clubId={club.vereinId} canManageMeterChanges={has(Permission.manageMeterChanges)} />}
          {activeId === "arbeitsstunden-pruefen" && <WorkHoursReview session={session} canManageWorkHours={has(Permission.manageWorkHours)} />}
          {activeId === "arbeitseinsaetze" && <WorkAssignmentsManagement session={session} canEdit={context.role !== "user"} saisonId={workspaceContext.saisonId} onBack={() => setActiveId("start")} />}
          {activeId === "wartung" && <MaintenanceContracts session={session} canManage={context.role !== "user"} />}
          {activeId === "termine" && <AppointmentManagement session={session} canEdit={context.role !== "user"} onBack={() => setActiveId("start")} />}
          {activeId === "bekanntmachungen" && <AnnouncementManagement session={session} canEdit={context.role !== "user"} onBack={() => setActiveId("start")} />}
          {activeId === "impressum" && <ImprintPage session={session} />}
          {activeId === "export" && <ExportCenter session={session} canExport={context.role !== "user"} />}
          {activeId === "benutzer" && <UserRightsAdministration session={session} />}
          {activeId === "saisons" && <SeasonAdministration session={session} onSeasonSaved={(saved) => { setSeasons((current) => [...current.filter((item) => item.id !== saved.id), saved].sort((a, b) => b.jahr - a.jahr)); selectWorkspaceSeason(saved); }} />}
          {activeId === "verein" && <ClubConfigurationAdministration session={session} />}
          {activeId === "mitglied-arbeitsstunden" && selectedMemberId && <MemberWorkHours session={session} memberId={selectedMemberId} saisonId={workspaceContext.saisonId} canEditOwn={selectedMemberId === context.mitgliedId} canManageWorkHours={has(Permission.manageWorkHours)} />}
          {activeId === "mitglied-wartung" && selectedMemberId && <MaintenanceContracts session={session} memberId={selectedMemberId} canManage={context.role !== "user"} />}
          {activeId === "mitglied-dokumente" && selectedMemberId && <DocumentList session={session} memberId={selectedMemberId} canManage={has(Permission.manageDocuments)} leaseContractIntent={leaseContractIntent} onLeaseContractIntentHandled={() => setLeaseContractIntent(null)} />}
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

function currentLocalDateTime() {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: "Europe/Berlin",
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit",
    hourCycle: "h23",
  }).formatToParts(new Date()).reduce<Record<string, string>>((result, part) => {
    result[part.type] = part.value;
    return result;
  }, {});

  return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`;
}

function addBerlinMonths(value: string, months: number) {
  const date = new Date(`${value}:00Z`);
  date.setUTCMonth(date.getUTCMonth() + months);
  return date.toISOString().slice(0, 16);
}

function WorkAssignmentsManagement({ session, canEdit, saisonId, onBack }: { session: BrowserSession; canEdit: boolean; saisonId: number | null; onBack: () => void }) {
  const [items, setItems] = useState<WorkAssignment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [draft, setDraft] = useState<Partial<WorkAssignment>>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);

  function emptyDraft(): Partial<WorkAssignment> {
    const today = currentLocalDateTime().slice(0, 10);
    return { titel: "", beschreibung: "", datum: today, start_uhrzeit: "10:00", end_uhrzeit: "13:00", treffpunkt: "", max_teilnehmer: null, stunden_wert: 0, sichtbar_ab: currentLocalDateTime(), sichtbar_bis: `${today}T23:59`, anmeldung_bis: "", aktiv: true };
  }

  const load = () => readSupabase<WorkAssignment>(session, "arbeitseinsatz", {
    select: "id,titel,beschreibung,datum,start_uhrzeit,end_uhrzeit,treffpunkt,max_teilnehmer,stunden_wert,sichtbar_ab,sichtbar_bis,anmeldung_bis,aktiv",
    order: "datum.asc",
    limit: "500",
  }).then((rows) => {
    rows.sort((left, right) => `${left.datum}|${left.start_uhrzeit ?? "99:99"}|${left.end_uhrzeit ?? "99:99"}|${left.titel ?? ""}`.localeCompare(`${right.datum}|${right.start_uhrzeit ?? "99:99"}|${right.end_uhrzeit ?? "99:99"}|${right.titel ?? ""}`, "de"));
    setItems(rows);
    return rows;
  }).catch((cause: Error) => { setMessage(cause.message); return [] as WorkAssignment[]; });

  useEffect(() => { void load(); }, [session]);
  const selected = items.find((item) => item.id === selectedId) ?? null;
  const selectedIndex = selected ? items.findIndex((item) => item.id === selected.id) : -1;
  const editLock = useEditLock(session, "arbeitseinsatz", selected?.id, Boolean(selected && canEdit && !creating));
  useEffect(() => { if (!creating) setDraft(selected ?? emptyDraft()); }, [selectedId, creating]);
  useEffect(() => { if (editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editLock.message]);
  const set = (key: keyof WorkAssignment, value: string | number | boolean | null) => setDraft({ ...draft, [key]: value });

  function startNew() { setCreating(true); setSelectedId(null); setDraft(emptyDraft()); setMessage(""); }
  function selectEntry(id: number) { setCreating(false); setSelectedId(id); setMessage(""); }
  function moveSelection(offset: number) { const target = items[selectedIndex + offset]; if (target) selectEntry(target.id); }
  function prepareNextShift(source: Partial<WorkAssignment>) {
    const toMinutes = (value: string | null | undefined, fallback: number) => { if (!value) return fallback; const [hours, minutes] = value.split(":").map(Number); return Number.isFinite(hours + minutes) ? hours * 60 + minutes : fallback; };
    const toTime = (value: number) => `${String(Math.floor(value / 60)).padStart(2, "0")}:${String(value % 60).padStart(2, "0")}`;
    const start = toMinutes(source.start_uhrzeit, 600);
    const end = toMinutes(source.end_uhrzeit, 780);
    const duration = end > start ? end - start : 180;
    const nextStart = Math.min(end, 1439);
    const nextEnd = Math.min(nextStart + duration, 1439);
    setCreating(true);
    setSelectedId(null);
    setDraft({ ...source, id: undefined, start_uhrzeit: toTime(nextStart), end_uhrzeit: toTime(nextEnd) });
    setMessage("Arbeitseinsatz gespeichert. Die nächste Schicht ist bereits vorbefüllt.");
  }

  async function save(prepareNext = false) {
    const hours = Number(draft.stunden_wert ?? 0);
    const capacity = draft.max_teilnehmer === null || draft.max_teilnehmer === undefined || draft.max_teilnehmer === "" ? null : Number(draft.max_teilnehmer);
    if (!draft.titel?.trim() || !draft.datum) { setMessage("Titel und Datum sind erforderlich."); return; }
    if (draft.start_uhrzeit && draft.end_uhrzeit && draft.end_uhrzeit < draft.start_uhrzeit) { setMessage("Das Ende darf nicht vor dem Beginn liegen."); return; }
    if (draft.sichtbar_ab && draft.sichtbar_bis && draft.sichtbar_bis < draft.sichtbar_ab) { setMessage("Das Sichtbarkeitsende darf nicht vor dem Beginn liegen."); return; }
    if (draft.anmeldung_bis && draft.anmeldung_bis.slice(0, 10) > draft.datum) { setMessage("Der Anmeldeschluss darf nicht nach dem Einsatztag liegen."); return; }
    if (!Number.isFinite(hours) || hours < 0 || (capacity !== null && (!Number.isInteger(capacity) || capacity < 1))) { setMessage("Stundenwert und Teilnehmerbegrenzung sind ungültig."); return; }
    if (!creating && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    setSaving(true); setMessage("");
    try {
      const payload: Omit<WorkAssignment, "id"> = { titel: draft.titel.trim(), beschreibung: draft.beschreibung?.trim() || null, datum: draft.datum, start_uhrzeit: draft.start_uhrzeit || null, end_uhrzeit: draft.end_uhrzeit || null, treffpunkt: draft.treffpunkt?.trim() || null, max_teilnehmer: capacity, stunden_wert: hours, sichtbar_ab: draft.sichtbar_ab || null, sichtbar_bis: draft.sichtbar_bis || null, anmeldung_bis: draft.anmeldung_bis || null, aktiv: draft.aktiv !== false };
      const rows = creating
        ? await writeSupabase<WorkAssignment>(session, "arbeitseinsatz", "POST", payload)
        : await writeSupabase<WorkAssignment>(session, "arbeitseinsatz", "PATCH", payload, { id: `eq.${selected?.id}` });
      const persisted = { ...payload, id: rows[0]?.id ?? selected?.id ?? 0 } as WorkAssignment;
      await load();
      if (prepareNext) prepareNextShift(persisted);
      else { setCreating(false); setSelectedId(persisted.id || null); setMessage("Arbeitseinsatz gespeichert."); }
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitseinsatz konnte nicht gespeichert werden."); }
    finally { setSaving(false); }
  }

  async function cancelAssignment() {
    if (!selected) return;
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!window.confirm(`Den Arbeitseinsatz „${selected.titel ?? "Ohne Titel"}“ wirklich absagen?`)) return;
    setSaving(true); setMessage("");
    try { await writeSupabase<WorkAssignment>(session, "arbeitseinsatz", "PATCH", { aktiv: false }, { id: `eq.${selected.id}` }); await load(); setMessage("Arbeitseinsatz wurde abgesagt."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitseinsatz konnte nicht abgesagt werden."); }
    finally { setSaving(false); }
  }

  async function removeAssignment() {
    if (!selected) return;
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!window.confirm(`Den Arbeitseinsatz „${selected.titel ?? "Ohne Titel"}“ einschließlich seiner Anmeldungen endgültig löschen?`)) return;
    setSaving(true); setMessage("");
    try { await deleteSupabase(session, "arbeitseinsatz", { id: `eq.${selected.id}` }); setSelectedId(null); setCreating(false); await load(); setMessage("Arbeitseinsatz wurde gelöscht."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitseinsatz konnte nicht gelöscht werden."); }
    finally { setSaving(false); }
  }

  return <section className="data-workspace work-assignment-management">
    <div className="data-toolbar"><div className="editor-actions"><button className="secondary-action" onClick={onBack}>Zur Startseite</button><button className="secondary-action" onClick={() => void load()}>Aktualisieren</button></div><span>{items.length} Arbeitseinsätze</span>{canEdit && <button onClick={startNew}>Arbeitseinsatz anlegen</button>}</div>
    {message && <p className="notice" role="status">{message}</p>}
    <div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Datum</th><th>Zeit</th><th>Titel</th><th>Treffpunkt</th><th>Teilnehmer</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id} className={selected?.id === item.id && !creating ? "selected-row" : ""} onClick={() => selectEntry(item.id)}><td>{formatDate(item.datum)}</td><td>{formatTimeRange(item.start_uhrzeit, item.end_uhrzeit)}</td><td><strong>{item.titel}</strong></td><td>{item.treffpunkt ?? "–"}</td><td>{item.max_teilnehmer || "unbegrenzt"}</td><td>{item.aktiv ? "aktiv" : "abgesagt"}</td></tr>)}</tbody></table>{items.length === 0 && <p className="empty-state">Keine Arbeitseinsätze vorhanden.</p>}</div>
      <aside className="detail-panel member-editor"><div className="detail-title"><div><h2>{creating ? "Neuer Arbeitseinsatz" : selected ? "Arbeitseinsatz bearbeiten" : "Auswahl"}</h2>{selected && <p>{selectedIndex + 1} von {items.length}</p>}</div>{selected && <div className="record-navigation"><button className="secondary-action" disabled={selectedIndex <= 0} onClick={() => moveSelection(-1)} aria-label="Vorheriger Arbeitseinsatz">←</button><button className="secondary-action" disabled={selectedIndex < 0 || selectedIndex >= items.length - 1} onClick={() => moveSelection(1)} aria-label="Nächster Arbeitseinsatz">→</button></div>}</div>
        {(selected || creating) ? <><fieldset disabled={!canEdit || saving}><label className="wide">Titel *<input value={draft.titel ?? ""} onChange={(e) => set("titel", e.target.value)} /></label><label>Datum *<input type="date" value={draft.datum ?? ""} onChange={(e) => set("datum", e.target.value)} /></label><label>Beginn<input type="time" value={draft.start_uhrzeit ?? ""} onChange={(e) => set("start_uhrzeit", e.target.value)} /></label><label>Ende<input type="time" value={draft.end_uhrzeit ?? ""} onChange={(e) => set("end_uhrzeit", e.target.value)} /></label><label>Treffpunkt<input value={draft.treffpunkt ?? ""} onChange={(e) => set("treffpunkt", e.target.value)} /></label><label>Max. Teilnehmer<input type="number" min="1" value={draft.max_teilnehmer ?? ""} onChange={(e) => set("max_teilnehmer", e.target.value ? Number(e.target.value) : null)} placeholder="unbegrenzt" /></label><label>Stundenwert<input type="number" min="0" step="0.25" value={draft.stunden_wert ?? 0} onChange={(e) => set("stunden_wert", Number(e.target.value))} /></label><label>Sichtbar ab<input type="datetime-local" value={(draft.sichtbar_ab ?? "").slice(0, 16)} onChange={(e) => set("sichtbar_ab", e.target.value)} /></label><label>Sichtbar bis<input type="datetime-local" value={(draft.sichtbar_bis ?? "").slice(0, 16)} onChange={(e) => set("sichtbar_bis", e.target.value)} /></label><label>Anmeldeschluss<input type="datetime-local" value={(draft.anmeldung_bis ?? "").slice(0, 16)} onChange={(e) => set("anmeldung_bis", e.target.value)} /></label><label className="check"><input type="checkbox" checked={draft.aktiv !== false} onChange={(e) => set("aktiv", e.target.checked)} /> Einsatz aktiv</label><label className="wide">Beschreibung<textarea value={draft.beschreibung ?? ""} onChange={(e) => set("beschreibung", e.target.value)} /></label></fieldset>
          {canEdit && <div className="editor-actions assignment-editor-actions"><button disabled={saving} onClick={() => void save(false)}>Speichern</button><button className="secondary-action" disabled={saving} onClick={() => void save(true)}>Speichern + nächste Schicht</button>{selected?.aktiv && <button className="reject-action" disabled={saving} onClick={() => void cancelAssignment()}>Absagen</button>}{selected && <button className="reject-action" disabled={saving} onClick={() => void removeAssignment()}>Löschen</button>}<button className="secondary-action" onClick={() => { setCreating(false); setSelectedId(null); setMessage(""); }}>Schließen</button></div>}
          {selected && canEdit && <WorkAssignmentParticipants session={session} assignment={selected} saisonId={saisonId} />}</> : <p>Wähle links einen Arbeitseinsatz aus oder lege einen neuen an.</p>}
      </aside></div>
  </section>;
}

function WorkAssignmentParticipants({ session, assignment, saisonId }: { session: BrowserSession; assignment: WorkAssignment; saisonId: number | null }) {
  const [registrations, setRegistrations] = useState<WorkAssignmentRegistration[]>([]); const [members, setMembers] = useState<Member[]>([]); const [memberId, setMemberId] = useState(""); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const load = () => Promise.all([readSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", { select: "id,arbeitseinsatz_id,mitglied_id,status,bemerkung,angemeldet_am,updated_at", arbeitseinsatz_id: `eq.${assignment.id}`, order: "angemeldet_am.asc", limit: "500" }), readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", aktiv: "eq.true", order: "name.asc,vorname.asc", limit: "500" })]).then(([nextRegistrations, nextMembers]) => { setRegistrations(nextRegistrations); setMembers(nextMembers); }).catch((cause: Error) => setMessage(cause.message));
  useEffect(() => { load(); }, [session, assignment.id]);
  const activeRegistrations = registrations.filter((item) => item.status === "angemeldet" || item.status === "teilgenommen");
  const capacity = assignment.max_teilnehmer ? Number(assignment.max_teilnehmer) : null;
  const deadlinePassed = Boolean(assignment.anmeldung_bis && assignment.anmeldung_bis.slice(0, 16) < currentLocalDateTime());
  const registrationBlocked = !assignment.aktiv || deadlinePassed || (capacity !== null && activeRegistrations.length >= capacity);
  async function register() { const id = Number(memberId); if (!id) return; if (registrationBlocked) { setMessage(!assignment.aktiv ? "Der Arbeitseinsatz ist abgesagt." : deadlinePassed ? "Der Anmeldeschluss ist abgelaufen." : "Die Teilnehmerbegrenzung ist erreicht."); return; } setSaving(true); setMessage(""); try { const current = registrations.find((item) => item.mitglied_id === id); if (current) await writeSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", "PATCH", { status: "angemeldet" }, { id: `eq.${current.id}` }); else await writeSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", "POST", { arbeitseinsatz_id: assignment.id, mitglied_id: id, status: "angemeldet" }); setMemberId(""); await load(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Anmeldung konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  async function setStatus(item: WorkAssignmentRegistration, status: WorkAssignmentRegistration["status"]) { setSaving(true); setMessage(""); try { await writeSupabase<WorkAssignmentRegistration>(session, "arbeitseinsatz_anmeldung", "PATCH", { status }, { id: `eq.${item.id}` }); if (status === "teilgenommen") { if (!saisonId) throw new Error("Für die Übernahme fehlt die aktive Saison."); await writeSupabase<WorkHour>(session, "arbeitsstunde", "POST", { mitglied_id: item.mitglied_id, saison_id: saisonId, datum: assignment.datum, stunden: assignment.stunden_wert, art_der_arbeit: assignment.titel ?? "Arbeitseinsatz", status: "offen", freigegeben: false }); } await load(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Teilnahme konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  const name = (id: number) => { const member = members.find((item) => item.id === id); return member ? `${member.name ?? ""}, ${member.vorname ?? ""}` : `Mitglied #${id}`; };
  return <section className="assignment-participants"><div className="detail-title"><div><h3>Teilnehmer</h3><p>{activeRegistrations.length}{capacity ? ` von ${capacity}` : " · unbegrenzt"}</p></div></div>{message && <p className="notice">{message}</p>}{registrationBlocked && <p className="context-note">{!assignment.aktiv ? "Der Einsatz ist abgesagt; neue Anmeldungen sind gesperrt." : deadlinePassed ? "Der Anmeldeschluss ist abgelaufen." : "Die maximale Teilnehmerzahl ist erreicht."}</p>}<div className="participant-add"><select value={memberId} disabled={registrationBlocked} onChange={(event) => setMemberId(event.target.value)}><option value="">Mitglied auswählen</option>{members.filter((member) => !registrations.some((item) => item.mitglied_id === member.id && (item.status === "angemeldet" || item.status === "teilgenommen"))).map((member) => <option key={member.id} value={member.id}>{member.name}, {member.vorname}</option>)}</select><button disabled={saving || !memberId || registrationBlocked} onClick={register}>Anmelden</button></div><ul>{registrations.map((item) => <li key={item.id}><span><strong>{name(item.mitglied_id)}</strong><small>{item.status}</small></span><div>{item.status !== "abgesagt" && <button className="secondary-action" disabled={saving} onClick={() => setStatus(item, "abgesagt")}>Abmelden</button>}<button disabled={saving || item.status === "teilgenommen" || item.status === "abgesagt"} onClick={() => setStatus(item, "teilgenommen")}>Teilnahme übernehmen</button></div></li>)}</ul>{registrations.length === 0 && <p>Noch keine Teilnehmer.</p>}</section>;
}

function AppointmentManagement({ session, canEdit, onBack }: { session: BrowserSession; canEdit: boolean; onBack: () => void }) {
  const [items, setItems] = useState<Appointment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [draft, setDraft] = useState<Partial<Appointment>>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);

  function emptyDraft(): Partial<Appointment> {
    const now = currentLocalDateTime();
    const [hours, minutes] = now.slice(11, 16).split(":").map(Number);
    const startMinutes = hours * 60 + minutes;
    const endMinutes = Math.min(startMinutes + 60, 1439);
    const endTime = `${String(Math.floor(endMinutes / 60)).padStart(2, "0")}:${String(endMinutes % 60).padStart(2, "0")}`;
    return { titel: "", beschreibung: "", datum: now.slice(0, 10), start_uhrzeit: now.slice(11, 16), end_uhrzeit: endTime, sichtbar_ab: now, sichtbar_bis: `${now.slice(0, 10)}T23:59`, aktiv: true };
  }

  const load = () => readSupabase<Appointment>(session, "termin", {
    select: "id,titel,beschreibung,datum,start_uhrzeit,end_uhrzeit,sichtbar_ab,sichtbar_bis,aktiv",
    order: "datum.asc",
    limit: "500",
  }).then((rows) => {
    rows.sort((left, right) => `${left.datum}|${left.start_uhrzeit ?? "99:99"}|${left.end_uhrzeit ?? "99:99"}|${left.titel ?? ""}`.localeCompare(`${right.datum}|${right.start_uhrzeit ?? "99:99"}|${right.end_uhrzeit ?? "99:99"}|${right.titel ?? ""}`, "de"));
    setItems(rows);
    return rows;
  }).catch((cause: Error) => { setMessage(cause.message); return [] as Appointment[]; });

  useEffect(() => { void load(); }, [session]);
  const selected = items.find((item) => item.id === selectedId) ?? null;
  const selectedIndex = selected ? items.findIndex((item) => item.id === selected.id) : -1;
  const editLock = useEditLock(session, "termin", selected?.id, Boolean(selected && canEdit && !creating));
  useEffect(() => { if (!creating) setDraft(selected ?? emptyDraft()); }, [selectedId, creating]);
  useEffect(() => { if (editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editLock.message]);
  const set = (key: keyof Appointment, value: string | boolean | null) => setDraft({ ...draft, [key]: value });
  function startNew() { setCreating(true); setSelectedId(null); setDraft(emptyDraft()); setMessage(""); }
  function selectEntry(id: number) { setCreating(false); setSelectedId(id); setMessage(""); }
  function moveSelection(offset: number) { const target = items[selectedIndex + offset]; if (target) selectEntry(target.id); }

  async function save() {
    const title = draft.titel?.trim() ?? "";
    if (!title || !draft.datum) { setMessage("Titel und Datum sind erforderlich."); return; }
    if (draft.start_uhrzeit && draft.end_uhrzeit && draft.end_uhrzeit < draft.start_uhrzeit) { setMessage("Das Terminende darf nicht vor dem Beginn liegen."); return; }
    if (draft.sichtbar_ab && draft.sichtbar_bis && draft.sichtbar_bis < draft.sichtbar_ab) { setMessage("Das Sichtbarkeitsende darf nicht vor dem Beginn liegen."); return; }
    if (!creating && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    setSaving(true); setMessage("");
    try {
      const payload: Omit<Appointment, "id"> = { titel: title, beschreibung: draft.beschreibung?.trim() || null, datum: draft.datum, start_uhrzeit: draft.start_uhrzeit || null, end_uhrzeit: draft.end_uhrzeit || null, sichtbar_ab: draft.sichtbar_ab || null, sichtbar_bis: draft.sichtbar_bis || null, aktiv: draft.aktiv !== false };
      const rows = creating
        ? await writeSupabase<Appointment>(session, "termin", "POST", payload)
        : await writeSupabase<Appointment>(session, "termin", "PATCH", payload, { id: `eq.${selected?.id}` });
      const savedId = rows[0]?.id ?? selected?.id ?? null;
      await load(); setCreating(false); setSelectedId(savedId); setMessage("Termin gespeichert.");
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Termin konnte nicht gespeichert werden."); }
    finally { setSaving(false); }
  }

  async function deactivate() {
    if (!selected) return;
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!window.confirm(`Den Termin „${selected.titel ?? "Ohne Titel"}“ wirklich deaktivieren?`)) return;
    setSaving(true); setMessage("");
    try { await writeSupabase<Appointment>(session, "termin", "PATCH", { aktiv: false }, { id: `eq.${selected.id}` }); await load(); setMessage("Termin wurde deaktiviert."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Termin konnte nicht deaktiviert werden."); }
    finally { setSaving(false); }
  }

  async function remove() {
    if (!selected) return;
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!window.confirm(`Den Termin „${selected.titel ?? "Ohne Titel"}“ endgültig löschen?`)) return;
    setSaving(true); setMessage("");
    try { await deleteSupabase(session, "termin", { id: `eq.${selected.id}` }); setSelectedId(null); setCreating(false); await load(); setMessage("Termin wurde gelöscht."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Termin konnte nicht gelöscht werden."); }
    finally { setSaving(false); }
  }

  return <section className="data-workspace appointment-management">
    <div className="data-toolbar"><div className="editor-actions"><button className="secondary-action" onClick={onBack}>Zur Startseite</button><button className="secondary-action" onClick={() => void load()}>Aktualisieren</button></div><span>{items.length} Termine</span>{canEdit && <button onClick={startNew}>Termin anlegen</button>}</div>
    {message && <p className="notice" role="status">{message}</p>}
    <div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Datum</th><th>Zeit</th><th>Titel</th><th>Sichtbarkeit</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id} className={selected?.id === item.id && !creating ? "selected-row" : ""} onClick={() => selectEntry(item.id)}><td>{formatDate(item.datum)}</td><td>{formatTimeRange(item.start_uhrzeit, item.end_uhrzeit)}</td><td><strong>{item.titel}</strong></td><td>{formatDate(item.sichtbar_ab)} – {formatDate(item.sichtbar_bis)}</td><td>{item.aktiv ? "aktiv" : "inaktiv"}</td></tr>)}</tbody></table>{items.length === 0 && <p className="empty-state">Keine Termine vorhanden.</p>}</div>
      <aside className="detail-panel member-editor"><div className="detail-title"><div><h2>{creating ? "Neuer Termin" : selected ? "Termin bearbeiten" : "Auswahl"}</h2>{selected && <p>{selectedIndex + 1} von {items.length}</p>}</div>{selected && <div className="record-navigation"><button className="secondary-action" disabled={selectedIndex <= 0} onClick={() => moveSelection(-1)} aria-label="Vorheriger Termin">←</button><button className="secondary-action" disabled={selectedIndex < 0 || selectedIndex >= items.length - 1} onClick={() => moveSelection(1)} aria-label="Nächster Termin">→</button></div>}</div>
        {(selected || creating) ? <><fieldset disabled={!canEdit || saving}><label className="wide">Titel *<input value={draft.titel ?? ""} onChange={(event) => set("titel", event.target.value)} /></label><label>Datum *<input type="date" value={draft.datum ?? ""} onChange={(event) => set("datum", event.target.value)} /></label><label>Beginn<input type="time" value={draft.start_uhrzeit ?? ""} onChange={(event) => set("start_uhrzeit", event.target.value)} /></label><label>Ende<input type="time" value={draft.end_uhrzeit ?? ""} onChange={(event) => set("end_uhrzeit", event.target.value)} /></label><label>Sichtbar ab<input type="datetime-local" value={(draft.sichtbar_ab ?? "").slice(0, 16)} onChange={(event) => set("sichtbar_ab", event.target.value)} /></label><label>Sichtbar bis<input type="datetime-local" value={(draft.sichtbar_bis ?? "").slice(0, 16)} onChange={(event) => set("sichtbar_bis", event.target.value)} /></label><label className="wide">Beschreibung<textarea value={draft.beschreibung ?? ""} onChange={(event) => set("beschreibung", event.target.value)} /></label><label className="check"><input type="checkbox" checked={draft.aktiv !== false} onChange={(event) => set("aktiv", event.target.checked)} /> Termin aktiv</label></fieldset>
          {canEdit && <div className="editor-actions"><button disabled={saving} onClick={() => void save()}>Speichern</button>{selected?.aktiv && <button className="reject-action" disabled={saving} onClick={() => void deactivate()}>Deaktivieren</button>}{selected && <button className="reject-action" disabled={saving} onClick={() => void remove()}>Löschen</button>}<button className="secondary-action" onClick={() => { setCreating(false); setSelectedId(null); setMessage(""); }}>Schließen</button></div>}</> : <p>Wähle links einen Termin aus oder lege einen neuen an.</p>}
      </aside></div>
  </section>;
}

function AnnouncementManagement({ session, canEdit, onBack }: { session: BrowserSession; canEdit: boolean; onBack: () => void }) {
  const [items, setItems] = useState<Announcement[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [preview, setPreview] = useState(false);
  const [draft, setDraft] = useState<Partial<Announcement>>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const htmlEditorRef = useRef<HTMLTextAreaElement | null>(null);

  function emptyDraft(): Partial<Announcement> {
    const visibleFrom = currentLocalDateTime();
    const visibleUntil = addBerlinMonths(visibleFrom, 1);
    return { titel: "", inhalt_html: "<p></p>", sichtbar_ab: visibleFrom, sichtbar_bis: visibleUntil, sort_order: null, aktiv: true };
  }

  const load = () => readSupabase<Announcement>(session, "bekanntmachung", {
    select: "id,titel,inhalt_html,sichtbar_ab,sichtbar_bis,sort_order,aktiv",
    order: "sort_order.asc",
    limit: "500",
  }).then((rows) => {
    rows.sort((left, right) => {
      const orderDifference = (left.sort_order ?? Number.MAX_SAFE_INTEGER) - (right.sort_order ?? Number.MAX_SAFE_INTEGER);
      if (orderDifference) return orderDifference;
      return String(right.sichtbar_ab ?? "").localeCompare(String(left.sichtbar_ab ?? "")) || String(left.titel ?? "").localeCompare(String(right.titel ?? ""), "de");
    });
    setItems(rows);
    return rows;
  }).catch((cause: Error) => { setMessage(cause.message); return [] as Announcement[]; });

  useEffect(() => { void load(); }, [session]);
  const selected = items.find((item) => item.id === selectedId) ?? null;
  const selectedIndex = selected ? items.findIndex((item) => item.id === selected.id) : -1;
  const editLock = useEditLock(session, "bekanntmachung", selected?.id, Boolean(selected && canEdit && !creating));
  useEffect(() => { if (!creating) setDraft(selected ?? emptyDraft()); setPreview(false); }, [selectedId, creating]);
  useEffect(() => { if (editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editLock.message]);
  const set = (key: keyof Announcement, value: string | number | boolean | null) => setDraft({ ...draft, [key]: value });
  function startNew() { setCreating(true); setSelectedId(null); setDraft(emptyDraft()); setPreview(false); setMessage(""); }
  function selectEntry(id: number) { setCreating(false); setSelectedId(id); setPreview(false); setMessage(""); }
  function moveSelection(offset: number) { const target = items[selectedIndex + offset]; if (target) selectEntry(target.id); }
  function insertSnippet(snippet: string) {
    const editor = htmlEditorRef.current;
    const existing = String(draft.inhalt_html ?? "");
    const start = editor?.selectionStart ?? existing.length;
    const end = editor?.selectionEnd ?? start;
    const next = `${existing.slice(0, start)}${snippet}${existing.slice(end)}`;
    set("inhalt_html", next);
    queueMicrotask(() => { if (editor) { editor.focus(); editor.setSelectionRange(start + snippet.length, start + snippet.length); } });
  }
  const previewDocument = `<!doctype html><html lang="de"><head><meta charset="utf-8"><meta http-equiv="Content-Security-Policy" content="default-src 'none'; img-src data:; style-src 'unsafe-inline'"><meta name="viewport" content="width=device-width,initial-scale=1"><style>body{font:16px/1.55 system-ui,sans-serif;color:#172016;margin:24px;overflow-wrap:anywhere}a{color:#52601c}img{max-width:100%;height:auto}table{border-collapse:collapse;max-width:100%}td,th{border:1px solid #ccd2c4;padding:6px}</style></head><body>${String(draft.inhalt_html ?? "")}</body></html>`;

  async function save() {
    const title = draft.titel?.trim() ?? "";
    const html = draft.inhalt_html?.trim() ?? "";
    const sortOrder = draft.sort_order === null || draft.sort_order === undefined ? null : Number(draft.sort_order);
    if (!title) { setMessage("Titel ist erforderlich."); return; }
    if (!html) { setMessage("HTML-Inhalt ist erforderlich."); setPreview(false); return; }
    if (draft.sichtbar_ab && draft.sichtbar_bis && draft.sichtbar_bis < draft.sichtbar_ab) { setMessage("Das Sichtbarkeitsende darf nicht vor dem Beginn liegen."); return; }
    if (sortOrder !== null && !Number.isInteger(sortOrder)) { setMessage("Die Sortierreihenfolge muss eine ganze Zahl sein."); return; }
    if (!creating && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    setSaving(true); setMessage("");
    try {
      const payload: Omit<Announcement, "id"> = { titel: title, inhalt_html: html, sichtbar_ab: draft.sichtbar_ab || null, sichtbar_bis: draft.sichtbar_bis || null, sort_order: sortOrder, aktiv: draft.aktiv !== false };
      const rows = creating
        ? await writeSupabase<Announcement>(session, "bekanntmachung", "POST", payload)
        : await writeSupabase<Announcement>(session, "bekanntmachung", "PATCH", payload, { id: `eq.${selected?.id}` });
      const savedId = rows[0]?.id ?? selected?.id ?? null;
      await load(); setCreating(false); setSelectedId(savedId); setMessage("Bekanntmachung gespeichert.");
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Bekanntmachung konnte nicht gespeichert werden."); }
    finally { setSaving(false); }
  }

  async function deactivate() {
    if (!selected) return;
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!window.confirm(`Die Bekanntmachung „${selected.titel ?? "Ohne Titel"}“ wirklich deaktivieren?`)) return;
    setSaving(true); setMessage("");
    try { await writeSupabase<Announcement>(session, "bekanntmachung", "PATCH", { aktiv: false }, { id: `eq.${selected.id}` }); await load(); setMessage("Bekanntmachung wurde deaktiviert."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Bekanntmachung konnte nicht deaktiviert werden."); }
    finally { setSaving(false); }
  }

  async function remove() {
    if (!selected) return;
    if (!editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; }
    if (!window.confirm(`Die Bekanntmachung „${selected.titel ?? "Ohne Titel"}“ endgültig löschen?`)) return;
    setSaving(true); setMessage("");
    try { await deleteSupabase(session, "bekanntmachung", { id: `eq.${selected.id}` }); setSelectedId(null); setCreating(false); await load(); setMessage("Bekanntmachung wurde gelöscht."); }
    catch (cause) { setMessage(cause instanceof Error ? cause.message : "Bekanntmachung konnte nicht gelöscht werden."); }
    finally { setSaving(false); }
  }

  return <section className="data-workspace announcement-management">
    <div className="data-toolbar"><div className="editor-actions"><button className="secondary-action" onClick={onBack}>Zur Startseite</button><button className="secondary-action" onClick={() => void load()}>Aktualisieren</button></div><span>{items.length} Bekanntmachungen</span>{canEdit && <button onClick={startNew}>Bekanntmachung anlegen</button>}</div>
    {message && <p className="notice" role="status">{message}</p>}
    <div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Sortierung</th><th>Titel</th><th>Sichtbar</th><th>Status</th></tr></thead><tbody>{items.map((item) => <tr key={item.id} className={selected?.id === item.id && !creating ? "selected-row" : ""} onClick={() => selectEntry(item.id)}><td>{item.sort_order ?? "–"}</td><td><strong>{item.titel}</strong><small>{plainText(item.inhalt_html).slice(0, 100)}</small></td><td>{formatDate(item.sichtbar_ab)} – {formatDate(item.sichtbar_bis)}</td><td>{item.aktiv ? "aktiv" : "inaktiv"}</td></tr>)}</tbody></table>{items.length === 0 && <p className="empty-state">Keine Bekanntmachungen vorhanden.</p>}</div>
      <aside className="detail-panel member-editor"><div className="detail-title"><div><h2>{creating ? "Neue Bekanntmachung" : selected ? "Bekanntmachung bearbeiten" : "Auswahl"}</h2>{selected && <p>{selectedIndex + 1} von {items.length}</p>}</div>{selected && <div className="record-navigation"><button className="secondary-action" disabled={selectedIndex <= 0} onClick={() => moveSelection(-1)} aria-label="Vorherige Bekanntmachung">←</button><button className="secondary-action" disabled={selectedIndex < 0 || selectedIndex >= items.length - 1} onClick={() => moveSelection(1)} aria-label="Nächste Bekanntmachung">→</button></div>}</div>
        {(selected || creating) ? <><fieldset disabled={!canEdit || saving}><label className="wide">Titel *<input value={draft.titel ?? ""} onChange={(event) => set("titel", event.target.value)} /></label><div className="wide html-editor-tabs"><button type="button" className={!preview ? "active" : "secondary-action"} onClick={() => setPreview(false)}>HTML</button><button type="button" className={preview ? "active" : "secondary-action"} onClick={() => setPreview(true)}>Vorschau</button></div>{!preview ? <div className="wide html-editor-area"><div className="html-snippets"><button type="button" onClick={() => insertSnippet("<p>Text</p>")}>Absatz</button><button type="button" onClick={() => insertSnippet("<h3>Überschrift</h3>")}>Überschrift</button><button type="button" onClick={() => insertSnippet("<strong>Betonung</strong>")}>Fett</button><button type="button" onClick={() => insertSnippet('<a href="https://">Linktext</a>')}>Link</button><button type="button" onClick={() => insertSnippet("<ul>\n  <li>Punkt 1</li>\n  <li>Punkt 2</li>\n</ul>")}>Liste</button></div><label>Inhalt (HTML) *<textarea ref={htmlEditorRef} value={draft.inhalt_html ?? ""} onChange={(event) => set("inhalt_html", event.target.value)} /></label></div> : <div className="wide announcement-preview"><iframe title="Sichere Vorschau der Bekanntmachung" sandbox="" srcDoc={previewDocument} /></div>}<label>Sichtbar ab<input type="datetime-local" value={(draft.sichtbar_ab ?? "").slice(0, 16)} onChange={(event) => set("sichtbar_ab", event.target.value)} /></label><label>Sichtbar bis<input type="datetime-local" value={(draft.sichtbar_bis ?? "").slice(0, 16)} onChange={(event) => set("sichtbar_bis", event.target.value)} /></label><label>Sortierreihenfolge<input type="number" step="1" value={draft.sort_order ?? ""} onChange={(event) => set("sort_order", event.target.value ? Number(event.target.value) : null)} /></label><label className="check"><input type="checkbox" checked={draft.aktiv !== false} onChange={(event) => set("aktiv", event.target.checked)} /> Bekanntmachung aktiv</label></fieldset>
          {canEdit && <div className="editor-actions"><button disabled={saving} onClick={() => void save()}>Speichern</button>{selected?.aktiv && <button className="reject-action" disabled={saving} onClick={() => void deactivate()}>Deaktivieren</button>}{selected && <button className="reject-action" disabled={saving} onClick={() => void remove()}>Löschen</button>}<button className="secondary-action" onClick={() => { setCreating(false); setSelectedId(null); setMessage(""); }}>Schließen</button></div>}</> : <p>Wähle links eine Bekanntmachung aus oder lege eine neue an.</p>}
      </aside></div>
  </section>;
}

function AssociationManagement({ session, section }: { session: BrowserSession; section: "einsatz" | "termin" | "bekanntmachung" }) {
  const [items, setItems] = useState<Array<WorkAssignment | Appointment | Announcement>>([]);
  const [error, setError] = useState("");
  useEffect(() => {
    const source = section === "einsatz" ? ["arbeitseinsatz", "id,titel,beschreibung,datum,start_uhrzeit,treffpunkt,stunden_wert,aktiv"] as const : section === "termin" ? ["termin", "id,titel,beschreibung,datum,start_uhrzeit,aktiv"] as const : ["bekanntmachung", "id,titel,inhalt_html,sichtbar_ab,sichtbar_bis,aktiv"] as const;
    readSupabase<WorkAssignment | Appointment | Announcement>(session, source[0], { select: source[1], order: section === "bekanntmachung" ? "sichtbar_ab.desc" : "datum.asc", limit: "500" }).then(setItems).catch((cause: Error) => setError(cause.message));
  }, [session, section]);
  const isAnnouncement = section === "bekanntmachung";
  return <section className="data-workspace">{error ? <p className="notice">{error}</p> : <div className="management-list">{items.map((item) => { const dated = item as WorkAssignment | Appointment; const announcement = item as Announcement; const description = isAnnouncement ? plainText(announcement.inhalt_html) : dated.beschreibung; return <article key={item.id} className="management-card"><div><h2>{item.titel ?? "Ohne Titel"}</h2><p>{description || "Keine Beschreibung hinterlegt."}</p></div><aside>{isAnnouncement ? <><strong>{announcement.aktiv ? "aktiv" : "inaktiv"}</strong><span>{formatDate(announcement.sichtbar_ab)}{announcement.sichtbar_bis ? ` – ${formatDate(announcement.sichtbar_bis)}` : ""}</span></> : <><strong>{formatDate(dated.datum)}</strong><span>{dated.start_uhrzeit?.slice(0, 5) ?? ""}{"treffpunkt" in dated && dated.treffpunkt ? ` · ${dated.treffpunkt}` : ""}</span></>}</aside></article>; })}</div>}{items.length === 0 && !error && <p className="empty-state">Keine Einträge vorhanden.</p>}<p className="detail-hint">Die Liste verwendet die bestehenden Verwaltungsdaten. Erstellen und Bearbeiten werden als nächster, schreibender Arbeitsschritt ergänzt.</p></section>;
}

function plainText(value: string | null) {
  return (value ?? "").replace(/<[^>]*>/g, " ").replace(/\s+/g, " ").trim();
}

function MaintenanceContracts({ session, memberId, canManage }: { session: BrowserSession; memberId?: number; canManage: boolean }) {
  const [contracts, setContracts] = useState<MaintenanceContract[]>([]); const [assignments, setAssignments] = useState<MaintenanceAssignment[]>([]); const [members, setMembers] = useState<Member[]>([]); const [parcels, setParcels] = useState<Parcel[]>([]); const [parcelAssignments, setParcelAssignments] = useState<ParcelAssignment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null); const [editing, setEditing] = useState(false); const [draft, setDraft] = useState<MaintenanceContract | null>(null); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const [assignMemberId, setAssignMemberId] = useState(memberId ? String(memberId) : ""); const [assignContractId, setAssignContractId] = useState(""); const [validFrom, setValidFrom] = useState(new Date().toISOString().slice(0, 10)); const [assignmentNote, setAssignmentNote] = useState(""); const [endDates, setEndDates] = useState<Record<number, string>>({});
  const today = new Date().toISOString().slice(0, 10);
  async function load() {
    setMessage("");
    try {
      const assignmentQuery: Record<string, string> = memberId && !canManage ? { hauptmitglied_id: `eq.${memberId}` } : {};
      const [loadedContracts, loadedAssignments, loadedMembers, loadedParcels, loadedParcelAssignments] = await Promise.all([
        readSupabase<MaintenanceContract>(session, "wartungsvertraege", { select: "id,titel,beschreibung,bereich,max_aktive_zuordnungen,befreit_von_pflichtstunden,aktiv,bemerkung,is_demo", is_demo: "eq.false", order: "titel.asc", limit: "500" }),
        readSupabase<MaintenanceAssignment>(session, "wartungsvertrag_zuordnungen", { select: "id,wartungsvertrag_id,hauptmitglied_id,gueltig_ab,gueltig_bis,bemerkung", ...assignmentQuery, order: "gueltig_ab.desc", limit: "2000" }),
        readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", aktiv: "eq.true", order: "name.asc,vorname.asc", limit: "1000" }),
        readSupabase<Parcel>(session, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", limit: "1000" }),
        readSupabase<ParcelAssignment>(session, "parzellen_belegung", { select: "id,parzelle_id,mitglied_id,von_datum,bis_datum", limit: "2000" }),
      ]);
      setContracts(loadedContracts); setAssignments(loadedAssignments); setMembers(loadedMembers); setParcels(loadedParcels); setParcelAssignments(loadedParcelAssignments);
      const permittedIds = memberId ? new Set(loadedAssignments.filter((item) => item.hauptmitglied_id === memberId).map((item) => item.wartungsvertrag_id)) : null;
      const selectable = permittedIds ? loadedContracts.filter((item) => permittedIds.has(item.id)) : loadedContracts;
      setSelectedId((current) => current && selectable.some((item) => item.id === current) ? current : selectable[0]?.id ?? null);
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Wartungsverträge konnten nicht geladen werden."); }
  }
  useEffect(() => { setAssignMemberId(memberId ? String(memberId) : ""); void load(); }, [session, memberId]);
  const isActive = (item: MaintenanceAssignment) => item.gueltig_ab <= today && (!item.gueltig_bis || item.gueltig_bis >= today);
  const activeAssignments = assignments.filter(isActive);
  const memberAssignments = memberId ? assignments.filter((assignment) => assignment.hauptmitglied_id === memberId) : assignments;
  const visibleContracts = memberId ? contracts.filter((contract) => memberAssignments.some((assignment) => assignment.wartungsvertrag_id === contract.id)) : contracts;
  const selected = contracts.find((item) => item.id === selectedId) ?? null;
  const editLock = useEditLock(session, "wartungsvertraege", draft?.id, Boolean(editing && draft?.id));
  const selectedAssignments = selected ? memberAssignments.filter((item) => item.wartungsvertrag_id === selected.id) : [];
  const occupancy = (contractId: number) => activeAssignments.filter((item) => item.wartungsvertrag_id === contractId).length;
  const assignableForMember = memberId ? contracts.filter((contract) => contract.aktiv && occupancy(contract.id) < contract.max_aktive_zuordnungen && !activeAssignments.some((item) => item.hauptmitglied_id === memberId && item.wartungsvertrag_id === contract.id)) : [];
  const memberName = (id: number) => { const member = members.find((item) => item.id === id); return member ? `${member.name ?? ""}, ${member.vorname ?? ""}`.replace(/^, |, $/g, "") || `Mitglied #${id}` : `Mitglied #${id}`; };
  const memberContext = (id: number) => members.find((item) => item.id === id)?.hauptmitglied_id ? "Nebenmitglied" : "Hauptmitglied";
  const gardenNumbers = (id: number) => { const parcelIds = parcelAssignments.filter((item) => item.mitglied_id === id && (!item.bis_datum || item.bis_datum >= today)).map((item) => item.parzelle_id); return parcels.filter((item) => parcelIds.includes(item.id)).map((item) => item.garten_nr).filter(Boolean).join(", ") || "–"; };
  function startNew() { setDraft({ id: 0, titel: "", beschreibung: "", bereich: "", max_aktive_zuordnungen: 1, befreit_von_pflichtstunden: true, aktiv: true, bemerkung: "", is_demo: false }); setEditing(true); setMessage(""); }
  function startEdit() { if (selected) { setDraft({ ...selected }); setEditing(true); setMessage(""); } }
  useEffect(() => { if (editing && editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editing, editLock.message]);
  async function saveContract() { if (!draft?.titel.trim() || draft.max_aktive_zuordnungen < 1) { setMessage("Titel und ein Kontingent größer als 0 sind erforderlich."); return; } if (draft.id && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; } setSaving(true); setMessage(""); const payload = { titel: draft.titel.trim(), beschreibung: draft.beschreibung?.trim() || null, bereich: draft.bereich?.trim() || null, max_aktive_zuordnungen: draft.max_aktive_zuordnungen, befreit_von_pflichtstunden: draft.befreit_von_pflichtstunden, aktiv: draft.aktiv, bemerkung: draft.bemerkung?.trim() || null, is_demo: false }; try { if (draft.id) await writeSupabase<MaintenanceContract>(session, "wartungsvertraege", "PATCH", payload, { id: `eq.${draft.id}` }); else { const created = await writeSupabase<MaintenanceContract>(session, "wartungsvertraege", "POST", payload); setSelectedId(created[0]?.id ?? null); } setEditing(false); await load(); setMessage("Wartungsvertrag gespeichert."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Wartungsvertrag konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  async function assign() { const contractId = memberId ? Number(assignContractId) : selected?.id; const targetMember = Number(assignMemberId); if (!contractId || !targetMember || !validFrom) { setMessage("Vertrag, Mitglied und Gültigkeitsbeginn sind erforderlich."); return; } setSaving(true); setMessage(""); try { await writeSupabase<MaintenanceAssignment>(session, "wartungsvertrag_zuordnungen", "POST", { wartungsvertrag_id: contractId, hauptmitglied_id: targetMember, gueltig_ab: validFrom, gueltig_bis: null, bemerkung: assignmentNote.trim() || null }); setAssignmentNote(""); setAssignContractId(""); await load(); setMessage("Wartungsvertrag wurde zugeordnet."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Zuordnung konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  async function endAssignment(item: MaintenanceAssignment) { const endDate = endDates[item.id] || today; if (endDate < item.gueltig_ab) { setMessage("Das Enddatum darf nicht vor dem Beginn liegen."); return; } setSaving(true); setMessage(""); try { await writeSupabase<MaintenanceAssignment>(session, "wartungsvertrag_zuordnungen", "PATCH", { gueltig_bis: endDate }, { id: `eq.${item.id}` }); await load(); setMessage("Zuordnung wurde beendet."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Zuordnung konnte nicht beendet werden."); } finally { setSaving(false); } }
  if (editing && draft) return <section className="data-workspace maintenance-editor"><div className="detail-title"><div><h2>{draft.id ? "Wartungsvertrag bearbeiten" : "Wartungsvertrag anlegen"}</h2><p>Titel, Kontingent, Befreiung und Aktivstatus</p></div></div>{message && <p className="notice">{message}</p>}<fieldset disabled={saving}><label>Titel<input value={draft.titel} onChange={(event) => setDraft({ ...draft, titel: event.target.value })} /></label><label>Bereich<input value={draft.bereich ?? ""} onChange={(event) => setDraft({ ...draft, bereich: event.target.value })} placeholder="z. B. Wasseranlage" /></label><label>Max. aktive Zuordnungen<input type="number" min="1" value={draft.max_aktive_zuordnungen} onChange={(event) => setDraft({ ...draft, max_aktive_zuordnungen: Math.max(1, Number(event.target.value) || 1) })} /></label><label className="check"><input type="checkbox" checked={draft.befreit_von_pflichtstunden} onChange={(event) => setDraft({ ...draft, befreit_von_pflichtstunden: event.target.checked })} /> Befreit von Pflichtstunden</label><label className="wide">Beschreibung<textarea value={draft.beschreibung ?? ""} onChange={(event) => setDraft({ ...draft, beschreibung: event.target.value })} /></label><label className="wide">Bemerkung<textarea value={draft.bemerkung ?? ""} onChange={(event) => setDraft({ ...draft, bemerkung: event.target.value })} /></label><label className="check"><input type="checkbox" checked={draft.aktiv} onChange={(event) => setDraft({ ...draft, aktiv: event.target.checked })} /> Vertrag aktiv</label></fieldset><div className="editor-actions"><button disabled={saving} onClick={saveContract}>{saving ? "Speichert …" : "Speichern"}</button><button className="secondary-action" onClick={() => { setEditing(false); setDraft(null); }}>Abbrechen</button></div></section>;
  return <section className="data-workspace maintenance-workspace" aria-label={memberId ? "Wartungsverträge des Mitglieds" : "Wartungsvertragsverwaltung"}>{message && <p className="notice" role="status">{message}</p>}<div className="data-toolbar"><span>{memberId ? `${visibleContracts.length} zugeordnete Verträge` : `${contracts.length} Wartungsverträge`}</span>{canManage && !memberId && <button onClick={startNew}>Wartungsvertrag anlegen</button>}</div>{canManage && memberId && <div className="maintenance-member-assign"><strong>Wartungsvertrag zuweisen</strong><label>Freier Vertrag<select value={assignContractId} onChange={(event) => setAssignContractId(event.target.value)}><option value="">Vertrag wählen</option>{assignableForMember.map((contract) => <option key={contract.id} value={contract.id}>{contract.titel} · {occupancy(contract.id)} von {contract.max_aktive_zuordnungen}</option>)}</select></label><label>Gültig ab<input type="date" value={validFrom} onChange={(event) => setValidFrom(event.target.value)} /></label><label>Bemerkung<input value={assignmentNote} onChange={(event) => setAssignmentNote(event.target.value)} /></label><button disabled={saving || !assignContractId} onClick={assign}>Zuordnung speichern</button></div>}<div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Titel</th><th>Bereich</th><th>Belegung</th><th>Status</th></tr></thead><tbody>{visibleContracts.map((contract) => { const occupied = occupancy(contract.id); return <tr key={contract.id} className={selectedId === contract.id ? "selected-row" : ""} onClick={() => setSelectedId(contract.id)}><td><strong>{contract.titel}</strong><small>{contract.beschreibung || "Keine Beschreibung"}</small></td><td>{contract.bereich || "–"}</td><td>{occupied} von {contract.max_aktive_zuordnungen}</td><td>{contract.aktiv ? "aktiv" : "inaktiv"}</td></tr>; })}</tbody></table>{visibleContracts.length === 0 && <p className="empty-state">{memberId ? "Diesem Mitglied ist kein Wartungsvertrag zugeordnet." : "Keine Wartungsverträge vorhanden."}</p>}</div>{selected ? <aside className="detail-panel maintenance-detail"><div className="detail-title"><div><h2>{selected.titel}</h2><p>{selected.bereich || "Kein Bereich"}</p></div>{canManage && <button onClick={startEdit}>Bearbeiten</button>}</div><p>{selected.beschreibung || "Keine Beschreibung hinterlegt."}</p><div className="maintenance-summary"><div><span>Kontingent</span><strong>{selected.max_aktive_zuordnungen}</strong></div><div><span>Belegt</span><strong>{occupancy(selected.id)}</strong></div><div><span>Frei</span><strong>{Math.max(0, selected.max_aktive_zuordnungen - occupancy(selected.id))}</strong></div></div><p className="context-note">{selected.befreit_von_pflichtstunden ? "Zuordnung befreit von Pflichtstunden." : "Keine Befreiung von Pflichtstunden."}</p>{selected.bemerkung && <p>{selected.bemerkung}</p>}<h3>Zuordnungen</h3><div className="maintenance-assignments">{selectedAssignments.map((item) => <article key={item.id}><div><strong>{memberName(item.hauptmitglied_id)}</strong><span>{memberContext(item.hauptmitglied_id)} · Garten {gardenNumbers(item.hauptmitglied_id)}</span><span>{formatDate(item.gueltig_ab)} – {item.gueltig_bis ? formatDate(item.gueltig_bis) : "aktiv"}</span>{item.bemerkung && <small>{item.bemerkung}</small>}</div>{canManage && isActive(item) && <div><input type="date" value={endDates[item.id] || today} min={item.gueltig_ab} onChange={(event) => setEndDates({ ...endDates, [item.id]: event.target.value })} /><button className="secondary-action" disabled={saving} onClick={() => endAssignment(item)}>Beenden</button></div>}</article>)}{selectedAssignments.length === 0 && <p>Keine Zuordnungen vorhanden.</p>}</div>{canManage && selected.aktiv && !memberId && <div className="maintenance-assignment-form"><h3>Mitglied zuordnen</h3><label>Mitglied<select value={assignMemberId} onChange={(event) => setAssignMemberId(event.target.value)}><option value="">Mitglied wählen</option>{members.map((member) => <option key={member.id} value={member.id}>{member.name}, {member.vorname} · #{member.id}</option>)}</select></label><label>Gültig ab<input type="date" value={validFrom} onChange={(event) => setValidFrom(event.target.value)} /></label><label className="wide">Bemerkung<input value={assignmentNote} onChange={(event) => setAssignmentNote(event.target.value)} /></label><button disabled={saving || occupancy(selected.id) >= selected.max_aktive_zuordnungen} onClick={assign}>{occupancy(selected.id) >= selected.max_aktive_zuordnungen ? "Kontingent belegt" : "Zuordnung speichern"}</button></div>}</aside> : <aside className="detail-panel"><h2>Wartungsvertrag</h2><p>Wähle links einen Vertrag aus.</p></aside>}</div></section>;
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

function DocumentList({ session, memberId, parcelId, compact = false, canManage = false, leaseContractIntent, onLeaseContractIntentHandled }: { session: BrowserSession; memberId?: number; parcelId?: number; compact?: boolean; canManage?: boolean; leaseContractIntent?: { parcelId: number; startDate: string } | null; onLeaseContractIntentHandled?: () => void }) {
  const [items, setItems] = useState<DocumentRecord[]>([]);
  const [error, setError] = useState("");
  const [opening, setOpening] = useState<number | null>(null);
  const [file, setFile] = useState<File | null>(null);
  const [title, setTitle] = useState("");
  const [uploading, setUploading] = useState(false);
  const [archiving, setArchiving] = useState<number | null>(null);
  const ownerKind = memberId ? "mitglied" : "parzelle";
  const ownerId = memberId ?? parcelId;
  async function load() { const ownerQuery: Record<string, string> | null = memberId ? { mitglied_id: `eq.${memberId}` } : parcelId ? { parzelle_id: `eq.${parcelId}` } : null; if (!ownerQuery) { setItems([]); return; } try { setItems(await readSupabase<DocumentRecord>(session, "dokument", { select: "id,mitglied_id,parzelle_id,bucket,storage_path,drive_file_id,titel,dateiname,mime_type,size_bytes,updated_at,archiviert_at", ...ownerQuery, order: "updated_at.desc", limit: "500" })); } catch (cause) { setError(cause instanceof Error ? cause.message : "Dokumente konnten nicht geladen werden."); } }
  useEffect(() => { void load(); }, [session, memberId, parcelId]);
  useEffect(() => { if (leaseContractIntent) onLeaseContractIntentHandled?.(); }, [leaseContractIntent, onLeaseContractIntentHandled]);
  async function openDocument(item: DocumentRecord) {
    setOpening(item.id); setError("");
    try { const documentUrl = item.drive_file_id ? await openDriveDocument(session, item.id) : item.bucket && item.storage_path ? await createDocumentOpenUrl(session, item.bucket, item.storage_path) : null; if (!documentUrl) throw new Error("Für dieses Dokument fehlt eine sichere Ablage."); window.open(documentUrl, "_blank", "noopener,noreferrer"); } catch (cause) { setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht geöffnet werden."); } finally { setOpening(null); }
  }
  async function upload() { if (!file || !ownerId || !title.trim()) { setError("Titel und Datei sind erforderlich."); return; } setUploading(true); setError(""); try { const uploaded = await uploadDocument(session, file, { kind: ownerKind, id: ownerId, title: title.trim() }); await writeSupabase<DocumentRecord>(session, "dokument", "POST", { mitglied_id: memberId ?? null, parzelle_id: parcelId ?? null, bucket: "dokumente", storage_path: uploaded.storagePath, drive_file_id: uploaded.driveFileId, titel: title.trim(), dateiname: uploaded.fileName, mime_type: uploaded.mimeType, size_bytes: uploaded.sizeBytes }); setFile(null); setTitle(""); await load(); } catch (cause) { setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht gespeichert werden."); } finally { setUploading(false); } }
  async function archive(item: DocumentRecord) { const reason = window.prompt("Begründung für die Archivierung (mindestens 3 Zeichen):"); if (!reason) return; const password = window.prompt("Archivpasswort:"); if (!password) return; setArchiving(item.id); setError(""); try { await archiveDocument(session, item.id, password, reason); await load(); } catch (cause) { setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht archiviert werden."); } finally { setArchiving(null); } }
  const activeItems = items.filter((item) => !item.archiviert_at);
  return <section className={compact ? "parcel-documents" : "data-workspace"} aria-label="Dokumente">{compact ? <h3>Parzellen-Dokumente</h3> : <p className="document-intro">Dokumente werden ausschließlich über den geschützten Vereins-Dokumentendienst geöffnet. Die Browser-App speichert keine Dokumentkopie lokal.</p>}{error && <p className="notice" role="alert">{error}</p>}{canManage && memberId && <ContractComposer session={session} memberId={memberId} onSaved={load} leaseContractIntent={leaseContractIntent} />}{canManage && <fieldset className="document-upload" disabled={uploading}><label>Titel<input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="z. B. Pachtvertrag 2026" /></label><label>Datei<input type="file" accept="application/pdf,image/*,.doc,.docx,.odt" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /></label><button type="button" onClick={upload}>{uploading ? "Lädt hoch …" : "Dokument hochladen"}</button></fieldset>}<div className="data-table-wrap"><table><thead><tr><th>Titel</th><th>Datei</th><th>Geändert</th><th>Größe</th><th></th></tr></thead><tbody>{activeItems.map((item) => <tr key={item.id}><td><strong>{item.titel ?? "Ohne Titel"}</strong></td><td>{item.dateiname ?? "–"}</td><td>{formatDate(item.updated_at)}</td><td>{formatBytes(item.size_bytes)}</td><td><button className="table-action" disabled={(!item.drive_file_id && (!item.bucket || !item.storage_path)) || opening === item.id} onClick={() => openDocument(item)}>{opening === item.id ? "Öffne …" : item.mime_type === "application/pdf" ? "Vorschau" : "Öffnen"}</button>{canManage && <button className="table-action secondary-action" disabled={archiving === item.id} onClick={() => archive(item)}>{archiving === item.id ? "Archiviert …" : "Archivieren"}</button>}</td></tr>)}</tbody></table>{activeItems.length === 0 && <p className="empty-state">Keine aktiven Dokumente vorhanden.</p>}</div>{!compact && <p className="detail-hint">Mitgliedsantrag und Pachtvertrag verwenden die offiziellen PDF-Vorlagen. Vorschau, Unterschriften und sichere Ablage erfolgen über den geschützten Server-Dienst.</p>}</section>;
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

function formatBytes(value: number | null) {
  if (!value) return "–";
  return value < 1024 * 1024 ? `${Math.round(value / 1024)} KB` : `${(value / (1024 * 1024)).toFixed(1)} MB`;
}
