import type { FC } from 'react';
import { useState } from 'react';
import { zodResolver } from '@hookform/resolvers/zod';
import { Save } from 'lucide-react';
import { useForm } from 'react-hook-form';
import {
  LibrariesField,
  ServerNameField,
  JellyseerrFieldset,
  PublicBaseUrlField,
} from '@/components/configForm/fields';
import { formSchema } from '@/components/configForm/formSchema.tsx';
import { InstallUrlsContainer } from '@/components/configForm/installUrls/InstallUrlsContainer';
import { LogsViewer } from '@/components/logsViewer';
import { Button } from '@/components/ui/button.tsx';
import { Form } from '@/components/ui/form';
import { useConfigStorage } from '@/hooks/useConfigStorage';
import { stripTrailingSlash } from '@/lib/utils';
import { saveConfigToServer } from '@/services/backendService';
import type { ServerInfo } from '@/types';

interface Props {
  serverInfo: ServerInfo;
}

const ConfigForm: FC<Props> = ({ serverInfo }) => {
  const [saving, setSaving] = useState(false);
  const [saved, setSaved] = useState(false);

  const form = useForm({
    resolver: zodResolver(formSchema),
    defaultValues: {
      serverName: serverInfo.serverName,
      libraries: [],
      jellyseerrEnabled: false,
      jellyseerrUrl: '',
      jellyseerrApiKey: '',
      publicBaseUrl: '',
    },
  });

  const { saveConfig } = useConfigStorage(
    form,
    serverInfo.accessToken,
    serverInfo.libraries,
  );

  const serverName = form.watch('serverName');

  const handleSaveToServer = async () => {
    setSaving(true);
    setSaved(false);
    try {
      const values = form.getValues();
      saveConfig();
      await saveConfigToServer({
        config: {
          jellyseerrEnabled: values.jellyseerrEnabled ?? false,
          jellyseerrUrl: stripTrailingSlash(values.jellyseerrUrl ?? ''),
          jellyseerrApiKey: values.jellyseerrApiKey ?? '',
          publicBaseUrl: stripTrailingSlash(values.publicBaseUrl ?? ''),
          selectedLibraries:
            values.libraries?.map((lib: { key: string }) =>
              lib.key.replace(/-/g, ''),
            ) ?? [],
        },
        token: serverInfo.accessToken,
      });

      setSaved(true);
      setTimeout(() => setSaved(false), 2000);
    } catch (error) {
      console.error('Failed to save configuration:', error);
      alert('Failed to save configuration to server');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Form {...form}>
      <form className="space-y-2 p-2 rounded-lg border">
        <ServerNameField form={form} />
        <LibrariesField
          form={form}
          serverName={serverName}
          libraries={serverInfo.libraries}
        />
        <PublicBaseUrlField form={form} />
        <JellyseerrFieldset form={form} />
        <InstallUrlsContainer form={form} serverInfo={serverInfo} />
        <div className="p-3">
          <LogsViewer accessToken={serverInfo.accessToken} />
        </div>
        <div className="flex flex-col items-center justify-center gap-2 p-3">
          <Button
            type="button"
            variant="outline"
            className="w-full max-w-md"
            onClick={() => void handleSaveToServer()}
            disabled={saving}
          >
            <Save className="mr-2 h-4 w-4" />
            {saved
              ? 'Saved to Jellyfin!'
              : saving
                ? 'Saving...'
                : 'Save Configuration to Jellyfin'}
          </Button>
        </div>
      </form>
    </Form>
  );
};

export default ConfigForm;
