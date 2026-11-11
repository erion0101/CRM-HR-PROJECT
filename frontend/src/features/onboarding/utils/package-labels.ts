import type { OnboardingTemplateKind } from '../types';

export const packageKindLabel: Record<OnboardingTemplateKind, string> = {
  Company: 'Company package',
  Project: 'Project package',
  General: 'Standalone package'
};

export const packageKindDescription: Record<OnboardingTemplateKind, string> = {
  Company: 'How the company works. Every new employee should get this one.',
  Project: 'Documents for one project. Added automatically when an employee is onboarded onto that project.',
  General: 'A package on its own, not combined with a project package.'
};
