import type { BracketTierDto } from '../../api/types.ts';
import { Button } from '../atoms/Button.tsx';
import { NumberInput } from '../atoms/Inputs.tsx';

interface BracketTierEditorProps {
  tiers: BracketTierDto[];
  onChange: (tiers: BracketTierDto[]) => void;
  error?: string | null;
}

/**
 * Dynamic tier rows for a TIERED_BRACKET rule. Lower bound of row 0 is fixed at 0; each row's lower
 * bound follows the previous row's upper bound. The last row's upper bound is left blank (= infinity).
 */
export function BracketTierEditor({ tiers, onChange, error }: BracketTierEditorProps) {
  const update = (index: number, patch: Partial<BracketTierDto>) => {
    const next = tiers.map((t, i) => (i === index ? { ...t, ...patch } : t));
    // keep bounds contiguous
    for (let i = 1; i < next.length; i++) next[i].lowerBound = next[i - 1].upperBound ?? next[i].lowerBound;
    onChange(next.map((t, i) => ({ ...t, order: i + 1 })));
  };

  const addRow = () => {
    const last = tiers[tiers.length - 1];
    const lower = last ? (last.upperBound ?? last.lowerBound) : 0;
    onChange([
      ...tiers.map((t, i) => (i === tiers.length - 1 ? { ...t, upperBound: t.upperBound ?? lower } : t)),
      { order: tiers.length + 1, lowerBound: lower, upperBound: null, rate: 0 },
    ]);
  };

  const removeRow = (index: number) => {
    const next = tiers.filter((_, i) => i !== index).map((t, i) => ({ ...t, order: i + 1 }));
    if (next.length > 0) next[next.length - 1].upperBound = null;
    onChange(next);
  };

  return (
    <div>
      <div className="mb-1 flex items-center justify-between">
        <span className="label mb-0">Barem kademeleri</span>
        <Button type="button" variant="ghost" className="!py-1 !text-xs" onClick={addRow}>+ Kademe</Button>
      </div>

      <div className="space-y-2">
        {tiers.map((tier, i) => (
          <div key={i} className="grid grid-cols-[1fr_1fr_1fr_auto] items-center gap-2">
            <NumberInput
              aria-label={`Kademe ${i + 1} alt sınır`}
              value={tier.lowerBound}
              onValueChange={(v) => update(i, { lowerBound: v === '' ? 0 : v })}
              disabled={i === 0}
            />
            <NumberInput
              aria-label={`Kademe ${i + 1} üst sınır`}
              placeholder={i === tiers.length - 1 ? '∞ (boş)' : ''}
              value={tier.upperBound ?? ''}
              onValueChange={(v) => update(i, { upperBound: v === '' ? null : v })}
            />
            <NumberInput
              aria-label={`Kademe ${i + 1} oran`}
              step={0.01}
              placeholder="oran (0.05)"
              value={tier.rate}
              onValueChange={(v) => update(i, { rate: v === '' ? 0 : v })}
            />
            <Button
              type="button"
              variant="ghost"
              className="!px-2 !py-1 text-red-600"
              onClick={() => removeRow(i)}
              disabled={tiers.length <= 1}
              aria-label={`Kademe ${i + 1} sil`}
            >
              ✕
            </Button>
          </div>
        ))}
      </div>
      <p className="mt-1 text-xs text-slate-400">Oran ondalık: %5 → 0.05. Aralık yarı-açık [alt, üst).</p>
      {error && <p className="mt-1 text-xs font-medium text-red-600">{error}</p>}
    </div>
  );
}
