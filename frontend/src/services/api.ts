import axios from 'axios';

const BASE_URL = 'http://localhost:5294'; // AuthService URL

export const authApi = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const imageApi = axios.create({
  baseURL: 'http://localhost:5295', // ImageService URL (will be available when EF issue is fixed)
  headers: {
    'Content-Type': 'application/json',
  },
});

export const shareApi = axios.create({
  baseURL: 'http://localhost:5296', // ShareService URL (will be available when EF issue is fixed)
  headers: {
    'Content-Type': 'application/json',
  },
});

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
  
  health: async () => {
    const response = await authApi.get('/health');
    return response.data;
  }
};

// Interceptor to add auth token to requests
authApi.interceptors.request.use(
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

imageApi.interceptors.request.use(
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

shareApi.interceptors.request.use(
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