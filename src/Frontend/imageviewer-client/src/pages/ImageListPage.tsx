import React, { useEffect, useState } from 'react';
import { useAppDispatch, useAppSelector, selectImages } from '../store';
import { 
  fetchImagesAsync, 
  setCurrentPage, 
  setViewMode, 
  clearSelection,
  toggleImageSelection,
  selectAllImages 
} from '../store/slices/imageSlice';
import ImageGrid from '../components/images/ImageGrid';
import ImageUpload from '../components/images/ImageUpload';

const ImageListPage: React.FC = () => {
  const dispatch = useAppDispatch();
  const { 
    images, 
    isLoading, 
    error, 
    currentPage, 
    pageSize, 
    totalPages,
    hasNext,
    hasPrevious,
    selectedImages,
    viewMode 
  } = useAppSelector(selectImages);

  const [showUploadModal, setShowUploadModal] = useState(false);

  useEffect(() => {
    dispatch(fetchImagesAsync({ page: currentPage, pageSize }));
  }, [dispatch, currentPage, pageSize]);

  const handlePageChange = (newPage: number) => {
    dispatch(setCurrentPage(newPage));
  };

  const handleViewModeChange = (mode: 'grid' | 'list') => {
    dispatch(setViewMode(mode));
  };

  const handleImageSelect = (imageId: string) => {
    dispatch(toggleImageSelection(imageId));
  };

  const handleSelectAll = () => {
    if (selectedImages.length === images.length) {
      dispatch(clearSelection());
    } else {
      dispatch(selectAllImages());
    }
  };

  const handleUploadComplete = () => {
    setShowUploadModal(false);
    // 업로드 완료 후 첫 페이지로 이동하여 새로운 이미지 확인
    dispatch(setCurrentPage(1));
    dispatch(fetchImagesAsync({ page: 1, pageSize }));
  };

  const refreshImages = () => {
    dispatch(fetchImagesAsync({ page: currentPage, pageSize }));
  };

  return (
    <div className="min-h-screen bg-gray-50">
      {/* 헤더 */}
      <div className="bg-white shadow-sm">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex items-center justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-2xl font-bold text-gray-900">내 이미지</h1>
              <span className="ml-2 text-sm text-gray-500">
                ({images.length}개)
              </span>
            </div>
            <div className="flex items-center space-x-4">
              {/* 새로고침 버튼 */}
              <button
                onClick={refreshImages}
                className="p-2 text-gray-400 hover:text-gray-600 transition-colors"
                title="새로고침"
              >
                <svg className="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 4v5h.582m15.356 2A8.001 8.001 0 004.582 9m0 0H9m11 11v-5h-.581m0 0a8.003 8.003 0 01-15.357-2m15.357 2H15" />
                </svg>
              </button>

              {/* 뷰 모드 토글 */}
              <div className="flex border border-gray-300 rounded-md">
                <button
                  onClick={() => handleViewModeChange('grid')}
                  className={`p-2 text-sm ${
                    viewMode === 'grid'
                      ? 'bg-blue-500 text-white'
                      : 'bg-white text-gray-700 hover:bg-gray-50'
                  } transition-colors rounded-l-md`}
                >
                  <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M5 3a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2V5a2 2 0 00-2-2H5zM5 11a2 2 0 00-2 2v2a2 2 0 002 2h2a2 2 0 002-2v-2a2 2 0 00-2-2H5zM11 5a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V5zM11 13a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
                  </svg>
                </button>
                <button
                  onClick={() => handleViewModeChange('list')}
                  className={`p-2 text-sm ${
                    viewMode === 'list'
                      ? 'bg-blue-500 text-white'
                      : 'bg-white text-gray-700 hover:bg-gray-50'
                  } transition-colors rounded-r-md`}
                >
                  <svg className="w-4 h-4" fill="currentColor" viewBox="0 0 20 20">
                    <path fillRule="evenodd" d="M3 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1zm0 4a1 1 0 011-1h12a1 1 0 110 2H4a1 1 0 01-1-1z" clipRule="evenodd" />
                  </svg>
                </button>
              </div>

              {/* 업로드 버튼 */}
              <button
                onClick={() => setShowUploadModal(true)}
                className="bg-blue-500 hover:bg-blue-600 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors"
              >
                이미지 업로드
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* 메인 컨텐츠 */}
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
        {/* 선택 도구 모음 */}
        {images.length > 0 && (
          <div className="flex items-center justify-between mb-6 p-4 bg-white rounded-lg shadow-sm">
            <div className="flex items-center space-x-4">
              <button
                onClick={handleSelectAll}
                className="text-sm text-blue-600 hover:text-blue-800 font-medium"
              >
                {selectedImages.length === images.length ? '전체 선택 해제' : '전체 선택'}
              </button>
              {selectedImages.length > 0 && (
                <span className="text-sm text-gray-600">
                  {selectedImages.length}개 선택됨
                </span>
              )}
            </div>
            {selectedImages.length > 0 && (
              <div className="flex items-center space-x-2">
                <button
                  onClick={() => dispatch(clearSelection())}
                  className="px-3 py-1 text-sm text-gray-600 hover:text-gray-800 border border-gray-300 rounded"
                >
                  선택 해제
                </button>
                <button
                  className="px-3 py-1 text-sm text-red-600 hover:text-red-800 border border-red-300 rounded"
                  onClick={() => {
                    if (window.confirm(`선택된 ${selectedImages.length}개의 이미지를 삭제하시겠습니까?`)) {
                      // TODO: 다중 삭제 구현
                      console.log('Delete selected images:', selectedImages);
                    }
                  }}
                >
                  삭제
                </button>
              </div>
            )}
          </div>
        )}

        {/* 에러 메시지 */}
        {error && (
          <div className="mb-6 rounded-md bg-red-50 p-4">
            <div className="flex">
              <div className="flex-shrink-0">
                <svg className="h-5 w-5 text-red-400" viewBox="0 0 20 20" fill="currentColor">
                  <path fillRule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clipRule="evenodd" />
                </svg>
              </div>
              <div className="ml-3">
                <h3 className="text-sm font-medium text-red-800">오류가 발생했습니다</h3>
                <div className="mt-2 text-sm text-red-700">
                  <p>{error}</p>
                </div>
              </div>
            </div>
          </div>
        )}

        {/* 이미지 그리드 */}
        <ImageGrid
          images={images}
          loading={isLoading}
          onImageSelect={handleImageSelect}
          selectedImages={selectedImages}
          viewMode={viewMode}
        />

        {/* 페이지네이션 */}
        {totalPages > 1 && (
          <div className="mt-8 flex items-center justify-center">
            <nav className="flex items-center space-x-2">
              <button
                onClick={() => handlePageChange(currentPage - 1)}
                disabled={!hasPrevious}
                className={`px-3 py-2 rounded-md text-sm font-medium ${
                  hasPrevious
                    ? 'text-gray-700 bg-white hover:bg-gray-50 border border-gray-300'
                    : 'text-gray-400 bg-gray-100 border border-gray-200 cursor-not-allowed'
                }`}
              >
                이전
              </button>
              
              {/* 페이지 번호들 */}
              <div className="flex items-center space-x-1">
                {Array.from({ length: Math.min(5, totalPages) }, (_, i) => {
                  const pageNum = Math.max(1, Math.min(totalPages - 4, currentPage - 2)) + i;
                  return (
                    <button
                      key={pageNum}
                      onClick={() => handlePageChange(pageNum)}
                      className={`px-3 py-2 rounded-md text-sm font-medium ${
                        pageNum === currentPage
                          ? 'text-white bg-blue-500'
                          : 'text-gray-700 bg-white hover:bg-gray-50 border border-gray-300'
                      }`}
                    >
                      {pageNum}
                    </button>
                  );
                })}
              </div>

              <button
                onClick={() => handlePageChange(currentPage + 1)}
                disabled={!hasNext}
                className={`px-3 py-2 rounded-md text-sm font-medium ${
                  hasNext
                    ? 'text-gray-700 bg-white hover:bg-gray-50 border border-gray-300'
                    : 'text-gray-400 bg-gray-100 border border-gray-200 cursor-not-allowed'
                }`}
              >
                다음
              </button>
            </nav>
          </div>
        )}
      </div>

      {/* 업로드 모달 */}
      {showUploadModal && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          <div className="flex items-center justify-center min-h-screen pt-4 px-4 pb-20 text-center sm:block sm:p-0">
            <div 
              className="fixed inset-0 transition-opacity bg-gray-500 bg-opacity-75"
              onClick={() => setShowUploadModal(false)}
            ></div>

            <div className="inline-block align-bottom bg-white rounded-lg text-left overflow-hidden shadow-xl transform transition-all sm:my-8 sm:align-middle sm:max-w-4xl sm:w-full">
              <div className="bg-white px-4 pt-5 pb-4 sm:p-6 sm:pb-4">
                <div className="flex items-center justify-between mb-4">
                  <h3 className="text-lg leading-6 font-medium text-gray-900">
                    이미지 업로드
                  </h3>
                  <button
                    onClick={() => setShowUploadModal(false)}
                    className="text-gray-400 hover:text-gray-600"
                  >
                    <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
                <ImageUpload onUploadComplete={handleUploadComplete} />
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ImageListPage;