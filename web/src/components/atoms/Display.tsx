import { cn } from '../../lib/cn.ts';
import { formatMoney, formatPercent } from '../../lib/format.ts';

export function Money({ value, currency, className }: { value: number; currency?: string; className?: string }) {
  return (
    <span className={cn('tabular-nums', value < 0 && 'text-red-600', className)}>
      {formatMoney(value, currency)}
    </span>
  );
}

export function Percent({ value, className }: { value: number | null | undefined; className?: string }) {
  return <span className={cn('tabular-nums', className)}>{formatPercent(value)}</span>;
}
