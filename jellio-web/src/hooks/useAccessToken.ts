import { useEffect, useState } from 'react';
import { z } from 'zod';
import type { Maybe } from '@/types';

const credentialsSchema = z.object({
  Servers: z.array(z.object({ AccessToken: z.string().optional() })).optional(),
  AccessToken: z.string().optional(),
});

const useAccessToken = (): Maybe<string> => {
  const [accessToken, setAccessToken] = useState<Maybe<string>>();

  useEffect(() => {
    try {
      const stored = localStorage.getItem('jellyfin_credentials');
      if (!stored) {
        setAccessToken(null);
        return;
      }

      const parsed = credentialsSchema.safeParse(JSON.parse(stored));
      if (!parsed.success) {
        setAccessToken(null);
        return;
      }

      // Case 1: Web client-style credentials: { Servers: [{ AccessToken }...] }
      const withToken = parsed.data.Servers?.find((s) => s.AccessToken);
      if (withToken?.AccessToken) {
        setAccessToken(withToken.AccessToken);
        return;
      }

      // Case 2: Direct session-style: { AccessToken: "..." }
      if (parsed.data.AccessToken) {
        setAccessToken(parsed.data.AccessToken);
        return;
      }

      setAccessToken(null);
    } catch {
      setAccessToken(null);
    }
  }, []);

  return accessToken;
};

export default useAccessToken;
