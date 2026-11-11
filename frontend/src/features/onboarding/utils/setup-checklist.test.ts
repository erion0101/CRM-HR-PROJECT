import { describe, expect, it } from 'vitest';

import type { OnboardingQuizAdmin, OnboardingTemplate } from '../types';
import { buildSetupChecklist } from './setup-checklist';

const pkg = (overrides: Partial<OnboardingTemplate>): OnboardingTemplate =>
  ({
    id: 'company',
    name: 'Company onboarding',
    description: '',
    kind: 'Company',
    autoAssignToNewEmployees: false,
    durationDays: 30,
    status: 'Published',
    currentVersion: 1,
    publishedVersion: 1,
    steps: [],
    version: 1,
    ...overrides
  }) as OnboardingTemplate;

const quiz = (status: OnboardingQuizAdmin['status'], questions: number) =>
  ({ status, questions: Array.from({ length: questions }, () => ({})) }) as OnboardingQuizAdmin;

describe('buildSetupChecklist', () => {
  it('starts with documents when nothing exists', () => {
    const { items, next } = buildSetupChecklist({
      companyDocuments: { ready: 0, scanning: 0 },
      projects: [],
      packages: [],
      quizzes: {}
    });
    expect(items.map((item) => item.label)).toEqual(['Company documents', 'Company package', 'Company quiz']);
    expect(next?.label).toBe('Company documents');
  });

  it('points at the quiz once documents and the package are ready', () => {
    const { next } = buildSetupChecklist({
      companyDocuments: { ready: 5, scanning: 0 },
      projects: [],
      packages: [pkg({})],
      quizzes: { company: quiz('Draft', 0) }
    });
    expect(next).toMatchObject({
      label: 'Company quiz',
      detail: 'Draft, 0 questions',
      actionLabel: 'Build the quiz'
    });
    const reviewing = buildSetupChecklist({
      companyDocuments: { ready: 5, scanning: 0 },
      projects: [],
      packages: [pkg({})],
      quizzes: { company: quiz('Draft', 12) }
    });
    expect(reviewing.next?.actionLabel).toBe('Review and publish');
  });

  it('adds documents, package and quiz lines for each project', () => {
    const { items, next } = buildSetupChecklist({
      companyDocuments: { ready: 5, scanning: 0 },
      projects: [{ id: 'p1', name: 'Pulse One', ready: 3, scanning: 1 }],
      packages: [
        pkg({}),
        pkg({ id: 'p1pkg', kind: 'Project', projectId: 'p1', status: 'Draft', publishedVersion: undefined })
      ],
      quizzes: { company: quiz('Published', 12) }
    });
    expect(items.map((item) => item.label)).toEqual([
      'Company documents',
      'Company package',
      'Company quiz',
      'Pulse One documents',
      'Pulse One package',
      'Pulse One quiz'
    ]);
    expect(items[3].detail).toBe('3 documents ready, 1 being scanned');
    expect(next).toMatchObject({ label: 'Pulse One package', detail: 'Draft, not published' });
    expect(items[5].detail).toBe('Publish the package first');
  });

  it('has no next step when everything is done', () => {
    const { next } = buildSetupChecklist({
      companyDocuments: { ready: 1, scanning: 0 },
      projects: [],
      packages: [pkg({})],
      quizzes: { company: quiz('Published', 4) }
    });
    expect(next).toBeUndefined();
  });
});
