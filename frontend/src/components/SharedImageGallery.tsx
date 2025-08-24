import React, { useState, useEffect } from 'react';
import BlurredImage from './BlurredImage';

interface SharedImage {
  shareRequestId: string;
  imageId: string;
  originalFileName: string;
  fileSize: number;
  width: number;
  height: number;
  description: string;
  ownerId: string;
  ownerEmail: string;
  ownerUsername: string;
  sharedAt: string;
  thumbnailPath: string;
}

interface UserSettings {
  previewCount: number;
  previewSize: number;
  blurIntensity: number;
  darkMode: boolean;
}

const SharedImageGallery: React.FC = () => {
  const [sharedImages, setSharedImages] = useState<SharedImage[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [selectedImage, setSelectedImage] = useState<SharedImage | null>(null);
  const [searchTerm, setSearchTerm] = useState('');
  const [userSettings, setUserSettings] = useState<UserSettings>({
    previewCount: 12,
    previewSize: 200,
    blurIntensity: 50,
    darkMode: false
  });

  useEffect(() => {
    loadSharedImages();
    loadUserSettings();
  }, [currentPage]);

  const loadUserSettings = () => {
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

  const loadSharedImages = async () => {
    setLoading(true);
    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch(
        `http://localhost:5125/api/share/shared-with-me?page=${currentPage}&pageSize=${userSettings.previewCount}`,
        {
          headers: {
            'Authorization': `Bearer ${token}`
          }
        }
      );

      if (response.ok) {
        const result = await response.json();
        if (result.success) {
          setSharedImages(result.data.sharedImages);
          setTotalPages(result.data.pagination.totalPages);
        } else {
          setError(result.message || '공유된 이미지를 불러오는데 실패했습니다.');
        }
      } else {
        setError('공유된 이미지를 불러오는데 실패했습니다.');
      }
    } catch (err: any) {
      setError('네트워크 오류가 발생했습니다.');
    } finally {
      setLoading(false);
    }
  };

  const filteredImages = sharedImages.filter(image => 
    image.originalFileName.toLowerCase().includes(searchTerm.toLowerCase()) ||
    image.description?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    image.ownerEmail.toLowerCase().includes(searchTerm.toLowerCase()) ||
    image.ownerUsername.toLowerCase().includes(searchTerm.toLowerCase())
  );

  const openImageModal = (image: SharedImage) => {
    setSelectedImage(image);
  };

  const closeImageModal = () => {
    setSelectedImage(null);
  };

  const formatFileSize = (bytes: number) => {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  };

  const formatDate = (dateString: string) => {
    return new Date(dateString).toLocaleString('ko-KR');
  };

  if (loading) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        <div style={{ fontSize: '1.125rem', color: '#6b7280' }}>
          공유된 이미지를 불러오는 중...
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div style={{ padding: '2rem', textAlign: 'center' }}>
        <div style={{ fontSize: '1.125rem', color: '#dc2626', marginBottom: '1rem' }}>
          {error}
        </div>
        <button onClick={loadSharedImages} className="btn btn-primary">
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
            나와 공유된 이미지 ({filteredImages.length}개)
          </h2>
          
          <div style={{ display: 'flex', gap: '1rem', alignItems: 'center' }}>
            {/* Search */}
            <input
              type="text"
              placeholder="이미지 또는 소유자 검색..."
              className="input"
              value={searchTerm}
              onChange={(e) => {
                setSearchTerm(e.target.value);
                setCurrentPage(1);
              }}
              style={{ width: '200px' }}
            />
          </div>
        </div>
      </div>

      {/* Settings Panel */}
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

      {/* Image Grid */}
      {filteredImages.length === 0 ? (
        <div className="card" style={{ textAlign: 'center', padding: '3rem' }}>
          <div style={{ fontSize: '3rem', marginBottom: '1rem', opacity: 0.5 }}>🔗</div>
          <div style={{ fontSize: '1.125rem', color: '#6b7280' }}>
            {searchTerm ? '검색 결과가 없습니다.' : '공유된 이미지가 없습니다.'}
          </div>
        </div>
      ) : (
        <>
          <div style={{ 
            display: 'grid', 
            gridTemplateColumns: `repeat(auto-fill, minmax(${userSettings.previewSize}px, 1fr))`, 
            gap: '1.5rem',
            marginBottom: '2rem'
          }}>
            {filteredImages.map(image => (
              <div key={image.shareRequestId} className="card" style={{ padding: 0, overflow: 'hidden', cursor: 'pointer' }}>
                <div 
                  onClick={() => openImageModal(image)}
                  style={{
                    position: 'relative',
                    overflow: 'hidden'
                  }}
                >
                  <BlurredImage
                    src={`http://localhost:5215${image.thumbnailPath}`}
                    alt={image.originalFileName}
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
                      공유됨
                    </div>
                  )}
                </div>
                <div style={{ padding: '1rem' }}>
                  <h4 style={{ margin: 0, marginBottom: '0.5rem', fontSize: '1rem', fontWeight: '500', color: '#1f2937' }}>
                    {image.originalFileName}
                  </h4>
                  <p style={{ margin: 0, fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.5rem' }}>
                    {image.description || '설명 없음'}
                  </p>
                  <div style={{ fontSize: '0.75rem', color: '#9ca3af', marginBottom: '0.5rem' }}>
                    소유자: {image.ownerUsername || image.ownerEmail}
                  </div>
                  <div style={{ fontSize: '0.75rem', color: '#9ca3af' }}>
                    공유일: {formatDate(image.sharedAt)} • {formatFileSize(image.fileSize)}
                  </div>
                </div>
              </div>
            ))}
          </div>

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
                src={`http://localhost:5215${selectedImage.thumbnailPath}`} // In real implementation, use full image path
                alt={selectedImage.originalFileName}
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
                {selectedImage.originalFileName}
              </h3>
              <p style={{ margin: 0, marginBottom: '1rem', color: '#6b7280' }}>
                {selectedImage.description || '설명 없음'}
              </p>
              <div style={{ fontSize: '0.875rem', color: '#9ca3af' }}>
                소유자: {selectedImage.ownerUsername || selectedImage.ownerEmail} • 
                크기: {formatFileSize(selectedImage.fileSize)} • 
                해상도: {selectedImage.width}×{selectedImage.height} • 
                공유일: {formatDate(selectedImage.sharedAt)}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default SharedImageGallery;