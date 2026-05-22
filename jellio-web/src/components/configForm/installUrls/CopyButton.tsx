import { type FC, useEffect, useRef, useState } from 'react';
import { Copy } from 'lucide-react';
import { Button } from '@/components/ui/button';

interface Props {
  onCopy: () => Promise<void>;
  onError?: (error: unknown) => void;
  ariaLabel: string;
}

const COPIED_FLASH_MS = 2000;

export const CopyButton: FC<Props> = ({ onCopy, onError, ariaLabel }) => {
  const [copied, setCopied] = useState(false);
  const timerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  useEffect(
    () => () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    },
    [],
  );

  const onClick = () => {
    onCopy()
      .then(() => {
        if (timerRef.current) clearTimeout(timerRef.current);
        setCopied(true);
        timerRef.current = setTimeout(() => setCopied(false), COPIED_FLASH_MS);
      })
      .catch((error: unknown) => onError?.(error));
  };

  return (
    <Button
      type="button"
      variant="outline"
      onClick={onClick}
      aria-label={ariaLabel}
      className="cursor-pointer"
    >
      <Copy className="h-4 w-4" />
      {copied ? 'Copied' : 'Copy'}
    </Button>
  );
};
