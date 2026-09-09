import { useRef, useState } from 'react';
import type { EndOfDayImportResult, NightlyBatchResult, PeriodCalculationOutcome } from '../api/types.ts';
import { useRunEndOfDayImport, useRunNightlyBatch, useRunSampleImport } from '../api/queries.ts';
import { formatMoney, MONTHS_TR } from '../lib/format.ts';
import { friendlyMessage } from '../lib/errors.ts';
import { PageHeader } from '../components/templates/PageHeader.tsx';
import { Modal } from '../components/molecules/Modal.tsx';
import { useToast } from '../components/molecules/Toast.tsx';
import { Field } from '../components/molecules/Field.tsx';
import { ImportErrorTable } from '../components/organisms/ImportErrorTable.tsx';
import { StatGrid } from '../components/organisms/StatGrid.tsx';
import { Button } from '../components/atoms/Button.tsx';
import { Badge } from '../components/atoms/Badge.tsx';
import { Card } from '../components/atoms/Card.tsx';
import { TextInput } from '../components/atoms/Inputs.tsx';

type SourceKey = 'PMS' | 'POS' | 'ERP';
const SOURCES: { key: SourceKey; label: string }[] = [
  { key: 'PMS', label: 'PMS · Fidelio' },
  { key: 'POS', label: 'POS · Flyby' },
  { key: 'ERP', label: 'ERP · Oracle JDE' },
];

const periodLabel = (o: { year: number; month: number }) => `${MONTHS_TR[o.month - 1]} ${o.year}`;

