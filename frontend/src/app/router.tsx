import { Suspense, type ReactNode } from 'react';
import { createBrowserRouter, Navigate } from 'react-router';

import { RouteErrorBoundary } from '@/components/feedback/route-error-boundary';
import { Skeleton } from '@/components/feedback/skeleton';
import { AppShell } from '@/components/navigation/app-shell';
import { RoleRoute } from '@/components/navigation/role-route';
import { ProtectedRoute } from '@/features/auth/components/protected-route';
import { lazyWithRetry } from '@/lib/lazy-with-retry';

const LoginPage = lazyWithRetry(() =>
  import('@/features/auth/pages/login-page').then((module) => ({ default: module.LoginPage }))
);
const SecurityPage = lazyWithRetry(() =>
  import('@/features/auth/pages/security-page').then((module) => ({ default: module.SecurityPage }))
);
const DashboardPage = lazyWithRetry(() =>
  import('@/features/dashboard/pages/dashboard-page').then((module) => ({ default: module.DashboardPage }))
);
const EmployeesPage = lazyWithRetry(() =>
  import('@/features/employees/pages/employees-page').then((module) => ({ default: module.EmployeesPage }))
);
const NewHirePage = lazyWithRetry(() =>
  import('@/features/new-hire/pages/new-hire-page').then((module) => ({ default: module.NewHirePage }))
);
const CreateEmployeePage = lazyWithRetry(() =>
  import('@/features/employees/pages/create-employee-page').then((module) => ({ default: module.CreateEmployeePage }))
);
const EmployeeDetailPage = lazyWithRetry(() =>
  import('@/features/employees/pages/employee-detail-page').then((module) => ({ default: module.EmployeeDetailPage }))
);
const DepartmentsPage = lazyWithRetry(() =>
  import('@/features/organization/pages/organization-pages').then((module) => ({ default: module.DepartmentsPage }))
);
const PositionsPage = lazyWithRetry(() =>
  import('@/features/organization/pages/organization-pages').then((module) => ({ default: module.PositionsPage }))
);
const OrganizationChartPage = lazyWithRetry(() =>
  import('@/features/organization/pages/organization-chart-page').then((module) => ({
    default: module.OrganizationChartPage
  }))
);
const UsersPage = lazyWithRetry(() =>
  import('@/features/administration/pages/users-page').then((module) => ({ default: module.UsersPage }))
);
const AuditPage = lazyWithRetry(() =>
  import('@/features/administration/pages/audit-page').then((module) => ({ default: module.AuditPage }))
);
const AiAssistantPage = lazyWithRetry(() =>
  import('@/features/ai-assistant/pages/ai-assistant-page').then((module) => ({ default: module.AiAssistantPage }))
);
const AiSettingsPage = lazyWithRetry(() =>
  import('@/features/ai-assistant/pages/ai-settings-page').then((module) => ({ default: module.AiSettingsPage }))
);
const KnowledgePage = lazyWithRetry(() =>
  import('@/features/knowledge/pages/knowledge-pages').then((module) => ({ default: module.KnowledgePage }))
);
const KnowledgeArticlePage = lazyWithRetry(() =>
  import('@/features/knowledge/pages/knowledge-pages').then((module) => ({ default: module.KnowledgeArticlePage }))
);
const ProjectsPage = lazyWithRetry(() =>
  import('@/features/projects/pages/project-pages').then((module) => ({ default: module.ProjectsPage }))
);
const ProjectDetailPage = lazyWithRetry(() =>
  import('@/features/projects/pages/project-pages').then((module) => ({ default: module.ProjectDetailPage }))
);
const CompanyPage = lazyWithRetry(() =>
  import('@/features/company/pages/company-page').then((module) => ({ default: module.CompanyPage }))
);
const OnboardingPage = lazyWithRetry(() =>
  import('@/features/onboarding/pages/onboarding-page').then((module) => ({ default: module.OnboardingPage }))
);
const OnboardingHomePage = lazyWithRetry(() =>
  import('@/features/onboarding/pages/onboarding-home-page').then((module) => ({ default: module.OnboardingHomePage }))
);
const OnboardingPackagesPage = lazyWithRetry(() =>
  import('@/features/onboarding/pages/onboarding-packages-page').then((module) => ({
    default: module.OnboardingPackagesPage
  }))
);
const OnboardingProgressPage = lazyWithRetry(() =>
  import('@/features/onboarding/pages/onboarding-progress-page').then((module) => ({
    default: module.OnboardingProgressPage
  }))
);
const OnboardingInsightsPage = lazyWithRetry(() =>
  import('@/features/onboarding/pages/onboarding-insights-page').then((module) => ({
    default: module.OnboardingInsightsPage
  }))
);
const OnboardingQuizPage = lazyWithRetry(() =>
  import('@/features/onboarding/pages/onboarding-quiz-page').then((module) => ({ default: module.OnboardingQuizPage }))
);
const ForbiddenPage = lazyWithRetry(() =>
  import('@/features/system/pages/status-pages').then((module) => ({ default: module.ForbiddenPage }))
);
const NotFoundPage = lazyWithRetry(() =>
  import('@/features/system/pages/status-pages').then((module) => ({ default: module.NotFoundPage }))
);

