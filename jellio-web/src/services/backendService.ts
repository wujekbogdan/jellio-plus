import axios from 'axios';
import { getBaseUrl, getOrCreateDeviceId } from '@/lib/utils';
import type { Library } from '@/types';

export const getServerInfo = async (
  token?: string,
): Promise<{ serverName: string; libraries: Library[] }> => {
  try {
    const headers: Record<string, string> = {};
    if (token) {
      const deviceId = getOrCreateDeviceId();
      headers.Authorization = `MediaBrowser Token="${token}"`;
      headers['X-Emby-Token'] = token;
      headers['X-Emby-Authorization'] =
        `MediaBrowser Client="Jellio++", Device="Web", DeviceId="${deviceId}", Version="1.5.0", Token="${token}"`;
    }

    const response = await axios.get<{
      name: string;
      libraries: { Name: string; Id: string; CollectionType: string }[];
    }>(`${getBaseUrl()}/server-info`, {
      headers,
      withCredentials: true, // Include cookies for Jellyfin session auth
    });

    return {
      serverName: response.data.name,
      libraries: response.data.libraries.map((lib) => ({
        name: lib.Name,
        key: lib.Id,
        type: lib.CollectionType,
      })),
    };
  } catch (error) {
    console.error('Error while getting server info:', error);
    throw error;
  }
};

export const startAddonSession = async (token?: string): Promise<string> => {
  try {
    const headers: Record<string, string> = {};
    if (token) {
      const deviceId = getOrCreateDeviceId();
      headers.Authorization = `MediaBrowser Token="${token}"`;
      headers['X-Emby-Token'] = token;
      headers['X-Emby-Authorization'] =
        `MediaBrowser Client="Jellio++", Device="Web", DeviceId="${deviceId}", Version="1.5.0", Token="${token}"`;
    }

    const response = await axios.post<{ accessToken: string }>(
      `${getBaseUrl()}/start-session`,
      null,
      {
        headers,
        withCredentials: true,
      },
    );
    return response.data.accessToken;
  } catch (error) {
    console.error('Error starting new session:', error);
    throw error;
  }
};

export interface SaveConfigData {
  jellyseerrEnabled: boolean;
  jellyseerrUrl?: string;
  jellyseerrApiKey?: string;
  publicBaseUrl?: string;
  selectedLibraries?: string[];
}

export const saveConfigToServer = async (
  config: SaveConfigData,
  token?: string,
): Promise<void> => {
  try {
    const headers: Record<string, string> = {};
    if (token) {
      const deviceId = getOrCreateDeviceId();
      headers.Authorization = `MediaBrowser Token="${token}"`;
      headers['X-Emby-Token'] = token;
      headers['X-Emby-Authorization'] =
        `MediaBrowser Client="Jellio++", Device="Web", DeviceId="${deviceId}", Version="1.5.0", Token="${token}"`;
    }

    await axios.post(`${getBaseUrl()}/save-config`, config, {
      headers,
      withCredentials: true,
    });
  } catch (error) {
    console.error('Error saving configuration:', error);
    throw error;
  }
};

export const getConfigFromServer = async (
  token?: string,
): Promise<SaveConfigData> => {
  try {
    const headers: Record<string, string> = {};
    if (token) {
      const deviceId = getOrCreateDeviceId();
      headers.Authorization = `MediaBrowser Token="${token}"`;
      headers['X-Emby-Token'] = token;
      headers['X-Emby-Authorization'] =
        `MediaBrowser Client="Jellio++", Device="Web", DeviceId="${deviceId}", Version="1.5.0", Token="${token}"`;
    }

    const response = await axios.get<SaveConfigData>(
      `${getBaseUrl()}/get-config`,
      {
        headers,
        withCredentials: true,
      },
    );

    return response.data;
  } catch (error) {
    console.error('Error getting configuration:', error);
    throw error;
  }
};

export interface LogEntry {
  timestamp: string;
  message: string;
  level: 'Info' | 'Warning' | 'Error';
}

export const getLogs = async (
  token?: string,
  limit?: number,
): Promise<LogEntry[]> => {
  try {
    const headers: Record<string, string> = {};
    if (token) {
      const deviceId = getOrCreateDeviceId();
      headers.Authorization = `MediaBrowser Token="${token}"`;
      headers['X-Emby-Token'] = token;
      headers['X-Emby-Authorization'] =
        `MediaBrowser Client="Jellio++", Device="Web", DeviceId="${deviceId}", Version="1.5.0", Token="${token}"`;
    }

    const params = limit ? { limit } : {};
    const response = await axios.get<{ logs?: LogEntry[] }>(
      `${getBaseUrl()}/logs`,
      {
        headers,
        params,
        withCredentials: true,
      },
    );

    return response.data.logs ?? [];
  } catch (error) {
    console.error('Error getting logs:', error);
    throw error;
  }
};

export const clearLogs = async (token?: string): Promise<void> => {
  try {
    const headers: Record<string, string> = {};
    if (token) {
      const deviceId = getOrCreateDeviceId();
      headers.Authorization = `MediaBrowser Token="${token}"`;
      headers['X-Emby-Token'] = token;
      headers['X-Emby-Authorization'] =
        `MediaBrowser Client="Jellio++", Device="Web", DeviceId="${deviceId}", Version="1.5.0", Token="${token}"`;
    }

    await axios.post(`${getBaseUrl()}/logs/clear`, null, {
      headers,
      withCredentials: true,
    });
  } catch (error) {
    console.error('Error clearing logs:', error);
    throw error;
  }
};
