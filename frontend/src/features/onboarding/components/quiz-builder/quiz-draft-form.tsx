import { useForm } from 'react-hook-form';

import { FieldShell, Input } from '@/components/forms/field';
import { Button } from '@/components/ui/button';

export interface QuizDraftValues {
  title: string;
  passingPercentage: number;
  questionCount: number;
}

export function QuizDraftForm({
  templateName,
  loading,
  onCancel,
  onCreate
}: {
  templateName: string;
  loading: boolean;
  onCancel: () => void;
  onCreate: (value: QuizDraftValues) => void;
}) {
  const form = useForm<QuizDraftValues>({
    defaultValues: { title: `${templateName} knowledge assessment`, passingPercentage: 80, questionCount: 10 }
  });

  return (
    <form className="form-stack" onSubmit={form.handleSubmit(onCreate)}>
      <div className="form-grid">
        <FieldShell label="Assessment title" htmlFor="quizTitle" required>
          <Input id="quizTitle" {...form.register('title', { required: true })} />
        </FieldShell>
        <FieldShell label="Passing percentage" htmlFor="quizPass" required>
          <Input
            id="quizPass"
            type="number"
            min={1}
            max={100}
            {...form.register('passingPercentage', { valueAsNumber: true })}
          />
        </FieldShell>
        <FieldShell
          label="Questions per attempt"
          htmlFor="quizCount"
          required
          hint="AI generates a larger question bank so retakes show different questions"
        >
          <Input
            id="quizCount"
            type="number"
            min={1}
            max={100}
            {...form.register('questionCount', { valueAsNumber: true })}
          />
        </FieldShell>
      </div>
      <div className="form-actions">
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button type="submit" loading={loading}>
          Create draft
        </Button>
      </div>
    </form>
  );
}
