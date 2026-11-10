import { useQueries, useQuery } from '@tanstack/react-query';

import { getKnowledgeArticles, getKnowledgeDocuments } from '@/features/knowledge/api/knowledge-api';
import { getDepartments, getPositions } from '@/features/organization/api/organization-api';
import { getProjects } from '@/features/projects/api/projects-api';

export interface SourceOption {
  id: string;
  label: string;
}

export interface DocumentOption extends SourceOption {
  /** Knowledge article the document is attached to; used as the default section title. */
  articleTitle: string;
  documentName: string;
}

/**
 * Loads everything the template builder can link a step to: published company/project articles,
 * their clean documents, and the department/position targeting catalogs.
 */
export function useTemplateSourceOptions() {
  const companyArticles = useQuery({
    queryKey: ['knowledge', 'template-options'],
    queryFn: () => getKnowledgeArticles()
  });
  const projects = useQuery({ queryKey: ['projects', 'template-options'], queryFn: getProjects });
  const projectArticles = useQueries({
    queries: (projects.data ?? []).map((project) => ({
      queryKey: ['knowledge', 'project', project.id, 'template-options'],
      queryFn: () => getKnowledgeArticles({ projectId: project.id })
    }))
  });

  const projectNames = new Map((projects.data ?? []).map((project) => [project.id, project.name]));
  const publishedArticles = [
    ...(companyArticles.data ?? []),
    ...projectArticles.flatMap((query) => query.data ?? [])
  ].filter((article) => article.status === 'Published');
  const scopeLabel = (projectId?: string) =>
    projectId ? `${projectNames.get(projectId) ?? 'Project'} · ` : 'Company · ';

  const documentQueries = useQueries({
    queries: publishedArticles.map((article) => ({
      queryKey: ['knowledge', article.id, 'documents', 'template-options'],
      queryFn: () => getKnowledgeDocuments(article.id)
    }))
  });

  const articles: SourceOption[] = publishedArticles.map((article) => ({
    id: article.id,
    label: `${scopeLabel(article.projectId)}${article.title}`
  }));
  const documents: DocumentOption[] = publishedArticles.flatMap((article, index) =>
    (documentQueries[index]?.data ?? [])
      .filter((document) => document.status === 'Clean')
      .map((document) => ({
        id: document.id,
        label: `${scopeLabel(article.projectId)}${article.title} · ${document.displayName}`,
        articleTitle: article.title,
        documentName: document.displayName
      }))
  );

  const departments = useQuery({
    queryKey: ['departments', 'template-options'],
    queryFn: () => getDepartments(undefined, 100)
  });
  const positions = useQuery({
    queryKey: ['positions', 'template-options'],
    queryFn: () => getPositions(undefined, 100)
  });

  return {
    articles,
    documents,
    projects: (projects.data ?? []).map((project) => ({ id: project.id, label: project.name })),
    departments: (departments.data?.items ?? []).map((unit) => ({ id: unit.id, label: unit.name })),
    positions: (positions.data?.items ?? []).map((position) => ({ id: position.id, label: position.title }))
  };
}
