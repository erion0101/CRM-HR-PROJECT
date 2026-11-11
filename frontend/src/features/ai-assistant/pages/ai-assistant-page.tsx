import { Bot, MessageSquarePlus, ShieldCheck, Sparkles, Trash2 } from 'lucide-react';
import { Link, useNavigate, useParams } from 'react-router';

import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import { AiComposer, AiWelcome, InlineError, Message, StreamingMessage } from '../components/ai-chat-parts';
import { useAiChat } from '../hooks/use-ai-chat';

/** The assistant as a full page with the conversation history on the left. The header button opens the same chat as a side panel. */
export function AiAssistantPage() {
  const { conversationId } = useParams();
  const navigate = useNavigate();
  const chat = useAiChat(conversationId, (id) => navigate(id ? `/ai-assistant/${id}` : '/ai-assistant'));
  const { capabilities, conversations, conversation, send, create, remove } = chat;

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
            prompts={capabilities.data?.suggestedPrompts ?? []}
            prompt={chat.prompt}
            loading={create.isPending || send.isPending}
            onPrompt={(value) => create.mutate(value)}
            onPromptChange={chat.setPrompt}
            onSubmit={chat.submit}
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
      </section>
    </div>
  );
}
