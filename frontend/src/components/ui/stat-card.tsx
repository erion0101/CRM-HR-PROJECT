import type { LucideIcon } from 'lucide-react';

import { Card } from './card';

interface StatCardProps {
  label: string;
  value: string | number;
  helper: string;
  icon: LucideIcon;
  tone?: 'green' | 'blue' | 'amber' | 'purple';
}

export function StatCard({ label, value, helper, icon: Icon, tone = 'green' }: StatCardProps) {
  return (
    <Card className="stat-card">
      <span className={`stat-card__icon stat-card__icon--${tone}`}>
        <Icon size={20} />
      </span>
      <div>
        <p>{label}</p>
        <strong>{value}</strong>
        <small>{helper}</small>
      </div>
    </Card>
  );
}
