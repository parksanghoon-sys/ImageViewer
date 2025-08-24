import React, { useState, useEffect } from 'react';
import SimpleLogin from './components/SimpleLogin';
import SimpleDashboard from './components/SimpleDashboard';

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
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-lg">로딩 중...</div>
      </div>
    );
  }

  return (
    <div className="App">
      {user ? (
        <SimpleDashboard user={user} onLogout={handleLogout} />
      ) : (
        <SimpleLogin onLoginSuccess={handleLoginSuccess} />
      )}
    </div>
  );
}

export default App;
