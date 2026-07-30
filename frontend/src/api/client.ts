import axios from 'axios';
import { authEmitter, AUTH_EVENTS } from './authEmitter';

// Create an axios instance with a base URL
export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000',
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor to attach the auth token
apiClient.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('token');
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor to handle errors globally.
// On 401: fire an `unauthorized` event on authEmitter so that AuthProvider
// (which has access to useNavigate) can redirect without a full-page reload.
apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('token');
      authEmitter.dispatchEvent(new Event(AUTH_EVENTS.UNAUTHORIZED));
    }
    return Promise.reject(error);
  }
);

