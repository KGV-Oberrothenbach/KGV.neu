"use client";

import { useState } from "react";

export type NavigationItem = { id: string; label: string; detail: string; depth?: number };
export type NavigationGroup = { id: string; label: string; kind: "item" | "group"; items: NavigationItem[]; target?: NavigationItem };

export default function Navigation({ groups, activeId, onNavigate, memberLabel }: { groups: NavigationGroup[]; activeId: string; onNavigate: (id: string) => void; memberLabel: string | null }) {
  const groupForActiveItem = groups.find((group) => group.items.some((item) => item.id === activeId))?.id;
  const [navigationState, setNavigationState] = useState(() => ({
    activeGroup: groupForActiveItem,
    expandedGroups: groupForActiveItem ? [groupForActiveItem] : [],
  }));
  if (navigationState.activeGroup !== groupForActiveItem) {
    setNavigationState((current) => ({
      activeGroup: groupForActiveItem,
      expandedGroups: groupForActiveItem && !current.expandedGroups.includes(groupForActiveItem)
        ? [...current.expandedGroups, groupForActiveItem]
        : current.expandedGroups,
    }));
  }
  const { expandedGroups } = navigationState;

  const toggleGroup = (groupId: string) => setNavigationState((current) => ({
    ...current,
    expandedGroups: current.expandedGroups.includes(groupId)
      ? current.expandedGroups.filter((id) => id !== groupId)
      : [...current.expandedGroups, groupId],
  }));

  return <nav className="navigation-list">{groups.map((group) => {
    const expanded = expandedGroups.includes(group.id);
    const hasMemberChildren = group.id === "members" && group.items.some((item) => item.depth === 2);
    if (group.kind === "item" && group.target) {
      return <section className={hasMemberChildren ? "nav-group expanded" : ""} key={group.id}>
        <button className={group.target.id === activeId ? "nav-root-item active" : "nav-root-item"} onClick={() => onNavigate(group.target!.id)}>{group.label}</button>
        {hasMemberChildren && <div className="nav-group-items">{group.items.map((item, index) => <div key={item.id}>
          {item.depth === 2 && (index === 0 || group.items[index - 1]?.depth !== 2) && <p className="nav-member-context">{memberLabel}</p>}
          <button className={`nav-item nav-depth-${item.depth ?? 1}${item.id === activeId ? " active" : ""}`} onClick={() => onNavigate(item.id)}>{item.label}</button>
        </div>)}</div>}
      </section>;
    }
    return <section className={expanded ? "nav-group expanded" : "nav-group"} key={group.id}>
      <button className="nav-group-toggle" aria-expanded={expanded} onClick={() => toggleGroup(group.id)}><span>{group.label}</span><span className="nav-chevron" aria-hidden="true">›</span></button>
      {expanded && <div className="nav-group-items">{group.items.map((item) => <button key={item.id} className={`nav-item nav-depth-${item.depth ?? 1}${item.id === activeId ? " active" : ""}`} onClick={() => onNavigate(item.id)}>{item.label}</button>)}</div>}
    </section>;
  })}</nav>;
}
