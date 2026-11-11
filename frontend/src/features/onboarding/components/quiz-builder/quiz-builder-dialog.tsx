import { useQuery } from '@tanstack/react-query';
import { Brain, Plus } from 'lucide-react';
import { useState } from 'react';

import { EmptyState } from '@/components/feedback/empty-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';

import {
  addOnboardingQuizQuestion,
  createOnboardingQuiz,
  deleteOnboardingQuizQuestion,
  generateOnboardingQuiz,
  getOnboardingQuiz,
  getOnboardingQuizSources,
  publishOnboardingQuiz,
  updateOnboardingQuizQuestion
} from '../../api/onboarding-api';
import { useOnboardingMutation } from '../../hooks/use-onboarding-mutation';
import type { OnboardingQuizAdmin, QuizQuestionAdmin, QuizQuestionInput } from '../../types';
import { ManualQuestionForm } from './manual-question-form';
import { QuizDraftForm, type QuizDraftValues } from './quiz-draft-form';
import { QuizQuestionCard } from './quiz-question-card';

interface QuizTemplate {
  id: string;
  name: string;
}

export function QuizBuilderDialog({ template, onClose }: { template: QuizTemplate | null; onClose: () => void }) {
  return (
    <Dialog
      open={Boolean(template)}
      onClose={onClose}
      title="Quiz builder"
      description={`${template?.name ?? ''} · AI creates drafts only; HR reviews and publishes.`}
      width="large"
    >
      {/* Keyed so each template starts with fresh form defaults. */}
      {template ? <QuizBuilder key={template.id} template={template} onClose={onClose} /> : null}
    </Dialog>
  );
}

const isGenerating = (quiz?: OnboardingQuizAdmin | null) =>
  quiz?.generationStatus === 'Queued' || quiz?.generationStatus === 'Running';

function GenerationStatus({ quiz }: { quiz: OnboardingQuizAdmin }) {
  if (isGenerating(quiz))
    return (
      <small role="status">
        AI is generating questions from the package documents
        {quiz.generationStatus === 'Queued' ? ' (waiting for the worker)' : ''}…
      </small>
    );
  if (quiz.generationStatus === 'Failed')
    return (
      <small className="field__message" role="alert">
        AI generation failed: {quiz.generationError ?? 'unknown error'}
      </small>
    );
  return null;
}

type Editor = { mode: 'closed' } | { mode: 'add' } | { mode: 'edit'; question: QuizQuestionAdmin };

