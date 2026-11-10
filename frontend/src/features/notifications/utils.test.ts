import { describe, expect, it } from 'vitest';

import { timeAgo } from './utils';

describe('timeAgo', () => {
  const now = Date.parse('2026-11-10T12:00:00Z');

  it('uses short relative units', () => {
    expect(timeAgo('2026-11-10T11:59:40Z', now)).toBe('just now');
    expect(timeAgo('2026-11-10T11:55:00Z', now)).toBe('5 min ago');
    expect(timeAgo('2026-11-10T09:00:00Z', now)).toBe('3 h ago');
    expect(timeAgo('2026-11-08T12:00:00Z', now)).toBe('2 d ago');
  });

  it('falls back to a date after a week', () => {
    expect(timeAgo('2026-10-01T12:00:00Z', now)).not.toContain('ago');
  });
});
