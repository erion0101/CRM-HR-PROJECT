import { X } from 'lucide-react';
import { useEffect, useRef, type PropsWithChildren } from 'react';

import { Button } from './button';

interface DialogProps {
  open: boolean;
  title: string;
  description?: string;
  onClose: () => void;
  footer?: React.ReactNode;
  width?: 'medium' | 'large' | 'wide';
}

export function Dialog({ open, title, description, onClose, footer, width = 'medium', children }: PropsWithChildren<DialogProps>) {
  const panelRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    panelRef.current?.focus();
    const closeOnEscape = (event: KeyboardEvent) => { if (event.key === 'Escape') onClose(); };
    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [onClose, open]);

  if (!open) return null;

  return (
    <div className="dialog-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) onClose(); }}>
      <div className={`dialog dialog--${width}`} role="dialog" aria-modal="true" aria-labelledby="dialog-title" tabIndex={-1} ref={panelRef}>
        <header className="dialog__header">
          <div><h2 id="dialog-title">{title}</h2>{description ? <p>{description}</p> : null}</div>
          <Button variant="ghost" size="small" onClick={onClose} aria-label="Close dialog"><X size={18} /></Button>
        </header>
        <div className="dialog__body">{children}</div>
        {footer ? <footer className="dialog__footer">{footer}</footer> : null}
      </div>
    </div>
  );
}
