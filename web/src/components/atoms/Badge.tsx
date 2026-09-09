import type { ReactNode } from 'react';
import { cn } from '../../lib/cn.ts';

type Tone = 'neutral' | 'green' | 'amber' | 'red' | 'blue' | 'slate';

const toneClass: Record<Tone, string> = {
  neutral: 'bg-slate-100 text-slate-700',
  green: 'bg-emerald-100 text-emerald-800',
  amber: 'bg-amber-100 text-amber-800',
  red: 'bg-red-100 text-red-800',
  blue: 'bg-blue-100 text-blue-800',
  slate: 'bg-slate-200 text-slate-800',
};

export function Badge({ tone = 'neutral', children }: { tone?: Tone; children: ReactNode }) {
  return (
    <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium', toneClass[tone])}>
      {children}
    </span>
  );
}
