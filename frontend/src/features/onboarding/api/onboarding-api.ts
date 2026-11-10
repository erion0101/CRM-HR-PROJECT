import { http } from '@/lib/api-client';
import type {
  OnboardingAssignment,
  OnboardingAssignmentOptions,
  OnboardingDashboard,
  OnboardingInsights,
  OnboardingQuizAdmin,
  OnboardingQuizSource,
  OnboardingReadingItem,
  OnboardingReadingStatus,
  OnboardingStep,
  OnboardingStepStatus,
  OnboardingTemplate,
  OnboardingTemplateKind,
  OnboardingTemplateStepInput,
  QuizAttempt,
  QuizQuestionInput,
  QuizResult
} from '../types';

export async function getMyOnboarding(): Promise<OnboardingDashboard> {
  const { data } = await http.get<OnboardingDashboard>('/onboarding/me');
  return data;
}

export async function getOnboardingTemplates(publishedOnly = true): Promise<OnboardingTemplate[]> {
  const { data } = await http.get<OnboardingTemplate[]>('/onboarding/templates', { params: { publishedOnly } });
  return data;
}

export async function createOnboardingTemplate(input: {
  name: string;
  description: string;
  durationDays: number;
  departmentId?: string;
  positionId?: string;
  kind: OnboardingTemplateKind;
  projectId?: string;
  steps: OnboardingTemplateStepInput[];
}): Promise<OnboardingTemplate> {
  const { data } = await http.post<OnboardingTemplate>('/onboarding/templates', input);
  return data;
}

export async function publishOnboardingTemplate(template: OnboardingTemplate): Promise<OnboardingTemplate> {
  const { data } = await http.post<OnboardingTemplate>(`/onboarding/templates/${template.id}/publish`, undefined, {
    headers: { 'If-Match': `"${template.version}"` }
  });
  return data;
}

export async function setOnboardingAutoAssign({
  template,
  enabled
}: {
  template: OnboardingTemplate;
  enabled: boolean;
}): Promise<OnboardingTemplate> {
  const { data } = await http.put<OnboardingTemplate>(
    `/onboarding/templates/${template.id}/auto-assign`,
    { enabled },
    { headers: { 'If-Match': `"${template.version}"` } }
  );
  return data;
}

export async function getOnboardingAssignments(): Promise<OnboardingAssignment[]> {
  const { data } = await http.get<OnboardingAssignment[]>('/onboarding/assignments');
  return data;
}

export async function assignOnboarding(input: {
  employeeId: string;
  templateId: string;
  projectId?: string;
  managerEmployeeId?: string;
  mentorEmployeeId?: string;
  startDate: string;
  /** Reading steps to include; omit to include every reading step. */
  readingStepIds?: string[];
}): Promise<OnboardingAssignment> {
  const { data } = await http.post<OnboardingAssignment>('/onboarding/assignments', input);
  return data;
}

export async function updateOnboardingStep(
  assignmentId: string,
  step: OnboardingStep,
  status: OnboardingStepStatus,
  completionNote?: string
): Promise<OnboardingAssignment> {
  const { data } = await http.put<OnboardingAssignment>(
    `/onboarding/assignments/${assignmentId}/steps/${step.id}`,
    { status, completionNote },
    { headers: { 'If-Match': `"${step.version}"` } }
  );
  return data;
}

export async function reviewOnboardingStep(
  assignmentId: string,
  step: OnboardingStep,
  approved: boolean,
  reviewNote?: string
): Promise<OnboardingAssignment> {
  const { data } = await http.post<OnboardingAssignment>(
    `/onboarding/assignments/${assignmentId}/steps/${step.id}/review`,
    { approved, reviewNote },
    { headers: { 'If-Match': `"${step.version}"` } }
  );
  return data;
}

export async function updateReadingProgress(
  assignmentId: string,
  item: OnboardingReadingItem,
  status: OnboardingReadingStatus
): Promise<OnboardingAssignment> {
  const { data } = await http.put<OnboardingAssignment>(
    `/onboarding/assignments/${assignmentId}/reading/${item.stepProgressId}`,
    { status },
    { headers: { 'If-Match': `"${item.version}"` } }
  );
  return data;
}

