import { CheckCircle2, ClipboardCheck, LockKeyhole, RotateCcw } from 'lucide-react';
import { Link } from 'react-router';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import type {
  OnboardingAssessment,
  OnboardingAssignment,
  OnboardingReadingItem,
  OnboardingReadingStatus
} from '../types';
import { groupBy } from '../utils/group-by';
import { quizStatus } from '../utils/onboarding-labels';
import { ReadingModule } from './reading-module';

interface AssessmentPanelProps {
  assignment: OnboardingAssignment;
  busy: boolean;
  onReading: (item: OnboardingReadingItem, status: OnboardingReadingStatus) => void;
  onStartQuiz: () => void;
}

export function AssessmentPanel({ assignment, busy, onReading, onStartQuiz }: AssessmentPanelProps) {
  const assessment = assignment.assessment;
  if (!assessment || assessment.status === 'NotConfigured') return null;
  const sections = groupBy(assessment.readingItems, (item) => item.sectionTitle);

  return (
    <section className="assessment-panel" id="required-reading">
      <div className="section-heading">
        <div>
          <h2>Read, then take the quiz</h2>
          <p>
            Open and read every required document, then mark it as read. The quiz unlocks when all of them are read.
            {assessment.readingCycleNumber > 1 ? ` This is round ${assessment.readingCycleNumber}.` : ''}
          </p>
        </div>
        <Badge
          tone={assessment.status === 'Passed' ? 'positive' : assessment.status === 'Available' ? 'info' : 'warning'}
        >
          {quizStatus[assessment.status].label}
        </Badge>
      </div>

      <Card className="reading-progress-card">
        <div>
          <strong>
            {assessment.readDocuments}/{assessment.requiredDocuments} required documents read
          </strong>
          <span>{assessment.readingProgressPercent}%</span>
        </div>
        <div className="mini-progress">
          <span style={{ width: `${assessment.readingProgressPercent}%` }} />
        </div>
      </Card>

      <div className="reading-modules">
        {sections.map(([section, items]) => (
          <ReadingModule
            key={section}
            assignmentId={assignment.id}
            section={section}
            items={items}
            busy={busy}
            onReading={onReading}
          />
        ))}
      </div>

      <AssessmentGate assessment={assessment} busy={busy} onStartQuiz={onStartQuiz} />
      <AttemptHistory assessment={assessment} />
    </section>
  );
}

function AssessmentGate({
  assessment,
  busy,
  onStartQuiz
}: {
  assessment: OnboardingAssessment;
  busy: boolean;
  onStartQuiz: () => void;
}) {
  const activeAttempt = assessment.attempts.find((attempt) => attempt.status === 'InProgress');
  const icon =
    assessment.status === 'Locked' ? (
      <LockKeyhole size={24} />
    ) : assessment.status === 'Passed' ? (
      <CheckCircle2 size={24} />
    ) : (
      <ClipboardCheck size={24} />
    );
  const description =
    assessment.status === 'Locked'
      ? 'Complete every required reading item to unlock the quiz.'
      : assessment.status === 'Passed'
        ? 'You passed the quiz.'
        : `Ready when you are. Passing score: ${assessment.passingPercentage}%.`;

  return (
    <Card className={`assessment-gate assessment-gate--${assessment.status.toLowerCase()}`}>
      <span>{icon}</span>
      <div>
        <small>Onboarding quiz</small>
        <h3>{assessment.quizTitle ?? 'Onboarding quiz'}</h3>
        <p>{description}</p>
      </div>
      {activeAttempt ? (
        <Link className="button button--primary button--medium" to={`/onboarding/quiz/${activeAttempt.id}`}>
          Resume quiz
        </Link>
      ) : assessment.status === 'Available' ? (
        <Button loading={busy} onClick={onStartQuiz}>
          Start assessment
        </Button>
      ) : null}
    </Card>
  );
}

function AttemptHistory({ assessment }: { assessment: OnboardingAssessment }) {
  if (!assessment.attempts.length) return null;
  return (
    <Card className="attempt-history">
      <h3>Quiz attempts</h3>
      {assessment.attempts.map((attempt) => (
        <div key={attempt.id}>
          <span>
            Attempt {attempt.attemptNumber} · Cycle {attempt.readingCycleNumber}
          </span>
          <Badge tone={attempt.status === 'Passed' ? 'positive' : attempt.status === 'Failed' ? 'negative' : 'info'}>
            {attempt.status}
          </Badge>
          <strong>{attempt.scorePercentage == null ? 'In progress' : `${attempt.scorePercentage}%`}</strong>
          {attempt.status === 'Failed' ? (
            <small>
              <RotateCcw size={13} /> Full reading reset completed
            </small>
          ) : null}
        </div>
      ))}
    </Card>
  );
}
