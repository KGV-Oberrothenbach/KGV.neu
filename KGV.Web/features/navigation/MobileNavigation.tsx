"use client";

import type { NavigationGroup } from "./Navigation";

export default function MobileNavigation({ groups, activeId, onNavigate, selectedMemberLabel }: { groups: NavigationGroup[]; activeId: string; onNavigate: (id: string) => void; selectedMemberLabel: string | null }) {
  return (
    <div className="mobile-navigation">
      <label htmlFor="mobile-page">Bereich</label>
      <select id="mobile-page" value={activeId} onChange={(event) => onNavigate(event.target.value)}>
        {groups.map((group) =>
          group.standalone && group.target ? (
            <option key={group.id} value={group.target.id}>{group.label}</option>
          ) : (
            <optgroup key={group.id} label={group.label}>
              {group.target && <option value={group.target.id}>{group.target.label}</option>}
              {group.items.map((item) => (
                <option key={item.id} value={item.id}>{item.depth === 2 ? `${selectedMemberLabel}: ${item.label}` : item.label}</option>
              ))}
            </optgroup>
          )
        )}
      </select>
    </div>
  );
}
