import { describe, expect, it } from 'vitest';

import { isChunkLoadError } from './lazy-with-retry';

describe('isChunkLoadError', () => {
  it.each([
    'Failed to fetch dynamically imported module: http://localhost/assets/security-page.js',
    'Importing a module script failed.',
    'ChunkLoadError: Loading chunk 12 failed'
  ])('recognizes a recoverable stale chunk failure: %s', (message) => {
    expect(isChunkLoadError(new TypeError(message))).toBe(true);
  });

  it('does not treat an application exception as a stale chunk', () => {
    expect(isChunkLoadError(new Error('Could not save the employee.'))).toBe(false);
  });
});
