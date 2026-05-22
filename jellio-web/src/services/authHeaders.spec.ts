import { describe, expect, it } from 'vitest';
import { buildAuthHeaders } from './authHeaders';

describe('buildAuthHeaders', () => {
  it('should build the Jellyfin MediaBrowser auth headers from a token and device id', () => {
    const headers = buildAuthHeaders({ token: 'abc123', deviceId: 'dev-1' });

    expect(headers).toEqual({
      Authorization: 'MediaBrowser Token="abc123"',
      'X-Emby-Token': 'abc123',
      'X-Emby-Authorization':
        'MediaBrowser Client="Jellio++", Device="Web", DeviceId="dev-1", Version="1.5.0", Token="abc123"',
    });
  });
});
