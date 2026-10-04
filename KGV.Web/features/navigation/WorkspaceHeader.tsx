"use client";

import { ChangeClubAction } from "../auth/ChangeClubAction";

export default function WorkspaceHeader({ clubName, email, role, onLogout, onChangeClub }: { clubName: string; email: string; role: string; onLogout: () => void; onChangeClub: () => Promise<void> }) {
  return (
    <header className="workspace-header">
      <div className="brand"><span className="brand-mark">K</span><span>{clubName}</span></div>
      <div className="account">
        <span>{email}</span>
        <span className="role-pill">{role}</span>
        <ChangeClubAction className="text-button" label="Verein wechseln" onConfirm={onChangeClub} />
        <button className="text-button" onClick={onLogout}>Abmelden</button>
      </div>
    </header>
  );
}
