import { FilePlus2, Plus, Trash2 } from 'lucide-react';
import { useState } from 'react';
import { useFieldArray, useForm, type UseFormRegister } from 'react-hook-form';

import { FieldShell, Input, Select, Textarea } from '@/components/forms/field';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';

import { type DocumentOption, type SourceOption, useTemplateSourceOptions } from '../hooks/use-template-source-options';
import type { OnboardingStepType, OnboardingTemplateKind, OnboardingTemplateStepInput } from '../types';
import { packageKindDescription, packageKindLabel } from '../utils/package-labels';
import { DocumentBulkPicker } from './document-bulk-picker';

const packageKinds: OnboardingTemplateKind[] = ['Company', 'Project', 'General'];

export interface TemplateDraft {
  name: string;
  description: string;
  kind: OnboardingTemplateKind;
  projectId?: string;
  durationDays: number;
  departmentId?: string;
  positionId?: string;
  steps: OnboardingTemplateStepInput[];
}

type TemplateFormStep = Omit<
  OnboardingTemplateStepInput,
  'sectionOrder' | 'stepOrder' | 'dependsOnStepOrder' | 'knowledgeArticleId' | 'documentId' | 'externalUrl'
> & { knowledgeArticleId: string; documentId: string; externalUrl: string; dependsOnStepOrder: string };

interface TemplateFormValues {
  name: string;
  description: string;
  kind: OnboardingTemplateKind;
  projectId: string;
  durationDays: number;
  departmentId: string;
  positionId: string;
  steps: TemplateFormStep[];
}

const stepTypeOptions: { value: OnboardingStepType; label: string }[] = [
  { value: 'PracticalTask', label: 'Practical task' },
  { value: 'ReadDocument', label: 'Read document' },
  { value: 'ReadArticle', label: 'Read article' },
  { value: 'PolicyAcknowledgment', label: 'Acknowledge policy' },
  { value: 'ExternalResource', label: 'External resource' },
  { value: 'ManagerReviewedMilestone', label: 'Manager-reviewed milestone' }
];

const emptyStep = (): TemplateFormStep => ({
  sectionTitle: 'First week',
  title: '',
  instructions: '',
  expectedOutcome: '',
  stepType: 'PracticalTask',
  dueAfterDays: 7,
  isRequired: true,
  knowledgeArticleId: '',
  documentId: '',
  externalUrl: '',
  dependsOnStepOrder: ''
});

function readingStepFor(document: DocumentOption): TemplateFormStep {
  const name = document.documentName.replace(/\.(pdf|docx)$/i, '');
  return {
    ...emptyStep(),
    sectionTitle: document.articleTitle,
    title: name,
    instructions: `Read "${name}" carefully. The onboarding quiz will ask about it.`,
    expectedOutcome: `You understand the key points of "${name}".`,
    stepType: 'ReadDocument',
    documentId: document.id
  };
}

const isUntouched = (step: TemplateFormStep) =>
  !step.title && !step.instructions && !step.expectedOutcome && step.stepType === 'PracticalTask';

/** Only send the link field that matches the step type; the form keeps all of them while editing. */
function toStepInput(step: TemplateFormStep, index: number): OnboardingTemplateStepInput {
  const usesArticle = step.stepType === 'ReadArticle' || step.stepType === 'PolicyAcknowledgment';
  return {
    ...step,
    sectionOrder: index,
    stepOrder: index,
    dueAfterDays: Number(step.dueAfterDays),
    knowledgeArticleId: usesArticle ? step.knowledgeArticleId || undefined : undefined,
    documentId: step.stepType === 'ReadDocument' ? step.documentId || undefined : undefined,
    externalUrl: step.stepType === 'ExternalResource' ? step.externalUrl || undefined : undefined,
    dependsOnStepOrder: step.dependsOnStepOrder === '' ? undefined : Number(step.dependsOnStepOrder)
  };
}

