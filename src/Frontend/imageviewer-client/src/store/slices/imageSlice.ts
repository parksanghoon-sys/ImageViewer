/**
 * 이미지 상태 관리 모듈 (Redux Slice)
 * 
 * Redux란? React 앱의 전역 상태를 관리하는 라이브러리입니다.
 * Slice란? Redux Toolkit에서 상태와 그 상태를 변경하는 함수들을 하나로 묶은 단위입니다.
 * 
 * 이 파일에서는 이미지 관련 모든 상태와 액션들을 정의합니다:
 * - 이미지 목록
 * - 페이지네이션 정보
 * - 로딩 상태
 * - 선택된 이미지들
 * - 보기 모드 (그리드/리스트)
 * - 정렬/필터 옵션
 */

import { createSlice, createAsyncThunk, PayloadAction } from '@reduxjs/toolkit';
import { imageApi } from '../../services/api';
import { ImageMetadata, ImageListResponse, ImageUploadRequest, ImageUploadResponse, ApiError } from '../../types/api';

/**
 * 이미지 관련 상태의 타입 정의
 * 
 * 이 인터페이스는 이미지 페이지에서 관리하는 모든 상태의 구조를 정의합니다.
 */
export interface ImageState {
  // 이미지 데이터
  images: ImageMetadata[];        // 현재 페이지의 이미지 목록
  
  // 페이지네이션 관련
  currentPage: number;            // 현재 페이지 번호
  pageSize: number;               // 한 페이지당 이미지 개수
  totalCount: number;             // 전체 이미지 개수
  totalPages: number;             // 전체 페이지 수
  hasNext: boolean;               // 다음 페이지 존재 여부
  hasPrevious: boolean;           // 이전 페이지 존재 여부
  
  // 로딩 상태
  isLoading: boolean;             // 이미지 목록 로딩 중 여부
  isUploading: boolean;           // 이미지 업로드 중 여부
  error: string | null;           // 에러 메시지 (없으면 null)
  uploadProgress: number;         // 업로드 진행률 (0-100)
  
  // UI 상태
  selectedImages: string[];       // 선택된 이미지 ID 목록
  viewMode: 'grid' | 'list';      // 보기 모드 (격자 또는 목록)
  sortBy: 'newest' | 'oldest' | 'name' | 'size';  // 정렬 기준
  filterByType: 'all' | 'jpeg' | 'png' | 'gif' | 'webp';  // 파일 타입 필터
}

/**
 * 앱이 시작될 때의 초기 상태
 * 
 * Redux 스토어가 생성될 때 이미지 상태가 가지는 기본값들입니다.
 */
const initialState: ImageState = {
  images: [],                 // 빈 이미지 목록으로 시작
  currentPage: 1,             // 첫 번째 페이지부터
  pageSize: 12,               // 한 페이지에 12개씩 표시
  totalCount: 0,              // 전체 이미지 개수 0개
  totalPages: 0,              // 전체 페이지 수 0개
  hasNext: false,             // 다음 페이지 없음
  hasPrevious: false,         // 이전 페이지 없음
  isLoading: false,           // 로딩 중 아님
  isUploading: false,         // 업로드 중 아님
  error: null,                // 에러 없음
  uploadProgress: 0,          // 업로드 진행률 0%
  selectedImages: [],         // 선택된 이미지 없음
  viewMode: 'grid',           // 기본은 격자 보기
  sortBy: 'newest',           // 기본은 최신순 정렬
  filterByType: 'all',        // 기본은 모든 파일 타입 표시
};

/**
 * 비동기 액션들 (Async Thunks)
 * 
 * createAsyncThunk는 비동기 작업(API 호출)을 Redux에서 쉽게 처리할 수 있게 해주는 함수입니다.
 * 각 비동기 액션은 pending(진행중) → fulfilled(성공) | rejected(실패) 상태를 자동으로 생성합니다.
 */

/**
 * 이미지 목록을 서버에서 가져오는 비동기 액션
 * 
 * @param page 가져올 페이지 번호 (기본값: 1)
 * @param pageSize 한 페이지당 이미지 개수 (기본값: 12)
 * @returns 이미지 목록과 페이지네이션 정보
 */
export const fetchImagesAsync = createAsyncThunk<
  ImageListResponse,                              // 성공 시 반환할 타입
  { page?: number; pageSize?: number },           // 입력 파라미터 타입
  { rejectValue: ApiError }                       // 실패 시 반환할 타입
>('images/fetchImages', async ({ page = 1, pageSize = 12 }, { rejectWithValue }) => {
  try {
    // API 호출
    const response = await imageApi.getMyImages(page, pageSize);
    if (response.success && response.data) {
      return response.data;  // 성공 시 데이터 반환
    } else {
      // API 응답은 받았지만 success가 false인 경우
      return rejectWithValue({
        message: response.errorMessage || '이미지 목록을 가져올 수 없습니다.',
        code: response.errorCode,
      });
    }
  } catch (error) {
    // 네트워크 에러 등 예외 발생 시
    return rejectWithValue(error as ApiError);
  }
});

