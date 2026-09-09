import { useMemo, useState } from 'react';
import type { PersonnelSyncResult, UserDto } from '../api/types.ts';
import { useSetUserActive, useSyncPersonnel, useUsers } from '../api/queries.ts';
import { formatDate } from '../lib/format.ts';
import { friendlyMessage } from '../lib/errors.ts';
import { PageHeader } from '../components/templates/PageHeader.tsx';
import { QueryBoundary } from '../components/molecules/QueryBoundary.tsx';
import { Modal } from '../components/molecules/Modal.tsx';
import { useToast } from '../components/molecules/Toast.tsx';
import { DataTable } from '../components/organisms/DataTable.tsx';
import type { Column } from '../components/organisms/DataTable.tsx';
import { UserForm } from '../components/organisms/UserForm.tsx';
import { Button } from '../components/atoms/Button.tsx';
import { Badge } from '../components/atoms/Badge.tsx';

const roleTone = (code: string) => (code === 'ADMIN' ? 'red' : code === 'ACCOUNTING' ? 'blue' : 'neutral');

export function UsersPage() {
  const users = useUsers();
  const setActive = useSetUserActive();
  const syncPersonnel = useSyncPersonnel();
  const toast = useToast();
  const [open, setOpen] = useState(false);
  const [syncResult, setSyncResult] = useState<PersonnelSyncResult | null>(null);

  const runSync = () =>
    syncPersonnel.mutate(undefined, {
      onSuccess: (r) => {
        const changed = r.departmentsAdded + r.employeesAdded + r.usersAdded;
        if (changed === 0 && r.errors.length === 0) {
          toast.success('Tüm personel zaten kayıtlı — değişiklik yok');
          return;
        }
        setSyncResult(r);
        toast.success(`${r.employeesAdded} personel, ${r.usersAdded} kullanıcı eklendi`);
      },
      onError: (e) => toast.error(friendlyMessage(e)),
    });

  const columns = useMemo<Column<UserDto>[]>(
    () => [
      {
        key: 'user',
        header: 'Kullanıcı',
        render: (u) => (
          <div>
            <div className="font-medium text-slate-800">{u.fullName}</div>
            <div className="font-mono text-xs text-slate-400">{u.username}{u.employeeNo ? ` · ${u.employeeNo}` : ''}</div>
          </div>
        ),
      },
      { key: 'role', header: 'Rol', render: (u) => <Badge tone={roleTone(u.roleCode)}>{u.roleCode}</Badge> },
      {
        key: 'pwd',
        header: 'Şifre',
        render: (u) =>
          u.mustChangePassword
            ? <Badge tone="amber">değiştirilmedi</Badge>
            : <Badge tone="green">değiştirildi</Badge>,
      },
      { key: 'created', header: 'Oluşturma', render: (u) => <span className="text-xs text-slate-500">{formatDate(u.createdAt)}</span> },
      { key: 'active', header: 'Durum', render: (u) => <Badge tone={u.isActive ? 'green' : 'slate'}>{u.isActive ? 'Aktif' : 'Pasif'}</Badge> },
      {
        key: 'actions',
        header: '',
        align: 'right',
        render: (u) => (
          <Button
            variant="ghost"
            className="!py-1 !text-xs"
            disabled={setActive.isPending}
            onClick={() =>
              setActive.mutate(
                { username: u.username, active: !u.isActive },
                {
                  onSuccess: () => toast.success(u.isActive ? 'Pasifleştirildi' : 'Aktifleştirildi'),
                  onError: (e) => toast.error(friendlyMessage(e)),
                },
              )
            }
          >
            {u.isActive ? 'Pasifleştir' : 'Aktifleştir'}
          </Button>
        ),
      },
    ],
    [setActive, toast],
  );

  return (
    <div>
      <PageHeader
        title="Kullanıcılar"
        description="Hesapları görüntüle, yeni kullanıcı ekle veya personel listesini senkronize et. Yeni kullanıcıya rastgele şifre atanır ve ilk girişte değiştirmesi istenir."
        actions={
          <div className="flex gap-2">
            <Button variant="ghost" loading={syncPersonnel.isPending} onClick={runSync}>
              Personelleri senkronize et
            </Button>
            <Button onClick={() => setOpen(true)}>+ Yeni kullanıcı</Button>
          </div>
        }
      />

      <QueryBoundary query={users} empty="Kullanıcı yok" isEmpty={(u) => u.length === 0}>
        {(rows) => <DataTable columns={columns} rows={rows} getRowKey={(u) => u.username} />}
      </QueryBoundary>

      <Modal open={open} title="Yeni kullanıcı" onClose={() => setOpen(false)}>
        {open && <UserForm onDone={() => setOpen(false)} />}
      </Modal>

      <Modal
        open={syncResult != null}
        title="Personel senkronizasyonu"
        onClose={() => setSyncResult(null)}
        footer={<Button onClick={() => setSyncResult(null)}>Kapat</Button>}
      >
        {syncResult && <SyncResultView result={syncResult} />}
      </Modal>
    </div>
  );
}

function SyncResultView({ result }: { result: PersonnelSyncResult }) {
  return (
    <div className="space-y-4 text-sm">
      <div className="flex gap-2">
        <Badge tone="green">{result.departmentsAdded} departman</Badge>
        <Badge tone="green">{result.employeesAdded} personel</Badge>
        <Badge tone="green">{result.usersAdded} kullanıcı</Badge>
        {result.skipped.length > 0 && <Badge tone="slate">{result.skipped.length} zaten kayıtlı</Badge>}
      </div>

      {result.credentials.length > 0 && (
        <div>
          <p className="mb-1 font-semibold text-slate-700">Yeni hesaplar — geçici şifreler</p>
          <p className="mb-2 text-xs text-slate-500">
            Bu şifreler yalnızca şimdi gösteriliyor. Kredential dosyaları <span className="font-mono">users/</span> klasörüne de yazıldı.
          </p>
          <table className="w-full border border-slate-200 text-left">
            <thead className="bg-slate-50 text-xs text-slate-500">
              <tr>
                <th className="px-3 py-1.5 font-medium">Kullanıcı adı</th>
                <th className="px-3 py-1.5 font-medium">Geçici şifre</th>
              </tr>
            </thead>
            <tbody>
              {result.credentials.map((c) => (
                <tr key={c.username} className="border-t border-slate-100">
                  <td className="px-3 py-1.5 font-mono">{c.username}</td>
                  <td className="px-3 py-1.5 font-mono font-semibold tracking-wide">{c.password}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {result.errors.length > 0 && (
        <div className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-red-700">
          <p className="font-semibold">Atlanan satırlar</p>
          <ul className="mt-1 list-disc pl-5 text-xs">
            {result.errors.map((e, i) => <li key={i}>{e}</li>)}
          </ul>
        </div>
      )}
    </div>
  );
}
