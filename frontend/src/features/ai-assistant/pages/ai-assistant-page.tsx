import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Bot, FileSearch, MessageSquarePlus, Send, ShieldCheck, Sparkles, Trash2, User } from 'lucide-react';
import { useEffect, useMemo, useRef, useState, type FormEvent } from 'react';
import { Link, useNavigate, useParams } from 'react-router';

import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { toAppError } from '@/lib/api-error';

import { createAiConversation, deleteAiConversation, streamAiMessage } from '../api/ai-api';
import { aiKeys, useAiCapabilities, useAiConversation, useAiConversations } from '../hooks/use-ai';
import type { AiConversationMessage, AiSource } from '../types';

export function AiAssistantPage() {
  const { conversationId } = useParams();
  const navigate = useNavigate();
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
      navigate(`/ai-assistant/${created.id}`);
      if (initialMessage) send.mutate({ id: created.id, message: initialMessage });
    }
  });
  const remove = useMutation({
    mutationFn: deleteAiConversation,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: aiKeys.conversations });
      navigate('/ai-assistant');
    }
  });

  useEffect(() => () => abortRef.current?.abort(), []);
  const suggested = capabilities.data?.suggestedPrompts ?? [];
  const messages = useMemo(() => conversation.data?.messages ?? [], [conversation.data]);
  const submit = (event: FormEvent) => {
    event.preventDefault();
    const value = prompt.trim();
    if (!value || send.isPending || create.isPending) return;
    if (conversationId) send.mutate({ id: conversationId, message: value });
    else create.mutate(value);
  };

  if (capabilities.isLoading)
    return (
      <div className="route-loader">
        <Skeleton count={5} />
      </div>
    );
  if (capabilities.isError)
    return <ErrorState title="AI assistant status is unavailable" onRetry={() => void capabilities.refetch()} />;

  return (
    <div className="ai-workspace">
      <aside className="ai-history">
        <div className="ai-history__header">
          <div>
            <Sparkles size={18} />
            <strong>HR Assistant</strong>
          </div>
          <Button size="small" onClick={() => create.mutate(undefined)} loading={create.isPending}>
            <MessageSquarePlus size={15} /> New
          </Button>
        </div>
        <div className="ai-history__list">
          {conversations.data?.map((item) => (
            <Link key={item.id} to={`/ai-assistant/${item.id}`} className={item.id === conversationId ? 'active' : ''}>
              <strong>{item.title}</strong>
              <small>
                {new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(
                  new Date(item.updatedAtUtc)
                )}
              </small>
            </Link>
          ))}
          {!conversations.data?.length ? <p>No conversations yet.</p> : null}
        </div>
        <div className="ai-privacy">
          <ShieldCheck size={17} />
          <span>
            <strong>Read-only and permission scoped</strong>
            <small>No salary, bank, medical, or restricted data.</small>
          </span>
        </div>
      </aside>
      <section className="ai-chat">
        <header className="ai-chat__header">
          <div>
            <span>
              <Bot size={20} />
            </span>
            <div>
              <h1>{conversation.data?.title ?? 'AI HR Assistant'}</h1>
              <p>Answers use only HR records and approved documents you can access.</p>
            </div>
          </div>
          {conversationId ? (
            <Button
              variant="ghost"
              size="small"
              aria-label="Delete conversation"
              onClick={() => remove.mutate(conversationId)}
            >
              <Trash2 size={16} />
            </Button>
          ) : null}
        </header>
        {!capabilities.data?.enabled ? (
          <Card className="ai-disabled">
            <ShieldCheck size={28} />
            <h2>Assistant is not available yet</h2>
            <p>{capabilities.data?.disabledReason}</p>
            <small>Core employee and document features continue to work normally.</small>
          </Card>
        ) : !conversationId ? (
          <AiWelcome
            prompts={suggested}
            prompt={prompt}
            loading={create.isPending || send.isPending}
            onPrompt={(value) => create.mutate(value)}
            onPromptChange={setPrompt}
            onSubmit={submit}
          />
        ) : conversation.isLoading ? (
          <div className="ai-chat__messages">
            <Skeleton count={5} />
          </div>
        ) : conversation.isError ? (
          <ErrorState title="Conversation could not be loaded" onRetry={() => void conversation.refetch()} />
        ) : (
          <>
            <div className="ai-chat__messages">
              {messages.map((message) => (
                <Message key={message.id} message={message} />
              ))}
              {send.isPending ? (
                <div className="ai-message ai-message--assistant">
                  <span className="ai-avatar">
                    <Bot size={17} />
                  </span>
                  <div className="ai-bubble">
                    <small>Assistant</small>
                    <p>{streamedText || streamStatus || 'Checking authorized HR records…'}</p>
                  </div>
                </div>
              ) : null}
              {send.isError ? <div className="ai-inline-error">{toAppError(send.error).message}</div> : null}
            </div>
            <AiComposer
              prompt={prompt}
              loading={send.isPending}
              onPromptChange={setPrompt}
              onSubmit={submit}
              onCancel={() => abortRef.current?.abort()}
            />
          </>
        )}
      </section>
    </div>
  );
}

