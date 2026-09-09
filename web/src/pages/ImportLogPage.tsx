import { useState } from 'react';
import type { ImportBatchLogDto } from '../api/types.ts';
import { useImportBatchesByDate } from '../api/queries.ts';
import { formatDateTime } from '../lib/format.ts';
import { PageHeader } from '../components/templates/PageHeader.tsx';
import { QueryBoundary } from '../components/molecules/QueryBoundary.tsx';
import { ImportErrorTable } from '../components/organisms/ImportErrorTable.tsx';
import { StatGrid } from '../components/organisms/StatGrid.tsx';
import { Badge } from '../components/atoms/Badge.tsx';
import { Card } from '../components/atoms/Card.tsx';
import { Field } from '../components/molecules/Field.tsx';
import { TextInput } from '../components/atoms/Inputs.tsx';

const today = () => {
  const d = new Date();
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
};

const statusTone = (s: string) =>
  s === 'COMPLETED' ? 'green' : s === 'FAILED' ? 'red' : 'slate';

function BatchCard({ batch }: { batch: ImportBatchLogDto }) {
  return (
    <Card className="space-y-4 p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <div className="flex items-center gap-2">
            <span className="font-semibold text-slate-800">Batch #{batch.batchId}</span>
            <Badge tone={statusTone(batch.status)}>{batch.status}</Badge>
            {batch.rowsFailed > 0 && <Badge tone="red">{batch.rowsFailed} hatalı satır</Badge>}
          </div>
          <p className="mt-1 text-xs text-slate-500">
            {batch.description} · iş günü {batch.businessDay} · {batch.runByUsername}
          </p>
        </div>
        <div className="text-right text-xs text-slate-500">
          <div>başladı: {formatDateTime(batch.startedAt)}</div>
          <div>bitti: {batch.finishedAt ? formatDateTime(batch.finishedAt) : '—'}</div>
        </div>
      </div>

      <StatGrid
        stats={[
          { label: 'Okunan', value: batch.rowsRead },
          { label: 'Eklenen', value: batch.rowsInserted, tone: 'green' },
          { label: 'Mükerrer', value: batch.rowsDuplicate, tone: batch.rowsDuplicate ? 'amber' : 'default' },
          { label: 'Hatalı', value: batch.rowsFailed, tone: batch.rowsFailed ? 'red' : 'default' },
          { label: 'Mutabakat (atlanan)', value: batch.rowsReconciled, hint: 'ERP→PMS çift sayım önlendi' },
        ]}
      />

      <div>
        <h2 className="mb-2 text-sm font-semibold text-slate-700">
          Karantinaya alınan satırlar ({batch.errors.length})
        </h2>
        <ImportErrorTable rows={batch.errors} />
      </div>
    </Card>
  );
}

export function ImportLogPage() {
  const [date, setDate] = useState(today);
  const batches = useImportBatchesByDate(date);

  return (
    <div>
      <PageHeader
        title="İçe Aktarma Günlüğü"
        description="Seçili gün çalışan içe aktarma işleri (gece job'ı ve elle tetiklenenler) ve karantinaya alınan hatalı satırları."
        actions={
          <div className="w-44">
            <Field label="Gün">
              <TextInput type="date" value={date} max={today()} onChange={(e) => setDate(e.target.value)} />
            </Field>
          </div>
        }
      />

      <QueryBoundary
        query={batches}
        empty="Bu gün için içe aktarma çalışması yok."
        isEmpty={(b) => b.length === 0}
      >
        {(rows) => {
          const totalFailed = rows.reduce((n, b) => n + b.rowsFailed, 0);
          return (
            <div className="space-y-4">
              <div className="flex gap-2 text-sm">
                <Badge tone="slate">{rows.length} çalışma</Badge>
                <Badge tone={totalFailed ? 'red' : 'green'}>{totalFailed} toplam hatalı satır</Badge>
              </div>
              {rows.map((b) => <BatchCard key={b.batchId} batch={b} />)}
            </div>
          );
        }}
      </QueryBoundary>
    </div>
  );
}
