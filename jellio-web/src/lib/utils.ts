import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

export const stripTrailingSlash = (url: string) => url.replace(/\/+$/, '');

export function getBaseUrl(publicBaseUrl?: string): string {
  if (publicBaseUrl && publicBaseUrl.length > 0) {
    return `${stripTrailingSlash(publicBaseUrl)}/jelliopp`;
  }
  const match = /.*?\/jelliopp/.exec(window.location.href);
  if (!match) {
    throw new Error('URL must include /jelliopp');
  }
  return match[0];
}

export function getOrCreateDeviceId(): string {
  const key = 'jelliopp_device_id';
  let id = localStorage.getItem(key);
  if (!id) {
    // Simple, stable identifier for header metadata
    id = crypto.randomUUID
      ? crypto.randomUUID()
      : `${Date.now()}-${Math.random().toString(36).slice(2)}`;
    localStorage.setItem(key, id);
  }
  return id;
}
