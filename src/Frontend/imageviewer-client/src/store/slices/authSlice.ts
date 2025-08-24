import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { authApi, TokenManager } from '../../services/api';
import { User, LoginRequest, RegisterRequest, ApiError } from '../../types/api';

// 상태 타입 정의
export interface AuthState {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  error: string | null;
  isInitialized: boolean;
}

// 초기 상태
const initialState: AuthState = {
  user: null,
  isAuthenticated: false,
  isLoading: false,
  error: null,
  isInitialized: false,
};

// 비동기 액션들
export const loginAsync = createAsyncThunk<
  { user: User; accessToken: string; refreshToken: string },
  LoginRequest,
  { rejectValue: ApiError }
>('auth/login', async (credentials, { rejectWithValue }) => {
  try {
    const response = await authApi.login(credentials);
    if (response.success && response.data) {
      const { user, accessToken, refreshToken } = response.data;
      TokenManager.setTokens(accessToken, refreshToken);
      return { user, accessToken, refreshToken };
    } else {
      return rejectWithValue({
        message: response.errorMessage || '로그인에 실패했습니다.',
        code: response.errorCode,
        validationErrors: response.validationErrors,
      });
    }
  } catch (error) {
    return rejectWithValue(error as ApiError);
  }
});

export const registerAsync = createAsyncThunk<
  { user: User; accessToken: string; refreshToken: string },
  RegisterRequest,
  { rejectValue: ApiError }
>('auth/register', async (userData, { rejectWithValue }) => {
  try {
    const response = await authApi.register(userData);
    if (response.success && response.data) {
      const { user, accessToken, refreshToken } = response.data;
      TokenManager.setTokens(accessToken, refreshToken);
      return { user, accessToken, refreshToken };
    } else {
      return rejectWithValue({
        message: response.errorMessage || '회원가입에 실패했습니다.',
        code: response.errorCode,
        validationErrors: response.validationErrors,
      });
    }
  } catch (error) {
    return rejectWithValue(error as ApiError);
  }
});

export const getCurrentUserAsync = createAsyncThunk<
  User,
  void,
  { rejectValue: ApiError }
>('auth/getCurrentUser', async (_, { rejectWithValue }) => {
  try {
    const response = await authApi.getCurrentUser();
    if (response.success && response.data) {
      return response.data;
    } else {
      return rejectWithValue({
        message: response.errorMessage || '사용자 정보를 가져올 수 없습니다.',
        code: response.errorCode,
      });
    }
  } catch (error) {
    return rejectWithValue(error as ApiError);
  }
});

export const initializeAuthAsync = createAsyncThunk<
  User | null,
  void,
  { rejectValue: string }
>('auth/initialize', async (_, { rejectWithValue }) => {
  try {
    const accessToken = TokenManager.getAccessToken();
    if (!accessToken) {
      return null;
    }

    const response = await authApi.getCurrentUser();
    if (response.success && response.data) {
      return response.data;
    } else {
      TokenManager.clearTokens();
      return null;
    }
  } catch (error) {
    TokenManager.clearTokens();
    return null;
  }
});

// Auth slice 생성
const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    logout: (state) => {
      state.user = null;
      state.isAuthenticated = false;
      state.error = null;
      TokenManager.clearTokens();
    },
    clearError: (state) => {
      state.error = null;
    },
    setError: (state, action: PayloadAction<string>) => {
      state.error = action.payload;
    },
  },
  extraReducers: (builder) => {
    // 로그인
    builder
      .addCase(loginAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(loginAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.user = action.payload.user;
        state.isAuthenticated = true;
        state.error = null;
      })
      .addCase(loginAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.user = null;
        state.isAuthenticated = false;
        state.error = action.payload?.message || '로그인에 실패했습니다.';
      });

    // 회원가입
    builder
      .addCase(registerAsync.pending, (state) => {
        state.isLoading = true;
        state.error = null;
      })
      .addCase(registerAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.user = action.payload.user;
        state.isAuthenticated = true;
        state.error = null;
      })
      .addCase(registerAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.user = null;
        state.isAuthenticated = false;
        state.error = action.payload?.message || '회원가입에 실패했습니다.';
      });

    // 현재 사용자 정보 조회
    builder
      .addCase(getCurrentUserAsync.pending, (state) => {
        state.isLoading = true;
      })
      .addCase(getCurrentUserAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.user = action.payload;
        state.isAuthenticated = true;
        state.error = null;
      })
      .addCase(getCurrentUserAsync.rejected, (state, action) => {
        state.isLoading = false;
        state.user = null;
        state.isAuthenticated = false;
        state.error = action.payload?.message || '사용자 정보를 가져올 수 없습니다.';
      });

    // 인증 초기화
    builder
      .addCase(initializeAuthAsync.pending, (state) => {
        state.isLoading = true;
        state.isInitialized = false;
      })
      .addCase(initializeAuthAsync.fulfilled, (state, action) => {
        state.isLoading = false;
        state.isInitialized = true;
        if (action.payload) {
          state.user = action.payload;
          state.isAuthenticated = true;
        } else {
          state.user = null;
          state.isAuthenticated = false;
        }
        state.error = null;
      })
      .addCase(initializeAuthAsync.rejected, (state) => {
        state.isLoading = false;
        state.isInitialized = true;
        state.user = null;
        state.isAuthenticated = false;
        state.error = null;
      });
  },
});

export const { logout, clearError, setError } = authSlice.actions;
export default authSlice.reducer;