export async function startQuizAttempt(assignmentId: string): Promise<QuizAttempt> {
  const { data } = await http.post<QuizAttempt>(`/onboarding/assignments/${assignmentId}/quiz-attempts`);
  return data;
}

export async function getQuizAttempt(attemptId: string): Promise<QuizAttempt> {
  const { data } = await http.get<QuizAttempt>(`/onboarding/quiz-attempts/${attemptId}`);
  return data;
}

export async function submitQuizAttempt(attempt: QuizAttempt, answers: Record<string, string>): Promise<QuizResult> {
  const { data } = await http.post<QuizResult>(
    `/onboarding/quiz-attempts/${attempt.id}/submit`,
    {
      answers: attempt.questions.map((question) => ({
        questionId: question.id,
        selectedOptionId: answers[question.id]
      }))
    },
    { headers: { 'If-Match': `"${attempt.version}"` } }
  );
  return data;
}

export async function getOnboardingQuiz(templateId: string): Promise<OnboardingQuizAdmin | null> {
  const response = await http.get<OnboardingQuizAdmin>(`/onboarding/templates/${templateId}/quiz`);
  return response.status === 204 ? null : response.data;
}

export async function createOnboardingQuiz(
  templateId: string,
  input: {
    title: string;
    passingPercentage: number;
    questionCount: number;
    randomizeQuestions: boolean;
    randomizeOptions: boolean;
  }
): Promise<OnboardingQuizAdmin> {
  const { data } = await http.post<OnboardingQuizAdmin>(`/onboarding/templates/${templateId}/quiz`, input);
  return data;
}

export async function generateOnboardingQuiz(quizId: string): Promise<OnboardingQuizAdmin> {
  const { data } = await http.post<OnboardingQuizAdmin>(`/onboarding/quizzes/${quizId}/generate`);
  return data;
}

export async function publishOnboardingQuiz(quiz: OnboardingQuizAdmin): Promise<OnboardingQuizAdmin> {
  const { data } = await http.post<OnboardingQuizAdmin>(`/onboarding/quizzes/${quiz.id}/publish`, undefined, {
    headers: { 'If-Match': `"${quiz.version}"` }
  });
  return data;
}

export async function getOnboardingQuizSources(templateId: string): Promise<OnboardingQuizSource[]> {
  const { data } = await http.get<OnboardingQuizSource[]>(`/onboarding/templates/${templateId}/quiz-sources`);
  return data;
}

export async function addOnboardingQuizQuestion(
  quizId: string,
  question: QuizQuestionInput
): Promise<OnboardingQuizAdmin> {
  const { data } = await http.post<OnboardingQuizAdmin>(`/onboarding/quizzes/${quizId}/questions`, question);
  return data;
}

export async function updateOnboardingQuizQuestion(
  quizId: string,
  questionId: string,
  version: number,
  question: QuizQuestionInput
): Promise<OnboardingQuizAdmin> {
  const { data } = await http.put<OnboardingQuizAdmin>(
    `/onboarding/quizzes/${quizId}/questions/${questionId}`,
    question,
    { headers: { 'If-Match': `"${version}"` } }
  );
  return data;
}

export async function deleteOnboardingQuizQuestion(
  quizId: string,
  questionId: string,
  version: number
): Promise<OnboardingQuizAdmin> {
  const { data } = await http.delete<OnboardingQuizAdmin>(`/onboarding/quizzes/${quizId}/questions/${questionId}`, {
    headers: { 'If-Match': `"${version}"` }
  });
  return data;
}

export async function getOnboardingInsights(): Promise<OnboardingInsights> {
  const { data } = await http.get<OnboardingInsights>('/onboarding/insights');
  return data;
}

export async function getAssignmentOptions(
  templateId: string,
  projectId?: string
): Promise<OnboardingAssignmentOptions> {
  const { data } = await http.get<OnboardingAssignmentOptions>(
    `/onboarding/templates/${templateId}/assignment-options`,
    {
      params: projectId ? { projectId } : undefined
    }
  );
  return data;
}
