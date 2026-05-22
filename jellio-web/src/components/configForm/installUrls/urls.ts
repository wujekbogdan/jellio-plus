import { encode } from 'js-base64';
import type { ConfigFormType } from '@/components/configForm/formSchema';
import { stripTrailingSlash } from '@/lib/utils';
import { HttpError } from '@/services/apiFetch';

const STREMIO_WEB_INSTALL_BASE =
  'https://web.stremio.com/#/addons?addon=' as const;

const insertGuidDashes = (key: string) =>
  key.replace(/^(.{8})(.{4})(.{4})(.{4})(.{12})$/, '$1-$2-$3-$4-$5');

const jellyseerrFields = (values: ConfigFormType) => {
  if (!values.jellyseerrEnabled || !values.jellyseerrUrl) return {};
  return {
    JellyseerrEnabled: true,
    JellyseerrUrl: stripTrailingSlash(values.jellyseerrUrl),
    ...(values.jellyseerrApiKey
      ? { JellyseerrApiKey: values.jellyseerrApiKey }
      : {}),
  };
};

const publicBaseUrlField = (values: ConfigFormType) =>
  values.publicBaseUrl
    ? { PublicBaseUrl: stripTrailingSlash(values.publicBaseUrl) }
    : {};

export const buildConfiguration = ({
  token,
  values,
  serverName,
}: {
  token: string;
  values: ConfigFormType;
  serverName: string;
}) => ({
  AuthToken: token,
  LibrariesGuids: values.libraries.map((library) =>
    insertGuidDashes(library.key),
  ),
  ServerName: serverName,
  ...jellyseerrFields(values),
  ...publicBaseUrlField(values),
});

export type AddonConfiguration = ReturnType<typeof buildConfiguration>;

export const buildManifestUrl = ({
  base,
  configuration,
}: {
  base: string;
  configuration: AddonConfiguration;
}) => `${base}/${encode(JSON.stringify(configuration), true)}/manifest.json`;

export const buildWebUrl = (manifestUrl: string) =>
  `${STREMIO_WEB_INSTALL_BASE}${encodeURIComponent(manifestUrl)}`;

export const buildAppUrl = (manifestUrl: string) =>
  manifestUrl.replace(/^https?:\/\//, 'stremio://');

const extractServerMessage = (body: unknown) => {
  if (body && typeof body === 'object' && 'message' in body) {
    const { message } = body;
    return typeof message === 'string' ? message : null;
  }
  return null;
};

export const formatInstallError = (error: unknown) => {
  if (error instanceof HttpError) {
    const message = extractServerMessage(error.responseBody) ?? error.message;
    return `HTTP ${error.status}: ${message}`;
  }
  if (error instanceof Error) return error.message;
  throw error;
};