const deferred = (element: ReactNode) => (
  <Suspense
    fallback={
      <div className="route-loader">
        <Skeleton count={4} />
      </div>
    }
  >
    {element}
  </Suspense>
);

export const router = createBrowserRouter([
  { path: '/login', element: deferred(<LoginPage />), errorElement: <RouteErrorBoundary /> },
  {
    element: <ProtectedRoute />,
    errorElement: <RouteErrorBoundary />,
    children: [
      {
        element: <AppShell />,
        children: [
          { index: true, element: deferred(<DashboardPage />) },
          { path: 'employees', element: deferred(<EmployeesPage />) },
          { path: 'employees/new', element: <RoleRoute roles={['HR']}>{deferred(<CreateEmployeePage />)}</RoleRoute> },
          { path: 'employees/new-hire', element: <RoleRoute roles={['HR']}>{deferred(<NewHirePage />)}</RoleRoute> },
          { path: 'employees/:employeeId', element: deferred(<EmployeeDetailPage />) },
          { path: 'organization-chart', element: deferred(<OrganizationChartPage />) },
          { path: 'departments', element: deferred(<DepartmentsPage />) },
          { path: 'positions', element: deferred(<PositionsPage />) },
          { path: 'knowledge', element: deferred(<KnowledgePage />) },
          { path: 'knowledge/articles/:articleId', element: deferred(<KnowledgeArticlePage />) },
          { path: 'projects', element: deferred(<ProjectsPage />) },
          { path: 'projects/:projectId', element: deferred(<ProjectDetailPage />) },
          { path: 'company', element: <RoleRoute roles={['HR']}>{deferred(<CompanyPage />)}</RoleRoute> },
          { path: 'onboarding', element: deferred(<OnboardingHomePage />) },
          { path: 'onboarding/me', element: deferred(<OnboardingPage />) },
          {
            path: 'onboarding/packages',
            element: <RoleRoute roles={['HR']}>{deferred(<OnboardingPackagesPage />)}</RoleRoute>
          },
          {
            path: 'onboarding/progress',
            element: <RoleRoute roles={['HR', 'Manager']}>{deferred(<OnboardingProgressPage />)}</RoleRoute>
          },
          { path: 'onboarding/manage', element: <Navigate to="/onboarding/packages" replace /> },
          {
            path: 'onboarding/insights',
            element: <RoleRoute roles={['HR', 'Manager']}>{deferred(<OnboardingInsightsPage />)}</RoleRoute>
          },
          { path: 'onboarding/quiz/:attemptId', element: deferred(<OnboardingQuizPage />) },
          { path: 'ai-assistant', element: deferred(<AiAssistantPage />) },
          { path: 'ai-assistant/:conversationId', element: deferred(<AiAssistantPage />) },
          { path: 'administration/users', element: <RoleRoute roles={['HR']}>{deferred(<UsersPage />)}</RoleRoute> },
          { path: 'administration/audit', element: <RoleRoute roles={['HR']}>{deferred(<AuditPage />)}</RoleRoute> },
          {
            path: 'administration/ai-settings',
            element: <RoleRoute roles={['HR']}>{deferred(<AiSettingsPage />)}</RoleRoute>
          },
          { path: 'account/security', element: deferred(<SecurityPage />) },
          { path: 'forbidden', element: deferred(<ForbiddenPage />) },
          { path: '*', element: deferred(<NotFoundPage />) }
        ]
      }
    ]
  }
]);
