import { BookOpen, Check, CheckCircle2, FileText } from 'lucide-react';
import { Link } from 'react-router';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import type { OnboardingReadingItem, OnboardingReadingStatus } from '../types';

type ReadingHandler = (item: OnboardingReadingItem, status: OnboardingReadingStatus) => void;

export function ReadingModule({
  assignmentId,
  section,
  items,
  busy,
  onReading
}: {
  assignmentId: string;
  section: string;
  items: OnboardingReadingItem[];
  busy: boolean;
  onReading: ReadingHandler;
}) {
  const readCount = items.filter((item) => item.status === 'Read').length;
  return (
    <Card className="reading-module">
      <header>
        <div>
          <small>Required module</small>
          <h3>{section}</h3>
        </div>
        <span>
          {readCount}/{items.length} read
        </span>
      </header>
      {items.map((item) => (
        <ReadingItem key={item.id} assignmentId={assignmentId} item={item} busy={busy} onReading={onReading} />
      ))}
    </Card>
  );
}

function ReadingItem({
  assignmentId,
  item,
  busy,
  onReading
}: {
  assignmentId: string;
  item: OnboardingReadingItem;
  busy: boolean;
  onReading: ReadingHandler;
}) {
  const isRead = item.status === 'Read';
  const hasSource = Boolean(item.documentId || item.knowledgeArticleId);
  // The backend rejects "Mark as read" for an item that was never opened; mirror that here.
  const mustOpenFirst = hasSource && item.status === 'Unread';
  const markOpened = () => {
    if (item.status === 'Unread') onReading(item, 'InProgress');
  };

  return (
    <div className={`reading-item ${isRead ? 'is-read' : ''}`}>
      <span>{isRead ? <CheckCircle2 size={20} /> : <FileText size={20} />}</span>
      <div>
        <strong>{item.title}</strong>
        <small>
          {item.isRequired ? 'Required' : 'Optional'} · {mustOpenFirst ? 'Open to start reading' : item.status}
        </small>
      </div>
      <div className="row-actions">
        {item.knowledgeArticleId ? (
          <Link
            className="button button--secondary button--small"
            to={`/knowledge/articles/${item.knowledgeArticleId}`}
            onClick={markOpened}
          >
            <BookOpen size={14} /> Read
          </Link>
        ) : null}
        {item.documentId ? (
          <a
            className="button button--secondary button--small"
            href={`/api/v1/onboarding/assignments/${assignmentId}/reading/${item.stepProgressId}/download`}
            target="_blank"
            rel="noreferrer"
            onClick={markOpened}
          >
            <FileText size={14} /> Open
          </a>
        ) : null}
        {isRead ? (
          <Badge tone="positive">Read</Badge>
        ) : (
          <Button
            size="small"
            disabled={busy || mustOpenFirst}
            title={mustOpenFirst ? 'Open the document before marking it as read' : undefined}
            onClick={() => onReading(item, 'Read')}
          >
            <Check size={14} /> Mark as read
          </Button>
        )}
      </div>
    </div>
  );
}
