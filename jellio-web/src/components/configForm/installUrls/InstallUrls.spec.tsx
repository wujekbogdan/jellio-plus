import { act } from 'react';
import { cleanup, render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { decode } from 'js-base64';
import { useForm } from 'react-hook-form';
import { describe, expect, it, vi, type MockInstance } from 'vitest';
import { InstallUrlsContainer } from './InstallUrlsContainer';
import type { ConfigFormType } from '@/components/configForm/formSchema';
import { HttpError } from '@/services/apiFetch';
import { flushAct } from '@/test-utils/flushAct';

vi.mock('@/services/backendService', () => ({
  startAddonSession: vi.fn(),
}));

vi.mock('@/lib/utils', async () => {
  const actual =
    await vi.importActual<typeof import('@/lib/utils')>('@/lib/utils');
  return {
    ...actual,
    getOrCreateDeviceId: () => 'test-device',
  };
});

const { startAddonSession } = await import('@/services/backendService');
const mockedStartAddonSession = vi.mocked(startAddonSession);

const buildDefaults = (
  overrides: Partial<ConfigFormType> = {},
): ConfigFormType => ({
  serverName: 'Home Jellyfin',
  libraries: [
    { key: 'aabbccdd11223344eeff00112233aabb', name: 'Movies', type: 'movies' },
  ],
  jellyseerrEnabled: false,
  jellyseerrUrl: '',
  jellyseerrApiKey: '',
  publicBaseUrl: 'https://jellyfin.example.com',
  ...overrides,
});

const Host = ({ defaultValues }: { defaultValues: ConfigFormType }) => {
  const form = useForm<ConfigFormType>({ defaultValues });
  return (
    <InstallUrlsContainer
      form={form}
      serverInfo={{
        accessToken: 'init-token',
        serverName: defaultValues.serverName,
        libraries: defaultValues.libraries,
      }}
    />
  );
};

const decodeConfigFromUrl = (url: string): unknown => {
  const encoded = url
    .replace(/^.*\/jelliopp\//, '')
    .replace(/\/manifest\.json$/, '');
  return JSON.parse(decode(encoded));
};

type ClipboardSpy = MockInstance<(text: string) => Promise<void>>;

const withInstallUrlsHarness =
  (
    test: (helpers: {
      clipboardSpy: ClipboardSpy;
      renderHost: (overrides?: Partial<ConfigFormType>) => Promise<void>;
    }) => Promise<void> | void,
  ) =>
  async () => {
    const clipboardSpy: ClipboardSpy = vi
      .fn<(text: string) => Promise<void>>()
      .mockResolvedValue(undefined);
    vi.stubGlobal('navigator', {
      ...navigator,
      clipboard: { writeText: clipboardSpy },
    });
    const renderHost = (overrides?: Partial<ConfigFormType>) =>
      flushAct(() => {
        render(<Host defaultValues={buildDefaults(overrides)} />);
      });
    try {
      await test({ clipboardSpy, renderHost });
    } finally {
      cleanup();
      vi.unstubAllGlobals();
      vi.clearAllMocks();
      expect.hasAssertions();
    }
  };

describe('InstallUrls', () => {
  it(
    'should fetch a session token on mount and copy the displayed manifest URL when Copy is clicked',
    withInstallUrlsHarness(async ({ clipboardSpy, renderHost }) => {
      mockedStartAddonSession.mockResolvedValueOnce('SENTINEL_TOKEN');
      await renderHost();

      const copyButton = await screen.findByRole('button', {
        name: /copy stremio addon manifest url/i,
      });

      const manifestInput = screen.getByDisplayValue(
        /^https:\/\/jellyfin\.example\.com\/jelliopp\/.+\/manifest\.json$/,
      );
      const displayedUrl = manifestInput.getAttribute('value') ?? '';
      expect(decodeConfigFromUrl(displayedUrl)).toMatchObject({
        AuthToken: 'SENTINEL_TOKEN',
        ServerName: 'Home Jellyfin',
        PublicBaseUrl: 'https://jellyfin.example.com',
      });

      await userEvent.click(copyButton);

      expect(mockedStartAddonSession).toHaveBeenCalledWith({
        token: 'init-token',
      });
      expect(clipboardSpy).toHaveBeenCalledOnce();
      expect(clipboardSpy.mock.calls[0][0]).toBe(displayedUrl);
    }),
  );

  it(
    'should show the HTTP error inline and hide install URLs when starting a new addon session fails',
    withInstallUrlsHarness(async ({ clipboardSpy, renderHost }) => {
      mockedStartAddonSession.mockRejectedValueOnce(
        new HttpError({
          status: 503,
          statusText: 'Service Unavailable',
          responseBody: { message: 'Jellyfin is restarting' },
        }),
      );
      await renderHost();

      await screen.findByText(/HTTP 503: Jellyfin is restarting/i);
      expect(
        screen.queryByRole('button', {
          name: /copy stremio addon manifest url/i,
        }),
      ).toBeNull();
      expect(clipboardSpy).not.toHaveBeenCalled();
    }),
  );

  it(
    'should warn the user when the install base URL is not HTTPS',
    withInstallUrlsHarness(async ({ renderHost }) => {
      mockedStartAddonSession.mockResolvedValueOnce('tk');
      await renderHost({ publicBaseUrl: 'http://jellyfin.lan' });

      expect(
        await screen.findByText(/Stremio requires HTTPS addon URLs/i),
      ).toBeTruthy();
    }),
  );

  it(
    'should not warn when the install base URL is HTTPS',
    withInstallUrlsHarness(async ({ renderHost }) => {
      mockedStartAddonSession.mockResolvedValueOnce('tk');
      await renderHost();

      await screen.findByRole('button', {
        name: /copy stremio addon manifest url/i,
      });
      expect(
        screen.queryByText(/Stremio requires HTTPS addon URLs/i),
      ).toBeNull();
    }),
  );

  it(
    'should flash a Copied confirmation after a successful clipboard write',
    withInstallUrlsHarness(async ({ renderHost }) => {
      mockedStartAddonSession.mockResolvedValueOnce('tk');
      await renderHost();

      const copyButton = await screen.findByRole('button', {
        name: /copy stremio addon manifest url/i,
      });
      await userEvent.click(copyButton);
      expect(await screen.findByText(/^copied$/i)).toBeTruthy();
    }),
  );

  it(
    'should show a copy-failed message when clipboard.writeText rejects',
    withInstallUrlsHarness(async ({ clipboardSpy, renderHost }) => {
      mockedStartAddonSession.mockResolvedValueOnce('tk');
      await renderHost();

      clipboardSpy.mockRejectedValueOnce(new Error('denied'));
      const copyButton = await screen.findByRole('button', {
        name: /copy stremio addon manifest url/i,
      });
      await userEvent.click(copyButton);
      expect(await screen.findByText(/copy failed/i)).toBeTruthy();
    }),
  );

  it(
    'should refetch a session token when Retry is clicked after a failure',
    withInstallUrlsHarness(async ({ renderHost }) => {
      mockedStartAddonSession.mockRejectedValueOnce(
        new HttpError({
          status: 503,
          statusText: 'Service Unavailable',
          responseBody: { message: 'Jellyfin is restarting' },
        }),
      );
      await renderHost();

      await screen.findByText(/HTTP 503: Jellyfin is restarting/i);
      mockedStartAddonSession.mockResolvedValueOnce('SECOND_TOKEN');

      await act(async () => {
        await userEvent.click(screen.getByRole('button', { name: /retry/i }));
      });

      const manifestInput = await screen.findByDisplayValue(
        /^https:\/\/jellyfin\.example\.com\/jelliopp\/.+\/manifest\.json$/,
      );
      expect(
        decodeConfigFromUrl(manifestInput.getAttribute('value') ?? ''),
      ).toMatchObject({ AuthToken: 'SECOND_TOKEN' });
      expect(mockedStartAddonSession).toHaveBeenCalledTimes(2);
    }),
  );
});
