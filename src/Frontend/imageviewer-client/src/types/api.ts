/**
 * API 타입 정의 파일
 * 
 * 이 파일은 프론트엔드와 백엔드 간의 데이터 교환 형식을 정의합니다.
 * TypeScript의 interface를 사용해서 API 요청/응답의 구조를 명확히 합니다.
 * 
 * 왜 타입을 정의하나요?
 * 1. 개발 시점에 오타나 잘못된 데이터 접근을 방지
 * 2. 코드 자동완성 기능 제공
 * 3. API 스펙 문서 역할
 */

/**
 * 모든 API 응답의 공통 구조
 * 
 * 제네릭 타입 T는 실제 데이터의 타입을 나타냅니다.
 * 예: ApiResponse<User>, ApiResponse<ImageMetadata[]>
 */
export interface ApiResponse<T = any> {
  success: boolean;                                    // 요청 성공 여부
  data?: T;                                           // 실제 데이터 (성공 시에만)
  errorMessage?: string;                              // 에러 메시지 (실패 시)
  errorCode?: string;                                 // 에러 코드 (실패 시)
  validationErrors?: Record<string, string[]>;        // 폼 검증 에러들 (실패 시)
  timestamp: string;                                  // 응답 시간
}

/**
 * 인증 관련 타입들
 */

/**
 * 로그인 요청 데이터
 */
export interface LoginRequest {
  email: string;        // 사용자 이메일
  password: string;     // 비밀번호
}

/**
 * 회원가입 요청 데이터
 */
export interface RegisterRequest {
  email: string;           // 사용자 이메일
  username: string;        // 사용자명 (닉네임)
  password: string;        // 비밀번호
  confirmPassword: string; // 비밀번호 확인
}

/**
 * 인증 성공 시 응답 데이터
 */
export interface AuthResponse {
  accessToken: string;   // API 요청에 사용할 짧은 수명의 토큰
  refreshToken: string;  // 액세스 토큰 갱신용 긴 수명의 토큰
  user: User;           // 로그인한 사용자 정보
}

/**
 * 토큰 갱신 요청 데이터
 */
export interface RefreshTokenRequest {
  refreshToken: string;  // 저장된 리프레시 토큰
}

/**
 * 사용자 정보
 */
export interface User {
  id: string;              // 사용자 고유 ID (GUID)
  email: string;           // 이메일 주소
  username: string;        // 사용자명
  isActive: boolean;       // 계정 활성화 여부
  lastLoginAt?: string;    // 마지막 로그인 시간 (선택적)
  createdAt: string;       // 계정 생성 시간
}

// 이미지 관련 타입
export interface ImageMetadata {
  id: string;
  title: string;
  description?: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  width: number;
  height: number;
  imageUrl: string;
  thumbnailUrl?: string;
  isPublic: boolean;
  tags: string[];
  userId: string;
  userName: string;
  uploadedAt: string;
  thumbnailReady: boolean;
  isOwner: boolean;
}

export interface ImageListResponse {
  images: ImageMetadata[];
  pagination: {
    currentPage: number;
    pageSize: number;
    totalItems: number;
    totalPages: number;
    hasPreviousPage: boolean;
    hasNextPage: boolean;
    startIndex: number;
    endIndex: number;
  };
  searchSummary: {
    searchKeyword?: string;
    tagFilter?: string;
    sortBy: string;
    sortOrder: string;
    unfilteredCount: number;
    filteredCount: number;
    isFiltered: boolean;
  };
}

export interface ImageUploadRequest {
  file: File;
  title?: string;
  description?: string;
  isPublic?: boolean;
  tags?: string;
}

export interface ImageUploadResponse {
  id: string;
  title: string;
  description?: string;
  fileName: string;
  fileSize: number;
  contentType: string;
  width: number;
  height: number;
  imageUrl: string;
  thumbnailUrl?: string;
  isPublic: boolean;
  tags: string[];
  userId: string;
  userName: string;
  uploadedAt: string;
  thumbnailReady: boolean;
  isOwner: boolean;
}

// 사용자 설정 관련 타입
export interface UserSettings {
  previewCount: number;
  previewSize: number;
  blurIntensity: number;
  autoGenerateThumbnails: boolean;
  receiveShareNotifications: boolean;
  receiveEmailNotifications: boolean;
  useDarkMode: boolean;
}

// 공유 관련 타입 (향후 확장용)
export interface ShareRequest {
  id: string;
  requesterId: string;
  ownerId: string;
  imageId: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Expired';
  requestMessage?: string;
  responseMessage?: string;
  createdAt: string;
  respondedAt?: string;
  expiresAt: string;
}

// 에러 타입
export interface ApiError {
  message: string;
  code?: string;
  validationErrors?: Record<string, string[]>;
}