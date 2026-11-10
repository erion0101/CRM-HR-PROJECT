import { useQuery } from '@tanstack/react-query';
import { Brain, Plus, ShieldCheck, UserRoundCheck } from 'lucide-react';
import { useState } from 'react';

import { canManageOnboarding, canManagePeople } from '@/app/access';
import { EmptyState } from '@/components/feedback/empty-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { PageHeader } from '@/components/navigation/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useAuth } from '@/features/auth/hooks/use-auth';
import { getProjects } from '@/features/projects/api/projects-api';

import {
  assignOnboarding,
  createOnboardingTemplate,
  getOnboardingAssignments,
  getOnboardingTemplates,
  publishOnboardingTemplate,
  reviewOnboardingStep,
  setOnboardingAutoAssign
} from '../api/onboarding-api';
import { AssignmentDialog } from '../components/assignment-dialog';
import { OnboardingWorkspaceTabs } from '../components/onboarding-workspace-tabs';
import { QuizBuilderDialog } from '../components/quiz-builder/quiz-builder-dialog';
import { TemplateDialog } from '../components/template-dialog';
import { useOnboardingMutation } from '../hooks/use-onboarding-mutation';
import type { OnboardingAssignment, OnboardingStep, OnboardingTemplate } from '../types';
import { packageKindLabel } from '../utils/package-labels';

export function OnboardingManagementPage() {
  const { user } = useAuth();
  const canCreateTemplates = canManagePeople(user);
  const [templateOpen, setTemplateOpen] = useState(false);
  const [assignmentOpen, setAssignmentOpen] = useState(false);
  const [quizTemplate, setQuizTemplate] = useState<{ id: string; name: string } | null>(null);

  const templates = useQuery({
    queryKey: ['onboarding', 'templates', 'manage'],
    queryFn: () => getOnboardingTemplates(false)
  });
  const assignments = useQuery({
    queryKey: ['onboarding', 'assignments'],
    queryFn: getOnboardingAssignments,
    enabled: canManageOnboarding(user)
  });

  const create = useOnboardingMutation({
    mutationFn: createOnboardingTemplate,
    success: { title: 'Onboarding template created' },
    errorTitle: 'Could not create template',
    onSuccess: () => setTemplateOpen(false)
  });
  const autoAssign = useOnboardingMutation({
    mutationFn: setOnboardingAutoAssign,
    success: { title: 'Default package updated' },
    errorTitle: 'Could not change the default package'
  });
  const publish = useOnboardingMutation({
    mutationFn: publishOnboardingTemplate,
    success: { title: 'Template published' },
    errorTitle: 'Could not publish template'
  });
  const assign = useOnboardingMutation({
    mutationFn: assignOnboarding,
    success: { title: 'Onboarding journey assigned' },
    errorTitle: 'Could not assign onboarding',
    onSuccess: () => setAssignmentOpen(false)
  });
  const review = useOnboardingMutation({
    mutationFn: ({
      assignment,
      step,
      approved
    }: {
      assignment: OnboardingAssignment;
      step: OnboardingStep;
      approved: boolean;
    }) => reviewOnboardingStep(assignment.id, step, approved),
    success: { title: 'Milestone reviewed' },
    errorTitle: 'Could not review milestone'
  });

  // A template with a newer draft still assigns its last published version.
  const publishedTemplates =
    templates.data?.filter((template) => template.publishedVersion != null && template.status !== 'Retired') ?? [];
  const projects = useQuery({ queryKey: ['projects'], queryFn: getProjects });
  const projectNames = new Map((projects.data ?? []).map((project) => [project.id, project.name]));

  return (
    <div className="page-stack onboarding-management-page">
      <PageHeader
        eyebrow="Onboarding"
        title="Journeys and progress"
        description="Publish reusable journeys, assign employees, and review milestones."
        actions={
          <div className="row-actions">
            {canCreateTemplates ? (
              <Button variant="secondary" onClick={() => setTemplateOpen(true)}>
                <Plus size={16} /> New template
              </Button>
            ) : null}
            <Button onClick={() => setAssignmentOpen(true)}>
              <UserRoundCheck size={16} /> Assign journey
            </Button>
          </div>
        }
      >
        <OnboardingWorkspaceTabs />
      </PageHeader>

      <section>
        <div className="section-heading">
          <div>
            <h2>Templates</h2>
            <p>Assignments always snapshot a published version.</p>
          </div>
        </div>
        {templates.isLoading ? (
          <Card>
            <Skeleton count={4} />
          </Card>
        ) : templates.data?.length ? (
          <div className="template-grid">
            {templates.data.map((template) => (
              <TemplateCard
                key={template.id}
                template={template}
                projectName={template.projectId ? projectNames.get(template.projectId) : undefined}
                canEdit={canCreateTemplates}
                publishing={publish.isPending}
                onPublish={() => publish.mutate(template)}
                onOpenAssessment={() => setQuizTemplate({ id: template.id, name: template.name })}
                updatingDefault={autoAssign.isPending}
                onToggleDefault={() => autoAssign.mutate({ template, enabled: !template.autoAssignToNewEmployees })}
              />
            ))}
          </div>
        ) : (
          <Card>
            <EmptyState title="No onboarding templates" description="Create the first reusable onboarding journey." />
          </Card>
        )}
      </section>

      <section>
        <div className="section-heading">
          <div>
            <h2>Employee progress</h2>
            <p>Only employees inside your authorized scope are shown.</p>
          </div>
        </div>
        {assignments.isLoading ? (
          <Card>
            <Skeleton count={5} />
          </Card>
        ) : assignments.data?.length ? (
          <Card className="assignment-list">
            {assignments.data.map((assignment) => (
              <AssignmentRow
                key={assignment.id}
                assignment={assignment}
                reviewing={review.isPending}
                onApprove={(step) => review.mutate({ assignment, step, approved: true })}
              />
            ))}
          </Card>
        ) : (
          <Card>
            <EmptyState title="No onboarding assignments" description="Assign a published template to an employee." />
          </Card>
        )}
      </section>

      <TemplateDialog
        open={templateOpen}
        loading={create.isPending}
        onClose={() => setTemplateOpen(false)}
        onSave={(value) => create.mutateAsync(value)}
      />
      <AssignmentDialog
        open={assignmentOpen}
        loading={assign.isPending}
        templates={publishedTemplates}
        onClose={() => setAssignmentOpen(false)}
        onSave={(value) => assign.mutateAsync(value)}
      />
      <QuizBuilderDialog template={quizTemplate} onClose={() => setQuizTemplate(null)} />
    </div>
  );
}

