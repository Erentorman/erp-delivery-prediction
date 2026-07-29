import { createContext, useContext, useState, useEffect, ReactNode } from 'react';
import { useNavigate } from 'react-router-dom';
import { authEmitter, AUTH_EVENTS } from '../api/authEmitter';

interface AuthContextType {
  token: string | null;
  login: (token: string) => void;
  logout: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [token, setToken] = useState<string | null>(localStorage.getItem('token'));
  const navigate = useNavigate();

  const login = (newToken: string) => {
    localStorage.setItem('token', newToken);
    setToken(newToken);
  };

  const logout = () => {
    localStorage.removeItem('token');
    setToken(null);
  };

  // Listen for 401 Unauthorized events fired by the Axios interceptor.
  // The interceptor cannot use useNavigate directly (lives outside React tree),
  // so it emits an event here; we handle the redirect via React Router.
  useEffect(() => {
    const handleUnauthorized = () => {
      setToken(null);
      navigate('/login', { replace: true });
    };

    authEmitter.addEventListener(AUTH_EVENTS.UNAUTHORIZED, handleUnauthorized);
    return () => {
      authEmitter.removeEventListener(AUTH_EVENTS.UNAUTHORIZED, handleUnauthorized);
    };
  }, [navigate]);

  return (
    <AuthContext.Provider value={{ token, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
}

