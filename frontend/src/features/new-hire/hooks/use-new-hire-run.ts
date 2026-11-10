import { useRef, useState } from 'react';

import { queryClient } from '@/app/providers/query-provider';
import { createUser } from '@/features/administration/api/administration-api';
import { createEmployee, setEmployment } from '@/features/employees/api/employees-api';
import { assignOnboarding } from '@/features/onboarding/api/onboarding-api';
import { assignDepartmentEmployee } from '@/features/organization/api/organization-api';
import { addProjectMember } from '@/features/projects/api/projects-api';
import { toAppError } from '@/lib/api-error';
import type { Role } from '@/types/api';

export interface NewHirePlan {
  person: { employeeNumber: string; firstName: string; lastName: string; workEmail: string; phone?: string };
  job: { departmentId: string; positionId: string; startDate: string; managerEmployeeId?: string };
  project?: { projectId: string; projectRole: string };
  onboarding?: { templateId: string; projectId?: string; mentorEmployeeId?: string; readingStepIds?: string[] };
  login?: { role: Role; temporaryPassword: string };
}

export type RunStepKey = 'employee' | 'job' | 'project' | 'onboarding' | 'employment' | 'login';
export type RunStepStatus = 'pending' | 'running' | 'done' | 'failed' | 'skipped';
export interface RunStep {
  key: RunStepKey;
  label: string;
  status: RunStepStatus;
  error?: string;
}

const labels: Record<RunStepKey, string> = {
  employee: 'Create the employee record',
  job: 'Set department, position, and manager',
  project: 'Add to the project',
  onboarding: 'Start the onboarding journey',
  employment: 'Activate employment',
  login: 'Create the login account'
};

/**
 * The order matters: onboarding is assigned before employment is activated, so an automatic "default package"
 * assignment (which only runs for employees without a journey) cannot clash with the one HR chose here.
 */
const order: RunStepKey[] = ['employee', 'job', 'project', 'onboarding', 'employment', 'login'];

function initialSteps(plan: NewHirePlan): RunStep[] {
  const included: Record<RunStepKey, boolean> = {
    employee: true,
    job: true,
    project: Boolean(plan.project),
    onboarding: Boolean(plan.onboarding),
    employment: true,
    login: Boolean(plan.login)
  };
  return order.map((key) => ({ key, label: labels[key], status: included[key] ? 'pending' : 'skipped' }));
}

/** Runs the new-hire steps one by one. Completed steps are remembered, so "Try again" resumes after a failure. */
export function useNewHireRun() {
  const [steps, setSteps] = useState<RunStep[]>([]);
  const [running, setRunning] = useState(false);
  const employeeId = useRef<string | undefined>(undefined);
  const completed = useRef(new Set<RunStepKey>());

  const update = (key: RunStepKey, status: RunStepStatus, error?: string) =>
    setSteps((current) => current.map((step) => (step.key === key ? { ...step, status, error } : step)));

  const actions: Record<RunStepKey, (plan: NewHirePlan) => Promise<void>> = {
    employee: async (plan) => {
      employeeId.current = (await createEmployee(plan.person)).id;
    },
    job: async (plan) => {
      await assignDepartmentEmployee(plan.job.departmentId, employeeId.current!, {
        positionId: plan.job.positionId,
        effectiveFrom: plan.job.startDate
      });
    },
    project: async (plan) => {
      await addProjectMember(plan.project!.projectId, {
        employeeId: employeeId.current!,
        projectRole: plan.project!.projectRole,
        responsibilities: 'Onboarding onto the project',
        effectiveFrom: plan.job.startDate
      });
    },
    onboarding: async (plan) => {
      await assignOnboarding({
        employeeId: employeeId.current!,
        templateId: plan.onboarding!.templateId,
        projectId: plan.onboarding!.projectId,
        managerEmployeeId: plan.job.managerEmployeeId,
        mentorEmployeeId: plan.onboarding!.mentorEmployeeId,
        startDate: plan.job.startDate,
        readingStepIds: plan.onboarding!.readingStepIds
      });
    },
    employment: async (plan) => {
      await setEmployment(employeeId.current!, { status: 'Active', startDate: plan.job.startDate });
    },
    login: async (plan) => {
      await createUser({
        email: plan.person.workEmail,
        temporaryPassword: plan.login!.temporaryPassword,
        role: plan.login!.role,
        employeeId: employeeId.current!
      });
    }
  };

  const run = async (plan: NewHirePlan) => {
    const planned = initialSteps(plan);
    setSteps(planned.map((step) => (completed.current.has(step.key) ? { ...step, status: 'done' } : step)));
    setRunning(true);
    try {
      for (const step of planned) {
        if (step.status === 'skipped' || completed.current.has(step.key)) continue;
        update(step.key, 'running');
        try {
          await actions[step.key](plan);
          completed.current.add(step.key);
          update(step.key, 'done');
        } catch (error) {
          update(step.key, 'failed', toAppError(error).message);
          return false;
        }
      }
      return true;
    } finally {
      setRunning(false);
      await queryClient.invalidateQueries();
    }
  };

  const finished = steps.length > 0 && steps.every((step) => step.status === 'done' || step.status === 'skipped');
  const started = completed.current.size > 0;
  return { steps, running, finished, started, employeeId: employeeId.current, run };
}
