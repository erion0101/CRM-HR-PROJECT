import type { Project } from '@/features/projects/types';

export type OnboardingStepType =
  | 'ReadArticle'
  | 'ReadDocument'
  | 'ExternalResource'
  | 'PracticalTask'
  | 'PolicyAcknowledgment'
  | 'ManagerReviewedMilestone';
export type OnboardingStepStatus = 'NotStarted' | 'InProgress' | 'Blocked' | 'PendingReview' | 'Completed';

export interface OnboardingStep {
  id: string;
  sectionTitle: string;
  sectionOrder: number;
  title: string;
  instructions: string;
  expectedOutcome: string;
  stepType: OnboardingStepType;
  stepOrder: number;
  isRequired: boolean;
  dueDate: string;
  isOverdue: boolean;
  knowledgeArticleId?: string;
  documentId?: string;
  externalUrl?: string;
  status: OnboardingStepStatus;
  completionNote?: string;
  completedAtUtc?: string;
  reviewNote?: string;
  version: number;
}

export interface OnboardingAssignment {
  id: string;
  employeeId: string;
  employeeName: string;
  templateId: string;
  templateName: string;
  templateVersion: number;
  projectId?: string;
  projectName?: string;
  /** Project package included automatically alongside the main package. */
  projectTemplateId?: string;
  projectTemplateName?: string;
  managerEmployeeId?: string;
  managerName?: string;
  mentorEmployeeId?: string;
  mentorName?: string;
  startDate: string;
  status: string;
  progressPercent: number;
  completedAtUtc?: string;
  steps: OnboardingStep[];
  assessment: OnboardingAssessment;
  version: number;
}

export type OnboardingReadingStatus = 'Unread' | 'InProgress' | 'Read';
export type OnboardingAssessmentStatus = 'NotConfigured' | 'Locked' | 'Available' | 'InProgress' | 'Passed';
export type QuizAttemptStatus = 'InProgress' | 'Passed' | 'Failed';

export interface OnboardingReadingItem {
  id: string;
  stepProgressId: string;
  sectionTitle: string;
  title: string;
  isRequired: boolean;
  documentId?: string;
  knowledgeArticleId?: string;
  status: OnboardingReadingStatus;
  startedAtUtc?: string;
  readAtUtc?: string;
  version: number;
  /** Minutes the employee must spend after first opening the item. */
  minimumReadingMinutes?: number;
  /** When "Mark as read" becomes available; set once the item has been opened. */
  readableAtUtc?: string;
}

export interface QuizAttemptSummary {
  id: string;
  attemptNumber: number;
  readingCycleNumber: number;
  status: QuizAttemptStatus;
  scorePercentage?: number;
  passingPercentage: number;
  startedAtUtc: string;
  submittedAtUtc?: string;
}

export interface OnboardingAssessment {
  status: OnboardingAssessmentStatus;
  readingCycleNumber: number;
  requiredDocuments: number;
  readDocuments: number;
  readingProgressPercent: number;
  quizId?: string;
  quizTitle?: string;
  passingPercentage?: number;
  readingItems: OnboardingReadingItem[];
  attempts: QuizAttemptSummary[];
}

