import { ArrowLeft, ArrowRight } from 'lucide-react';

import { Button } from '@/components/ui/button';

interface CursorPaginationProps {
  canPrevious: boolean;
  canNext: boolean;
  page: number;
  onPrevious: () => void;
  onNext: () => void;
}

export function CursorPagination({ canPrevious, canNext, page, onPrevious, onNext }: CursorPaginationProps) {
  return (
    <nav className="pagination" aria-label="Pagination">
      <span>Page {page}</span>
      <div>
        <Button variant="secondary" size="small" disabled={!canPrevious} onClick={onPrevious}>
          <ArrowLeft size={15} /> Previous
        </Button>
        <Button variant="secondary" size="small" disabled={!canNext} onClick={onNext}>
          Next <ArrowRight size={15} />
        </Button>
      </div>
    </nav>
  );
}
