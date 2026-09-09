import { toFriendlyError } from '../../lib/errors.ts';
import { Button } from '../atoms/Button.tsx';

/**
 * The one place a user-facing error is rendered. Shows a calm message; for unexpected
 * (server) errors it hides the internal detail and offers a reference code for IT.
 */
export function ErrorState({ error, onRetry }: { error: unknown; onRetry?: () => void }) {
  const f = toFriendlyError(error);

  return (
    <div className="mx-auto max-w-md rounded-lg border border-slate-200 bg-white px-6 py-8 text-center shadow-sm">
      <div
        className={`mx-auto flex h-10 w-10 items-center justify-center rounded-full text-lg ${
          f.kind === 'expected' ? 'bg-amber-100 text-amber-700' : 'bg-red-100 text-red-700'
        }`}
        aria-hidden
      >
        {f.kind === 'offline' ? '⚡' : '!'}
      </div>

      <h2 className="mt-3 text-base font-semibold text-slate-800">{f.title}</h2>
      <p className="mt-1 text-sm text-slate-600">{f.message}</p>

      {f.reference && (
        <p className="mt-3 text-xs text-slate-400">
          Referans kodu: <span className="font-mono text-slate-500">{f.reference}</span>
        </p>
      )}

      {onRetry && f.kind !== 'session' && (
        <Button variant="ghost" className="mt-4" onClick={onRetry}>
          Tekrar dene
        </Button>
      )}
    </div>
  );
}
