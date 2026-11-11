import { Link } from 'react-router';

import { Badge } from '@/components/ui/badge';

import type { OnboardingAttentionItem, OnboardingAttentionReason } from '../types';

const reasonLabel: Record<OnboardingAttentionReason, string> = {
  OverdueSteps: 'Overdue steps',
  RepeatedQuizFailures: 'Failed quiz repeatedly',
  AwaitingReview: 'Waiting for review',
  NotStarted: 'Not started'
};

const reasonTone: Record<OnboardingAttentionReason, 'negative' | 'warning' | 'neutral'> = {
  OverdueSteps: 'negative',
  RepeatedQuizFailures: 'negative',
  AwaitingReview: 'warning',
  NotStarted: 'neutral'
};

/** One person who needs attention, with the reasons as text badges. The name opens their onboarding. */
export function AttentionRow({ item }: { item: OnboardingAttentionItem }) {
  return (
    <div className="assignment-row">
      <div>
        <strong>
          <Link to={`/employees/${item.employeeId}?tab=onboarding`}>{item.employeeName}</Link>
        </strong>
        <small>
          {item.journeyName} · started {new Date(item.startDate).toLocaleDateString()}
          {item.overdueSteps ? ` · ${item.overdueSteps} overdue` : ''}
          {item.failedAttempts ? ` · ${item.failedAttempts} failed attempts` : ''}
        </small>
      </div>
      <div className="row-actions">
        {item.reasons.map((reason) => (
          <Badge key={reason} tone={reasonTone[reason]}>
            {reasonLabel[reason]}
          </Badge>
        ))}
      </div>
    </div>
  );
}
