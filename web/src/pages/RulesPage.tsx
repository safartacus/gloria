import { useMemo, useState } from 'react';
import type { CommissionRuleDto } from '../api/types.ts';
import { useDeactivateRule, useRules } from '../api/queries.ts';
import { formatDate } from '../lib/format.ts';
import { friendlyMessage } from '../lib/errors.ts';
import { PageHeader } from '../components/templates/PageHeader.tsx';
import { QueryBoundary } from '../components/molecules/QueryBoundary.tsx';
import { Modal } from '../components/molecules/Modal.tsx';
import { ConfirmButton } from '../components/molecules/ConfirmButton.tsx';
import { useToast } from '../components/molecules/Toast.tsx';
import { DataTable } from '../components/organisms/DataTable.tsx';
import type { Column } from '../components/organisms/DataTable.tsx';
import { RuleForm } from '../components/organisms/RuleForm.tsx';
import { Button } from '../components/atoms/Button.tsx';
import { Badge } from '../components/atoms/Badge.tsx';
import { Money, Percent } from '../components/atoms/Display.tsx';

const typeTone = (code: string) =>
  code === 'FIXED_PERCENTAGE' ? 'blue' : code === 'TIERED_BRACKET' ? 'amber' : 'green';

function scopeText(r: CommissionRuleDto): string {
  const parts = [
    r.scopeSourceSystemCode,
    r.scopeDepartmentCode,
    r.scopeProductCodePattern,
  ].filter(Boolean);
  return parts.length ? parts.join(' · ') : '(tümü)';
}

export function RulesPage() {
  const rules = useRules();
  const deactivate = useDeactivateRule();
  const toast = useToast();
  const [modal, setModal] = useState<{ kind: 'create' } | { kind: 'version'; rule: CommissionRuleDto } | null>(null);

  const columns = useMemo<Column<CommissionRuleDto>[]>(
    () => [
      {
        key: 'group',
        header: 'Grup / Versiyon',
        render: (r) => (
          <div>
            <div className="font-medium text-slate-800">{r.ruleGroupCode}</div>
            <div className="text-xs text-slate-400">v{r.version} — {r.name}</div>
          </div>
        ),
      },
      { key: 'type', header: 'Tip', render: (r) => <Badge tone={typeTone(r.ruleTypeCode)}>{r.ruleTypeCode}</Badge> },
      { key: 'scope', header: 'Kapsam', render: (r) => <span className="text-xs text-slate-500">{scopeText(r)}</span> },
      {
        key: 'param',
        header: 'Parametre',
        align: 'right',
        render: (r) =>
          r.ruleTypeCode === 'FIXED_PERCENTAGE' ? <Percent value={r.rate} />
          : r.ruleTypeCode === 'FIXED_AMOUNT' ? <Money value={r.amountPerTransaction ?? 0} />
          : <span className="text-xs text-slate-500">{r.bracketTiers.length} kademe</span>,
      },
      { key: 'priority', header: 'Öncelik', align: 'right', render: (r) => r.priority },
      {
        key: 'effective',
        header: 'Geçerlilik',
        render: (r) => (
          <span className="text-xs text-slate-500">{formatDate(r.effectiveFrom)} → {r.effectiveTo ? formatDate(r.effectiveTo) : '∞'}</span>
        ),
      },
      { key: 'active', header: 'Durum', render: (r) => <Badge tone={r.isActive ? 'green' : 'slate'}>{r.isActive ? 'Aktif' : 'Pasif'}</Badge> },
      {
        key: 'actions',
        header: '',
        align: 'right',
        render: (r) => (
          <div className="flex justify-end gap-2">
            <Button variant="ghost" className="!py-1 !text-xs" onClick={() => setModal({ kind: 'version', rule: r })}>
              Yeni versiyon
            </Button>
            {r.isActive && (
              <ConfirmButton
                label="Deaktive"
                confirmLabel="Deaktive et"
                loading={deactivate.isPending}
                onConfirm={() =>
                  deactivate.mutate(
                    { id: r.id, effectiveTo: new Date().toISOString().slice(0, 10) },
                    { onSuccess: () => toast.success('Kural deaktive edildi'), onError: (e) => toast.error(friendlyMessage(e)) },
                  )
                }
              />
            )}
          </div>
        ),
      },
    ],
    [deactivate, toast],
  );

  return (
    <div>
      <PageHeader
        title="Prim Kuralları"
        description="Tüm versiyonlar. Yeni kural veya oran değişikliği kod gerektirmez."
        actions={<Button onClick={() => setModal({ kind: 'create' })}>+ Yeni kural</Button>}
      />

      <QueryBoundary query={rules} empty="Henüz kural tanımlı değil" isEmpty={(r) => r.length === 0}>
        {(rows) => <DataTable columns={columns} rows={rows} getRowKey={(r) => r.id} />}
      </QueryBoundary>

      <Modal
        open={modal != null}
        wide
        title={modal?.kind === 'version' ? `Yeni versiyon — ${modal.rule.ruleGroupCode}` : 'Yeni prim kuralı'}
        onClose={() => setModal(null)}
      >
        {modal && (
          <RuleForm
            mode={modal.kind === 'create' ? { kind: 'create' } : { kind: 'version', rule: modal.rule }}
            onDone={() => setModal(null)}
          />
        )}
      </Modal>
    </div>
  );
}
