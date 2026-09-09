import { useState } from 'react';
import { friendlyMessage } from '../../lib/errors.ts';
import { useAuth } from '../../auth/AuthProvider.tsx';
import { Button } from '../atoms/Button.tsx';
import { TextInput } from '../atoms/Inputs.tsx';
import { Field } from '../molecules/Field.tsx';
import { Modal } from '../molecules/Modal.tsx';
import { useToast } from '../molecules/Toast.tsx';

export function ChangePasswordModal({ open, onClose }: { open: boolean; onClose: () => void }) {
  const { changePassword } = useAuth();
  const toast = useToast();
  const [current, setCurrent] = useState('');
  const [next, setNext] = useState('');
  const [confirm, setConfirm] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const close = () => {
    setCurrent('');
    setNext('');
    setConfirm('');
    setError(null);
    onClose();
  };

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    if (next.length < 8) return setError('Yeni şifre en az 8 karakter olmalı.');
    if (next !== confirm) return setError('Yeni şifreler eşleşmiyor.');
    setBusy(true);
    try {
      await changePassword(current, next);
      toast.success('Şifreniz değiştirildi');
      close();
    } catch (err) {
      setError(friendlyMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <Modal open={open} title="Şifre değiştir" onClose={close}>
      <form className="space-y-4" onSubmit={submit}>
        <Field label="Mevcut şifre">
          <TextInput type="password" autoComplete="current-password" value={current} onChange={(e) => setCurrent(e.target.value)} />
        </Field>
        <Field label="Yeni şifre" hint="En az 8 karakter">
          <TextInput type="password" autoComplete="new-password" value={next} onChange={(e) => setNext(e.target.value)} />
        </Field>
        <Field label="Yeni şifre (tekrar)">
          <TextInput type="password" autoComplete="new-password" value={confirm} onChange={(e) => setConfirm(e.target.value)} />
        </Field>

        {error && <p className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>}

        <div className="flex justify-end gap-2">
          <Button type="button" variant="ghost" onClick={close}>Vazgeç</Button>
          <Button type="submit" loading={busy} disabled={!current || !next || !confirm}>Değiştir</Button>
        </div>
      </form>
    </Modal>
  );
}
