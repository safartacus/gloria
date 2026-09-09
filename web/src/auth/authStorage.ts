export interface AuthUser {
  username: string;
  fullName: string;
  roleCode: 'ADMIN' | 'ACCOUNTING' | 'EMPLOYEE' | string;
  employeeId: number | null;
  mustChangePassword: boolean;
}

export interface StoredAuth {
  accessToken: string;
  refreshToken: string;
  user: AuthUser;
}

const KEY = 'gloria.auth';
const EVENT = 'gloria:auth-changed';

function emit() {
  window.dispatchEvent(new Event(EVENT));
}

export const authStorage = {
  get(): StoredAuth | null {
    try {
      const raw = window.localStorage.getItem(KEY);
      return raw ? (JSON.parse(raw) as StoredAuth) : null;
    } catch {
      return null;
    }
  },
  set(auth: StoredAuth) {
    window.localStorage.setItem(KEY, JSON.stringify(auth));
    emit();
  },
  clear() {
    window.localStorage.removeItem(KEY);
    emit();
  },
  subscribe(handler: () => void): () => void {
    window.addEventListener(EVENT, handler);
    window.addEventListener('storage', handler); // cross-tab
    return () => {
      window.removeEventListener(EVENT, handler);
      window.removeEventListener('storage', handler);
    };
  },
};
