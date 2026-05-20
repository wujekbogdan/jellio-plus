import type { FC } from 'react';
import type { UseFormReturn } from 'react-hook-form';
import type { ConfigFormType } from '@/components/configForm/formSchema.tsx';
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

export const ServerNameField: FC<Props> = ({ form }) => {
  return (
    <FormField
      control={form.control}
      name="serverName"
      render={({ field }) => (
        <FormItem className="rounded-lg border p-2">
          <FormLabel className="text-base">Server name</FormLabel>
          <FormControl>
            <Input {...field} />
          </FormControl>
          <FormDescription>
            Friendly server name for display in the Stremio UI.
          </FormDescription>
          <FormMessage />
        </FormItem>
      )}
    />
  );
};
