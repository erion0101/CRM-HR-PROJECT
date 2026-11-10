import { useQuery } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';

import { FieldShell, Input, Select } from '@/components/forms/field';
import { Button } from '@/components/ui/button';
import { Dialog } from '@/components/ui/dialog';
import { useEmployees } from '@/features/employees/hooks/use-employees';
import { getProjects } from '@/features/projects/api/projects-api';

import type { OnboardingTemplate } from '../types';
import { packageKindLabel } from '../utils/package-labels';

export interface AssignmentDraft {
  employeeId: string;
  templateId: string;
  projectId?: string;
  managerEmployeeId?: string;
  mentorEmployeeId?: string;
  startDate: string;
}

type AssignmentFormValues = Required<AssignmentDraft>;

export function AssignmentDialog({
  open,
  loading,
  templates,
  onClose,
  onSave
}: {
  open: boolean;
  loading: boolean;
  /** Published templates; project packages are also used to preview what a project adds. */
  templates: OnboardingTemplate[];
  onClose: () => void;
  onSave: (value: AssignmentDraft) => Promise<unknown>;
}) {
  const employees = useEmployees({ limit: 100 });
  const projects = useQuery({ queryKey: ['projects'], queryFn: getProjects });
  const form = useForm<AssignmentFormValues>({
    defaultValues: {
      employeeId: '',
      templateId: '',
      projectId: '',
      managerEmployeeId: '',
      mentorEmployeeId: '',
      startDate: new Date().toISOString().slice(0, 10)
    }
  });
  const people = (employees.data?.items ?? []).map((employee) => ({
    id: employee.id,
    name: `${employee.firstName} ${employee.lastName}`
  }));
  const projectNames = new Map((projects.data ?? []).map((project) => [project.id, project.name]));
  const selected = templates.find((template) => template.id === form.watch('templateId'));
  const isProjectPackage = selected?.kind === 'Project';
  const projectId = form.watch('projectId');
  const templateOptions = [...templates]
    .sort((a, b) => kindOrder[a.kind] - kindOrder[b.kind] || a.name.localeCompare(b.name))
    .map((template) => ({
      id: template.id,
      name: `${template.name} · ${packageKindLabel[template.kind]}${
        template.projectId ? ` (${projectNames.get(template.projectId) ?? 'project'})` : ''
      }`
    }));

  const submit = form.handleSubmit(async (value) =>
    onSave({
      ...value,
      // A project package always runs on its own project.
      projectId: (isProjectPackage ? selected.projectId : value.projectId) || undefined,
      managerEmployeeId: value.managerEmployeeId || undefined,
      mentorEmployeeId: value.mentorEmployeeId || undefined
    })
  );

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Assign onboarding journey"
      description="Confirm the template, project, manager, mentor, and start date."
    >
      <form className="form-stack" onSubmit={submit}>
        <FieldShell label="Employee" htmlFor="assignmentEmployee" required>
          <Select id="assignmentEmployee" {...form.register('employeeId', { required: true })}>
            <option value="">Choose an employee</option>
            <NamedOptions items={people} />
          </Select>
        </FieldShell>
        <FieldShell label="Published template" htmlFor="assignmentTemplate" required>
          <Select id="assignmentTemplate" {...form.register('templateId', { required: true })}>
            <option value="">Choose a template</option>
            <NamedOptions items={templateOptions} />
          </Select>
        </FieldShell>
        {isProjectPackage ? (
          <p className="field__hint">
            Project: {projectNames.get(selected.projectId ?? '') ?? 'the package project'}. The employee must already be
            an active member.
          </p>
        ) : (
          <FieldShell label="Project" htmlFor="assignmentProject" hint="Employee must already have active membership">
            <Select id="assignmentProject" {...form.register('projectId')}>
              <option value="">No project track</option>
              <NamedOptions items={projects.data ?? []} />
            </Select>
          </FieldShell>
        )}
        {selected && !isProjectPackage && projectId ? (
          <ProjectPackagePreview
            projectName={projectNames.get(projectId) ?? 'This project'}
            projectPackage={templates.find(
              (template) => template.kind === 'Project' && template.projectId === projectId
            )}
          />
        ) : null}
        <div className="form-grid">
          <FieldShell label="Manager" htmlFor="assignmentManager">
            <Select id="assignmentManager" {...form.register('managerEmployeeId')}>
              <option value="">Not assigned</option>
              <NamedOptions items={people} />
            </Select>
          </FieldShell>
          <FieldShell label="Mentor" htmlFor="assignmentMentor">
            <Select id="assignmentMentor" {...form.register('mentorEmployeeId')}>
              <option value="">Not assigned</option>
              <NamedOptions items={people} />
            </Select>
          </FieldShell>
        </div>
        <FieldShell label="Start date" htmlFor="assignmentStart" required>
          <Input id="assignmentStart" type="date" {...form.register('startDate', { required: true })} />
        </FieldShell>
        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={loading}>
            Assign journey
          </Button>
        </div>
      </form>
    </Dialog>
  );
}

const kindOrder: Record<OnboardingTemplate['kind'], number> = { Company: 0, General: 1, Project: 2 };

function ProjectPackagePreview({
  projectName,
  projectPackage
}: {
  projectName: string;
  projectPackage?: OnboardingTemplate;
}) {
  return projectPackage ? (
    <p className="field__hint">
      Includes the project package <strong>{projectPackage.name}</strong>. Its documents follow the company documents,
      and one quiz at the end covers both.
    </p>
  ) : (
    <p className="field__hint">
      {projectName} has no published onboarding package yet, so only the selected package will be assigned.
    </p>
  );
}

function NamedOptions({ items }: { items: { id: string; name: string }[] }) {
  return (
    <>
      {items.map((item) => (
        <option value={item.id} key={item.id}>
          {item.name}
        </option>
      ))}
    </>
  );
}
