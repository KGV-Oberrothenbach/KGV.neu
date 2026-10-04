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
  openMeterPhoto,
  readSupabase,
  sendPasswordReset,
  uploadDocument,
  uploadMeterPhoto,
  writeSupabase,
} from "../lib/supabase-auth";
import { type ClubContext } from "../models/auth/club";
import { AuthProvider, useAuth } from "../features/auth/AuthProvider";
import { ChangeClubAction } from "../features/auth/ChangeClubAction";
import { ClubSelection } from "../features/auth/ClubSelection";
import { LoginForm } from "../features/auth/LoginForm";
import { OtpFlow } from "../features/auth/OtpFlow";
import { enqueueMeterPhoto, listPendingMeterPhotos, ndefReaderConstructor, pendingPhotoFile, putPendingMeterPhoto, removePendingMeterPhoto, type PendingMeterPhoto } from "../lib/browser-media";
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
type Member = { id: number; vorname: string | null; name: string | null; email: string | null; aktiv: boolean; hauptmitglied_id: number | null; auth_user_id?: string | null; geburtsdatum: string | null; adresse: string | null; plz: string | null; ort: string | null; telefon: string | null; handy: string | null; whatsapp_einwilligung: boolean; mitglied_seit: string | null; mitglied_ende: string | null; bemerkung: string | null };
type Parcel = { id: number; garten_nr: string; Anlage: string; flaeche_qm: number | null; hat_strom: boolean; hat_wasser: boolean; aktiv: boolean };
type ParcelAssignment = { id: number; parzelle_id: number; mitglied_id: number; von_datum: string | null; bis_datum: string | null };
type Meter = { id: number; parzelle_id: number; medium: string; zaehlernummer: string; eingebaut_am: string; ausgebaut_am: string | null; eichfaellig_am: string; status: string | null };
type Reading = { id: number; zaehler_id: number; stand: number; ablesedatum: string; art: string; freigegeben: boolean; pruefstatus: string; pruefkommentar: string | null; geprueft_von: number | null; geprueft_am: string | null; foto_pfad?: string | null; foto_drive_file_id?: string | null; foto_dateiname?: string | null };
type RfidScanContext = { parzelle_id: number; anlage: string | null; garten_nr: string | null; medium: string | null; rfid_tag_uid: string | null; aktiver_zaehler_id: number | null; zaehlernummer: string | null; status: string | null };
type WorkHour = { id: number; mitglied_id: number; saison_id: number; datum: string; stunden: number; art_der_arbeit: string; status: string | null; freigegeben: boolean; genehmigt_von: number | null; genehmigt_am: string | null };
type WorkHourHistory = { id: number; arbeitsstunde_id: number; aktion: string; begruendung: string; geprueft_von: number; geprueft_am: string; vorher_snapshot: WorkHour; nachher_snapshot: WorkHour | null };
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
    readSupabase<Pick<Member, "id" | "vorname" | "name">>(session, "mitglied", {
      select: "id,vorname,name",
      id: `eq.${selectedMemberId}`,
      limit: "1",
    }).then((items) => {
      if (active) setSelectedMember(items[0] ?? null);
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
    canAccessMeterChanges: context.role !== "user",
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
          {activeId === "mitglieder" && <MemberSearch session={session} selectedMemberId={selectedMemberId} onSelect={selectMember} canCreate={has(Permission.createMember) || has(Permission.editAllMembers)} onCreate={() => { setCreatingMember(true); setActiveId("mitglied-stammdaten"); }} />}
          {activeId === "parzellen" && <ParcelWorkspace session={session} selectedParcelId={selectedParcelId} onSelect={selectParcel} canEdit={has(Permission.writeParzellen) || context.role === "admin"} onOpenMember={(memberId) => { selectMember(memberId); setActiveId("mitglied-gaerten"); }} />}
          {activeId === "ablesen" && <MeterOverview session={session} clubId={club.vereinId} reviewerMemberId={context.mitgliedId} selectedParcelId={selectedParcelId} seasonYear={season} canApprove={has(Permission.approveMeterReadings)} canManageMeterChanges={has(Permission.manageMeterChanges)} onNavigate={setActiveId} />}
          {activeId === "foto-uploads" && <PendingPhotoUploads session={session} clubId={club.vereinId} />}
          {activeId === "zaehlerwechsel" && <MeterChange session={session} />}
          {activeId === "arbeitsstunden-pruefen" && <WorkHoursOverview session={session} reviewerMemberId={context.mitgliedId} />}
          {activeId === "arbeitseinsaetze" && <WorkAssignmentsManagement session={session} canEdit={context.role !== "user"} saisonId={workspaceContext.saisonId} onBack={() => setActiveId("start")} />}
          {activeId === "wartung" && <MaintenanceContracts session={session} canManage={context.role !== "user"} />}
          {activeId === "termine" && <AppointmentManagement session={session} canEdit={context.role !== "user"} onBack={() => setActiveId("start")} />}
          {activeId === "bekanntmachungen" && <AnnouncementManagement session={session} canEdit={context.role !== "user"} onBack={() => setActiveId("start")} />}
          {activeId === "impressum" && <ImprintPage session={session} />}
          {activeId === "export" && <ExportCenter session={session} canExport={context.role !== "user"} />}
          {activeId === "benutzer" && <UserRightsAdministration session={session} />}
          {activeId === "saisons" && <SeasonAdministration session={session} onSeasonSaved={(saved) => { setSeasons((current) => [...current.filter((item) => item.id !== saved.id), saved].sort((a, b) => b.jahr - a.jahr)); selectWorkspaceSeason(saved); }} />}
          {activeId === "verein" && <ClubConfigurationAdministration session={session} />}
          {activeId === "mitglied-arbeitsstunden" && selectedMemberId && <OwnWorkHours session={session} memberId={selectedMemberId} saisonId={workspaceContext.saisonId} canEdit={selectedMemberId === context.mitgliedId || has(Permission.editAllMembers)} />}
          {activeId === "mitglied-wartung" && selectedMemberId && <MaintenanceContracts session={session} memberId={selectedMemberId} canManage={context.role !== "user"} />}
          {activeId === "mitglied-dokumente" && selectedMemberId && <DocumentList session={session} memberId={selectedMemberId} canManage={has(Permission.manageDocuments)} />}
          {activeId === "mitglied-admin" && selectedMemberId && <UserRightsAdministration session={session} fixedMemberId={selectedMemberId} />}
          {activeId === "mitglied-gaerten" && selectedMemberId && <MemberGardensWorkspace session={session} memberId={selectedMemberId} selectedParcelId={selectedParcelId} onSelect={selectParcel} canEdit={has(Permission.createMember) || has(Permission.writeParzellen) || context.role === "admin"} onOpenDocuments={() => setActiveId("mitglied-dokumente")} />}
          {activeId === "mitglied-protokolle" && selectedMemberId && <ParcelProtocolsWorkspace session={session} memberId={selectedMemberId} currentBoardMemberId={context.mitgliedId} />}
          {activeId === "mitglied-stammdaten" && (selectedMemberId || creatingMember) && <MemberStammdatenPage session={session} memberId={creatingMember ? null : selectedMemberId} canEdit={creatingMember || has(Permission.editAllMembers) || (selectedMemberId === context.mitgliedId && has(Permission.seeOwnData))} canCreate={creatingMember} canManageSecondary={has(Permission.editAllMembers)} onSaved={(member) => { selectMember(member.id); setActiveId("mitglied-stammdaten"); }} onCancel={() => { setCreatingMember(false); setActiveId("mitglieder"); }} />}
        </article>
      </section>
    </main>
  );
}

function MemberSearch({ session, selectedMemberId, onSelect, canCreate, onCreate }: { session: BrowserSession; selectedMemberId: number | null; onSelect: (mitgliedId: number) => void; canCreate: boolean; onCreate: () => void }) {
  const [members, setMembers] = useState<Member[]>([]);
  const [parcels, setParcels] = useState<Parcel[]>([]);
  const [assignments, setAssignments] = useState<ParcelAssignment[]>([]);
  const [query, setQuery] = useState("");
  const [showInactiveMembers, setShowInactiveMembers] = useState(false);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);

  const loadMembers = () => {
    setLoading(true); setError("");
    return Promise.all([
      readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", order: "name.asc,vorname.asc", limit: "1000" }),
      readSupabase<Parcel>(session, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", order: "garten_nr.asc", limit: "1000" }),
      readSupabase<ParcelAssignment>(session, "parzellen_belegung", { select: "id,parzelle_id,mitglied_id,von_datum,bis_datum", order: "von_datum.desc", limit: "3000" }),
    ]).then(([nextMembers, nextParcels, nextAssignments]) => { setMembers(nextMembers); setParcels(nextParcels); setAssignments(nextAssignments); }).catch((cause: Error) => setError(cause.message)).finally(() => setLoading(false));
  };

  useEffect(() => {
    loadMembers();
  }, [session]);
  const gardenNumbers = (memberId: number) => {
    const today = new Date().toISOString().slice(0, 10);
    const parcelIds = new Set(assignments
      .filter((item) => item.mitglied_id === memberId && (!item.von_datum || item.von_datum <= today) && (!item.bis_datum || item.bis_datum >= today))
      .map((item) => item.parzelle_id));
    return parcels.filter((item) => parcelIds.has(item.id)).map((item) => item.garten_nr).sort((a, b) => a.localeCompare(b, "de", { numeric: true })).join(", ");
  };
  const normalized = query.trim().toLocaleLowerCase("de");
  const results = members.filter((member) => (showInactiveMembers || member.aktiv) && (!normalized || [member.name, member.vorname, member.email, String(member.id), gardenNumbers(member.id)].filter(Boolean).join(" ").toLocaleLowerCase("de").includes(normalized)));
  const selected = members.find((item) => item.id === selectedMemberId) ?? null;
  return <section className="data-workspace" aria-label="Mitglieder suchen">
    <div className="data-toolbar"><label>Suche<input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Name, E-Mail, Mitgliedsnummer oder Gartennummer" /></label><label className="filter-check"><input type="checkbox" checked={showInactiveMembers} onChange={(event) => setShowInactiveMembers(event.target.checked)} /> Inaktive Mitglieder anzeigen</label><span>{loading ? "Lädt …" : `${results.length} Mitglieder`}</span>{canCreate && <button onClick={onCreate}>Mitglied anlegen</button>}</div>
    {error ? <p className="notice" role="alert">{error}</p> : <><div className="data-table-wrap"><table><thead><tr><th>Name</th><th>Garten</th><th>E-Mail</th><th>Status</th></tr></thead><tbody>{results.map((member) => <tr key={member.id} className={selected?.id === member.id ? "selected-row" : ""} onClick={() => onSelect(member.id)}><td><strong>{member.name ?? "–"}</strong>, {member.vorname ?? ""}</td><td>{gardenNumbers(member.id) || "–"}</td><td>{member.email ?? "–"}</td><td>{member.aktiv ? "aktiv" : "inaktiv"}</td></tr>)}</tbody></table></div>{selected && <p className="context-note">{[selected.vorname, selected.name].filter(Boolean).join(" ")} ist ausgewählt. Die zugehörigen Bereiche – einschließlich Stammdaten – stehen nun eingerückt im Menü.</p>}</>}
  </section>;
}