function QuizBuilder({ template, onClose }: { template: QuizTemplate; onClose: () => void }) {
  const quizQuery = useQuery({
    queryKey: ['onboarding', 'quiz', template.id],
    queryFn: () => getOnboardingQuiz(template.id),
    // Generation runs in the background worker; poll until it finishes.
    refetchInterval: (query) => (isGenerating(query.state.data) ? 3_000 : false)
  });
  const sourcesQuery = useQuery({
    queryKey: ['onboarding', 'quiz-sources', template.id],
    queryFn: () => getOnboardingQuizSources(template.id)
  });
  const [editor, setEditor] = useState<Editor>({ mode: 'closed' });
  const closeEditor = () => setEditor({ mode: 'closed' });

  const create = useOnboardingMutation({
    mutationFn: (value: QuizDraftValues) =>
      createOnboardingQuiz(template.id, {
        ...value,
        passingPercentage: Number(value.passingPercentage),
        questionCount: Number(value.questionCount),
        randomizeQuestions: true,
        randomizeOptions: true
      }),
    success: { title: 'Quiz draft created' },
    errorTitle: 'Could not create the quiz'
  });
  const generate = useOnboardingMutation({
    mutationFn: generateOnboardingQuiz,
    success: {
      title: 'Generating questions',
      message: 'This can take a few minutes. You can close this window; the questions appear here when ready.'
    },
    errorTitle: 'AI generation could not start'
  });
  const publish = useOnboardingMutation({
    mutationFn: publishOnboardingQuiz,
    success: {
      title: 'Quiz published',
      message: 'New hires on this package get it from now on. Its questions can no longer be changed.'
    },
    errorTitle: 'Could not publish the quiz'
  });
  const addQuestion = useOnboardingMutation({
    mutationFn: ({ quiz, question }: { quiz: OnboardingQuizAdmin; question: QuizQuestionInput }) =>
      addOnboardingQuizQuestion(quiz.id, question),
    success: { title: 'Question added' },
    errorTitle: 'Could not add question',
    onSuccess: closeEditor
  });
  const updateQuestion = useOnboardingMutation({
    mutationFn: ({
      quiz,
      current,
      question
    }: {
      quiz: OnboardingQuizAdmin;
      current: QuizQuestionAdmin;
      question: QuizQuestionInput;
    }) => updateOnboardingQuizQuestion(quiz.id, current.id, current.version, question),
    success: { title: 'Question updated' },
    errorTitle: 'Could not update question',
    onSuccess: closeEditor
  });
  const deleteQuestion = useOnboardingMutation({
    mutationFn: ({ quiz, question }: { quiz: OnboardingQuizAdmin; question: QuizQuestionAdmin }) =>
      deleteOnboardingQuizQuestion(quiz.id, question.id, question.version),
    success: { title: 'Question removed' },
    errorTitle: 'Could not remove question'
  });

  if (quizQuery.isLoading) return <Skeleton count={6} />;
  const quiz = quizQuery.data;
  if (!quiz)
    return (
      <QuizDraftForm
        templateName={template.name}
        loading={create.isPending}
        onCancel={onClose}
        onCreate={(value) => create.mutate(value)}
      />
    );

  const isDraft = quiz.status === 'Draft';
  const bankTooSmall =
    isDraft && quiz.questions.length >= quiz.questionCount && quiz.questions.length < quiz.questionCount * 2;
  const sources = sourcesQuery.data ?? [];

  return (
    <div className="form-stack quiz-builder">
      <Card className="quiz-builder__summary">
        <div>
          <small>
            Version {quiz.quizVersion} · {quiz.generationSource}
          </small>
          <h3>{quiz.title}</h3>
          <p>
            {quiz.questions.length} questions in bank · {quiz.questionCount} per attempt · Pass at{' '}
            {quiz.passingPercentage}%
          </p>
          <GenerationStatus quiz={quiz} />
          {bankTooSmall ? (
            <small>
              Tip: add at least {quiz.questionCount * 2} questions so a retake after a failed attempt shows different
              questions.
            </small>
          ) : null}
        </div>
        <Badge tone={quiz.status === 'Published' ? 'positive' : 'warning'}>{quiz.status}</Badge>
      </Card>

      {isDraft ? (
        <div className="row-actions">
          <Button
            variant="secondary"
            disabled={quiz.questions.length > 0 || isGenerating(quiz)}
            loading={generate.isPending || isGenerating(quiz)}
            onClick={() => generate.mutate(quiz.id)}
          >
            <Brain size={16} /> Generate with AI
          </Button>
          <Button variant="secondary" onClick={() => setEditor({ mode: 'add' })}>
            <Plus size={16} /> Add manually
          </Button>
          <Button
            disabled={quiz.questions.length < quiz.questionCount}
            loading={publish.isPending}
            onClick={() => publish.mutate(quiz)}
          >
            Publish quiz
          </Button>
        </div>
      ) : null}

      {quiz.questions.length ? (
        <div className="quiz-review-list">
          {quiz.questions.map((question, index) => (
            <QuizQuestionCard
              key={question.id}
              question={question}
              number={index + 1}
              editable={isDraft}
              deleting={deleteQuestion.isPending}
              onEdit={() => setEditor({ mode: 'edit', question })}
              onDelete={() => {
                if (window.confirm('Remove this draft question?')) deleteQuestion.mutate({ quiz, question });
              }}
            />
          ))}
        </div>
      ) : (
        <Card>
          <EmptyState
            title="No questions yet"
            description="Generate grounded draft questions from approved indexed documents, or add reviewed questions manually."
          />
        </Card>
      )}

      {isDraft && editor.mode === 'add' ? (
        <ManualQuestionForm
          key="new-question"
          quiz={quiz}
          sources={sources}
          loading={addQuestion.isPending}
          onCancel={closeEditor}
          onSave={(question) => addQuestion.mutate({ quiz, question })}
        />
      ) : null}
      {isDraft && editor.mode === 'edit' ? (
        <ManualQuestionForm
          key={editor.question.id}
          quiz={quiz}
          question={editor.question}
          sources={sources}
          loading={updateQuestion.isPending}
          onCancel={closeEditor}
          onSave={(question) => updateQuestion.mutate({ quiz, current: editor.question, question })}
        />
      ) : null}
    </div>
  );
}
