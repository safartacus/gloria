import { useMemo, useState } from 'react';
import type { BracketTierDto, CommissionRuleDto } from '../../api/types.ts';
import { friendlyMessage } from '../../lib/errors.ts';
import {
  useCreateRule,
  useCreateRuleVersion,
  useDepartments,
  useRuleTypes,
  useSourceSystems,
} from '../../api/queries.ts';
import { useForm } from '../../hooks/useForm.ts';
import { useToast } from '../molecules/Toast.tsx';
import { Field } from '../molecules/Field.tsx';
import { Select, TextInput, NumberInput } from '../atoms/Inputs.tsx';
import { Button } from '../atoms/Button.tsx';
import { BracketTierEditor } from './BracketTierEditor.tsx';

type Mode = { kind: 'create' } | { kind: 'version'; rule: CommissionRuleDto };

interface RuleFormState extends Record<string, unknown> {
  ruleGroupCode: string;
  name: string;
  ruleTypeCode: string;
  scopeDepartmentCode: string;
  scopeProductCodePattern: string;
  scopeSourceSystemCode: string;
  priority: number | '';
  effectiveFrom: string;
  rate: number | '';
  amountPerTransaction: number | '';
  bracketBasis: string;
}

const DEFAULT_TIERS: BracketTierDto[] = [
  { order: 1, lowerBound: 0, upperBound: 100000, rate: 0.03 },
  { order: 2, lowerBound: 100000, upperBound: 250000, rate: 0.05 },
  { order: 3, lowerBound: 250000, upperBound: null, rate: 0.07 },
];

const today = () => new Date().toISOString().slice(0, 10);

