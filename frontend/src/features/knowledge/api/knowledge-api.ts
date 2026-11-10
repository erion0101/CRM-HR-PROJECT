import { http } from '@/lib/api-client';
import type { KnowledgeArticle, KnowledgeArticleInput } from '../types';
import type { EmployeeDocument } from '@/features/documents/types';

export async function getKnowledgeArticles(options: { projectId?: string; search?: string; includeDrafts?: boolean } = {}): Promise<KnowledgeArticle[]> {
  const { data } = await http.get<KnowledgeArticle[]>('/knowledge/articles', { params: options });
  return data;
}

export async function getKnowledgeArticle(id: string): Promise<KnowledgeArticle> {
  const { data } = await http.get<KnowledgeArticle>(`/knowledge/articles/${id}`);
  return data;
}

export async function createKnowledgeArticle(input: KnowledgeArticleInput): Promise<KnowledgeArticle> {
  const { data } = await http.post<KnowledgeArticle>('/knowledge/articles', input);
  return data;
}

export async function addKnowledgeRevision(article: KnowledgeArticle, input: Omit<KnowledgeArticleInput, 'slug' | 'projectId'> & { changeSummary?: string }): Promise<KnowledgeArticle> {
  const { data } = await http.post<KnowledgeArticle>(`/knowledge/articles/${article.id}/revisions`, input, { headers: { 'If-Match': `"${article.version}"` } });
  return data;
}

export async function submitKnowledgeForReview(article: KnowledgeArticle): Promise<KnowledgeArticle> {
  const { data } = await http.post<KnowledgeArticle>(`/knowledge/articles/${article.id}/review`, undefined, { headers: { 'If-Match': `"${article.version}"` } });
  return data;
}

export async function publishKnowledgeArticle(article: KnowledgeArticle): Promise<KnowledgeArticle> {
  const { data } = await http.post<KnowledgeArticle>(`/knowledge/articles/${article.id}/publish`, undefined, { headers: { 'If-Match': `"${article.version}"` } });
  return data;
}

export async function getKnowledgeDocuments(articleId: string): Promise<EmployeeDocument[]> {
  const { data } = await http.get<EmployeeDocument[]>(`/knowledge/articles/${articleId}/documents`);
  return data;
}

export async function uploadKnowledgeDocument(articleId: string, file: File, documentType: string): Promise<EmployeeDocument> {
  const body = new FormData();
  body.append('file', file);
  body.append('documentType', documentType);
  const { data } = await http.post<EmployeeDocument>(`/knowledge/articles/${articleId}/documents`, body);
  return data;
}

export async function downloadKnowledgeDocument(articleId: string, document: EmployeeDocument): Promise<void> {
  const response = await http.get<Blob>(`/knowledge/articles/${articleId}/documents/${document.id}/download`, { responseType: 'blob' });
  const url = URL.createObjectURL(response.data);
  const link = window.document.createElement('a');
  link.href = url;
  link.download = document.displayName;
  link.click();
  URL.revokeObjectURL(url);
}

export async function approveKnowledgeDocumentForAi(articleId: string, documentId: string): Promise<void> {
  await http.post(`/knowledge/articles/${articleId}/documents/${documentId}/ai-approval`, { classification: 'NormalInternal' });
}
