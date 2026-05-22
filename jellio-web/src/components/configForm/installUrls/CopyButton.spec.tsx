import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { CopyButton } from './CopyButton';
import { flushAct } from '@/test-utils/flushAct';

describe('CopyButton', () => {
  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it('should keep Copied visible until the flash window since the most recent click elapses', async () => {
    vi.useFakeTimers();
    const onCopy = vi.fn<() => Promise<void>>().mockResolvedValue(undefined);

    render(<CopyButton onCopy={onCopy} ariaLabel="Copy link" />);
    const button = screen.getByRole('button', { name: /copy link/i });

    await flushAct(() => fireEvent.click(button));
    expect(button.textContent).toMatch(/copied/i);

    await flushAct(() => vi.advanceTimersByTime(1000));
    expect(button.textContent).toMatch(/copied/i);

    await flushAct(() => fireEvent.click(button));
    await flushAct(() => vi.advanceTimersByTime(1500));
    expect(button.textContent).toMatch(/copied/i);

    await flushAct(() => vi.advanceTimersByTime(600));
    expect(button.textContent).not.toMatch(/copied/i);
  });
});
