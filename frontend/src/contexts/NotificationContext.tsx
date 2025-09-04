import React, { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import * as signalR from '@microsoft/signalr';
import { api } from '../services/api';
import { toast } from 'react-hot-toast';

interface NotificationData {
  id: string;
  userId: string;
  title: string;
  message: string;
  type: string;
  priority: 'low' | 'medium' | 'high' | 'urgent';
  category: 'system' | 'social' | 'update' | 'security';
  data?: string;
  isRead: boolean;
  createdAt: string;
  readAt?: string;
  actionUrl?: string;
}

interface NotificationContextType {
  notifications: NotificationData[];
  unreadCount: number;
  connection: signalR.HubConnection | null;
  isConnected: boolean;
  markAsRead: (notificationId: string) => Promise<void>;
  markAllAsRead: () => Promise<void>;
  fetchNotifications: () => Promise<void>;
  addNotification: (notification: Omit<NotificationData, 'userId'>) => void;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export const useNotification = () => {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotification must be used within a NotificationProvider');
  }
  return context;
};

interface NotificationProviderProps {
  children: ReactNode;
}

export const NotificationProvider: React.FC<NotificationProviderProps> = ({ children }) => {
  const [notifications, setNotifications] = useState<NotificationData[]>([]);
  const [unreadCount, setUnreadCount] = useState(0);
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const setupSignalRConnection = async () => {
      const token = localStorage.getItem('accessToken');
      if (!token) return;

      try {
        // Parse token to get userId
        const payload = JSON.parse(atob(token.split('.')[1]));
        const userId = payload.sub || payload.userId;

        const newConnection = new signalR.HubConnectionBuilder()
          .withUrl('http://localhost:5004/notificationHub', {
            accessTokenFactory: () => token
          })
          .withAutomaticReconnect()
          .build();

        // Set up event handlers
        newConnection.on('ReceiveNotification', (notification: NotificationData) => {
          console.log('Received notification:', notification);
          
          // Add notification to list
          setNotifications(prev => [notification, ...prev.slice(0, 49)]);
          setUnreadCount(prev => prev + 1);
          
          // Show custom toast notification based on type
          switch (notification.type) {
            case 'ImageUploaded':
              const data = notification.data ? JSON.parse(notification.data) : {};
              toast.success(`📷 이미지 업로드 완료: ${data.FileName || '이미지'}`);
              break;
            case 'ShareRequest':
              toast(`📋 ${notification.title}`, { icon: '🔔' });
              break;
            case 'ShareApproved':
              toast.success(`✅ ${notification.title}`);
              break;
            default:
              toast(notification.title + (notification.message ? `: ${notification.message}` : ''), {
                icon: '🔔',
              });
          }
        });

        // Start connection
        await newConnection.start();
        setConnection(newConnection);
        setIsConnected(true);

        // Join user group
        await newConnection.invoke('JoinUserGroup', userId);
        
        console.log('SignalR Connected');

      } catch (error) {
        console.error('SignalR Connection Error:', error);
        setIsConnected(false);
      }
    };

    setupSignalRConnection();

    return () => {
      if (connection) {
        connection.stop();
      }
    };
  }, []);

  const fetchNotifications = async () => {
    try {
      const token = localStorage.getItem('accessToken');
      if (!token) return;

      const payload = JSON.parse(atob(token.split('.')[1]));
      const userId = payload.sub || payload.userId;

      const response = await api.get<NotificationData[]>(`/notifications/${userId}`);
      setNotifications(response.data);

      // Fetch unread count
      const countResponse = await api.get<{count: number}>(`/notifications/${userId}/unread-count`);
      setUnreadCount(countResponse.data.count);
    } catch (error) {
      console.error('Failed to fetch notifications:', error);
    }
  };

  const markAsRead = async (notificationId: string) => {
    try {
      await api.post(`/notifications/${notificationId}/mark-read`);
      
      setNotifications(prev => 
        prev.map(n => 
          n.id === notificationId 
            ? { ...n, isRead: true, readAt: new Date().toISOString() }
            : n
        )
      );
      
      setUnreadCount(prev => Math.max(0, prev - 1));
    } catch (error) {
      console.error('Failed to mark notification as read:', error);
    }
  };

  const markAllAsRead = async () => {
    try {
      const token = localStorage.getItem('accessToken');
      if (!token) return;

      const payload = JSON.parse(atob(token.split('.')[1]));
      const userId = payload.sub || payload.userId;

      await api.post(`/notifications/user/${userId}/mark-all-read`);
      
      setNotifications(prev => 
        prev.map(n => ({ ...n, isRead: true, readAt: new Date().toISOString() }))
      );
      
      setUnreadCount(0);
    } catch (error) {
      console.error('Failed to mark all notifications as read:', error);
    }
  };

  const addNotification = (notification: Omit<NotificationData, 'userId'>) => {
    const token = localStorage.getItem('accessToken');
    if (!token) return;

    const payload = JSON.parse(atob(token.split('.')[1]));
    const userId = payload.sub || payload.userId;

    const newNotification: NotificationData = {
      ...notification,
      userId,
      priority: notification.priority || 'medium',
      category: notification.category || 'system'
    };

    setNotifications(prev => [newNotification, ...prev]);
    if (!newNotification.isRead) {
      setUnreadCount(prev => prev + 1);
    }
  };

  // Fetch initial notifications when component mounts
  useEffect(() => {
    fetchNotifications();
  }, []);

  const value: NotificationContextType = {
    notifications,
    unreadCount,
    connection,
    isConnected,
    markAsRead,
    markAllAsRead,
    fetchNotifications,
    addNotification,
  };

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  );
};