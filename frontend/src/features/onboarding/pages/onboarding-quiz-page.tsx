import { useMutation, useQuery } from '@tanstack/react-query';
import { ArrowLeft, CheckCircle2, ClipboardCheck } from 'lucide-react';
import { useMemo, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router';

import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { PageHeader } from '@/components/navigation/page-header';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { toAppError } from '@/lib/api-error';

import { getQuizAttempt, submitQuizAttempt } from '../api/onboarding-api';

export function OnboardingQuizPage() {
  const { attemptId = '' } = useParams();
  const navigate = useNavigate();
  const { notify } = useNotification();
  const [answers, setAnswers] = useState<Record<string, string>>({});
  const query = useQuery({
    queryKey: ['onboarding', 'attempt', attemptId],
    queryFn: () => getQuizAttempt(attemptId),
    enabled: Boolean(attemptId)
  });
  const attempt = query.data;
  const answered = useMemo(() => Object.keys(answers).length, [answers]);
  const submit = useMutation({
    mutationFn: () => submitQuizAttempt(attempt!, answers),
    onSuccess: async (result) => {
      await queryClient.invalidateQueries({ queryKey: ['onboarding'] });
      notify({
        title: result.status === 'Passed' ? 'Assessment passed' : 'Reading restart required',
        message: result.message,
        tone: result.status === 'Passed' ? 'success' : 'error'
      });
      navigate('/onboarding', { state: { quizResult: result } });
    },
    onError: (error) =>
      notify({ title: 'Could not submit assessment', message: toAppError(error).message, tone: 'error' })
  });

  if (query.isLoading)
    return (
      <Card>
        <Skeleton count={8} />
      </Card>
    );
  if (query.isError || !attempt)
    return (
      <Card>
        <ErrorState
          title="Assessment unavailable"
          description="This assessment does not exist or is no longer available to your account."
          onRetry={() => void query.refetch()}
        />
      </Card>
    );
  if (attempt.status !== 'InProgress')
    return (
      <div className="page-stack">
        <PageHeader
          eyebrow="Assessment"
          title={`Attempt ${attempt.attemptNumber}`}
          description={`This attempt is ${attempt.status.toLowerCase()}.`}
        />
        <Card>
          <div className="quiz-finished">
            <CheckCircle2 size={36} />
            <h2>{attempt.scorePercentage}%</h2>
            <p>Passing score: {attempt.passingPercentage}%</p>
            <Link className="button button--primary button--medium" to="/onboarding">
              Return to onboarding
            </Link>
          </div>
        </Card>
      </div>
    );

  const allAnswered = answered === attempt.questions.length;
  return (
    <div className="page-stack quiz-page">
      <PageHeader
        eyebrow={`Reading cycle ${attempt.readingCycleNumber} · Attempt ${attempt.attemptNumber}`}
        title="Onboarding assessment"
        description="Select one answer for every question. Your answers are graded securely after submission."
        actions={
          <Link className="button button--secondary button--medium" to="/onboarding">
            <ArrowLeft size={16} /> Exit assessment
          </Link>
        }
      />
      <Card className="quiz-progress">
        <div>
          <ClipboardCheck size={20} />
          <strong>
            {answered} of {attempt.questions.length} answered
          </strong>
        </div>
        <div className="mini-progress">
          <span style={{ width: `${attempt.questions.length ? (answered * 100) / attempt.questions.length : 0}%` }} />
        </div>
        <small>You need {attempt.passingPercentage}% to pass. A failed attempt restarts all required reading.</small>
      </Card>
      <div className="quiz-questions">
        {attempt.questions.map((question) => (
          <Card className="quiz-question" key={question.id}>
            <header>
              <span>Question {question.number}</span>
              <strong>{question.questionText}</strong>
            </header>
            <div className="quiz-options">
              {question.options.map((option, index) => (
                <label className={answers[question.id] === option.id ? 'is-selected' : ''} key={option.id}>
                  <input
                    type="radio"
                    name={question.id}
                    value={option.id}
                    checked={answers[question.id] === option.id}
                    onChange={() => setAnswers((current) => ({ ...current, [question.id]: option.id }))}
                  />
                  <span>{String.fromCharCode(65 + index)}</span>
                  <p>{option.text}</p>
                </label>
              ))}
            </div>
          </Card>
        ))}
      </div>
      <Card className="quiz-submit">
        <div>
          <strong>Ready to submit?</strong>
          <p>Submission is final for this attempt. If you do not pass, every required reading item resets to Unread.</p>
        </div>
        <Button
          disabled={!allAnswered}
          loading={submit.isPending}
          onClick={() => {
            if (window.confirm('Submit your answers? This attempt cannot be changed afterward.')) submit.mutate();
          }}
        >
          Submit assessment
        </Button>
      </Card>
    </div>
  );
}
