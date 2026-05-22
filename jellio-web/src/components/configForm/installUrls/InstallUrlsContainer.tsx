import { Suspense, useCallback, useState } from 'react';
import { ErrorBoundary } from 'react-error-boundary';
import type { UseFormReturn } from 'react-hook-form';
import type { ConfigFormType } from '@/components/configForm/formSchema';
import { InstallUrls } from '@/components/configForm/installUrls/InstallUrls';
import { formatInstallError } from '@/components/configForm/installUrls/urls';
import { Button } from '@/components/ui/button';
import { Label } from '@/components/ui/label';
import { startAddonSession } from '@/services/backendService';
import type { ServerInfo } from '@/types';

const LoadingMessage = () => (
  <p className="text-xs text-muted-foreground">Generating install URLs…</p>
);

const ErrorMessage = ({
  error,
  onRetry,
}: {
  error: unknown;
  onRetry: () => void;
}) => (
  <div className="flex items-center gap-2">
    <p className="text-xs text-destructive">
      Could not generate install URL: {formatInstallError(error)}
    </p>
    <Button type="button" variant="outline" size="sm" onClick={onRetry}>
      Retry
    </Button>
  </div>
);

export const InstallUrlsContainer = ({
  form,
  serverInfo,
}: {
  form: UseFormReturn<ConfigFormType>;
  serverInfo: ServerInfo;
}) => {
  const [tokenPromise, setTokenPromise] = useState(() =>
    startAddonSession({ token: serverInfo.accessToken }),
  );
  const onRetry = useCallback(
    () => setTokenPromise(startAddonSession({ token: serverInfo.accessToken })),
    [serverInfo.accessToken],
  );
  return (
    <div className="rounded-lg border p-2 space-y-2">
      <Label className="text-base">Install</Label>
      <ErrorBoundary
        resetKeys={[tokenPromise]}
        fallbackRender={({ error }) => (
          <ErrorMessage error={error} onRetry={onRetry} />
        )}
      >
        <Suspense fallback={<LoadingMessage />}>
          <InstallUrls
            tokenPromise={tokenPromise}
            form={form}
            serverInfo={serverInfo}
          />
        </Suspense>
      </ErrorBoundary>
    </div>
  );
};
