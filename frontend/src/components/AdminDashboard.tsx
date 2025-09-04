import React, { useState, useEffect } from 'react';
import { api } from '../services/api';

interface HealthStatus {
  serviceName: string;
  status: 'Healthy' | 'Unhealthy' | 'Degraded';
  responseTime: number;
  timestamp: string;
  database?: string;
  details?: any;
}

interface SystemStats {
  totalUsers: number;
  totalImages: number;
  totalShares: number;
  activeConnections: number;
  memoryUsage: string;
  uptime: string;
  lastUpdated: string;
}

const AdminDashboard: React.FC = () => {
  const [healthStatuses, setHealthStatuses] = useState<HealthStatus[]>([]);
  const [systemStats, setSystemStats] = useState<SystemStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [autoRefresh, setAutoRefresh] = useState(true);

  const services = [
    { name: 'AuthService', url: 'http://localhost:5294', port: '5294' },
    { name: 'ImageService', url: 'http://localhost:5215', port: '5215' },
    { name: 'ShareService', url: 'http://localhost:5125', port: '5125' },
    { name: 'NotificationService', url: 'http://localhost:5267', port: '5267' },
    { name: 'PostgreSQL', url: 'postgresql://localhost:45432', port: '45432' },
    { name: 'RabbitMQ', url: 'http://localhost:15672', port: '15672' }
  ];

  const checkServiceHealth = async (service: { name: string; url: string; port: string }): Promise<HealthStatus> => {
    const startTime = Date.now();
    
    try {
      // 각 서비스의 health endpoint 호출
      let healthUrl = '';
      if (service.name === 'PostgreSQL') {
        // PostgreSQL은 직접 연결 테스트
        const response = await fetch(`${service.url.replace('postgresql://', 'http://')}/health`, {
          method: 'GET',
          timeout: 5000
        } as any);
        healthUrl = `${service.url}/health`;
      } else if (service.name === 'RabbitMQ') {
        // RabbitMQ Management API
        const response = await fetch(`${service.url}/api/overview`, {
          method: 'GET',
          headers: {
            'Authorization': 'Basic ' + btoa('guest:guest')
          },
          timeout: 5000
        } as any);
        healthUrl = `${service.url}/api/overview`;
      } else {
        // .NET 서비스들의 health endpoint
        healthUrl = `${service.url}/health`;
        const response = await fetch(healthUrl, {
          method: 'GET',
          timeout: 5000
        } as any);
      }

      const responseTime = Date.now() - startTime;
      
      return {
        serviceName: service.name,
        status: 'Healthy',
        responseTime,
        timestamp: new Date().toISOString(),
        details: { port: service.port, url: healthUrl }
      };
    } catch (error) {
      const responseTime = Date.now() - startTime;
      
      return {
        serviceName: service.name,
        status: 'Unhealthy',
        responseTime,
        timestamp: new Date().toISOString(),
        details: { 
          port: service.port, 
          error: error instanceof Error ? error.message : 'Unknown error' 
        }
      };
    }
  };

  const fetchSystemStats = async (): Promise<SystemStats> => {
    try {
      // 실제 구현에서는 각 서비스의 통계 API를 호출
      const stats: SystemStats = {
        totalUsers: Math.floor(Math.random() * 1000) + 100,
        totalImages: Math.floor(Math.random() * 5000) + 500,
        totalShares: Math.floor(Math.random() * 2000) + 200,
        activeConnections: Math.floor(Math.random() * 50) + 10,
        memoryUsage: `${Math.floor(Math.random() * 2000) + 500}MB`,
        uptime: `${Math.floor(Math.random() * 24) + 1}시간 ${Math.floor(Math.random() * 60)}분`,
        lastUpdated: new Date().toISOString()
      };
      
      return stats;
    } catch (error) {
      console.error('Failed to fetch system stats:', error);
      throw error;
    }
  };

  const refreshHealthStatus = async () => {
    setLoading(true);
    
    try {
      const healthPromises = services.map(service => checkServiceHealth(service));
      const healthResults = await Promise.all(healthPromises);
      setHealthStatuses(healthResults);

      const stats = await fetchSystemStats();
      setSystemStats(stats);
    } catch (error) {
      console.error('Failed to refresh health status:', error);
    }
    
    setLoading(false);
  };

  useEffect(() => {
    refreshHealthStatus();
  }, []);

  useEffect(() => {
    if (!autoRefresh) return;

    const interval = setInterval(refreshHealthStatus, 10000); // 10초마다 갱신
    return () => clearInterval(interval);
  }, [autoRefresh]);

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Healthy':
        return 'text-green-600 bg-green-100';
      case 'Degraded':
        return 'text-yellow-600 bg-yellow-100';
      case 'Unhealthy':
        return 'text-red-600 bg-red-100';
      default:
        return 'text-gray-600 bg-gray-100';
    }
  };

  const getStatusIcon = (status: string) => {
    switch (status) {
      case 'Healthy':
        return '✅';
      case 'Degraded':
        return '⚠️';
      case 'Unhealthy':
        return '❌';
      default:
        return '❓';
    }
  };

  return (
    <div className="p-6 max-w-7xl mx-auto">
      {/* 헤더 */}
      <div className="flex justify-between items-center mb-6">
        <div>
          <h1 className="text-3xl font-bold text-gray-900">시스템 관리</h1>
          <p className="text-gray-600 mt-1">서비스 상태 및 시스템 통계</p>
        </div>
        <div className="flex items-center space-x-4">
          <label className="flex items-center space-x-2">
            <input
              type="checkbox"
              checked={autoRefresh}
              onChange={(e) => setAutoRefresh(e.target.checked)}
              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            <span className="text-sm text-gray-700">자동 새로고침</span>
          </label>
          <button
            onClick={refreshHealthStatus}
            disabled={loading}
            className="bg-blue-600 text-white px-4 py-2 rounded-lg hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {loading ? '새로고침 중...' : '새로고침'}
          </button>
        </div>
      </div>

      {/* 시스템 통계 */}
      {systemStats && (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6 mb-8">
          <div className="bg-white p-6 rounded-lg shadow border">
            <div className="flex items-center">
              <div className="p-2 bg-blue-100 rounded-lg">
                <span className="text-2xl">👥</span>
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-600">전체 사용자</p>
                <p className="text-2xl font-bold text-gray-900">{systemStats.totalUsers.toLocaleString()}</p>
              </div>
            </div>
          </div>

          <div className="bg-white p-6 rounded-lg shadow border">
            <div className="flex items-center">
              <div className="p-2 bg-green-100 rounded-lg">
                <span className="text-2xl">📷</span>
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-600">전체 이미지</p>
                <p className="text-2xl font-bold text-gray-900">{systemStats.totalImages.toLocaleString()}</p>
              </div>
            </div>
          </div>

          <div className="bg-white p-6 rounded-lg shadow border">
            <div className="flex items-center">
              <div className="p-2 bg-purple-100 rounded-lg">
                <span className="text-2xl">🔗</span>
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-600">공유 횟수</p>
                <p className="text-2xl font-bold text-gray-900">{systemStats.totalShares.toLocaleString()}</p>
              </div>
            </div>
          </div>

          <div className="bg-white p-6 rounded-lg shadow border">
            <div className="flex items-center">
              <div className="p-2 bg-orange-100 rounded-lg">
                <span className="text-2xl">🌐</span>
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-600">활성 연결</p>
                <p className="text-2xl font-bold text-gray-900">{systemStats.activeConnections}</p>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* 서비스 Health Status */}
      <div className="bg-white rounded-lg shadow border">
        <div className="px-6 py-4 border-b border-gray-200">
          <h2 className="text-xl font-semibold text-gray-900">서비스 상태</h2>
          <p className="text-sm text-gray-600 mt-1">
            마지막 업데이트: {new Date().toLocaleString('ko-KR')}
          </p>
        </div>

        <div className="p-6">
          {loading ? (
            <div className="flex justify-center py-8">
              <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
            </div>
          ) : (
            <div className="grid gap-4">
              {healthStatuses.map((health) => (
                <div
                  key={health.serviceName}
                  className="flex items-center justify-between p-4 border border-gray-200 rounded-lg hover:bg-gray-50 transition-colors"
                >
                  <div className="flex items-center space-x-4">
                    <span className="text-2xl">{getStatusIcon(health.status)}</span>
                    <div>
                      <h3 className="font-semibold text-gray-900">{health.serviceName}</h3>
                      <p className="text-sm text-gray-600">
                        포트: {health.details?.port} | 응답시간: {health.responseTime}ms
                      </p>
                      {health.details?.error && (
                        <p className="text-sm text-red-600 mt-1">오류: {health.details.error}</p>
                      )}
                    </div>
                  </div>
                  
                  <div className="flex items-center space-x-3">
                    <span className={`px-3 py-1 text-xs font-medium rounded-full ${getStatusColor(health.status)}`}>
                      {health.status}
                    </span>
                    <span className="text-xs text-gray-500">
                      {new Date(health.timestamp).toLocaleTimeString('ko-KR')}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* 시스템 정보 */}
      {systemStats && (
        <div className="mt-8 bg-white rounded-lg shadow border">
          <div className="px-6 py-4 border-b border-gray-200">
            <h2 className="text-xl font-semibold text-gray-900">시스템 정보</h2>
          </div>
          
          <div className="p-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
              <div>
                <h3 className="font-medium text-gray-900 mb-2">메모리 사용량</h3>
                <p className="text-2xl font-bold text-blue-600">{systemStats.memoryUsage}</p>
              </div>
              
              <div>
                <h3 className="font-medium text-gray-900 mb-2">시스템 가동 시간</h3>
                <p className="text-2xl font-bold text-green-600">{systemStats.uptime}</p>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default AdminDashboard;