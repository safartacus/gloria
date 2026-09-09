import type { ReactNode } from 'react';
import { cn } from '../../lib/cn.ts';
import { Card } from '../atoms/Card.tsx';

type Tone = 'default' | 'green' | 'amber' | 'red';

const valueTone: Record<Tone, string> = {
  default: 'text-slate-900',
  green: 'text-emerald-700',
  amber: 'text-amber-700',
  red: 'text-red-700',
};

export function StatCard({
  label,
  value,
  tone = 'default',
  hint,
}: {
  label: string;
  value: ReactNode;
  tone?: Tone;
  hint?: string;
}) {
  return (
    <Card className="p-4">
      <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">{label}</p>
      <p className={cn('mt-1 text-2xl font-semibold tabular-nums', valueTone[tone])}>{value}</p>
      {hint && <p className="mt-1 text-xs text-slate-400">{hint}</p>}
    </Card>
  );
}