export function RuleForm({ mode, onDone }: { mode: Mode; onDone: () => void }) {
  const ruleTypes = useRuleTypes();
  const departments = useDepartments();
  const sourceSystems = useSourceSystems();
  const toast = useToast();
  const createRule = useCreateRule();
  const createVersion = useCreateRuleVersion();

  const seed = mode.kind === 'version' ? mode.rule : null;

  const { values, setValue, errors, setErrors } = useForm<RuleFormState>({
    ruleGroupCode: seed?.ruleGroupCode ?? '',
    name: seed?.name ?? '',
    ruleTypeCode: seed?.ruleTypeCode ?? 'FIXED_PERCENTAGE',
    scopeDepartmentCode: seed?.scopeDepartmentCode ?? '',
    scopeProductCodePattern: seed?.scopeProductCodePattern ?? '',
    scopeSourceSystemCode: seed?.scopeSourceSystemCode ?? '',
    priority: seed?.priority ?? 100,
    effectiveFrom: today(),
    rate: seed?.rate ?? '',
    amountPerTransaction: seed?.amountPerTransaction ?? '',
    bracketBasis: seed?.bracketBasis ?? 'MONTHLY_CUMULATIVE_BASE',
  });
  const [tiers, setTiers] = useState<BracketTierDto[]>(seed?.bracketTiers?.length ? seed.bracketTiers : DEFAULT_TIERS);
  const [formError, setFormError] = useState<string | null>(null);

  const type = values.ruleTypeCode;
  const pending = createRule.isPending || createVersion.isPending;

  const validate = (): boolean => {
    const next: Partial<Record<keyof RuleFormState, string>> = {};
    if (mode.kind === 'create' && !values.ruleGroupCode.trim()) next.ruleGroupCode = 'Zorunlu';
    if (!values.name.trim()) next.name = 'Zorunlu';
    if (type === 'FIXED_PERCENTAGE' && (values.rate === '' || Number(values.rate) <= 0)) next.rate = '0-1 arası oran girin';
    if (type === 'FIXED_AMOUNT' && (values.amountPerTransaction === '' || Number(values.amountPerTransaction) <= 0)) next.amountPerTransaction = 'Pozitif tutar girin';
    setErrors(next);
    return Object.keys(next).length === 0;
  };

  const scopePayload = () => ({
    scopeDepartmentCode: values.scopeDepartmentCode || null,
    scopeProductCodePattern: values.scopeProductCodePattern || null,
    scopeSourceSystemCode: values.scopeSourceSystemCode || null,
  });

  const typePayload = () => ({
    rate: type === 'FIXED_PERCENTAGE' ? Number(values.rate) : null,
    amountPerTransaction: type === 'FIXED_AMOUNT' ? Number(values.amountPerTransaction) : null,
    bracketBasis: type === 'TIERED_BRACKET' ? values.bracketBasis : null,
    bracketTiers: type === 'TIERED_BRACKET' ? tiers : null,
  });

  const submit = async () => {
    setFormError(null);
    if (!validate()) return;
    try {
      if (mode.kind === 'create') {
        await createRule.mutateAsync({
          ruleGroupCode: values.ruleGroupCode.trim(),
          name: values.name.trim(),
          ruleTypeCode: type,
          priority: Number(values.priority || 100),
          effectiveFrom: values.effectiveFrom,
          ...scopePayload(),
          ...typePayload(),
        });
        toast.success('Kural oluşturuldu');
      } else {
        await createVersion.mutateAsync({
          id: mode.rule.id,
          body: {
            newEffectiveFrom: values.effectiveFrom,
            name: values.name.trim(),
            priority: Number(values.priority || 100),
            ...scopePayload(),
            ...typePayload(),
          },
        });
        toast.success(`v${mode.rule.version + 1} oluşturuldu`);
      }
      onDone();
    } catch (err) {
      setFormError(friendlyMessage(err));
    }
  };

  const departmentOptions = useMemo(() => departments.data ?? [], [departments.data]);
  const sourceOptions = useMemo(() => sourceSystems.data ?? [], [sourceSystems.data]);

  return (
    <form
      className="space-y-4"
      onSubmit={(e) => {
        e.preventDefault();
        void submit();
      }}
    >
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <Field label="Grup kodu" error={errors.ruleGroupCode}>
          <TextInput
            value={values.ruleGroupCode}
            disabled={mode.kind === 'version'}
            placeholder="SPA_SERVICE"
            onChange={(e) => setValue('ruleGroupCode', e.target.value)}
          />
        </Field>
        <Field label="Ad" error={errors.name}>
          <TextInput value={values.name} onChange={(e) => setValue('name', e.target.value)} />
        </Field>

        <Field label="Kural tipi" hint={mode.kind === 'version' ? 'Versiyonlar tipi değiştiremez' : undefined}>
          <Select
            value={type}
            disabled={mode.kind === 'version'}
            onChange={(e) => setValue('ruleTypeCode', e.target.value)}
          >
            {(ruleTypes.data ?? []).map((t) => (
              <option key={t.code} value={t.code}>{t.name}</option>
            ))}
          </Select>
        </Field>
        <Field label="Öncelik" hint="Küçük değer önce eşleşir">
          <NumberInput value={values.priority} onValueChange={(v) => setValue('priority', v)} />
        </Field>

        <Field label={mode.kind === 'version' ? 'Yeni geçerlilik başlangıcı' : 'Geçerlilik başlangıcı'}>
          <TextInput type="date" value={values.effectiveFrom} onChange={(e) => setValue('effectiveFrom', e.target.value)} />
        </Field>
        <div />

        <Field label="Kapsam — departman" hint="Boş = tüm departmanlar">
          <Select value={values.scopeDepartmentCode} onChange={(e) => setValue('scopeDepartmentCode', e.target.value)}>
            <option value="">(hepsi)</option>
            {departmentOptions.map((d) => (
              <option key={d.code} value={d.code}>{d.code}</option>
            ))}
          </Select>
        </Field>
        <Field label="Kapsam — kaynak sistem" hint="Boş = tüm kaynaklar">
          <Select value={values.scopeSourceSystemCode} onChange={(e) => setValue('scopeSourceSystemCode', e.target.value)}>
            <option value="">(hepsi)</option>
            {sourceOptions.map((s) => (
              <option key={s.code} value={s.code}>{s.code}</option>
            ))}
          </Select>
        </Field>

        <Field label="Kapsam — ürün kodu deseni" hint="% joker: SPA_% , %_MSJ%">
          <TextInput
            value={values.scopeProductCodePattern}
            placeholder="SPA_%"
            onChange={(e) => setValue('scopeProductCodePattern', e.target.value)}
          />
        </Field>
        <div />
      </div>

      <hr className="border-slate-200" />

      {type === 'FIXED_PERCENTAGE' && (
        <Field label="Oran" error={errors.rate} hint="%5 → 0.05">
          <NumberInput step={0.01} value={values.rate} onValueChange={(v) => setValue('rate', v)} />
        </Field>
      )}

      {type === 'FIXED_AMOUNT' && (
        <Field label="İşlem başına tutar (TL)" error={errors.amountPerTransaction}>
          <NumberInput step={1} value={values.amountPerTransaction} onValueChange={(v) => setValue('amountPerTransaction', v)} />
        </Field>
      )}

      {type === 'TIERED_BRACKET' && (
        <div className="space-y-3">
          <Field label="Barem bazı">
            <Select value={values.bracketBasis} onChange={(e) => setValue('bracketBasis', e.target.value)}>
              <option value="MONTHLY_CUMULATIVE_BASE">Aylık kümülatif matrah</option>
              <option value="TRANSACTION_AMOUNT">İşlem tutarı</option>
            </Select>
          </Field>
          <BracketTierEditor tiers={tiers} onChange={setTiers} />
        </div>
      )}

      {formError && (
        <p className="rounded-md border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700">{formError}</p>
      )}

      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onDone}>Vazgeç</Button>
        <Button type="submit" loading={pending}>
          {mode.kind === 'create' ? 'Kuralı oluştur' : 'Yeni versiyon oluştur'}
        </Button>
      </div>
    </form>
  );
}
