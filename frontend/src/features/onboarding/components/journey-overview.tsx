import { Sparkles } from 'lucide-react';
import type { CSSProperties } from 'react';

import { Card } from '@/components/ui/card';

import type { OnboardingAssignment, OnboardingStep } from '../types';

export function JourneyOverview({ journey, next }: { journey: OnboardingAssignment; next?: OnboardingStep }) {
  return (
    <>
      <Card className="onboarding-hero">
        <div>
          <span className="onboarding-hero__icon">
            <Sparkles size={22} />
          </span>
          <small>Your onboarding</small>
          <h2>{journey.templateName}</h2>
          {journey.projectTemplateName ? (
            <small>
              + {journey.projectTemplateName}
              {journey.projectName ? ` (${journey.projectName})` : ''}
            </small>
          ) : null}
          <p>{next ? `Next: ${next.title}` : 'All required steps are complete.'}</p>
        </div>
        <div className="progress-ring" style={{ '--progress': `${journey.progressPercent * 3.6}deg` } as CSSProperties}>
          <strong>{journey.progressPercent}%</strong>
          <span>complete</span>
        </div>
      </Card>

      <div className="onboarding-summary">
        <SummaryCard
          label="Status"
          value={journey.status}
          detail={`Started ${new Date(journey.startDate).toLocaleDateString()}`}
        />
        <SummaryCard label="Manager" value={journey.managerName ?? 'Not assigned'} detail="Journey reviewer" />
        <SummaryCard label="Mentor" value={journey.mentorName ?? 'Not assigned'} detail="Onboarding support" />
        <SummaryCard
          label="Due next"
          value={next ? new Date(next.dueDate).toLocaleDateString() : 'Complete'}
          detail={next?.isOverdue ? 'Overdue' : 'Current schedule'}
        />
      </div>
    </>
  );
}

function SummaryCard({ label, value, detail }: { label: string; value: string; detail: string }) {
  return (
    <Card>
      <small>{label}</small>
      <strong>{value}</strong>
      <span>{detail}</span>
    </Card>
  );
}
