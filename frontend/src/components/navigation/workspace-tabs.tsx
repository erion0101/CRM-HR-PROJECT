import type { LucideIcon } from 'lucide-react';
import { NavLink } from 'react-router';

export interface WorkspaceTab {
  label: string;
  to: string;
  icon: LucideIcon;
  end?: boolean;
}

export function WorkspaceTabs({ label, items }: { label: string; items: WorkspaceTab[] }) {
  return <nav className="workspace-tabs" aria-label={label}>{items.map(({ label: itemLabel, to, icon: Icon, end }) => <NavLink key={to} to={to} end={end}><Icon size={17} /><span>{itemLabel}</span></NavLink>)}</nav>;
}
