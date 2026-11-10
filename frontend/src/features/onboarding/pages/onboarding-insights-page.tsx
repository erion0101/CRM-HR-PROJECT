import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, CheckCircle2, Clock3, Target } from 'lucide-react';
import { Link } from 'react-router';

import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { PageHeader } from '@/components/navigation/page-header';
import { Badge } from '@/components/ui/badge';
import { Card } from '@/components/ui/card';
import { StatCard } from '@/components/ui/stat-card';

import { getOnboardingInsights } from '../api/onboarding-api';
import { OnboardingWorkspaceTabs } from '../components/onboarding-workspace-tabs';
import type {
  OnboardingAttentionItem,
  OnboardingAttentionReason,
  OnboardingDocumentDifficulty,
  OnboardingInsights
} from '../types';

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

export function OnboardingInsightsPage() {
  const query = useQuery({ queryKey: ['onboarding', 'insights'], queryFn: getOnboardingInsights });

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Onboarding"
        title="Onboarding insights"
        description="How onboarding is going for the people you look after, who needs help, and which documents are hard to understand."
      >
        <OnboardingWorkspaceTabs />
      </PageHeader>
      {query.isLoading ? (
        <Card>
          <Skeleton count={6} />
        </Card>
      ) : query.isError || !query.data ? (
        <Card>
          <ErrorState
            title="Insights are not available"
            description="The onboarding insights could not be loaded."
            onRetry={() => void query.refetch()}
          />
        </Card>
      ) : (
        <InsightsContent insights={query.data} />
      )}
    </div>
  );
}

function InsightsContent({ insights }: { insights: OnboardingInsights }) {
  const passRate = insights.firstAttemptPassRate;
  return (
    <>
      <div className="stats-grid">
        <StatCard
          label="Active journeys"
          value={insights.activeJourneys}
          helper={`${insights.completedLast30Days} completed in the last 30 days`}
          icon={CheckCircle2}
        />
        <StatCard
          label="Overdue"
          value={insights.overdueJourneys}
          helper="Journeys with a required step past its due date"
          icon={Clock3}
          tone="amber"
        />
        <StatCard
          label="Waiting for review"
          value={insights.awaitingReview}
          helper="Journeys with a milestone to approve"
          icon={AlertTriangle}
          tone="purple"
        />
        <StatCard
          label="First-attempt pass rate"
          value={passRate == null ? '—' : `${passRate}%`}
          helper={
            insights.averageAttemptsToPass == null
              ? 'No quiz results yet'
              : `${insights.averageAttemptsToPass} attempts on average to pass`
          }
          icon={Target}
          tone="blue"
        />
      </div>

      <section>
        <div className="section-heading">
          <div>
            <h2>Needs attention</h2>
            <p>People whose onboarding is overdue, stalled, failing repeatedly, or waiting on a reviewer.</p>
          </div>
        </div>
        {insights.needsAttention.length ? (
          <Card className="assignment-list">
            {insights.needsAttention.map((item) => (
              <AttentionRow key={item.assignmentId} item={item} />
            ))}
          </Card>
        ) : (
          <Card>
            <EmptyState
              title="Everyone is on track"
              description="No journey is overdue, stalled, or failing right now."
            />
          </Card>
        )}
      </section>

      <section>
        <div className="section-heading">
          <div>
            <h2>Hardest documents</h2>
            <p>
              Share of quiz answers about each document that were wrong. A high rate usually means the document needs to
              be clearer.
            </p>
          </div>
        </div>
        {insights.documentDifficulty.length ? (
          <Card className="assignment-list" role="list" aria-label="Wrong-answer rate by document">
            {insights.documentDifficulty.map((document) => (
              <DifficultyRow key={document.documentId ?? document.documentLabel} document={document} />
            ))}
          </Card>
        ) : (
          <Card>
            <EmptyState
              title="Not enough quiz answers yet"
              description="Documents appear here once employees have answered at least five questions about them."
            />
          </Card>
        )}
      </section>
    </>
  );
}

function AttentionRow({ item }: { item: OnboardingAttentionItem }) {
  return (
    <div className="assignment-row">
      <div>
        <strong>
          <Link to={`/employees/${item.employeeId}`}>{item.employeeName}</Link>
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

function DifficultyRow({ document }: { document: OnboardingDocumentDifficulty }) {
  const summary = `${document.incorrectRate}% wrong · ${document.incorrectAnswers} of ${document.answers} answers`;
  return (
    <div className="assignment-row" role="listitem" title={`${document.documentLabel}: ${summary}`}>
      <div>
        <strong>{document.documentLabel}</strong>
        <small>{summary}</small>
      </div>
      <div className="mini-progress" aria-hidden="true">
        <span style={{ width: `${Math.min(100, document.incorrectRate)}%` }} />
      </div>
      <span>{document.incorrectRate}%</span>
    </div>
  );
}
