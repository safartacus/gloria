import { NavLink } from 'react-router-dom';
import { cn } from '../../lib/cn.ts';
import { useAuth } from '../../auth/AuthProvider.tsx';
import { UserMenu } from './UserMenu.tsx';

const NAV = [
  { to: '/commission', label: 'Primim', roles: ['EMPLOYEE'] },
  { to: '/rules', label: 'Prim Kuralları', roles: ['ADMIN', 'ACCOUNTING'] },
  { to: '/periods', label: 'Dönemler', roles: ['ADMIN', 'ACCOUNTING'] },
  { to: '/users', label: 'Kullanıcılar', roles: ['ADMIN'] },
  { to: '/import', label: 'İçe Aktarma', roles: ['ADMIN'] },
  { to: '/import-log', label: 'İçe Aktarma Günlüğü', roles: ['ADMIN'] },
];

export function AppHeader() {
  const { user } = useAuth();
  const role = user?.roleCode ?? '';
  const items = NAV.filter((n) => n.roles.includes(role));

  return (
    <header className="bg-brand-700 text-white">
      <div className="mx-auto flex max-w-6xl flex-wrap items-center gap-x-6 gap-y-2 px-4 py-2.5">
        <span className="text-lg font-semibold tracking-tight">Gloria · Prim</span>
        <nav className="flex flex-1 flex-wrap gap-1">
          {items.map((n) => (
            <NavLink
              key={n.to}
              to={n.to}
              className={({ isActive }) =>
                cn(
                  'rounded-md px-3 py-1.5 text-sm font-medium transition',
                  isActive ? 'bg-white/15 text-white' : 'text-brand-100 hover:bg-white/10',
                )
              }
            >
              {n.label}
            </NavLink>
          ))}
        </nav>
        <UserMenu />
      </div>
    </header>
  );
}
