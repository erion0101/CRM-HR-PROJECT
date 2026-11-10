import type { PropsWithChildren } from 'react';

interface BadgeProps {
  tone?: 'neutral' | 'positive' | 'warning' | 'negative' | 'info';
  className?: string;
}

export function Badge({ children, tone = 'neutral', className = '' }: PropsWithChildren<BadgeProps>) {
  return <span className={`badge badge--${tone} ${className}`}>{children}</span>;
}
