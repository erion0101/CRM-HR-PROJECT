import { useQueries, useQuery } from '@tanstack/react-query';

import type { EmployeeDocument } from '@/features/documents/types';
import { getKnowledgeArticles, getKnowledgeDocuments } from '@/features/knowledge/api/knowledge-api';
import { getProjects } from '@/features/projects/api/projects-api';

import { getOnboardingQuiz, getOnboardingTemplates } from '../api/onboarding-api';
import { buildSetupChecklist } from '../utils/setup-checklist';

function countDocuments(documents: EmployeeDocument[][]) {
  const all = documents.flat();
  return {
    ready: all.filter((document) => document.status === 'Clean').length,
    scanning: all.filter((document) => document.status === 'PendingScan').length
  };
}

/** Live onboarding setup status for HR: documents, packages, and quizzes, company first and then each project. */
export function useOnboardingSetup(enabled = true) {
  const projects = useQuery({ queryKey: ['projects'], queryFn: getProjects, enabled });
  const activeProjects = (projects.data ?? []).filter((project) => project.status === 'Active');
  const companyArticles = useQuery({
    queryKey: ['knowledge', 'setup', 'company'],
    queryFn: () => getKnowledgeArticles(),
    enabled
  });
  const projectArticles = useQueries({
    queries: activeProjects.map((project) => ({
      queryKey: ['knowledge', 'setup', 'project', project.id],
      queryFn: () => getKnowledgeArticles({ projectId: project.id }),
      enabled
    }))
  });
  const articles = [
    ...(companyArticles.data ?? []).filter((article) => !article.projectId),
    ...projectArticles.flatMap((query) => query.data ?? [])
  ].filter((article) => article.status === 'Published');
  const documents = useQueries({
    queries: articles.map((article) => ({
      queryKey: ['knowledge', article.id, 'documents'],
      queryFn: () => getKnowledgeDocuments(article.id),
      enabled
    }))
  });
  const documentsFor = (projectId?: string) =>
    countDocuments(
      articles
        .map((article, index) => ({ article, documents: documents[index]?.data ?? [] }))
        .filter(({ article }) => (article.projectId ?? undefined) === projectId)
        .map((item) => item.documents)
    );

  const packages = useQuery({
    queryKey: ['onboarding', 'templates', 'manage'],
    queryFn: () => getOnboardingTemplates(false),
    enabled
  });
  const published = (packages.data ?? []).filter((pkg) => pkg.publishedVersion != null);
  const quizzes = useQueries({
    queries: published.map((pkg) => ({
      queryKey: ['onboarding', 'quiz', pkg.id],
      queryFn: () => getOnboardingQuiz(pkg.id),
      enabled
    }))
  });

  const loading = projects.isLoading || companyArticles.isLoading || packages.isLoading;
  const checklist = buildSetupChecklist({
    companyDocuments: documentsFor(undefined),
    projects: activeProjects.map((project) => ({ id: project.id, name: project.name, ...documentsFor(project.id) })),
    packages: packages.data ?? [],
    quizzes: Object.fromEntries(published.map((pkg, index) => [pkg.id, quizzes[index]?.data]))
  });
  return { loading, ...checklist };
}
