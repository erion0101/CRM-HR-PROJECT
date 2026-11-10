import { useMutation, useQuery } from '@tanstack/react-query';
import { ArrowLeft, BookOpen, ExternalLink, FolderKanban, Plus, UserPlus, Users } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useParams } from 'react-router';

import { canManageProjects } from '@/app/access';
import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { FieldShell, Input, Select, Textarea } from '@/components/forms/field';
import { PageHeader } from '@/components/navigation/page-header';
import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';
import { useAuth } from '@/features/auth/hooks/use-auth';
import { createKnowledgeArticle, getKnowledgeArticles } from '@/features/knowledge/api/knowledge-api';
import type { KnowledgeArticleInput } from '@/features/knowledge/types';
import { OnboardingWorkspaceTabs } from '@/features/onboarding/components/onboarding-workspace-tabs';
import { getDepartments } from '@/features/organization/api/organization-api';
import { toAppError } from '@/lib/api-error';

import {
  addProjectMember,
  createProject,
  getProject,
  getProjectEmployeeOptions,
  getProjects
} from '../api/projects-api';
import type { ProjectInput } from '../types';

export function ProjectsPage() {
  const { user } = useAuth();
  const { notify } = useNotification();
  const [creating, setCreating] = useState(false);
  const query = useQuery({ queryKey: ['projects'], queryFn: getProjects });
  const create = useMutation({
    mutationFn: createProject,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['projects'] });
      setCreating(false);
      notify({ title: 'Project created', tone: 'success' });
    },
    onError: (error) => notify({ title: 'Could not create project', message: toAppError(error).message, tone: 'error' })
  });
  const canCreate = canManageProjects(user);
  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Onboarding"
        title="Projects"
        description="Business context, team responsibilities, technical guidance, and getting-started information."
        actions={
          canCreate ? (
            <Button onClick={() => setCreating(true)}>
              <Plus size={17} /> New project
            </Button>
          ) : undefined
        }
      >
        <OnboardingWorkspaceTabs />
      </PageHeader>
      {query.isLoading ? (
        <Card>
          <Skeleton count={5} />
        </Card>
      ) : query.isError ? (
        <Card>
          <ErrorState
            title="Projects could not be loaded"
            description="Try again."
            onRetry={() => void query.refetch()}
          />
        </Card>
      ) : query.data?.length ? (
        <div className="project-grid">
          {query.data.map((project) => (
            <Link to={`/projects/${project.id}`} className="project-card" key={project.id}>
              <span>
                <FolderKanban size={21} />
              </span>
              <div>
                <small>
                  {project.code} · {project.departmentName ?? 'Cross-functional'}
                </small>
                <h2>{project.name}</h2>
                <p>{project.purpose}</p>
                <footer>
                  <Badge tone={project.status === 'Active' ? 'positive' : 'neutral'}>{project.status}</Badge>
                  <span>
                    <Users size={14} /> {project.members.length} members
                  </span>
                </footer>
              </div>
            </Link>
          ))}
        </div>
      ) : (
        <Card>
          <EmptyState
            title="No projects available"
            description="Only projects you are authorized to access are shown."
            actionLabel={canCreate ? 'Create first project' : undefined}
            onAction={canCreate ? () => setCreating(true) : undefined}
          />
        </Card>
      )}
      <ProjectEditor
        open={creating}
        loading={create.isPending}
        onClose={() => setCreating(false)}
        onSave={(value) => create.mutateAsync(value)}
      />
    </div>
  );
}

