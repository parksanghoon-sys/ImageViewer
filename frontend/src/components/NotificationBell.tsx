import React, { useState, useRef, useEffect } from 'react';
import { BellIcon } from '@heroicons/react/24/outline';
import { BellIcon as BellSolidIcon } from '@heroicons/react/24/solid';
import { useNotification } from '../contexts/NotificationContext';

interface NotificationBellProps {
  className?: string;
}

const NotificationBell: React.FC<NotificationBellProps> = ({ className = '' }) => {
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);
  const buttonRef = useRef<HTMLButtonElement>(null);
  const { notifications, unreadCount, markAsRead, markAllAsRead, isConnected } = useNotification();

  // 외부 클릭 시 팝업 닫기
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (
        dropdownRef.current && 
        !dropdownRef.current.contains(event.target as Node) &&
        buttonRef.current &&
        !buttonRef.current.contains(event.target as Node)
      ) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => {
      document.removeEventListener('mousedown', handleClickOutside);
    };
  }, []);

  // 알림 타입별 메시지 포맷팅
  const formatNotificationMessage = (notification: any) => {
    if (notification.message) return notification.message;
    if (notification.title) return notification.title;
    
    switch (notification.type) {
      case 'ShareRequest':
        return '이미지 공유를 요청받았습니다.';
      case 'ShareApproved':
        return '이미지 공유 요청이 승인되었습니다.';
      case 'ShareRejected':
        return '이미지 공유 요청이 거절되었습니다.';
      case 'ImageUploaded':
        return '새 이미지가 업로드되었습니다.';
      default:
        return notification.message || '새 알림이 있습니다.';
    }
  };

  // 알림 타입별 색상
  const getNotificationColor = (type: string) => {
    switch (type) {
      case 'ShareRequest':
        return 'text-blue-600';
      case 'ShareApproved':
        return 'text-green-600';
      case 'ShareRejected':
        return 'text-red-600';
      case 'ImageUploaded':
        return 'text-purple-600';
      default:
        return 'text-gray-600';
    }
  };

  // 시간 포맷팅
  const formatTime = (dateString: string) => {
    const date = new Date(dateString);
    const now = new Date();
    const diffInHours = Math.floor((now.getTime() - date.getTime()) / (1000 * 60 * 60));
    
    if (diffInHours < 1) {
      return '방금 전';
    } else if (diffInHours < 24) {
      return `${diffInHours}시간 전`;
    } else {
      const diffInDays = Math.floor(diffInHours / 24);
      return `${diffInDays}일 전`;
    }
  };

  const handleNotificationClick = (notification: any) => {
    if (!notification.isRead) {
      markAsRead(notification.id);
    }
  };

  return (
    <div className="relative">
      {/* 알림 벨 버튼 */}
      <button
        ref={buttonRef}
        onClick={() => setIsOpen(!isOpen)}
        className={`relative p-2 text-gray-600 hover:text-gray-900 hover:bg-gray-100 rounded-full transition-colors duration-200 ${className}`}
      >
        {unreadCount > 0 ? (
          <BellSolidIcon className="h-6 w-6 text-blue-600" />
        ) : (
          <BellIcon className="h-6 w-6" />
        )}
        
        {/* 읽지 않은 알림 개수 배지 */}
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 bg-red-500 text-white text-xs font-bold rounded-full min-w-[18px] h-[18px] flex items-center justify-center px-1">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </button>

      {/* 알림 팝업 */}
      {isOpen && (
        <div
          ref={dropdownRef}
          className="absolute right-0 mt-2 w-80 bg-white border border-gray-200 rounded-lg shadow-lg z-50 max-h-96 overflow-hidden"
        >
          {/* 팝업 헤더 */}
          <div className="px-4 py-3 border-b border-gray-200 bg-gray-50">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold text-gray-900">
                알림 {!isConnected && <span className="text-xs text-red-500">(오프라인)</span>}
              </h3>
              {unreadCount > 0 && (
                <button
                  onClick={markAllAsRead}
                  className="text-sm text-blue-600 hover:text-blue-800 font-medium"
                >
                  모두 읽음
                </button>
              )}
            </div>
          </div>

          {/* 알림 목록 */}
          <div className="max-h-80 overflow-y-auto">
            {notifications.length === 0 ? (
              <div className="px-4 py-8 text-center text-gray-500">
                <BellIcon className="h-12 w-12 mx-auto mb-2 text-gray-300" />
                <p>새로운 알림이 없습니다.</p>
              </div>
            ) : (
              <div className="divide-y divide-gray-100">
                {notifications.map((notification) => (
                  <div
                    key={notification.id}
                    onClick={() => handleNotificationClick(notification)}
                    className={`px-4 py-3 cursor-pointer transition-colors duration-200 ${
                      notification.isRead 
                        ? 'bg-white hover:bg-gray-50' 
                        : 'bg-blue-50 hover:bg-blue-100'
                    }`}
                  >
                    <div className="flex items-start space-x-3">
                      {/* 읽음/안읽음 표시 점 */}
                      <div className={`w-2 h-2 rounded-full mt-2 flex-shrink-0 ${
                        notification.isRead ? 'bg-gray-300' : 'bg-blue-500'
                      }`} />
                      
                      <div className="flex-1 min-w-0">
                        <p className={`text-sm ${
                          notification.isRead ? 'text-gray-700' : 'text-gray-900 font-medium'
                        } ${getNotificationColor(notification.type)}`}>
                          {formatNotificationMessage(notification)}
                        </p>
                        <p className="text-xs text-gray-500 mt-1">
                          {formatTime(notification.createdAt)}
                        </p>
                      </div>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>

          {/* 팝업 푸터 */}
          {notifications.length > 0 && (
            <div className="px-4 py-2 border-t border-gray-200 bg-gray-50">
              <button
                onClick={() => setIsOpen(false)}
                className="text-sm text-gray-600 hover:text-gray-800"
              >
                알림 창 닫기
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
};

export default NotificationBell;