function MemberStammdatenPage({ session, memberId, canEdit, canCreate, canManageSecondary, onSaved, onCancel }: { session: BrowserSession; memberId: number | null; canEdit: boolean; canCreate: boolean; canManageSecondary: boolean; onSaved: (member: Member) => void; onCancel: () => void }) {
  const [member, setMember] = useState<Member | null>(null); const [loading, setLoading] = useState(!canCreate); const [message, setMessage] = useState("");
  useEffect(() => { if (canCreate || !memberId) { setMember(null); setLoading(false); return; } setLoading(true); setMessage(""); readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", id: `eq.${memberId}`, limit: "1" }).then((items) => { setMember(items[0] ?? null); if (!items[0]) setMessage("Das ausgewählte Mitglied konnte nicht geladen werden."); }).catch((cause: Error) => setMessage(cause.message)).finally(() => setLoading(false)); }, [session, memberId, canCreate]);
  if (loading) return <section className="data-workspace"><p>Lädt …</p></section>;
  if (message) return <section className="data-workspace"><p className="notice">{message}</p></section>;
  return <section className="data-workspace member-master-data"><MemberDetail session={session} member={member} canEdit={canEdit} canCreate={canCreate} canManageSecondary={canManageSecondary} onSaved={(saved) => { setMember(saved); onSaved(saved); }} onCancel={onCancel} /></section>;
}

function MemberDetail({ session, member, canEdit, canCreate, canManageSecondary, onSaved, onCancel }: { session: BrowserSession; member: Member | null; canEdit: boolean; canCreate: boolean; canManageSecondary: boolean; onSaved: (member: Member) => void; onCancel: () => void }) {
  const [draft, setDraft] = useState<Partial<Member>>({}); const [editing, setEditing] = useState(canCreate); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const editLock = useEditLock(session, "mitglied", member?.id, Boolean(member && editing && canEdit && !canCreate));
  useEffect(() => { setDraft(member ?? { vorname: "", name: "", email: "", aktiv: true, whatsapp_einwilligung: false, mitglied_seit: new Date().toISOString().slice(0, 10), mitglied_ende: null }); setEditing(canCreate); setMessage(""); }, [member, canCreate]);
  const field = (key: keyof Member) => ({ value: String(draft[key] ?? ""), onChange: (event: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) => setDraft({ ...draft, [key]: event.target.value }) });
  async function save() { if (!draft.vorname?.trim() || !draft.name?.trim()) { setMessage("Vor- und Nachname sind erforderlich."); return; } setSaving(true); setMessage(""); try { const payload = { vorname: draft.vorname.trim(), name: draft.name.trim(), email: draft.email?.trim() || null, geburtsdatum: draft.geburtsdatum || null, adresse: draft.adresse?.trim() || null, plz: draft.plz?.trim() || null, ort: draft.ort?.trim() || null, telefon: draft.telefon?.trim() || null, handy: draft.handy?.trim() || null, whatsapp_einwilligung: Boolean(draft.whatsapp_einwilligung), mitglied_seit: draft.mitglied_seit || null, mitglied_ende: draft.mitglied_ende || null, bemerkung: draft.bemerkung?.trim() || null, aktiv: !draft.mitglied_ende && draft.aktiv !== false }; const rows = canCreate ? await writeSupabase<Member>(session, "mitglied", "POST", payload) : await writeSupabase<Member>(session, "mitglied", "PATCH", payload, { id: `eq.${member?.id}` }); const saved = rows[0]; if (!saved) throw new Error("Die Änderung wurde nicht bestätigt."); onSaved(saved); setMessage("Gespeichert."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Speichern nicht möglich."); } finally { setSaving(false); } }
  if (!member && !canCreate) return <aside className="detail-panel"><h2>Mitglied</h2><p>Wähle links ein Mitglied aus.</p></aside>;
  return <aside className="detail-panel member-editor"><div className="detail-title"><div><h2>{canCreate ? "Mitglied anlegen" : "Stammdaten"}</h2>{member && <p>Mitglied #{member.id}</p>}</div>{canEdit && !editing && <button onClick={() => setEditing(true)}>Bearbeiten</button>}</div>{editLock.message && <p className="notice" role="status">{editLock.message}</p>}{message && <p className="notice" role="status">{message}</p>}<fieldset disabled={!editing || saving || !editLock.acquired}><label>Vorname *<input {...field("vorname")} /></label><label>Nachname *<input {...field("name")} /></label><label>E-Mail<input type="email" {...field("email")} /></label><label>Geburtsdatum<input type="date" {...field("geburtsdatum")} /></label><label className="wide">Straße / Hausnummer<input {...field("adresse")} /></label><label>PLZ<input {...field("plz")} /></label><label>Ort<input {...field("ort")} /></label><label>Telefon<input {...field("telefon")} /></label><label>Mobilnummer<input {...field("handy")} /></label><label>Mitglied seit<input type="date" {...field("mitglied_seit")} /></label><label>Mitglied Ende<input type="date" {...field("mitglied_ende")} /></label><label className="check"><input type="checkbox" checked={Boolean(draft.whatsapp_einwilligung)} onChange={(event) => setDraft({ ...draft, whatsapp_einwilligung: event.target.checked })} /> WhatsApp-Einwilligung</label><label className="wide">Bemerkung<textarea value={draft.bemerkung ?? ""} onChange={(event) => setDraft({ ...draft, bemerkung: event.target.value })} /></label></fieldset>{editing && <div className="editor-actions"><button onClick={save} disabled={saving || !editLock.acquired}>{saving ? "Speichert …" : editLock.checking ? "Sperre wird geprüft …" : "Speichern"}</button><button className="secondary-action" onClick={() => { if (canCreate) onCancel(); else { setDraft(member ?? {}); setEditing(false); } }} disabled={saving}>Abbrechen</button></div>}{member && !member.hauptmitglied_id && canManageSecondary && <><SecondaryMemberPanel session={session} mainMember={member} onChanged={() => onSaved(member)} /><MembershipEndPanel session={session} mainMember={member} onChanged={() => onSaved(member)} /></>}<p className="detail-hint">Der Mitgliedsantrag wird nach Abschluss der Stammdaten im Dokumentbereich erzeugt und dort zur Unterschrift übergeben. Die Daten werden nur mit den Rechten des angemeldeten Vereinskontos gespeichert.</p></aside>;
}

function SecondaryMemberPanel({ session, mainMember, onChanged }: { session: BrowserSession; mainMember: Member; onChanged: () => void }) {
  const [secondary, setSecondary] = useState<Member | null>(null); const [open, setOpen] = useState(false); const [editing, setEditing] = useState(false); const [draft, setDraft] = useState<Partial<Member>>({}); const [copyAddress, setCopyAddress] = useState(true); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const editLock = useEditLock(session, "mitglied", secondary?.id, Boolean(secondary && editing));
  const load = () => readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", hauptmitglied_id: `eq.${mainMember.id}`, limit: "1" }).then((items) => setSecondary(items[0] ?? null)).catch(() => setMessage("Nebenmitglied konnte nicht geladen werden."));
  useEffect(() => { load(); }, [mainMember.id]);
  function beginCreate() { setDraft({ vorname: "", name: mainMember.name ?? "", adresse: mainMember.adresse, plz: mainMember.plz, ort: mainMember.ort, mitglied_seit: mainMember.mitglied_seit ?? new Date().toISOString().slice(0, 10), aktiv: true, whatsapp_einwilligung: false }); setOpen(true); setEditing(false); }
  function beginEdit() { setDraft(secondary ?? {}); setEditing(true); setOpen(false); }
  const set = (key: keyof Member, value: string | boolean) => setDraft({ ...draft, [key]: value });
  async function save() { if (!draft.vorname?.trim() || !draft.name?.trim()) { setMessage("Vor- und Nachname sind erforderlich."); return; } setSaving(true); setMessage(""); try { const payload = { hauptmitglied_id: mainMember.id, vorname: draft.vorname.trim(), name: draft.name.trim(), email: draft.email?.trim() || null, geburtsdatum: draft.geburtsdatum || null, adresse: draft.adresse?.trim() || null, plz: draft.plz?.trim() || null, ort: draft.ort?.trim() || null, telefon: draft.telefon?.trim() || null, handy: draft.handy?.trim() || null, whatsapp_einwilligung: Boolean(draft.whatsapp_einwilligung), mitglied_seit: draft.mitglied_seit || mainMember.mitglied_seit || null, bemerkung: draft.bemerkung?.trim() || null, aktiv: true, mitglied_ende: null }; if (secondary) await writeSupabase<Member>(session, "mitglied", "PATCH", payload, { id: `eq.${secondary.id}` }); else await writeSupabase<Member>(session, "mitglied", "POST", payload); setOpen(false); setEditing(false); await load(); onChanged(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Nebenmitglied konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  return <section className="secondary-member"><h3>Nebenmitglied</h3>{editLock.message && <p className="notice">{editLock.message}</p>}{message && <p className="notice">{message}</p>}{secondary && !editing ? <><p><strong>{secondary.vorname} {secondary.name}</strong><br />Mitglied #{secondary.id}{secondary.email ? ` · ${secondary.email}` : ""}</p><div className="editor-actions"><button onClick={beginEdit}>Nebenmitglied bearbeiten</button></div></> : (open || editing) ? <div className="secondary-form"><fieldset disabled={saving || !editLock.acquired}><label>Vorname *<input value={draft.vorname ?? ""} onChange={(event) => set("vorname", event.target.value)} /></label><label>Nachname *<input value={draft.name ?? ""} onChange={(event) => set("name", event.target.value)} /></label>{open && <label className="check"><input type="checkbox" checked={copyAddress} onChange={(event) => { setCopyAddress(event.target.checked); if (event.target.checked) setDraft({ ...draft, adresse: mainMember.adresse, plz: mainMember.plz, ort: mainMember.ort }); }} /> Adresse vom Hauptmitglied übernehmen</label>}<label>E-Mail<input type="email" value={draft.email ?? ""} onChange={(event) => set("email", event.target.value)} /></label><label>Geburtsdatum<input type="date" value={draft.geburtsdatum ?? ""} onChange={(event) => set("geburtsdatum", event.target.value)} /></label><label className="wide">Straße / Hausnummer<input value={draft.adresse ?? ""} onChange={(event) => set("adresse", event.target.value)} /></label><label>PLZ<input value={draft.plz ?? ""} onChange={(event) => set("plz", event.target.value)} /></label><label>Ort<input value={draft.ort ?? ""} onChange={(event) => set("ort", event.target.value)} /></label><label>Telefon<input value={draft.telefon ?? ""} onChange={(event) => set("telefon", event.target.value)} /></label><label>Mobilnummer<input value={draft.handy ?? ""} onChange={(event) => set("handy", event.target.value)} /></label><label className="wide">Bemerkung<textarea value={draft.bemerkung ?? ""} onChange={(event) => set("bemerkung", event.target.value)} /></label></fieldset><div className="editor-actions"><button disabled={saving || !editLock.acquired} onClick={save}>{saving ? "Speichert …" : editLock.checking ? "Sperre wird geprüft …" : "Nebenmitglied speichern"}</button><button className="secondary-action" onClick={() => { setOpen(false); setEditing(false); }}>Abbrechen</button></div></div> : <button onClick={beginCreate}>Nebenmitglied anlegen</button>}</section>;
}

function MembershipEndPanel({ session, mainMember, onChanged }: { session: BrowserSession; mainMember: Member; onChanged: () => void }) {
  const [secondary, setSecondary] = useState<Member | null>(null); const [endDate, setEndDate] = useState(new Date().toISOString().slice(0, 10)); const [successor, setSuccessor] = useState("end-all"); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  useEffect(() => { readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", hauptmitglied_id: `eq.${mainMember.id}`, aktiv: "eq.true", limit: "1" }).then((items) => setSecondary(items[0] ?? null)).catch(() => setSecondary(null)); }, [session, mainMember.id]);
  async function endMembership() { if (!endDate) return; const promotion = successor === "promote" && secondary; const confirmation = promotion ? `Soll ${secondary?.vorname} ${secondary?.name} ab ${endDate} als Hauptmitglied weitergeführt werden?` : `Soll die Mitgliedschaft von ${mainMember.vorname} ${mainMember.name} zum ${endDate} beendet werden${secondary ? " und das Nebenmitglied ebenfalls beendet werden" : ""}?`; if (!window.confirm(confirmation)) return; setSaving(true); setMessage(""); try { if (promotion) await writeSupabase<Member>(session, "mitglied", "PATCH", { hauptmitglied_id: null, mitglied_ende: null, aktiv: true }, { id: `eq.${secondary.id}` }); else if (secondary) await writeSupabase<Member>(session, "mitglied", "PATCH", { mitglied_ende: endDate, aktiv: false }, { id: `eq.${secondary.id}` }); await writeSupabase<Member>(session, "mitglied", "PATCH", { mitglied_ende: endDate, aktiv: false }, { id: `eq.${mainMember.id}` }); setMessage(promotion ? "Mitgliedschaft beendet; das Nebenmitglied wurde zum Hauptmitglied." : "Mitgliedschaft beendet."); onChanged(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Mitgliedschaft konnte nicht beendet werden."); } finally { setSaving(false); } }
  return <section className="secondary-member membership-end"><h3>Mitgliedschaft beenden</h3>{message && <p className="notice">{message}</p>}<label>Enddatum<input type="date" value={endDate} onChange={(event) => setEndDate(event.target.value)} /></label>{secondary && <label>Folgeentscheidung<select value={successor} onChange={(event) => setSuccessor(event.target.value)}><option value="end-all">Nebenmitglied ebenfalls beenden</option><option value="promote">Nebenmitglied zum Hauptmitglied machen</option></select></label>}<div className="editor-actions"><button className="reject-action" disabled={saving} onClick={endMembership}>{saving ? "Beendet …" : "Mitgliedschaft beenden"}</button></div></section>;
}

function ParcelManagement({ session, selectedParcelId, onSelect, onNavigate, canEdit }: { session: BrowserSession; selectedParcelId: number | null; onSelect: (parzelleId: number) => void; onNavigate: (id: string) => void; canEdit: boolean }) {
  const [parcels, setParcels] = useState<Parcel[]>([]);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  useEffect(() => {
    readSupabase<Parcel>(session, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", order: "garten_nr.asc", limit: "500" })
      .then(setParcels).catch((cause: Error) => setError(cause.message)).finally(() => setLoading(false));
  }, [session]);
  const selected = parcels.find((item) => item.id === selectedParcelId) ?? null;
  return <section className="data-workspace" aria-label="Parzellenverwaltung"><div className="data-toolbar"><span>{loading ? "Lädt …" : `${parcels.length} Parzellen`}</span></div>{error ? <p className="notice" role="alert">{error}</p> : <div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Garten</th><th>Anlage</th><th>Fläche</th><th>Status</th></tr></thead><tbody>{parcels.map((parcel) => <tr key={parcel.id} className={selected?.id === parcel.id ? "selected-row" : ""} onClick={() => onSelect(parcel.id)}><td><strong>{parcel.garten_nr}</strong></td><td>{parcel.Anlage}</td><td>{parcel.flaeche_qm ?? "–"} m²</td><td>{parcel.aktiv ? "aktiv" : "inaktiv"}</td></tr>)}</tbody></table></div><ParcelDetailEditor session={session} parcel={selected} canEdit={canEdit} onOpenMeters={() => onNavigate("ablesen")} /></div>}</section>;
}

function ParcelDetailEditor({ session, parcel, canEdit, onOpenMeters }: { session: BrowserSession; parcel: Parcel | null; canEdit: boolean; onOpenMeters: () => void }) {
  const [draft, setDraft] = useState<Parcel | null>(parcel); const [editing, setEditing] = useState(false); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const editLock = useEditLock(session, "parzelle", parcel?.id, Boolean(parcel && editing && canEdit));
  useEffect(() => { setDraft(parcel); setEditing(false); setMessage(""); }, [parcel?.id]);
  useEffect(() => { if (editing && editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editing, editLock.message]);
  async function save() { if (!draft || !editLock.acquired) { if (editLock.message) setMessage(editLock.message); return; } setSaving(true); try { await writeSupabase<Parcel>(session, "parzelle", "PATCH", { flaeche_qm: draft.flaeche_qm, hat_strom: draft.hat_strom, hat_wasser: draft.hat_wasser, aktiv: draft.aktiv }, { id: `eq.${draft.id}` }); setEditing(false); setMessage("Parzellen-Stammdaten gespeichert."); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Speichern nicht möglich."); } finally { setSaving(false); } }
  if (!draft) return <aside className="detail-panel"><h2>Parzellen-Detail</h2><p>Wähle links eine Parzelle aus.</p></aside>;
  return <aside className="detail-panel member-editor"><div className="detail-title"><div><h2>Parzellen-Detail</h2><p>{draft.garten_nr} – {draft.Anlage}</p></div>{canEdit && !editing && <button onClick={() => setEditing(true)}>Bearbeiten</button>}</div>{message && <p className="notice">{message}</p>}<fieldset disabled={!editing || saving}><label>Fläche in m²<input inputMode="decimal" value={draft.flaeche_qm ?? ""} onChange={(event) => setDraft({ ...draft, flaeche_qm: event.target.value ? Number(event.target.value.replace(",", ".")) : null })} /></label><label className="check"><input type="checkbox" checked={draft.hat_strom} onChange={(event) => setDraft({ ...draft, hat_strom: event.target.checked })} /> Strom vorhanden</label><label className="check"><input type="checkbox" checked={draft.hat_wasser} onChange={(event) => setDraft({ ...draft, hat_wasser: event.target.checked })} /> Wasser vorhanden</label><label className="check"><input type="checkbox" checked={draft.aktiv} onChange={(event) => setDraft({ ...draft, aktiv: event.target.checked })} /> Parzelle aktiv</label></fieldset>{editing && <div className="editor-actions"><button onClick={save} disabled={saving}>Speichern</button><button className="secondary-action" onClick={() => { setDraft(parcel); setEditing(false); }}>Abbrechen</button></div>}<div className="parcel-quick-actions"><button className="secondary-action" onClick={onOpenMeters}>Stromzähler öffnen</button><button className="secondary-action" onClick={onOpenMeters}>Wasserzähler öffnen</button></div>{canEdit && <ParcelAssignments session={session} parcelId={draft.id} />}<DocumentList session={session} parcelId={draft.id} compact canManage={canEdit} /><p className="detail-hint">Zähler und Dokumente öffnen immer im aktuell ausgewählten Parzellenkontext.</p></aside>;
}

function ParcelAssignments({ session, parcelId }: { session: BrowserSession; parcelId: number }) {
  const [items, setItems] = useState<ParcelAssignment[]>([]); const [members, setMembers] = useState<Member[]>([]); const [memberId, setMemberId] = useState(""); const [fromDate, setFromDate] = useState(new Date().toISOString().slice(0, 10)); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const load = () => readSupabase<ParcelAssignment>(session, "parzellen_belegung", { select: "id,parzelle_id,mitglied_id,von_datum,bis_datum", parzelle_id: `eq.${parcelId}`, order: "von_datum.desc" }).then(setItems).catch(() => setItems([]));
  useEffect(() => { load(); readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", aktiv: "eq.true", order: "name.asc,vorname.asc", limit: "500" }).then(setMembers).catch(() => setMembers([])); }, [session, parcelId]);
  const active = items.find((item) => !item.bis_datum) ?? null;
  async function assign() { const id = Number(memberId); if (!id || !fromDate) { setMessage("Mitglied und Beginn sind erforderlich."); return; } setSaving(true); setMessage(""); try { await writeSupabase<ParcelAssignment>(session, "parzellen_belegung", "POST", { parzelle_id: parcelId, mitglied_id: id, von_datum: fromDate, bis_datum: null }); setMemberId(""); await load(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Belegung konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  async function end() { if (!active) return; setSaving(true); setMessage(""); try { await writeSupabase<ParcelAssignment>(session, "parzellen_belegung", "PATCH", { bis_datum: new Date().toISOString().slice(0, 10) }, { id: `eq.${active.id}` }); await load(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Belegung konnte nicht beendet werden."); } finally { setSaving(false); } }
  return <section className="secondary-member"><h3>Belegung</h3>{message && <p className="notice">{message}</p>}{active ? <p><strong>Aktiv: Mitglied #{active.mitglied_id}</strong><br /><button className="secondary-action" disabled={saving} onClick={end}>Belegung heute beenden</button></p> : <div className="secondary-form"><label>Mitglied<select value={memberId} onChange={(event) => setMemberId(event.target.value)}><option value="">Mitglied wählen</option>{members.map((member) => <option key={member.id} value={member.id}>{member.name}, {member.vorname} · #{member.id}</option>)}</select></label><label>Beginn<input type="date" value={fromDate} onChange={(event) => setFromDate(event.target.value)} /></label><button disabled={saving} onClick={assign}>Mitglied zuordnen</button></div>}{items.length ? <ul className="review-history">{items.map((item) => <li key={item.id}><strong>Mitglied #{item.mitglied_id}</strong><span>{formatDate(item.von_datum)} – {item.bis_datum ? formatDate(item.bis_datum) : "aktiv"}</span></li>)}</ul> : <p>Keine Belegung hinterlegt.</p>}</section>;
}

function MeterOverview({ session, clubId, reviewerMemberId, selectedParcelId, seasonYear, canApprove, canManageMeterChanges, onNavigate: navigate }: { session: BrowserSession; clubId: string; reviewerMemberId: number | null; selectedParcelId: number | null; seasonYear: number; canApprove: boolean; canManageMeterChanges: boolean; onNavigate: (id: string) => void }) {
  const [meters, setMeters] = useState<Meter[]>([]);
  const [parcels, setParcels] = useState<Parcel[]>([]);
  const [readings, setReadings] = useState<Reading[]>([]);
  const [assignments, setAssignments] = useState<ParcelAssignment[]>([]);
  const [members, setMembers] = useState<Member[]>([]);
  const [activeTab, setActiveTab] = useState<"meters" | "due" | "open" | "reviewed">("meters");
  const [selectedReadingId, setSelectedReadingId] = useState<number | null>(null);
  const [wifiOnly, setWifiOnly] = useState(() => typeof window !== "undefined" && window.localStorage.getItem("kgv-meter-photo-wifi-only") === "true");
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const onNavigate = (id: string) => { if (id !== "zaehlerwechsel" || canManageMeterChanges) navigate(id); };
  function changeWifiOnly(value: boolean) { setWifiOnly(value); window.localStorage.setItem("kgv-meter-photo-wifi-only", String(value)); }
  async function load() {
    setLoading(true); setError("");
    return Promise.all([
      readSupabase<Meter>(session, "zaehler", { select: "id,parzelle_id,medium,zaehlernummer,eingebaut_am,ausgebaut_am,eichfaellig_am,status", order: "medium.asc,zaehlernummer.asc", limit: "1000" }),
      readSupabase<Parcel>(session, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", limit: "500" }),
      readSupabase<Reading>(session, "zaehler_ablesung", { select: "id,zaehler_id,stand,ablesedatum,art,freigegeben,pruefstatus,pruefkommentar,geprueft_von,geprueft_am,foto_pfad,foto_drive_file_id,foto_dateiname", order: "ablesedatum.desc", limit: "1000" }),
      readSupabase<ParcelAssignment>(session, "parzellen_belegung", { select: "id,parzelle_id,mitglied_id,von_datum,bis_datum", order: "von_datum.desc", limit: "2000" }),
      readSupabase<Member>(session, "mitglied", { select: "id,vorname,name,email,aktiv,hauptmitglied_id,geburtsdatum,adresse,plz,ort,telefon,handy,whatsapp_einwilligung,mitglied_seit,mitglied_ende,bemerkung", limit: "1000" }),
    ]).then(([loadedMeters, loadedParcels, loadedReadings, loadedAssignments, loadedMembers]) => { setMeters(loadedMeters); setParcels(loadedParcels); setReadings(loadedReadings); setAssignments(loadedAssignments); setMembers(loadedMembers); }).catch((cause: Error) => setError(cause.message)).finally(() => setLoading(false));
  }
  useEffect(() => {
    void load();
  }, [session]);
  const parcelName = (parcelId: number) => { const parcel = parcels.find((item) => item.id === parcelId); return parcel ? `${parcel.garten_nr} – ${parcel.Anlage}` : "Unbekannte Parzelle"; };
  const meterFor = (meterId: number) => meters.find((item) => item.id === meterId);
  const memberName = (memberId: number | null | undefined) => { const member = members.find((item) => item.id === memberId); return member ? [member.vorname, member.name].filter(Boolean).join(" ") || `Mitglied #${member.id}` : memberId ? `Mitglied #${memberId}` : "Quelle im Modell nicht verfügbar"; };
  const submitterName = (reading: Reading) => { const meter = meterFor(reading.zaehler_id); if (!meter) return "Quelle im Modell nicht verfügbar"; const date = reading.ablesedatum.slice(0, 10); const assignment = assignments.filter((item) => item.parzelle_id === meter.parzelle_id && (!item.von_datum || item.von_datum <= date) && (!item.bis_datum || item.bis_datum >= date)).sort((a, b) => String(b.von_datum ?? "").localeCompare(String(a.von_datum ?? "")))[0]; return memberName(assignment?.mitglied_id); };
  const scopedMeters = selectedParcelId ? meters.filter((item) => item.parzelle_id === selectedParcelId) : meters;
  const openReadings = readings.filter((item) => item.pruefstatus === "eingereicht" && !item.freigegeben).filter((item) => !selectedParcelId || meterFor(item.zaehler_id)?.parzelle_id === selectedParcelId);
  const reviewedReadings = readings.filter((item) => item.pruefstatus !== "eingereicht" || item.freigegeben).filter((item) => !selectedParcelId || meterFor(item.zaehler_id)?.parzelle_id === selectedParcelId);
  const dueMeters = scopedMeters.filter((item) => !item.ausgebaut_am && item.eichfaellig_am && new Date(item.eichfaellig_am) <= new Date(Date.now() + 365 * 24 * 60 * 60 * 1000)).sort((a, b) => a.eichfaellig_am.localeCompare(b.eichfaellig_am));
  const selectedReading = openReadings.find((item) => item.id === selectedReadingId) ?? null;
  const scopedReadings = readings.filter((item) => !selectedParcelId || meterFor(item.zaehler_id)?.parzelle_id === selectedParcelId);
  const selectedHistory = selectedReading ? readings.filter((item) => item.zaehler_id === selectedReading.zaehler_id && item.id !== selectedReading.id).sort((a, b) => b.ablesedatum.localeCompare(a.ablesedatum)).slice(0, 5) : [];
  return <section className="data-workspace" aria-label="Zähler und Ablesungen"><section className="meter-overview-actions"><div><strong>Ablesen</strong><p>Erfassung, Jahresendablesung, Eichfristen und Prüfung in einem PC-Arbeitsbereich.</p></div><label className="wifi-setting"><input type="checkbox" checked={wifiOnly} onChange={(event) => changeWifiOnly(event.target.checked)} /> Fotos nur bei erkanntem WLAN direkt hochladen</label><div><button className="secondary-action" onClick={() => onNavigate("foto-uploads")}>Foto-Uploads</button><button className="secondary-action" onClick={() => onNavigate("zaehlerwechsel")}>Zählerwechsel</button></div></section><MeterReadingEntry session={session} clubId={clubId} meters={scopedMeters} parcels={parcels} readings={readings} seasonYear={seasonYear} directApproval={canApprove} wifiOnly={wifiOnly} onSaved={load} />{selectedParcelId && <p className="context-note">Parzellenfilter aktiv: #{selectedParcelId}</p>}<div className="meter-summary"><div><span>Aktive Zähler</span><strong>{scopedMeters.filter((item) => !item.ausgebaut_am).length}</strong></div><div><span>Offene Prüfungen</span><strong>{openReadings.length}</strong></div><div><span>In 12 Monaten eichfällig</span><strong>{dueMeters.length}</strong></div></div><div className="tabs" role="tablist"><button className={activeTab === "meters" ? "tab active" : "tab"} onClick={() => setActiveTab("meters")}>Zähler und Historie</button><button className={activeTab === "due" ? "tab active" : "tab"} onClick={() => setActiveTab("due")}>Eichfällige Zähler <span>{dueMeters.length}</span></button>{canApprove && <button className={activeTab === "open" ? "tab active" : "tab"} onClick={() => setActiveTab("open")}>Eingereichte Ablesungen <span>{openReadings.length}</span></button>}{canApprove && <button className={activeTab === "reviewed" ? "tab active" : "tab"} onClick={() => setActiveTab("reviewed")}>Prüfverlauf</button>}</div>{loading ? <p>Lädt …</p> : error ? <p className="notice" role="alert">{error}</p> : activeTab === "meters" ? <><div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Medium</th><th>Zähler</th><th>Einbau</th><th>Eichfällig</th><th>Status</th></tr></thead><tbody>{scopedMeters.map((meter) => <tr key={meter.id}><td>{parcelName(meter.parzelle_id)}</td><td>{meter.medium}</td><td><strong>{meter.zaehlernummer}</strong></td><td>{formatDate(meter.eingebaut_am)}</td><td>{formatDate(meter.eichfaellig_am)}</td><td>{meter.ausgebaut_am ? "ausgebaut" : meter.status ?? "aktiv"}</td></tr>)}</tbody></table></div><section className="detail-panel reading-history"><h2>Ablesehistorie</h2><div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Zähler</th><th>Art</th><th>Datum</th><th>Stand</th><th>Status</th><th>Foto</th></tr></thead><tbody>{scopedReadings.map((reading) => { const meter = meterFor(reading.zaehler_id); return <tr key={reading.id}><td>{meter ? parcelName(meter.parzelle_id) : "–"}</td><td>{meter?.zaehlernummer ?? "–"}</td><td>{reading.art.toUpperCase()}</td><td>{formatDate(reading.ablesedatum)}</td><td>{reading.stand}</td><td>{reading.freigegeben ? "freigegeben" : reading.pruefstatus}</td><td>{reading.foto_drive_file_id || reading.foto_pfad ? <PhotoOpenButton session={session} reading={reading} /> : "–"}</td></tr>; })}</tbody></table></div></section></> : activeTab === "due" ? <div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Medium</th><th>Zähler</th><th>Eichfällig</th><th>Tage</th><th>Status</th></tr></thead><tbody>{dueMeters.map((meter) => { const days = Math.ceil((new Date(meter.eichfaellig_am).getTime() - Date.now()) / 86_400_000); return <tr key={meter.id}><td>{parcelName(meter.parzelle_id)}</td><td>{meter.medium}</td><td><strong>{meter.zaehlernummer}</strong></td><td>{formatDate(meter.eichfaellig_am)}</td><td>{days < 0 ? `${Math.abs(days)} überfällig` : days}</td><td>{days < 0 ? "überfällig" : days <= 180 ? "bald fällig" : "beobachten"}</td></tr>; })}</tbody></table>{dueMeters.length === 0 && <p className="empty-state">Keine Zähler werden innerhalb der nächsten zwölf Monate eichfällig.</p>}</div> : activeTab === "reviewed" ? <div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Mitglied</th><th>Zähler</th><th>Datum</th><th>Stand</th><th>Entscheidung</th><th>Geprüft von</th><th>Kommentar</th></tr></thead><tbody>{reviewedReadings.map((reading) => { const meter = meterFor(reading.zaehler_id); return <tr key={reading.id}><td>{meter ? parcelName(meter.parzelle_id) : "–"}</td><td>{submitterName(reading)}</td><td>{meter?.zaehlernummer ?? "–"}</td><td>{formatDate(reading.ablesedatum)}</td><td>{reading.stand}</td><td>{reading.freigegeben ? "freigegeben" : reading.pruefstatus}</td><td>{memberName(reading.geprueft_von)}</td><td>{reading.pruefkommentar || "–"}</td></tr>; })}</tbody></table>{reviewedReadings.length === 0 && <p className="empty-state">Noch kein Prüfverlauf vorhanden.</p>}</div> : <div className="split-view review-workspace"><div className="data-table-wrap"><table><thead><tr><th>Parzelle</th><th>Mitglied</th><th>Medium / Zähler</th><th>Art</th><th>Datum</th><th>Stand</th><th>Foto</th></tr></thead><tbody>{openReadings.map((reading) => { const meter = meterFor(reading.zaehler_id); return <tr key={reading.id} className={selectedReading?.id === reading.id ? "selected-row" : ""} onClick={() => setSelectedReadingId(reading.id)}><td>{meter ? parcelName(meter.parzelle_id) : "–"}</td><td>{submitterName(reading)}</td><td>{meter ? `${meter.medium} · ${meter.zaehlernummer}` : "–"}</td><td>{reading.art.toUpperCase()}</td><td>{formatDate(reading.ablesedatum)}</td><td>{reading.stand}</td><td>{reading.foto_drive_file_id || reading.foto_pfad ? "vorhanden" : "–"}</td></tr>; })}</tbody></table>{openReadings.length === 0 && <p className="empty-state">Keine offenen Ablesungen zur Prüfung.</p>}</div><ReadingReview session={session} reading={selectedReading} memberName={selectedReading ? submitterName(selectedReading) : ""} history={selectedHistory} reviewerMemberId={reviewerMemberId} onSaved={async () => { setSelectedReadingId(null); await load(); }} /></div>}</section>;
}

function MeterChange({ session }: { session: BrowserSession }) {
  const [meters, setMeters] = useState<Meter[]>([]); const [parcels, setParcels] = useState<Parcel[]>([]); const [meterId, setMeterId] = useState(""); const [endValue, setEndValue] = useState(""); const [date, setDate] = useState(new Date().toISOString().slice(0, 10)); const [newNumber, setNewNumber] = useState(""); const [eichdatum, setEichdatum] = useState(""); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  useEffect(() => { Promise.all([readSupabase<Meter>(session, "zaehler", { select: "id,parzelle_id,medium,zaehlernummer,eingebaut_am,ausgebaut_am,eichfaellig_am,status", limit: "1000" }), readSupabase<Parcel>(session, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", limit: "500" })]).then(([a, b]) => { setMeters(a); setParcels(b); }).catch((e: Error) => setMessage(e.message)); }, [session]);
  const selected = meters.find((item) => item.id === Number(meterId));
  async function save() { const end = Number(endValue.replace(",", ".")); if (!selected || !date || !Number.isFinite(end) || end < 0 || !newNumber.trim() || !eichdatum) { setMessage("Alle Ausbau- und Einbaudaten sind erforderlich."); return; } setSaving(true); setMessage(""); try { await callSupabaseRpc<Meter>(session, "remove_meter", { p_zaehler_id: selected.id, p_ausgebaut_am: date, p_endstand: end, p_ablesedatum: date, p_foto_pfad: null }); await callSupabaseRpc<Meter>(session, "create_meter_installation", { p_parzelle_id: selected.parzelle_id, p_medium: selected.medium, p_zaehlernummer: newNumber.trim(), p_eichdatum: eichdatum, p_eingebaut_am: date }); setMessage("Zählerwechsel erfolgreich gespeichert."); setMeterId(""); setEndValue(""); setNewNumber(""); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Zählerwechsel konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  return <section className="data-workspace"><section className="detail-panel member-editor"><h2>Zählerwechsel</h2>{message && <p className="notice">{message}</p>}<fieldset><label className="wide">Aktiver Zähler<select value={meterId} onChange={(event) => setMeterId(event.target.value)}><option value="">Zähler auswählen</option>{meters.filter((item) => !item.ausgebaut_am).map((item) => { const parcel = parcels.find((p) => p.id === item.parzelle_id); return <option key={item.id} value={item.id}>{parcel?.garten_nr ?? "?"} · {item.medium} · {item.zaehlernummer}</option>; })}</select></label><label>Ausbaudatum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Endstand<input inputMode="decimal" value={endValue} onChange={(event) => setEndValue(event.target.value)} /></label><label>Neue Zählernummer<input value={newNumber} onChange={(event) => setNewNumber(event.target.value)} /></label><label>Eichdatum<input type="date" value={eichdatum} onChange={(event) => setEichdatum(event.target.value)} /></label></fieldset><div className="editor-actions"><button disabled={saving} onClick={save}>{saving ? "Speichert …" : "Wechsel abschließen"}</button></div></section></section>;
}

function MeterReadingEntry({ session, clubId, meters, parcels, readings, seasonYear, directApproval, wifiOnly, onSaved }: { session: BrowserSession; clubId: string; meters: Meter[]; parcels: Parcel[]; readings: Reading[]; seasonYear: number; directApproval: boolean; wifiOnly: boolean; onSaved: () => void | Promise<void> }) {
  const [meterId, setMeterId] = useState(""); const [parcelId, setParcelId] = useState(""); const [medium, setMedium] = useState(""); const [kind, setKind] = useState<"normal" | "jea">("normal"); const [value, setValue] = useState(""); const [date, setDate] = useState(new Date().toISOString().slice(0, 10)); const [file, setFile] = useState<File | null>(null); const [previewUrl, setPreviewUrl] = useState(""); const [photoRequired, setPhotoRequired] = useState(true); const [submissionsAllowed, setSubmissionsAllowed] = useState(directApproval); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  useEffect(() => { readSupabase<{ bool_value: boolean }>(session, "app_setting", { select: "bool_value", setting_key: "eq.meter_reading_photo_required", limit: "1" }).then((rows) => setPhotoRequired(rows[0]?.bool_value ?? true)).catch(() => setPhotoRequired(true)); }, [session]);
  useEffect(() => { if (directApproval) { setSubmissionsAllowed(true); return; } readSupabase<{ bool_value: boolean }>(session, "app_setting", { select: "bool_value", setting_key: "eq.allow_user_meter_reading_submissions", limit: "1" }).then((rows) => setSubmissionsAllowed(rows[0]?.bool_value ?? false)).catch(() => setSubmissionsAllowed(false)); }, [session, directApproval]);
  useEffect(() => { if (!file) { setPreviewUrl(""); return; } const url = URL.createObjectURL(file); setPreviewUrl(url); return () => URL.revokeObjectURL(url); }, [file]);
  const activeMeters = meters.filter((item) => !item.ausgebaut_am);
  const filteredMeters = activeMeters.filter((item) => (!parcelId || item.parzelle_id === Number(parcelId)) && (!medium || item.medium.toLocaleLowerCase("de") === medium));
  const selectedMeter = activeMeters.find((item) => item.id === Number(meterId));
  const latestReading = selectedMeter ? readings.filter((item) => item.zaehler_id === selectedMeter.id && item.freigegeben).sort((a, b) => b.ablesedatum.localeCompare(a.ablesedatum))[0] : undefined;
  function selectKind(next: "normal" | "jea") { setKind(next); if (next === "jea") setDate(`${seasonYear}-12-31`); }
  async function save() {
    if (!directApproval && !submissionsAllowed) { setMessage("Eigene Zählerablesungen sind aktuell zentral deaktiviert."); return; }
    const meter = meters.find((item) => item.id === Number(meterId)); const stand = Number(value.replace(",", ".")); const parcel = meter && parcels.find((item) => item.id === meter.parzelle_id);
    if (!meter || !parcel || !date || !Number.isFinite(stand) || stand < 0) { setMessage("Zähler, Datum und gültiger Stand sind erforderlich."); return; }
    if (latestReading && latestReading.ablesedatum.slice(0, 10) <= date && stand < latestReading.stand) { setMessage(`Der Stand darf nicht unter der letzten freigegebenen Ablesung (${latestReading.stand} vom ${formatDate(latestReading.ablesedatum)}) liegen.`); return; }
    if (readings.some((item) => item.zaehler_id === meter.id && item.ablesedatum.slice(0, 10) === date && item.art === kind && item.pruefstatus !== "abgelehnt")) { setMessage("Für diesen Zähler, dieses Datum und diese Ableseart existiert bereits eine Ablesung."); return; }
    if (photoRequired && !file) { setMessage("Für diese Ablesung ist ein Foto erforderlich."); return; }
    setSaving(true); setMessage("");
    try {
      const rows = await writeSupabase<Reading>(session, "zaehler_ablesung", "POST", { zaehler_id: meter.id, stand, ablesedatum: date, art: kind, freigegeben: directApproval, pruefstatus: directApproval ? "freigegeben" : "eingereicht" });
      const reading = rows[0];
      if (file && reading) {
        const details = { datum: date, medium: meter.medium, anlage: parcel.Anlage, garten: parcel.garten_nr, zaehlernummer: meter.zaehlernummer };
        const connection = (navigator as Navigator & { connection?: { type?: string } }).connection;
        const wifiDetected = connection?.type === "wifi";
        if (wifiOnly && !wifiDetected) { await enqueueMeterPhoto({ clubId, readingId: reading.id, fileName: file.name || `ablesung-${reading.id}.jpg`, contentType: file.type || "image/jpeg", content: file, details }); setMessage("Ablesung gespeichert. Da der Browser kein WLAN bestätigt, bleibt das Foto in der lokalen Warteschlange."); }
        else try { const photo = await uploadMeterPhoto(session, file, details); await writeSupabase<Reading>(session, "zaehler_ablesung", "PATCH", { foto_drive_file_id: photo.fileId, foto_dateiname: photo.fileName }, { id: `eq.${reading.id}` }); setMessage(directApproval ? "Ablesung und Foto wurden direkt freigegeben gespeichert." : "Ablesung und Foto wurden zur Prüfung eingereicht."); }
        catch (uploadError) { await enqueueMeterPhoto({ clubId, readingId: reading.id, fileName: file.name || `ablesung-${reading.id}.jpg`, contentType: file.type || "image/jpeg", content: file, details }); setMessage(`Ablesung wurde gespeichert. Das Foto liegt sicher in der lokalen Warteschlange. ${uploadError instanceof Error ? uploadError.message : ""}`.trim()); }
      } else setMessage(directApproval ? "Ablesung wurde direkt freigegeben gespeichert." : "Ablesung wurde zur Prüfung eingereicht.");
      setMeterId(""); setValue(""); setFile(null); await onSaved();
    } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Ablesung konnte nicht gespeichert werden."); }
    finally { setSaving(false); }
  }
  return <section className="detail-panel member-editor meter-entry"><div className="detail-title"><div><h2>{kind === "jea" ? "Jahresendablesung erfassen" : "Ablesung erfassen"}</h2><p>{photoRequired ? "Foto ist erforderlich" : "Foto ist optional"} · {directApproval ? "wird direkt freigegeben" : "wird zur Prüfung eingereicht"}</p></div><div className="reading-kind"><button className={kind === "normal" ? "active" : "secondary-action"} onClick={() => selectKind("normal")}>Normal</button><button className={kind === "jea" ? "active" : "secondary-action"} onClick={() => selectKind("jea")}>JEA {seasonYear}</button></div></div>{message && <p className="notice" role="status">{message}</p>}<RfidBrowserScanner session={session} onResolved={(context) => { if (context.aktiver_zaehler_id) { setParcelId(String(context.parzelle_id)); setMedium(String(context.medium ?? "").toLocaleLowerCase("de")); setMeterId(String(context.aktiver_zaehler_id)); setMessage(`RFID erkannt: Garten ${context.garten_nr ?? "–"}, ${context.medium ?? "–"}.`); } }} /><fieldset><label>Parzelle<select value={parcelId} onChange={(event) => { setParcelId(event.target.value); setMeterId(""); }}><option value="">Alle Parzellen</option>{parcels.filter((parcel) => activeMeters.some((meter) => meter.parzelle_id === parcel.id)).sort((a, b) => a.garten_nr.localeCompare(b.garten_nr, "de", { numeric: true })).map((parcel) => <option key={parcel.id} value={parcel.id}>{parcel.garten_nr} – {parcel.Anlage}</option>)}</select></label><label>Medium<select value={medium} onChange={(event) => { setMedium(event.target.value); setMeterId(""); }}><option value="">Alle Medien</option><option value="strom">Strom</option><option value="wasser">Wasser</option></select></label><label className="wide">Aktiver Zähler *<select value={meterId} onChange={(event) => setMeterId(event.target.value)}><option value="">Zähler wählen</option>{filteredMeters.map((item) => { const parcel = parcels.find((entry) => entry.id === item.parzelle_id); return <option key={item.id} value={item.id}>Garten {parcel?.garten_nr ?? "?"} · {item.medium} · {item.zaehlernummer}</option>; })}</select></label><label>Datum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Stand<input inputMode="decimal" value={value} onChange={(event) => setValue(event.target.value)} /></label><label className="wide">Foto aufnehmen oder auswählen<input type="file" accept="image/*" capture="environment" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /></label></fieldset>{latestReading && <p className="context-note">Letzte freigegebene Ablesung: {latestReading.stand} am {formatDate(latestReading.ablesedatum)}</p>}{previewUrl && <div className="photo-preview"><img src={previewUrl} alt="Vorschau des ausgewählten Ablesefotos" /><button className="secondary-action" onClick={() => setFile(null)}>Foto entfernen</button></div>}<div className="editor-actions"><button disabled={saving} onClick={save}>{saving ? "Speichert …" : directApproval ? "Ablesung speichern" : "Ablesung einreichen"}</button></div></section>;
}

function RfidBrowserScanner({ session, onResolved }: { session: BrowserSession; onResolved: (context: RfidScanContext) => void }) {
  const supported = typeof window !== "undefined" && Boolean(ndefReaderConstructor()) && window.isSecureContext;
  const [scanning, setScanning] = useState(false); const [message, setMessage] = useState(supported ? "Web-NFC ist auf diesem Gerät verfügbar." : "Dieser Browser bietet kein nutzbares Web-NFC. RFID bleibt auf diesem Gerät ein MAUI-Spezialweg; die Zählerauswahl funktioniert weiterhin manuell.");
  async function start() {
    const Reader = ndefReaderConstructor(); if (!Reader || !window.isSecureContext) return;
    setScanning(true); setMessage("RFID-Scan aktiv. Tag an das Gerät halten.");
    try {
      const reader = new Reader();
      reader.onreadingerror = () => { setMessage("RFID-Tag konnte nicht gelesen werden."); setScanning(false); };
      reader.onreading = async (event) => { const uid = (event.serialNumber ?? "").trim().toUpperCase(); if (!uid) { setMessage("Der Browser hat keine RFID-UID geliefert. Bitte MAUI verwenden."); setScanning(false); return; } try { const rows = await readSupabase<RfidScanContext>(session, "v_rfid_scan_context", { select: "parzelle_id,anlage,garten_nr,medium,rfid_tag_uid,aktiver_zaehler_id,zaehlernummer,status", rfid_tag_uid: `eq.${uid}`, limit: "2" }); if (rows.length !== 1) throw new Error("Für diesen RFID-Tag wurde kein eindeutiger Zählerkontext gefunden."); onResolved(rows[0]); setMessage(`RFID ${uid} wurde gelesen.`); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "RFID-Kontext konnte nicht geladen werden."); } finally { setScanning(false); } };
      await reader.scan();
    } catch (cause) { setMessage(cause instanceof Error ? `Web-NFC konnte nicht gestartet werden: ${cause.message}` : "Web-NFC konnte nicht gestartet werden."); setScanning(false); }
  }
  return <div className={supported ? "rfid-browser supported" : "rfid-browser"}><div><strong>{supported ? "RFID im Browser" : "RFID nicht verfügbar"}</strong><p>{message}</p></div>{supported && <button type="button" className="secondary-action" disabled={scanning} onClick={start}>{scanning ? "Scan läuft …" : "RFID scannen"}</button>}</div>;
}

function PhotoOpenButton({ session, reading }: { session: BrowserSession; reading: Reading }) {
  const [opening, setOpening] = useState(false); const [error, setError] = useState("");
  async function open() { setOpening(true); setError(""); try { if (!reading.foto_drive_file_id && reading.foto_pfad && /^https?:\/\//i.test(reading.foto_pfad)) { window.open(reading.foto_pfad, "_blank", "noopener,noreferrer"); return; } const url = await openMeterPhoto(session, reading.id); window.open(url, "_blank", "noopener,noreferrer"); window.setTimeout(() => URL.revokeObjectURL(url), 60_000); } catch (cause) { setError(cause instanceof Error ? cause.message : "Foto konnte nicht geöffnet werden."); } finally { setOpening(false); } }
  return <span className="photo-open"><button className="table-action" disabled={opening} onClick={open}>{opening ? "Öffnet …" : "Foto anzeigen"}</button>{error && <small>{error}</small>}</span>;
}

function PendingPhotoUploads({ session, clubId }: { session: BrowserSession; clubId: string }) {
  const [items, setItems] = useState<PendingMeterPhoto[]>([]); const [busy, setBusy] = useState(false); const [message, setMessage] = useState(""); const [online, setOnline] = useState(() => typeof navigator === "undefined" ? true : navigator.onLine);
  async function load() { try { setItems(await listPendingMeterPhotos(clubId)); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Lokale Foto-Warteschlange konnte nicht geladen werden."); } }
  useEffect(() => { void load(); const update = () => setOnline(navigator.onLine); window.addEventListener("online", update); window.addEventListener("offline", update); return () => { window.removeEventListener("online", update); window.removeEventListener("offline", update); }; }, [clubId]);
  async function uploadOne(item: PendingMeterPhoto) {
    const uploading: PendingMeterPhoto = { ...item, status: "uploading", lastAttemptAt: new Date().toISOString(), attemptCount: item.attemptCount + 1, lastError: null }; await putPendingMeterPhoto(uploading); await load();
    try { const photo = await uploadMeterPhoto(session, pendingPhotoFile(uploading), uploading.details); await writeSupabase<Reading>(session, "zaehler_ablesung", "PATCH", { foto_drive_file_id: photo.fileId, foto_dateiname: photo.fileName }, { id: `eq.${uploading.readingId}` }); await removePendingMeterPhoto(uploading.id); return true; }
    catch (cause) { await putPendingMeterPhoto({ ...uploading, status: "failed", lastError: cause instanceof Error ? cause.message : "Upload fehlgeschlagen." }); return false; }
  }
  async function retryAll() { if (!online || busy) return; setBusy(true); setMessage("Uploads werden erneut versucht …"); let success = 0; let failed = 0; for (const item of items) { if (await uploadOne(item)) success++; else failed++; } await load(); setMessage(success || failed ? `${success} Foto(s) hochgeladen, ${failed} fehlgeschlagen.` : "Keine offenen Foto-Uploads vorhanden."); setBusy(false); }
  function preview(item: PendingMeterPhoto) { const url = URL.createObjectURL(item.content); window.open(url, "_blank", "noopener,noreferrer"); window.setTimeout(() => URL.revokeObjectURL(url), 60_000); }
  async function remove(item: PendingMeterPhoto) { if (!window.confirm(`Lokales Foto „${item.fileName}“ endgültig aus der Warteschlange löschen?`)) return; await removePendingMeterPhoto(item.id); await load(); setMessage("Lokales Foto wurde gelöscht. Die bereits gespeicherte Ablesung bleibt erhalten."); }
  return <section className="data-workspace pending-photos"><div className="data-toolbar"><span>{items.length ? `${items.length} offene Foto-Uploads` : "Keine offenen Foto-Uploads"}</span><button disabled={!items.length || !online || busy} onClick={retryAll}>{busy ? "Uploads laufen …" : "Alle erneut versuchen"}</button></div><p className={online ? "context-note" : "notice"}>{online ? "Fotos bleiben bis zum erfolgreichen Upload ausschließlich in diesem Browser und diesem Vereinskontext gespeichert." : "Keine Internetverbindung. Die Fotos bleiben lokal gespeichert."}</p>{message && <p className="notice" role="status">{message}</p>}<div className="pending-photo-list">{items.map((item) => <article key={item.id}><LocalPhotoThumbnail item={item} /><div><strong>{item.fileName}</strong><span>Garten {item.details.garten} · {item.details.medium} · Ablesung #{item.readingId}</span><span>{new Intl.DateTimeFormat("de-DE", { dateStyle: "short", timeStyle: "short" }).format(new Date(item.createdAt))} · Versuch {item.attemptCount}</span><small>{item.status === "failed" ? `Fehlgeschlagen: ${item.lastError ?? "erneuter Versuch möglich"}` : item.status === "uploading" ? "Wird hochgeladen …" : "Lokal gespeichert, Upload ausstehend"}</small></div><div><button className="secondary-action" onClick={() => preview(item)}>Anzeigen</button><button disabled={!online || busy} onClick={async () => { setBusy(true); const ok = await uploadOne(item); await load(); setMessage(ok ? "Foto wurde hochgeladen und mit der Ablesung verknüpft." : "Upload fehlgeschlagen. Das Foto bleibt lokal gespeichert."); setBusy(false); }}>Erneut versuchen</button><button className="danger-action" disabled={busy} onClick={() => remove(item)}>Löschen</button></div></article>)}{items.length === 0 && <p className="empty-state">Es sind keine lokalen Ablesefotos vorgemerkt.</p>}</div></section>;
}

function LocalPhotoThumbnail({ item }: { item: PendingMeterPhoto }) {
  const [url, setUrl] = useState("");
  useEffect(() => { const next = URL.createObjectURL(item.content); setUrl(next); return () => URL.revokeObjectURL(next); }, [item.id, item.content]);
  return <div className="pending-photo-thumb">{url && <img src={url} alt="Lokale Vorschau des Ablesefotos" />}</div>;
}

function ReadingReview({ session, reading, memberName, history, reviewerMemberId, onSaved }: { session: BrowserSession; reading: Reading | null; memberName: string; history: Reading[]; reviewerMemberId: number | null; onSaved: () => void }) {
  const [comment, setComment] = useState(""); const [date, setDate] = useState(""); const [value, setValue] = useState(""); const [saving, setSaving] = useState(false); const [error, setError] = useState("");
  const editLock = useEditLock(session, "zaehler_ablesung", reading?.id, Boolean(reading));
  useEffect(() => { setComment(""); setDate(reading?.ablesedatum?.slice(0, 10) ?? ""); setValue(reading ? String(reading.stand) : ""); setError(""); }, [reading?.id]);
  useEffect(() => { if (editLock.message) queueMicrotask(() => setError(editLock.message)); }, [editLock.message]);
  async function decide(action: "freigegeben" | "abgelehnt" | "korrigiert" | "entfernt") { if (!reading || !reviewerMemberId) { setError("Für die Prüfung ist ein verknüpftes Mitgliedskonto erforderlich."); return; } if (!editLock.acquired) { setError(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; } if (!comment.trim()) { setError(action === "entfernt" ? "Eine Löschbegründung ist erforderlich." : "Ein Prüfkommentar ist erforderlich."); return; } const stand = Number(value.replace(",", ".")); if (action === "korrigiert" && (!date || !Number.isFinite(stand) || stand < 0)) { setError("Für die Korrektur müssen Datum und Zählerstand gültig sein."); return; } if (action === "korrigiert" && !window.confirm("Die Ablesung mit den geänderten Werten korrigieren und direkt freigeben?")) return; if (action === "entfernt" && !window.confirm("Die Ablesung mit Begründung aus dem offenen Prüfprozess entfernen?")) return; setSaving(true); setError(""); try { const rejected = action === "abgelehnt" || action === "entfernt"; const prefix = action === "korrigiert" ? "Korrigiert im Prüfprozess: " : action === "entfernt" ? "Im Prüfprozess entfernt: " : ""; await writeSupabase<Reading>(session, "zaehler_ablesung", "PATCH", { ablesedatum: action === "korrigiert" ? date : reading.ablesedatum, stand: action === "korrigiert" ? stand : reading.stand, pruefstatus: rejected ? "abgelehnt" : "freigegeben", pruefkommentar: `${prefix}${comment.trim()}`, geprueft_von: reviewerMemberId, geprueft_am: new Date().toISOString(), freigegeben: !rejected }, { id: `eq.${reading.id}` }); onSaved(); } catch (cause) { setError(cause instanceof Error ? cause.message : "Die Entscheidung konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  return <aside className="detail-panel review-panel"><h2>Ablesung prüfen</h2>{reading ? <><p><strong>{memberName}</strong><br />{formatDate(reading.ablesedatum)} · Stand <strong>{reading.stand}</strong> · {reading.art.toUpperCase()}</p>{reading.foto_drive_file_id || reading.foto_pfad ? <PhotoOpenButton session={session} reading={reading} /> : <p className="context-note">Zu dieser Ablesung ist kein Foto hinterlegt.</p>}{error && <p className="notice">{error}</p>}<label>Prüfkommentar / Begründung *<textarea value={comment} onChange={(event) => setComment(event.target.value)} /></label><details><summary>Korrekturwerte</summary><label>Datum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Zählerstand<input inputMode="decimal" value={value} onChange={(event) => setValue(event.target.value)} /></label></details><div className="review-actions meter-review-actions"><button disabled={saving} onClick={() => decide("freigegeben")}>Freigeben</button><button disabled={saving} className="secondary-action" onClick={() => decide("korrigiert")}>Korrigieren</button><button disabled={saving} className="reject-action" onClick={() => decide("abgelehnt")}>Ablehnen</button><button disabled={saving} className="danger-action" onClick={() => decide("entfernt")}>Entfernen</button></div><h3>Letzte Ablesungen dieses Zählers</h3>{history.length ? <ul className="review-history">{history.map((item) => <li key={item.id}><strong>{item.stand} · {item.art.toUpperCase()}</strong><span>{formatDate(item.ablesedatum)} · {item.freigegeben ? "freigegeben" : item.pruefstatus}</span>{item.pruefkommentar && <p>{item.pruefkommentar}</p>}</li>)}</ul> : <p>Noch keine vorherige Ablesung vorhanden.</p>}</> : <p>Wähle links eine eingereichte Ablesung aus.</p>}</aside>;
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

function currentLocalDateTime() {
  const now = new Date();
  const offset = now.getTimezoneOffset() * 60_000;
  return new Date(now.getTime() - offset).toISOString().slice(0, 16);
}

function WorkHoursOverview({ session, reviewerMemberId }: { session: BrowserSession; reviewerMemberId: number | null }) {
  const [items, setItems] = useState<WorkHour[]>([]); const [selectedId, setSelectedId] = useState<number | null>(null); const [comment, setComment] = useState(""); const [hours, setHours] = useState(""); const [date, setDate] = useState(""); const [workType, setWorkType] = useState(""); const [history, setHistory] = useState<WorkHourHistory[]>([]); const [error, setError] = useState(""); const [saving, setSaving] = useState(false);
  const load = () => readSupabase<WorkHour>(session, "arbeitsstunde", { select: "id,mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am", order: "datum.desc", limit: "1000" }).then(setItems).catch((cause: Error) => setError(cause.message));
  useEffect(() => { load(); }, [session]);
  const open = items.filter((item) => !item.freigegeben && item.status !== "abgelehnt"); const selected = open.find((item) => item.id === selectedId) ?? null;
  const editLock = useEditLock(session, "arbeitsstunde", selected?.id, Boolean(selected));
  useEffect(() => { if (!selected) { setHistory([]); return; } setHours(String(selected.stunden)); setDate(selected.datum?.slice(0, 10) ?? ""); setWorkType(selected.art_der_arbeit); readSupabase<WorkHourHistory>(session, "arbeitsstunde_pruefverlauf", { select: "id,arbeitsstunde_id,aktion,begruendung,geprueft_von,geprueft_am,vorher_snapshot,nachher_snapshot", arbeitsstunde_id: `eq.${selected.id}`, order: "geprueft_am.desc" }).then(setHistory).catch(() => setHistory([])); }, [selectedId]);
  useEffect(() => { if (editLock.message) queueMicrotask(() => setError(editLock.message)); }, [editLock.message]);
  async function decide(action: "freigegeben" | "abgelehnt" | "korrigiert") { if (!selected || !reviewerMemberId) { setError("Für die Prüfung ist ein verknüpftes Mitgliedskonto erforderlich."); return; } if (!editLock.acquired) { setError(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; } if (!comment.trim()) { setError("Ein Prüfkommentar ist erforderlich."); return; } const correctedHours = Number(hours.replace(",", ".")); if (action === "korrigiert" && (!Number.isFinite(correctedHours) || correctedHours <= 0 || !workType.trim() || !date)) { setError("Für die Korrektur müssen Datum, Stunden und Art der Arbeit gültig sein."); return; } setSaving(true); setError(""); try { const after: WorkHour = { ...selected, datum: action === "korrigiert" ? date : selected.datum, stunden: action === "korrigiert" ? correctedHours : selected.stunden, art_der_arbeit: action === "korrigiert" ? workType.trim() : selected.art_der_arbeit, status: action === "abgelehnt" ? "abgelehnt" : "genehmigt", freigegeben: action !== "abgelehnt", genehmigt_von: action === "abgelehnt" ? null : reviewerMemberId, genehmigt_am: action === "abgelehnt" ? null : new Date().toISOString() }; await writeSupabase<WorkHour>(session, "arbeitsstunde", "PATCH", { datum: after.datum, stunden: after.stunden, art_der_arbeit: after.art_der_arbeit, status: after.status, freigegeben: after.freigegeben, genehmigt_von: after.genehmigt_von, genehmigt_am: after.genehmigt_am, lockedbyuserid: null, lockat: null }, { id: `eq.${selected.id}` }); await writeSupabase<WorkHourHistory>(session, "arbeitsstunde_pruefverlauf", "POST", { arbeitsstunde_id: selected.id, aktion: action, begruendung: comment.trim(), geprueft_von: reviewerMemberId, geprueft_am: new Date().toISOString(), vorher_snapshot: selected, nachher_snapshot: after }); setComment(""); setSelectedId(null); await load(); } catch (cause) { setError(cause instanceof Error ? cause.message : "Die Entscheidung konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  return <section className="data-workspace"><div className="meter-summary"><div><span>Offen</span><strong>{open.length}</strong></div><div><span>Freigegeben</span><strong>{items.filter((item) => item.freigegeben).length}</strong></div><div><span>Stunden offen</span><strong>{open.reduce((sum, item) => sum + Number(item.stunden), 0)}</strong></div></div>{error && <p className="notice" role="alert">{error}</p>}<div className="split-view review-workspace"><div className="data-table-wrap"><table><thead><tr><th>Datum</th><th>Mitglied</th><th>Arbeit</th><th>Stunden</th></tr></thead><tbody>{open.map((item) => <tr key={item.id} className={selected?.id === item.id ? "selected-row" : ""} onClick={() => setSelectedId(item.id)}><td>{formatDate(item.datum)}</td><td>#{item.mitglied_id}</td><td>{item.art_der_arbeit}</td><td>{item.stunden}</td></tr>)}</tbody></table>{open.length === 0 && <p className="empty-state">Keine offenen Arbeitsstunden.</p>}</div><aside className="detail-panel review-panel"><h2>Prüfung</h2>{selected ? <><p><strong>Mitglied #{selected.mitglied_id}</strong><br />{formatDate(selected.datum)} · {selected.stunden} Stunden</p><label>Prüfkommentar *<textarea value={comment} onChange={(event) => setComment(event.target.value)} placeholder="Begründung für die Entscheidung" /></label><details><summary>Korrekturwerte</summary><label>Datum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Stunden<input inputMode="decimal" value={hours} onChange={(event) => setHours(event.target.value)} /></label><label>Art der Arbeit<input value={workType} onChange={(event) => setWorkType(event.target.value)} /></label></details><div className="review-actions"><button disabled={saving} onClick={() => decide("freigegeben")}>Freigeben</button><button disabled={saving} className="secondary-action" onClick={() => decide("korrigiert")}>Korrigieren</button><button disabled={saving} className="reject-action" onClick={() => decide("abgelehnt")}>Ablehnen</button></div><h3>Verlauf</h3>{history.length ? <ul className="review-history">{history.map((entry) => <li key={entry.id}><strong>{entry.aktion}</strong><span>{formatDate(entry.geprueft_am)} · Mitglied #{entry.geprueft_von}</span><p>{entry.begruendung}</p></li>)}</ul> : <p>Noch kein Verlauf vorhanden.</p>}</> : <p>Wähle links eine offene Arbeitsstunde aus.</p>}</aside></div></section>;
}

function OwnWorkHours({ session, memberId, saisonId, canEdit }: { session: BrowserSession; memberId: number; saisonId: number | null; canEdit: boolean }) {
  const [items, setItems] = useState<WorkHour[]>([]); const [history, setHistory] = useState<WorkHourHistory[]>([]); const [requiredHours, setRequiredHours] = useState<number | null>(null); const [date, setDate] = useState(new Date().toISOString().slice(0, 10)); const [hours, setHours] = useState(""); const [workType, setWorkType] = useState(""); const [editingId, setEditingId] = useState<number | null>(null); const [message, setMessage] = useState(""); const [saving, setSaving] = useState(false);
  const load = async () => { setMessage(""); try { const query = { select: "id,mitglied_id,saison_id,datum,stunden,art_der_arbeit,status,freigegeben,genehmigt_von,genehmigt_am", mitglied_id: `eq.${memberId}`, ...(saisonId ? { saison_id: `eq.${saisonId}` } : {}), order: "datum.desc", limit: "500" }; const [workHours, seasons] = await Promise.all([readSupabase<WorkHour>(session, "arbeitsstunde", query), saisonId ? readSupabase<{ id: number; pflichtstunden_soll: number | null }>(session, "saison", { select: "id,pflichtstunden_soll", id: `eq.${saisonId}`, limit: "1" }) : Promise.resolve([])]); setItems(workHours); setRequiredHours(seasons[0]?.pflichtstunden_soll ?? null); if (workHours.length) { const records = await readSupabase<WorkHourHistory>(session, "arbeitsstunde_pruefverlauf", { select: "id,arbeitsstunde_id,aktion,begruendung,geprueft_von,geprueft_am,vorher_snapshot,nachher_snapshot", arbeitsstunde_id: `in.(${workHours.map((item) => item.id).join(",")})`, order: "geprueft_am.desc", limit: "1000" }); setHistory(records); } else setHistory([]); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitsstunden konnten nicht geladen werden."); } };
  useEffect(() => { load(); }, [session, memberId, saisonId]);
  const editLock = useEditLock(session, "arbeitsstunde", editingId, editingId !== null);
  useEffect(() => { if (editingId && editLock.message) queueMicrotask(() => setMessage(editLock.message)); }, [editingId, editLock.message]);
  function resetForm() { setEditingId(null); setDate(new Date().toISOString().slice(0, 10)); setHours(""); setWorkType(""); }
  async function save() { const value = Number(hours.replace(",", ".")); if (!saisonId || !date || !workType.trim() || !Number.isFinite(value) || value <= 0) { setMessage("Saison, Datum, Stunden und Art der Arbeit sind erforderlich."); return; } if (editingId && !editLock.acquired) { setMessage(editLock.message || "Die Bearbeitungssperre wird noch geprüft."); return; } setSaving(true); setMessage(""); try { const payload = { datum: date, stunden: value, art_der_arbeit: workType.trim(), ...(editingId ? {} : { mitglied_id: memberId, saison_id: saisonId, status: "offen", freigegeben: false }) }; if (editingId) await writeSupabase<WorkHour>(session, "arbeitsstunde", "PATCH", payload, { id: `eq.${editingId}` }); else await writeSupabase<WorkHour>(session, "arbeitsstunde", "POST", payload); resetForm(); await load(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitsstunde konnte nicht gespeichert werden."); } finally { setSaving(false); } }
  async function remove(item: WorkHour) { if (item.freigegeben || item.status === "abgelehnt" || !window.confirm("Diese offene Arbeitsstunde wirklich löschen?")) return; try { await deleteSupabase(session, "arbeitsstunde", { id: `eq.${item.id}` }); await load(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Arbeitsstunde konnte nicht gelöscht werden."); } }
  function edit(item: WorkHour) { setEditingId(item.id); setDate(item.datum.slice(0, 10)); setHours(String(item.stunden)); setWorkType(item.art_der_arbeit); }
  const approved = items.filter((item) => item.freigegeben).reduce((sum, item) => sum + Number(item.stunden), 0); const open = items.filter((item) => !item.freigegeben && item.status !== "abgelehnt").reduce((sum, item) => sum + Number(item.stunden), 0); const remaining = requiredHours === null ? null : Math.max(0, requiredHours - approved - open);
  return <section className="data-workspace"><div className="meter-summary"><div><span>Sollstunden</span><strong>{requiredHours === null ? "–" : `${requiredHours} h`}</strong></div><div><span>Freigegeben</span><strong>{approved} h</strong></div><div><span>Noch offen</span><strong>{remaining === null ? `${open} h offen` : `${remaining} h`}</strong></div></div>{message && <p className="notice">{message}</p>}{canEdit && <section className="detail-panel member-editor"><h2>{editingId ? "Arbeitsstunde bearbeiten" : "Arbeitsstunde erfassen"}</h2><fieldset><label>Datum<input type="date" value={date} onChange={(event) => setDate(event.target.value)} /></label><label>Stunden<input inputMode="decimal" value={hours} onChange={(event) => setHours(event.target.value)} /></label><label className="wide">Art der Arbeit<input value={workType} onChange={(event) => setWorkType(event.target.value)} /></label></fieldset><div className="editor-actions"><button disabled={saving || !saisonId} onClick={save}>{editingId ? "Änderung speichern" : "Arbeitsstunde speichern"}</button>{editingId && <button className="secondary-action" onClick={resetForm}>Abbrechen</button>}</div></section>}<div className="split-view"><div className="data-table-wrap"><table><thead><tr><th>Datum</th><th>Arbeit</th><th>Stunden</th><th>Status</th><th></th></tr></thead><tbody>{items.map((item) => <tr key={item.id}><td>{formatDate(item.datum)}</td><td>{item.art_der_arbeit}</td><td>{item.stunden}</td><td>{item.freigegeben ? "Freigegeben" : item.status === "abgelehnt" ? "Abgelehnt" : "Offen"}</td><td>{canEdit && !item.freigegeben && item.status !== "abgelehnt" && <><button className="table-action" onClick={() => edit(item)}>Bearbeiten</button><button className="table-action reject-action" onClick={() => remove(item)}>Löschen</button></>}</td></tr>)}</tbody></table>{items.length === 0 && <p className="empty-state">Noch keine Arbeitsstunden vorhanden.</p>}</div><aside className="detail-panel"><h2>Prüfverlauf</h2>{history.length ? <ul className="review-history">{history.map((entry) => <li key={entry.id}><strong>{entry.aktion}</strong><span>{formatDate(entry.geprueft_am)} · Mitglied #{entry.geprueft_von}</span><p>{entry.begruendung}</p></li>)}</ul> : <p>Noch kein Prüfverlauf für die gewählte Saison vorhanden.</p>}</aside></div></section>;
}

function WorkAssignmentsManagement({ session, canEdit, saisonId, onBack }: { session: BrowserSession; canEdit: boolean; saisonId: number | null; onBack: () => void }) {
  const [items, setItems] = useState<WorkAssignment[]>([]);
  const [selectedId, setSelectedId] = useState<number | null>(null);
  const [creating, setCreating] = useState(false);
  const [draft, setDraft] = useState<Partial<WorkAssignment>>({});
  const [message, setMessage] = useState("");
  const [saving, setSaving] = useState(false);

  function emptyDraft(): Partial<WorkAssignment> {
    const today = new Date().toISOString().slice(0, 10);
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
  const deadlinePassed = Boolean(assignment.anmeldung_bis && new Date(assignment.anmeldung_bis).getTime() < Date.now());
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
    const visibleUntilDate = new Date(`${visibleFrom}:00`);
    visibleUntilDate.setMonth(visibleUntilDate.getMonth() + 1);
    const offset = visibleUntilDate.getTimezoneOffset() * 60_000;
    const visibleUntil = new Date(visibleUntilDate.getTime() - offset).toISOString().slice(0, 16);
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

function SignaturePad({ label, value, onChange }: { label: string; value: string; onChange: (value: string) => void }) {
  const canvasRef = useRef<HTMLCanvasElement | null>(null);
  const drawing = useRef(false);
  function point(event: React.PointerEvent<HTMLCanvasElement>) { const canvas = canvasRef.current!; const rect = canvas.getBoundingClientRect(); return { x: (event.clientX - rect.left) * canvas.width / rect.width, y: (event.clientY - rect.top) * canvas.height / rect.height }; }
  function start(event: React.PointerEvent<HTMLCanvasElement>) { const canvas = canvasRef.current!; const context = canvas.getContext("2d")!; const p = point(event); drawing.current = true; canvas.setPointerCapture(event.pointerId); context.beginPath(); context.moveTo(p.x, p.y); }
  function move(event: React.PointerEvent<HTMLCanvasElement>) { if (!drawing.current) return; const context = canvasRef.current!.getContext("2d")!; const p = point(event); context.lineWidth = 2.2; context.lineCap = "round"; context.strokeStyle = "#172016"; context.lineTo(p.x, p.y); context.stroke(); }
  function stop() { if (!drawing.current || !canvasRef.current) return; drawing.current = false; onChange(canvasRef.current.toDataURL("image/png")); }
  function clear() { const canvas = canvasRef.current; if (canvas) canvas.getContext("2d")?.clearRect(0, 0, canvas.width, canvas.height); onChange(""); }
  return <div className="signature-pad"><div><strong>{label}</strong><span>{value ? "erfasst" : "offen"}</span></div><canvas ref={canvasRef} width="520" height="140" onPointerDown={start} onPointerMove={move} onPointerUp={stop} onPointerCancel={stop} aria-label={label} /><button type="button" className="secondary-action" onClick={clear}>Unterschrift löschen</button></div>;
}

function ContractComposer({ session, memberId, onSaved }: { session: BrowserSession; memberId: number; onSaved: () => Promise<void> }) {
  const [type, setType] = useState<"mitgliedsantrag" | "mitgliedsvertrag" | "pachtvertrag">("mitgliedsantrag");
  const [startDate, setStartDate] = useState(new Date().toISOString().slice(0, 10));
  const [memberFee, setMemberFee] = useState(""); const [admissionFee, setAdmissionFee] = useState("");
  const [parcels, setParcels] = useState<Parcel[]>([]); const [parcelId, setParcelId] = useState("");
  const [memberSignature, setMemberSignature] = useState(""); const [secondarySignature, setSecondarySignature] = useState(""); const [boardSignature, setBoardSignature] = useState("");
  const [message, setMessage] = useState(""); const [busy, setBusy] = useState(false);
  useEffect(() => { readSupabase<ParcelAssignment>(session, "parzellen_belegung", { select: "id,parzelle_id,mitglied_id,von_datum,bis_datum", mitglied_id: `eq.${memberId}`, bis_datum: "is.null" }).then(async (assignments) => { const ids = [...new Set(assignments.map((item) => item.parzelle_id))]; if (!ids.length) { setParcels([]); return; } const found = await readSupabase<Parcel>(session, "parzelle", { select: "id,garten_nr,Anlage,flaeche_qm,hat_strom,hat_wasser,aktiv", id: `in.(${ids.join(",")})`, order: "garten_nr.asc" }); setParcels(found); if (found.length === 1) setParcelId(String(found[0].id)); }).catch(() => setParcels([])); }, [session, memberId]);
  function request(action: "preview" | "finalize") { return { action, type, member_id: memberId, parcel_id: type === "pachtvertrag" ? Number(parcelId) || undefined : undefined, start_date: startDate, member_fee: memberFee ? Number(memberFee.replace(",", ".")) : 0, admission_fee: admissionFee ? Number(admissionFee.replace(",", ".")) : 0, signature_member: memberSignature || undefined, signature_secondary: secondarySignature || undefined, signature_board: boardSignature || undefined }; }
  async function preview() { if (type === "pachtvertrag" && !parcelId) { setMessage("Bitte zuerst eine Parzelle wählen."); return; } setBusy(true); setMessage(""); try { const result = await generateContract(session, request("preview")); if (result.previewUrl) window.open(result.previewUrl, "_blank", "noopener,noreferrer"); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Vorschau konnte nicht erstellt werden."); } finally { setBusy(false); } }
  async function finalize() { if (!memberSignature || !boardSignature) { setMessage("Die Unterschriften des Mitglieds und des Vereins sind erforderlich."); return; } if (type === "pachtvertrag" && !parcelId) { setMessage("Bitte zuerst eine Parzelle wählen."); return; } setBusy(true); setMessage(""); try { const result = await generateContract(session, request("finalize")); setMessage(result.message ?? "Das signierte Dokument wurde gespeichert."); setMemberSignature(""); setSecondarySignature(""); setBoardSignature(""); await onSaved(); } catch (cause) { setMessage(cause instanceof Error ? cause.message : "Vertrag konnte nicht gespeichert werden."); } finally { setBusy(false); } }
  return <details className="contract-composer"><summary>Vertragsdokument erstellen und unterschreiben</summary>{message && <p className="notice" role="status">{message}</p>}<fieldset disabled={busy}><label>Dokumenttyp<select value={type} onChange={(event) => setType(event.target.value as typeof type)}><option value="mitgliedsantrag">Mitgliedsantrag</option><option value="mitgliedsvertrag">Mitgliedsvertrag</option><option value="pachtvertrag">Pachtvertrag</option></select></label><label>Beginn<input type="date" value={startDate} onChange={(event) => setStartDate(event.target.value)} /></label>{type === "mitgliedsantrag" && <><label>Mitgliedsbeitrag jährlich<input inputMode="decimal" value={memberFee} onChange={(event) => setMemberFee(event.target.value)} /></label><label>Aufnahmegebühr<input inputMode="decimal" value={admissionFee} onChange={(event) => setAdmissionFee(event.target.value)} /></label></>}{type === "pachtvertrag" && <label className="wide">Parzelle<select value={parcelId} onChange={(event) => setParcelId(event.target.value)}><option value="">Parzelle wählen</option>{parcels.map((parcel) => <option key={parcel.id} value={parcel.id}>{parcel.garten_nr} – {parcel.Anlage}</option>)}</select></label>}</fieldset><div className="signature-grid"><SignaturePad label={type === "pachtvertrag" ? "Unterschrift Pächter 1" : "Unterschrift Mitglied"} value={memberSignature} onChange={setMemberSignature} /><SignaturePad label={type === "pachtvertrag" ? "Unterschrift Pächter 2 / Vertreter (optional)" : "Gesetzlicher Vertreter (optional)"} value={secondarySignature} onChange={setSecondarySignature} /><SignaturePad label="Unterschrift Verein" value={boardSignature} onChange={setBoardSignature} /></div><div className="editor-actions"><button type="button" className="secondary-action" disabled={busy} onClick={preview}>PDF-Vorschau</button><button type="button" disabled={busy} onClick={finalize}>{busy ? "Verarbeitet …" : "Signiert sicher ablegen"}</button></div></details>;
}

function DocumentList({ session, memberId, parcelId, compact = false, canManage = false }: { session: BrowserSession; memberId?: number; parcelId?: number; compact?: boolean; canManage?: boolean }) {
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
  async function openDocument(item: DocumentRecord) {
    setOpening(item.id); setError("");
    try { const documentUrl = item.drive_file_id ? await openDriveDocument(session, item.id) : item.bucket && item.storage_path ? await createDocumentOpenUrl(session, item.bucket, item.storage_path) : null; if (!documentUrl) throw new Error("Für dieses Dokument fehlt eine sichere Ablage."); window.open(documentUrl, "_blank", "noopener,noreferrer"); } catch (cause) { setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht geöffnet werden."); } finally { setOpening(null); }
  }
  async function upload() { if (!file || !ownerId || !title.trim()) { setError("Titel und Datei sind erforderlich."); return; } setUploading(true); setError(""); try { const uploaded = await uploadDocument(session, file, { kind: ownerKind, id: ownerId, title: title.trim() }); await writeSupabase<DocumentRecord>(session, "dokument", "POST", { mitglied_id: memberId ?? null, parzelle_id: parcelId ?? null, bucket: "dokumente", storage_path: uploaded.storagePath, drive_file_id: uploaded.driveFileId, titel: title.trim(), dateiname: uploaded.fileName, mime_type: uploaded.mimeType, size_bytes: uploaded.sizeBytes }); setFile(null); setTitle(""); await load(); } catch (cause) { setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht gespeichert werden."); } finally { setUploading(false); } }
  async function archive(item: DocumentRecord) { const reason = window.prompt("Begründung für die Archivierung (mindestens 3 Zeichen):"); if (!reason) return; const password = window.prompt("Archivpasswort:"); if (!password) return; setArchiving(item.id); setError(""); try { await archiveDocument(session, item.id, password, reason); await load(); } catch (cause) { setError(cause instanceof Error ? cause.message : "Das Dokument konnte nicht archiviert werden."); } finally { setArchiving(null); } }
  const activeItems = items.filter((item) => !item.archiviert_at);
  return <section className={compact ? "parcel-documents" : "data-workspace"} aria-label="Dokumente">{compact ? <h3>Parzellen-Dokumente</h3> : <p className="document-intro">Dokumente werden ausschließlich über den geschützten Vereins-Dokumentendienst geöffnet. Die Browser-App speichert keine Dokumentkopie lokal.</p>}{error && <p className="notice" role="alert">{error}</p>}{canManage && memberId && <ContractComposer session={session} memberId={memberId} onSaved={load} />}{canManage && <fieldset className="document-upload" disabled={uploading}><label>Titel<input value={title} onChange={(event) => setTitle(event.target.value)} placeholder="z. B. Pachtvertrag 2026" /></label><label>Datei<input type="file" accept="application/pdf,image/*,.doc,.docx,.odt" onChange={(event) => setFile(event.target.files?.[0] ?? null)} /></label><button type="button" onClick={upload}>{uploading ? "Lädt hoch …" : "Dokument hochladen"}</button></fieldset>}<div className="data-table-wrap"><table><thead><tr><th>Titel</th><th>Datei</th><th>Geändert</th><th>Größe</th><th></th></tr></thead><tbody>{activeItems.map((item) => <tr key={item.id}><td><strong>{item.titel ?? "Ohne Titel"}</strong></td><td>{item.dateiname ?? "–"}</td><td>{formatDate(item.updated_at)}</td><td>{formatBytes(item.size_bytes)}</td><td><button className="table-action" disabled={(!item.drive_file_id && (!item.bucket || !item.storage_path)) || opening === item.id} onClick={() => openDocument(item)}>{opening === item.id ? "Öffne …" : item.mime_type === "application/pdf" ? "Vorschau" : "Öffnen"}</button>{canManage && <button className="table-action secondary-action" disabled={archiving === item.id} onClick={() => archive(item)}>{archiving === item.id ? "Archiviert …" : "Archivieren"}</button>}</td></tr>)}</tbody></table>{activeItems.length === 0 && <p className="empty-state">Keine aktiven Dokumente vorhanden.</p>}</div>{!compact && <p className="detail-hint">Mitgliedsantrag und Pachtvertrag verwenden die offiziellen PDF-Vorlagen. Vorschau, Unterschriften und sichere Ablage erfolgen über den geschützten Server-Dienst.</p>}</section>;
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