export function ProjectDetailPage() {
  const { projectId = '' } = useParams();
  const navigate = useNavigate();
  const { notify } = useNotification();
  const [memberOpen, setMemberOpen] = useState(false);
  const [articleOpen, setArticleOpen] = useState(false);
  const query = useQuery({
    queryKey: ['projects', projectId],
    queryFn: () => getProject(projectId),
    enabled: Boolean(projectId)
  });
  const knowledge = useQuery({
    queryKey: ['knowledge', 'project', projectId],
    queryFn: () => getKnowledgeArticles({ projectId, includeDrafts: query.data?.canManage }),
    enabled: Boolean(projectId) && query.isSuccess
  });
  const addMember = useMutation({
    mutationFn: (input: { employeeId: string; projectRole: string; responsibilities: string; effectiveFrom: string }) =>
      addProjectMember(projectId, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['projects'] });
      setMemberOpen(false);
      notify({ title: 'Project member added', tone: 'success' });
    },
    onError: (error) => notify({ title: 'Could not add member', message: toAppError(error).message, tone: 'error' })
  });
  const addArticle = useMutation({
    mutationFn: (input: KnowledgeArticleInput) => createKnowledgeArticle(input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['knowledge'] });
      setArticleOpen(false);
      notify({ title: 'Project knowledge draft created', tone: 'success' });
    },
    onError: (error) =>
      notify({ title: 'Could not create project knowledge', message: toAppError(error).message, tone: 'error' })
  });
  if (query.isLoading)
    return (
      <Card>
        <Skeleton count={7} />
      </Card>
    );
  if (query.isError || !query.data)
    return (
      <Card>
        <ErrorState
          title="Project unavailable"
          description="You may no longer have active project access."
          onRetry={() => void query.refetch()}
        />
      </Card>
    );
  const project = query.data;
  return (
    <div className="page-stack project-detail-page">
      <button className="text-link" onClick={() => navigate(-1)}>
        <ArrowLeft size={16} /> Back to projects
      </button>
      <PageHeader
        eyebrow={`${project.code} · ${project.status}`}
        title={project.name}
        description={project.purpose}
        actions={
          project.canManage ? (
            <div className="row-actions">
              <Button variant="secondary" onClick={() => setArticleOpen(true)}>
                <BookOpen size={16} /> Add knowledge
              </Button>
              <Button onClick={() => setMemberOpen(true)}>
                <UserPlus size={16} /> Add member
              </Button>
            </div>
          ) : undefined
        }
      />
      <div className="project-detail-grid">
        <Card className="project-overview">
          <h2>Project overview</h2>
          <p>{project.description}</p>
          <dl className="detail-list">
            <div>
              <dt>Owning unit</dt>
              <dd>{project.departmentName ?? 'Cross-functional'}</dd>
            </div>
            <div>
              <dt>Project lead</dt>
              <dd>{project.leadEmployeeName}</dd>
            </div>
            <div>
              <dt>Onboarding contact</dt>
              <dd>{project.onboardingContactEmployeeName ?? project.leadEmployeeName}</dd>
            </div>
          </dl>
          {project.links.length ? (
            <div className="resource-links">
              {project.links.map((link) => (
                <a href={link.url} target="_blank" rel="noreferrer" key={link.id}>
                  <ExternalLink size={15} /> {link.label}
                  <small>{link.resourceType}</small>
                </a>
              ))}
            </div>
          ) : null}
        </Card>
        <Card className="project-team">
          <h2>Team and responsibilities</h2>
          {project.members.length ? (
            project.members.map((member) => (
              <div className="project-member" key={member.membershipId}>
                <Avatar name={member.employeeName} />
                <span>
                  <strong>{member.employeeName}</strong>
                  <small>{member.projectRole}</small>
                  <p>{member.responsibilities}</p>
                </span>
              </div>
            ))
          ) : (
            <EmptyState title="No active project members" description="The project lead can add the first member." />
          )}
        </Card>
      </div>
      <section>
        <div className="section-heading">
          <div>
            <h2>Project knowledge</h2>
            <p>Published guidance and reviewed project documentation.</p>
          </div>
        </div>
        {knowledge.isLoading ? (
          <Card>
            <Skeleton count={3} />
          </Card>
        ) : knowledge.data?.length ? (
          <div className="knowledge-grid">
            {knowledge.data.map((article) => (
              <Link className="knowledge-card" to={`/knowledge/articles/${article.id}`} key={article.id}>
                <span>
                  <BookOpen size={19} />
                </span>
                <div>
                  <small>{article.category}</small>
                  <h2>{article.title}</h2>
                  <p>{article.description}</p>
                  <footer>
                    <Badge tone={article.status === 'Published' ? 'positive' : 'warning'}>{article.status}</Badge>
                  </footer>
                </div>
              </Link>
            ))}
          </div>
        ) : (
          <Card>
            <EmptyState title="No project knowledge yet" description="Published project guidance will appear here." />
          </Card>
        )}
      </section>
      <MemberDialog
        open={memberOpen}
        loading={addMember.isPending}
        onClose={() => setMemberOpen(false)}
        onSave={(value) => addMember.mutateAsync(value)}
      />
      <ProjectKnowledgeDialog
        open={articleOpen}
        projectId={project.id}
        loading={addArticle.isPending}
        onClose={() => setArticleOpen(false)}
        onSave={(value) => addArticle.mutateAsync(value)}
      />
    </div>
  );
}

