import { useState } from 'react';
import { Link } from 'react-router-dom';
import { ApiError } from '../api/client.ts';
import { useMyCommission } from '../api/queries.ts';
import { useAuth } from '../auth/AuthProvider.tsx';
import { MONTHS_TR, previousMonth } from '../lib/format.ts';
import { PageHeader } from '../components/templates/PageHeader.tsx';
import { MonthPicker } from '../components/molecules/MonthPicker.tsx';
import { QueryBoundary } from '../components/molecules/QueryBoundary.tsx';
import { StatGrid } from '../components/organisms/StatGrid.tsx';
import { CommissionTraceTable } from '../components/organisms/CommissionTraceTable.tsx';
import { Money } from '../components/atoms/Display.tsx';
import { Badge } from '../components/atoms/Badge.tsx';
import { Card } from '../components/atoms/Card.tsx';

export function MyCommissionPage() {
  const initial = previousMonth();
  const [year, setYear] = useState(initial.year);
  const [month, setMonth] = useState(initial.month);

  const { user } = useAuth();
  const isEmployee = user?.employeeId != null;
  const query = useMyCommission(year, month, isEmployee);

  const notCalculated = query.isError && query.error instanceof ApiError && query.error.status === 404;

  return (
    <div>
      <PageHeader
        title="Primim"
        description="Kendi aylık primim ve hesaplama adımları"
        actions={<MonthPicker year={year} month={month} onChange={(y, m) => { setYear(y); setMonth(m); }} />}
      />

      {!isEmployee ? (
        <Card className="px-6 py-12 text-center text-sm text-slate-600">
          <p className="font-medium text-slate-800">Bu ekran yalnızca personele özeldir.</p>
          <p className="mt-1">
            <span className="font-mono">{user?.username}</span> ({user?.roleCode}) bir personele bağlı değil.{' '}
            <Link to="/periods" className="font-medium text-brand-700 underline">Dönem Özeti</Link>'ne geçin.
          </p>
        </Card>
      ) : notCalculated ? (
        <Card className="px-6 py-16 text-center text-sm text-slate-500">
          {MONTHS_TR[month - 1]} {year} için primin henüz hesaplanmadı.
        </Card>
      ) : (
        <QueryBoundary query={query}>
          {(result) => (
            <div className="space-y-5">
              <StatGrid
                stats={[
                  { label: 'Toplam prim', value: <Money value={result.totalCommission} />, tone: result.totalCommission < 0 ? 'red' : 'green' },
                  { label: 'Satış', value: result.saleCount },
                  { label: 'İade', value: result.refundCount, tone: result.refundCount ? 'amber' : 'default' },
                  { label: 'Kapsam dışı', value: result.unmatchedCount, tone: result.unmatchedCount ? 'amber' : 'default' },
                  {
                    label: 'Durum',
                    value: (
                      <span className="text-base">
                        <Badge tone={result.periodStatus === 'CLOSED' ? 'slate' : 'blue'}>
                          {result.periodStatus === 'CLOSED' ? 'Kapalı' : 'Açık'}
                        </Badge>{' '}
                        <Badge tone={result.status === 'FINALIZED' ? 'green' : 'neutral'}>
                          {result.status === 'FINALIZED' ? 'Kesin' : 'Taslak'}
                        </Badge>
                      </span>
                    ),
                  },
                ]}
              />
              <div>
                <h2 className="mb-2 text-sm font-semibold text-slate-700">Hesaplama adımları</h2>
                <CommissionTraceTable steps={result.steps} />
              </div>
            </div>
          )}
        </QueryBoundary>
      )}
    </div>
  );
}
