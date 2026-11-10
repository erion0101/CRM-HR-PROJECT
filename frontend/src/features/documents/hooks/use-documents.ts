import { useQuery } from '@tanstack/react-query';

import { getDocumentFolders, getDocuments, searchDocuments } from '../api/documents-api';

export const documentKeys = {
  employee: (employeeId: string) => ['documents', 'employee', employeeId] as const,
  folders: ['documents', 'folders'] as const,
  workspace: (folderId?: string, search?: string) => ['documents', 'workspace', { folderId, search }] as const
};

export function useEmployeeDocuments(employeeId: string) {
  return useQuery({
    queryKey: documentKeys.employee(employeeId),
    queryFn: () => getDocuments(employeeId),
    enabled: Boolean(employeeId),
    refetchInterval: (query) => (query.state.data?.some((item) => item.status === 'PendingScan') ? 4_000 : false)
  });
}

export function useDocumentFolders() {
  return useQuery({ queryKey: documentKeys.folders, queryFn: getDocumentFolders });
}

export function useDocumentWorkspace(folderId?: string, search?: string) {
  return useQuery({
    queryKey: documentKeys.workspace(folderId, search),
    queryFn: () => searchDocuments(folderId, search)
  });
}
