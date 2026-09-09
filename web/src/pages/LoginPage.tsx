import { useState } from 'react';
import { friendlyMessage } from '../lib/errors.ts';
import { useAuth } from '../auth/AuthProvider.tsx';
import { Card } from '../components/atoms/Card.tsx';
import { Button } from '../components/atoms/Button.tsx';
import { TextInput } from '../components/atoms/Inputs.tsx';
import { Field } from '../components/molecules/Field.tsx';

export function LoginPage() {
  const { login } = useAuth();
  const [username, setUsername] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const submit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await login(username.trim(), password);
    } catch (err) {
      setError(friendlyMessage(err));
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="flex min-h-full items-center justify-center bg-slate-100 p-4">
      <Card className="w-full max-w-sm p-6">
        <div className="mb-5 text-center">
          <h1 className="text-lg font-semibold text-slate-900">Gloria · Prim</h1>
          <p className="mt-1 text-sm text-slate-500">Devam etmek için giriş yapın</p>
        </div>

        <form className="space-y-4" onSubmit={submit}>
          <Field label="Kullanıcı adı">
            <TextInput
              autoFocus
              autoComplete="username"
              value={username}
              onChange={(e) => setUsername(e.target.value)}
              placeholder="admin / accounting / P1001"
            />
          </Field>
          <Field label="Şifre">
            <TextInput
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </Field>

          {error && (
            <p className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{error}</p>
          )}

          <Button type="submit" className="w-full" loading={busy} disabled={!username || !password}>
            Giriş yap
          </Button>
        </form>

        <p className="mt-4 text-center text-xs text-slate-400">
          Şifreler ilk kurulumda API loguna yazılır (Development).
        </p>
      </Card>
    </div>
  );
}
