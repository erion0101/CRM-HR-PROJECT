import { useQuery } from '@tanstack/react-query';

import { getOnboardingTemplates } from '@/features/onboarding/api/onboarding-api';
import { getDepartments, getPositions } from '@/features/organization/api/organization-api';
import { getProjects } from '@/features/projects/api/projects-api';

/** Lookups shared by several steps; React Query de-duplicates the requests. */
export function useNewHireLookups() {
  const departments = useQuery({
    queryKey: ['departments', 'new-hire'],
    queryFn: () => getDepartments(undefined, 100)
  });
  const positions = useQuery({ queryKey: ['positions', 'new-hire'], queryFn: () => getPositions(undefined, 100) });
  const projects = useQuery({ queryKey: ['projects'], queryFn: getProjects });
  const templates = useQuery({
    queryKey: ['onboarding', 'templates', 'published'],
    queryFn: () => getOnboardingTemplates(true)
  });
  return {
    departments: (departments.data?.items ?? []).filter((unit) => unit.type === 'Department'),
    positions: positions.data?.items ?? [],
    projects: projects.data ?? [],
    templates: templates.data ?? []
  };
}
