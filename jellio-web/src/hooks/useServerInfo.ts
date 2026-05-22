import { useEffect, useRef, useState } from 'react';
import useAccessToken from '@/hooks/useAccessToken.ts';
import { HttpError } from '@/services/apiFetch';
import { getServerInfo } from '@/services/backendService.ts';
import type { ServerInfo, Maybe } from '@/types';

const useServerInfo = (): Maybe<ServerInfo> => {
  const accessToken = useAccessToken();
  const [serverInfo, setServerInfo] = useState<ServerInfo | null | undefined>();

  const attemptedOnceRef = useRef(false);

  useEffect(() => {
    let cancelled = false;
    const fetchServerInfo = async (): Promise<void> => {
      try {
        const info = await getServerInfo({ token: accessToken ?? undefined });
        if (cancelled) return;
        setServerInfo({ accessToken: accessToken ?? '', ...info });
      } catch (error: unknown) {
        if (cancelled) return;
        const status = error instanceof HttpError ? error.status : undefined;
        if (status === 401 || status === 403) {
          setServerInfo(null);
          return;
        }
        console.warn(
          'Non-auth error fetching server info (will not redirect):',
          error,
        );
        // Keep in loading state to avoid redirect loop; retry once quickly
        if (!attemptedOnceRef.current) {
          attemptedOnceRef.current = true;
          setTimeout(() => {
            if (!cancelled) void fetchServerInfo();
          }, 400);
        } else {
          setServerInfo(undefined);
        }
      }
    };

    void fetchServerInfo();
    return () => {
      cancelled = true;
    };
  }, [accessToken]);

  return serverInfo;
};

export default useServerInfo;
