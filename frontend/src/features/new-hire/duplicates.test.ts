import { describe, expect, it } from 'vitest';

import { exactDuplicates } from './duplicates';

const mila = {
  id: '1',
  firstName: 'Mila',
  lastName: 'Demo',
  employeeNumber: 'EMP-1',
  workEmail: 'mila.demo@astercare.example'
};
const diana = {
  id: '2',
  firstName: 'Diana',
  lastName: 'Demo',
  employeeNumber: 'EMP-12',
  workEmail: 'diana@astercare.example'
};

describe('exactDuplicates', () => {
  it('matches email and number exactly, ignoring case and spaces', () => {
    expect(exactDuplicates([mila], ' MILA.demo@AsterCare.example ', 'emp-1')).toEqual([
      { field: 'workEmail', employee: mila },
      { field: 'employeeNumber', employee: mila }
    ]);
  });

  it('ignores partial matches from the contains-search', () => {
    expect(exactDuplicates([diana], 'ana@astercare.example', 'EMP-1')).toEqual([]);
  });
});
