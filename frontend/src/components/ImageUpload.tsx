import React, { useState, useRef, DragEvent, ChangeEvent } from 'react';

interface ImageUploadProps {
  onUploadSuccess?: (images: any[]) => void;
  onUploadError?: (error: string) => void;
}

interface ImageFile {
  file: File;
  preview: string;
  title: string;
  description: string;
  tags: string;
  isPublic: boolean;
  uploading: boolean;
  progress: number;
  error?: string;
}

const ImageUpload: React.FC<ImageUploadProps> = ({ onUploadSuccess, onUploadError }) => {
  const [selectedImages, setSelectedImages] = useState<ImageFile[]>([]);
  const [isDragOver, setIsDragOver] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleDragOver = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(false);
  };

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragOver(false);
    
    const files = Array.from(e.dataTransfer.files);
    handleFiles(files);
  };

  const handleFileSelect = (e: ChangeEvent<HTMLInputElement>) => {
    const files = Array.from(e.target.files || []);
    handleFiles(files);
  };

  const handleFiles = (files: File[]) => {
    const validFiles = files.filter(file => {
      const isImage = file.type.startsWith('image/');
      const isValidSize = file.size <= 10 * 1024 * 1024; // 10MB limit
      return isImage && isValidSize;
    });

    const newImageFiles: ImageFile[] = validFiles.map(file => ({
      file,
      preview: URL.createObjectURL(file),
      title: file.name.replace(/\.[^/.]+$/, ''), // Remove extension
      description: '',
      tags: '',
      isPublic: false,
      uploading: false,
      progress: 0
    }));

    setSelectedImages(prev => [...prev, ...newImageFiles]);
  };

  const updateImageData = (index: number, field: keyof ImageFile, value: any) => {
    setSelectedImages(prev => prev.map((img, i) => 
      i === index ? { ...img, [field]: value } : img
    ));
  };

  const removeImage = (index: number) => {
    setSelectedImages(prev => {
      const newImages = [...prev];
      URL.revokeObjectURL(newImages[index].preview);
      newImages.splice(index, 1);
      return newImages;
    });
  };

  const uploadImage = async (imageFile: ImageFile, index: number) => {
    updateImageData(index, 'uploading', true);
    updateImageData(index, 'progress', 0);

    try {
      const formData = new FormData();
      formData.append('file', imageFile.file);
      formData.append('title', imageFile.title);
      formData.append('description', imageFile.description);
      formData.append('tags', imageFile.tags);
      formData.append('isPublic', imageFile.isPublic.toString());

      const token = localStorage.getItem('accessToken');
      const headers: any = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const response = await fetch('http://localhost:5215/api/image/upload', {
        method: 'POST',
        headers,
        body: formData
      });

      if (response.ok) {
        const result = await response.json();
        updateImageData(index, 'progress', 100);
        return result;
      } else {
        const errorText = await response.text();
        throw new Error(`업로드 실패 (${response.status}): ${errorText}`);
      }
    } catch (error: any) {
      updateImageData(index, 'error', error.message);
      throw error;
    } finally {
      updateImageData(index, 'uploading', false);
    }
  };

  const uploadAllImages = async () => {
    const uploadPromises = selectedImages.map((img, index) => uploadImage(img, index));
    
    try {
      const results = await Promise.allSettled(uploadPromises);
      const successful = results.filter(result => result.status === 'fulfilled');
      const failed = results.filter(result => result.status === 'rejected');

      if (successful.length > 0) {
        onUploadSuccess?.(successful.map(result => (result as any).value));
      }

      if (failed.length > 0) {
        onUploadError?.(`${failed.length}개의 이미지 업로드가 실패했습니다.`);
      }

      // Clear uploaded images
      setSelectedImages([]);
    } catch (error: any) {
      onUploadError?.(error.message);
    }
  };

  return (
    <div style={{ padding: '2rem' }}>
      <div className="card">
        <h2 style={{ fontSize: '1.5rem', fontWeight: 'bold', marginBottom: '1.5rem', color: '#1f2937' }}>
          이미지 업로드
        </h2>

        {/* Drop Zone */}
        <div
          onDragOver={handleDragOver}
          onDragLeave={handleDragLeave}
          onDrop={handleDrop}
          onClick={() => fileInputRef.current?.click()}
          style={{
            border: `2px dashed ${isDragOver ? '#3b82f6' : '#d1d5db'}`,
            borderRadius: '8px',
            padding: '3rem',
            textAlign: 'center',
            cursor: 'pointer',
            backgroundColor: isDragOver ? '#f0f9ff' : '#f9fafb',
            marginBottom: '2rem',
            transition: 'all 0.2s ease'
          }}
        >
          <div style={{ fontSize: '3rem', marginBottom: '1rem', opacity: 0.5 }}>📷</div>
          <div style={{ fontSize: '1.125rem', fontWeight: '500', color: '#374151', marginBottom: '0.5rem' }}>
            이미지를 드래그하여 놓거나 클릭하여 선택하세요
          </div>
          <div style={{ fontSize: '0.875rem', color: '#6b7280' }}>
            JPG, PNG, GIF 파일만 지원 (최대 10MB)
          </div>
        </div>

        <input
          ref={fileInputRef}
          type="file"
          multiple
          accept="image/*"
          onChange={handleFileSelect}
          style={{ display: 'none' }}
        />

        {/* Selected Images */}
        {selectedImages.length > 0 && (
          <div>
            <h3 style={{ fontSize: '1.125rem', fontWeight: '500', marginBottom: '1rem', color: '#1f2937' }}>
              선택된 이미지 ({selectedImages.length}개)
            </h3>

            <div style={{ display: 'grid', gap: '1.5rem' }}>
              {selectedImages.map((imageFile, index) => (
                <div key={index} className="card" style={{ padding: '1.5rem' }}>
                  <div style={{ display: 'grid', gridTemplateColumns: '150px 1fr auto', gap: '1rem', alignItems: 'start' }}>
                    {/* Preview */}
                    <img
                      src={imageFile.preview}
                      alt="Preview"
                      style={{
                        width: '150px',
                        height: '150px',
                        objectFit: 'cover',
                        borderRadius: '8px'
                      }}
                    />

                    {/* Form Fields */}
                    <div style={{ display: 'grid', gap: '1rem' }}>
                      <div>
                        <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}>
                          제목
                        </label>
                        <input
                          type="text"
                          className="input"
                          value={imageFile.title}
                          onChange={(e) => updateImageData(index, 'title', e.target.value)}
                          placeholder="이미지 제목"
                        />
                      </div>

                      <div>
                        <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}>
                          설명
                        </label>
                        <textarea
                          className="input"
                          value={imageFile.description}
                          onChange={(e) => updateImageData(index, 'description', e.target.value)}
                          placeholder="이미지 설명"
                          rows={3}
                          style={{ resize: 'vertical' }}
                        />
                      </div>

                      <div>
                        <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}>
                          태그
                        </label>
                        <input
                          type="text"
                          className="input"
                          value={imageFile.tags}
                          onChange={(e) => updateImageData(index, 'tags', e.target.value)}
                          placeholder="태그 (쉼표로 구분)"
                        />
                      </div>

                      <div style={{ display: 'flex', alignItems: 'center', gap: '0.5rem' }}>
                        <input
                          type="checkbox"
                          id={`public-${index}`}
                          checked={imageFile.isPublic}
                          onChange={(e) => updateImageData(index, 'isPublic', e.target.checked)}
                          style={{ width: '1rem', height: '1rem' }}
                        />
                        <label htmlFor={`public-${index}`} style={{ fontSize: '0.875rem', color: '#374151' }}>
                          다른 사용자와 공유 허용
                        </label>
                      </div>

                      {/* Upload Progress */}
                      {imageFile.uploading && (
                        <div>
                          <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.5rem' }}>
                            업로드 중... {imageFile.progress}%
                          </div>
                          <div style={{ width: '100%', height: '8px', backgroundColor: '#e5e7eb', borderRadius: '4px', overflow: 'hidden' }}>
                            <div 
                              style={{ 
                                width: `${imageFile.progress}%`, 
                                height: '100%', 
                                backgroundColor: '#3b82f6',
                                transition: 'width 0.3s ease'
                              }}
                            />
                          </div>
                        </div>
                      )}

                      {/* Error */}
                      {imageFile.error && (
                        <div style={{ fontSize: '0.875rem', color: '#dc2626', padding: '0.5rem', backgroundColor: '#fef2f2', borderRadius: '4px' }}>
                          {imageFile.error}
                        </div>
                      )}
                    </div>

                    {/* Remove Button */}
                    <button
                      onClick={() => removeImage(index)}
                      style={{
                        width: '2rem',
                        height: '2rem',
                        borderRadius: '50%',
                        backgroundColor: '#ef4444',
                        color: 'white',
                        border: 'none',
                        cursor: 'pointer',
                        fontSize: '1rem',
                        display: 'flex',
                        alignItems: 'center',
                        justifyContent: 'center'
                      }}
                      title="제거"
                    >
                      ×
                    </button>
                  </div>
                </div>
              ))}
            </div>

            {/* Upload All Button */}
            <div style={{ marginTop: '2rem', textAlign: 'center' }}>
              <button
                onClick={uploadAllImages}
                className="btn btn-primary"
                disabled={selectedImages.some(img => img.uploading)}
                style={{ padding: '0.75rem 2rem', fontSize: '1rem' }}
              >
                {selectedImages.some(img => img.uploading) ? '업로드 중...' : `모든 이미지 업로드 (${selectedImages.length}개)`}
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ImageUpload;