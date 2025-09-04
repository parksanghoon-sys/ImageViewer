import axios from 'axios';

const BASE_URL = 'http://localhost:5000'; // API Gateway URL

export const api = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// For backward compatibility
export const authApi = api;
export const imageApi = api;
export const shareApi = api;

// Define response types
interface LoginResponse {
  success: boolean;
  data: {
    user: any;
    accessToken: string;
    refreshToken: string;
  };
  errorMessage?: string;
}

// Auth API endpoints
export const authService = {
  login: async (email: string, password: string): Promise<LoginResponse> => {
    const response = await authApi.post<LoginResponse>('/api/auth/login', { email, password });
    return response.data;
  },
  
  register: async (email: string, password: string, username: string) => {
    const response = await authApi.post('/api/auth/register', { email, password, username });
    return response.data;
  },
  
  refreshToken: async (refreshToken: string) => {
    const response = await authApi.post('/api/auth/refresh', { refreshToken });
    return response.data;
  },
  
  getUsers: async () => {
    const response = await api.get('/api/auth/users');
    return response.data;
  }
};

// Interceptor to add auth token to requests
api.interceptors.request.use(
  (config) => {
    const token = localStorage.getItem('accessToken');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);