import type { ReactNode } from 'react';
import type { UseQueryResult } from '@tanstack/react-query';
import { Spinner } from '../atoms/Spinner.tsx';
import { ErrorState } from './ErrorState.tsx';

interface QueryBoundaryProps<T> {
  query: UseQueryResult<T>;
  children: (data: T) => ReactNode;
  empty?: string;
  isEmpty?: (data: T) => boolean;
}

export function QueryBoundary<T>({ query, children, empty, isEmpty }: QueryBoundaryProps<T>) {
  if (query.isPending) {
    return (
      <div className="flex items-center justify-center gap-2 py-16 text-sm text-slate-500">
        <Spinner /> Yükleniyor…
      </div>
    );
  }

  if (query.isError) {
    return (
      <div className="py-10">
        <ErrorState error={query.error} onRetry={() => query.refetch()} />
      </div>
    );
  }

  const data = query.data as T;
  if (empty && isEmpty?.(data)) {
    return <div className="py-16 text-center text-sm text-slate-400">{empty}</div>;
  }

  return <>{children(data)}</>;
}
