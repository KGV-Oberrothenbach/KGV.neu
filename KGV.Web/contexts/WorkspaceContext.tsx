"use client";

import { createContext, type ReactNode, useCallback, useContext, useMemo, useState } from "react";
import { loadWorkspaceContext, saveWorkspaceContext, type WorkspaceContext as PersistedWorkspaceContext } from "../lib/supabase-auth";

export type WorkspaceMember = Pick<{ id: number; vorname: string | null; name: string | null }, "id" | "vorname" | "name">;
export type WorkspaceSeason = { id: number; jahr: number };

type WorkspaceContextValue = {
  workspaceContext: PersistedWorkspaceContext;
  selectedMember: WorkspaceMember | null;
  creatingMember: boolean;
  setSelectedMember: (member: WorkspaceMember | null) => void;
  setCreatingMember: (creatingMember: boolean) => void;
  updateWorkspaceContext: (next: PersistedWorkspaceContext | ((current: PersistedWorkspaceContext) => PersistedWorkspaceContext)) => void;
  selectMember: (mitgliedId: number) => void;
  selectParcel: (parzelleId: number) => void;
  selectSeason: (season: WorkspaceSeason) => void;
};

const WorkspaceContext = createContext<WorkspaceContextValue | null>(null);

export function WorkspaceContextProvider({ children }: { children: ReactNode }) {
  const [workspaceContext, setWorkspaceContext] = useState(() => loadWorkspaceContext());
  const [selectedMember, setSelectedMember] = useState<WorkspaceMember | null>(null);
  const [creatingMember, setCreatingMember] = useState(false);

  const updateWorkspaceContext = useCallback((next: PersistedWorkspaceContext | ((current: PersistedWorkspaceContext) => PersistedWorkspaceContext)) => {
    setWorkspaceContext((current) => {
      const updated = typeof next === "function" ? next(current) : next;
      saveWorkspaceContext(updated);
      return updated;
    });
  }, []);

  const selectMember = useCallback((mitgliedId: number) => {
    setCreatingMember(false);
    updateWorkspaceContext((current) => ({ ...current, mitgliedId, parzelleId: null }));
  }, [updateWorkspaceContext]);

  const selectParcel = useCallback((parzelleId: number) => {
    updateWorkspaceContext((current) => ({ ...current, parzelleId }));
  }, [updateWorkspaceContext]);

  const selectSeason = useCallback((season: WorkspaceSeason) => {
    updateWorkspaceContext((current) => ({ ...current, saisonId: season.id, saisonJahr: season.jahr }));
  }, [updateWorkspaceContext]);

  const value = useMemo(() => ({ workspaceContext, selectedMember, creatingMember, setSelectedMember, setCreatingMember, updateWorkspaceContext, selectMember, selectParcel, selectSeason }), [workspaceContext, selectedMember, creatingMember, updateWorkspaceContext, selectMember, selectParcel, selectSeason]);
  return <WorkspaceContext.Provider value={value}>{children}</WorkspaceContext.Provider>;
}

export function useWorkspaceContext() {
  const context = useContext(WorkspaceContext);
  if (!context) throw new Error("useWorkspaceContext must be used within a WorkspaceContextProvider");
  return context;
}