/**
 * 이미지를 서버에 업로드하는 비동기 액션
 * 
 * 사용자가 선택한 이미지 파일을 서버에 업로드합니다.
 * 업로드 중에는 진행률을 표시할 수 있습니다.
 * 
 * @param uploadData 업로드할 이미지 정보 (파일, 제목, 설명, 태그 등)
 * @returns 업로드된 이미지 정보
 */
export const uploadImageAsync = createAsyncThunk<
  ImageUploadResponse,        // 성공 시 반환할 타입
  ImageUploadRequest,         // 입력 파라미터 타입 (파일 + 메타데이터)
  { rejectValue: ApiError }   // 실패 시 반환할 타입
>('images/uploadImage', async (uploadData, { rejectWithValue }) => {
  try {
    const response = await imageApi.uploadImage(uploadData);
    if (response.success && response.data) {
      return response.data;
    } else {
      return rejectWithValue({
        message: response.errorMessage || '이미지 업로드에 실패했습니다.',
        code: response.errorCode,
        validationErrors: response.validationErrors, // 파일 크기/형식 등의 검증 에러
      });
    }
  } catch (error) {
    return rejectWithValue(error as ApiError);
  }
});

export const updateImageAsync = createAsyncThunk<
  { id: string; description: string },
  { imageId: string; description: string },
  { rejectValue: ApiError }
>('images/updateImage', async ({ imageId, description }, { rejectWithValue }) => {
  try {
    const response = await imageApi.updateImage(imageId, description);
    if (response.success && response.data) {
      return response.data;
    } else {
      return rejectWithValue({
        message: response.errorMessage || '이미지 정보 수정에 실패했습니다.',
        code: response.errorCode,
      });
    }
  } catch (error) {
    return rejectWithValue(error as ApiError);
  }
});

export const deleteImageAsync = createAsyncThunk<
  string,
  string,
  { rejectValue: ApiError }
>('images/deleteImage', async (imageId, { rejectWithValue }) => {
  try {
    const response = await imageApi.deleteImage(imageId);
    if (response.success) {
      return imageId;
    } else {
      return rejectWithValue({
        message: response.errorMessage || '이미지 삭제에 실패했습니다.',
        code: response.errorCode,
      });
    }
  } catch (error) {
    return rejectWithValue(error as ApiError);
  }
});

/**
 * 이미지 슬라이스 생성
 * 
 * createSlice는 Redux Toolkit의 핵심 함수로, 상태와 리듀서를 한 번에 생성합니다.
 * 리듀서란? 현재 상태와 액션을 받아서 새로운 상태를 반환하는 함수입니다.
 */