function ProjectEditor({
  open,
  loading,
  onClose,
  onSave
}: {
  open: boolean;
  loading: boolean;
  onClose: () => void;
  onSave: (input: ProjectInput) => Promise<unknown>;
}) {
  const employees = useQuery({
    queryKey: ['projects', 'employee-options'],
    queryFn: getProjectEmployeeOptions,
    enabled: open
  });
  const departments = useQuery({ queryKey: ['departments'], queryFn: () => getDepartments(undefined, 100) });
  const form = useForm<ProjectInput>({
    defaultValues: { code: '', name: '', purpose: '', description: '', leadEmployeeId: '', status: 'Active', links: [] }
  });
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Create project"
      description="Capture only the project context employees need for onboarding."
      width="large"
    >
      <form className="form-stack" onSubmit={form.handleSubmit(onSave)}>
        <div className="form-grid">
          <FieldShell label="Project name" htmlFor="projectName" required>
            <Input id="projectName" {...form.register('name', { required: true })} />
          </FieldShell>
          <FieldShell label="Project code" htmlFor="projectCode" required>
            <Input id="projectCode" {...form.register('code', { required: true })} />
          </FieldShell>
          <FieldShell label="Owning unit" htmlFor="projectDepartment">
            <Select id="projectDepartment" {...form.register('departmentId')}>
              <option value="">Cross-functional</option>
              {departments.data?.items.map((unit) => (
                <option key={unit.id} value={unit.id}>
                  {unit.name}
                </option>
              ))}
            </Select>
          </FieldShell>
          <FieldShell label="Project lead" htmlFor="projectLead" required>
            <Select id="projectLead" {...form.register('leadEmployeeId', { required: true })}>
              <option value="">Choose an employee</option>
              {employees.data?.map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.fullName}
                </option>
              ))}
            </Select>
          </FieldShell>
        </div>
        <FieldShell label="Business purpose" htmlFor="projectPurpose" required>
          <Textarea id="projectPurpose" {...form.register('purpose', { required: true })} />
        </FieldShell>
        <FieldShell label="Description and scope" htmlFor="projectDescription" required>
          <Textarea id="projectDescription" rows={6} {...form.register('description', { required: true })} />
        </FieldShell>
        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={loading}>
            Create project
          </Button>
        </div>
      </form>
    </Dialog>
  );
}

function MemberDialog({
  open,
  loading,
  onClose,
  onSave
}: {
  open: boolean;
  loading: boolean;
  onClose: () => void;
  onSave: (value: {
    employeeId: string;
    projectRole: string;
    responsibilities: string;
    effectiveFrom: string;
  }) => Promise<unknown>;
}) {
  const employees = useQuery({
    queryKey: ['projects', 'employee-options'],
    queryFn: getProjectEmployeeOptions,
    enabled: open
  });
  const form = useForm<{ employeeId: string; projectRole: string; responsibilities: string; effectiveFrom: string }>({
    defaultValues: {
      employeeId: '',
      projectRole: '',
      responsibilities: '',
      effectiveFrom: new Date().toISOString().slice(0, 10)
    }
  });
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Add project member"
      description="Membership controls access to confidential project knowledge."
    >
      <form className="form-stack" onSubmit={form.handleSubmit(onSave)}>
        <FieldShell label="Employee" htmlFor="memberEmployee" required>
          <Select id="memberEmployee" {...form.register('employeeId', { required: true })}>
            <option value="">Choose an employee</option>
            {employees.data?.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.fullName}
              </option>
            ))}
          </Select>
        </FieldShell>
        <FieldShell label="Project role" htmlFor="memberRole" required>
          <Input id="memberRole" {...form.register('projectRole', { required: true })} />
        </FieldShell>
        <FieldShell label="Responsibilities" htmlFor="memberResponsibilities" required>
          <Textarea id="memberResponsibilities" {...form.register('responsibilities', { required: true })} />
        </FieldShell>
        <FieldShell label="Effective from" htmlFor="memberFrom" required>
          <Input id="memberFrom" type="date" {...form.register('effectiveFrom', { required: true })} />
        </FieldShell>
        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={loading}>
            Add member
          </Button>
        </div>
      </form>
    </Dialog>
  );
}

function ProjectKnowledgeDialog({
  open,
  projectId,
  loading,
  onClose,
  onSave
}: {
  open: boolean;
  projectId: string;
  loading: boolean;
  onClose: () => void;
  onSave: (value: KnowledgeArticleInput) => Promise<unknown>;
}) {
  const form = useForm<{ slug: string; title: string; description: string; category: string; content: string }>({
    defaultValues: { slug: '', title: '', description: '', category: 'Project overview', content: '' }
  });
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Add project knowledge"
      description="Create a project-member-only draft for review."
      width="large"
    >
      <form
        className="form-stack"
        onSubmit={form.handleSubmit(async (value) =>
          onSave({ ...value, projectId, audience: 'ProjectMembers', sortOrder: 0 })
        )}
      >
        <div className="form-grid">
          <FieldShell label="Title" htmlFor="projectArticleTitle" required>
            <Input id="projectArticleTitle" {...form.register('title', { required: true })} />
          </FieldShell>
          <FieldShell label="Slug" htmlFor="projectArticleSlug" required>
            <Input id="projectArticleSlug" {...form.register('slug', { required: true, pattern: /^[a-zA-Z0-9-]+$/ })} />
          </FieldShell>
          <FieldShell label="Category" htmlFor="projectArticleCategory" required>
            <Input id="projectArticleCategory" {...form.register('category', { required: true })} />
          </FieldShell>
        </div>
        <FieldShell label="Description" htmlFor="projectArticleDescription" required>
          <Textarea id="projectArticleDescription" {...form.register('description', { required: true })} />
        </FieldShell>
        <FieldShell label="Content" htmlFor="projectArticleContent" required>
          <Textarea id="projectArticleContent" rows={12} {...form.register('content', { required: true })} />
        </FieldShell>
        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={loading}>
            Create draft
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
