import { describe, expect, it } from 'vitest';

import { formatCountdown } from './format-countdown';

describe('formatCountdown', () => {
  it('formats minutes and padded seconds', () => {
    expect(formatCountdown(125_000)).toBe('2:05');
  });

  it('rounds partial seconds up and never goes negative', () => {
    expect(formatCountdown(400)).toBe('0:01');
    expect(formatCountdown(-5_000)).toBe('0:00');
  });
});
