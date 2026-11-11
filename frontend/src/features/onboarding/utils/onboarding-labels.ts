import type { OnboardingAssessmentStatus } from '../types';

type Tone = 'neutral' | 'positive' | 'warning' | 'negative' | 'info';

/** Plain-English names for an employee's onboarding status, with a badge tone. Text always carries the meaning. */
export const onboardingStatus: Record<string, { label: string; tone: Tone }> = {
  NotStarted: { label: 'Not started', tone: 'neutral' },
  InProgress: { label: 'In progress', tone: 'info' },
  Blocked: { label: 'Blocked', tone: 'negative' },
  PendingReview: { label: 'Waiting for review', tone: 'warning' },
  Completed: { label: 'Completed', tone: 'positive' },
  Cancelled: { label: 'Cancelled', tone: 'neutral' },
  Superseded: { label: 'Replaced', tone: 'neutral' }
};

export const quizStatus: Record<OnboardingAssessmentStatus, { label: string; tone: Tone }> = {
  NotConfigured: { label: 'No quiz', tone: 'neutral' },
  Locked: { label: 'Reading documents', tone: 'neutral' },
  Available: { label: 'Quiz ready to take', tone: 'info' },
  InProgress: { label: 'Taking the quiz', tone: 'info' },
  Passed: { label: 'Quiz passed', tone: 'positive' }
};

export const statusOf = (status: string) => onboardingStatus[status] ?? { label: status, tone: 'neutral' as Tone };
