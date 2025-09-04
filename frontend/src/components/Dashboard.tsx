import React, { useState, useEffect } from 'react';
import { authService } from '../services/api';
import NotificationBell from './NotificationBell';

interface User {
  id: string;
  email: string;
  username: string;
  role: number;
  isActive: boolean;
  createdAt: string;
}

interface DashboardProps {
  user: User;
  onLogout: () => void;
}

const Dashboard: React.FC<DashboardProps> = ({ user, onLogout }) => {
  const [authServiceStatus, setAuthServiceStatus] = useState<string>('확인 중...');
  const [imageServiceStatus, setImageServiceStatus] = useState<string>('서비스 없음');
  const [shareServiceStatus, setShareServiceStatus] = useState<string>('서비스 없음');
  

  useEffect(() => {
    checkServiceStatus();
  }, []);

  const checkServiceStatus = async () => {
    // Check AuthService by trying to get users
    try {
      await authService.getUsers();
      setAuthServiceStatus('정상 작동');
    } catch (error) {
      setAuthServiceStatus('오프라인');
    }

    // Check ImageService (will be available when EF issue is fixed)
    // TODO: Add ImageService health check when service is running
    
    // Check ShareService (will be available when EF issue is fixed)
    // TODO: Add ShareService health check when service is running
  };

  const handleLogout = () => {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('user');
    onLogout();
  };


  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-xl font-semibold text-gray-900">ImageViewer</h1>
            </div>
            <div className="flex items-center space-x-4">
              <NotificationBell />
              <span className="text-sm text-gray-500">
                {user.username} ({user.email})
              </span>
              <button
                onClick={handleLogout}
                className="bg-red-600 hover:bg-red-700 text-white px-3 py-2 rounded-md text-sm font-medium"
              >
                로그아웃
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="border-4 border-dashed border-gray-200 rounded-lg p-8">
            <h2 className="text-2xl font-bold text-gray-900 mb-6">서비스 상태</h2>
            
            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className={`w-3 h-3 rounded-full ${
                        authServiceStatus === '정상 작동' ? 'bg-green-500' : 'bg-red-500'
                      }`}></div>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          AuthService
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {authServiceStatus}
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className={`w-3 h-3 rounded-full ${
                        imageServiceStatus === '정상 작동' ? 'bg-green-500' : 'bg-gray-400'
                      }`}></div>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          ImageService
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {imageServiceStatus}
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>

              <div className="bg-white overflow-hidden shadow rounded-lg">
                <div className="p-5">
                  <div className="flex items-center">
                    <div className="flex-shrink-0">
                      <div className={`w-3 h-3 rounded-full ${
                        shareServiceStatus === '정상 작동' ? 'bg-green-500' : 'bg-gray-400'
                      }`}></div>
                    </div>
                    <div className="ml-5 w-0 flex-1">
                      <dl>
                        <dt className="text-sm font-medium text-gray-500 truncate">
                          ShareService
                        </dt>
                        <dd className="text-lg font-medium text-gray-900">
                          {shareServiceStatus}
                        </dd>
                      </dl>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="mt-8">
              <h3 className="text-lg font-medium text-gray-900 mb-4">사용자 정보</h3>
              <div className="bg-white shadow overflow-hidden sm:rounded-md">
                <div className="px-4 py-5 sm:p-6">
                  <div className="grid grid-cols-1 gap-x-4 gap-y-6 sm:grid-cols-2">
                    <div>
                      <dt className="text-sm font-medium text-gray-500">사용자 ID</dt>
                      <dd className="mt-1 text-sm text-gray-900">{user.id}</dd>
                    </div>
                    <div>
                      <dt className="text-sm font-medium text-gray-500">이메일</dt>
                      <dd className="mt-1 text-sm text-gray-900">{user.email}</dd>
                    </div>
                    <div>
                      <dt className="text-sm font-medium text-gray-500">사용자명</dt>
                      <dd className="mt-1 text-sm text-gray-900">{user.username}</dd>
                    </div>
                    <div>
                      <dt className="text-sm font-medium text-gray-500">권한</dt>
                      <dd className="mt-1 text-sm text-gray-900">
                        {user.role === 2 ? 'Admin' : user.role === 1 ? 'User' : 'Guest'}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-sm font-medium text-gray-500">계정 상태</dt>
                      <dd className="mt-1 text-sm text-gray-900">
                        {user.isActive ? '활성' : '비활성'}
                      </dd>
                    </div>
                    <div>
                      <dt className="text-sm font-medium text-gray-500">가입일</dt>
                      <dd className="mt-1 text-sm text-gray-900">
                        {new Date(user.createdAt).toLocaleDateString('ko-KR')}
                      </dd>
                    </div>
                  </div>
                </div>
              </div>
            </div>

            <div className="mt-8">
              <div className="bg-yellow-50 border border-yellow-200 rounded-md p-4">
                <div className="flex">
                  <div className="ml-3">
                    <h3 className="text-sm font-medium text-yellow-800">
                      개발 진행 상황
                    </h3>
                    <div className="mt-2 text-sm text-yellow-700">
                      <ul className="list-disc list-inside space-y-1">
                        <li>✅ AuthService 정상 작동 중</li>
                        <li>⚠️ ImageService, ShareService EntityFramework 버전 충돌로 수정 필요</li>
                        <li>🔄 프론트엔드 기본 구조 구현 완료</li>
                        <li>📋 이미지 업로드/조회 기능 구현 예정</li>
                        <li>🎨 블러 처리 UI 구현 예정</li>
                      </ul>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
};

export default Dashboard;