function FileSlot({
  label,
  file,
  onPick,
  onClear,
}: {
  label: string;
  file: File | null;
  onPick: (f: File) => void;
  onClear: () => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  return (
    <div className="flex flex-wrap items-center gap-3 rounded-md border border-slate-200 px-3 py-2">
      <span className="w-32 shrink-0 text-sm font-medium text-slate-700">{label}</span>
      <input
        ref={inputRef}
        type="file"
        accept=".csv,text/csv,text/plain"
        className="hidden"
        onChange={(e) => {
          const f = e.target.files?.[0];
          if (f) onPick(f);
          e.target.value = '';
        }}
      />
      <Button type="button" variant="ghost" className="!py-1 !text-xs" onClick={() => inputRef.current?.click()}>
        Dosya seç
      </Button>
      {file ? (
        <span className="flex items-center gap-2 text-xs text-slate-500">
          <span className="font-mono">{file.name}</span>
          <span className="text-slate-400">({(file.size / 1024).toFixed(0)} KB)</span>
          <button type="button" className="text-red-500 hover:underline" onClick={onClear}>kaldır</button>
        </span>
      ) : (
        <span className="text-xs text-slate-400">seçilmedi</span>
      )}
    </div>
  );
}

function ResultView({ result }: { result: EndOfDayImportResult }) {
  return (
    <div className="space-y-4">
      <StatGrid
        stats={[
          { label: 'Okunan', value: result.rowsRead },
          { label: 'Eklenen', value: result.rowsInserted, tone: 'green' },
          { label: 'Mükerrer', value: result.rowsDuplicate, tone: result.rowsDuplicate ? 'amber' : 'default' },
          { label: 'Hatalı', value: result.rowsFailed, tone: result.rowsFailed ? 'red' : 'default' },
          { label: 'Mutabakat (atlanan)', value: result.rowsReconciled, hint: 'ERP→PMS çift sayım önlendi' },
        ]}
      />
      <div>
        <h2 className="mb-2 text-sm font-semibold text-slate-700">
          Karantinaya alınan satırlar ({result.errors.length})
        </h2>
        <ImportErrorTable rows={result.errors} />
      </div>
    </div>
  );
}

function CalculationRow({ o }: { o: PeriodCalculationOutcome }) {
  return (
    <tr className="border-t border-slate-100">
      <td className="px-3 py-1.5 font-medium text-slate-700">{periodLabel(o)}</td>
      <td className="px-3 py-1.5">
        {o.calculated
          ? <Badge tone="green">hesaplandı</Badge>
          : <Badge tone="amber">atlandı</Badge>}
      </td>
      <td className="px-3 py-1.5 text-right">{o.employeeCount}</td>
      <td className="px-3 py-1.5 text-right font-semibold">{formatMoney(o.totalCommission)}</td>
      <td className="px-3 py-1.5 text-right">{o.totalRefunds}</td>
      <td className="px-3 py-1.5 text-right">{o.totalUnmatched}</td>
      <td className="px-3 py-1.5 text-xs text-slate-500">{o.skippedReason ?? ''}</td>
    </tr>
  );
}

function NightlyResultView({ result }: { result: NightlyBatchResult }) {
  const ps = result.personnelSync;
  return (
    <div className="space-y-5 text-sm">
      <section>
        <p className="mb-2 font-semibold text-slate-700">1 · Personel senkronizasyonu</p>
        <div className="flex flex-wrap gap-2">
          <Badge tone="green">{ps.departmentsAdded} departman</Badge>
          <Badge tone="green">{ps.employeesAdded} personel</Badge>
          <Badge tone="green">{ps.usersAdded} kullanıcı</Badge>
          {ps.skipped.length > 0 && <Badge tone="slate">{ps.skipped.length} zaten kayıtlı</Badge>}
          {ps.errors.length > 0 && <Badge tone="red">{ps.errors.length} hata</Badge>}
        </div>
        {ps.credentials.length > 0 && (
          <p className="mt-2 text-xs text-slate-500">
            {ps.credentials.length} yeni hesabın geçici şifreleri <span className="font-mono">users/</span> klasörüne yazıldı.
          </p>
        )}
      </section>

      <section>
        <p className="mb-2 font-semibold text-slate-700">2 · İçe aktarma</p>
        {result.import
          ? <ResultView result={result.import} />
          : <p className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-amber-800">{result.importSkippedReason}</p>}
      </section>

      <section>
        <p className="mb-2 font-semibold text-slate-700">3 · Prim hesaplama & dönem özeti</p>
        {result.calculations.length === 0 ? (
          <p className="text-xs text-slate-500">Hesaplanacak dönem yok (içe aktarılan satır bulunamadı).</p>
        ) : (
          <table className="w-full border border-slate-200 text-left">
            <thead className="bg-slate-50 text-xs text-slate-500">
              <tr>
                <th className="px-3 py-1.5 font-medium">Dönem</th>
                <th className="px-3 py-1.5 font-medium">Durum</th>
                <th className="px-3 py-1.5 text-right font-medium">Personel</th>
                <th className="px-3 py-1.5 text-right font-medium">Toplam prim</th>
                <th className="px-3 py-1.5 text-right font-medium">İade</th>
                <th className="px-3 py-1.5 text-right font-medium">Kapsam dışı</th>
                <th className="px-3 py-1.5 font-medium">Not</th>
              </tr>
            </thead>
            <tbody>
              {result.calculations.map((o) => <CalculationRow key={`${o.year}-${o.month}`} o={o} />)}
            </tbody>
          </table>
        )}
      </section>
    </div>
  );
}

export function ImportPage() {
  const run = useRunEndOfDayImport();
  const sample = useRunSampleImport();
  const nightly = useRunNightlyBatch();
  const toast = useToast();

  const [businessDay, setBusinessDay] = useState(() => new Date().toISOString().slice(0, 10));
  const [files, setFiles] = useState<Record<SourceKey, File | null>>({ PMS: null, POS: null, ERP: null });
  const [nightlyResult, setNightlyResult] = useState<NightlyBatchResult | null>(null);

  const anyFile = Object.values(files).some(Boolean);
  const result = run.data ?? sample.data;
  const busy = run.isPending || sample.isPending || nightly.isPending;

  const submitUpload = () => {
    const form = new FormData();
    if (businessDay) form.append('businessDay', businessDay);
    if (files.PMS) form.append('pms', files.PMS);
    if (files.POS) form.append('pos', files.POS);
    if (files.ERP) form.append('erp', files.ERP);

    run.mutate(form, {
      onSuccess: (r) => {
        toast.success(`Batch #${r.batchId}: ${r.rowsInserted} eklendi, ${r.rowsFailed} hata`);
        setFiles({ PMS: null, POS: null, ERP: null });
      },
      onError: (e) => toast.error(friendlyMessage(e)),
    });
  };

  const runNightly = () =>
    nightly.mutate(undefined, {
      onSuccess: (r) => {
        setNightlyResult(r);
        const calc = r.calculations.filter((c) => c.calculated).length;
        const ins = r.import?.rowsInserted ?? 0;
        toast.success(`Gece işi tamam: ${ins} satır içe aktarıldı, ${calc} dönem hesaplandı`);
      },
      onError: (e) => toast.error(friendlyMessage(e)),
    });

  return (
    <div>
      <PageHeader
        title="Gün Sonu İçe Aktarma"
        description="PMS / POS / ERP gün sonu CSV'lerini buradan yükleyin. Mükerrer atlanır, hatalı satır karantinaya alınır, ERP↔PMS mutabakatı yapılır."
        actions={
          <div className="flex gap-2">
            <Button variant="ghost" loading={sample.isPending} disabled={busy} onClick={() =>
              sample.mutate(undefined, {
                onSuccess: (r) => toast.success(`Örnek veri: ${r.rowsInserted} eklendi, ${r.rowsFailed} hata`),
                onError: (e) => toast.error(friendlyMessage(e)),
              })
            }>
              Örnek CSV'lerle dene
            </Button>
            <Button loading={nightly.isPending} disabled={busy} onClick={runNightly}>
              Gece işini şimdi çalıştır
            </Button>
          </div>
        }
      />

      <Card className="mb-5 space-y-4 p-5">
        <p className="text-xs text-slate-500">
          <span className="font-medium text-slate-600">Gece işini şimdi çalıştır</span>: gece job'ının aynısını elle tetikler —
          <span className="font-mono"> data/personel.csv</span> senkronu + <span className="font-mono">data/</span> klasöründeki
          PMS/POS/ERP dosyalarını içe aktarma + içe aktarılan her dönemin primlerini yeniden hesaplama.
        </p>

        <div className="max-w-xs">
          <Field label="İş günü">
            <TextInput type="date" value={businessDay} onChange={(e) => setBusinessDay(e.target.value)} />
          </Field>
        </div>

        <div className="space-y-2">
          {SOURCES.map((s) => (
            <FileSlot
              key={s.key}
              label={s.label}
              file={files[s.key]}
              onPick={(f) => setFiles((cur) => ({ ...cur, [s.key]: f }))}
              onClear={() => setFiles((cur) => ({ ...cur, [s.key]: null }))}
            />
          ))}
        </div>

        <div className="flex items-center gap-3">
          <Button loading={run.isPending} disabled={!anyFile || busy} onClick={submitUpload}>
            Gün sonu içe aktar
          </Button>
          {!anyFile && <span className="text-xs text-slate-400">En az bir dosya seçin.</span>}
        </div>
      </Card>

      {result && <ResultView result={result} />}

      <Modal
        open={nightlyResult != null}
        title="Gece işi sonucu"
        onClose={() => setNightlyResult(null)}
        wide
        footer={<Button onClick={() => setNightlyResult(null)}>Kapat</Button>}
      >
        {nightlyResult && <NightlyResultView result={nightlyResult} />}
      </Modal>
    </div>
  );
}
