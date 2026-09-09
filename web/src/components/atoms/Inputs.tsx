import type { InputHTMLAttributes, SelectHTMLAttributes, LabelHTMLAttributes } from 'react';
import { cn } from '../../lib/cn.ts';

export function Label({ className, ...rest }: LabelHTMLAttributes<HTMLLabelElement>) {
  return <label className={cn('label', className)} {...rest} />;
}

export function TextInput({ className, ...rest }: InputHTMLAttributes<HTMLInputElement>) {
  return <input className={cn('input', className)} {...rest} />;
}

interface NumberInputProps extends Omit<InputHTMLAttributes<HTMLInputElement>, 'value' | 'onChange' | 'type'> {
  value: number | '';
  onValueChange: (value: number | '') => void;
}

export function NumberInput({ value, onValueChange, className, ...rest }: NumberInputProps) {
  return (
    <input
      type="number"
      className={cn('input', className)}
      value={value}
      onChange={(e) => onValueChange(e.target.value === '' ? '' : Number(e.target.value))}
      {...rest}
    />
  );
}

export function Select({ className, children, ...rest }: SelectHTMLAttributes<HTMLSelectElement>) {
  return (
    <select className={cn('input bg-white', className)} {...rest}>
      {children}
    </select>
  );
}
