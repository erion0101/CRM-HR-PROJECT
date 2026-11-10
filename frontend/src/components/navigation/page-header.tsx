import type { PropsWithChildren, ReactNode } from 'react';

interface PageHeaderProps {
  eyebrow?: string;
  title: string;
  description?: string;
  actions?: ReactNode;
}

export function PageHeader({ eyebrow, title, description, actions, children }: PropsWithChildren<PageHeaderProps>) {
  return (
    <header className="page-header">
      <div className="page-header__main">
        {eyebrow ? <span className="eyebrow">{eyebrow}</span> : null}
        <h1>{title}</h1>
        {description ? <p>{description}</p> : null}
      </div>
      {actions ? <div className="page-header__actions">{actions}</div> : null}
      {children ? <div className="page-header__toolbar">{children}</div> : null}
    </header>
  );
}
