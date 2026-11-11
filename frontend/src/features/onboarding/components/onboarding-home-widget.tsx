import { useQuery } from '@tanstack/react-query';
import { ArrowRight, ListChecks, UserPlus } from 'lucide-react';
import { Link } from 'react-router';

import { Card } from '@/components/ui/card';

import { getOnboardingInsights } from '../api/onboarding-api';
import { useOnboardingSetup } from '../hooks/use-onboarding-setup';

/** HR's onboarding at a glance on Home: the next setup step, who is in onboarding, and the two main actions. */
export function OnboardingHomeWidget() {
  const setup = useOnboardingSetup();
  const insights = useQuery({ queryKey: ['onboarding', 'insights'], queryFn: getOnboardingInsights });
  const people = insights.data;

  return (
    <Card className="home-widget home-widget--onboarding">
      <header>
        <span>
          <ListChecks size={19} />
        </span>
        <h2>Onboarding</h2>
      </header>
      <div className="home-onboarding">
        <div>
          <p className="home-onboarding__status">
            {setup.loading ? (
              'Checking setup…'
            ) : setup.next ? (
              <>
                Next setup step: <strong>{setup.next.label.toLowerCase()}</strong> ({setup.next.detail.toLowerCase()}) ·{' '}
                <Link to={setup.next.to}>{setup.next.actionLabel}</Link>
              </>
            ) : (
              'Setup is complete: documents, packages, and quizzes are ready.'
            )}
          </p>
          {people ? (
            <p>
              <strong>{people.activeJourneys}</strong> in onboarding · <strong>{people.awaitingReview}</strong> waiting
              for review · <strong>{people.needsAttention.length}</strong> need attention
            </p>
          ) : null}
        </div>
        <div className="row-actions">
          <Link className="button button--secondary button--small" to="/onboarding">
            Open onboarding <ArrowRight size={14} />
          </Link>
          <Link className="button button--primary button--small" to="/employees/new-hire">
            <UserPlus size={14} /> Add a new hire
          </Link>
        </div>
      </div>
    </Card>
  );
}
