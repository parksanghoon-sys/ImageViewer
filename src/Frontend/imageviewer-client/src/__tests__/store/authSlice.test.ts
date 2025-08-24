import { configureStore } from '@reduxjs/toolkit';
import authReducer, {
  loginAsync,
  registerAsync,
  getCurrentUserAsync,
  initializeAuthAsync,
  logout,
  clearError,
  setError,
} from '../../store/slices/authSlice';
import { authApi } from '../../services/api';

// Mock the API module
jest.mock('../../services/api');
const mockedAuthApi = authApi as jest.Mocked<typeof authApi>;

describe('authSlice', () => {
  let store: ReturnType<typeof configureStore>;

  beforeEach(() => {
    store = configureStore({
      reducer: {
        auth: authReducer,
      },
    });
    jest.clearAllMocks();
  });

  describe('initial state', () => {
    it('should have correct initial state', () => {
      const state = store.getState().auth;
      
      expect(state).toEqual({
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
        isInitialized: false,
      });
    });
  });

  describe('synchronous actions', () => {
    it('should handle logout', () => {
      // Set some initial authenticated state
      store.dispatch(setError('Some error'));
      
      store.dispatch(logout());
      
      const state = store.getState().auth;
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
      expect(state.error).toBeNull();
    });

    it('should handle clearError', () => {
      store.dispatch(setError('Some error'));
      expect(store.getState().auth.error).toBe('Some error');
      
      store.dispatch(clearError());
      expect(store.getState().auth.error).toBeNull();
    });

    it('should handle setError', () => {
      const errorMessage = 'Test error message';
      
      store.dispatch(setError(errorMessage));
      expect(store.getState().auth.error).toBe(errorMessage);
    });
  });

  describe('loginAsync', () => {
    const mockUser = {
      id: 'user-id',
      email: 'test@example.com',
      username: 'testuser',
      isActive: true,
      createdAt: '2023-01-01T00:00:00Z',
    };

    const mockAuthResponse = {
      user: mockUser,
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
    };

    it('should handle successful login', async () => {
      mockedAuthApi.login.mockResolvedValueOnce({
        success: true,
        data: mockAuthResponse,
        timestamp: '2023-01-01T00:00:00Z',
      });

      const loginData = {
        email: 'test@example.com',
        password: 'password123',
      };

      await store.dispatch(loginAsync(loginData));

      const state = store.getState().auth;
      expect(state.isLoading).toBe(false);
      expect(state.isAuthenticated).toBe(true);
      expect(state.user).toEqual(mockUser);
      expect(state.error).toBeNull();
      expect(mockedAuthApi.login).toHaveBeenCalledWith(loginData);
    });

    it('should handle failed login', async () => {
      const errorMessage = '이메일 또는 비밀번호가 올바르지 않습니다.';
      
      mockedAuthApi.login.mockRejectedValueOnce({
        message: errorMessage,
      });

      const loginData = {
        email: 'test@example.com',
        password: 'wrongpassword',
      };

      await store.dispatch(loginAsync(loginData));

      const state = store.getState().auth;
      expect(state.isLoading).toBe(false);
      expect(state.isAuthenticated).toBe(false);
      expect(state.user).toBeNull();
      expect(state.error).toBe(errorMessage);
    });

    it('should set loading state during login', () => {
      mockedAuthApi.login.mockImplementationOnce(
        () => new Promise((resolve) => {
          // Don't resolve immediately to test loading state
          setTimeout(() => resolve({
            success: true,
            data: mockAuthResponse,
            timestamp: '2023-01-01T00:00:00Z',
          }), 100);
        })
      );

      const loginData = {
        email: 'test@example.com',
        password: 'password123',
      };

      store.dispatch(loginAsync(loginData));

      const state = store.getState().auth;
      expect(state.isLoading).toBe(true);
      expect(state.error).toBeNull();
    });
  });

  describe('registerAsync', () => {
    const mockUser = {
      id: 'user-id',
      email: 'test@example.com',
      username: 'testuser',
      isActive: true,
      createdAt: '2023-01-01T00:00:00Z',
    };

    const mockAuthResponse = {
      user: mockUser,
      accessToken: 'access-token',
      refreshToken: 'refresh-token',
    };

    it('should handle successful registration', async () => {
      mockedAuthApi.register.mockResolvedValueOnce({
        success: true,
        data: mockAuthResponse,
        timestamp: '2023-01-01T00:00:00Z',
      });

      const registerData = {
        email: 'test@example.com',
        username: 'testuser',
        password: 'password123',
        confirmPassword: 'password123',
      };

      await store.dispatch(registerAsync(registerData));

      const state = store.getState().auth;
      expect(state.isLoading).toBe(false);
      expect(state.isAuthenticated).toBe(true);
      expect(state.user).toEqual(mockUser);
      expect(state.error).toBeNull();
      expect(mockedAuthApi.register).toHaveBeenCalledWith(registerData);
    });

    it('should handle failed registration', async () => {
      const errorMessage = '이미 사용 중인 이메일입니다.';
      
      mockedAuthApi.register.mockRejectedValueOnce({
        message: errorMessage,
      });

      const registerData = {
        email: 'test@example.com',
        username: 'testuser',
        password: 'password123',
        confirmPassword: 'password123',
      };

      await store.dispatch(registerAsync(registerData));

      const state = store.getState().auth;
      expect(state.isLoading).toBe(false);
      expect(state.isAuthenticated).toBe(false);
      expect(state.user).toBeNull();
      expect(state.error).toBe(errorMessage);
    });
  });

  describe('getCurrentUserAsync', () => {
    const mockUser = {
      id: 'user-id',
      email: 'test@example.com',
      username: 'testuser',
      isActive: true,
      createdAt: '2023-01-01T00:00:00Z',
    };

    it('should handle successful user fetch', async () => {
      mockedAuthApi.getCurrentUser.mockResolvedValueOnce({
        success: true,
        data: mockUser,
        timestamp: '2023-01-01T00:00:00Z',
      });

      await store.dispatch(getCurrentUserAsync());

      const state = store.getState().auth;
      expect(state.isLoading).toBe(false);
      expect(state.isAuthenticated).toBe(true);
      expect(state.user).toEqual(mockUser);
      expect(state.error).toBeNull();
    });

    it('should handle failed user fetch', async () => {
      const errorMessage = '사용자 정보를 가져올 수 없습니다.';
      
      mockedAuthApi.getCurrentUser.mockRejectedValueOnce({
        message: errorMessage,
      });

      await store.dispatch(getCurrentUserAsync());

      const state = store.getState().auth;
      expect(state.isLoading).toBe(false);
      expect(state.isAuthenticated).toBe(false);
      expect(state.user).toBeNull();
      expect(state.error).toBe(errorMessage);
    });
  });

  describe('initializeAuthAsync', () => {
    const mockUser = {
      id: 'user-id',
      email: 'test@example.com',
      username: 'testuser',
      isActive: true,
      createdAt: '2023-01-01T00:00:00Z',
    };

    it('should initialize with existing valid token', async () => {
      // Mock TokenManager to return a token
      const mockTokenManager = {
        getAccessToken: jest.fn().mockReturnValue('valid-token'),
        clearTokens: jest.fn(),
      };
      
      // Mock getCurrentUser to succeed
      mockedAuthApi.getCurrentUser.mockResolvedValueOnce({
        success: true,
        data: mockUser,
        timestamp: '2023-01-01T00:00:00Z',
      });

      await store.dispatch(initializeAuthAsync());

      const state = store.getState().auth;
      expect(state.isInitialized).toBe(true);
      expect(state.isLoading).toBe(false);
      expect(state.user).toEqual(mockUser);
      expect(state.isAuthenticated).toBe(true);
    });

    it('should initialize without token', async () => {
      // Mock TokenManager to return null
      const mockTokenManager = {
        getAccessToken: jest.fn().mockReturnValue(null),
        clearTokens: jest.fn(),
      };

      await store.dispatch(initializeAuthAsync());

      const state = store.getState().auth;
      expect(state.isInitialized).toBe(true);
      expect(state.isLoading).toBe(false);
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
    });

    it('should handle failed token validation', async () => {
      // Mock getCurrentUser to fail
      mockedAuthApi.getCurrentUser.mockRejectedValueOnce(new Error('Invalid token'));

      await store.dispatch(initializeAuthAsync());

      const state = store.getState().auth;
      expect(state.isInitialized).toBe(true);
      expect(state.isLoading).toBe(false);
      expect(state.user).toBeNull();
      expect(state.isAuthenticated).toBe(false);
    });
  });

  describe('error handling', () => {
    it('should clear error when login succeeds after previous failure', async () => {
      // First, set an error
      store.dispatch(setError('Previous error'));
      expect(store.getState().auth.error).toBe('Previous error');

      // Then succeed with login
      const mockUser = {
        id: 'user-id',
        email: 'test@example.com',
        username: 'testuser',
        isActive: true,
        createdAt: '2023-01-01T00:00:00Z',
      };

      mockedAuthApi.login.mockResolvedValueOnce({
        success: true,
        data: {
          user: mockUser,
          accessToken: 'access-token',
          refreshToken: 'refresh-token',
        },
        timestamp: '2023-01-01T00:00:00Z',
      });

      await store.dispatch(loginAsync({
        email: 'test@example.com',
        password: 'password123',
      }));

      const state = store.getState().auth;
      expect(state.error).toBeNull();
    });

    it('should handle API response without success flag', async () => {
      mockedAuthApi.login.mockResolvedValueOnce({
        success: false,
        errorMessage: 'API error message',
        timestamp: '2023-01-01T00:00:00Z',
      });

      await store.dispatch(loginAsync({
        email: 'test@example.com',
        password: 'password123',
      }));

      const state = store.getState().auth;
      expect(state.error).toBe('API error message');
      expect(state.isAuthenticated).toBe(false);
    });
  });
});