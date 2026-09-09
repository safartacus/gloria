import type { ImportErrorDto } from '../../api/types.ts';
import { Badge } from '../atoms/Badge.tsx';
import { DataTable } from './DataTable.tsx';
import type { Column } from './DataTable.tsx';

const columns: Column<ImportErrorDto>[] = [
  { key: 'sourceSystemCode', header: 'Kaynak', render: (e) => <Badge tone="slate">{e.sourceSystemCode}</Badge> },
  { key: 'rowNumber', header: 'Satır', align: 'right', render: (e) => e.rowNumber },
  { key: 'errorCode', header: 'Hata', render: (e) => <Badge tone="red">{e.errorCode}</Badge> },
  { key: 'errorMessage', header: 'Mesaj', render: (e) => <span className="text-xs text-slate-600">{e.errorMessage}</span> },
  {
    key: 'rawContent',
    header: 'Ham satır',
    render: (e) => (
      <code className="block max-w-md truncate text-xs text-slate-400" title={e.rawContent}>{e.rawContent}</code>
    ),
  },
];

/** Quarantined import rows — shared by the import screen and the daily job-log screen. */
export function ImportErrorTable({ rows }: { rows: ImportErrorDto[] }) {
  return (
    <DataTable
      columns={columns}
      rows={rows}
      getRowKey={(e) => `${e.sourceSystemCode}-${e.rowNumber}-${e.errorCode}`}
      empty="Hatalı satır yok"
    />
  );
}
