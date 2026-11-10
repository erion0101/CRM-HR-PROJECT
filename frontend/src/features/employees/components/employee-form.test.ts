import { describe, expect, it } from 'vitest';

import { normalizeEmployeeInput } from '../utils/normalize-employee-input';

describe('normalizeEmployeeInput', () => {
  it('omits blank optional values before sending them to the API', () => {
    const result = normalizeEmployeeInput({
      employeeNumber: 'DEMO-MANAGER',
      firstName: 'Erion',
      lastName: 'Manager',
      workEmail: 'erion.manger@gmail.com',
      phone: '   ',
      dateOfBirth: ''
    });

    expect(result.phone).toBeUndefined();
    expect(result.dateOfBirth).toBeUndefined();
  });

  it('preserves valid optional values', () => {
    const result = normalizeEmployeeInput({
      employeeNumber: 'DEMO-MANAGER',
      firstName: 'Erion',
      lastName: 'Manager',
      workEmail: 'erion.manger@gmail.com',
      phone: ' +383 44 000 000 ',
      dateOfBirth: '1990-01-15'
    });

    expect(result.phone).toBe('+383 44 000 000');
    expect(result.dateOfBirth).toBe('1990-01-15');
  });
});
