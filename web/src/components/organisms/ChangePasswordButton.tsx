import { useState } from 'react';
import { Button } from '../atoms/Button.tsx';
import { ChangePasswordModal } from './ChangePasswordModal.tsx';

export function ChangePasswordButton({
  variant = 'ghost',
  label = 'Şifre değiştir',
}: {
  variant?: 'primary' | 'ghost' | 'danger';
  label?: string;
}) {
  const [open, setOpen] = useState(false);
  return (
    <>
      <Button variant={variant} className="!py-1.5" onClick={() => setOpen(true)}>
        {label}
      </Button>
      <ChangePasswordModal open={open} onClose={() => setOpen(false)} />
    </>
  );
}
