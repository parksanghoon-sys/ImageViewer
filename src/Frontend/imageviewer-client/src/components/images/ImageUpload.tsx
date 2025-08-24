import React, { useState, useCallback } from 'react';
import { useAppDispatch, useAppSelector, selectImages } from '../../store';
import { uploadImageAsync, resetUploadState } from '../../store/slices/imageSlice';

interface ImageUploadProps {
  onUploadComplete?: () => void;
  maxFiles?: number;
  maxFileSize?: number; // bytes
}

const ImageUpload: React.FC<ImageUploadProps> = ({
  onUploadComplete,
  maxFiles = 10,
  maxFileSize = 10 * 1024 * 1024, // 10MB
}) => {
  const dispatch = useAppDispatch();
  const { isUploading, uploadProgress, error } = useAppSelector(selectImages);

  const [dragActive, setDragActive] = useState(false);
  const [selectedFiles, setSelectedFiles] = useState<File[]>([]);
  const [previews, setPreviews] = useState<string[]>([]);
  const [descriptions, setDescriptions] = useState<{ [key: string]: string }>({});
  const [titles, setTitles] = useState<{ [key: string]: string }>({});
  const [tags, setTags] = useState<{ [key: string]: string }>({});
  const [isPublic, setIsPublic] = useState<{ [key: string]: boolean }>({});

  // 파일 유효성 검사
  const validateFile = (file: File): string | null => {
    // 파일 크기 검사
    if (file.size > maxFileSize) {
      return `파일 크기가 너무 큽니다. (최대 ${(maxFileSize / 1024 / 1024).toFixed(1)}MB)`;
    }

    // 파일 타입 검사
    const allowedTypes = ['image/jpeg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
      return '지원되지 않는 파일 형식입니다. (JPEG, PNG, GIF, WebP만 허용)';
    }

    return null;
  };

  // 파일 선택 처리
  const handleFiles = useCallback((files: FileList | File[]) => {
    const fileArray = Array.from(files);
    const validFiles: File[] = [];
    const newPreviews: string[] = [];
    let errorMessages: string[] = [];

    fileArray.forEach((file) => {
      const validationError = validateFile(file);
      if (validationError) {
        errorMessages.push(`${file.name}: ${validationError}`);
      } else {
        validFiles.push(file);
        // 미리보기 이미지 생성
        const reader = new FileReader();
        reader.onload = (e) => {
          newPreviews.push(e.target?.result as string);
          if (newPreviews.length === validFiles.length) {
            setPreviews(prev => [...prev, ...newPreviews]);
          }
        };
        reader.readAsDataURL(file);
      }
    });

    if (errorMessages.length > 0) {
      alert(errorMessages.join('\n'));
    }

    // 최대 파일 개수 확인
    const totalFiles = selectedFiles.length + validFiles.length;
    if (totalFiles > maxFiles) {
      alert(`최대 ${maxFiles}개의 파일만 업로드할 수 있습니다.`);
      return;
    }

    setSelectedFiles(prev => [...prev, ...validFiles]);
  }, [selectedFiles, maxFiles, maxFileSize]);

  // 드래그 앤 드롭 핸들러
  const handleDrag = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    if (e.type === 'dragenter' || e.type === 'dragover') {
      setDragActive(true);
    } else if (e.type === 'dragleave') {
      setDragActive(false);
    }
  }, []);

  const handleDrop = useCallback((e: React.DragEvent) => {
    e.preventDefault();
    e.stopPropagation();
    setDragActive(false);
    
    if (e.dataTransfer.files) {
      handleFiles(e.dataTransfer.files);
    }
  }, [handleFiles]);

  // 파일 선택 핸들러
  const handleFileSelect = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files) {
      handleFiles(e.target.files);
    }
  };

  // 파일 제거
  const removeFile = (index: number) => {
    setSelectedFiles(prev => prev.filter((_, i) => i !== index));
    setPreviews(prev => prev.filter((_, i) => i !== index));
    
    // 해당 파일의 모든 정보 제거
    const fileKey = `file_${index}`;
    setDescriptions(prev => {
      const newDescriptions = { ...prev };
      delete newDescriptions[fileKey];
      return newDescriptions;
    });
    setTitles(prev => {
      const newTitles = { ...prev };
      delete newTitles[fileKey];
      return newTitles;
    });
    setTags(prev => {
      const newTags = { ...prev };
      delete newTags[fileKey];
      return newTags;
    });
    setIsPublic(prev => {
      const newIsPublic = { ...prev };
      delete newIsPublic[fileKey];
      return newIsPublic;
    });
  };

  // 제목 변경
  const handleTitleChange = (index: number, title: string) => {
    const fileKey = `file_${index}`;
    setTitles(prev => ({
      ...prev,
      [fileKey]: title,
    }));
  };

  // 설명 변경
  const handleDescriptionChange = (index: number, description: string) => {
    const fileKey = `file_${index}`;
    setDescriptions(prev => ({
      ...prev,
      [fileKey]: description,
    }));
  };

  // 태그 변경
  const handleTagsChange = (index: number, tagsStr: string) => {
    const fileKey = `file_${index}`;
    setTags(prev => ({
      ...prev,
      [fileKey]: tagsStr,
    }));
  };

  // 공개 여부 변경
  const handlePublicChange = (index: number, isPublicValue: boolean) => {
    const fileKey = `file_${index}`;
    setIsPublic(prev => ({
      ...prev,
      [fileKey]: isPublicValue,
    }));
  };

  // 업로드 실행
  const handleUpload = async () => {
    if (selectedFiles.length === 0) {
      alert('업로드할 파일을 선택해주세요.');
      return;
    }

    for (let i = 0; i < selectedFiles.length; i++) {
      const file = selectedFiles[i];
      const fileKey = `file_${i}`;
      const title = titles[fileKey] || '';
      const description = descriptions[fileKey] || '';
      const tagsStr = tags[fileKey] || '';
      const isPublicValue = isPublic[fileKey] || false;

      try {
        await dispatch(uploadImageAsync({ 
          file, 
          title,
          description, 
          tags: tagsStr,
          isPublic: isPublicValue 
        })).unwrap();
      } catch (error) {
        console.error(`File upload failed for ${file.name}:`, error);
        // 개별 파일 업로드 실패 시에도 계속 진행
      }
    }

    // 업로드 완료 후 초기화
    setSelectedFiles([]);
    setPreviews([]);
    setDescriptions({});
    setTitles({});
    setTags({});
    setIsPublic({});
    dispatch(resetUploadState());
    
    if (onUploadComplete) {
      onUploadComplete();
    }
  };

  // 모든 파일 제거
  const clearAll = () => {
    setSelectedFiles([]);
    setPreviews([]);
    setDescriptions({});
    setTitles({});
    setTags({});
    setIsPublic({});
    dispatch(resetUploadState());
  };

  return (
    <div className="w-full max-w-4xl mx-auto p-6">
      {/* 드래그 앤 드롭 영역 */}
      <div
        className={`relative border-2 border-dashed rounded-lg p-8 text-center transition-colors ${
          dragActive
            ? 'border-primary-400 bg-primary-50'
            : 'border-gray-300 hover:border-gray-400'
        }`}
        onDragEnter={handleDrag}
        onDragLeave={handleDrag}
        onDragOver={handleDrag}
        onDrop={handleDrop}
      >
        <svg
          className="mx-auto h-12 w-12 text-gray-400"
          stroke="currentColor"
          fill="none"
          viewBox="0 0 48 48"
        >
          <path
            d="M28 8H12a4 4 0 00-4 4v20m32-12v8m0 0v8a4 4 0 01-4 4H12a4 4 0 01-4-4v-4m32-4l-3.172-3.172a4 4 0 00-5.656 0L28 28M8 32l9.172-9.172a4 4 0 015.656 0L28 28m0 0l4 4m4-24h8m-4-4v8m-12 4h.02"
            strokeWidth={2}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
        <div className="mt-4">
          <label htmlFor="file-upload" className="cursor-pointer">
            <span className="mt-2 block text-sm font-medium text-gray-900">
              이미지를 여기에 드래그하거나{' '}
              <span className="text-primary-600 hover:text-primary-500">
                클릭하여 선택
              </span>
            </span>
            <input
              id="file-upload"
              name="file-upload"
              type="file"
              className="sr-only"
              multiple
              accept="image/*"
              onChange={handleFileSelect}
              disabled={isUploading}
            />
          </label>
          <p className="mt-1 text-xs text-gray-500">
            PNG, JPG, GIF, WebP 파일 (최대 {maxFiles}개, 각 파일 {(maxFileSize / 1024 / 1024).toFixed(1)}MB 이하)
          </p>
        </div>
      </div>

      {/* 에러 메시지 */}
      {error && (
        <div className="mt-4 rounded-md bg-red-50 p-4">
          <div className="text-sm text-red-700">{error}</div>
        </div>
      )}

      {/* 선택된 파일들 */}
      {selectedFiles.length > 0 && (
        <div className="mt-6">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-medium text-gray-900">
              선택된 파일들 ({selectedFiles.length})
            </h3>
            <button
              type="button"
              onClick={clearAll}
              className="text-sm text-gray-500 hover:text-gray-700"
              disabled={isUploading}
            >
              모두 제거
            </button>
          </div>

          <div className="space-y-4">
            {selectedFiles.map((file, index) => (
              <div key={`${file.name}_${index}`} className="flex items-start space-x-4 p-4 border border-gray-200 rounded-lg">
                {/* 미리보기 이미지 */}
                {previews[index] && (
                  <div className="flex-shrink-0">
                    <img
                      src={previews[index]}
                      alt={`Preview ${index}`}
                      className="h-16 w-16 object-cover rounded-lg"
                    />
                  </div>
                )}

                {/* 파일 정보 */}
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium text-gray-900 truncate">
                        {file.name}
                      </p>
                      <p className="text-sm text-gray-500">
                        {(file.size / 1024 / 1024).toFixed(2)} MB
                      </p>
                    </div>
                    <button
                      type="button"
                      onClick={() => removeFile(index)}
                      className="ml-4 text-red-400 hover:text-red-600"
                      disabled={isUploading}
                    >
                      <svg className="h-5 w-5" fill="currentColor" viewBox="0 0 20 20">
                        <path
                          fillRule="evenodd"
                          d="M4.293 4.293a1 1 0 011.414 0L10 8.586l4.293-4.293a1 1 0 111.414 1.414L11.414 10l4.293 4.293a1 1 0 01-1.414 1.414L10 11.414l-4.293 4.293a1 1 0 01-1.414-1.414L8.586 10 4.293 5.707a1 1 0 010-1.414z"
                          clipRule="evenodd"
                        />
                      </svg>
                    </button>
                  </div>

                  {/* 제목 입력 */}
                  <div className="mt-2">
                    <input
                      type="text"
                      placeholder="이미지 제목 (필수)"
                      value={titles[`file_${index}`] || ''}
                      onChange={(e) => handleTitleChange(index, e.target.value)}
                      className="w-full text-sm border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                      disabled={isUploading}
                      required
                    />
                  </div>

                  {/* 설명 입력 */}
                  <div className="mt-2">
                    <textarea
                      placeholder="이미지 설명 (선택사항)"
                      value={descriptions[`file_${index}`] || ''}
                      onChange={(e) => handleDescriptionChange(index, e.target.value)}
                      className="w-full text-sm border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                      rows={2}
                      disabled={isUploading}
                    />
                  </div>

                  {/* 태그 입력 */}
                  <div className="mt-2">
                    <input
                      type="text"
                      placeholder="태그 (쉼표로 구분, 예: 풍경, 여행, 자연)"
                      value={tags[`file_${index}`] || ''}
                      onChange={(e) => handleTagsChange(index, e.target.value)}
                      className="w-full text-sm border border-gray-300 rounded-md px-3 py-2 focus:outline-none focus:ring-2 focus:ring-primary-500 focus:border-primary-500"
                      disabled={isUploading}
                    />
                  </div>

                  {/* 공개 여부 체크박스 */}
                  <div className="mt-2 flex items-center">
                    <input
                      type="checkbox"
                      id={`public_${index}`}
                      checked={isPublic[`file_${index}`] || false}
                      onChange={(e) => handlePublicChange(index, e.target.checked)}
                      className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
                      disabled={isUploading}
                    />
                    <label 
                      htmlFor={`public_${index}`} 
                      className="ml-2 text-sm text-gray-700 cursor-pointer"
                    >
                      공개 이미지로 설정 (다른 사용자가 볼 수 있습니다)
                    </label>
                  </div>
                </div>
              </div>
            ))}
          </div>

          {/* 업로드 진행률 */}
          {isUploading && (
            <div className="mt-4">
              <div className="flex items-center justify-between text-sm text-gray-600 mb-2">
                <span>업로드 중...</span>
                <span>{uploadProgress}%</span>
              </div>
              <div className="w-full bg-gray-200 rounded-full h-2">
                <div
                  className="bg-primary-600 h-2 rounded-full transition-all duration-300"
                  style={{ width: `${uploadProgress}%` }}
                />
              </div>
            </div>
          )}

          {/* 업로드 버튼 */}
          <div className="mt-6 flex justify-end space-x-3">
            <button
              type="button"
              onClick={clearAll}
              className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
              disabled={isUploading}
            >
              취소
            </button>
            <button
              type="button"
              onClick={handleUpload}
              disabled={isUploading || selectedFiles.length === 0}
              className="px-4 py-2 bg-primary-600 border border-transparent rounded-md text-sm font-medium text-white hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isUploading ? (
                <div className="flex items-center">
                  <div className="spinner mr-2"></div>
                  업로드 중...
                </div>
              ) : (
                `${selectedFiles.length}개 파일 업로드`
              )}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};

export default ImageUpload;