import { useQuery } from '@tanstack/react-query';
import { Copy, RefreshCw } from 'lucide-react';
import type { UseFormReturn } from 'react-hook-form';

import { roleLabels } from '@/app/access';
import { FieldShell, Input, Select } from '@/components/forms/field';
import { Button } from '@/components/ui/button';
import { useEmployees } from '@/features/employees/hooks/use-employees';
import { getAssignmentOptions } from '@/features/onboarding/api/onboarding-api';
import { ReadingSelection } from '@/features/onboarding/components/reading-selection';
import { packageKindLabel } from '@/features/onboarding/utils/package-labels';
import type { Role } from '@/types/api';

import { useNewHireLookups } from '../hooks/use-new-hire-lookups';
import type { NewHireForm } from '../types';
import { generateTemporaryPassword } from '../utils';

type StepProps = { form: UseFormReturn<NewHireForm> };

export function PersonStep({ form }: StepProps) {
  const { register, formState } = form;
  return (
    <div className="form-stack">
      <div className="form-grid">
        <FieldShell label="First name" htmlFor="hireFirstName" required error={formState.errors.firstName?.message}>
          <Input id="hireFirstName" autoFocus {...register('firstName', { required: 'Enter the first name.' })} />
        </FieldShell>
        <FieldShell label="Last name" htmlFor="hireLastName" required error={formState.errors.lastName?.message}>
          <Input id="hireLastName" {...register('lastName', { required: 'Enter the last name.' })} />
        </FieldShell>
        <FieldShell
          label="Work email"
          htmlFor="hireEmail"
          required
          hint="Also used as their login"
          error={formState.errors.workEmail?.message}
        >
          <Input
            id="hireEmail"
            type="email"
            {...register('workEmail', {
              required: 'Enter the work email.',
              pattern: { value: /^[^@\s]+@[^@\s]+\.[^@\s]+$/, message: 'Enter a valid email address.' }
            })}
          />
        </FieldShell>
        <FieldShell
          label="Employee number"
          htmlFor="hireNumber"
          required
          hint="Suggested automatically; change it if you use your own numbers"
          error={formState.errors.employeeNumber?.message}
        >
          <Input id="hireNumber" {...register('employeeNumber', { required: 'Enter an employee number.' })} />
        </FieldShell>
        <FieldShell label="Phone" htmlFor="hirePhone" hint="Optional">
          <Input id="hirePhone" type="tel" {...register('phone')} />
        </FieldShell>
      </div>
    </div>
  );
}

