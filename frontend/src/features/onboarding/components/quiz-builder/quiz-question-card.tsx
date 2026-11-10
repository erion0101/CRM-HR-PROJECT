import { Pencil, Trash2 } from 'lucide-react';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import type { QuizQuestionAdmin } from '../../types';

export function QuizQuestionCard({
  question,
  number,
  editable,
  deleting,
  onEdit,
  onDelete
}: {
  question: QuizQuestionAdmin;
  number: number;
  editable: boolean;
  deleting: boolean;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <Card>
      <header>
        <span>
          Question {number} · {question.topic}
        </span>
        <div className="row-actions">
          <Badge tone="neutral">{question.difficulty}</Badge>
          {editable ? (
            <>
              <Button size="small" variant="ghost" onClick={onEdit}>
                <Pencil size={14} /> Edit
              </Button>
              <Button size="small" variant="ghost" loading={deleting} onClick={onDelete}>
                <Trash2 size={14} /> Remove
              </Button>
            </>
          ) : null}
        </div>
      </header>
      <h3>{question.questionText}</h3>
      <ol>
        {question.options.map((option) => (
          <li className={option.isCorrect ? 'is-correct' : ''} key={option.id}>
            {option.text}
            {option.isCorrect ? ' — Correct' : ''}
          </li>
        ))}
      </ol>
      <p>
        <strong>Explanation:</strong> {question.explanation}
      </p>
      <small>
        Source: {question.sourceLabel}
        {question.sourcePage ? ` · page ${question.sourcePage}` : ''}
      </small>
    </Card>
  );
}
