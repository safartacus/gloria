import { MONTHS_TR } from '../../lib/format.ts';
import { Select } from '../atoms/Inputs.tsx';

interface MonthPickerProps {
  year: number;
  month: number;
  onChange: (year: number, month: number) => void;
  years?: number[];
}

const DEFAULT_YEARS = [2025, 2026, 2027];

export function MonthPicker({ year, month, onChange, years = DEFAULT_YEARS }: MonthPickerProps) {
  return (
    <div className="flex items-center gap-2">
      <Select
        aria-label="Yıl"
        className="w-28"
        value={year}
        onChange={(e) => onChange(Number(e.target.value), month)}
      >
        {years.map((y) => (
          <option key={y} value={y}>{y}</option>
        ))}
      </Select>
      <Select
        aria-label="Ay"
        className="w-36"
        value={month}
        onChange={(e) => onChange(year, Number(e.target.value))}
      >
        {MONTHS_TR.map((name, idx) => (
          <option key={name} value={idx + 1}>{name}</option>
        ))}
      </Select>
    </div>
  );
}
