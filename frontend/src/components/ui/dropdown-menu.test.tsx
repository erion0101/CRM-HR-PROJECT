import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';

import { DropdownMenu } from './dropdown-menu';

describe('DropdownMenu', () => {
  it('opens and invokes the selected action', async () => {
    const user = userEvent.setup();
    const onSelect = vi.fn();
    render(<DropdownMenu items={[{ label: 'Edit unit', onSelect }]} />);

    await user.click(screen.getByRole('button', { name: 'Open actions' }));
    await user.click(screen.getByRole('menuitem', { name: 'Edit unit' }));

    expect(onSelect).toHaveBeenCalledOnce();
    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
  });

  it('closes on Escape and restores focus to the trigger', async () => {
    const user = userEvent.setup();
    render(<DropdownMenu items={[{ label: 'View details', onSelect: vi.fn() }]} />);
    const trigger = screen.getByRole('button', { name: 'Open actions' });

    await user.click(trigger);
    await user.keyboard('{Escape}');

    expect(screen.queryByRole('menu')).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });
});
