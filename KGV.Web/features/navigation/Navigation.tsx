"use client";

export type NavigationItem = { id: string; label: string; detail: string; depth?: number };
export type NavigationGroup = { id: string; label: string; items: NavigationItem[]; target?: NavigationItem; standalone?: boolean };

export default function Navigation({ groups, activeId, onNavigate, memberLabel }: { groups: NavigationGroup[]; activeId: string; onNavigate: (id: string) => void; memberLabel: string | null }) {
  return <nav className="navigation-list">{groups.map((group) => {
    const expanded = group.target?.id === activeId || group.items.some((item) => item.id === activeId);
    const hasMemberChildren = group.id === "members" && group.items.some((item) => item.depth === 2);
    if (group.standalone && group.target) {
      return <button key={group.id} className={group.target.id === activeId ? "nav-root-item active" : "nav-root-item"} onClick={() => onNavigate(group.target!.id)}>{group.label}</button>;
    }
    const destination = group.target?.id ?? group.items[0]?.id;
    return <section className={expanded ? "nav-group expanded" : "nav-group"} key={group.id}>
      <button className={`${group.target?.id === activeId ? "nav-group-toggle active" : "nav-group-toggle"}`} aria-expanded={expanded} onClick={() => destination && onNavigate(destination)}><span>{group.label}</span><span className="nav-chevron" aria-hidden="true">›</span></button>
      {expanded && <div className="nav-group-items">
        {group.items.map((item, index) => <div key={item.id}>
          {hasMemberChildren && item.depth === 2 && (index === 0 || group.items[index - 1]?.depth !== 2) && <p className="nav-member-context">{memberLabel}</p>}
          <button className={`nav-item nav-depth-${item.depth ?? 1}${item.id === activeId ? " active" : ""}`} onClick={() => onNavigate(item.id)}>{item.label}</button>
        </div>)}
      </div>}
    </section>;
  })}</nav>;
}
