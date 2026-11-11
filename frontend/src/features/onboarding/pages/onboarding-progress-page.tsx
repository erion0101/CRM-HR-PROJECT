import { useQuery } from '@tanstack/react-query';
import { UserPlus, UserRoundCheck } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router';

import { canManagePeople } from '@/app/access';
import { Skeleton } from '@/components/feedback/skeleton';
import { SearchInput } from '@/components/forms/search-input';
import { PageHeader } from '@/components/navigation/page-header';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useAuth } from '@/features/auth/hooks/use-auth';

import { getOnboardingAssignments, reviewOnboardingStep } from '../api/onboarding-api';
import { OnboardingProgressRow } from '../components/onboarding-progress-row';
import { OnboardingWorkspaceTabs } from '../components/onboarding-workspace-tabs';
import { StartOnboardingDialog } from '../components/start-onboarding-dialog';
import { useOnboardingMutation } from '../hooks/use-onboarding-mutation';
import type { OnboardingAssignment, OnboardingStep } from '../types';

type Filter = 'active' | 'review' | 'completed' | 'all';
const filters: { id: Filter; label: string; matches: (item: OnboardingAssignment) => boolean }[] = [
  {
    id: 'active',
    label: 'In onboarding',
    matches: (item) => !['Completed', 'Cancelled', 'Superseded'].includes(item.status)
  },
  { id: 'review', label: 'Waiting for review', matches: (item) => item.status === 'PendingReview' },
  { id: 'completed', label: 'Completed', matches: (item) => item.status === 'Completed' },
  { id: 'all', label: 'All', matches: (item) => item.status !== 'Superseded' }
];

/** Daily work: everyone in onboarding, with their progress and the approvals waiting for you. */
export function OnboardingProgressPage() {
  const { user } = useAuth();
  const isHr = canManagePeople(user);
  const [filter, setFilter] = useState<Filter>('active');
  const [search, setSearch] = useState('');
  const [starting, setStarting] = useState(false);
  const progress = useQuery({ queryKey: ['onboarding', 'assignments'], queryFn: () => getOnboardingAssignments() });
  const review = useOnboardingMutation({
    mutationFn: ({ assignment, step }: { assignment: OnboardingAssignment; step: OnboardingStep }) =>
      reviewOnboardingStep(assignment.id, step, true),
    success: { title: 'Approved' },
    errorTitle: 'Could not approve'
  });

  const all = progress.data ?? [];
  const term = search.trim().toLowerCase();
  const visible = all
    .filter(filters.find((item) => item.id === filter)!.matches)
    .filter((item) => !term || item.employeeName.toLowerCase().includes(term));

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Onboarding"
        title="Progress"
        description="Everyone in onboarding, how far they are, and what is waiting for you."
        actions={
          <div className="row-actions">
            <Button variant="secondary" onClick={() => setStarting(true)}>
              <UserRoundCheck size={16} /> Start onboarding
            </Button>
            {isHr ? (
              <Link className="button button--primary button--medium" to="/employees/new-hire">
                <UserPlus size={16} /> Add a new hire
              </Link>
            ) : null}
          </div>
        }
      >
        <OnboardingWorkspaceTabs />
      </PageHeader>

      <div className="progress-toolbar">
        <div className="segmented" role="group" aria-label="Show">
          {filters.map((item) => (
            <button
              type="button"
              key={item.id}
              aria-pressed={filter === item.id}
              className={filter === item.id ? 'is-active' : ''}
              onClick={() => setFilter(item.id)}
            >
              {item.label} <span>{all.filter(item.matches).length}</span>
            </button>
          ))}
        </div>
        <SearchInput
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Find a person"
          aria-label="Find a person"
        />
      </div>

      {progress.isLoading ? (
        <Card>
          <Skeleton count={5} />
        </Card>
      ) : visible.length ? (
        <Card className="progress-list">
          {visible.map((assignment) => (
            <OnboardingProgressRow
              key={assignment.id}
              assignment={assignment}
              reviewing={review.isPending}
              onApprove={(step) => review.mutate({ assignment, step })}
            />
          ))}
        </Card>
      ) : (
        <Card>
          <div className="state-panel">
            <h3>{all.length ? 'Nobody matches' : 'Nobody is in onboarding yet'}</h3>
            <p>
              {all.length
                ? 'Try another filter or name.'
                : 'Add a new hire, or start onboarding for someone who is already in the system.'}
            </p>
            {all.length ? null : (
              <div className="row-actions">
                <Button size="small" variant="secondary" onClick={() => setStarting(true)}>
                  Start onboarding
                </Button>
                {isHr ? (
                  <Link className="button button--primary button--small" to="/employees/new-hire">
                    Add a new hire
                  </Link>
                ) : null}
              </div>
            )}
          </div>
        </Card>
      )}

      <StartOnboardingDialog open={starting} onClose={() => setStarting(false)} />
    </div>
  );
}
