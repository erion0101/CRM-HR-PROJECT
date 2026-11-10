import { http } from '@/lib/api-client';
import type { Project, ProjectEmployeeOption, ProjectInput } from '../types';

export async function getProjects(): Promise<Project[]> {
  const { data } = await http.get<Project[]>('/projects');
  return data;
}

export async function getProject(id: string): Promise<Project> {
  const { data } = await http.get<Project>(`/projects/${id}`);
  return data;
}

export async function getProjectEmployeeOptions(): Promise<ProjectEmployeeOption[]> {
  const { data } = await http.get<ProjectEmployeeOption[]>('/projects/employee-options');
  return data;
}

export async function createProject(input: ProjectInput): Promise<Project> {
  const { data } = await http.post<Project>('/projects', input);
  return data;
}

export async function addProjectMember(
  projectId: string,
  input: { employeeId: string; projectRole: string; responsibilities: string; effectiveFrom: string }
): Promise<Project> {
  const { data } = await http.post<Project>(`/projects/${projectId}/members`, { projectId, ...input });
  return data;
}
