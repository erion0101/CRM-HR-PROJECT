import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, ArrowRight, CheckCircle2, Circle, Clock3, UserPlus, Users } from 'lucide-react';
import { Link } from 'react-router';

import { canManagePeople } from '@/app/access';
import { Skeleton } from '@/components/feedback/skeleton';
import { PageHeader } from '@/components/navigation/page-header';
import { Card } from '@/components/ui/card';
import { StatCard } from '@/components/ui/stat-card';
import { useAuth } from '@/features/auth/hooks/use-auth';

import { getOnboardingInsights } from '../api/onboarding-api';
import { AttentionRow } from '../components/attention-row';
import { OnboardingWorkspaceTabs } from '../components/onboarding-workspace-tabs';
import { useOnboardingSetup } from '../hooks/use-onboarding-setup';

/** The HR and manager landing page for onboarding: is setup finished, and how are people doing? */
export function OnboardingOverviewPage() {
  const { user } = useAuth();
  const isHr = canManagePeople(user);
  const insights = useQuery({ queryKey: ['onboarding', 'insights'], queryFn: getOnboardingInsights });

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Onboarding"
        title="Onboarding"
        description={
          isHr
            ? 'Set up what new hires read and answer, then follow how each person is doing.'
            : 'How onboarding is going for the people you look after.'
        }
        actions={
          isHr ? (
            <Link className="button button--primary button--medium" to="/employees/new-hire">
              <UserPlus size={17} /> Add a new hire
            </Link>
          ) : undefined
        }
      >
        <OnboardingWorkspaceTabs />
      </PageHeader>

      {isHr ? <SetupChecklist /> : null}

      <section className="page-stack">
        <div className="section-heading">
          <div>
            <h2>People</h2>
            <p>Everyone currently in onboarding.</p>
          </div>
          <Link to="/onboarding/progress">
            See everyone <ArrowRight size={15} />
          </Link>
        </div>
        {insights.isLoading ? (
          <Card>
            <Skeleton count={3} />
          </Card>
        ) : insights.data ? (
          <>
            <div className="stats-grid">
              <StatCard
                label="In onboarding"
                value={insights.data.activeJourneys}
                helper={`${insights.data.completedLast30Days} finished in the last 30 days`}
                icon={Users}
              />
              <StatCard
                label="Waiting for review"
                value={insights.data.awaitingReview}
                helper="Milestones to approve"
                icon={CheckCircle2}
                tone="purple"
              />
              <StatCard
                label="Overdue"
                value={insights.data.overdueJourneys}
                helper="A required step is late"
                icon={Clock3}
                tone="amber"
              />
              <StatCard
                label="Need attention"
                value={insights.data.needsAttention.length}
                helper="Overdue, stuck, or failing the quiz"
                icon={AlertTriangle}
                tone="blue"
              />
            </div>
            {insights.data.needsAttention.length ? (
              <Card className="assignment-list">
                {insights.data.needsAttention.slice(0, 5).map((item) => (
                  <AttentionRow key={item.assignmentId} item={item} />
                ))}
                {insights.data.needsAttention.length > 5 ? (
                  <div className="assignment-row">
                    <Link to="/onboarding/insights">
                      See all {insights.data.needsAttention.length} people who need attention
                    </Link>
                  </div>
                ) : null}
              </Card>
            ) : insights.data.activeJourneys ? (
              <p className="field__hint">Everyone is on track.</p>
            ) : (
              <Card>
                <div className="state-panel">
                  <h3>Nobody is in onboarding yet</h3>
                  <p>When you add a new hire, their onboarding shows up here.</p>
                  {isHr ? (
                    <Link className="button button--primary button--small" to="/employees/new-hire">
                      Add a new hire
                    </Link>
                  ) : null}
                </div>
              </Card>
            )}
          </>
        ) : null}
      </section>
    </div>
  );
}

function SetupChecklist() {
  const setup = useOnboardingSetup();
  if (setup.loading)
    return (
      <Card>
        <Skeleton count={4} />
      </Card>
    );
  if (!setup.next)
    return (
      <Card className="setup-done">
        <CheckCircle2 size={20} aria-hidden="true" />
        <span>
          <strong>Onboarding is set up.</strong> Documents, packages, and quizzes are all ready.{' '}
          <Link to="/onboarding/packages">Review packages</Link>
        </span>
      </Card>
    );
  const done = setup.items.filter((item) => item.done).length;
  return (
    <Card className="setup-checklist">
      <header>
        <div>
          <h2>Set up onboarding</h2>
          <p>
            {done} of {setup.items.length} done. Next: <strong>{setup.next.label.toLowerCase()}</strong>.
          </p>
        </div>
        <div
          className="mini-progress"
          role="progressbar"
          aria-label="Setup progress"
          aria-valuenow={done}
          aria-valuemin={0}
          aria-valuemax={setup.items.length}
        >
          <span style={{ width: `${(done / setup.items.length) * 100}%` }} />
        </div>
      </header>
      <ol>
        {setup.items.map((item) => {
          const isNext = item.id === setup.next?.id;
          return (
            <li key={item.id} className={item.done ? 'is-done' : isNext ? 'is-next' : ''}>
              {item.done ? (
                <CheckCircle2 size={18} aria-label="Done" />
              ) : (
                <Circle size={18} aria-label={isNext ? 'Next step' : 'To do'} />
              )}
              <span>
                <strong>{item.label}</strong>
                <small>
                  {item.detail}
                  {isNext ? ' · next step' : ''}
                </small>
              </span>
              {item.done ? null : (
                <Link className={`button button--${isNext ? 'primary' : 'secondary'} button--small`} to={item.to}>
                  {item.actionLabel}
                </Link>
              )}
            </li>
          );
        })}
      </ol>
    </Card>
  );
}
