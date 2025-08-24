/**
 * API 서비스 모듈
 * 
 * 이 파일은 백엔드 API와 통신하는 모든 함수들을 정의합니다.
 * - axios를 사용해서 HTTP 요청을 보냅니다
 * - JWT 토큰 자동 관리 (로그인 상태 유지)
 * - 토큰 만료시 자동 갱신
 * - 에러 처리
 */

import axios, { AxiosInstance, AxiosResponse, AxiosError } from 'axios';
import {
  ApiResponse,
  LoginRequest,
  RegisterRequest,
  AuthResponse,
  RefreshTokenRequest,
  User,
  ImageMetadata,
  ImageListResponse,
  ImageUploadRequest,
  ImageUploadResponse,
  ApiError
} from '../types/api';

// API 서버 주소 설정
// .env 파일에서 환경변수를 가져오거나, 없으면 기본값 사용
const AUTH_API_BASE_URL = process.env.REACT_APP_AUTH_API_URL || 'http://localhost:5294';
const IMAGE_API_BASE_URL = process.env.REACT_APP_IMAGE_API_URL || 'http://localhost:5215';

/**
 * JWT 토큰 관리 클래스
 * 
 * 브라우저의 localStorage에 JWT 토큰을 저장하고 관리합니다.
 * - accessToken: 실제 API 요청에 사용되는 토큰 (수명이 짧음)
 * - refreshToken: accessToken을 갱신할 때 사용하는 토큰 (수명이 김)
 */
class TokenManager {
  // localStorage에 저장할 키 이름들 (상수로 정의해서 오타 방지)
  private static readonly ACCESS_TOKEN_KEY = 'accessToken';
  private static readonly REFRESH_TOKEN_KEY = 'refreshToken';

  /**
   * 저장된 액세스 토큰을 가져옵니다
   * @returns 액세스 토큰 문자열 또는 null (저장된 토큰이 없으면)
   */
  static getAccessToken(): string | null {
    return localStorage.getItem(this.ACCESS_TOKEN_KEY);
  }

  /**
   * 액세스 토큰을 localStorage에 저장합니다
   * @param token 저장할 액세스 토큰
   */
  static setAccessToken(token: string): void {
    localStorage.setItem(this.ACCESS_TOKEN_KEY, token);
  }

  /**
   * 저장된 리프레시 토큰을 가져옵니다
   * @returns 리프레시 토큰 문자열 또는 null
   */
  static getRefreshToken(): string | null {
    return localStorage.getItem(this.REFRESH_TOKEN_KEY);
  }

  /**
   * 리프레시 토큰을 localStorage에 저장합니다
   * @param token 저장할 리프레시 토큰
   */
  static setRefreshToken(token: string): void {
    localStorage.setItem(this.REFRESH_TOKEN_KEY, token);
  }

