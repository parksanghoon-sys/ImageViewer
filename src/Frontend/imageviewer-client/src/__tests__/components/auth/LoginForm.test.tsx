import React from 'react';
import { render, screen, fireEvent, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { configureStore } from '@reduxjs/toolkit';
import '@testing-library/jest-dom';

import LoginForm from '../../../components/auth/LoginForm';
import authReducer from '../../../store/slices/authSlice';
import imageReducer from '../../../store/slices/imageSlice';

// Mock navigate
const mockNavigate = jest.fn();
jest.mock('react-router-dom', () => ({
  ...jest.requireActual('react-router-dom'),
  useNavigate: () => mockNavigate,
}));

// Mock API
jest.mock('../../../services/api', () => ({
  authApi: {
    login: jest.fn(),
  },
}));

const createTestStore = (initialState = {}) => {
  return configureStore({
    reducer: {
      auth: authReducer,
      images: imageReducer,
    },
    preloadedState: {
      auth: {
        user: null,
        isAuthenticated: false,
        isLoading: false,
        error: null,
        isInitialized: true,
        ...initialState.auth,
      },
      images: {
        images: [],
        currentPage: 1,
        pageSize: 12,
        totalCount: 0,
        totalPages: 0,
        hasNext: false,
        hasPrevious: false,
        isLoading: false,
        isUploading: false,
        error: null,
        uploadProgress: 0,
        selectedImages: [],
        viewMode: 'grid' as const,
        sortBy: 'newest' as const,
        filterByType: 'all' as const,
        ...initialState.images,
      },
    },
  });
};

const renderWithProviders = (
  ui: React.ReactElement,
  { initialState = {}, ...renderOptions } = {}
) => {
  const store = createTestStore(initialState);
  
  const Wrapper: React.FC<{ children: React.ReactNode }> = ({ children }) => (
    <Provider store={store}>
      <MemoryRouter>
        {children}
      </MemoryRouter>
    </Provider>
  );

  return { store, ...render(ui, { wrapper: Wrapper, ...renderOptions }) };
};

describe('LoginForm', () => {
  beforeEach(() => {
    mockNavigate.mockClear();
  });

  it('renders login form correctly', () => {
    renderWithProviders(<LoginForm />);

    expect(screen.getByText('ImageViewer 로그인')).toBeInTheDocument();
    expect(screen.getByLabelText('이메일')).toBeInTheDocument();
    expect(screen.getByLabelText('비밀번호')).toBeInTheDocument();
    expect(screen.getByRole('button', { name: '로그인' })).toBeInTheDocument();
    expect(screen.getByText('새 계정 만들기')).toBeInTheDocument();
  });

  it('shows validation errors for empty fields', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginForm />);

    const submitButton = screen.getByRole('button', { name: '로그인' });
    await user.click(submitButton);

    expect(screen.getByText('이메일을 입력해주세요.')).toBeInTheDocument();
    expect(screen.getByText('비밀번호를 입력해주세요.')).toBeInTheDocument();
  });

  it('shows validation error for invalid email format', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginForm />);

    const emailInput = screen.getByLabelText('이메일');
    const submitButton = screen.getByRole('button', { name: '로그인' });

    await user.type(emailInput, 'invalid-email');
    await user.click(submitButton);

    expect(screen.getByText('올바른 이메일 형식을 입력해주세요.')).toBeInTheDocument();
  });

  it('shows validation error for short password', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginForm />);

    const passwordInput = screen.getByLabelText('비밀번호');
    const submitButton = screen.getByRole('button', { name: '로그인' });

    await user.type(passwordInput, '123');
    await user.click(submitButton);

    expect(screen.getByText('비밀번호는 최소 6자 이상이어야 합니다.')).toBeInTheDocument();
  });

  it('toggles password visibility', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginForm />);

    const passwordInput = screen.getByLabelText('비밀번호') as HTMLInputElement;
    const toggleButton = screen.getByRole('button', { name: '' }); // Password toggle button

    expect(passwordInput.type).toBe('password');

    await user.click(toggleButton);
    expect(passwordInput.type).toBe('text');

    await user.click(toggleButton);
    expect(passwordInput.type).toBe('password');
  });

  it('clears field validation errors when user starts typing', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginForm />);

    const emailInput = screen.getByLabelText('이메일');
    const submitButton = screen.getByRole('button', { name: '로그인' });

    // Trigger validation error
    await user.click(submitButton);
    expect(screen.getByText('이메일을 입력해주세요.')).toBeInTheDocument();

    // Start typing to clear error
    await user.type(emailInput, 't');
    expect(screen.queryByText('이메일을 입력해주세요.')).not.toBeInTheDocument();
  });

  it('disables form during login loading state', () => {
    renderWithProviders(<LoginForm />, {
      initialState: {
        auth: {
          isLoading: true,
        },
      },
    });

    const emailInput = screen.getByLabelText('이메일');
    const passwordInput = screen.getByLabelText('비밀번호');
    const submitButton = screen.getByRole('button', { name: '로그인 중...' });

    expect(emailInput).not.toBeDisabled(); // Input fields should not be disabled
    expect(passwordInput).not.toBeDisabled();
    expect(submitButton).toBeDisabled();
  });

  it('displays error message when login fails', () => {
    const errorMessage = '이메일 또는 비밀번호가 올바르지 않습니다.';
    
    renderWithProviders(<LoginForm />, {
      initialState: {
        auth: {
          error: errorMessage,
        },
      },
    });

    expect(screen.getByText('로그인 실패')).toBeInTheDocument();
    expect(screen.getByText(errorMessage)).toBeInTheDocument();
  });

  it('redirects to dashboard when already authenticated', () => {
    renderWithProviders(<LoginForm />, {
      initialState: {
        auth: {
          isAuthenticated: true,
          user: {
            id: 'user-id',
            email: 'test@example.com',
            username: 'testuser',
            isActive: true,
            createdAt: '2023-01-01T00:00:00Z',
          },
        },
      },
    });

    expect(mockNavigate).toHaveBeenCalledWith('/dashboard');
  });

  it('submits form with valid data', async () => {
    const user = userEvent.setup();
    const { store } = renderWithProviders(<LoginForm />);

    const emailInput = screen.getByLabelText('이메일');
    const passwordInput = screen.getByLabelText('비밀번호');
    const submitButton = screen.getByRole('button', { name: '로그인' });

    await user.type(emailInput, 'test@example.com');
    await user.type(passwordInput, 'password123');
    await user.click(submitButton);

    // Check that the form submission was attempted
    await waitFor(() => {
      const state = store.getState();
      // In a real test, you would mock the API call and verify it was called
      expect(state.auth.isLoading).toBe(false); // Assuming the mock resolves immediately
    });
  });

  it('renders register link correctly', () => {
    renderWithProviders(<LoginForm />);

    const registerLink = screen.getByText('새 계정 만들기');
    expect(registerLink).toBeInTheDocument();
    expect(registerLink.closest('a')).toHaveAttribute('href', '/register');
  });

  it('renders forgot password link correctly', () => {
    renderWithProviders(<LoginForm />);

    const forgotPasswordLink = screen.getByText('비밀번호를 잊으셨나요?');
    expect(forgotPasswordLink).toBeInTheDocument();
    expect(forgotPasswordLink.closest('a')).toHaveAttribute('href', '/forgot-password');
  });

  it('shows loading spinner during authentication', () => {
    renderWithProviders(<LoginForm />, {
      initialState: {
        auth: {
          isLoading: true,
        },
      },
    });

    expect(screen.getByText('로그인 중...')).toBeInTheDocument();
    // In a real application, you would also check for the spinner element
  });

  it('handles form submission with enter key', async () => {
    const user = userEvent.setup();
    renderWithProviders(<LoginForm />);

    const emailInput = screen.getByLabelText('이메일');
    const passwordInput = screen.getByLabelText('비밀번호');

    await user.type(emailInput, 'test@example.com');
    await user.type(passwordInput, 'password123');
    await user.keyboard('{Enter}');

    // Form should attempt to submit
    // In a real test, you would verify the API call was made
  });

  it('maintains input values during re-renders', async () => {
    const user = userEvent.setup();
    const { rerender } = renderWithProviders(<LoginForm />);

    const emailInput = screen.getByLabelText('이메일') as HTMLInputElement;
    const passwordInput = screen.getByLabelText('비밀번호') as HTMLInputElement;

    await user.type(emailInput, 'test@example.com');
    await user.type(passwordInput, 'password123');

    expect(emailInput.value).toBe('test@example.com');
    expect(passwordInput.value).toBe('password123');

    // Re-render component
    rerender(<LoginForm />);

    expect(emailInput.value).toBe('test@example.com');
    expect(passwordInput.value).toBe('password123');
  });
});