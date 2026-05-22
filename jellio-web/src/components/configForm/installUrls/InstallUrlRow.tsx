import { type FC, useState } from 'react';
import { ExternalLink } from 'lucide-react';
import { CopyButton } from '@/components/configForm/installUrls/CopyButton';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';

interface Props {
  label: string;
  url: string;
  canCopy: boolean;
  copy: () => Promise<void>;
  onLaunch?: () => void;
}

export const InstallUrlRow: FC<Props> = ({
  label,
  url,
  canCopy,
  copy,
  onLaunch,
}) => {
  const [copyFailed, setCopyFailed] = useState(false);
  const onCopy = async () => {
    setCopyFailed(false);
    await copy();
  };
  const onError = () => setCopyFailed(true);
  return (
    <div className="flex flex-col gap-1">
      <Label>{label}</Label>
      <div className="flex items-center gap-2">
        <Input
          readOnly
          value={url}
          onFocus={(event) => event.currentTarget.select()}
          onClick={(event) => event.currentTarget.select()}
          className="font-mono text-xs cursor-pointer"
        />
        {canCopy && (
          <CopyButton
            onCopy={onCopy}
            onError={onError}
            ariaLabel={`Copy ${label}`}
          />
        )}
        {onLaunch && (
          <Button
            type="button"
            variant="outline"
            onClick={onLaunch}
            aria-label={`Install via ${label}`}
          >
            <ExternalLink className="h-4 w-4" />
            Install
          </Button>
        )}
      </div>
      {copyFailed && (
        <p className="text-xs text-destructive">
          Copy failed — select the URL above and copy manually.
        </p>
      )}
    </div>
  );
};
