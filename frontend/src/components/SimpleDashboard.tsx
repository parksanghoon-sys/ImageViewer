import React, { useState, useEffect } from 'react';
import ImageUpload from './ImageUpload';
import ImageGallery from './ImageGallery';

interface User {
  id: string;
  email: string;
  username: string;
  role: number;
  isActive: boolean;
  createdAt: string;
}

interface SimpleDashboardProps {
  user: User;
  onLogout: () => void;
}

type ActiveTab = 'dashboard' | 'gallery' | 'upload' | 'shared';

const SimpleDashboard: React.FC<SimpleDashboardProps> = ({ user, onLogout }) => {
  const [activeTab, setActiveTab] = useState<ActiveTab>('dashboard');
  const [authServiceStatus, setAuthServiceStatus] = useState<string>('확인 중...');
  const [imageServiceStatus, setImageServiceStatus] = useState<string>('확인 중...');
  const [shareServiceStatus, setShareServiceStatus] = useState<string>('확인 중...');

  useEffect(() => {
    checkServices();
  }, []);

  const checkServices = async () => {
    // Check AuthService
    try {
      const response = await fetch('http://localhost:5294/health');
      if (response.ok) {
        setAuthServiceStatus('정상 작동');
      } else {
        setAuthServiceStatus('오프라인');
      }
    } catch (error) {
      setAuthServiceStatus('오프라인');
    }

    // Check ImageService
    try {
      const response = await fetch('http://localhost:5215/api/image/health');
      if (response.ok) {
        setImageServiceStatus('정상 작동');
      } else {
        setImageServiceStatus('오프라인');
      }
    } catch (error) {
      setImageServiceStatus('오프라인');
    }

    // Check ShareService
    try {
      const response = await fetch('http://localhost:5125/api/share/health');
      if (response.ok) {
        setShareServiceStatus('정상 작동');
      } else {
        setShareServiceStatus('오프라인');
      }
    } catch (error) {
      setShareServiceStatus('오프라인');
    }
  };

  const handleLogout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    onLogout();
  };

  return (
    <div style={{ minHeight: '100vh', backgroundColor: '#f9fafb' }}>
      {/* Navigation */}
      <nav style={{ 
        backgroundColor: 'white', 
        boxShadow: '0 1px 3px rgba(0, 0, 0, 0.1)',
        padding: '1rem 0'
      }}>
        <div className="container" style={{ 
          display: 'flex', 
          justifyContent: 'space-between', 
          alignItems: 'center' 
        }}>
          <h1 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 'bold', color: '#1f2937' }}>
            ImageViewer
          </h1>
          <div style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <span style={{ fontSize: '0.875rem', color: '#6b7280' }}>
              {user.username} ({user.email})
            </span>
            <button onClick={handleLogout} className="btn btn-danger">
              로그아웃
            </button>
          </div>
        </div>
      </nav>

      {/* Tab Navigation */}
      <nav style={{ backgroundColor: 'white', borderBottom: '1px solid #e5e7eb' }}>
        <div className="container">
          <div style={{ display: 'flex', gap: '0' }}>
            {[
              { id: 'dashboard' as ActiveTab, label: '대시보드', icon: '📊' },
              { id: 'gallery' as ActiveTab, label: '내 이미지', icon: '🖼️' },
              { id: 'upload' as ActiveTab, label: '업로드', icon: '📤' },
              { id: 'shared' as ActiveTab, label: '공유된 이미지', icon: '🔗' }
            ].map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                style={{
                  padding: '1rem 1.5rem',
                  border: 'none',
                  backgroundColor: 'transparent',
                  color: activeTab === tab.id ? '#3b82f6' : '#6b7280',
                  fontWeight: activeTab === tab.id ? '600' : '400',
                  borderBottom: activeTab === tab.id ? '2px solid #3b82f6' : '2px solid transparent',
                  cursor: 'pointer',
                  fontSize: '0.875rem',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '0.5rem'
                }}
              >
                <span style={{ fontSize: '1rem' }}>{tab.icon}</span>
                {tab.label}
              </button>
            ))}
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container" style={{ paddingTop: '2rem' }}>
        {activeTab === 'dashboard' && (
          <>
            {/* Service Status */}
            <div className="card">
              <h2 style={{ fontSize: '1.25rem', fontWeight: 'bold', marginBottom: '1.5rem', color: '#1f2937' }}>
                서비스 상태
              </h2>
          
          <div className="grid grid-3">
            <div className="card">
              <div style={{ display: 'flex', alignItems: 'center' }}>
                <span className={`status-indicator ${
                  authServiceStatus === '정상 작동' ? 'status-green' : 'status-red'
                }`}></span>
                <div>
                  <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>
                    AuthService
                  </div>
                  <div style={{ fontSize: '1rem', fontWeight: '500', color: '#1f2937' }}>
                    {authServiceStatus}
                  </div>
                </div>
              </div>
            </div>

            <div className="card">
              <div style={{ display: 'flex', alignItems: 'center' }}>
                <span className={`status-indicator ${
                  imageServiceStatus === '정상 작동' ? 'status-green' : 'status-red'
                }`}></span>
                <div>
                  <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>
                    ImageService
                  </div>
                  <div style={{ fontSize: '1rem', fontWeight: '500', color: '#1f2937' }}>
                    {imageServiceStatus}
                  </div>
                </div>
              </div>
            </div>

            <div className="card">
              <div style={{ display: 'flex', alignItems: 'center' }}>
                <span className={`status-indicator ${
                  shareServiceStatus === '정상 작동' ? 'status-green' : 'status-red'
                }`}></span>
                <div>
                  <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>
                    ShareService
                  </div>
                  <div style={{ fontSize: '1rem', fontWeight: '500', color: '#1f2937' }}>
                    {shareServiceStatus}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>

        {/* User Info */}
        <div className="card">
          <h3 style={{ fontSize: '1.125rem', fontWeight: '500', marginBottom: '1rem', color: '#1f2937' }}>
            사용자 정보
          </h3>
          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '1rem' }}>
            <div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>사용자 ID</div>
              <div style={{ fontSize: '0.875rem', color: '#1f2937' }}>{user.id}</div>
            </div>
            <div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>이메일</div>
              <div style={{ fontSize: '0.875rem', color: '#1f2937' }}>{user.email}</div>
            </div>
            <div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>사용자명</div>
              <div style={{ fontSize: '0.875rem', color: '#1f2937' }}>{user.username}</div>
            </div>
            <div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>권한</div>
              <div style={{ fontSize: '0.875rem', color: '#1f2937' }}>
                {user.role === 2 ? 'Admin' : user.role === 1 ? 'User' : 'Guest'}
              </div>
            </div>
            <div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>계정 상태</div>
              <div style={{ fontSize: '0.875rem', color: '#1f2937' }}>
                {user.isActive ? '활성' : '비활성'}
              </div>
            </div>
            <div>
              <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>가입일</div>
              <div style={{ fontSize: '0.875rem', color: '#1f2937' }}>
                {new Date(user.createdAt).toLocaleDateString('ko-KR')}
              </div>
            </div>
          </div>
        </div>

        {/* Progress Status */}
        <div style={{ 
          backgroundColor: '#fef3c7', 
          border: '1px solid #f59e0b', 
          borderRadius: '8px', 
          padding: '1rem',
          marginBottom: '1rem'
        }}>
          <h3 style={{ fontSize: '1rem', fontWeight: '500', color: '#92400e', marginBottom: '0.75rem' }}>
            개발 진행 상황
          </h3>
          <ul style={{ margin: 0, paddingLeft: '1.25rem', color: '#92400e', fontSize: '0.875rem' }}>
            <li>✅ 모든 마이크로서비스 정상 작동 중</li>
            <li>✅ 회원가입/로그인 기능 완료</li>
            <li>✅ JWT 인증 시스템 정상 작동</li>
            <li>📋 이미지 업로드/조회 기능 구현 예정</li>
            <li>🎨 블러 처리 UI 구현 예정</li>
          </ul>
        </div>
          </>
        )}
        
        {activeTab === 'gallery' && (
          <ImageGallery />
        )}
        
        {activeTab === 'upload' && (
          <ImageUpload 
        onUploadSuccess={() => {
              setActiveTab('gallery');
            }}
            onUploadError={(error) => {
              alert(`업로드 실패: ${error}`);
            }}
          />
        )}
        
        {activeTab === 'shared' && (
          <ImageGallery showSharedImages={true} />
        )}
      </main>
    </div>
  );
};

export default SimpleDashboard;