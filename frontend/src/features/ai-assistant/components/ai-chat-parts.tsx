import { Bot, FileSearch, Send, Sparkles, User } from 'lucide-react';
import type { FormEvent } from 'react';
import { Link } from 'react-router';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { toAppError } from '@/lib/api-error';

import type { AiConversationMessage, AiSource } from '../types';

/** The reply being written, shown while the answer streams in. */
export function StreamingMessage({ text }: { text: string }) {
  return (
    <div className="ai-message ai-message--assistant">
      <span className="ai-avatar">
        <Bot size={17} />
      </span>
      <div className="ai-bubble">
        <small>Assistant</small>
        <p>{text || 'Checking authorized HR records…'}</p>
      </div>
    </div>
  );
}

export function InlineError({ error }: { error: unknown }) {
  return <div className="ai-inline-error">{toAppError(error).message}</div>;
}

export function AiWelcome({
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

export function AiComposer({
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

export function Message({ message }: { message: AiConversationMessage }) {
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
