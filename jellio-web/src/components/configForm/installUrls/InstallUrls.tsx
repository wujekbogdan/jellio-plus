import { type FC, use } from 'react';
import type { UseFormReturn } from 'react-hook-form';
import type { ConfigFormType } from '@/components/configForm/formSchema';
import { InstallUrlRow } from '@/components/configForm/installUrls/InstallUrlRow';
import {
  buildAppUrl,
  buildConfiguration,
  buildManifestUrl,
  buildWebUrl,
} from '@/components/configForm/installUrls/urls';
import { getBaseUrl } from '@/lib/utils';
import type { ServerInfo } from '@/types';

const HTTP_WARNING =
  'Stremio requires HTTPS addon URLs. Set an HTTPS Public Base URL to enable installation. The only exception is 127.0.0.1.';

const buildUrlSet = ({
  token,
  values,
  serverName,
}: {
  token: string;
  values: ConfigFormType;
  serverName: string;
}) => {
  const base = getBaseUrl(values.publicBaseUrl);
  const configuration = buildConfiguration({ token, values, serverName });
  const manifest = buildManifestUrl({ base, configuration });
  return {
    base,
    manifest,
    web: buildWebUrl(manifest),
    app: buildAppUrl(manifest),
  };
};

const canCopyToClipboard = () =>
  typeof globalThis.navigator?.clipboard?.writeText === 'function';

interface Props {
  tokenPromise: Promise<string>;
  form: UseFormReturn<ConfigFormType>;
  serverInfo: ServerInfo;
}

export const InstallUrls: FC<Props> = ({ tokenPromise, form, serverInfo }) => {
  const token = use(tokenPromise);
  const values = form.watch();
  const canCopy = canCopyToClipboard();
  const urls = buildUrlSet({
    token,
    values,
    serverName: serverInfo.serverName,
  });

  const copy = (url: string) => () => navigator.clipboard.writeText(url);
  const launch = (url: string) => () => {
    window.location.href = url;
  };

  return (
    <>
      {!urls.base.startsWith('https://') && (
        <p className="text-xs text-yellow-600 dark:text-yellow-400">
          {HTTP_WARNING}
        </p>
      )}
      <div className="flex flex-col gap-3">
        <InstallUrlRow
          label="Stremio addon manifest URL"
          url={urls.manifest}
          canCopy={canCopy}
          copy={copy(urls.manifest)}
        />
        <InstallUrlRow
          label="Stremio web installation URL"
          url={urls.web}
          canCopy={canCopy}
          copy={copy(urls.web)}
          onLaunch={launch(urls.web)}
        />
        <InstallUrlRow
          label="Stremio app installation URL"
          url={urls.app}
          canCopy={canCopy}
          copy={copy(urls.app)}
          onLaunch={launch(urls.app)}
        />
      </div>
    </>
  );
};
