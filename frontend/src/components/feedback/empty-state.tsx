import { Inbox } from 'lucide-react';

import { Button } from '@/components/ui/button';

interface EmptyStateProps {
  title: string;
  description: string;
  actionLabel?: string;
  onAction?: () => void;
}

export function EmptyState({ title, description, actionLabel, onAction }: EmptyStateProps) {
  return (
    <div className="state-panel">
      <span className="state-panel__icon"><Inbox size={22} /></span>
      <h3>{title}</h3><p>{description}</p>
      {actionLabel && onAction ? <Button size="small" onClick={onAction}>{actionLabel}</Button> : null}
    </div>
  );
}
