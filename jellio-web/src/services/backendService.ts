import { z } from 'zod';
import { getBaseUrl, getOrCreateDeviceId } from '@/lib/utils';
import { makeApiFetch } from '@/services/apiFetch';
import { buildAuthHeaders } from '@/services/authHeaders';
import type { Library } from '@/types';

const headersFor = (token: string | undefined) =>
  token
    ? buildAuthHeaders({ token, deviceId: getOrCreateDeviceId() })
    : undefined;

const serverInfoResponseSchema = z.object({
  name: z.string(),
  libraries: z.array(
    z.object({
      Name: z.string(),
      Id: z.string(),
      CollectionType: z.string(),
    }),
  ),
});

const fetchServerInfoResponse = makeApiFetch(serverInfoResponseSchema);

export const getServerInfo = async ({
  token,
}: {
  token?: string;
}): Promise<{ serverName: string; libraries: Library[] }> => {
  const response = await fetchServerInfoResponse({
    url: `${getBaseUrl()}/server-info`,
    headers: headersFor(token),
  });
  return {
    serverName: response.name,
    libraries: response.libraries.map((library) => ({
      name: library.Name,
      key: library.Id,
      type: library.CollectionType,
    })),
  };
};

const startSessionResponseSchema = z.object({ accessToken: z.string() });
const fetchStartSession = makeApiFetch(startSessionResponseSchema);

export const startAddonSession = async ({ token }: { token?: string }) => {
  const response = await fetchStartSession({
    url: `${getBaseUrl()}/start-session`,
    method: 'POST',
    headers: headersFor(token),
  });
  return response.accessToken;
};

export const saveConfigDataSchema = z.object({
  jellyseerrEnabled: z.boolean(),
  jellyseerrUrl: z.string().optional(),
  jellyseerrApiKey: z.string().optional(),
  publicBaseUrl: z.string().optional(),
  selectedLibraries: z.array(z.string()).optional(),
});

export type SaveConfigData = z.infer<typeof saveConfigDataSchema>;

const fetchSaveConfigAck = makeApiFetch(z.unknown());

export const saveConfigToServer = async ({
  config,
  token,
}: {
  config: SaveConfigData;
  token?: string;
}) => {
  await fetchSaveConfigAck({
    url: `${getBaseUrl()}/save-config`,
    method: 'POST',
    body: config,
    headers: headersFor(token),
  });
};

const fetchConfig = makeApiFetch(saveConfigDataSchema.partial());

export const getConfigFromServer = ({ token }: { token?: string }) =>
  fetchConfig({
    url: `${getBaseUrl()}/get-config`,
    headers: headersFor(token),
  });

const logEntrySchema = z.object({
  timestamp: z.string(),
  message: z.string(),
  level: z.enum(['Info', 'Warning', 'Error']),
});

export type LogEntry = z.infer<typeof logEntrySchema>;

const logsResponseSchema = z.object({
  logs: z.array(logEntrySchema).optional(),
});

const fetchLogsResponse = makeApiFetch(logsResponseSchema);

export const getLogs = async ({
  token,
  limit,
}: {
  token?: string;
  limit?: number;
}) => {
  const query = limit === undefined ? '' : `?limit=${limit}`;
  const response = await fetchLogsResponse({
    url: `${getBaseUrl()}/logs${query}`,
    headers: headersFor(token),
  });
  return response.logs ?? [];
};

const fetchClearLogsAck = makeApiFetch(z.unknown());

export const clearLogs = async ({ token }: { token?: string }) => {
  await fetchClearLogsAck({
    url: `${getBaseUrl()}/logs/clear`,
    method: 'POST',
    headers: headersFor(token),
  });
};
