export type DocumentStatus = 'PendingScan' | 'Clean' | 'Rejected' | 'Archived';
export type DocumentFolderScope = 'Global' | 'Employee';

export interface EmployeeDocument {
  id: string;
  folderId: string | null;
  displayName: string;
  documentType: string;
  status: DocumentStatus;
  currentVersion: number;
  version: number;
}

export interface DocumentFolder {
  id: string;
  name: string;
  description: string | null;
  isSystemFolder: boolean;
  scope: DocumentFolderScope;
  employeeId: string | null;
  allowedRoles: string[];
  itemCount: number;
  version: number;
}

export interface DocumentFolderInput {
  name: string;
  description?: string;
  scope: DocumentFolderScope;
  employeeId?: string;
  allowedRoles: string[];
}

export interface DocumentWorkspaceItem {
  id: string;
  employeeId: string;
  employeeName: string;
  folderId: string | null;
  displayName: string;
  documentType: string;
  contentType: string;
  sizeBytes: number;
  status: DocumentStatus;
  currentVersion: number;
  uploadedAtUtc: string;
  version: number;
}
