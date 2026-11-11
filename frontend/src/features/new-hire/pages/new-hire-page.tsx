import { useQuery } from '@tanstack/react-query';
import { ArrowLeft, ArrowRight, CheckCircle2, Circle, Copy, Loader2, MinusCircle, XCircle } from 'lucide-react';
import { useState } from 'react';
import { useForm, type FieldPath } from 'react-hook-form';
import { Link } from 'react-router';

import { roleLabels } from '@/app/access';
import { PageHeader } from '@/components/navigation/page-header';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { getAssignmentOptions } from '@/features/onboarding/api/onboarding-api';

import { JobStep, LoginStep, OnboardingStep, PersonStep } from '../components/new-hire-steps';
import { useNewHireLookups } from '../hooks/use-new-hire-lookups';
import { type NewHirePlan, type RunStep, useNewHireRun } from '../hooks/use-new-hire-run';
import { type DuplicateEmployee, findDuplicateEmployees } from '../duplicates';
import type { NewHireForm } from '../types';
import { generateTemporaryPassword, suggestEmployeeNumber } from '../utils';

const steps: { title: string; description: string; fields: FieldPath<NewHireForm>[] }[] = [
  { title: 'Person', description: 'Who is joining?', fields: ['firstName', 'lastName', 'workEmail', 'employeeNumber'] },
  {
    title: 'Job',
    description: 'Where will they work, and from when?',
    fields: ['departmentId', 'positionId', 'startDate']
  },
  { title: 'Login', description: 'How will they sign in?', fields: [] },
  { title: 'Onboarding', description: 'What must they read before starting?', fields: ['templateId'] },
  { title: 'Review', description: 'Check everything, then create.', fields: [] }
];

/** Keyed wrapper so "Add another new hire" starts from a completely fresh form. */
export function NewHirePage() {
  const [round, setRound] = useState(0);
  return <NewHireWizard key={round} onRestart={() => setRound((value) => value + 1)} />;
}

