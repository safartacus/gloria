import type { ReactNode } from 'react';
import { useAuth } from '../../auth/AuthProvider.tsx';
import { AppHeader } from '../organisms/AppHeader.tsx';
import { ChangePasswordButton } from '../organisms/ChangePasswordButton.tsx';

export function PageLayout({ children }: { children: ReactNode }) {
  const { user } = useAuth();

  return (
    <div className="min-h-full">
      <AppHeader />

      {user?.mustChangePassword && (
        <div className="border-b border-amber-200 bg-amber-50">
          <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-2 px-4 py-2 text-sm text-amber-800">
            <span>
              <strong>Şifreniz sistem tarafından atandı.</strong> Güvenliğiniz için lütfen yeni bir şifre belirleyin.
            </span>
            <ChangePasswordButton variant="primary" label="Şimdi değiştir" />
          </div>
        </div>
      )}

      <main className="mx-auto max-w-6xl px-4 py-6">{children}</main>
    </div>
  );
}
