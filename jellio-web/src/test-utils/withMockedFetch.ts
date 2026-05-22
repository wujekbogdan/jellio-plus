import { expect, vi } from 'vitest';

export const withMockedFetch =
  (response: ResponseInit & { body?: unknown }) =>
  (test: () => Promise<void> | void) =>
  async () => {
    const payload =
      response.body === undefined ? null : JSON.stringify(response.body);
    vi.stubGlobal(
      'fetch',
      vi.fn().mockResolvedValue(new Response(payload, response)),
    );
    try {
      await test();
    } finally {
      vi.unstubAllGlobals();
      expect.hasAssertions();
    }
  };
