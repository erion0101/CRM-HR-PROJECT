import type { OnboardingQuizAdmin, OnboardingTemplate } from '../types';

export interface SetupItem {
  id: string;
  label: string;
  detail: string;
  done: boolean;
  actionLabel: string;
  to: string;
}

export interface SetupInput {
  companyDocuments: { ready: number; scanning: number };
  projects: { id: string; name: string; ready: number; scanning: number }[];
  packages: OnboardingTemplate[];
  /** Quiz per package id; null when the package has no quiz yet. */
  quizzes: Record<string, OnboardingQuizAdmin | null | undefined>;
}

const plural = (count: number, word: string) => `${count} ${word}${count === 1 ? '' : 's'}`;

function documentsDetail(ready: number, scanning: number) {
  if (ready === 0 && scanning === 0) return 'No documents yet';
  return [ready ? `${plural(ready, 'document')} ready` : '', scanning ? `${scanning} being scanned` : '']
    .filter(Boolean)
    .join(', ');
}

function packageItems(id: string, label: string, pkg: OnboardingTemplate | undefined, input: SetupInput): SetupItem[] {
  const published = pkg?.publishedVersion != null && pkg.status !== 'Retired';
  const quiz = pkg ? input.quizzes[pkg.id] : undefined;
  const quizDone = quiz?.status === 'Published';
  const quizDetail = !published
    ? 'Publish the package first'
    : quizDone
      ? `Published, ${plural(quiz.questions.length, 'question')}`
      : quiz
        ? `Draft, ${plural(quiz.questions.length, 'question')}`
        : 'No quiz yet';
  return [
    {
      id: `${id}-package`,
      label: `${label} package`,
      detail: !pkg
        ? 'Not created yet'
        : published
          ? `Published, ${plural(pkg.steps.length, 'step')}`
          : 'Draft, not published',
      done: published,
      actionLabel: pkg ? 'Open packages' : 'Create the package',
      to: '/onboarding/packages'
    },
    {
      id: `${id}-quiz`,
      label: `${label} quiz`,
      detail: quizDetail,
      done: quizDone,
      actionLabel: quiz ? 'Review and publish' : 'Build the quiz',
      to: '/onboarding/packages'
    }
  ];
}

/**
 * The onboarding setup as an ordered checklist: documents, then a package built from them, then its quiz,
 * first for the company and then for each project. The first unfinished item is the next thing HR should do.
 */
export function buildSetupChecklist(input: SetupInput): { items: SetupItem[]; next?: SetupItem } {
  const company =
    input.packages.find((pkg) => pkg.kind === 'Company') ?? input.packages.find((pkg) => pkg.kind === 'General');
  const items: SetupItem[] = [
    {
      id: 'company-documents',
      label: 'Company documents',
      detail: documentsDetail(input.companyDocuments.ready, input.companyDocuments.scanning),
      done: input.companyDocuments.ready > 0,
      actionLabel: 'Add documents',
      to: '/company'
    },
    ...packageItems('company', 'Company', company, input)
  ];
  for (const project of input.projects) {
    items.push({
      id: `project-${project.id}-documents`,
      label: `${project.name} documents`,
      detail: documentsDetail(project.ready, project.scanning),
      done: project.ready > 0,
      actionLabel: 'Add documents',
      to: '/company'
    });
    const pkg = input.packages.find((item) => item.kind === 'Project' && item.projectId === project.id);
    items.push(...packageItems(`project-${project.id}`, project.name, pkg, input));
  }
  return { items, next: items.find((item) => !item.done) };
}
