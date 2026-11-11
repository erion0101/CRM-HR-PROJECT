import { useQuery } from '@tanstack/react-query';
import { Plus } from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router';

import { canManagePeople } from '@/app/access';
import { Skeleton } from '@/components/feedback/skeleton';
import { PageHeader } from '@/components/navigation/page-header';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useAuth } from '@/features/auth/hooks/use-auth';
import { getProjects } from '@/features/projects/api/projects-api';

import {
  createOnboardingTemplate,
  getOnboardingTemplates,
  publishOnboardingTemplate,
  setOnboardingAutoAssign
} from '../api/onboarding-api';
import { OnboardingWorkspaceTabs } from '../components/onboarding-workspace-tabs';
import { PackageCard } from '../components/package-card';
import { QuizBuilderDialog } from '../components/quiz-builder/quiz-builder-dialog';
import { TemplateDialog } from '../components/template-dialog';
import { useOnboardingMutation } from '../hooks/use-onboarding-mutation';

/** Setup: the packages new hires get, and each package's quiz. */
export function OnboardingPackagesPage() {
  const { user } = useAuth();
  const canEdit = canManagePeople(user);
  const [creating, setCreating] = useState(false);
  const [quizFor, setQuizFor] = useState<{ id: string; name: string } | null>(null);
  const packages = useQuery({
    queryKey: ['onboarding', 'templates', 'manage'],
    queryFn: () => getOnboardingTemplates(false)
  });
  const projects = useQuery({ queryKey: ['projects'], queryFn: getProjects });
  const projectNames = new Map((projects.data ?? []).map((project) => [project.id, project.name]));

  const create = useOnboardingMutation({
    mutationFn: createOnboardingTemplate,
    success: { title: 'Package created', message: 'Publish it when it is ready, then build its quiz.' },
    errorTitle: 'Could not create the package',
    onSuccess: () => setCreating(false)
  });
  const publish = useOnboardingMutation({
    mutationFn: publishOnboardingTemplate,
    success: { title: 'Package published', message: 'Next: build its quiz.' },
    errorTitle: 'Could not publish the package'
  });
  const autoAssign = useOnboardingMutation({
    mutationFn: setOnboardingAutoAssign,
    success: { title: 'Updated who gets this package' },
    errorTitle: 'Could not change the package'
  });

  const sorted = [...(packages.data ?? [])].sort(
    (a, b) => (a.kind === 'Project' ? 1 : 0) - (b.kind === 'Project' ? 1 : 0) || a.name.localeCompare(b.name)
  );

  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Onboarding"
        title="Packages & quizzes"
        description="A package is the set of documents and steps a new hire goes through. Each package has one quiz at the end."
        actions={
          canEdit ? (
            <Button onClick={() => setCreating(true)}>
              <Plus size={16} /> New package
            </Button>
          ) : undefined
        }
      >
        <OnboardingWorkspaceTabs />
      </PageHeader>

      <ol className="how-it-works" aria-label="How setup works">
        <li>
          <strong>1. Documents</strong>
          <span>
            Add company and project documents in <Link to="/company">Documents</Link>.
          </span>
        </li>
        <li>
          <strong>2. Package</strong>
          <span>Create a package from those documents and publish it.</span>
        </li>
        <li>
          <strong>3. Quiz</strong>
          <span>Build the quiz (AI can draft it), check the questions, and publish.</span>
        </li>
      </ol>

      {packages.isLoading ? (
        <Card>
          <Skeleton count={4} />
        </Card>
      ) : sorted.length ? (
        <div className="template-grid">
          {sorted.map((pkg) => (
            <PackageCard
              key={pkg.id}
              pkg={pkg}
              projectName={pkg.projectId ? projectNames.get(pkg.projectId) : undefined}
              canEdit={canEdit}
              publishing={publish.isPending}
              onPublish={() => publish.mutate(pkg)}
              onOpenQuiz={() => setQuizFor({ id: pkg.id, name: pkg.name })}
              updatingDefault={autoAssign.isPending}
              onToggleDefault={() => autoAssign.mutate({ template: pkg, enabled: !pkg.autoAssignToNewEmployees })}
            />
          ))}
        </div>
      ) : (
        <Card>
          <div className="state-panel">
            <h3>No packages yet</h3>
            <p>
              Add your company documents first, then create a company package from them. Projects can get their own
              package later.
            </p>
            <div className="row-actions">
              <Link className="button button--secondary button--small" to="/company">
                Go to documents
              </Link>
              {canEdit ? (
                <Button size="small" onClick={() => setCreating(true)}>
                  New package
                </Button>
              ) : null}
            </div>
          </div>
        </Card>
      )}

      <TemplateDialog
        open={creating}
        loading={create.isPending}
        onClose={() => setCreating(false)}
        onSave={(value) => create.mutateAsync(value)}
      />
      <QuizBuilderDialog template={quizFor} onClose={() => setQuizFor(null)} />
    </div>
  );
}
