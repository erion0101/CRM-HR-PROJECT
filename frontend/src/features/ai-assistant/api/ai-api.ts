import { apiBaseUrl, getAntiforgeryToken, http } from '@/lib/api-client';

import type { AiCapabilities, AiConversation, AiMessageResponse, AiSettings, AiUsageSummary } from '../types';

export const getAiCapabilities = async (): Promise<AiCapabilities> => (await http.get<AiCapabilities>('/ai/capabilities')).data;
export const getAiConversations = async (): Promise<AiConversation[]> => (await http.get<AiConversation[]>('/ai/conversations')).data;
export const getAiConversation = async (id: string): Promise<AiConversation> => (await http.get<AiConversation>(`/ai/conversations/${id}`)).data;
export const createAiConversation = async (input: { title?: string; contextType?: string; contextId?: string }): Promise<AiConversation> =>
  (await http.post<AiConversation>('/ai/conversations', input)).data;
export const deleteAiConversation = async (id: string): Promise<void> => { await http.delete(`/ai/conversations/${id}`); };
export const getAiSettings = async (): Promise<AiSettings> => (await http.get<AiSettings>('/administration/ai-settings')).data;
export const getAiUsage = async (): Promise<AiUsageSummary> => (await http.get<AiUsageSummary>('/administration/ai-usage')).data;
export const testAiProvider = async (): Promise<boolean> => (await http.post<{ connected: boolean }>('/administration/ai-settings/test')).data.connected;

export async function streamAiMessage(
  conversationId: string,
  message: string,
  onDelta: (text: string) => void,
  onStatus?: (message: string) => void,
  signal?: AbortSignal,
): Promise<AiMessageResponse> {
  const response = await fetch(`${apiBaseUrl}/ai/conversations/${conversationId}/messages`, {
    method: 'POST',
    credentials: 'include',
    signal,
    headers: { 'Content-Type': 'application/json', Accept: 'text/event-stream', 'X-CSRF-TOKEN': await getAntiforgeryToken() },
    body: JSON.stringify({ message }),
  });
  if (!response.ok || !response.body) throw new Error(`AI request failed (${response.status}).`);
  const reader = response.body.getReader();
  const decoder = new TextDecoder();
  let buffer = '';
  let completed: AiMessageResponse | undefined;
  while (true) {
    const { value, done } = await reader.read();
    if (done) break;
    buffer += decoder.decode(value, { stream: true });
    const events = buffer.split('\n\n');
    buffer = events.pop() ?? '';
    for (const event of events) {
      const eventName = event.split('\n').find((line) => line.startsWith('event: '))?.slice(7);
      const raw = event.split('\n').find((line) => line.startsWith('data: '))?.slice(6);
      if (!raw) continue;
      const payload = JSON.parse(raw) as unknown;
      if (eventName === 'delta' && payload && typeof payload === 'object' && 'text' in payload) onDelta(String(payload.text));
      if (eventName === 'status' && payload && typeof payload === 'object' && 'message' in payload) onStatus?.(String(payload.message));
      if (eventName === 'done') completed = payload as AiMessageResponse;
      if (eventName === 'error') throw new Error(payload && typeof payload === 'object' && 'message' in payload ? String(payload.message) : 'The AI request could not be completed.');
    }
  }
  if (!completed) throw new Error('The AI stream ended before completion.');
  return completed;
}
