import { useState } from 'react';
import { Button } from '../atoms/Button.tsx';

interface ConfirmButtonProps {
  label: string;
  confirmLabel?: string;
  onConfirm: () => void;
  variant?: 'primary' | 'ghost' | 'danger';
  loading?: boolean;
  disabled?: boolean;
}

export function ConfirmButton({
  label,
  confirmLabel = 'Emin misiniz?',
  onConfirm,
  variant = 'danger',
  loading,
  disabled,
}: ConfirmButtonProps) {
  const [armed, setArmed] = useState(false);

  if (armed) {
    return (
      <span className="inline-flex items-center gap-1">
        <Button variant={variant} loading={loading} disabled={disabled} onClick={() => { setArmed(false); onConfirm(); }}>
          {confirmLabel}
        </Button>
        <Button variant="ghost" onClick={() => setArmed(false)}>Vazgeç</Button>
      </span>
    );
  }

  return (
    <Button variant={variant} disabled={disabled} onClick={() => setArmed(true)}>
      {label}
    </Button>
  );
}
