import React, { useEffect, useRef, useState } from 'react';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { useNotification } from '../contexts/NotificationContext';
import { toast } from 'react-hot-toast';

interface NotificationProviderProps {
  children: React.ReactNode;
}

interface RealTimeNotification {
  id: string;
  title: string;
  message: string;
  type: 'ImageUploaded' | 'ShareRequest' | 'ShareApproved' | 'ShareRejected' | 'SystemUpdate' | 'Security' | 'Warning' | 'Info';
  priority: 'low' | 'medium' | 'high' | 'urgent';
  category: 'system' | 'social' | 'update' | 'security';
  userId: string;
  data?: any;
  createdAt: string;
}

const NotificationProvider: React.FC<NotificationProviderProps> = ({ children }) => {
  const [connection, setConnection] = useState<HubConnection | null>(null);
  const [connectionStatus, setConnectionStatus] = useState<'connecting' | 'connected' | 'disconnected' | 'error'>('disconnected');
  const connectionRef = useRef<HubConnection | null>(null);
  const { addNotification } = useNotification();

  // Get user token for SignalR authentication
  const getAuthToken = () => {
    return localStorage.getItem('accessToken');
  };

  // Handle incoming notifications
  const handleNotification = (notification: RealTimeNotification) => {
    console.log('Received real-time notification:', notification);
    
    // Add to notification context
    addNotification({
      id: notification.id,
      title: notification.title,
      message: notification.message,
      type: notification.type,
      priority: notification.priority,
      category: notification.category,
      isRead: false,
      createdAt: notification.createdAt,
      actionUrl: notification.data?.actionUrl
    });

    // Show toast based on notification type
    switch (notification.type) {
      case 'ImageUploaded':
        toast.success(`📷 이미지 업로드 완료: ${notification.data?.filename || '새 이미지'}`);
        break;
      case 'ShareRequest':
        toast(`📋 ${notification.data?.requesterName || '사용자'}님이 이미지 공유를 요청했습니다.`, {
          icon: '🔔',
          duration: 5000
        });
        break;
      case 'ShareApproved':
        toast.success(`✅ ${notification.data?.ownerName || '사용자'}님이 공유 요청을 승인했습니다.`);
        break;
      case 'ShareRejected':
        toast.error(`❌ 공유 요청이 거절되었습니다.`);
        break;
      default:
        if (notification.priority === 'high' || notification.priority === 'urgent') {
          toast.error(notification.title + (notification.message ? `: ${notification.message}` : ''));
        } else {
          toast(notification.title + (notification.message ? `: ${notification.message}` : ''), {
            icon: '🔔',
          });
        }
    }
  };

  // Connect to SignalR hub
  const connectToHub = async () => {
    const token = getAuthToken();
    if (!token) {
      console.log('No auth token, skipping SignalR connection');
      return;
    }

    if (connectionRef.current && connectionRef.current.state === 'Connected') {
      console.log('Already connected to SignalR hub');
      return;
    }

    setConnectionStatus('connecting');
    
    try {
      const newConnection = new HubConnectionBuilder()
        .withUrl(`http://localhost:5004/notificationHub`, {
          accessTokenFactory: () => token,
          withCredentials: false
        })
        .withAutomaticReconnect()
        .configureLogging(process.env.NODE_ENV === 'development' ? LogLevel.Information : LogLevel.Error)
        .build();

      // Setup event handlers
      newConnection.onclose((error) => {
        console.log('SignalR connection closed:', error);
        setConnectionStatus('disconnected');
        setConnection(null);
      });

      newConnection.onreconnecting((error) => {
        console.log('SignalR reconnecting:', error);
        setConnectionStatus('connecting');
      });

      newConnection.onreconnected((connectionId) => {
        console.log('SignalR reconnected:', connectionId);
        setConnectionStatus('connected');
        toast.dismiss();
      });

      // Notification events
      newConnection.on('ReceiveNotification', handleNotification);
      
      connectionRef.current = newConnection;
      await newConnection.start();
      
      console.log('Connected to SignalR notification hub');
      setConnection(newConnection);
      setConnectionStatus('connected');

      // Join user-specific notification group
      await newConnection.invoke('JoinUserGroup');
      
    } catch (error) {
      console.error('Failed to connect to SignalR hub:', error);
      setConnectionStatus('error');
      toast.error('실시간 알림 연결에 실패했습니다.');
    }
  };

  // Disconnect from hub
  const disconnectFromHub = async () => {
    if (connectionRef.current) {
      try {
        await connectionRef.current.stop();
      } catch (error) {
        console.error('Error disconnecting from SignalR hub:', error);
      }
      connectionRef.current = null;
    }
    setConnection(null);
    setConnectionStatus('disconnected');
  };

  // Initialize connection on mount
  useEffect(() => {
    const token = getAuthToken();
    if (token) {
      connectToHub();
    }

    return () => {
      disconnectFromHub();
    };
  }, []);

  // Listen for auth token changes
  useEffect(() => {
    const handleStorageChange = (e: StorageEvent) => {
      if (e.key === 'accessToken') {
        const newToken = getAuthToken();
        if (newToken && !connection) {
          connectToHub();
        } else if (!newToken && connection) {
          disconnectFromHub();
        }
      }
    };

    window.addEventListener('storage', handleStorageChange);
    return () => window.removeEventListener('storage', handleStorageChange);
  }, [connection]);

  return (
    <>
      {children}
      
      {/* Connection status indicator (for debugging) */}
      {process.env.NODE_ENV === 'development' && (
        <div className="fixed bottom-4 left-4 z-50">
          <div className={`
            px-3 py-2 rounded-lg text-xs font-medium shadow-lg
            ${connectionStatus === 'connected' ? 'bg-green-100 text-green-800' : ''}
            ${connectionStatus === 'connecting' ? 'bg-yellow-100 text-yellow-800' : ''}
            ${connectionStatus === 'disconnected' ? 'bg-gray-100 text-gray-800' : ''}
            ${connectionStatus === 'error' ? 'bg-red-100 text-red-800' : ''}
          `}>
            SignalR: {connectionStatus}
          </div>
        </div>
      )}
    </>
  );
};

export default NotificationProvider;