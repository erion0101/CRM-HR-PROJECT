import { Link } from 'react-router';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';

import type { OnboardingAssignment, OnboardingStep } from '../types';
import { quizStatus, statusOf } from '../utils/onboarding-labels';

/** One person's onboarding in the Progress list. The name opens their profile on the Onboarding tab. */
export function OnboardingProgressRow({
  assignment,
  reviewing,
  onApprove
}: {
  assignment: OnboardingAssignment;
  reviewing: boolean;
  onApprove: (step: OnboardingStep) => void;
}) {
  const status = statusOf(assignment.status);
  const assessment = assignment.assessment;
  const pendingReview = assignment.steps.filter((step) => step.status === 'PendingReview');
  const overdue = assignment.steps.filter((step) => step.isOverdue).length;
  return (
    <div className="progress-row">
      <div className="progress-row__person">
        <strong>
          <Link to={`/employees/${assignment.employeeId}?tab=onboarding`}>{assignment.employeeName}</Link>
        </strong>
        <small>
          {assignment.templateName}
          {assignment.projectTemplateName ? ` + ${assignment.projectTemplateName}` : ''} · started{' '}
          {new Date(assignment.startDate).toLocaleDateString()}
        </small>
      </div>
      <div className="progress-row__progress">
        <div
          className="mini-progress"
          role="progressbar"
          aria-label={`${assignment.employeeName} progress`}
          aria-valuenow={assignment.progressPercent}
          aria-valuemin={0}
          aria-valuemax={100}
        >
          <span style={{ width: `${assignment.progressPercent}%` }} />
        </div>
        <small>
          {assignment.progressPercent}% done
          {assessment && assessment.status !== 'NotConfigured'
            ? ` · ${assessment.readDocuments}/${assessment.requiredDocuments} read · ${quizStatus[assessment.status].label}`
            : ''}
          {overdue ? ` · ${overdue} overdue` : ''}
        </small>
      </div>
      <div className="row-actions progress-row__actions">
        <Badge tone={status.tone}>{status.label}</Badge>
        {pendingReview.map((step) => (
          <Button size="small" key={step.id} loading={reviewing} onClick={() => onApprove(step)}>
            Approve “{step.title}”
          </Button>
        ))}
      </div>
    </div>
  );
}
