import type { OnboardingTemplateKind } from '../types';

export const packageKindLabel: Record<OnboardingTemplateKind, string> = {
  Company: 'Company package',
  Project: 'Project package',
  General: 'Standalone journey'
};

export const packageKindDescription: Record<OnboardingTemplateKind, string> = {
  Company: 'How the company works and operates. Assign it to every new employee.',
  Project: 'Documents for one project. Added automatically when an employee is onboarded onto that project.',
  General: 'A self-contained journey that is not combined with a project package.'
};
