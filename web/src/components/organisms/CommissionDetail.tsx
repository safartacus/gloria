import type { CommissionResultDto } from '../../api/types.ts';
import { formatDateTime } from '../../lib/format.ts';
import { StatGrid } from './StatGrid.tsx';
import { CommissionTraceTable } from './CommissionTraceTable.tsx';
import { Money } from '../atoms/Display.tsx';
import { Badge } from '../atoms/Badge.tsx';

/** Full commission breakdown for one employee + period: totals + step-by-step trace. */
export function CommissionDetail({ result }: { result: CommissionResultDto }) {
  return (
    <div className="space-y-4">
      <StatGrid
        stats={[
          {
            label: 'Toplam prim',
            value: <Money value={result.totalCommission} />,
            tone: result.totalCommission < 0 ? 'red' : 'green',
          },
          { label: 'Satış', value: result.saleCount },
          { label: 'İade', value: result.refundCount, tone: result.refundCount ? 'amber' : 'default' },
          { label: 'Kapsam dışı', value: result.unmatchedCount, tone: result.unmatchedCount ? 'amber' : 'default' },
          {
            label: 'Durum',
            value: (
              <Badge tone={result.status === 'FINALIZED' ? 'green' : 'neutral'}>
                {result.status === 'FINALIZED' ? 'Kesin' : 'Taslak'}
              </Badge>
            ),
          },
        ]}
      />

      <div>
        <div className="mb-2 flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-700">Hesaplama adımları</h3>
          <span className="text-xs text-slate-400">
            {formatDateTime(result.calculatedAt)} · {result.calculatedByUsername}
          </span>
        </div>
        <CommissionTraceTable steps={result.steps} />
      </div>
    </div>
  );
}
