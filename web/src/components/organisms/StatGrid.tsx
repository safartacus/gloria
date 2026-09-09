import { StatCard } from '../molecules/StatCard.tsx';
import type { ReactNode } from 'react';

export interface Stat {
  label: string;
  value: ReactNode;
  tone?: 'default' | 'green' | 'amber' | 'red';
  hint?: string;
}

export function StatGrid({ stats }: { stats: Stat[] }) {
  return (
    <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-5">
      {stats.map((s) => (
        <StatCard key={s.label} label={s.label} value={s.value} tone={s.tone} hint={s.hint} />
      ))}
    </div>
  );
}
