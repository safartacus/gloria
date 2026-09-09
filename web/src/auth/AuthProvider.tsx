import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { useQueryClient } from '@tanstack/react-query';
import { api } from '../api/client.ts';
import { authStorage } from './authStorage.ts';
import type { AuthUser, StoredAuth } from './authStorage.ts';

interface AuthResultDto {
  accessToken: string;
  refreshToken: string;
  expiresInSeconds: number;
  user: AuthUser;
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  changePassword: (currentPassword: string, newPassword: string) => Promise<void>;
}

const AuthContext = createContext<AuthContextValue | null>(null);

export function AuthProvider({ children }: { children: ReactNode }) {
  const qc = useQueryClient();
  const [auth, setAuth] = useState<StoredAuth | null>(() => authStorage.get());

  useEffect(() => authStorage.subscribe(() => setAuth(authStorage.get())), []);

  const login = useCallback(
    async (username: string, password: string) => {
      const data = await api.post<AuthResultDto>('/auth/login', { username, password });
      authStorage.set({ accessToken: data.accessToken, refreshToken: data.refreshToken, user: data.user });
      qc.clear();
    },
    [qc],
  );

  const changePassword = useCallback(
    async (currentPassword: string, newPassword: string) => {
      const data = await api.post<AuthResultDto>('/auth/change-password', { currentPassword, newPassword });
      authStorage.set({ accessToken: data.accessToken, refreshToken: data.refreshToken, user: data.user });
      qc.clear();
    },
    [qc],
  );

  const logout = useCallback(async () => {
    const current = authStorage.get();
    if (current) {
      try {
        await api.post('/auth/logout', { refreshToken: current.refreshToken });
      } catch {
        /* revoke best-effort */
      }
    }
    authStorage.clear();
    qc.clear();
  }, [qc]);

  const value = useMemo<AuthContextValue>(
    () => ({ user: auth?.user ?? null, isAuthenticated: auth != null, login, logout, changePassword }),
    [auth, login, logout, changePassword],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth must be used within AuthProvider');
  return ctx;
}
