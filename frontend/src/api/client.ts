// Placeholder API client
const API_BASE_URL = '/api';

export const apiClient = {
  get: async (endpoint: string) => {
    // In the future this will use fetch with authorization headers
    console.log(`GET ${API_BASE_URL}${endpoint}`);
    return { data: [] };
  },
  post: async (endpoint: string, data: any) => {
    console.log(`POST ${API_BASE_URL}${endpoint}`, data);
    return { success: true };
  }
};
