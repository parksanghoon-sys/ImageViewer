import { configureStore } from '@reduxjs/toolkit';
import { TypedUseSelectorHook, useDispatch, useSelector } from 'react-redux';
import authReducer from './slices/authSlice';
import imageReducer from './slices/imageSlice';

// 스토어 설정
export const store = configureStore({
  reducer: {
    auth: authReducer,
    images: imageReducer,
  },
  middleware: (getDefaultMiddleware) =>
    getDefaultMiddleware({
      serializableCheck: {
        // Redux Toolkit에서 non-serializable 값들을 무시하도록 설정
        ignoredActions: ['persist/PERSIST', 'persist/REHYDRATE'],
      },
    }),
  devTools: process.env.NODE_ENV !== 'production',
});

// 타입 정의
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;

// 타입이 지정된 hooks
export const useAppDispatch = () => useDispatch<AppDispatch>();
export const useAppSelector: TypedUseSelectorHook<RootState> = useSelector;

// 선택자 함수들 (Selectors)
export const selectAuth = (state: RootState) => state.auth;
export const selectImages = (state: RootState) => state.images;

// 인증 관련 선택자
export const selectIsAuthenticated = (state: RootState) => state.auth.isAuthenticated;
export const selectCurrentUser = (state: RootState) => state.auth.user;
export const selectAuthLoading = (state: RootState) => state.auth.isLoading;
export const selectAuthError = (state: RootState) => state.auth.error;
export const selectIsAuthInitialized = (state: RootState) => state.auth.isInitialized;

// 이미지 관련 선택자
export const selectImageList = (state: RootState) => state.images.images;
export const selectImageLoading = (state: RootState) => state.images.isLoading;
export const selectImageUploading = (state: RootState) => state.images.isUploading;
export const selectImageError = (state: RootState) => state.images.error;
export const selectImagePagination = (state: RootState) => ({
  currentPage: state.images.currentPage,
  pageSize: state.images.pageSize,
  totalCount: state.images.totalCount,
  totalPages: state.images.totalPages,
  hasNext: state.images.hasNext,
  hasPrevious: state.images.hasPrevious,
});
export const selectSelectedImages = (state: RootState) => state.images.selectedImages;
export const selectImageViewSettings = (state: RootState) => ({
  viewMode: state.images.viewMode,
  sortBy: state.images.sortBy,
  filterByType: state.images.filterByType,
});
export const selectUploadProgress = (state: RootState) => state.images.uploadProgress;