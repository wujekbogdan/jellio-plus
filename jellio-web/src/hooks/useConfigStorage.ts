import { useEffect } from 'react';
import type { UseFormReturn } from 'react-hook-form';
import { getConfigFromServer } from '@/services/backendService';

const STORAGE_KEY = 'jelliopp_config';

interface StoredConfig {
  libraries?: Array<{ key: string; name: string; type: string }>;
  jellyseerrEnabled?: boolean;
  jellyseerrUrl?: string;
  jellyseerrApiKey?: string;
  publicBaseUrl?: string;
}

export const useConfigStorage = (form: UseFormReturn<any>, accessToken?: string, availableLibraries?: Array<{ key: string; name: string; type: string }>) => {
  // Load config from localStorage and server on mount
  useEffect(() => {
    const loadConfig = async () => {
      try {
        // First try localStorage (faster)
        const stored = localStorage.getItem(STORAGE_KEY);
        if (stored) {
          const config: StoredConfig = JSON.parse(stored);
          
          // Update form with stored values
          if (config.libraries && Array.isArray(config.libraries)) {
            form.setValue('libraries', config.libraries);
          }
          if (config.jellyseerrEnabled !== undefined) {
            form.setValue('jellyseerrEnabled', config.jellyseerrEnabled);
          }
          if (config.jellyseerrUrl) {
            form.setValue('jellyseerrUrl', config.jellyseerrUrl);
          }
          if (config.jellyseerrApiKey) {
            form.setValue('jellyseerrApiKey', config.jellyseerrApiKey);
          }
          if (config.publicBaseUrl) {
            form.setValue('publicBaseUrl', config.publicBaseUrl);
          }
        }

        // Then try server config (may override localStorage)
        if (accessToken) {
          const serverConfig = await getConfigFromServer(accessToken);
          if (serverConfig) {
            if (serverConfig.jellyseerrEnabled !== undefined) {
              form.setValue('jellyseerrEnabled', serverConfig.jellyseerrEnabled);
            }
            if (serverConfig.jellyseerrUrl) {
              form.setValue('jellyseerrUrl', serverConfig.jellyseerrUrl);
            }
            if (serverConfig.jellyseerrApiKey) {
              form.setValue('jellyseerrApiKey', serverConfig.jellyseerrApiKey);
            }
            if (serverConfig.publicBaseUrl) {
              form.setValue('publicBaseUrl', serverConfig.publicBaseUrl);
            }
            // Load libraries from server config if available
            if (serverConfig.selectedLibraries && Array.isArray(serverConfig.selectedLibraries) && availableLibraries) {
              // Convert server library IDs (format: "guidwithoutdashes") to form format
              // Match them with available libraries
              const selectedLibraries = serverConfig.selectedLibraries
                .map((id: string) => {
                  // Convert "guidwithoutdashes" to "guid-with-dashes" format
                  let formattedId = id;
                  if (id.length === 32) {
                    formattedId = `${id.slice(0, 8)}-${id.slice(8, 12)}-${id.slice(12, 16)}-${id.slice(16, 20)}-${id.slice(20, 32)}`;
                  }
                  // Find matching library
                  return availableLibraries.find(lib => lib.key === formattedId);
                })
                .filter((lib): lib is { key: string; name: string; type: string } => lib !== undefined);
              
              if (selectedLibraries.length > 0) {
                form.setValue('libraries', selectedLibraries);
              }
            }
          }
        }
      } catch (error) {
        console.error('Failed to load config:', error);
      }
    };

    loadConfig();
  }, [form, accessToken, availableLibraries]);

  // Save config to localStorage
  const saveConfig = () => {
    try {
      const values = form.getValues();
      
      // Strip trailing slashes from URLs before saving
      const stripTrailingSlash = (url: string) => url?.replace(/\/+$/, '') || '';
      
      const config: StoredConfig = {
        libraries: values.libraries || [],
        jellyseerrEnabled: values.jellyseerrEnabled,
        jellyseerrUrl: stripTrailingSlash(values.jellyseerrUrl),
        jellyseerrApiKey: values.jellyseerrApiKey,
        publicBaseUrl: stripTrailingSlash(values.publicBaseUrl),
      };
      
      localStorage.setItem(STORAGE_KEY, JSON.stringify(config));
      return true;
    } catch (error) {
      console.error('Failed to save config to localStorage:', error);
      return false;
    }
  };

  return { saveConfig };
};
