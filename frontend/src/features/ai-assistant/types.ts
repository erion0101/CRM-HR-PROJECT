export type AiResultType = 'Text' | 'EmployeeList' | 'EmployeeDetails' | 'ContractList' | 'DocumentList' | 'OrganizationTree' | 'Clarification' | 'Unavailable' | 'Error';
export type AiEvidenceStatus = 'VerifiedStructured' | 'DocumentDerived' | 'Mixed' | 'Conflict' | 'NotFound';

export interface AiCapabilities {
  enabled: boolean;
  governanceApproved: boolean;
  providerConfigured: boolean;
  capabilities: string[];
  suggestedPrompts: string[];
  disabledReason?: string;
}

export interface AiSource {
  id: string;
  kind: string;
  label: string;
  entityType?: string;
  entityId?: string;
  documentId?: string;
  location?: string;
  url?: string;
}

export interface AiStructuredResult { type: AiResultType; items: unknown }

export interface AiMessageResponse {
  conversationId: string;
  messageId: string;
  status: 'Pending' | 'Completed' | 'Failed';
  message: string;
  result: AiStructuredResult;
  evidenceStatus: AiEvidenceStatus;
  sources: AiSource[];
  suggestedPrompts: string[];
}

export interface AiConversationMessage {
  id: string;
  role: 'User' | 'Assistant';
  status: 'Pending' | 'Completed' | 'Failed';
  content: string;
  resultType: AiResultType;
  result?: AiStructuredResult;
  evidenceStatus: AiEvidenceStatus;
  createdAtUtc: string;
  sources: AiSource[];
}

export interface AiConversation {
  id: string;
  title: string;
  contextType?: string;
  contextId?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  messages: AiConversationMessage[];
}

export interface AiSettings {
  enabled: boolean;
  realDataApproved: boolean;
  providerConfigured: boolean;
  provider: string;
  chatModel: string;
  embeddingModel: string;
  conversationRetentionDays: number;
  requestsPerDayPerUser: number;
  requestsPerDayPerOrganization: number;
  monthlyBudgetUsd: number;
}

export interface AiUsageSummary { requestsToday: number; inputTokensToday: number; outputTokensToday: number; costThisMonthUsd: number }
