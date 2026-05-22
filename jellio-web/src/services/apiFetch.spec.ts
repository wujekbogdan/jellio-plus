import { describe, expect, it } from 'vitest';
import { z } from 'zod';
import { HttpError, makeApiFetch } from './apiFetch';
import { withMockedFetch } from '@/test-utils/withMockedFetch';

describe('apiFetch', () => {
  it(
    'should validate the response against the provided schema',
    withMockedFetch({
      status: 200,
      headers: { 'content-type': 'application/json' },
      body: { name: 'jellio', count: 3 },
    })(async () => {
      const schema = z.object({ name: z.string(), count: z.number() });
      const fetchPayload = makeApiFetch(schema);
      const result = await fetchPayload({ url: 'https://example.com/api' });
      expect(result).toEqual({ name: 'jellio', count: 3 });
    }),
  );

  it(
    'should throw HttpError carrying status and response body on non-2xx',
    withMockedFetch({
      status: 500,
      statusText: 'Internal Server Error',
      headers: { 'content-type': 'application/json' },
      body: { message: 'database is on fire' },
    })(async () => {
      const fetchPayload = makeApiFetch(z.unknown());
      const error = await fetchPayload({ url: 'https://example.com/api' })
        .then(() => null)
        .catch((reason: unknown) => reason);

      expect(error).toBeInstanceOf(HttpError);
      expect(error).toMatchObject({
        status: 500,
        responseBody: { message: 'database is on fire' },
      });
    }),
  );

  it(
    'should send POST requests with a JSON-serialized body and content-type header',
    withMockedFetch({
      status: 200,
      headers: { 'content-type': 'application/json' },
      body: { ok: true },
    })(async () => {
      const fetchPayload = makeApiFetch(z.object({ ok: z.boolean() }));
      await fetchPayload({
        url: 'https://example.com/api',
        method: 'POST',
        body: { hello: 'world' },
      });

      expect(fetch).toHaveBeenCalledWith(
        'https://example.com/api',
        expect.objectContaining({
          method: 'POST',
          body: JSON.stringify({ hello: 'world' }),
          headers: { 'content-type': 'application/json' },
        }),
      );
    }),
  );

  it(
    'should forward caller headers and include credentials so cookies are sent',
    withMockedFetch({
      status: 200,
      headers: { 'content-type': 'application/json' },
      body: {},
    })(async () => {
      const fetchPayload = makeApiFetch(z.unknown());
      await fetchPayload({
        url: 'https://example.com/api',
        headers: { Authorization: 'Bearer secret' },
      });

      expect(fetch).toHaveBeenCalledWith(
        'https://example.com/api',
        expect.objectContaining({
          credentials: 'include',
          headers: { Authorization: 'Bearer secret' },
        }),
      );
    }),
  );
});