export function JobStep({ form }: StepProps) {
  const { register, watch, formState, setValue } = form;
  const { departments, positions } = useNewHireLookups();
  const departmentId = watch('departmentId');
  const department = departments.find((item) => item.id === departmentId);
  const departmentPositions = positions.filter((position) => position.departmentId === departmentId);
  return (
    <div className="form-stack">
      <div className="form-grid">
        <FieldShell label="Department" htmlFor="hireDepartment" required error={formState.errors.departmentId?.message}>
          <Select
            id="hireDepartment"
            {...register('departmentId', {
              required: 'Choose a department.',
              onChange: () => setValue('positionId', '')
            })}
          >
            <option value="">Choose a department</option>
            {departments.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </Select>
        </FieldShell>
        <FieldShell label="Position" htmlFor="hirePosition" required error={formState.errors.positionId?.message}>
          <Select
            id="hirePosition"
            disabled={!departmentId}
            {...register('positionId', { required: 'Choose a position.' })}
          >
            <option value="">{departmentId ? 'Choose a position' : 'Choose a department first'}</option>
            {departmentPositions.map((position) => (
              <option key={position.id} value={position.id}>
                {position.title}
              </option>
            ))}
          </Select>
        </FieldShell>
        <FieldShell label="Start date" htmlFor="hireStart" required error={formState.errors.startDate?.message}>
          <Input id="hireStart" type="date" {...register('startDate', { required: 'Choose the first working day.' })} />
        </FieldShell>
      </div>
      {department ? (
        <p className="field__hint" role="status">
          {department.managerName
            ? `Reports to ${department.managerName}, the ${department.name} manager. They will also review this person's onboarding.`
            : `${department.name} has no manager set yet, so nobody will review this person's onboarding milestones.`}
        </p>
      ) : null}
    </div>
  );
}

export function LoginStep({ form }: StepProps) {
  const { register, watch, setValue } = form;
  const createLogin = watch('createLogin');
  const password = watch('temporaryPassword');
  return (
    <div className="form-stack">
      <label className="checkbox-field">
        <input type="checkbox" {...register('createLogin')} /> Create a login so they can sign in and do their
        onboarding
      </label>
      {createLogin ? (
        <div className="form-grid">
          <FieldShell label="Login email" htmlFor="hireLogin">
            <Input id="hireLogin" value={watch('workEmail')} readOnly />
          </FieldShell>
          <FieldShell label="Role" htmlFor="hireRole" hint="Employee is right for almost every new hire">
            <Select id="hireRole" {...register('role')}>
              {(Object.keys(roleLabels) as Role[]).map((role) => (
                <option key={role} value={role}>
                  {roleLabels[role]}
                </option>
              ))}
            </Select>
          </FieldShell>
          <FieldShell label="Temporary password" htmlFor="hirePassword" hint="You will see it again on the last screen">
            <div className="row-actions">
              <Input id="hirePassword" value={password} readOnly />
              <Button
                type="button"
                size="small"
                variant="secondary"
                aria-label="Generate a new password"
                onClick={() => setValue('temporaryPassword', generateTemporaryPassword())}
              >
                <RefreshCw size={14} />
              </Button>
              <Button
                type="button"
                size="small"
                variant="secondary"
                aria-label="Copy password"
                onClick={() => void navigator.clipboard?.writeText(password)}
              >
                <Copy size={14} />
              </Button>
            </div>
          </FieldShell>
        </div>
      ) : (
        <p className="field__hint">You can create the login later in Administration → User access.</p>
      )}
    </div>
  );
}

export function OnboardingStep({
  form,
  excluded,
  onToggle
}: StepProps & { excluded: ReadonlySet<string>; onToggle: (stepId: string) => void }) {
  const { register, watch } = form;
  const { projects, templates } = useNewHireLookups();
  const employees = useEmployees({ limit: 100 });
  const startOnboarding = watch('startOnboarding');
  const template = templates.find((item) => item.id === watch('templateId'));
  const projectId = template?.kind === 'Project' ? template.projectId : watch('projectId') || undefined;
  const options = useQuery({
    queryKey: ['onboarding', 'assignment-options', template?.id, projectId],
    queryFn: () => getAssignmentOptions(template!.id, projectId),
    enabled: startOnboarding && Boolean(template)
  });
  const sortedTemplates = [...templates].sort(
    (a, b) => (a.kind === 'Project' ? 1 : 0) - (b.kind === 'Project' ? 1 : 0)
  );

  return (
    <div className="form-stack">
      <FieldShell
        label="Project"
        htmlFor="hireProject"
        hint="Optional. They are added to the project, and its documents can be included below."
      >
        <Select id="hireProject" {...register('projectId')}>
          <option value="">No project yet</option>
          {projects.map((project) => (
            <option key={project.id} value={project.id}>
              {project.name}
            </option>
          ))}
        </Select>
      </FieldShell>
      {watch('projectId') ? (
        <FieldShell label="Role on the project" htmlFor="hireProjectRole">
          <Input id="hireProjectRole" {...register('projectRole')} />
        </FieldShell>
      ) : null}

      <label className="checkbox-field">
        <input type="checkbox" {...register('startOnboarding')} /> Start an onboarding journey with documents to read
        and a quiz
      </label>
      {startOnboarding ? (
        <>
          {templates.length === 0 ? (
            <p className="field__hint" role="alert">
              No published onboarding package yet. Create one in Onboarding → Manage, or untick this to skip onboarding
              for now.
            </p>
          ) : (
            <FieldShell label="Onboarding package" htmlFor="hireTemplate" required>
              <Select
                id="hireTemplate"
                {...register('templateId', {
                  validate: (value) => !startOnboarding || Boolean(value) || 'Choose a package.'
                })}
              >
                <option value="">Choose a package</option>
                {sortedTemplates.map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name} · {packageKindLabel[item.kind]}
                  </option>
                ))}
              </Select>
            </FieldShell>
          )}
          {options.data ? <ReadingSelection options={options.data} excluded={excluded} onToggle={onToggle} /> : null}
          <FieldShell label="Mentor" htmlFor="hireMentor" hint="Optional. Someone they can ask day-to-day questions.">
            <Select id="hireMentor" {...register('mentorEmployeeId')}>
              <option value="">No mentor</option>
              {(employees.data?.items ?? []).map((employee) => (
                <option key={employee.id} value={employee.id}>
                  {employee.firstName} {employee.lastName}
                </option>
              ))}
            </Select>
          </FieldShell>
        </>
      ) : null}
    </div>
  );
}
