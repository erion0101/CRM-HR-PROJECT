import { useQuery } from '@tanstack/react-query';

import { getAiCapabilities, getAiConversation, getAiConversations, getAiSettings, getAiUsage } from '../api/ai-api';

export const aiKeys = { all: ['ai'] as const, capabilities: ['ai', 'capabilities'] as const, conversations: ['ai', 'conversations'] as const };
export const useAiCapabilities = () => useQuery({ queryKey: aiKeys.capabilities, queryFn: getAiCapabilities, staleTime: 60_000, retry: false });
export const useAiConversations = () => useQuery({ queryKey: aiKeys.conversations, queryFn: getAiConversations });
export const useAiConversation = (id?: string) => useQuery({ queryKey: [...aiKeys.conversations, id], queryFn: () => getAiConversation(id!), enabled: Boolean(id) });
export const useAiSettings = () => useQuery({ queryKey: ['ai', 'settings'], queryFn: getAiSettings });
export const useAiUsage = () => useQuery({ queryKey: ['ai', 'usage'], queryFn: getAiUsage });
