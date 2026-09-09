import { useEffect, useRef, useState } from 'react';
import { useAuth } from '../../auth/AuthProvider.tsx';
import { ChangePasswordModal } from './ChangePasswordModal.tsx';

function initials(name: string | undefined): string {
  if (!name) return '?';
  const parts = name.trim().split(/\s+/);
  const first = parts[0]?.[0] ?? '';
  const last = parts.length > 1 ? parts[parts.length - 1][0] : '';
  return (first + last).toUpperCase() || first.toUpperCase();
}

const ROLE_TR: Record<string, string> = { ADMIN: 'Yönetici', ACCOUNTING: 'Muhasebe', EMPLOYEE: 'Personel' };

export function UserMenu() {
  const { user, logout } = useAuth();
  const [open, setOpen] = useState(false);
  const [pwOpen, setPwOpen] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const onDoc = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false);
    document.addEventListener('mousedown', onDoc);
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('mousedown', onDoc);
      document.removeEventListener('keydown', onKey);
    };
  }, [open]);

  const roleLabel = ROLE_TR[user?.roleCode ?? ''] ?? user?.roleCode;

  return (
    <div className="relative" ref={ref}>
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        aria-haspopup="menu"
        aria-expanded={open}
        className="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm text-brand-50 transition hover:bg-white/10"
      >
        <span className="flex h-7 w-7 items-center justify-center rounded-full bg-white/20 text-xs font-semibold">
          {initials(user?.fullName)}
        </span>
        <span className="hidden text-left leading-tight sm:block">
          <span className="block font-medium">{user?.fullName}</span>
          <span className="block text-xs text-brand-200">{roleLabel}</span>
        </span>
        <svg width="14" height="14" viewBox="0 0 20 20" fill="currentColor" className={open ? 'rotate-180 transition' : 'transition'}>
          <path fillRule="evenodd" d="M5.23 7.21a.75.75 0 011.06.02L10 10.94l3.71-3.71a.75.75 0 111.06 1.06l-4.24 4.24a.75.75 0 01-1.06 0L5.21 8.29a.75.75 0 01.02-1.08z" clipRule="evenodd" />
        </svg>
      </button>

      {open && (
        <div
          role="menu"
          className="absolute right-0 z-40 mt-1 w-56 overflow-hidden rounded-lg border border-slate-200 bg-white text-sm text-slate-700 shadow-lg"
        >
          <div className="border-b border-slate-100 px-3 py-2">
            <p className="font-medium text-slate-800">{user?.fullName}</p>
            <p className="text-xs text-slate-400">
              {user?.username} · {roleLabel}
            </p>
          </div>
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setOpen(false);
              setPwOpen(true);
            }}
            className="block w-full px-3 py-2 text-left hover:bg-slate-50"
          >
            Şifre değiştir
          </button>
          <button
            type="button"
            role="menuitem"
            onClick={() => {
              setOpen(false);
              void logout();
            }}
            className="block w-full px-3 py-2 text-left text-red-600 hover:bg-red-50"
          >
            Çıkış yap
          </button>
        </div>
      )}

      <ChangePasswordModal open={pwOpen} onClose={() => setPwOpen(false)} />
    </div>
  );
}
