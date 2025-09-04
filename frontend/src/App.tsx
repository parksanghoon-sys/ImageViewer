import React, { useState, useEffect } from 'react';
import { Toaster } from 'react-hot-toast';
import { NotificationProvider } from './contexts/NotificationContext';
import NotificationProviderComponent from './components/NotificationProvider';
import SimpleLogin from './components/SimpleLogin';
import SimpleDashboard from './components/SimpleDashboard';
import ErrorBoundary from './components/ErrorBoundary';
import LoadingSpinner from './components/LoadingSpinner';
import './styles/notifications.css';
import './styles/responsive.css';

interface User {
  id: string;
  email: string;
  username: string;
  role: number;
  isActive: boolean;
  createdAt: string;
}

function App() {
  const [user, setUser] = useState<User | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    // Check if user is already logged in
    const savedUser = localStorage.getItem('user');
    const accessToken = localStorage.getItem('accessToken');
    
    if (savedUser && accessToken) {
      try {
        setUser(JSON.parse(savedUser));
      } catch (error) {
        // Clear invalid data
        localStorage.removeItem('user');
        localStorage.removeItem('accessToken');
        localStorage.removeItem('refreshToken');
      }
    }
    
    setLoading(false);
  }, []);

  const handleLoginSuccess = (userData: User) => {
    setUser(userData);
  };

  const handleLogout = () => {
    setUser(null);
  };

  if (loading) {
    return <LoadingSpinner fullScreen text="앱을 로딩 중입니다..." />;
  }

  return (
    <ErrorBoundary>
      <div className="App">
        <Toaster
          position="top-right"
          reverseOrder={false}
          gutter={8}
          containerClassName=""
          containerStyle={{}}
          toastOptions={{
            className: '',
            duration: 4000,
            style: {
              background: '#363636',
              color: '#fff',
            },
            success: {
              duration: 3000,
              iconTheme: {
                primary: '#4ade80',
                secondary: '#fff',
              },
            },
            error: {
              duration: 4000,
              iconTheme: {
                primary: '#ef4444',
                secondary: '#fff',
              },
            },
          }}
        />
        
        {user ? (
          <ErrorBoundary>
            <NotificationProvider>
              <NotificationProviderComponent>
                <SimpleDashboard user={user} onLogout={handleLogout} />
              </NotificationProviderComponent>
            </NotificationProvider>
          </ErrorBoundary>
        ) : (
          <ErrorBoundary>
            <SimpleLogin onLoginSuccess={handleLoginSuccess} />
          </ErrorBoundary>
        )}
      </div>
    </ErrorBoundary>
  );
}

export default App;
