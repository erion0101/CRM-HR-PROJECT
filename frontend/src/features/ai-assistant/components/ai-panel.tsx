import { ArrowLeft, Bot, History, Maximize2, MessageSquarePlus, ShieldCheck, Trash2, X } from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router';

import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Button } from '@/components/ui/button';

import { useAiChat } from '../hooks/use-ai-chat';
import { AiComposer, AiWelcome, InlineError, Message, StreamingMessage } from './ai-chat-parts';

const shortDate = new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' });

/**
 * The assistant docked on the right of every page, like the AI chat in an IDE. On wide screens the page moves
 * aside to make room; on smaller screens the panel covers the page. Links in answers open the page behind it.
 */
export function AiPanel({ onClose }: { onClose: () => void }) {
  const navigate = useNavigate();
  const [conversationId, setConversationId] = useState<string | undefined>();
  const [showHistory, setShowHistory] = useState(false);
  const chat = useAiChat(conversationId, (id) => {
    setConversationId(id);
    setShowHistory(false);
  });
  const { capabilities, conversations, conversation, send, create, remove } = chat;
  const messagesRef = useRef<HTMLDivElement>(null);

  // Keep the newest message in view while the answer streams in.
  useEffect(() => {
    messagesRef.current?.scrollTo({ top: messagesRef.current.scrollHeight });
  }, [chat.messages.length, chat.streamedText]);

  const openChat = (id: string | undefined) => {
    setConversationId(id);
    setShowHistory(false);
    if (!id) chat.setPrompt('');
  };
  const chatAvailable = Boolean(capabilities.data?.enabled);
  const title = showHistory ? 'Conversations' : (conversation.data?.title ?? 'AI assistant');

  return (
    <aside
      className="ai-panel"
      aria-label="AI assistant"
      onKeyDown={(event) => {
        if (event.key === 'Escape') onClose();
      }}
    >
      <header className="ai-panel__header">
        {showHistory ? (
          <Button variant="ghost" size="small" aria-label="Back to the chat" onClick={() => setShowHistory(false)}>
            <ArrowLeft size={17} />
          </Button>
        ) : (
          <span className="ai-panel__icon" aria-hidden="true">
            <Bot size={17} />
          </span>
        )}
        <h2 title={title}>{title}</h2>
        <div className="ai-panel__actions">
          {chatAvailable ? (
            <>
              {showHistory ? null : (
                <Button
                  variant="ghost"
                  size="small"
                  aria-label="Conversations"
                  title="Conversations"
                  onClick={() => setShowHistory(true)}
                >
                  <History size={17} />
                </Button>
              )}
              <Button
                variant="ghost"
                size="small"
                aria-label="New chat"
                title="New chat"
                onClick={() => openChat(undefined)}
              >
                <MessageSquarePlus size={17} />
              </Button>
            </>
          ) : null}
          <Button
            variant="ghost"
            size="small"
            aria-label="Open as a full page"
            title="Open as a full page"
            onClick={() => {
              navigate(conversationId ? `/ai-assistant/${conversationId}` : '/ai-assistant');
              onClose();
            }}
          >
            <Maximize2 size={16} />
          </Button>
          <Button variant="ghost" size="small" aria-label="Close AI assistant" title="Close" onClick={onClose}>
            <X size={18} />
          </Button>
        </div>
      </header>

      {capabilities.isLoading ? (
        <div className="ai-panel__body">
          <Skeleton count={4} />
        </div>
      ) : capabilities.isError ? (
        <ErrorState title="AI assistant status is unavailable" onRetry={() => void capabilities.refetch()} />
      ) : !capabilities.data?.enabled ? (
        <div className="ai-panel__body">
          <div className="ai-disabled">
            <ShieldCheck size={28} />
            <h2>Assistant is not available yet</h2>
            <p>{capabilities.data?.disabledReason}</p>
            <small>Everything else in the app works normally.</small>
          </div>
        </div>
      ) : showHistory ? (
        <div className="ai-panel__history">
          {conversations.data?.length ? (
            conversations.data.map((item) => (
              <div className={`ai-panel__history-row ${item.id === conversationId ? 'active' : ''}`} key={item.id}>
                <button type="button" onClick={() => openChat(item.id)}>
                  <strong>{item.title}</strong>
                  <small>{shortDate.format(new Date(item.updatedAtUtc))}</small>
                </button>
                <Button
                  variant="ghost"
                  size="small"
                  aria-label={`Delete ${item.title}`}
                  loading={remove.isPending && remove.variables === item.id}
                  onClick={() => remove.mutate(item.id)}
                >
                  <Trash2 size={15} />
                </Button>
              </div>
            ))
          ) : (
            <p className="ai-panel__empty">No conversations yet.</p>
          )}
        </div>
      ) : !conversationId ? (
        <div className="ai-panel__body">
          <AiWelcome
            prompts={capabilities.data?.suggestedPrompts ?? []}
            prompt={chat.prompt}
            loading={create.isPending || send.isPending}
            onPrompt={(value) => create.mutate(value)}
            onPromptChange={chat.setPrompt}
            onSubmit={chat.submit}
          />
        </div>
      ) : conversation.isLoading ? (
        <div className="ai-panel__body">
          <Skeleton count={4} />
        </div>
      ) : conversation.isError ? (
        <ErrorState title="Conversation could not be loaded" onRetry={() => void conversation.refetch()} />
      ) : (
        <>
          <div className="ai-chat__messages" ref={messagesRef}>
            {chat.messages.map((message) => (
              <Message key={message.id} message={message} />
            ))}
            {send.isPending ? <StreamingMessage text={chat.streamedText || chat.streamStatus} /> : null}
            {send.isError ? <InlineError error={send.error} /> : null}
          </div>
          <AiComposer
            prompt={chat.prompt}
            loading={send.isPending}
            onPromptChange={chat.setPrompt}
            onSubmit={chat.submit}
            onCancel={chat.cancel}
          />
        </>
      )}
    </aside>
  );
}
