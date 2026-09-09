import type { ReactNode } from 'react';
import { cn } from '../../lib/cn.ts';

export interface Column<T> {
  key: string;
  header: string;
  render?: (row: T) => ReactNode;
  align?: 'left' | 'right';
  className?: string;
  /** Mark the header clickable for sorting (server-driven via onSortChange). */
  sortable?: boolean;
}

export interface SortState {
  key: string;
  dir: 'asc' | 'desc';
}

export interface PaginationState {
  page: number; // 1-based
  pageSize: number;
  totalCount: number;
  onPageChange: (page: number) => void;
  loading?: boolean;
}

interface DataTableProps<T> {
  columns: Column<T>[];
  rows: T[];
  getRowKey: (row: T) => string | number;
  rowClassName?: (row: T) => string | undefined;
  empty?: string;
  onRowClick?: (row: T) => void;
  /** Controlled sort — current state + a toggle handler per column key. */
  sort?: SortState | null;
  onSortChange?: (key: string) => void;
  /** Controlled, server-side pagination. Rows passed in are already the current page. */
  pagination?: PaginationState;
}

export function DataTable<T>({
  columns,
  rows,
  getRowKey,
  rowClassName,
  empty = 'Kayıt yok',
  onRowClick,
  sort,
  onSortChange,
  pagination,
}: DataTableProps<T>) {
  const p = pagination;
  const showPager = p != null && p.totalCount > p.pageSize;
  const totalPages = p ? Math.max(1, Math.ceil(p.totalCount / p.pageSize)) : 1;
  const from = p ? (p.page - 1) * p.pageSize + 1 : 0;
  const to = p ? Math.min(p.page * p.pageSize, p.totalCount) : 0;

  return (
    <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
      <div className="overflow-x-auto">
        <table className="min-w-full divide-y divide-slate-200">
          <thead className="bg-slate-50">
            <tr>
              {columns.map((c) => {
                const canSort = onSortChange != null && c.sortable;
                const active = sort?.key === c.key;
                return (
                  <th key={c.key} className={cn('th', c.align === 'right' && 'text-right')}>
                    {canSort ? (
                      <button
                        type="button"
                        onClick={() => onSortChange!(c.key)}
                        className={cn(
                          'inline-flex items-center gap-1 hover:text-slate-700',
                          c.align === 'right' && 'flex-row-reverse',
                        )}
                      >
                        {c.header}
                        <span className="text-[10px] text-slate-400">
                          {active ? (sort!.dir === 'asc' ? '▲' : '▼') : '↕'}
                        </span>
                      </button>
                    ) : (
                      c.header
                    )}
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody className={cn('divide-y divide-slate-100', p?.loading && 'opacity-60')}>
            {rows.length === 0 && (
              <tr>
                <td className="td py-10 text-center text-slate-400" colSpan={columns.length}>{empty}</td>
              </tr>
            )}
            {rows.map((row) => (
              <tr
                key={getRowKey(row)}
                onClick={onRowClick ? () => onRowClick(row) : undefined}
                className={cn('hover:bg-slate-50/70', onRowClick && 'cursor-pointer', rowClassName?.(row))}
              >
                {columns.map((c) => (
                  <td key={c.key} className={cn('td', c.align === 'right' && 'text-right tabular-nums', c.className)}>
                    {c.render ? c.render(row) : String((row as Record<string, unknown>)[c.key] ?? '')}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {showPager && (
        <div className="flex items-center justify-between border-t border-slate-200 px-3 py-2 text-sm text-slate-500">
          <span>{from}–{to} / {p!.totalCount}</span>
          <div className="flex items-center gap-1">
            <button
              type="button"
              className="btn-ghost !px-2 !py-1"
              disabled={p!.page <= 1 || p!.loading}
              onClick={() => p!.onPageChange(p!.page - 1)}
            >
              ‹ Önceki
            </button>
            <span className="px-2">{p!.page} / {totalPages}</span>
            <button
              type="button"
              className="btn-ghost !px-2 !py-1"
              disabled={p!.page >= totalPages || p!.loading}
              onClick={() => p!.onPageChange(p!.page + 1)}
            >
              Sonraki ›
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
