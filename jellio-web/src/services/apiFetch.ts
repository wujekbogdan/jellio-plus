import type { ZodType } from 'zod';

export class HttpError extends Error {
  readonly status: number;
  readonly responseBody: unknown;

  constructor({
    status,
    statusText,
    responseBody,
  }: {
    status: number;
    statusText: string;
    responseBody: unknown;
  }) {
    super(`HTTP ${status} ${statusText}`);
    this.name = 'HttpError';
    this.status = status;
    this.responseBody = responseBody;
  }
}

const parseBody = async (response: Response) => {
  const contentType = response.headers.get('content-type') ?? '';
  if (!contentType.includes('application/json')) return response.text();
  // response.json() is typed as `any` by the DOM lib; downstream zod
  // parsing validates the shape before consumers see it.
  // eslint-disable-next-line @typescript-eslint/no-unsafe-return
  return response.json();
};

export const makeApiFetch =
  <T>(schema: ZodType<T>) =>
  async ({
    url,
    method,
    body,
    headers,
  }: {
    url: string;
    method?: 'GET' | 'POST';
    body?: unknown;
    headers?: Record<string, string>;
  }): Promise<T> => {
    const response = await fetch(url, {
      method,
      body: body === undefined ? undefined : JSON.stringify(body),
      headers: {
        ...(body === undefined ? {} : { 'content-type': 'application/json' }),
        ...headers,
      },
      credentials: 'include',
    });
    const responseBody: unknown = await parseBody(response);
    if (!response.ok) {
      throw new HttpError({
        status: response.status,
        statusText: response.statusText,
        responseBody,
      });
    }
    return schema.parse(responseBody);
  };
