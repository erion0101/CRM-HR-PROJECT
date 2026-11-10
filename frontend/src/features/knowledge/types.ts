export type KnowledgeArticleStatus = 'Draft' | 'InReview' | 'Published' | 'Archived';
export type KnowledgeAudience = 'AllEmployees' | 'Department' | 'ProjectMembers' | 'Managers' | 'HrOnly';

export interface KnowledgeArticle {
  id: string;
  slug: string;
  title: string;
  description: string;
  category: string;
  content: string;
  contentFormat: 'Markdown';
  audience: KnowledgeAudience;
  projectId?: string;
  departmentId?: string;
  sortOrder: number;
  status: KnowledgeArticleStatus;
  revision: number;
  lastReviewedAtUtc?: string;
  version: number;
}

export interface KnowledgeArticleInput {
  slug: string;
  title: string;
  description: string;
  category: string;
  content: string;
  audience: KnowledgeAudience;
  projectId?: string;
  departmentId?: string;
  sortOrder: number;
}
