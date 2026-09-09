import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import type { CommissionListItemDto } from '../api/types.ts';
import {
  useCalculateCommissions,
  useClosePeriod,
  useEmployeeCommission,
  usePeriodCommissions,
  usePeriodCommissionSummary,
  usePeriods,
  useRules,
} from '../api/queries.ts';
import { MONTHS_TR, previousMonth } from '../lib/format.ts';
import { friendlyMessage } from '../lib/errors.ts';
import { PageHeader } from '../components/templates/PageHeader.tsx';
import { MonthPicker } from '../components/molecules/MonthPicker.tsx';
import { QueryBoundary } from '../components/molecules/QueryBoundary.tsx';
import { ConfirmButton } from '../components/molecules/ConfirmButton.tsx';
import { Modal } from '../components/molecules/Modal.tsx';
import { useToast } from '../components/molecules/Toast.tsx';
import { DataTable } from '../components/organisms/DataTable.tsx';
import type { Column, SortState } from '../components/organisms/DataTable.tsx';
import { StatGrid } from '../components/organisms/StatGrid.tsx';
import { CommissionDetail } from '../components/organisms/CommissionDetail.tsx';
import { Button } from '../components/atoms/Button.tsx';
import { Badge } from '../components/atoms/Badge.tsx';
import { Money } from '../components/atoms/Display.tsx';

const PAGE_SIZE = 10;

// column key === server sortBy value
const columns: Column<CommissionListItemDto>[] = [
  {
    key: 'employeeName',
    header: 'Personel',
    sortable: true,
    render: (r) => (
      <div>
        <div className="font-medium text-slate-800">{r.employeeName}</div>
        <div className="text-xs text-slate-400">{r.employeeNo}</div>
      </div>
    ),
  },
  { key: 'totalCommission', header: 'Toplam prim', align: 'right', sortable: true, render: (r) => <Money value={r.totalCommission} /> },
  { key: 'saleCount', header: 'Satış', align: 'right', sortable: true, render: (r) => r.saleCount },
  { key: 'refundCount', header: 'İade', align: 'right', sortable: true, render: (r) => r.refundCount },
  { key: 'unmatchedCount', header: 'Kapsam dışı', align: 'right', sortable: true, render: (r) => r.unmatchedCount },
  {
    key: 'status',
    header: 'Durum',
    sortable: true,
    render: (r) => (
      <Badge tone={r.status === 'FINALIZED' ? 'green' : 'neutral'}>{r.status === 'FINALIZED' ? 'Kesin' : 'Taslak'}</Badge>
    ),
  },
];

export function PeriodOverviewPage() {
  const initial = previousMonth();
  const [year, setYear] = useState(initial.year);
  const [month, setMonth] = useState(initial.month);
  const [page, setPage] = useState(1);
  const [sort, setSort] = useState<SortState>({ key: 'totalCommission', dir: 'desc' });
  const [selectedId, setSelectedId] = useState<number | null>(null);

  const periods = usePeriods();
  const rules = useRules();
  const commissions = usePeriodCommissions({ year, month, page, pageSize: PAGE_SIZE, sortBy: sort.key, sortDir: sort.dir });
  const summary = usePeriodCommissionSummary(year, month);

  const noActiveRules = rules.isSuccess && rules.data.filter((r) => r.isActive).length === 0;
  const detail = useEmployeeCommission(selectedId, year, month);
  const calculate = useCalculateCommissions();
  const close = useClosePeriod();
  const toast = useToast();

  const isClosed = useMemo(
    () => periods.data?.find((p) => p.year === year && p.month === month)?.status === 'CLOSED',
    [periods.data, year, month],
  );
  const closedBy = periods.data?.find((p) => p.year === year && p.month === month)?.closedByUsername;

  const changeMonth = (y: number, m: number) => {
    setYear(y);
    setMonth(m);
    setPage(1);
    setSelectedId(null);
  };

  const toggleSort = (key: string) => {
    setSort((cur) => (cur.key === key ? { key, dir: cur.dir === 'asc' ? 'desc' : 'asc' } : { key, dir: 'asc' }));
    setPage(1);
  };

  const s = summary.data;

  return (
    <div>
      <PageHeader
        title="Dönem Özeti"
        description="Sütun başlığına tıklayarak sırala (sunucu tarafı), satıra tıklayarak personelin detayını gör"
        actions={
          <div className="flex items-center gap-2">
            <MonthPicker year={year} month={month} onChange={changeMonth} />
            <Button
              loading={calculate.isPending}
              disabled={isClosed}
              onClick={() =>
                calculate.mutate(
                  { year, month },
                  {
                    onSuccess: (r) => toast.success(`${r.length} personel için hesaplandı`),
                    onError: (e) => toast.error(friendlyMessage(e)),
                  },
                )
              }
            >
              Hesapla
            </Button>
            <ConfirmButton
              label={isClosed ? 'Kapalı' : 'Dönemi kapat'}
              confirmLabel="Kapat"
              variant="danger"
              disabled={isClosed}
              loading={close.isPending}
              onConfirm={() =>
                close.mutate(
                  { year, month },
                  {
                    onSuccess: () => toast.success('Dönem kapatıldı'),
                    onError: (e) => toast.error(friendlyMessage(e)),
                  },
                )
              }
            />
          </div>
        }
      />

      {noActiveRules && (
        <div className="mb-4 rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          <strong>Aktif prim kuralı yok.</strong> Hesaplama çalışır ama tüm satışlar “kapsam dışı” kalır ve prim ₺0 olur.{' '}
          <Link to="/rules" className="font-medium text-brand-700 underline">Prim Kuralları</Link> ekranından kural ekleyin.
        </div>
      )}

      <div className="mb-4">
        <StatGrid
          stats={[
            { label: 'Toplam prim', value: <Money value={s?.totalCommission ?? 0} />, tone: 'green' },
            { label: 'Personel', value: s?.employeeCount ?? 0 },
            { label: 'Toplam iade', value: s?.totalRefunds ?? 0, tone: s?.totalRefunds ? 'amber' : 'default' },
            { label: 'Kapsam dışı', value: s?.totalUnmatched ?? 0, tone: s?.totalUnmatched ? 'amber' : 'default' },
            {
              label: 'Dönem',
              value: <Badge tone={isClosed ? 'slate' : 'blue'}>{isClosed ? 'Kapalı' : 'Açık'}</Badge>,
              hint: closedBy ? `kapatan: ${closedBy}` : undefined,
            },
          ]}
        />
      </div>

      <QueryBoundary query={commissions} empty="Bu dönem için hesaplama yok — 'Hesapla'ya bas" isEmpty={(d) => d.totalCount === 0}>
        {(paged) => (
          <DataTable
            columns={columns}
            rows={paged.items}
            getRowKey={(r) => r.employeeId}
            onRowClick={(r) => setSelectedId(r.employeeId)}
            sort={sort}
            onSortChange={toggleSort}
            pagination={{
              page: paged.page,
              pageSize: paged.pageSize,
              totalCount: paged.totalCount,
              onPageChange: setPage,
              loading: commissions.isFetching,
            }}
          />
        )}
      </QueryBoundary>

      <Modal
        open={selectedId != null}
        wide
        title={detail.data ? `${detail.data.employeeName} · ${MONTHS_TR[detail.data.month - 1]} ${detail.data.year}` : 'Detay'}
        onClose={() => setSelectedId(null)}
      >
        <QueryBoundary query={detail}>{(r) => <CommissionDetail result={r} />}</QueryBoundary>
      </Modal>
    </div>
  );
}
