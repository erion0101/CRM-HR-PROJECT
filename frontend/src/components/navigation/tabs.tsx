interface TabItem {
  id: string;
  label: string;
}

export function Tabs({ items, activeId, onChange }: { items: TabItem[]; activeId: string; onChange: (id: string) => void }) {
  return <nav className="tabs" aria-label="Employee sections">{items.map((item) => <button type="button" className={activeId === item.id ? 'active' : ''} key={item.id} onClick={() => onChange(item.id)}>{item.label}</button>)}</nav>;
}
