import { useQuery } from '@tanstack/react-query';
import { useLocation, useNavigate } from 'react-router';

import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { PageHeader } from '@/components/navigation/page-header';
import { Card } from '@/components/ui/card';

import { getMyOnboarding, startQuizAttempt, updateOnboardingStep, updateReadingProgress } from '../api/onboarding-api';
import { AssessmentPanel } from '../components/assessment-panel';
import { JourneyOverview } from '../components/journey-overview';
import { JourneySections } from '../components/journey-sections';
import { OnboardingWorkspaceTabs } from '../components/onboarding-workspace-tabs';
import { QuizResultCard } from '../components/quiz-result-card';
import { useOnboardingMutation } from '../hooks/use-onboarding-mutation';
import type {
  OnboardingAssignment,
  OnboardingReadingItem,
  OnboardingReadingStatus,
  OnboardingStep,
  OnboardingStepStatus,
  QuizResult
} from '../types';

const finishedStatuses = ['Completed', 'Cancelled', 'Superseded'];

export function OnboardingPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const query = useQuery({ queryKey: ['onboarding', 'me'], queryFn: getMyOnboarding });

  const updateStep = useOnboardingMutation({
    mutationFn: ({
      assignmentId,
      step,
      status
    }: {
      assignmentId: string;
      step: OnboardingStep;
      status: OnboardingStepStatus;
    }) => updateOnboardingStep(assignmentId, step, status),
    success: { title: 'Onboarding progress updated' },
    errorTitle: 'Could not update progress'
  });
  const updateReading = useOnboardingMutation({
    mutationFn: ({
      assignmentId,
      item,
      status
    }: {
      assignmentId: string;
      item: OnboardingReadingItem;
      status: OnboardingReadingStatus;
    }) => updateReadingProgress(assignmentId, item, status),
    errorTitle: 'Could not update reading progress'
  });
  const startQuiz = useOnboardingMutation({
    mutationFn: startQuizAttempt,
    invalidate: false,
    errorTitle: 'Assessment is not available',
    onSuccess: (attempt) => navigate(`/onboarding/quiz/${attempt.id}`)
  });

  if (query.isLoading)
    return (
      <Card>
        <Skeleton count={8} />
      </Card>
    );
  if (query.isError || !query.data)
    return (
      <Card>
        <ErrorState
          title="Onboarding is not available"
          description="Your account may not yet be linked to an employee profile."
          onRetry={() => void query.refetch()}
        />
      </Card>
    );

  const dashboard = query.data;
  const quizResult = (location.state as { quizResult?: QuizResult } | null)?.quizResult;
  const activeJourney: OnboardingAssignment | undefined =
    dashboard.journeys.find((journey) => !finishedStatuses.includes(journey.status)) ?? dashboard.journeys[0];
  const next = activeJourney?.steps.find((step) => step.status !== 'Completed');
  const subtitle = `${dashboard.position ?? 'Team member'}${dashboard.department ? ` · ${dashboard.department}` : ''}`;

  return (
    <div className="page-stack onboarding-page">
      <PageHeader
        eyebrow="Onboarding"
        title={`Welcome, ${dashboard.employeeName.split(' ')[0]}`}
        description={`${subtitle}. Follow your structured journey and return whenever you need guidance.`}
      >
        <OnboardingWorkspaceTabs />
      </PageHeader>

      {quizResult ? <QuizResultCard result={quizResult} /> : null}

      {activeJourney ? (
        <>
          <JourneyOverview journey={activeJourney} next={next} />
          <AssessmentPanel
            assignment={activeJourney}
            busy={updateReading.isPending || startQuiz.isPending}
            onReading={(item, status) => updateReading.mutate({ assignmentId: activeJourney.id, item, status })}
            onStartQuiz={() => startQuiz.mutate(activeJourney.id)}
          />
          <JourneySections
            assignment={activeJourney}
            busy={updateStep.isPending}
            onUpdate={(step, status) => updateStep.mutate({ assignmentId: activeJourney.id, step, status })}
          />
        </>
      ) : (
        <Card>
          <EmptyState
            title="No onboarding journey assigned"
            description="HR or your manager can assign a published onboarding journey. Company knowledge and projects remain available."
          />
        </Card>
      )}
    </div>
  );
}
