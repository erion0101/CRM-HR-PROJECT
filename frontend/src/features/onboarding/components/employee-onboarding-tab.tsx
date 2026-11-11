import { useQuery } from '@tanstack/react-query';
import { CheckCircle2, Circle, Clock3, FileText, UserRoundCheck } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router';

import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import { getOnboardingAssignments } from '../api/onboarding-api';
import type { OnboardingAssignment } from '../types';
import { quizStatus, statusOf } from '../utils/onboarding-labels';
import { StartOnboardingDialog } from './start-onboarding-dialog';

const readingTypes = ['ReadDocument', 'ReadArticle', 'PolicyAcknowledgment'];

/** A person's onboarding on their profile: what they have, how far they are, and a way to start it. */
export function EmployeeOnboardingTab({ employeeId, employeeName }: { employeeId: string; employeeName: string }) {
  const [starting, setStarting] = useState(false);
  const journeys = useQuery({
    queryKey: ['onboarding', 'assignments', employeeId],
    queryFn: () => getOnboardingAssignments(employeeId)
  });

  if (journeys.isLoading)
    return (
      <Card>
        <Skeleton count={4} />
      </Card>
    );
  if (journeys.isError)
    return (
      <Card>
        <ErrorState
          title="Onboarding could not be loaded"
          description="Try again in a moment."
          onRetry={() => void journeys.refetch()}
        />
      </Card>
    );

  const visible = (journeys.data ?? []).filter((item) => item.status !== 'Superseded');
  const open = visible.filter((item) => !['Completed', 'Cancelled'].includes(item.status));

  return (
    <div className="page-stack">
      {visible.length ? (
        <>
          {visible.map((journey) => (
            <JourneyCard key={journey.id} journey={journey} />
          ))}
          <div className="row-actions">
            <Button variant="secondary" size="small" onClick={() => setStarting(true)}>
              <UserRoundCheck size={15} /> {open.length ? 'Start another onboarding' : 'Start onboarding again'}
            </Button>
          </div>
        </>
      ) : (
        <Card>
          <div className="state-panel">
            <h3>No onboarding yet</h3>
            <p>
              Start onboarding to give {employeeName} the documents to read and the quiz. You choose the package and
              which documents they need.
            </p>
            <Button size="small" onClick={() => setStarting(true)}>
              <UserRoundCheck size={15} /> Start onboarding
            </Button>
          </div>
        </Card>
      )}
      <StartOnboardingDialog
        open={starting}
        employee={{ id: employeeId, name: employeeName }}
        onClose={() => setStarting(false)}
      />
    </div>
  );
}

function JourneyCard({ journey }: { journey: OnboardingAssignment }) {
  const status = statusOf(journey.status);
  const assessment = journey.assessment;
  const assessed = assessment && assessment.status !== 'NotConfigured';
  // With a quiz, reading progress lives in the current reading cycle; otherwise in the reading steps themselves.
  const documents = assessed
    ? assessment.readingItems.map((item) => ({ id: item.id, title: item.title, done: item.status === 'Read' }))
    : journey.steps
        .filter((step) => readingTypes.includes(step.stepType))
        .map((step) => ({ id: step.id, title: step.title, done: step.status === 'Completed' }));
  const otherSteps = journey.steps.filter((step) => !readingTypes.includes(step.stepType));
  const lastAttempt = assessment?.attempts[0];
  const overdue = journey.steps.filter((step) => step.isOverdue).length;

  return (
    <Card className="journey-card">
      <header>
        <div>
          <small>Started {new Date(journey.startDate).toLocaleDateString()}</small>
          <h2>
            {journey.templateName}
            {journey.projectTemplateName ? ` + ${journey.projectTemplateName}` : ''}
          </h2>
          <p>
            Reviewer: {journey.managerName ?? 'nobody set'}
            {journey.mentorName ? ` · Mentor: ${journey.mentorName}` : ''}
            {journey.projectName ? ` · Project: ${journey.projectName}` : ''}
          </p>
        </div>
        <Badge tone={status.tone}>{status.label}</Badge>
      </header>

      <div
        className="mini-progress"
        role="progressbar"
        aria-label="Onboarding progress"
        aria-valuenow={journey.progressPercent}
        aria-valuemin={0}
        aria-valuemax={100}
      >
        <span style={{ width: `${journey.progressPercent}%` }} />
      </div>
      <dl className="journey-card__facts">
        <div>
          <dt>Done</dt>
          <dd>{journey.progressPercent}%</dd>
        </div>
        <div>
          <dt>Documents read</dt>
          <dd>
            {documents.filter((item) => item.done).length} of {documents.length}
            {assessed && assessment.readingCycleNumber > 1 ? ` (round ${assessment.readingCycleNumber})` : ''}
          </dd>
        </div>
        <div>
          <dt>Quiz</dt>
          <dd>
            {assessment ? quizStatus[assessment.status].label : 'No quiz'}
            {lastAttempt?.scorePercentage != null
              ? ` · last score ${lastAttempt.scorePercentage}% (${assessment!.attempts.length} ${assessment!.attempts.length === 1 ? 'attempt' : 'attempts'})`
              : ''}
          </dd>
        </div>
        {overdue ? (
          <div>
            <dt>Overdue</dt>
            <dd>
              <Clock3 size={14} aria-hidden="true" /> {overdue} {overdue === 1 ? 'step' : 'steps'}
            </dd>
          </div>
        ) : null}
      </dl>

      {documents.length ? (
        <section>
          <h3>Documents to read</h3>
          <ul className="journey-card__list">
            {documents.map((item) => (
              <li key={item.id} className={item.done ? 'is-done' : ''}>
                {item.done ? (
                  <CheckCircle2 size={16} aria-label="Read" />
                ) : (
                  <FileText size={16} aria-label="Not read yet" />
                )}
                {item.title}
              </li>
            ))}
          </ul>
        </section>
      ) : null}
      {otherSteps.length ? (
        <section>
          <h3>Other steps</h3>
          <ul className="journey-card__list">
            {otherSteps.map((step) => (
              <li key={step.id} className={step.status === 'Completed' ? 'is-done' : ''}>
                {step.status === 'Completed' ? (
                  <CheckCircle2 size={16} aria-label="Done" />
                ) : (
                  <Circle size={16} aria-label={statusOf(step.status).label} />
                )}
                {step.title}
                {step.status === 'PendingReview' ? <Badge tone="warning">Waiting for review</Badge> : null}
              </li>
            ))}
          </ul>
        </section>
      ) : null}
      {journey.status === 'PendingReview' ? (
        <p className="field__hint">
          A milestone is waiting for approval in <Link to="/onboarding/progress">Onboarding → Progress</Link>.
        </p>
      ) : null}
    </Card>
  );
}
