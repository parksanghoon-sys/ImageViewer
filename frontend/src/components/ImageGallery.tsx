import React, { useState, useEffect } from 'react';
import BlurredImage from './BlurredImage';
import ShareImageModal from './ShareImageModal';

interface Image {
  id: string;
  title: string;
  description: string;
  tags: string[];
  isPublic: boolean;
  imageUrl: string;
  thumbnailUrl: string;
  uploadedAt: string;
  fileSize: number;
  contentType: string;
  userId: string;
  width?: number;
  height?: number;
  fileName?: string;
  userName?: string;
  thumbnailReady?: boolean;
  isOwner?: boolean;
}

interface UserSettings {
  previewCount: number;
  previewSize: number;
  blurIntensity: number;
  darkMode: boolean;
}

interface ImageGalleryProps {
  userId?: string;
  showSharedImages?: boolean;
}

type ViewMode = 'grid' | 'list';

const ImageGallery: React.FC<ImageGalleryProps> = ({ userId, showSharedImages = false }) => {
  const [images, setImages] = useState<Image[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [viewMode, setViewMode] = useState<ViewMode>('grid');
  const [searchTerm, setSearchTerm] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [selectedImage, setSelectedImage] = useState<Image | null>(null);
  const [shareModalOpen, setShareModalOpen] = useState(false);
  const [shareImageId, setShareImageId] = useState<string>('');
  const [shareImageTitle, setShareImageTitle] = useState<string>('');
  const [userSettings, setUserSettings] = useState<UserSettings>({
    previewCount: 12,
    previewSize: 200,
    blurIntensity: 50,
    darkMode: false
  });

  const imagesPerPage = userSettings.previewCount;

  useEffect(() => {
    loadImages();
    loadUserSettings();
  }, [userId, showSharedImages]);

  const loadImages = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('accessToken');
      const endpoint = showSharedImages 
        ? '/api/image/shared' 
        : userId 
          ? `/api/image/user/${userId}/images` 
          : '/api/image/my-images';

      const headers: any = {};
      if (token) {
        headers['Authorization'] = `Bearer ${token}`;
      }

      const response = await fetch(`http://localhost:5215${endpoint}`, {
        headers
      });

      if (response.ok) {
        const result = await response.json();
        console.log('API Response:', result); // 디버깅용
        setImages(result.data?.images || result.data?.Images || []);
      } else {
        setError(`이미지를 불러오는데 실패했습니다. (${response.status})`);
      }
    } catch (err: any) {
      setError('네트워크 오류가 발생했습니다.');
    } finally {
      setLoading(false);
    }
  };

  const loadUserSettings = async () => {
    try {
      const savedSettings = localStorage.getItem('userSettings');
      if (savedSettings) {
        setUserSettings(JSON.parse(savedSettings));
      }
    } catch (err) {
      // Use default settings
    }
  };

  const saveUserSettings = (settings: UserSettings) => {
    setUserSettings(settings);
    localStorage.setItem('userSettings', JSON.stringify(settings));
  };

  const filteredImages = images.filter(image => 
    image.title?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    image.description?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    image.tags?.some(tag => tag?.toLowerCase().includes(searchTerm.toLowerCase()))
  );

  const totalPages = Math.ceil(filteredImages.length / imagesPerPage);
  const startIndex = (currentPage - 1) * imagesPerPage;
  const paginatedImages = filteredImages.slice(startIndex, startIndex + imagesPerPage);

  const openImageModal = (image: Image) => {
    setSelectedImage(image);
  };

  const closeImageModal = () => {
    setSelectedImage(null);
  };

  const openShareModal = (imageId: string, imageTitle: string) => {
    setShareImageId(imageId);
    setShareImageTitle(imageTitle);
    setShareModalOpen(true);
  };

  const closeShareModal = () => {
    setShareModalOpen(false);
    setShareImageId('');
    setShareImageTitle('');
  };

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  if (loading) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        <div style={{ fontSize: '1.125rem', color: '#6b7280' }}>이미지를 불러오는 중...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        <div style={{ fontSize: '1.125rem', color: '#dc2626' }}>{error}</div>
        <button onClick={loadImages} className="btn btn-primary" style={{ marginTop: '1rem' }}>
          다시 시도
        </button>
      </div>
    );
  }

  return (
    <div style={{ padding: '2rem' }}>
      {/* Gallery Header */}
      <div className="card" style={{ marginBottom: '2rem' }}>
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: '1rem' }}>
          <h2 style={{ fontSize: '1.5rem', fontWeight: 'bold', color: '#1f2937', margin: 0 }}>
            {showSharedImages ? '공유된 이미지' : '내 이미지'} ({filteredImages.length}개)
          </h2>
          
          <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
            {/* Search */}
            <input
              type="text"
              placeholder="이미지 검색..."
              className="input"
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setCurrentPage(1);
              }}
              style={{ width: '200px' }}
            />

            {/* View Mode Toggle */}
            <div style={{ display: 'flex', border: '1px solid #d1d5db', borderRadius: '6px', overflow: 'hidden' }}>
              <button
                onClick={() => setViewMode('grid')}
                style={{
                  padding: '0.5rem 1rem',
                  border: 'none',
                  backgroundColor: viewMode === 'grid' ? '#3b82f6' : 'white',
                  color: viewMode === 'grid' ? 'white' : '#374151',
                  cursor: 'pointer'
                }}
              >
                그리드
              </button>
              <button
                onClick={() => setViewMode('list')}
                style={{
                  padding: '0.5rem 1rem',
                  border: 'none',
                  backgroundColor: viewMode === 'list' ? '#3b82f6' : 'white',
                  color: viewMode === 'list' ? 'white' : '#374151',
                  cursor: 'pointer'
                }}
              >
                리스트
              </button>
            </div>
          </div>
        </div>
      </div>

      {/* Settings Panel - CORE BLUR PROCESSING FEATURE */}
      <div className="card" style={{ marginBottom: '2rem' }}>
        <h3 style={{ fontSize: '1.125rem', fontWeight: '500', marginBottom: '1rem', color: '#1f2937' }}>
          표시 설정
        </h3>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}>
              페이지당 이미지 수: {userSettings.previewCount}
            </label>
            <input
              type="range"
              min="6"
              max="24"
              step="6"
              value={userSettings.previewCount}
              onChange={(e) => saveUserSettings({ ...userSettings, previewCount: parseInt(e.target.value) })}
              style={{ width: '100%' }}
            />
          </div>
          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}>
              미리보기 크기: {userSettings.previewSize}px
            </label>
            <input
              type="range"
              min="150"
              max="300"
              step="25"
              value={userSettings.previewSize}
              onChange={(e) => saveUserSettings({ ...userSettings, previewSize: parseInt(e.target.value) })}
              style={{ width: '100%' }}
            />
          </div>
          <div>
            <label style={{ display: 'block', marginBottom: '0.5rem', fontSize: '0.875rem', fontWeight: '500', color: '#374151' }}>
              블러 강도: {userSettings.blurIntensity}%
            </label>
            <input
              type="range"
              min="0"
              max="100"
              step="10"
              value={userSettings.blurIntensity}
              onChange={(e) => saveUserSettings({ ...userSettings, blurIntensity: parseInt(e.target.value) })}
              style={{ width: '100%' }}
            />
          </div>
        </div>
      </div>

      {/* Image Grid/List */}
      {paginatedImages.length === 0 ? (
        <div className="card" style={{ textAlign: 'center', padding: '3rem' }}>
          <div style={{ fontSize: '3rem', marginBottom: '1rem', opacity: 0.5 }}>📷</div>
          <div style={{ fontSize: '1.125rem', color: '#6b7280' }}>
            {searchTerm ? '검색 결과가 없습니다.' : '업로드된 이미지가 없습니다.'}
          </div>
        </div>
      ) : (
        <>
          {viewMode === 'grid' ? (
            <div style={{ 
              display: 'grid', 
              gridTemplateColumns: `repeat(auto-fill, minmax(${userSettings.previewSize}px, 1fr))`, 
              gap: '1.5rem',
              marginBottom: '2rem'
            }}>
              {paginatedImages.map(image => (
                <div key={image.id} className="card" style={{ padding: 0, overflow: 'hidden', cursor: 'pointer' }}>
                  <div 
                    onClick={() => openImageModal(image)}
                    style={{
                      position: 'relative',
                      overflow: 'hidden'
                    }}
                  >
                    <BlurredImage
                      src={image.thumbnailUrl || image.imageUrl}
                      alt={image.title}
                      blurIntensity={userSettings.blurIntensity}
                      previewSize={userSettings.previewSize}
                      className="w-full"
                      style={{
                        width: '100%',
                        height: `${userSettings.previewSize}px`,
                        objectFit: 'cover'
                      }}
                    />
                    {userSettings.blurIntensity > 0 && (
                      <div style={{
                        position: 'absolute',
                        top: '0.5rem',
                        right: '0.5rem',
                        backgroundColor: 'rgba(0, 0, 0, 0.7)',
                        color: 'white',
                        padding: '0.25rem 0.5rem',
                        borderRadius: '4px',
                        fontSize: '0.75rem'
                      }}>
                        미리보기
                      </div>
                    )}
                  </div>
                  <div style={{ padding: '1rem' }}>
                    <h4 style={{ margin: 0, marginBottom: '0.5rem', fontSize: '1rem', fontWeight: '500', color: '#1f2937' }}>
                      {image.title}
                    </h4>
                    <p style={{ margin: 0, fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.5rem' }}>
                      {image.description || '설명 없음'}
                    </p>
                    <div style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: '0.75rem' }}>
                      {new Date(image.uploadedAt).toLocaleDateString('ko-KR')} • {formatFileSize(image.fileSize)}
                    </div>
                    {!showSharedImages && (
                      <div style={{ display: 'flex', gap: '0.5rem' }}>
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            openShareModal(image.id, image.title);
                          }}
                          className="btn"
                          style={{
                            fontSize: '0.75rem',
                            padding: '0.25rem 0.5rem',
                            backgroundColor: '#3b82f6',
                            color: 'white',
                            display: 'flex',
                            alignItems: 'center',
                            gap: '0.25rem'
                          }}
                        >
                          🔗 공유
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <div style={{ marginBottom: '2rem' }}>
              {paginatedImages.map(image => (
                <div key={image.id} className="card" style={{ marginBottom: '1rem', padding: '1rem' }}>
                  <div style={{ display: 'grid', gridTemplateColumns: '100px 1fr auto', gap: '1rem', alignItems: 'center' }}>
                    <BlurredImage
                      src={image.thumbnailUrl || image.imageUrl}
                      alt={image.title}
                      blurIntensity={userSettings.blurIntensity}
                      previewSize={100}
                      style={{
                        width: '100px',
                        height: '100px',
                        objectFit: 'cover',
                        borderRadius: '8px',
                        cursor: 'pointer'
                      }}
                      onClick={() => openImageModal(image)}
                    />
                    <div>
                      <h4 style={{ margin: 0, marginBottom: '0.5rem', fontSize: '1.125rem', fontWeight: '500', color: '#1f2937' }}>
                        {image.title}
                      </h4>
                      <p style={{ margin: 0, fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.5rem' }}>
                        {image.description || '설명 없음'}
                      </p>
                      <div style={{ fontSize: '0.75rem', color: '#9ca3af' }}>
                        {new Date(image.uploadedAt).toLocaleDateString('ko-KR')} • {formatFileSize(image.fileSize)}
                      </div>
                    </div>
                    <div style={{ display: 'flex', gap: '0.5rem' }}>
                      <button 
                        onClick={() => openImageModal(image)}
                        className="btn btn-primary"
                        style={{ padding: '0.5rem 1rem' }}
                      >
                        보기
                      </button>
                      {!showSharedImages && (
                        <button
                          onClick={(e) => {
                            e.stopPropagation();
                            openShareModal(image.id, image.title);
                          }}
                          className="btn"
                          style={{
                            padding: '0.5rem 1rem',
                            backgroundColor: '#3b82f6',
                            color: 'white'
                          }}
                        >
                          공유
                        </button>
                      )}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}

          {/* Pagination */}
          {totalPages > 1 && (
            <div style={{ display: 'flex', justifyContent: 'center', gap: '0.5rem', marginTop: '2rem' }}>
              <button
                onClick={() => setCurrentPage(prev => Math.max(1, prev - 1))}
                disabled={currentPage === 1}
                className="btn"
                style={{ 
                  padding: '0.5rem 1rem',
                  backgroundColor: currentPage === 1 ? '#f3f4f6' : 'white',
                  color: currentPage === 1 ? '#9ca3af' : '#374151'
                }}
              >
                이전
              </button>
              
              {Array.from({ length: totalPages }, (_, i) => i + 1).map(page => (
                <button
                  key={page}
                  onClick={() => setCurrentPage(page)}
                  className="btn"
                  style={{
                    padding: '0.5rem 1rem',
                    backgroundColor: currentPage === page ? '#3b82f6' : 'white',
                    color: currentPage === page ? 'white' : '#374151'
                  }}
                >
                  {page}
                </button>
              ))}
              
              <button
                onClick={() => setCurrentPage(prev => Math.min(totalPages, prev + 1))}
                disabled={currentPage === totalPages}
                className="btn"
                style={{ 
                  padding: '0.5rem 1rem',
                  backgroundColor: currentPage === totalPages ? '#f3f4f6' : 'white',
                  color: currentPage === totalPages ? '#9ca3af' : '#374151'
                }}
              >
                다음
              </button>
            </div>
          )}
        </>
      )}

      {/* Image Modal */}
      {selectedImage && (
        <div 
          style={{
            position: 'fixed',
            top: 0,
            left: 0,
            right: 0,
            bottom: 0,
            backgroundColor: 'rgba(0, 0, 0, 0.9)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            zIndex: 1000,
            padding: '2rem'
          }}
          onClick={closeImageModal}
        >
          <div 
            style={{ 
              maxWidth: '90vw', 
              maxHeight: '90vh', 
              backgroundColor: 'white', 
              borderRadius: '8px', 
              overflow: 'hidden',
              display: 'flex',
              flexDirection: 'column'
            }}
            onClick={(e) => e.stopPropagation()}
          >
            <div style={{ position: 'relative' }}>
              <img
                src={selectedImage.imageUrl}
                alt={selectedImage.title}
                style={{
                  maxWidth: '80vw',
                  maxHeight: '70vh',
                  objectFit: 'contain'
                }}
              />
              <button
                onClick={closeImageModal}
                style={{
                  position: 'absolute',
                  top: '1rem',
                  right: '1rem',
                  width: '2rem',
                  height: '2rem',
                  borderRadius: '50%',
                  backgroundColor: 'rgba(0, 0, 0, 0.7)',
                  color: 'white',
                  border: 'none',
                  cursor: 'pointer',
                  fontSize: '1.25rem'
                }}
              >
                ×
              </button>
            </div>
            <div style={{ padding: '1.5rem' }}>
              <h3 style={{ margin: 0, marginBottom: '0.5rem', fontSize: '1.25rem', fontWeight: '600', color: '#1f2937' }}>
                {selectedImage.title}
              </h3>
              <p style={{ margin: 0, marginBottom: '1rem', color: '#6b7280' }}>
                {selectedImage.description || '설명 없음'}
              </p>
              <div style={{ fontSize: '0.875rem', color: '#9ca3af' }}>
                업로드: {new Date(selectedImage.uploadedAt).toLocaleString('ko-KR')} • 
                크기: {formatFileSize(selectedImage.fileSize)} • 
                형식: {selectedImage.contentType}
              </div>
              {selectedImage.tags && selectedImage.tags.length > 0 && (
                <div style={{ marginTop: '1rem' }}>
                  {selectedImage.tags.map(tag => (
                    <span 
                      key={tag}
                      style={{
                        display: 'inline-block',
                        backgroundColor: '#f3f4f6',
                        color: '#374151',
                        padding: '0.25rem 0.5rem',
                        borderRadius: '4px',
                        fontSize: '0.75rem',
                        marginRight: '0.5rem',
                        marginBottom: '0.5rem'
                      }}
                    >
                      #{tag}
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>
        </div>
      )}

      {/* Share Modal */}
      <ShareImageModal
        imageId={shareImageId}
        imageTitle={shareImageTitle}
        isOpen={shareModalOpen}
        onClose={closeShareModal}
        onShareSuccess={() => {
          closeShareModal();
          // Optionally reload images or show success message
        }}
      />
    </div>
  );
};

export default ImageGallery;