import { useState } from 'react';

import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';

import type { DocumentOption } from '../hooks/use-template-source-options';

/**
 * Lets HR tick several clean documents at once. Each selected document becomes one required
 * "Read document" step, which is what the reread-after-failure rule needs.
 */
export function DocumentBulkPicker({
  documents,
  alreadyAdded,
  onAdd,
  onCancel
}: {
  documents: DocumentOption[];
  alreadyAdded: ReadonlySet<string>;
  onAdd: (documents: DocumentOption[]) => void;
  onCancel: () => void;
}) {
  const available = documents.filter((document) => !alreadyAdded.has(document.id));
  const [selected, setSelected] = useState<ReadonlySet<string>>(
    () => new Set(available.map((document) => document.id))
  );
  const toggle = (id: string) =>
    setSelected((current) => {
      const next = new Set(current);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });

  return (
    <Card className="template-step-editor">
      <div className="section-heading">
        <div>
          <h3>Add documents</h3>
          <p>
            Each selected document becomes a required reading step. Only clean company and project documents are listed.
          </p>
        </div>
      </div>
      {available.length ? (
        <div className="form-stack">
          {available.map((document) => (
            <label className="checkbox-field" key={document.id}>
              <input type="checkbox" checked={selected.has(document.id)} onChange={() => toggle(document.id)} />{' '}
              {document.label}
            </label>
          ))}
        </div>
      ) : (
        <p>Every available document is already in this template.</p>
      )}
      <div className="form-actions">
        <Button type="button" variant="secondary" onClick={onCancel}>
          Cancel
        </Button>
        <Button
          type="button"
          disabled={selected.size === 0}
          onClick={() => onAdd(available.filter((document) => selected.has(document.id)))}
        >
          Add {selected.size} document{selected.size === 1 ? '' : 's'}
        </Button>
      </div>
    </Card>
  );
}
