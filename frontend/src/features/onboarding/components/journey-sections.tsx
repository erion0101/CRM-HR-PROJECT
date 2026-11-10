import { ArrowRight, BookOpen, Check, CheckCircle2, Circle, Clock3 } from 'lucide-react';
import { Link } from 'react-router';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import type { OnboardingAssignment, OnboardingStep, OnboardingStepStatus, OnboardingStepType } from '../types';
import { groupBy } from '../utils/group-by';

type StepHandler = (step: OnboardingStep, status: OnboardingStepStatus) => void;

const stepTone: Record<OnboardingStepStatus, 'neutral' | 'positive' | 'warning' | 'negative' | 'info'> = {
  NotStarted: 'neutral',
  InProgress: 'info',
  Blocked: 'negative',
  PendingReview: 'warning',
  Completed: 'positive'
};

/** Reading steps are shown by the assessment panel when an assessment is configured. */
const readingStepTypes: OnboardingStepType[] = ['ReadDocument', 'ReadArticle', 'PolicyAcknowledgment'];

export function JourneySections({
  assignment,
  busy,
  onUpdate
}: {
  assignment: OnboardingAssignment;
  busy: boolean;
  onUpdate: StepHandler;
}) {
  const hasAssessment = assignment.assessment?.status !== 'NotConfigured';
  const visibleSteps = hasAssessment
    ? assignment.steps.filter((step) => !readingStepTypes.includes(step.stepType))
    : assignment.steps;
  const sections = groupBy(visibleSteps, (step) => step.sectionTitle);

  return (
    <div className="journey-sections">
      {sections.map(([section, steps]) => (
        <Card className="journey-section" key={section}>
          <header>
            <div>
              <small>Onboarding path</small>
              <h2>{section}</h2>
            </div>
            <span>
              {steps.filter((step) => step.status === 'Completed').length}/{steps.length} complete
            </span>
          </header>
          {steps.map((step) => (
            <JourneyStep key={step.id} step={step} busy={busy} onUpdate={onUpdate} />
          ))}
        </Card>
      ))}
    </div>
  );
}

function JourneyStep({ step, busy, onUpdate }: { step: OnboardingStep; busy: boolean; onUpdate: StepHandler }) {
  const completed = step.status === 'Completed';
  const canComplete = !completed && step.status !== 'PendingReview';

  return (
    <div className={`journey-step ${completed ? 'is-complete' : ''}`}>
      <span className="journey-step__state">{completed ? <CheckCircle2 size={20} /> : <Circle size={20} />}</span>
      <div>
        <div className="journey-step__title">
          <strong>{step.title}</strong>
          <Badge tone={stepTone[step.status]}>{step.status.replace(/([A-Z])/g, ' $1').trim()}</Badge>
          <small>{step.isRequired ? 'Required' : 'Optional'}</small>
        </div>
        <p>{step.instructions}</p>
        <small>Expected outcome: {step.expectedOutcome}</small>
        <div className="journey-step__resources">
          {step.knowledgeArticleId ? (
            <Link to={`/knowledge/articles/${step.knowledgeArticleId}`}>
              <BookOpen size={14} /> Open article
            </Link>
          ) : null}
          {step.externalUrl ? (
            <a href={step.externalUrl} target="_blank" rel="noreferrer">
              <ArrowRight size={14} /> Open resource
            </a>
          ) : null}
          <span className={step.isOverdue ? 'overdue' : ''}>
            <Clock3 size={14} /> Due {new Date(step.dueDate).toLocaleDateString()}
          </span>
        </div>
      </div>
      <div className="journey-step__actions">
        {step.status === 'NotStarted' ? (
          <Button size="small" variant="secondary" disabled={busy} onClick={() => onUpdate(step, 'InProgress')}>
            Start
          </Button>
        ) : null}
        {canComplete ? (
          <Button size="small" disabled={busy} onClick={() => onUpdate(step, 'Completed')}>
            <Check size={14} /> {step.stepType === 'ManagerReviewedMilestone' ? 'Submit for review' : 'Complete'}
          </Button>
        ) : null}
      </div>
    </div>
  );
}
