import React, { useState } from 'react';
import { ImageMetadata } from '../../types/api';

interface ImageGridProps {
  images: ImageMetadata[];
  loading?: boolean;
  onImageSelect?: (imageId: string) => void;
  selectedImages?: string[];
  viewMode?: 'grid' | 'list';
}

const ImageGrid: React.FC<ImageGridProps> = ({
  images,
  loading = false,
  onImageSelect,
  selectedImages = [],
  viewMode = 'grid'
}) => {
  const [loadedImages, setLoadedImages] = useState<Set<string>>(new Set());
  const [imageErrors, setImageErrors] = useState<Set<string>>(new Set());

  const handleImageLoad = (imageId: string) => {
    setLoadedImages(prev => new Set([...prev, imageId]));
  };

  const handleImageError = (imageId: string) => {
    setImageErrors(prev => new Set([...prev, imageId]));
  };

  const formatFileSize = (bytes: number): string => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const formatDate = (dateString: string): string => {
    return new Date(dateString).toLocaleDateString('ko-KR', {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  };

  const getImageUrl = (image: ImageMetadata): string => {
    // 썸네일이 준비된 경우 썸네일 사용, 아니면 원본 이미지 사용
    return image.thumbnailReady && image.thumbnailUrl 
      ? `${process.env.REACT_APP_IMAGE_API_URL || 'https://localhost:7002'}${image.thumbnailUrl}`
      : `${process.env.REACT_APP_IMAGE_API_URL || 'https://localhost:7002'}${image.imageUrl}`;
  };

  if (loading) {
    return (
      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
        {Array.from({ length: 8 }).map((_, index) => (
          <div key={index} className="bg-gray-200 animate-pulse rounded-lg">
            <div className="aspect-square bg-gray-300 rounded-t-lg"></div>
            <div className="p-4 space-y-2">
              <div className="h-4 bg-gray-300 rounded w-3/4"></div>
              <div className="h-3 bg-gray-300 rounded w-1/2"></div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  if (images.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center py-12">
        <svg
          className="w-12 h-12 text-gray-400 mb-4"
          fill="none"
          stroke="currentColor"
          viewBox="0 0 24 24"
        >
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={2}
            d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z"
          />
        </svg>
        <h3 className="text-lg font-medium text-gray-900 mb-2">아직 업로드된 이미지가 없습니다</h3>
        <p className="text-gray-500 text-center">
          첫 번째 이미지를 업로드해보세요!
        </p>
      </div>
    );
  }

  if (viewMode === 'list') {
    return (
      <div className="space-y-4">
        {images.map((image) => (
          <div
            key={image.id}
            className={`flex items-center space-x-4 p-4 border rounded-lg hover:bg-gray-50 transition-colors ${
              selectedImages.includes(image.id) ? 'bg-blue-50 border-blue-200' : 'border-gray-200'
            }`}
            onClick={() => onImageSelect?.(image.id)}
          >
            {/* 체크박스 */}
            {onImageSelect && (
              <input
                type="checkbox"
                checked={selectedImages.includes(image.id)}
                onChange={() => onImageSelect(image.id)}
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
              />
            )}

            {/* 썸네일 */}
            <div className="flex-shrink-0 w-16 h-16">
              <img
                src={getImageUrl(image)}
                alt={image.title}
                className="w-full h-full object-cover rounded-lg"
                onLoad={() => handleImageLoad(image.id)}
                onError={() => handleImageError(image.id)}
              />
            </div>

            {/* 이미지 정보 */}
            <div className="flex-1 min-w-0">
              <div className="flex items-center justify-between">
                <div>
                  <h3 className="text-sm font-medium text-gray-900 truncate">
                    {image.title}
                  </h3>
                  <p className="text-sm text-gray-500 truncate">
                    {image.fileName}
                  </p>
                </div>
                <div className="text-right">
                  <p className="text-sm text-gray-900">{formatFileSize(image.fileSize)}</p>
                  <p className="text-xs text-gray-500">{image.width} × {image.height}</p>
                </div>
              </div>
              {image.description && (
                <p className="text-sm text-gray-600 mt-1 truncate">
                  {image.description}
                </p>
              )}
              <div className="flex items-center justify-between mt-2">
                <div className="flex items-center space-x-2">
                  {image.tags.length > 0 && (
                    <div className="flex flex-wrap gap-1">
                      {image.tags.slice(0, 3).map((tag, index) => (
                        <span
                          key={index}
                          className="inline-block px-2 py-1 text-xs font-medium text-blue-800 bg-blue-100 rounded-full"
                        >
                          {tag}
                        </span>
                      ))}
                      {image.tags.length > 3 && (
                        <span className="text-xs text-gray-500">+{image.tags.length - 3}</span>
                      )}
                    </div>
                  )}
                  {image.isPublic && (
                    <span className="inline-block px-2 py-1 text-xs font-medium text-green-800 bg-green-100 rounded-full">
                      공개
                    </span>
                  )}
                </div>
                <p className="text-xs text-gray-500">{formatDate(image.uploadedAt)}</p>
              </div>
            </div>
          </div>
        ))}
      </div>
    );
  }

  return (
    <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5 gap-4">
      {images.map((image) => (
        <div
          key={image.id}
          className={`group relative bg-white rounded-lg shadow-sm border hover:shadow-md transition-all duration-200 cursor-pointer ${
            selectedImages.includes(image.id) ? 'ring-2 ring-blue-500 border-blue-200' : 'border-gray-200'
          }`}
          onClick={() => onImageSelect?.(image.id)}
        >
          {/* 체크박스 */}
          {onImageSelect && (
            <div className="absolute top-2 left-2 z-10">
              <input
                type="checkbox"
                checked={selectedImages.includes(image.id)}
                onChange={(e) => {
                  e.stopPropagation();
                  onImageSelect(image.id);
                }}
                className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
              />
            </div>
          )}

          {/* 공개/비공개 표시 */}
          {image.isPublic && (
            <div className="absolute top-2 right-2 z-10">
              <span className="inline-block px-2 py-1 text-xs font-medium text-green-800 bg-green-100 rounded-full">
                공개
              </span>
            </div>
          )}

          {/* 이미지 */}
          <div className="aspect-square relative overflow-hidden rounded-t-lg">
            {imageErrors.has(image.id) ? (
              <div className="w-full h-full flex items-center justify-center bg-gray-100">
                <svg className="w-8 h-8 text-gray-400" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M4 3a2 2 0 00-2 2v10a2 2 0 002 2h12a2 2 0 002-2V5a2 2 0 00-2-2H4zm12 12H4l4-8 3 6 2-4 3 6z" clipRule="evenodd" />
                </svg>
              </div>
            ) : (
              <>
                {!loadedImages.has(image.id) && (
                  <div className="absolute inset-0 bg-gray-200 animate-pulse"></div>
                )}
                <img
                  src={getImageUrl(image)}
                  alt={image.title}
                  className={`w-full h-full object-cover transition-opacity duration-300 ${
                    loadedImages.has(image.id) ? 'opacity-100' : 'opacity-0'
                  }`}
                  onLoad={() => handleImageLoad(image.id)}
                  onError={() => handleImageError(image.id)}
                />
                {/* 썸네일 생성 중 표시 */}
                {!image.thumbnailReady && (
                  <div className="absolute inset-0 bg-black bg-opacity-50 flex items-center justify-center">
                    <div className="text-white text-xs text-center">
                      <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-white mx-auto mb-2"></div>
                      썸네일 생성 중...
                    </div>
                  </div>
                )}
              </>
            )}
          </div>

          {/* 이미지 정보 */}
          <div className="p-3">
            <h3 className="text-sm font-medium text-gray-900 truncate mb-1">
              {image.title}
            </h3>
            <p className="text-xs text-gray-500 truncate mb-2">
              {image.fileName}
            </p>
            
            {/* 태그 */}
            {image.tags.length > 0 && (
              <div className="flex flex-wrap gap-1 mb-2">
                {image.tags.slice(0, 2).map((tag, index) => (
                  <span
                    key={index}
                    className="inline-block px-1.5 py-0.5 text-xs font-medium text-blue-800 bg-blue-100 rounded"
                  >
                    {tag}
                  </span>
                ))}
                {image.tags.length > 2 && (
                  <span className="text-xs text-gray-500">+{image.tags.length - 2}</span>
                )}
              </div>
            )}

            {/* 메타데이터 */}
            <div className="flex items-center justify-between text-xs text-gray-500">
              <span>{formatFileSize(image.fileSize)}</span>
              <span>{image.width} × {image.height}</span>
            </div>
            
            {/* 업로드 날짜 */}
            <p className="text-xs text-gray-400 mt-1">
              {formatDate(image.uploadedAt)}
            </p>
          </div>
        </div>
      ))}
    </div>
  );
};

export default ImageGrid;