  /**
   * 저장된 모든 토큰을 삭제합니다 (로그아웃할 때 사용)
   */
  static clearTokens(): void {
    localStorage.removeItem(this.ACCESS_TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
  }

  /**
   * 액세스 토큰과 리프레시 토큰을 동시에 저장합니다
   * @param accessToken 액세스 토큰
   * @param refreshToken 리프레시 토큰
   */
  static setTokens(accessToken: string, refreshToken: string): void {
    this.setAccessToken(accessToken);
    this.setRefreshToken(refreshToken);
  }
}

/**
 * HTTP 클라이언트를 생성하는 함수
 * 
 * axios 인스턴스를 생성하고 인터셉터를 설정합니다.
 * 인터셉터란? HTTP 요청/응답을 가로채서 자동으로 처리하는 기능
 * 
 * @param baseURL API 서버의 기본 URL
 * @returns 설정이 완료된 axios 인스턴스
 */
const createApiClient = (baseURL: string): AxiosInstance => {
  // axios 인스턴스 생성 (HTTP 클라이언트)
  const client = axios.create({
    baseURL,                              // API 서버 주소
    timeout: 10000,                       // 요청 타임아웃 (10초)
    headers: {
      'Content-Type': 'application/json', // 기본적으로 JSON 형태로 데이터 전송
    },
  });

  /**
   * 요청 인터셉터: 모든 HTTP 요청이 서버로 보내지기 전에 자동으로 실행
   * 목적: 저장된 JWT 토큰을 자동으로 헤더에 추가
   */
  client.interceptors.request.use((config) => {
    const token = TokenManager.getAccessToken();
    if (token) {
      // Authorization 헤더에 Bearer 토큰 형식으로 추가
      // 예: "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  /**
   * 응답 인터셉터: 서버로부터 응답을 받은 후 자동으로 실행
   * 목적: 토큰 만료(401 에러) 시 자동으로 토큰을 갱신하고 요청을 재시도
   */
  client.interceptors.response.use(
    // 정상 응답인 경우: 그대로 반환
    (response) => response,
    // 에러 응답인 경우: 토큰 갱신 로직 실행
    async (error: AxiosError) => {
      const originalRequest = error.config as any;

      // 401 에러(인증 실패)이고, 아직 재시도하지 않은 요청인 경우
      if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
        originalRequest._retry = true; // 재시도 플래그 설정 (무한 루프 방지)

        try {
          const refreshToken = TokenManager.getRefreshToken();
          if (refreshToken) {
            // 리프레시 토큰으로 새 액세스 토큰 발급
            const response = await authApi.refreshToken({ refreshToken });
            const { accessToken, refreshToken: newRefreshToken } = response.data!;
            
            // 새 토큰들을 저장
            TokenManager.setTokens(accessToken, newRefreshToken);
            
            // 실패했던 원본 요청을 새 토큰으로 재시도
            originalRequest.headers!.Authorization = `Bearer ${accessToken}`;
            return client(originalRequest);
          }
        } catch (refreshError) {
          // 리프레시 토큰도 만료된 경우: 로그아웃 처리
          TokenManager.clearTokens();
          window.location.href = '/login'; // 로그인 페이지로 리다이렉트
          return Promise.reject(refreshError);
        }
      }

      // 401 에러가 아니거나 토큰 갱신에 실패한 경우: 에러 그대로 전달
      return Promise.reject(error);
    }
  );

  return client;
};

// 각 서비스별로 HTTP 클라이언트 인스턴스 생성
const authClient = createApiClient(AUTH_API_BASE_URL);   // 인증 관련 API용
const imageClient = createApiClient(IMAGE_API_BASE_URL); // 이미지 관련 API용

/**
 * API 에러를 일관된 형태로 처리하는 헬퍼 함수
 * 
 * 다양한 형태의 에러를 받아서 우리가 정의한 ApiError 형태로 변환합니다.
 * 
 * @param error axios에서 발생한 에러 객체
 * @returns 표준화된 ApiError 객체
 */
const handleApiError = (error: any): ApiError => {
  // 서버에서 온 에러 응답이 있는 경우
  if (error.response?.data) {
    const apiResponse: ApiResponse = error.response.data;
    return {
      message: apiResponse.errorMessage || '알 수 없는 오류가 발생했습니다.',
      code: apiResponse.errorCode,
      validationErrors: apiResponse.validationErrors, // 폼 검증 에러들
    };
  }

  // 일반적인 에러 메시지가 있는 경우
  if (error.message) {
    return {
      message: error.message,
    };
  }

  // 그 외의 경우: 기본 에러 메시지
  return {
    message: '네트워크 오류가 발생했습니다.',
  };
};

/**
 * 인증 관련 API 함수들
 * 
 * 로그인, 회원가입, 토큰 갱신 등 사용자 인증과 관련된 모든 API 호출을 담당합니다.
 */
export const authApi = {
  /**
   * 새 사용자 회원가입
   * 
   * @param data 회원가입 정보 (이메일, 사용자명, 비밀번호, 비밀번호 확인)
   * @returns 회원가입 성공 시 사용자 정보와 JWT 토큰들
   */
  register: async (data: RegisterRequest): Promise<ApiResponse<AuthResponse>> => {
    try {
      const response: AxiosResponse<ApiResponse<AuthResponse>> = await authClient.post('/api/auth/register', data);
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  /**
   * 사용자 로그인
   * 
   * @param data 로그인 정보 (이메일, 비밀번호)
   * @returns 로그인 성공 시 사용자 정보와 JWT 토큰들
   */
  login: async (data: LoginRequest): Promise<ApiResponse<AuthResponse>> => {
    try {
      const response: AxiosResponse<ApiResponse<AuthResponse>> = await authClient.post('/api/auth/login', data);
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  /**
   * JWT 토큰 갱신
   * 
   * 액세스 토큰이 만료되었을 때 리프레시 토큰을 사용해서 새 토큰을 발급받습니다.
   * 보통 HTTP 인터셉터에서 자동으로 호출됩니다.
   * 
   * @param data 리프레시 토큰
   * @returns 새로 발급된 액세스 토큰과 리프레시 토큰
   */
  refreshToken: async (data: RefreshTokenRequest): Promise<ApiResponse<AuthResponse>> => {
    try {
      const response: AxiosResponse<ApiResponse<AuthResponse>> = await authClient.post('/api/auth/refresh', data);
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  /**
   * 현재 로그인한 사용자의 정보 조회
   * 
   * JWT 토큰을 바탕으로 현재 사용자의 프로필 정보를 가져옵니다.
   * 
   * @returns 현재 사용자의 정보 (ID, 이메일, 사용자명 등)
   */
  getCurrentUser: async (): Promise<ApiResponse<User>> => {
    try {
      const response: AxiosResponse<ApiResponse<User>> = await authClient.get('/api/auth/me');
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  /**
   * 로그아웃
   * 
   * 브라우저에 저장된 JWT 토큰들을 삭제합니다.
   * 실제로는 서버에 요청을 보내지 않고 클라이언트에서만 토큰을 제거합니다.
   */
  logout: (): void => {
    TokenManager.clearTokens();
  },
};

// 이미지 API
export const imageApi = {
  // 이미지 업로드
  uploadImage: async (data: ImageUploadRequest): Promise<ApiResponse<ImageUploadResponse>> => {
    try {
      const formData = new FormData();
      formData.append('file', data.file);
      if (data.title) {
        formData.append('title', data.title);
      }
      if (data.description) {
        formData.append('description', data.description);
      }
      if (data.tags) {
        formData.append('tags', data.tags);
      }
      formData.append('isPublic', data.isPublic?.toString() || 'false');

      const response: AxiosResponse<ApiResponse<ImageUploadResponse>> = await imageClient.post(
        '/api/image/dev/upload',
        formData,
        {
          headers: {
            'Content-Type': 'multipart/form-data',
          },
        }
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  // 내 이미지 목록 조회
  getMyImages: async (page: number = 1, pageSize: number = 12): Promise<ApiResponse<ImageListResponse>> => {
    try {
      const response: AxiosResponse<ApiResponse<ImageListResponse>> = await imageClient.get(
        `/api/image/dev/my-images?page=${page}&pageSize=${pageSize}`
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  // 이미지 다운로드 URL 생성
  getImageDownloadUrl: (imageId: string, thumbnail: boolean = false): string => {
    const token = TokenManager.getAccessToken();
    const queryParams = new URLSearchParams();
    if (thumbnail) queryParams.append('thumbnail', 'true');
    if (token) queryParams.append('token', token);
    
    return `${IMAGE_API_BASE_URL}/api/image/${imageId}/download?${queryParams.toString()}`;
  },

  // 이미지 설명 수정
  updateImage: async (imageId: string, description: string): Promise<ApiResponse<{ id: string; description: string }>> => {
    try {
      const response: AxiosResponse<ApiResponse<{ id: string; description: string }>> = await imageClient.put(
        `/api/image/${imageId}`,
        description,
        {
          headers: {
            'Content-Type': 'application/json',
          },
        }
      );
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },

  // 이미지 삭제
  deleteImage: async (imageId: string): Promise<ApiResponse<null>> => {
    try {
      const response: AxiosResponse<ApiResponse<null>> = await imageClient.delete(`/api/image/${imageId}`);
      return response.data;
    } catch (error) {
      throw handleApiError(error);
    }
  },
};

// 토큰 관리 유틸리티 export
export { TokenManager };

// API 헬스체크
export const checkApiHealth = async (): Promise<{ auth: boolean; image: boolean }> => {
  const results = { auth: false, image: false };

  try {
    await authClient.get('/health');
    results.auth = true;
  } catch (error) {
    console.warn('Auth service health check failed:', error);
  }

  try {
    await imageClient.get('/health');
    results.image = true;
  } catch (error) {
    console.warn('Image service health check failed:', error);
  }

  return results;
};