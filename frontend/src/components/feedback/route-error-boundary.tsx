import { RefreshCw, TriangleAlert } from 'lucide-react';
import { useRouteError } from 'react-router';

import { isChunkLoadError } from '@/lib/lazy-with-retry';

export function RouteErrorBoundary() {
  const error = useRouteError();
  const isStaleApplication = isChunkLoadError(error);

  return (
    <main className="route-error-page">
      <div className="full-state" role="alert">
        <span>
          <TriangleAlert size={30} aria-hidden="true" />
        </span>
        <h1>{isStaleApplication ? 'Application update available' : 'We could not open this page'}</h1>
        <p>
          {isStaleApplication
            ? 'A newer version of the HR workspace is available. Reload once to continue with the latest version.'
            : 'An unexpected error occurred. Reload the application and try again.'}
        </p>
        <div className="route-error-page__actions">
          <button
            className="button button--primary button--medium"
            type="button"
            onClick={() => window.location.reload()}
          >
            <RefreshCw size={16} aria-hidden="true" /> Reload application
          </button>
          <a className="button button--secondary button--medium" href="/">
            Return to overview
          </a>
        </div>
      </div>
    </main>
  );
}