const imageSlice = createSlice({
  name: 'images',        // 이 슬라이스의 이름 (디버깅 시 유용)
  initialState,          // 위에서 정의한 초기 상태
  
  /**
   * 동기적인 상태 변경 함수들 (리듀서)
   * 
   * 각 함수는 상태를 직접 변경하는 것처럼 보이지만,
   * 실제로는 Redux Toolkit의 Immer가 불변성을 자동으로 관리해줍니다.
   */
  reducers: {
    /**
     * 현재 페이지 번호를 변경합니다
     */
    setCurrentPage: (state, action: PayloadAction<number>) => {
      state.currentPage = action.payload;
    },
    
    /**
     * 한 페이지당 표시할 이미지 개수를 변경합니다
     * 개수를 변경하면 자동으로 첫 페이지로 이동합니다
     */
    setPageSize: (state, action: PayloadAction<number>) => {
      state.pageSize = action.payload;
      state.currentPage = 1; // 페이지 크기 변경 시 첫 페이지로 이동
    },
    
    /**
     * 이미지 보기 모드를 변경합니다 (그리드/리스트)
     */
    setViewMode: (state, action: PayloadAction<'grid' | 'list'>) => {
      state.viewMode = action.payload;
    },
    
    /**
     * 이미지 정렬 기준을 변경합니다
     */
    setSortBy: (state, action: PayloadAction<'newest' | 'oldest' | 'name' | 'size'>) => {
      state.sortBy = action.payload;
    },
    
    /**
     * 파일 타입 필터를 변경합니다
     */
    setFilterByType: (state, action: PayloadAction<'all' | 'jpeg' | 'png' | 'gif' | 'webp'>) => {
      state.filterByType = action.payload;
    },
    
    /**
     * 특정 이미지의 선택 상태를 토글합니다 (선택 ↔ 해제)
     * 체크박스를 클릭했을 때 호출됩니다
     */
    toggleImageSelection: (state, action: PayloadAction<string>) => {
      const imageId = action.payload;
      const index = state.selectedImages.indexOf(imageId);
      if (index > -1) {
        // 이미 선택된 이미지면 선택 해제
        state.selectedImages.splice(index, 1);
      } else {
        // 선택되지 않은 이미지면 선택 추가
        state.selectedImages.push(imageId);
      }
    },
    
    /**
     * 현재 페이지의 모든 이미지를 선택합니다
     */
    selectAllImages: (state) => {
      state.selectedImages = state.images.map(img => img.id);
    },
    
    /**
     * 모든 이미지 선택을 해제합니다
     */
    clearSelection: (state) => {
      state.selectedImages = [];
    },
    
    /**
     * 에러 메시지를 지웁니다
     */
    clearError: (state) => {
      state.error = null;
    },
    
    /**
     * 업로드 진행률을 설정합니다 (0-100)
     */
    setUploadProgress: (state, action: PayloadAction<number>) => {
      state.uploadProgress = action.payload;
    },
    
    /**
     * 업로드 관련 상태를 초기화합니다
     */
    resetUploadState: (state) => {
      state.isUploading = false;
      state.uploadProgress = 0;
      state.error = null;
    },
  },
  /**
   * extraReducers: 비동기 액션들의 상태 변화를 처리합니다
   * 
   * 각 비동기 액션은 3가지 상태를 가집니다:
   * - pending: 요청이 진행 중일 때
   * - fulfilled: 요청이 성공했을 때  
   * - rejected: 요청이 실패했을 때
   */
  extraReducers: (builder) => {
    /**
     * 이미지 목록 조회 처리
     */
    builder
      // 이미지 목록 요청 시작
      .addCase(fetchImagesAsync.pending, (state) => {
        state.isLoading = true;    // 로딩 스피너 표시
        state.error = null;        // 기존 에러 메시지 제거
      })
      // 이미지 목록 요청 성공
      .addCase(fetchImagesAsync.fulfilled, (state, action) => {
        state.isLoading = false;   // 로딩 완료
        
        // 서버에서 받은 데이터로 상태 업데이트
        state.images = action.payload.images;
        state.currentPage = action.payload.pagination.currentPage;
        state.pageSize = action.payload.pagination.pageSize;
        state.totalCount = action.payload.pagination.totalItems;
        state.totalPages = action.payload.pagination.totalPages;
        state.hasNext = action.payload.pagination.hasNextPage;
        state.hasPrevious = action.payload.pagination.hasPreviousPage;
        state.error = null;
      })
      // 이미지 목록 요청 실패
      .addCase(fetchImagesAsync.rejected, (state, action) => {
        state.isLoading = false;   // 로딩 완료
        state.error = action.payload?.message || '이미지 목록을 가져올 수 없습니다.';
      });

    // 이미지 업로드
    builder
      .addCase(uploadImageAsync.pending, (state) => {
        state.isUploading = true;
        state.uploadProgress = 0;
        state.error = null;
      })
      .addCase(uploadImageAsync.fulfilled, (state, action) => {
        state.isUploading = false;
        state.uploadProgress = 100;
        state.error = null;
        
        // 업로드된 이미지를 목록 맨 앞에 추가 (임시로, 실제로는 새로고침 필요)
        const newImage: ImageMetadata = {
          id: action.payload.id,
          title: action.payload.title,
          description: action.payload.description,
          fileName: action.payload.fileName,
          fileSize: action.payload.fileSize,
          contentType: action.payload.contentType,
          width: action.payload.width,
          height: action.payload.height,
          imageUrl: action.payload.imageUrl,
          thumbnailUrl: action.payload.thumbnailUrl,
          isPublic: action.payload.isPublic,
          tags: action.payload.tags,
          userId: action.payload.userId,
          userName: action.payload.userName,
          uploadedAt: action.payload.uploadedAt,
          thumbnailReady: action.payload.thumbnailReady,
          isOwner: action.payload.isOwner,
        };
        state.images.unshift(newImage);
        state.totalCount += 1;
      })
      .addCase(uploadImageAsync.rejected, (state, action) => {
        state.isUploading = false;
        state.uploadProgress = 0;
        state.error = action.payload?.message || '이미지 업로드에 실패했습니다.';
      });

    // 이미지 정보 수정
    builder
      .addCase(updateImageAsync.pending, (state) => {
        state.error = null;
      })
      .addCase(updateImageAsync.fulfilled, (state, action) => {
        const { id, description } = action.payload;
        const imageIndex = state.images.findIndex(img => img.id === id);
        if (imageIndex > -1) {
          state.images[imageIndex].description = description;
        }
        state.error = null;
      })
      .addCase(updateImageAsync.rejected, (state, action) => {
        state.error = action.payload?.message || '이미지 정보 수정에 실패했습니다.';
      });

    // 이미지 삭제
    builder
      .addCase(deleteImageAsync.pending, (state) => {
        state.error = null;
      })
      .addCase(deleteImageAsync.fulfilled, (state, action) => {
        const deletedImageId = action.payload;
        state.images = state.images.filter(img => img.id !== deletedImageId);
        state.selectedImages = state.selectedImages.filter(id => id !== deletedImageId);
        state.totalCount -= 1;
        state.error = null;
      })
      .addCase(deleteImageAsync.rejected, (state, action) => {
        state.error = action.payload?.message || '이미지 삭제에 실패했습니다.';
      });
  },
});

export const {
  setCurrentPage,
  setPageSize,
  setViewMode,
  setSortBy,
  setFilterByType,
  toggleImageSelection,
  selectAllImages,
  clearSelection,
  clearError,
  setUploadProgress,
  resetUploadState,
} = imageSlice.actions;

export default imageSlice.reducer;