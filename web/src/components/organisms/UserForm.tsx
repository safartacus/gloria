import { useMemo, useState } from 'react';
import type { CreatedUserResult } from '../../api/types.ts';
import { friendlyMessage } from '../../lib/errors.ts';
import { useCreateUser, useEmployees, useRoles } from '../../api/queries.ts';
import { useToast } from '../molecules/Toast.tsx';
import { Field } from '../molecules/Field.tsx';
import { Select, TextInput } from '../atoms/Inputs.tsx';
import { Button } from '../atoms/Button.tsx';

export function UserForm({ onDone }: { onDone: () => void }) {
  const roles = useRoles();
  const employees = useEmployees();
  const createUser = useCreateUser();
  const toast = useToast();

  const [username, setUsername] = useState('');
  const [roleCode, setRoleCode] = useState('EMPLOYEE');
  const [employeeNo, setEmployeeNo] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [created, setCreated] = useState<CreatedUserResult | null>(null);

  const isEmployeeRole = roleCode === 'EMPLOYEE';
  const employeeOptions = useMemo(() => employees.data ?? [], [employees.data]);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    try {
      const result = await createUser.mutateAsync({
        username: username.trim(),
        roleCode,
        employeeNo: isEmployeeRole ? employeeNo || null : null,
      });
      setCreated(result);
      toast.success(`${result.username} oluşturuldu`);
    } catch (err) {
      setError(friendlyMessage(err));
    }
  };

  if (created) {
    return (
      <div className="space-y-4">
        <div className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm">
          <p className="font-semibold text-emerald-800">Kullanıcı oluşturuldu</p>
          <dl className="mt-2 grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 text-emerald-900">
            <dt className="text-emerald-700">Kullanıcı adı</dt><dd className="font-mono">{created.username}</dd>
            <dt className="text-emerald-700">İsim</dt><dd>{created.fullName}</dd>
            <dt className="text-emerald-700">Rol</dt><dd>{created.roleCode}</dd>
            <dt className="text-emerald-700">Geçici şifre</dt>
            <dd className="font-mono text-base font-semibold tracking-wide">{created.password}</dd>
          </dl>
          <p className="mt-2 text-xs text-emerald-700">
            Bu şifre yalnızca şimdi gösteriliyor. Kullanıcı ilk girişte değiştirmelidir.
            {created.credentialFilePath && <> Dosya: <span className="font-mono">{created.credentialFilePath}</span></>}
          </p>
        </div>
        <div className="flex justify-end">
          <Button onClick={onDone}>Kapat</Button>
        </div>
      </div>
    );
  }

  return (
    <form className="space-y-4" onSubmit={submit}>
      <Field label="Kullanıcı adı" hint={isEmployeeRole ? 'Genelde personel no ile aynı (ör. P1014)' : undefined}>
        <TextInput value={username} onChange={(e) => setUsername(e.target.value)} placeholder="P1014 / mehmet.admin" />
      </Field>

      <Field label="Rol">
        <Select value={roleCode} onChange={(e) => setRoleCode(e.target.value)}>
          {(roles.data ?? []).map((r) => (
            <option key={r.code} value={r.code}>{r.name} ({r.code})</option>
          ))}
        </Select>
      </Field>

      {isEmployeeRole && (
        <Field label="Personel" error={createUser.isError ? undefined : undefined}>
          <Select value={employeeNo} onChange={(e) => setEmployeeNo(e.target.value)}>
            <option value="">— personel seçin —</option>
            {employeeOptions.map((emp) => (
              <option key={emp.employeeNo} value={emp.employeeNo}>{emp.employeeNo} — {emp.fullName}</option>
            ))}
          </Select>
        </Field>
      )}

      {error && <p className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onDone}>Vazgeç</Button>
        <Button type="submit" loading={createUser.isPending} disabled={!username || (isEmployeeRole && !employeeNo)}>
          Oluştur
        </Button>
      </div>
    </form>
  );
}
