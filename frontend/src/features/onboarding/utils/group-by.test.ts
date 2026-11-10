import { describe, expect, it } from 'vitest';

import { groupBy } from './group-by';

describe('groupBy', () => {
  it('keeps sections in the order they first appear', () => {
    const items = [
      { section: 'Company', title: 'Handbook' },
      { section: 'Project', title: 'Architecture' },
      { section: 'Company', title: 'Security policy' }
    ];

    const groups = groupBy(items, (item) => item.section);

    expect(groups.map(([section]) => section)).toEqual(['Company', 'Project']);
    expect(groups[0][1].map((item) => item.title)).toEqual(['Handbook', 'Security policy']);
  });

  it('returns no groups for no items', () => {
    expect(groupBy([], () => 'x')).toEqual([]);
  });
});
