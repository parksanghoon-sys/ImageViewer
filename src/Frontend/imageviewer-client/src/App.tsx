import React, { useEffect } from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { Provider } from 'react-redux';
import { store, useAppDispatch, useAppSelector, selectIsAuthenticated, selectIsAuthInitialized } from './store';
import { initializeAuthAsync } from './store/slices/authSlice';

// 컴포넌트 임포트
import LoginForm from './components/auth/LoginForm';
import RegisterForm from './components/auth/RegisterForm';
import ImageUpload from './components/images/ImageUpload';

// 인증이 필요한 라우트를 보호하는 컴포넌트
const ProtectedRoute: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const isInitialized = useAppSelector(selectIsAuthInitialized);

  if (!isInitialized) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="spinner"></div>
      </div>
    );
  }

  return isAuthenticated ? <>{children}</> : <Navigate to="/login" replace />;
};

// 이미 로그인된 사용자를 위한 리다이렉트 컴포넌트
const AuthRedirect: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const isInitialized = useAppSelector(selectIsAuthInitialized);

  if (!isInitialized) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="spinner"></div>
      </div>
    );
  }

  return isAuthenticated ? <Navigate to="/dashboard" replace /> : <>{children}</>;
};

// 대시보드 컴포넌트 (임시)
const Dashboard: React.FC = () => {
  const user = useAppSelector(state => state.auth.user);

  return (
    <div className="min-h-screen bg-gray-50">
      <nav className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between h-16">
            <div className="flex items-center">
              <h1 className="text-xl font-semibold text-gray-900">ImageViewer</h1>
            </div>
            <div className="flex items-center space-x-4">
              <span className="text-sm text-gray-700">
                안녕하세요, {user?.username}님
              </span>
              <button
                onClick={() => {
                  store.dispatch({ type: 'auth/logout' });
                }}
                className="text-sm text-gray-500 hover:text-gray-700"
              >
                로그아웃
              </button>
            </div>
          </div>
        </div>
      </nav>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="border-4 border-dashed border-gray-200 rounded-lg">
            <ImageUpload />
          </div>
        </div>
      </main>
    </div>
  );
};

// 앱 초기화 컴포넌트
const AppInitializer: React.FC = () => {
  const dispatch = useAppDispatch();

  useEffect(() => {
    dispatch(initializeAuthAsync());
  }, [dispatch]);

  return (
    <Router>
      <Routes>
        {/* 퍼블릭 라우트 */}
        <Route
          path="/login"
          element={
            <AuthRedirect>
              <LoginForm />
            </AuthRedirect>
          }
        />
        <Route
          path="/register"
          element={
            <AuthRedirect>
              <RegisterForm />
            </AuthRedirect>
          }
        />

        {/* 보호된 라우트 */}
        <Route
          path="/dashboard"
          element={
            <ProtectedRoute>
              <Dashboard />
            </ProtectedRoute>
          }
        />

        {/* 기본 리다이렉트 */}
        <Route path="/" element={<Navigate to="/dashboard" replace />} />

        {/* 404 페이지 */}
        <Route
          path="*"
          element={
            <div className="min-h-screen flex items-center justify-center">
              <div className="text-center">
                <h1 className="text-4xl font-bold text-gray-900">404</h1>
                <p className="text-gray-600 mt-2">페이지를 찾을 수 없습니다.</p>
              </div>
            </div>
          }
        />
      </Routes>
    </Router>
  );
};

// 메인 App 컴포넌트
const App: React.FC = () => {
  return (
    <Provider store={store}>
      <AppInitializer />
    </Provider>
  );
};

export default App;
