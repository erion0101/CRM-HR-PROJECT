import { useQuery } from '@tanstack/react-query';
import { Brain, FileText, ShieldCheck } from 'lucide-react';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import { getOnboardingQuiz } from '../api/onboarding-api';
import type { OnboardingTemplate } from '../types';
import { packageKindLabel } from '../utils/package-labels';

const readingTypes = ['ReadDocument', 'ReadArticle', 'PolicyAcknowledgment'];

/** A package with its quiz status, so HR always sees the one thing left to do on it. */
export function PackageCard({
  pkg,
  projectName,
  canEdit,
  publishing,
  onPublish,
  onOpenQuiz,
  updatingDefault,
  onToggleDefault
}: {
  pkg: OnboardingTemplate;
  projectName?: string;
  canEdit: boolean;
  publishing: boolean;
  onPublish: () => void;
  onOpenQuiz: () => void;
  updatingDefault: boolean;
  onToggleDefault: () => void;
}) {
  const published = pkg.publishedVersion != null && pkg.status !== 'Retired';
  const quiz = useQuery({
    queryKey: ['onboarding', 'quiz', pkg.id],
    queryFn: () => getOnboardingQuiz(pkg.id),
    enabled: published
  });
  const documents = pkg.steps.filter((step) => readingTypes.includes(step.stepType)).length;
  const quizLine = !published
    ? 'Publish the package to build its quiz'
    : quiz.isLoading
      ? 'Checking the quiz…'
      : !quiz.data
        ? 'No quiz yet'
        : quiz.data.status === 'Published'
          ? `Quiz published · ${quiz.data.questions.length} questions`
          : `Quiz draft · ${quiz.data.questions.length} questions, not published yet`;
  const quizReady = quiz.data?.status === 'Published';

  return (
    <Card className="template-card package-card">
      <header>
        <span>
          <ShieldCheck size={18} />
        </span>
        <div className="row-actions">
          {pkg.autoAssignToNewEmployees ? <Badge tone="info">Default for new employees</Badge> : null}
          <Badge tone={published ? 'positive' : 'warning'}>{published ? 'Published' : 'Draft'}</Badge>
        </div>
      </header>
      <h3>{pkg.name}</h3>
      <small>
        {packageKindLabel[pkg.kind]}
        {projectName ? ` · ${projectName}` : ''}
      </small>
      <p>{pkg.description}</p>
      <ul className="package-card__facts">
        <li>
          <FileText size={14} /> {documents} {documents === 1 ? 'document' : 'documents'} to read ·{' '}
          {pkg.steps.length - documents} other {pkg.steps.length - documents === 1 ? 'step' : 'steps'}
        </li>
        <li className={quizReady ? 'is-done' : 'is-todo'}>
          <Brain size={14} /> {quizLine}
        </li>
      </ul>
      {canEdit ? (
        <footer>
          {published && pkg.kind === 'Company' ? (
            <Button size="small" variant="ghost" loading={updatingDefault} onClick={onToggleDefault}>
              {pkg.autoAssignToNewEmployees ? 'Stop giving to new employees' : 'Give to every new employee'}
            </Button>
          ) : (
            <span />
          )}
          {published ? (
            <Button size="small" variant={quizReady ? 'secondary' : 'primary'} onClick={onOpenQuiz}>
              <Brain size={15} /> {quizReady ? 'View quiz' : quiz.data ? 'Finish the quiz' : 'Build the quiz'}
            </Button>
          ) : (
            <Button size="small" loading={publishing} onClick={onPublish}>
              Publish package
            </Button>
          )}
        </footer>
      ) : null}
    </Card>
  );
}
