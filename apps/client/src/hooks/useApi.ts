import { useMemo } from 'react';
import { ApiClient } from '@/lib/api';
import { authClient } from '@/lib/auth-client';

export function useApi() {
  const apiClient = useMemo(() => {
    const baseUrl = process.env.NEXT_PUBLIC_API_URL || "";

    return new ApiClient(baseUrl, {
      fetch: async (url, options) => {
        const { data: jwtData, error } = await authClient.token();
        
        const headers = new Headers(options?.headers);
        
        if (jwtData?.token) {
          headers.set('Authorization', `Bearer ${jwtData.token}`);
        } else if (error) {
          console.error("Failed to fetch JWT token", error);
        }

        return fetch(url, { ...options, headers });
      }
    });
  }, []);

  return apiClient;
}
