import { describe, expect, it } from 'vitest';

import { generateTemporaryPassword, suggestEmployeeNumber } from './utils';

describe('generateTemporaryPassword', () => {
  it('always meets the account password policy', () => {
    for (let run = 0; run < 200; run++) {
      const password = generateTemporaryPassword();
      expect(password.length).toBeGreaterThanOrEqual(12);
      expect(password).toMatch(/[A-Z]/);
      expect(password).toMatch(/[a-z]/);
      expect(password).toMatch(/[0-9]/);
      expect(password).toMatch(/[^A-Za-z0-9]/);
      expect(password).not.toMatch(/[0O1lI]/);
    }
  });

  it('never goes below the 12-character minimum', () => {
    expect(generateTemporaryPassword(4)).toHaveLength(12);
  });
});

describe('suggestEmployeeNumber', () => {
  it('uses the date and a four-digit suffix', () => {
    expect(suggestEmployeeNumber(new Date('2026-11-10T12:00:00Z'), () => 821)).toBe('EMP-261110-1821');
  });
});