function NewHireWizard({ onRestart }: { onRestart: () => void }) {
  const lookups = useNewHireLookups();
  const run = useNewHireRun();
  const [current, setCurrent] = useState(0);
  const form = useForm<NewHireForm>({
    defaultValues: {
      firstName: '',
      lastName: '',
      workEmail: '',
      employeeNumber: suggestEmployeeNumber(),
      phone: '',
      departmentId: '',
      positionId: '',
      startDate: new Date().toISOString().slice(0, 10),
      createLogin: true,
      role: 'Employee',
      temporaryPassword: generateTemporaryPassword(),
      startOnboarding: true,
      templateId: '',
      projectId: '',
      projectRole: 'Team member',
      mentorEmployeeId: ''
    }
  });

  const values = form.watch();
  const template = lookups.templates.find((item) => item.id === values.templateId);
  const onboardingProjectId = template?.kind === 'Project' ? template.projectId : values.projectId || undefined;
  const options = useQuery({
    queryKey: ['onboarding', 'assignment-options', template?.id, onboardingProjectId],
    queryFn: () => getAssignmentOptions(template!.id, onboardingProjectId),
    enabled: values.startOnboarding && Boolean(template)
  });
  // Unticked documents belong to one package + project choice; changing the choice starts with everything ticked.
  const selectionKey = `${template?.id ?? ''}|${onboardingProjectId ?? ''}`;
  const [selection, setSelection] = useState<{ key: string; excluded: ReadonlySet<string> }>({
    key: '',
    excluded: new Set()
  });
  const excluded = selection.key === selectionKey ? selection.excluded : new Set<string>();
  const toggle = (stepId: string) => {
    const next = new Set(excluded);
    if (next.has(stepId)) next.delete(stepId);
    else next.add(stepId);
    setSelection({ key: selectionKey, excluded: next });
  };

  const [duplicates, setDuplicates] = useState<DuplicateEmployee[]>([]);
  const [checking, setChecking] = useState(false);

  /** Stops on the Person step when the email or employee number already belongs to someone. */
  const personIsNew = async () => {
    setChecking(true);
    try {
      const found = await findDuplicateEmployees(form.getValues('workEmail'), form.getValues('employeeNumber'));
      setDuplicates(found);
      for (const duplicate of found) {
        const name = `${duplicate.employee.firstName} ${duplicate.employee.lastName}`;
        form.setError(duplicate.field, {
          message:
            duplicate.field === 'workEmail'
              ? `Already used by ${name}. Use a different email.`
              : `Already used by ${name}. Use a different number.`
        });
      }
      return found.length === 0;
    } catch {
      return true; // The server still refuses duplicates with a clear message if this check is unavailable.
    } finally {
      setChecking(false);
    }
  };

  const goTo = async (target: number) => {
    if (target > current && !(await form.trigger(steps[current].fields))) return;
    if (current === 0 && target > 0 && !(await personIsNew())) return;
    // Preselect the first company package the first time HR reaches the onboarding step.
    if (target === 3 && !form.getValues('templateId')) {
      const company = lookups.templates.find((item) => item.kind === 'Company') ?? lookups.templates[0];
      if (company) form.setValue('templateId', company.id);
    }
    setCurrent(target);
  };

  const buildPlan = (): NewHirePlan => {
    const department = lookups.departments.find((item) => item.id === values.departmentId);
    const allStepIds = (options.data?.packages ?? []).flatMap((pkg) =>
      pkg.readingSteps.map((step) => step.templateStepId)
    );
    const membershipProject = template?.kind === 'Project' ? template.projectId : values.projectId;
    return {
      person: {
        employeeNumber: values.employeeNumber.trim(),
        firstName: values.firstName.trim(),
        lastName: values.lastName.trim(),
        workEmail: values.workEmail.trim(),
        phone: values.phone.trim() || undefined
      },
      job: {
        departmentId: values.departmentId,
        positionId: values.positionId,
        startDate: values.startDate,
        managerEmployeeId: department?.managerEmployeeId ?? undefined
      },
      project: membershipProject
        ? { projectId: membershipProject, projectRole: values.projectRole.trim() || 'Team member' }
        : undefined,
      onboarding:
        values.startOnboarding && values.templateId
          ? {
              templateId: values.templateId,
              projectId: onboardingProjectId,
              mentorEmployeeId: values.mentorEmployeeId || undefined,
              readingStepIds: excluded.size ? allStepIds.filter((id) => !excluded.has(id)) : undefined
            }
          : undefined,
      login: values.createLogin ? { role: values.role, temporaryPassword: values.temporaryPassword } : undefined
    };
  };

  const showRun = run.steps.length > 0;
  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="People"
        title="New hire"
        description="Add a new employee, give them a login, and start their onboarding in one go."
      />

      {run.finished ? (
        <DoneCard values={values} employeeId={run.employeeId} onRestart={onRestart} />
      ) : (
        <>
          <ol className="wizard-steps" aria-label="New hire steps">
            {steps.map((step, index) => (
              <li key={step.title} className={index === current ? 'is-current' : index < current ? 'is-done' : ''}>
                <button
                  type="button"
                  disabled={run.started || index > current}
                  aria-current={index === current ? 'step' : undefined}
                  onClick={() => void goTo(index)}
                >
                  <span>{index < current ? <CheckCircle2 size={16} /> : index + 1}</span>
                  {step.title}
                </button>
              </li>
            ))}
          </ol>

          <Card className="wizard-card">
            <header>
              <h2>{steps[current].title}</h2>
              <p>{steps[current].description}</p>
            </header>
            {current === 0 ? <PersonStep form={form} /> : null}
            {current === 0 && duplicates.length ? <DuplicateNotice duplicates={duplicates} /> : null}
            {current === 1 ? <JobStep form={form} /> : null}
            {current === 2 ? <LoginStep form={form} /> : null}
            {current === 3 ? <OnboardingStep form={form} excluded={excluded} onToggle={toggle} /> : null}
            {current === 4 ? <ReviewStep values={values} plan={buildPlan()} lookups={lookups} /> : null}

            {showRun ? <RunProgress steps={run.steps} /> : null}

            <footer className="form-actions">
              {current > 0 && !run.started ? (
                <Button type="button" variant="secondary" onClick={() => void goTo(current - 1)}>
                  <ArrowLeft size={16} /> Back
                </Button>
              ) : null}
              {current < steps.length - 1 ? (
                <Button type="button" loading={checking} onClick={() => void goTo(current + 1)}>
                  Next <ArrowRight size={16} />
                </Button>
              ) : (
                <Button type="button" loading={run.running} onClick={() => void run.run(buildPlan())}>
                  {run.started ? 'Try again' : 'Create new hire'}
                </Button>
              )}
            </footer>
          </Card>
        </>
      )}
    </div>
  );
}

function DuplicateNotice({ duplicates }: { duplicates: DuplicateEmployee[] }) {
  const people = [...new Map(duplicates.map((item) => [item.employee.id, item.employee])).values()];
  return (
    <div className="duplicate-notice" role="alert">
      <strong>This person may already exist.</strong>
      {people.map((employee) => (
        <p key={employee.id}>
          {employee.firstName} {employee.lastName} ({employee.employeeNumber}, {employee.workEmail}) already has
          {duplicates.filter((item) => item.employee.id === employee.id).length > 1
            ? ' this email and number'
            : duplicates.find((item) => item.employee.id === employee.id)!.field === 'workEmail'
              ? ' this work email'
              : ' this employee number'}
          . <Link to={`/employees/${employee.id}`}>Open their profile</Link>
        </p>
      ))}
      <small>
        If it is the same person, open their profile instead of creating them again. If not, change the highlighted
        field.
      </small>
    </div>
  );
}

