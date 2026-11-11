import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';

import { createAiConversation, deleteAiConversation, streamAiMessage } from '../api/ai-api';
import { aiKeys, useAiCapabilities, useAiConversation, useAiConversations } from './use-ai';

/**
 * Everything a chat view needs: the conversation list, the open conversation, sending with streaming,
 * starting and deleting conversations. `onOpen` is told which conversation to show (undefined = a new chat),
 * so the full page can use the URL and the side panel can use its own state.
 */
export function useAiChat(conversationId: string | undefined, onOpen: (id: string | undefined) => void) {
  const queryClient = useQueryClient();
  const capabilities = useAiCapabilities();
  const conversations = useAiConversations();
  const conversation = useAiConversation(conversationId);
  const [prompt, setPrompt] = useState('');
  const [streamedText, setStreamedText] = useState('');
  const [streamStatus, setStreamStatus] = useState('');
  const abortRef = useRef<AbortController | undefined>(undefined);

  const send = useMutation({
    mutationFn: async ({ id, message }: { id: string; message: string }) => {
      const controller = new AbortController();
      abortRef.current = controller;
      setStreamedText('');
      setStreamStatus('Starting secure AI request…');
      return streamAiMessage(
        id,
        message,
        (text) => setStreamedText((current) => current + text),
        setStreamStatus,
        controller.signal
      );
    },
    onSuccess: async (_, variables) => {
      setPrompt('');
      setStreamedText('');
      setStreamStatus('');
      await queryClient.invalidateQueries({ queryKey: [...aiKeys.conversations, variables.id] });
      await queryClient.invalidateQueries({ queryKey: aiKeys.conversations });
    }
  });
  const create = useMutation({
    mutationFn: (initialMessage?: string) => createAiConversation({}).then((created) => ({ created, initialMessage })),
    onSuccess: async ({ created, initialMessage }) => {
      await queryClient.invalidateQueries({ queryKey: aiKeys.conversations });
      onOpen(created.id);
      if (initialMessage) send.mutate({ id: created.id, message: initialMessage });
    }
  });
  const remove = useMutation({
    mutationFn: deleteAiConversation,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: aiKeys.conversations });
      onOpen(undefined);
    }
  });

  useEffect(() => () => abortRef.current?.abort(), []);
  const messages = useMemo(() => conversation.data?.messages ?? [], [conversation.data]);
  const submit = (event: FormEvent) => {
    event.preventDefault();
    const value = prompt.trim();
    if (!value || send.isPending || create.isPending) return;
    if (conversationId) send.mutate({ id: conversationId, message: value });
    else create.mutate(value);
  };

  return {
    capabilities,
    conversations,
    conversation,
    messages,
    prompt,
    setPrompt,
    streamedText,
    streamStatus,
    send,
    create,
    remove,
    submit,
    cancel: () => abortRef.current?.abort()
  };
}