export function TemplateDialog({
  open,
  loading,
  onClose,
  onSave
}: {
  open: boolean;
  loading: boolean;
  onClose: () => void;
  onSave: (value: TemplateDraft) => Promise<unknown>;
}) {
  const options = useTemplateSourceOptions();
  const form = useForm<TemplateFormValues>({
    defaultValues: {
      name: '',
      description: '',
      kind: 'Company',
      projectId: '',
      durationDays: 30,
      departmentId: '',
      positionId: '',
      steps: [emptyStep()]
    }
  });
  const steps = useFieldArray({ control: form.control, name: 'steps' });
  const kind = form.watch('kind');
  const [pickerOpen, setPickerOpen] = useState(false);
  const addDocuments = (documents: DocumentOption[]) => {
    const current = form.getValues('steps');
    const newSteps = documents.map(readingStepFor);
    // Replace the blank starter step instead of leaving an empty step at the top.
    if (current.length === 1 && isUntouched(current[0])) steps.replace(newSteps);
    else steps.append(newSteps);
    setPickerOpen(false);
  };
  const submit = form.handleSubmit(async (value) =>
    onSave({
      name: value.name,
      description: value.description,
      kind: value.kind,
      projectId: value.kind === 'Project' ? value.projectId : undefined,
      durationDays: Number(value.durationDays),
      departmentId: value.departmentId || undefined,
      positionId: value.positionId || undefined,
      steps: value.steps.map(toStepInput)
    })
  );

  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Create onboarding template"
      description="Build an ordered, reusable journey. Assignments snapshot the published version."
      width="large"
    >
      <form className="form-stack" onSubmit={submit}>
        <div className="form-grid">
          <FieldShell label="Package type" htmlFor="templateKind" required hint={packageKindDescription[kind]}>
            <Select id="templateKind" {...form.register('kind')}>
              {packageKinds.map((option) => (
                <option value={option} key={option}>
                  {packageKindLabel[option]}
                </option>
              ))}
            </Select>
          </FieldShell>
          {kind === 'Project' ? (
            <FieldShell
              label="Project"
              htmlFor="templateProject"
              required
              hint="Each project has one onboarding package"
            >
              <Select id="templateProject" {...form.register('projectId', { required: kind === 'Project' })}>
                <option value="">Choose a project</option>
                <OptionList options={options.projects} />
              </Select>
            </FieldShell>
          ) : null}
        </div>
        <div className="form-grid">
          <FieldShell label="Template name" htmlFor="templateName" required>
            <Input id="templateName" {...form.register('name', { required: true })} />
          </FieldShell>
          <FieldShell label="Duration in days" htmlFor="templateDuration" required>
            <Input
              id="templateDuration"
              type="number"
              min={1}
              max={365}
              {...form.register('durationDays', { valueAsNumber: true })}
            />
          </FieldShell>
          <FieldShell label="Target organization unit" htmlFor="templateDepartment">
            <Select id="templateDepartment" {...form.register('departmentId')}>
              <option value="">All units</option>
              <OptionList options={options.departments} />
            </Select>
          </FieldShell>
          <FieldShell label="Target position" htmlFor="templatePosition">
            <Select id="templatePosition" {...form.register('positionId')}>
              <option value="">All positions</option>
              <OptionList options={options.positions} />
            </Select>
          </FieldShell>
        </div>
        <FieldShell label="Description" htmlFor="templateDescription" required>
          <Textarea id="templateDescription" {...form.register('description', { required: true })} />
        </FieldShell>

        <div className="section-heading">
          <div>
            <h3>Journey steps</h3>
            <p>
              Steps run in this order. Add one required document step per file when every document must be reread after
              a failed assessment.
            </p>
          </div>
          <div className="row-actions">
            <Button type="button" size="small" variant="secondary" onClick={() => setPickerOpen(true)}>
              <FilePlus2 size={15} /> Add documents
            </Button>
            <Button type="button" size="small" variant="secondary" onClick={() => steps.append(emptyStep())}>
              <Plus size={15} /> Add step
            </Button>
          </div>
        </div>

        {pickerOpen ? (
          <DocumentBulkPicker
            documents={options.documents}
            alreadyAdded={
              new Set(
                form
                  .getValues('steps')
                  .map((step) => step.documentId)
                  .filter(Boolean)
              )
            }
            onAdd={addDocuments}
            onCancel={() => setPickerOpen(false)}
          />
        ) : null}

        {steps.fields.map((field, index) => (
          <TemplateStepEditor
            key={field.id}
            index={index}
            register={form.register}
            previousStepIds={steps.fields.slice(0, index).map((previous) => previous.id)}
            articles={options.articles}
            documents={options.documents}
            onRemove={steps.fields.length > 1 ? () => steps.remove(index) : undefined}
          />
        ))}

        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={loading}>
            Create draft
          </Button>
        </div>
      </form>
    </Dialog>
  );
}

