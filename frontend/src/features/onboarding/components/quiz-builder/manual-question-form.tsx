import { useState } from 'react';
import { useForm } from 'react-hook-form';

import { FieldShell, Input, Select, Textarea } from '@/components/forms/field';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import type { OnboardingQuizAdmin, OnboardingQuizSource, QuizQuestionAdmin, QuizQuestionInput } from '../../types';

type Difficulty = QuizQuestionAdmin['difficulty'];

interface ManualQuestionValues {
  question: string;
  explanation: string;
  topic: string;
  difficulty: Difficulty;
  correct: number;
  options: string[];
}

const optionIndexes = [0, 1, 2, 3] as const;

export function ManualQuestionForm({
  quiz,
  question,
  sources,
  loading,
  onCancel,
  onSave
}: {
  quiz: OnboardingQuizAdmin;
  question?: QuizQuestionAdmin;
  sources: OnboardingQuizSource[];
  loading: boolean;
  onCancel: () => void;
  onSave: (input: QuizQuestionInput) => void;
}) {
  const initialSourceIndex = Math.max(
    0,
    sources.findIndex(
      (source) =>
        source.documentVersionId === question?.sourceDocumentVersionId &&
        source.knowledgeRevisionId === question?.sourceKnowledgeRevisionId
    )
  );
  const [sourceIndex, setSourceIndex] = useState(initialSourceIndex);
  const form = useForm<ManualQuestionValues>({
    defaultValues: {
      question: question?.questionText ?? '',
      explanation: question?.explanation ?? '',
      topic: question?.topic ?? '',
      difficulty: question?.difficulty ?? 'Medium',
      correct: Math.max(0, question?.options.findIndex((option) => option.isCorrect) ?? 0),
      options: question?.options.map((option) => option.text) ?? ['', '', '', '']
    }
  });
  const source = sources[sourceIndex];

  const submit = form.handleSubmit((value) => {
    if (!source) return;
    onSave({
      questionText: value.question,
      explanation: value.explanation,
      topic: value.topic,
      difficulty: value.difficulty,
      sortOrder: question?.sortOrder ?? quiz.questions.length,
      sourceDocumentId: source.documentId,
      sourceDocumentVersionId: source.documentVersionId,
      sourceKnowledgeRevisionId: source.knowledgeRevisionId,
      sourceLabel: source.label,
      sourcePage: question?.sourcePage,
      sourceParagraph: question?.sourceParagraph,
      options: value.options.map((text, index) => ({
        text,
        isCorrect: index === Number(value.correct),
        sortOrder: index
      }))
    });
  });

  return (
    <Card className="manual-question">
      <div className="section-heading">
        <div>
          <h3>{question ? 'Edit reviewed question' : 'Add reviewed question'}</h3>
          <p>Manual review and authoring remain available when AI is disabled.</p>
        </div>
      </div>
      <form className="form-stack" onSubmit={submit}>
        <FieldShell label="Source" htmlFor="manualSource" required>
          <Select
            id="manualSource"
            value={sourceIndex}
            onChange={(event) => setSourceIndex(Number(event.target.value))}
          >
            {sources.map((item, index) => (
              <option key={`${item.label}-${index}`} value={index}>
                {item.sectionTitle} · {item.label}
              </option>
            ))}
          </Select>
        </FieldShell>
        <FieldShell label="Question" htmlFor="manualQuestion" required>
          <Textarea id="manualQuestion" {...form.register('question', { required: true })} />
        </FieldShell>
        <div className="form-grid">
          <FieldShell label="Topic" htmlFor="manualTopic" required>
            <Input id="manualTopic" {...form.register('topic', { required: true })} />
          </FieldShell>
          <FieldShell label="Difficulty" htmlFor="manualDifficulty">
            <Select id="manualDifficulty" {...form.register('difficulty')}>
              <option>Easy</option>
              <option>Medium</option>
              <option>Hard</option>
            </Select>
          </FieldShell>
        </div>
        <FieldShell label="Correct-answer explanation" htmlFor="manualExplanation" required>
          <Textarea id="manualExplanation" {...form.register('explanation', { required: true })} />
        </FieldShell>
        {optionIndexes.map((index) => (
          <div className="manual-option" key={index}>
            <input
              type="radio"
              value={index}
              aria-label={`Option ${String.fromCharCode(65 + index)} is correct`}
              {...form.register('correct', { valueAsNumber: true })}
            />
            <FieldShell label={`Option ${String.fromCharCode(65 + index)}`} htmlFor={`manualOption-${index}`} required>
              <Input id={`manualOption-${index}`} {...form.register(`options.${index}`, { required: true })} />
            </FieldShell>
          </div>
        ))}
        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={onCancel}>
            Cancel
          </Button>
          <Button type="submit" loading={loading} disabled={!source}>
            {question ? 'Save changes' : 'Add question'}
          </Button>
        </div>
      </form>
    </Card>
  );
}