function TemplateCard({
  template,
  projectName,
  canEdit,
  publishing,
  onPublish,
  onOpenAssessment,
  updatingDefault,
  onToggleDefault
}: {
  template: OnboardingTemplate;
  projectName?: string;
  canEdit: boolean;
  publishing: boolean;
  onPublish: () => void;
  onOpenAssessment: () => void;
  updatingDefault: boolean;
  onToggleDefault: () => void;
}) {
  const canBeDefault =
    template.kind === 'Company' && template.publishedVersion != null && template.status !== 'Retired';
  return (
    <Card className="template-card">
      <header>
        <span>
          <ShieldCheck size={18} />
        </span>
        <div className="row-actions">
          {template.autoAssignToNewEmployees ? <Badge tone="info">Default for new employees</Badge> : null}
          <Badge tone={template.status === 'Published' ? 'positive' : 'warning'}>{template.status}</Badge>
        </div>
      </header>
      <h3>{template.name}</h3>
      <small>
        {packageKindLabel[template.kind]}
        {projectName ? ` · ${projectName}` : ''}
      </small>
      <p>{template.description}</p>
      <footer>
        <span>
          {template.steps.length} steps · {template.durationDays} days
        </span>
        <div className="row-actions">
          {canEdit && canBeDefault ? (
            <Button size="small" variant="ghost" loading={updatingDefault} onClick={onToggleDefault}>
              {template.autoAssignToNewEmployees ? 'Stop auto-assigning' : 'Make default for new employees'}
            </Button>
          ) : null}
          {canEdit && template.status === 'Published' ? (
            <Button size="small" variant="secondary" onClick={onOpenAssessment}>
              <Brain size={15} /> Assessment
            </Button>
          ) : null}
          {canEdit && template.status === 'Draft' ? (
            <Button size="small" loading={publishing} onClick={onPublish}>
              Publish
            </Button>
          ) : null}
        </div>
      </footer>
    </Card>
  );
}

function AssignmentRow({
  assignment,
  reviewing,
  onApprove
}: {
  assignment: OnboardingAssignment;
  reviewing: boolean;
  onApprove: (step: OnboardingStep) => void;
}) {
  const pendingReview = assignment.steps.filter((step) => step.status === 'PendingReview');
  return (
    <div className="assignment-row">
      <div>
        <strong>{assignment.employeeName}</strong>
        <small>
          {assignment.templateName}
          {assignment.projectTemplateName ? ` + ${assignment.projectTemplateName}` : ''}
          {assignment.projectName ? ` · ${assignment.projectName}` : ''}
        </small>
      </div>
      <div className="mini-progress">
        <span style={{ width: `${assignment.progressPercent}%` }} />
      </div>
      <Badge tone={assignment.status === 'Completed' ? 'positive' : 'info'}>{assignment.status}</Badge>
      <span>{assignment.progressPercent}%</span>
      {pendingReview.map((step) => (
        <Button size="small" key={step.id} loading={reviewing} onClick={() => onApprove(step)}>
          Approve {step.title}
        </Button>
      ))}
    </div>
  );
}
