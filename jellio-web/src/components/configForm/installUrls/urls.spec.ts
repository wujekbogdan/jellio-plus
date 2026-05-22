import { decode } from 'js-base64';
import { describe, expect, it } from 'vitest';
import {
  buildAppUrl,
  buildConfiguration,
  buildManifestUrl,
  buildWebUrl,
  formatInstallError,
} from './urls';
import { HttpError } from '@/services/apiFetch';

describe('buildConfiguration', () => {
  it('should include AuthToken, LibrariesGuids and ServerName', () => {
    const configuration = buildConfiguration({
      token: 'token-xyz',
      values: {
        serverName: 'ignored',
        libraries: [
          {
            key: 'aabbccdd11223344eeff00112233aabb',
            name: 'Movies',
            type: 'movies',
          },
        ],
        jellyseerrEnabled: false,
        jellyseerrUrl: '',
        jellyseerrApiKey: '',
        publicBaseUrl: '',
      },
      serverName: 'My Server',
    });

    expect(configuration).toEqual({
      AuthToken: 'token-xyz',
      LibrariesGuids: ['aabbccdd-1122-3344-eeff-00112233aabb'],
      ServerName: 'My Server',
    });
  });

  it('should include PublicBaseUrl with trailing slashes stripped', () => {
    const configuration = buildConfiguration({
      token: 'tk',
      values: {
        serverName: '',
        libraries: [],
        jellyseerrEnabled: false,
        jellyseerrUrl: '',
        jellyseerrApiKey: '',
        publicBaseUrl: 'https://jellyfin.example.com//',
      },
      serverName: 'srv',
    });

    expect(configuration).toMatchObject({
      PublicBaseUrl: 'https://jellyfin.example.com',
    });
  });

  it('should omit PublicBaseUrl when empty', () => {
    const configuration = buildConfiguration({
      token: 'tk',
      values: {
        serverName: '',
        libraries: [],
        jellyseerrEnabled: false,
        jellyseerrUrl: '',
        jellyseerrApiKey: '',
        publicBaseUrl: '',
      },
      serverName: 'srv',
    });

    expect(configuration).not.toHaveProperty('PublicBaseUrl');
  });

  it('should include Jellyseerr fields when enabled and url provided', () => {
    const configuration = buildConfiguration({
      token: 'tk',
      values: {
        serverName: '',
        libraries: [],
        jellyseerrEnabled: true,
        jellyseerrUrl: 'https://jellyseerr.example.com/',
        jellyseerrApiKey: 'secret-key',
        publicBaseUrl: '',
      },
      serverName: 'srv',
    });

    expect(configuration).toMatchObject({
      JellyseerrEnabled: true,
      JellyseerrUrl: 'https://jellyseerr.example.com',
      JellyseerrApiKey: 'secret-key',
    });
  });
});

describe('buildManifestUrl', () => {
  it('should embed base64url-encoded configuration before /manifest.json', () => {
    const configuration = {
      AuthToken: 'tk',
      LibrariesGuids: [],
      ServerName: 's',
    };
    const url = buildManifestUrl({
      base: 'https://jellyfin.example.com/jelliopp',
      configuration,
    });

    const encoded = url
      .replace('https://jellyfin.example.com/jelliopp/', '')
      .replace('/manifest.json', '');
    expect(JSON.parse(decode(encoded))).toEqual(configuration);
  });
});

describe('buildWebUrl', () => {
  it('should wrap the manifest URL in the Stremio web installer', () => {
    const url = buildWebUrl(
      'https://jellyfin.example.com/jelliopp/x/manifest.json',
    );
    expect(url).toBe(
      'https://web.stremio.com/#/addons?addon=https%3A%2F%2Fjellyfin.example.com%2Fjelliopp%2Fx%2Fmanifest.json',
    );
  });
});

describe('buildAppUrl', () => {
  it('should replace http and https schemes with stremio://', () => {
    expect(buildAppUrl('https://example.com/m')).toBe(
      'stremio://example.com/m',
    );
    expect(buildAppUrl('http://example.com/m')).toBe('stremio://example.com/m');
  });
});

describe('formatInstallError', () => {
  it('should prefix HttpError with status and surface the server message when present', () => {
    const error = new HttpError({
      status: 500,
      statusText: 'Internal Server Error',
      responseBody: { message: 'database is on fire' },
    });

    expect(formatInstallError(error)).toBe('HTTP 500: database is on fire');
  });

  it('should fall back to error.message for plain Error instances', () => {
    expect(formatInstallError(new Error('network down'))).toBe('network down');
  });
});