function ReviewStep({
  values,
  plan,
  lookups
}: {
  values: NewHireForm;
  plan: NewHirePlan;
  lookups: ReturnType<typeof useNewHireLookups>;
}) {
  const department = lookups.departments.find((item) => item.id === values.departmentId);
  const position = lookups.positions.find((item) => item.id === values.positionId);
  const project = lookups.projects.find((item) => item.id === plan.project?.projectId);
  const template = lookups.templates.find((item) => item.id === plan.onboarding?.templateId);
  const rows: [string, string][] = [
    ['Name', `${values.firstName} ${values.lastName}`],
    ['Work email', values.workEmail],
    [
      'Job',
      `${position?.title ?? '—'} in ${department?.name ?? '—'}, starting ${new Date(values.startDate).toLocaleDateString()}`
    ],
    ['Manager', department?.managerName ?? 'None set for this department'],
    ['Login', plan.login ? `${roleLabels[plan.login.role]} account for ${values.workEmail}` : 'No login yet'],
    ['Project', project ? `${project.name} (${plan.project?.projectRole})` : 'None'],
    [
      'Onboarding',
      template
        ? `${template.name}${plan.onboarding?.readingStepIds ? `, ${plan.onboarding.readingStepIds.length} selected documents` : ', all documents'}`
        : 'Not started'
    ]
  ];
  return (
    <dl className="review-list">
      {rows.map(([label, value]) => (
        <div key={label}>
          <dt>{label}</dt>
          <dd>{value}</dd>
        </div>
      ))}
    </dl>
  );
}

const statusIcon: Record<RunStep['status'], React.ReactNode> = {
  pending: <Circle size={16} />,
  running: <Loader2 size={16} className="spin" />,
  done: <CheckCircle2 size={16} />,
  failed: <XCircle size={16} />,
  skipped: <MinusCircle size={16} />
};

function RunProgress({ steps }: { steps: RunStep[] }) {
  return (
    <ul className="run-progress" aria-live="polite">
      {steps.map((step) => (
        <li key={step.key} className={`is-${step.status}`}>
          {statusIcon[step.status]}
          <span>
            {step.label}
            {step.status === 'skipped' ? ' (skipped)' : ''}
            {step.error ? <small role="alert">{step.error}</small> : null}
          </span>
        </li>
      ))}
    </ul>
  );
}

function DoneCard({
  values,
  employeeId,
  onRestart
}: {
  values: NewHireForm;
  employeeId?: string;
  onRestart: () => void;
}) {
  return (
    <Card className="wizard-card">
      <header>
        <h2>
          <CheckCircle2 size={22} /> {values.firstName} {values.lastName} is set up
        </h2>
        <p>
          {values.startOnboarding ? 'Their onboarding journey is ready and waiting for them. ' : ''}
          {values.createLogin ? 'Send them these sign-in details:' : 'They have no login yet.'}
        </p>
      </header>
      {values.createLogin ? (
        <dl className="review-list">
          <div>
            <dt>Sign-in page</dt>
            <dd>{window.location.origin}/login</dd>
          </div>
          <div>
            <dt>Email</dt>
            <dd>{values.workEmail}</dd>
          </div>
          <div>
            <dt>Temporary password</dt>
            <dd className="row-actions">
              <code>{values.temporaryPassword}</code>
              <Button
                size="small"
                variant="secondary"
                aria-label="Copy password"
                onClick={() => void navigator.clipboard?.writeText(values.temporaryPassword)}
              >
                <Copy size={14} />
              </Button>
            </dd>
          </div>
        </dl>
      ) : null}
      {values.createLogin ? (
        <p className="field__hint">
          Ask them to change the password after signing in (profile menu → Security settings).
        </p>
      ) : null}
      <footer className="form-actions">
        {employeeId ? (
          <Link className="button button--secondary button--medium" to={`/employees/${employeeId}`}>
            Open their profile
          </Link>
        ) : null}
        <Link
          className="button button--secondary button--medium"
          to={employeeId ? `/employees/${employeeId}?tab=onboarding` : '/onboarding/progress'}
        >
          See their onboarding
        </Link>
        <Button onClick={onRestart}>Add another new hire</Button>
      </footer>
    </Card>
  );
}