function TemplateStepEditor({
  index,
  register,
  previousStepIds,
  articles,
  documents,
  onRemove
}: {
  index: number;
  register: UseFormRegister<TemplateFormValues>;
  previousStepIds: string[];
  articles: SourceOption[];
  documents: SourceOption[];
  onRemove?: () => void;
}) {
  const id = (name: string) => `template-${name}-${index}`;
  return (
    <Card className="template-step-editor">
      <div className="section-heading">
        <h3>Step {index + 1}</h3>
        {onRemove ? (
          <Button type="button" size="small" variant="ghost" onClick={onRemove}>
            <Trash2 size={15} /> Remove
          </Button>
        ) : null}
      </div>
      <div className="form-grid">
        <FieldShell label="Section" htmlFor={id('section')} required>
          <Input id={id('section')} {...register(`steps.${index}.sectionTitle`, { required: true })} />
        </FieldShell>
        <FieldShell label="Step type" htmlFor={id('type')} required>
          <Select id={id('type')} {...register(`steps.${index}.stepType`)}>
            {stepTypeOptions.map((option) => (
              <option value={option.value} key={option.value}>
                {option.label}
              </option>
            ))}
          </Select>
        </FieldShell>
        <FieldShell label="Step title" htmlFor={id('title')} required>
          <Input id={id('title')} {...register(`steps.${index}.title`, { required: true })} />
        </FieldShell>
        <FieldShell label="Due after days" htmlFor={id('due')} required>
          <Input
            id={id('due')}
            type="number"
            min={0}
            max={365}
            {...register(`steps.${index}.dueAfterDays`, { valueAsNumber: true })}
          />
        </FieldShell>
      </div>
      <FieldShell label="Instructions" htmlFor={id('instructions')} required>
        <Textarea id={id('instructions')} {...register(`steps.${index}.instructions`, { required: true })} />
      </FieldShell>
      <FieldShell label="Expected outcome" htmlFor={id('outcome')} required>
        <Textarea id={id('outcome')} {...register(`steps.${index}.expectedOutcome`, { required: true })} />
      </FieldShell>
      <div className="form-grid">
        <FieldShell
          label="Required document"
          htmlFor={id('document')}
          hint="Use with Read document; clean company and project PDFs from the Company page are shown"
        >
          <Select id={id('document')} {...register(`steps.${index}.documentId`)}>
            <option value="">No document</option>
            <OptionList options={documents} />
          </Select>
        </FieldShell>
        <FieldShell label="Knowledge article" htmlFor={id('article')} hint="Use with article and policy steps">
          <Select id={id('article')} {...register(`steps.${index}.knowledgeArticleId`)}>
            <option value="">No article</option>
            <OptionList options={articles} />
          </Select>
        </FieldShell>
        <FieldShell label="External URL" htmlFor={id('url')}>
          <Input id={id('url')} type="url" {...register(`steps.${index}.externalUrl`)} />
        </FieldShell>
        <FieldShell label="Depends on" htmlFor={id('dependency')}>
          <Select id={id('dependency')} {...register(`steps.${index}.dependsOnStepOrder`)}>
            <option value="">No dependency</option>
            {previousStepIds.map((stepId, previousIndex) => (
              <option value={previousIndex} key={stepId}>
                Step {previousIndex + 1}
              </option>
            ))}
          </Select>
        </FieldShell>
        <label className="checkbox-field">
          <input type="checkbox" {...register(`steps.${index}.isRequired`)} /> Required step
        </label>
      </div>
    </Card>
  );
}

function OptionList({ options }: { options: SourceOption[] }) {
  return (
    <>
      {options.map((option) => (
        <option value={option.id} key={option.id}>
          {option.label}
        </option>
      ))}
    </>
  );
}
