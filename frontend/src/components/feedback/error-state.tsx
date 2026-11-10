import { CircleAlert } from 'lucide-react';

import { Button } from '@/components/ui/button';

interface ErrorStateProps {
  title?: string;
  description?: string;
  onRetry?: () => void;
}

export function ErrorState({ title = 'Unable to load this page', description = 'Check your connection and try again.', onRetry }: ErrorStateProps) {
  return (
    <div className="state-panel state-panel--error">
      <span className="state-panel__icon"><CircleAlert size={22} /></span>
      <h3>{title}</h3><p>{description}</p>
      {onRetry ? <Button variant="secondary" size="small" onClick={onRetry}>Try again</Button> : null}
    </div>
  );
}
