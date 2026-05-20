import type { FC } from 'react';
import type { UseFormReturn } from 'react-hook-form';
import type { ConfigFormType } from '@/components/configForm/formSchema.tsx';
import { Checkbox } from '@/components/ui/checkbox.tsx';
import {
  FormControl,
  FormDescription,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form.tsx';
import { Input } from '@/components/ui/input.tsx';

interface Props {
  form: UseFormReturn<ConfigFormType>;
}

export const JellyseerrFieldset: FC<Props> = ({ form }) => {
  const enabled = form.watch('jellyseerrEnabled');
  return (
    <fieldset className="rounded-lg border p-2">
      <legend className="px-1 text-base font-medium">
        Jellyseerr integration
      </legend>
      <FormField
        control={form.control}
        name="jellyseerrEnabled"
        render={({ field }) => (
          <FormItem className="flex items-center justify-between py-2">
            <div>
              <FormLabel>Enable Jellyseerr</FormLabel>
              <FormDescription>
                Show a "Request via Jellyseerr" option when no stream is
                available.
              </FormDescription>
            </div>
            <FormControl>
              <Checkbox
                checked={field.value}
                onCheckedChange={field.onChange}
              />
            </FormControl>
            <FormMessage />
          </FormItem>
        )}
      />

      {enabled && (
        <div className="grid grid-cols-1 gap-3 md:grid-cols-2">
          <FormField
            control={form.control}
            name="jellyseerrUrl"
            render={({ field }) => (
              <FormItem className="flex flex-col">
                <FormLabel>Jellyseerr Base URL</FormLabel>
                <FormControl>
                  <Input
                    placeholder="https://jellyseerr.example.com"
                    {...field}
                  />
                </FormControl>
                <FormDescription className="text-xs min-h-[2.5rem]">
                  <strong>
                    Use your local address. Do not include a trailing slash.
                    (ex. "http://192.168.0.105:5055")
                  </strong>
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="jellyseerrApiKey"
            render={({ field }) => (
              <FormItem className="flex flex-col">
                <FormLabel>Jellyseerr API Key</FormLabel>
                <FormControl>
                  <Input
                    type="password"
                    placeholder="paste API key"
                    {...field}
                  />
                </FormControl>
                <FormDescription className="text-xs min-h-[2.5rem]">
                  Required for automatic API requests.
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
          <FormField
            control={form.control}
            name="publicBaseUrl"
            render={({ field }) => (
              <FormItem className="md:col-span-2">
                <FormLabel>Public Base URL</FormLabel>
                <FormControl>
                  <Input
                    placeholder="https://jellyfin.example.com"
                    {...field}
                  />
                </FormControl>
                <FormDescription className="text-xs">
                  <strong>
                    Use your Cloudflare Tunnel address to access Jellyfin when
                    not on your local network. (ex.
                    "https://jellyfin.example.com")
                  </strong>
                </FormDescription>
                <FormMessage />
              </FormItem>
            )}
          />
        </div>
      )}
    </fieldset>
  );
};
