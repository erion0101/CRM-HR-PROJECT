import type { OnboardingAssignmentOptions } from '../types';
import { packageKindLabel } from '../utils/package-labels';

/**
 * Checklist of the documents and articles in the chosen package(s). HR unticks what this employee does not need;
 * the employee then only reads, and is only quizzed on, the ticked items.
 */
export function ReadingSelection({
  options,
  excluded,
  onToggle
}: {
  options: OnboardingAssignmentOptions;
  excluded: ReadonlySet<string>;
  onToggle: (templateStepId: string) => void;
}) {
  const packages = options.packages.filter((pkg) => pkg.readingSteps.length > 0);
  if (!packages.length) return null;
  const included = packages.flatMap((pkg) => pkg.readingSteps).filter((step) => !excluded.has(step.templateStepId));
  const hasQuiz = packages.some((pkg) => pkg.hasQuiz);
  const coveredQuestions = included.reduce((sum, step) => sum + step.questionCount, 0);

  return (
    <fieldset className="form-stack reading-selection">
      <legend>
        <strong>Documents to read</strong>
      </legend>
      <p className="field__hint">
        Untick what this employee does not need. They will only read, and only be asked about, the ticked documents.
      </p>
      {packages.map((pkg) => (
        <div className="form-stack" key={pkg.templateId}>
          <small>
            {pkg.name} · {packageKindLabel[pkg.kind]}
          </small>
          {pkg.readingSteps.map((step) => (
            <label className="checkbox-field" key={step.templateStepId}>
              <input
                type="checkbox"
                checked={!excluded.has(step.templateStepId)}
                onChange={() => onToggle(step.templateStepId)}
              />{' '}
              {step.title}
              <span className="field__hint">
                {' '}
                · {step.isRequired ? 'required' : 'optional'}
                {pkg.hasQuiz ? ` · ${step.questionCount} quiz question${step.questionCount === 1 ? '' : 's'}` : ''}
              </span>
            </label>
          ))}
        </div>
      ))}
      <p className="field__hint" role="status">
        {included.length} of {packages.reduce((sum, pkg) => sum + pkg.readingSteps.length, 0)} documents selected
        {hasQuiz
          ? coveredQuestions > 0
            ? ` · quiz draws from ${coveredQuestions} reviewed question${coveredQuestions === 1 ? '' : 's'}`
            : ' · no reviewed questions cover these documents, so this employee will not take a quiz'
          : ''}
      </p>
    </fieldset>
  );
}
