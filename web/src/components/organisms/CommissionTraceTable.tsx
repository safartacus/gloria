import type { CalculationStepDto } from '../../api/types.ts';
import { Money, Percent } from '../atoms/Display.tsx';
import { Badge } from '../atoms/Badge.tsx';
import { DataTable } from './DataTable.tsx';
import type { Column } from './DataTable.tsx';

const columns: Column<CalculationStepDto>[] = [
  { key: 'order', header: '#', className: 'text-slate-400 w-10' },
  {
    key: 'productCode',
    header: 'Ürün / Kural',
    render: (s) => (
      <div>
        <div className="font-medium text-slate-800">{s.productCode}</div>
        <div className="text-xs text-slate-400">
          kural #{s.commissionRuleId} v{s.commissionRuleVersion} · {s.ruleTypeCode}
        </div>
      </div>
    ),
  },
  { key: 'base', header: 'Matrah', align: 'right', render: (s) => <Money value={s.base} /> },
  {
    key: 'applied',
    header: 'Oran / Tutar',
    align: 'right',
    render: (s) => (s.appliedRate != null ? <Percent value={s.appliedRate} /> : <Money value={s.appliedAmount ?? 0} />),
  },
  { key: 'stepAmount', header: 'Adım', align: 'right', render: (s) => <Money value={s.stepAmount} /> },
  { key: 'runningTotal', header: 'Ara toplam', align: 'right', render: (s) => <Money value={s.runningTotal} /> },
  {
    key: 'explanation',
    header: 'Açıklama',
    render: (s) => (
      <span className="text-xs text-slate-500">
        {s.isRefund && <Badge tone="red">iade</Badge>} {s.explanation}
      </span>
    ),
  },
];

export function CommissionTraceTable({ steps }: { steps: CalculationStepDto[] }) {
  return (
    <DataTable
      columns={columns}
      rows={steps}
      getRowKey={(s) => s.order}
      rowClassName={(s) => (s.isRefund ? 'bg-red-50/40' : undefined)}
      empty="Bu dönemde eşleşen satış yok"
    />
  );
}