export interface QuizOptionAdmin {
  id: string;
  text: string;
  isCorrect: boolean;
  sortOrder: number;
}
export interface QuizQuestionAdmin {
  id: string;
  questionText: string;
  explanation: string;
  topic: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';
  sortOrder: number;
  sourceDocumentId?: string;
  sourceDocumentVersionId?: string;
  sourceKnowledgeRevisionId?: string;
  sourceLabel: string;
  sourcePage?: number;
  sourceParagraph?: number;
  options: QuizOptionAdmin[];
  version: number;
}
export interface OnboardingQuizAdmin {
  id: string;
  templateVersionId: string;
  quizVersion: number;
  title: string;
  status: 'Draft' | 'Published' | 'Retired';
  generationSource: 'Manual' | 'AiDraft';
  passingPercentage: number;
  questionCount: number;
  randomizeQuestions: boolean;
  randomizeOptions: boolean;
  publishedAtUtc?: string;
  questions: QuizQuestionAdmin[];
  version: number;
  /** Background AI generation; the builder polls while Queued or Running. */
  generationStatus: 'None' | 'Queued' | 'Running' | 'Completed' | 'Failed';
  generationError?: string;
}
export interface OnboardingQuizSource {
  documentId?: string;
  documentVersionId?: string;
  knowledgeRevisionId?: string;
  label: string;
  sectionTitle: string;
}
export interface QuizQuestionInput {
  questionText: string;
  explanation: string;
  topic: string;
  difficulty: 'Easy' | 'Medium' | 'Hard';
  sortOrder: number;
  sourceDocumentId?: string;
  sourceDocumentVersionId?: string;
  sourceKnowledgeRevisionId?: string;
  sourceLabel: string;
  sourcePage?: number;
  sourceParagraph?: number;
  options: { text: string; isCorrect: boolean; sortOrder: number }[];
}
export interface QuizQuestion {
  id: string;
  questionText: string;
  number: number;
  options: { id: string; text: string }[];
}
export interface QuizAttempt {
  id: string;
  assignmentId: string;
  attemptNumber: number;
  readingCycleNumber: number;
  status: QuizAttemptStatus;
  passingPercentage: number;
  scorePercentage?: number;
  correctAnswers?: number;
  totalQuestions?: number;
  startedAtUtc: string;
  submittedAtUtc?: string;
  questions: QuizQuestion[];
  version: number;
}
export interface QuizResult {
  attemptId: string;
  status: QuizAttemptStatus;
  scorePercentage: number;
  passingPercentage: number;
  correctAnswers: number;
  totalQuestions: number;
  readingCycleNumber: number;
  readingReset: boolean;
  message: string;
  /** Populated only after a pass; a failed attempt never reveals the answer key. */
  answerReview: QuizAnswerReview[];
  documentScores: QuizDocumentScore[];
  assignment: OnboardingAssignment;
}
export interface QuizDocumentScore {
  sourceLabel: string;
  correctAnswers: number;
  totalQuestions: number;
}
export interface QuizAnswerReview {
  questionId: string;
  questionText: string;
  selectedOptionId: string;
  selectedOptionText: string;
  correctOptionId: string;
  correctOptionText: string;
  isCorrect: boolean;
  explanation: string;
  sourceLabel: string;
  sourcePage?: number;
  sourceParagraph?: number;
}

export interface OnboardingDashboard {
  employeeId: string;
  employeeName: string;
  position?: string;
  department?: string;
  projects: Project[];
  journeys: OnboardingAssignment[];
}

export interface OnboardingTemplateStepInput {
  sectionTitle: string;
  sectionOrder: number;
  title: string;
  instructions: string;
  expectedOutcome: string;
  stepType: OnboardingStepType;
  stepOrder: number;
  dueAfterDays: number;
  isRequired: boolean;
  knowledgeArticleId?: string;
  documentId?: string;
  externalUrl?: string;
  dependsOnStepOrder?: number;
  minimumReadingMinutes?: number;
}

/** Company packages go to every new employee; a project package is added automatically for its project. */
export type OnboardingTemplateKind = 'General' | 'Company' | 'Project';

export interface OnboardingTemplate {
  id: string;
  name: string;
  description: string;
  kind: OnboardingTemplateKind;
  projectId?: string;
  /** Assigned automatically when a new employee's employment becomes active. */
  autoAssignToNewEmployees: boolean;
  departmentId?: string;
  positionId?: string;
  durationDays: number;
  status: 'Draft' | 'Published' | 'Retired';
  currentVersion: number;
  publishedVersion?: number;
  steps: (OnboardingTemplateStepInput & { id: string; dependsOnStepId?: string })[];
  version: number;
}

export type OnboardingAttentionReason = 'OverdueSteps' | 'RepeatedQuizFailures' | 'AwaitingReview' | 'NotStarted';

export interface OnboardingAttentionItem {
  assignmentId: string;
  employeeId: string;
  employeeName: string;
  journeyName: string;
  startDate: string;
  overdueSteps: number;
  failedAttempts: number;
  reasons: OnboardingAttentionReason[];
}

export interface OnboardingDocumentDifficulty {
  documentId?: string;
  documentLabel: string;
  answers: number;
  incorrectAnswers: number;
  incorrectRate: number;
}

export interface OnboardingInsights {
  activeJourneys: number;
  completedLast30Days: number;
  overdueJourneys: number;
  awaitingReview: number;
  firstAttemptPassRate?: number | null;
  averageAttemptsToPass?: number | null;
  needsAttention: OnboardingAttentionItem[];
  documentDifficulty: OnboardingDocumentDifficulty[];
}

export interface OnboardingReadingOption {
  templateStepId: string;
  sectionTitle: string;
  title: string;
  isRequired: boolean;
  documentId?: string;
  knowledgeArticleId?: string;
  /** Reviewed quiz questions about this item. */
  questionCount: number;
}

export interface OnboardingPackageOption {
  templateId: string;
  name: string;
  kind: OnboardingTemplateKind;
  hasQuiz: boolean;
  questionsPerAttempt: number;
  readingSteps: OnboardingReadingOption[];
}

export interface OnboardingAssignmentOptions {
  packages: OnboardingPackageOption[];
}
