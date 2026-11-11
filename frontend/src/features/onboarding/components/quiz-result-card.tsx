import { RotateCcw } from 'lucide-react';

import { Card } from '@/components/ui/card';

import type { QuizDocumentScore, QuizResult } from '../types';

export function QuizResultCard({ result }: { result: QuizResult }) {
  const passed = result.status === 'Passed';
  const documentsToReview = result.documentScores.filter((score) => score.correctAnswers < score.totalQuestions);

  return (
    <Card className={`assessment-result assessment-result--${result.status.toLowerCase()}`}>
      <div>
        <small>Quiz result · Round {result.readingCycleNumber}</small>
        <h2>{passed ? 'Quiz passed' : 'Not passed yet: read the documents again'}</h2>
        <p>{result.message}</p>
        <strong>
          {result.scorePercentage}% score · {result.passingPercentage}% required
        </strong>

        {!passed && documentsToReview.length ? (
          <div className="assessment-feedback">
            <p>
              <strong>Focus on these documents when you reread:</strong>
            </p>
            {documentsToReview.map((score) => (
              <DocumentScoreRow key={score.sourceLabel} score={score} />
            ))}
          </div>
        ) : null}

        {passed && result.answerReview.length ? (
          <details className="assessment-feedback">
            <summary>Review answers and sources</summary>
            {result.answerReview.map((answer, index) => (
              <div className={answer.isCorrect ? 'is-correct' : 'is-incorrect'} key={answer.questionId}>
                <strong>
                  {index + 1}. {answer.questionText}
                </strong>
                <span>Your answer: {answer.selectedOptionText}</span>
                {!answer.isCorrect ? <span>Correct answer: {answer.correctOptionText}</span> : null}
                <p>{answer.explanation}</p>
                <small>
                  Source: {answer.sourceLabel}
                  {answer.sourcePage ? ` · page ${answer.sourcePage}` : ''}
                </small>
              </div>
            ))}
          </details>
        ) : null}
      </div>

      {result.readingReset ? (
        <a className="button button--primary button--medium" href="#required-reading">
          <RotateCcw size={16} /> Start reading again
        </a>
      ) : null}
    </Card>
  );
}

function DocumentScoreRow({ score }: { score: QuizDocumentScore }) {
  return (
    <div className="is-incorrect">
      <strong>{score.sourceLabel}</strong>
      <span>
        {score.correctAnswers} of {score.totalQuestions} questions answered correctly
      </span>
    </div>
  );
}
