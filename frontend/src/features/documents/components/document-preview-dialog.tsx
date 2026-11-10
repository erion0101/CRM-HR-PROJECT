import { useQuery } from '@tanstack/react-query';
import { Download, ExternalLink, FileText } from 'lucide-react';
import { useEffect, useState } from 'react';

import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { Button } from '@/components/ui/button';
import { Dialog } from '@/components/ui/dialog';

import { downloadDocument, getDocumentFile } from '../api/documents-api';
import type { EmployeeDocument } from '../types';

export function DocumentPreviewDialog({
  employeeId,
  document,
  onClose
}: {
  employeeId: string;
  document: EmployeeDocument | null;
  onClose: () => void;
}) {
  const query = useQuery({
    queryKey: ['documents', 'preview', employeeId, document?.id, document?.currentVersion],
    queryFn: () => getDocumentFile(employeeId, document!.id),
    enabled: Boolean(document && document.status === 'Clean'),
    staleTime: 60_000
  });
  const [url, setUrl] = useState<string | null>(null);

  useEffect(() => {
    if (!query.data) {
      setUrl(null);
      return;
    }
    const next = URL.createObjectURL(query.data);
    setUrl(next);
    return () => URL.revokeObjectURL(next);
  }, [query.data]);

  if (!document) return null;
  const isPdf = document.displayName.toLowerCase().endsWith('.pdf') || query.data?.type === 'application/pdf';
  return (
    <Dialog
      open
      title={document.displayName}
      description={`${document.documentType} · Version ${document.currentVersion}`}
      width="wide"
      onClose={onClose}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Close
          </Button>
          <Button disabled={document.status !== 'Clean'} onClick={() => void downloadDocument(employeeId, document)}>
            <Download size={16} /> Download
          </Button>
        </>
      }
    >
      <div className="document-preview">
        {query.isLoading ? (
          <div className="document-preview__loading">
            <Skeleton count={7} />
          </div>
        ) : query.isError ? (
          <ErrorState
            title="Preview could not be loaded"
            description="The file may no longer be available or your access may have changed."
            onRetry={() => void query.refetch()}
          />
        ) : isPdf && url ? (
          <iframe src={url} title={`Preview of ${document.displayName}`} />
        ) : (
          <div className="document-preview__unsupported">
            <span>
              <FileText size={34} />
            </span>
            <h3>Preview is not available for this file type</h3>
            <p>Download the document to open it with the appropriate application.</p>
            {url ? (
              <Button variant="secondary" onClick={() => window.open(url, '_blank', 'noopener,noreferrer')}>
                <ExternalLink size={16} /> Open file
              </Button>
            ) : null}
          </div>
        )}
      </div>
    </Dialog>
  );
}