function AiWelcome({
  prompts,
  prompt,
  loading,
  onPrompt,
  onPromptChange,
  onSubmit
}: {
  prompts: string[];
  prompt: string;
  loading: boolean;
  onPrompt: (value: string) => void;
  onPromptChange: (value: string) => void;
  onSubmit: (event: FormEvent) => void;
}) {
  return (
    <div className="ai-welcome">
      <span>
        <Sparkles size={28} />
      </span>
      <h2>Ask your HR workspace</h2>
      <p>I can search authorized structured records and HR-approved document content. I cannot modify records.</p>
      <AiComposer prompt={prompt} loading={loading} onPromptChange={onPromptChange} onSubmit={onSubmit} />
      {prompts.length ? (
        <div className="ai-welcome__prompts">
          {prompts.map((suggestion) => (
            <button key={suggestion} type="button" onClick={() => onPrompt(suggestion)} disabled={loading}>
              <FileSearch size={16} />
              {suggestion}
            </button>
          ))}
        </div>
      ) : null}
    </div>
  );
}

function AiComposer({
  prompt,
  loading,
  onPromptChange,
  onSubmit,
  onCancel
}: {
  prompt: string;
  loading: boolean;
  onPromptChange: (value: string) => void;
  onSubmit: (event: FormEvent) => void;
  onCancel?: () => void;
}) {
  return (
    <form className="ai-composer" onSubmit={onSubmit}>
      <textarea
        value={prompt}
        onChange={(event) => onPromptChange(event.target.value)}
        onKeyDown={(event) => {
          if (event.key === 'Enter' && !event.shiftKey) {
            event.preventDefault();
            event.currentTarget.form?.requestSubmit();
          }
        }}
        placeholder="Ask about employees, organization structure, contracts, or approved documents…"
        rows={2}
        disabled={loading}
        autoFocus
      />
      {loading && onCancel ? (
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
      ) : (
        <Button type="submit" disabled={!prompt.trim() || loading}>
          <Send size={16} /> Send
        </Button>
      )}
      <small>
        Press Enter to send · Shift + Enter for a new line. Verify important HR decisions against cited records.
      </small>
    </form>
  );
}

function Message({ message }: { message: AiConversationMessage }) {
  const isUser = message.role === 'User';
  return (
    <div className={`ai-message ai-message--${isUser ? 'user' : 'assistant'}`}>
      <span className="ai-avatar">{isUser ? <User size={17} /> : <Bot size={17} />}</span>
      <div className="ai-bubble">
        <small>{isUser ? 'You' : 'Assistant'}</small>
        <p>{message.content}</p>
        {!isUser ? (
          <>
            <Result result={message.result} />
            <Evidence value={message.evidenceStatus} />
            <Sources sources={message.sources} />
          </>
        ) : null}
      </div>
    </div>
  );
}

function Result({ result }: { result?: unknown }) {
  if (!result || typeof result !== 'object') return null;
  const candidate = result as { items?: unknown };
  const items = Array.isArray(candidate.items) ? candidate.items : Array.isArray(result) ? result : [];
  if (!items.length) return null;
  return (
    <div className="ai-results">
      {items.slice(0, 20).map((item, index) => (
        <div key={index}>
          {typeof item === 'object' && item ? (
            Object.entries(item as Record<string, unknown>)
              .slice(0, 5)
              .map(([key, value]) => (
                <span key={key}>
                  <small>{humanize(key)}</small>
                  <strong>{String(value ?? '—')}</strong>
                </span>
              ))
          ) : (
            <strong>{String(item)}</strong>
          )}
        </div>
      ))}
    </div>
  );
}

function Evidence({ value }: { value: string }) {
  const labels: Record<string, string> = {
    VerifiedStructured: 'Structured record',
    DocumentDerived: 'Document-derived',
    Mixed: 'Mixed evidence',
    Conflict: 'Conflict',
    NotFound: 'No evidence'
  };
  return (
    <Badge tone={value === 'Conflict' ? 'warning' : value === 'NotFound' ? 'neutral' : 'info'}>
      {labels[value] ?? value}
    </Badge>
  );
}

function Sources({ sources }: { sources: AiSource[] }) {
  if (!sources.length) return null;
  return (
    <details className="ai-sources">
      <summary>
        {sources.length} source{sources.length === 1 ? '' : 's'}
      </summary>
      {sources.map((source) => (
        <div key={source.id}>
          {source.url ? <Link to={source.url}>{source.label}</Link> : <strong>{source.label}</strong>}
          <small>
            {source.kind}
            {source.location ? ` · ${source.location}` : ''}
          </small>
        </div>
      ))}
    </details>
  );
}

const humanize = (value: string) =>
  value.replace(/([A-Z])/g, ' $1').replace(/^./, (character) => character.toUpperCase());
