import React, { useState } from 'react';
import NotificationBell from './NotificationBell';
import ImageUpload from './ImageUpload';
import ImageGallery from './ImageGallery';
import ShareManagement from './ShareManagement';
import SharedImageGallery from './SharedImageGallery';
import AdminDashboard from './AdminDashboard';

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

type ActiveTab = 'dashboard' | 'gallery' | 'upload' | 'shared' | 'share-requests' | 'shared-with-me' | 'admin';

const SimpleDashboard: React.FC<SimpleDashboardProps> = ({ user, onLogout }) => {
  const [activeTab, setActiveTab] = useState<ActiveTab>('dashboard');
  const [shareRequestType, setShareRequestType] = useState<'received' | 'sent'>('received');
  
  // Admin 권한 체크 (role이 2이면 Admin)
  const isAdmin = user.role === 2;

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
          alignItems: 'center',
          flexWrap: 'wrap',
          gap: '1rem'
        }}>
          <h1 style={{ margin: 0, fontSize: '1.25rem', fontWeight: 'bold', color: '#1f2937' }}>
            ImageViewer
          </h1>
          
          {/* Desktop Navigation */}
          <div className="desktop-nav" style={{ display: 'flex', alignItems: 'center', gap: '1rem' }}>
            <NotificationBell />
            <span className="user-info" style={{ fontSize: '0.875rem', color: '#6b7280' }}>
              {user.username} ({user.email})
            </span>
            <button onClick={handleLogout} className="btn btn-danger">
              로그아웃
            </button>
          </div>
          
          {/* Mobile Navigation Toggle */}
          <div className="mobile-nav" style={{ display: 'none' }}>
            <NotificationBell />
            <button onClick={handleLogout} className="btn btn-danger">
              로그아웃
            </button>
          </div>
        </div>
      </nav>

      {/* Tab Navigation */}
      <nav style={{ backgroundColor: 'white', borderBottom: '1px solid #e5e7eb' }}>
        <div className="container">
          <div className="tab-navigation" style={{ 
            display: 'flex', 
            gap: '0',
            overflowX: 'auto',
            WebkitOverflowScrolling: 'touch',
            scrollbarWidth: 'none',
            msOverflowStyle: 'none'
          }}>
            {[
              { id: 'dashboard' as ActiveTab, label: '대시보드', icon: '📊', shortLabel: '홈' },
              { id: 'gallery' as ActiveTab, label: '내 이미지', icon: '🖼️', shortLabel: '갤러리' },
              { id: 'upload' as ActiveTab, label: '업로드', icon: '📤', shortLabel: '업로드' },
              { id: 'share-requests' as ActiveTab, label: '공유 요청', icon: '📋', shortLabel: '공유' },
              { id: 'shared-with-me' as ActiveTab, label: '공유받은 이미지', icon: '🔗', shortLabel: '수신함' },
              ...(isAdmin ? [{ id: 'admin' as ActiveTab, label: '시스템 관리', icon: '⚙️', shortLabel: '관리' }] : [])
            ].map(tab => (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className="tab-button"
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
                  gap: '0.5rem',
                  flexShrink: 0,
                  minWidth: 'auto'
                }}
              >
                <span style={{ fontSize: '1rem' }}>{tab.icon}</span>
                <span className="tab-label">{tab.label}</span>
                <span className="tab-short-label" style={{ display: 'none' }}>{tab.shortLabel}</span>
              </button>
            ))}
          </div>
        </div>
      </nav>

      {/* Main Content */}
      <main className="container" style={{ paddingTop: '2rem' }}>
        {activeTab === 'dashboard' && (
          <>
            {/* System Statistics */}
            {user.role === 2 ? ( // Admin 계정일 때만 시스템 통계 표시
              <div className="card">
                <h2 style={{ fontSize: '1.25rem', fontWeight: 'bold', marginBottom: '1.5rem', color: '#1f2937' }}>
                  시스템 통계
                </h2>
                <div className="grid grid-3">
                  <div className="card">
                    <div style={{ textAlign: 'center' }}>
                      <div style={{ fontSize: '2rem', fontWeight: 'bold', color: '#3b82f6' }}>
                        🏗️
                      </div>
                      <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>
                        마이크로서비스 아키텍처
                      </div>
                      <div style={{ fontSize: '1rem', fontWeight: '500', color: '#1f2937' }}>
                        API Gateway 통합
                      </div>
                    </div>
                  </div>

                  <div className="card">
                    <div style={{ textAlign: 'center' }}>
                      <div style={{ fontSize: '2rem', fontWeight: 'bold', color: '#10b981' }}>
                        🐰
                      </div>
                      <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>
                        메시지 큐
                      </div>
                      <div style={{ fontSize: '1rem', fontWeight: '500', color: '#1f2937' }}>
                        RabbitMQ 연동
                      </div>
                    </div>
                  </div>

                  <div className="card">
                    <div style={{ textAlign: 'center' }}>
                      <div style={{ fontSize: '2rem', fontWeight: 'bold', color: '#8b5cf6' }}>
                        🐋
                      </div>
                      <div style={{ fontSize: '0.875rem', color: '#6b7280', marginBottom: '0.25rem' }}>
                        컨테이너화
                      </div>
                      <div style={{ fontSize: '1rem', fontWeight: '500', color: '#1f2937' }}>
                        Docker 배포
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ) : ( // 일반 사용자일 때는 간단한 환영 메시지
              <div className="card">
                <h2 style={{ fontSize: '1.5rem', fontWeight: 'bold', marginBottom: '1rem', color: '#1f2937', textAlign: 'center' }}>
                  환영합니다! 🎉
                </h2>
                <p style={{ fontSize: '1rem', color: '#6b7280', textAlign: 'center', marginBottom: '2rem' }}>
                  ImageViewer에서 이미지를 안전하게 저장하고 공유해보세요.
                </p>
                <div className="grid grid-3">
                  <div className="card" style={{ textAlign: 'center', cursor: 'pointer' }} 
                       onClick={() => setActiveTab('upload')}>
                    <div style={{ fontSize: '3rem', marginBottom: '0.5rem' }}>📤</div>
                    <div style={{ fontSize: '1rem', fontWeight: '500', color: '#3b82f6' }}>
                      이미지 업로드
                    </div>
                  </div>
                  
                  <div className="card" style={{ textAlign: 'center', cursor: 'pointer' }}
                       onClick={() => setActiveTab('gallery')}>
                    <div style={{ fontSize: '3rem', marginBottom: '0.5rem' }}>🖼️</div>
                    <div style={{ fontSize: '1rem', fontWeight: '500', color: '#10b981' }}>
                      내 이미지 보기
                    </div>
                  </div>
                  
                  <div className="card" style={{ textAlign: 'center', cursor: 'pointer' }}
                       onClick={() => setActiveTab('share-requests')}>
                    <div style={{ fontSize: '3rem', marginBottom: '0.5rem' }}>🔗</div>
                    <div style={{ fontSize: '1rem', fontWeight: '500', color: '#f59e0b' }}>
                      이미지 공유
                    </div>
                  </div>
                </div>
              </div>
            )}

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
          <ImageGallery key={Date.now()} />
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
        
        {activeTab === 'share-requests' && (
          <>
            <div style={{ display: 'flex', gap: '1rem', marginBottom: '2rem' }}>
              <button
                onClick={() => setShareRequestType('received')}
                className="btn"
                style={{ 
                  backgroundColor: shareRequestType === 'received' ? '#3b82f6' : 'white',
                  color: shareRequestType === 'received' ? 'white' : '#374151',
                  border: shareRequestType === 'received' ? 'none' : '1px solid #d1d5db'
                }}
              >
                받은 요청
              </button>
              <button
                onClick={() => setShareRequestType('sent')}
                className="btn"
                style={{ 
                  backgroundColor: shareRequestType === 'sent' ? '#3b82f6' : 'white',
                  color: shareRequestType === 'sent' ? 'white' : '#374151',
                  border: shareRequestType === 'sent' ? 'none' : '1px solid #d1d5db'
                }}
              >
                보낸 요청
              </button>
            </div>
            <ShareManagement type={shareRequestType} />
          </>
        )}
        
        {activeTab === 'shared-with-me' && (
          <SharedImageGallery />
        )}

        {activeTab === 'admin' && isAdmin && (
          <AdminDashboard />
        )}
      </main>
    </div>
  );
};

export default SimpleDashboard;