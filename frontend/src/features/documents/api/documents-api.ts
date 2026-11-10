import { http } from '@/lib/api-client';

import type { DocumentFolder, DocumentFolderInput, DocumentWorkspaceItem, EmployeeDocument } from '../types';

export async function getDocuments(employeeId: string): Promise<EmployeeDocument[]> {
  const { data } = await http.get<EmployeeDocument[]>(`/employees/${employeeId}/documents`);
  return data;
}

function uploadBody(file: File, documentType: string, folderId?: string): FormData {
  const form = new FormData();
  form.append('file', file);
  form.append('documentType', documentType);
  if (folderId) form.append('folderId', folderId);
  return form;
}

export async function uploadDocument(
  employeeId: string,
  file: File,
  documentType: string,
  folderId?: string
): Promise<EmployeeDocument> {
  const { data } = await http.post<EmployeeDocument>(
    `/employees/${employeeId}/documents`,
    uploadBody(file, documentType, folderId)
  );
  return data;
}

export async function uploadDocumentVersion(
  employeeId: string,
  document: EmployeeDocument,
  file: File
): Promise<EmployeeDocument> {
  const { data } = await http.post<EmployeeDocument>(
    `/employees/${employeeId}/documents/${document.id}/versions`,
    uploadBody(file, document.documentType),
    { headers: { 'If-Match': `"${document.version}"` } }
  );
  return data;
}

export async function downloadDocument(employeeId: string, document: EmployeeDocument): Promise<void> {
  const file = await getDocumentFile(employeeId, document.id);
  const url = URL.createObjectURL(file);
  const link = window.document.createElement('a');
  link.href = url;
  link.download = document.displayName;
  link.click();
  URL.revokeObjectURL(url);
}

export async function getDocumentFile(employeeId: string, documentId: string): Promise<Blob> {
  const response = await http.get<Blob>(`/employees/${employeeId}/documents/${documentId}/download`, {
    responseType: 'blob'
  });
  return response.data;
}

export async function archiveDocument(employeeId: string, document: EmployeeDocument): Promise<void> {
  await http.delete(`/employees/${employeeId}/documents/${document.id}`, {
    headers: { 'If-Match': `"${document.version}"` }
  });
}

export async function getDocumentFolders(): Promise<DocumentFolder[]> {
  const { data } = await http.get<DocumentFolder[]>('/document-folders');
  return data;
}

export async function createDocumentFolder(input: DocumentFolderInput): Promise<DocumentFolder> {
  const { data } = await http.post<DocumentFolder>('/document-folders', input);
  return data;
}

export async function searchDocuments(folderId?: string, search?: string): Promise<DocumentWorkspaceItem[]> {
  const { data } = await http.get<DocumentWorkspaceItem[]>('/documents', { params: { folderId, search } });
  return data;
}

export async function downloadWorkspaceDocument(document: DocumentWorkspaceItem): Promise<void> {
  await downloadDocument(document.employeeId, document);
}
