import type { HTMLAttributes } from 'react';
import { cn } from '../../lib/cn.ts';

export function Card({ className, ...rest }: HTMLAttributes<HTMLDivElement>) {
  return <div className={cn('card', className)} {...rest} />;
}
