import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { api } from './client.ts';
import type {
  CommissionCalculationRequest,
  CommissionListItemDto,
  CommissionResultDto,
  CommissionRuleDto,
  CreatedUserResult,
  CreateCommissionRuleRequest,
  CreateUserRequest,
  DepartmentDto,
  EmployeeDto,
  EndOfDayImportResult,
  ImportBatchLogDto,
  Me,
  NewVersionRequest,
  NightlyBatchResult,
  PagedResult,
  PeriodCommissionSummary,
  PeriodDto,
  PersonnelSyncResult,
  RoleDto,
  RuleTypeDto,
  SourceSystemDto,
  UserDto,
} from './types.ts';

export interface PeriodCommissionQuery {
  year: number;
  month: number;
  page: number;
  pageSize: number;
  sortBy: string;
  sortDir: 'asc' | 'desc';
}

export const keys = {
  me: ['me'] as const,
  ruleTypes: ['ruleTypes'] as const,
  sourceSystems: ['sourceSystems'] as const,
  departments: ['departments'] as const,
  employees: ['employees'] as const,
  users: ['users'] as const,
  rules: ['rules'] as const,
  periods: ['periods'] as const,
  periodCommissions: (q: PeriodCommissionQuery) =>
    ['commissions', 'period', q.year, q.month, q.page, q.pageSize, q.sortBy, q.sortDir] as const,
  periodCommissionSummary: (year: number, month: number) => ['commissions', 'summary', year, month] as const,
  employeeCommission: (employeeId: number, year: number, month: number) =>
    ['commissions', 'employee', employeeId, year, month] as const,
  myCommission: (year: number, month: number) => ['commissions', 'me', year, month] as const,
};

export const useMe = () => useQuery({ queryKey: keys.me, queryFn: () => api.get<Me>('/me') });

export const useRuleTypes = () =>
  useQuery({ queryKey: keys.ruleTypes, queryFn: () => api.get<RuleTypeDto[]>('/rule-types'), staleTime: Infinity });

export const useRoles = () =>
  useQuery({ queryKey: ['roles'], queryFn: () => api.get<RoleDto[]>('/roles'), staleTime: Infinity });

export const useSourceSystems = () =>
  useQuery({ queryKey: keys.sourceSystems, queryFn: () => api.get<SourceSystemDto[]>('/source-systems'), staleTime: Infinity });

export const useDepartments = () =>
  useQuery({ queryKey: keys.departments, queryFn: () => api.get<DepartmentDto[]>('/departments'), staleTime: Infinity });

export const useEmployees = () =>
  useQuery({ queryKey: keys.employees, queryFn: () => api.get<EmployeeDto[]>('/employees') });

export const useUsers = () =>
  useQuery({ queryKey: keys.users, queryFn: () => api.get<UserDto[]>('/users') });

export function useCreateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateUserRequest) => api.post<CreatedUserResult>('/users', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.users }),
  });
}

/** Additive roster sync from data/personel.csv — creates missing employees + logins. Same job runs nightly. */
export function useSyncPersonnel() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<PersonnelSyncResult>('/users/sync'),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.users });
      qc.invalidateQueries({ queryKey: keys.employees });
    },
  });
}

export function useSetUserActive() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ username, active }: { username: string; active: boolean }) =>
      api.post(`/users/${encodeURIComponent(username)}/${active ? 'activate' : 'deactivate'}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.users }),
  });
}

export const useRules = () =>
  useQuery({ queryKey: keys.rules, queryFn: () => api.get<CommissionRuleDto[]>('/rules') });

export const usePeriods = () =>
  useQuery({ queryKey: keys.periods, queryFn: () => api.get<PeriodDto[]>('/periods') });

export const usePeriodCommissions = (q: PeriodCommissionQuery) =>
  useQuery({
    queryKey: keys.periodCommissions(q),
    queryFn: () =>
      api.get<PagedResult<CommissionListItemDto>>(
        `/commissions?year=${q.year}&month=${q.month}&page=${q.page}&pageSize=${q.pageSize}` +
          `&sortBy=${encodeURIComponent(q.sortBy)}&sortDir=${q.sortDir}`,
      ),
    placeholderData: keepPreviousData,
  });

export const usePeriodCommissionSummary = (year: number, month: number) =>
  useQuery({
    queryKey: keys.periodCommissionSummary(year, month),
    queryFn: () => api.get<PeriodCommissionSummary>(`/commissions/summary?year=${year}&month=${month}`),
  });

export const useEmployeeCommission = (employeeId: number | null, year: number, month: number) =>
  useQuery({
    queryKey: keys.employeeCommission(employeeId ?? 0, year, month),
    queryFn: () => api.get<CommissionResultDto>(`/commissions/${employeeId}?year=${year}&month=${month}`),
    enabled: employeeId != null,
    retry: false,
  });

export const useMyCommission = (year: number, month: number, enabled = true) =>
  useQuery({
    queryKey: keys.myCommission(year, month),
    queryFn: () => api.get<CommissionResultDto>(`/commissions/me?year=${year}&month=${month}`),
    retry: false,
    enabled,
  });

export function useCreateRule() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateCommissionRuleRequest) => api.post<CommissionRuleDto>('/rules', body),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.rules }),
  });
}

export function useCreateRuleVersion() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, body }: { id: number; body: NewVersionRequest }) =>
      api.post<CommissionRuleDto>(`/rules/${id}/versions`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.rules }),
  });
}

export function useDeactivateRule() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, effectiveTo }: { id: number; effectiveTo: string }) =>
      api.post<CommissionRuleDto>(`/rules/${id}/deactivate?effectiveTo=${effectiveTo}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.rules }),
  });
}

export function useCalculateCommissions() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CommissionCalculationRequest) =>
      api.post<CommissionResultDto[]>('/commissions/calculate', body),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['commissions'] });
      qc.invalidateQueries({ queryKey: keys.periods });
    },
  });
}

export function useClosePeriod() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ year, month }: { year: number; month: number }) =>
      api.post<PeriodDto>(`/periods/close?year=${year}&month=${month}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: keys.periods });
      qc.invalidateQueries({ queryKey: ['commissions'] });
    },
  });
}

function invalidateAfterImport(qc: ReturnType<typeof useQueryClient>) {
  qc.invalidateQueries({ queryKey: keys.periods });
  qc.invalidateQueries({ queryKey: ['commissions'] });
}

/** Upload PMS/POS/ERP CSV files (multipart) and run the end-of-day import. */
export function useRunEndOfDayImport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (form: FormData) => api.postForm<EndOfDayImportResult>('/import/end-of-day', form),
    onSuccess: () => invalidateAfterImport(qc),
  });
}

/** Run the import over the bundled sample CSVs under data/ (demo convenience). */
export function useRunSampleImport() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<EndOfDayImportResult>('/import/end-of-day/sample'),
    onSuccess: () => invalidateAfterImport(qc),
  });
}

/** Import batches (nightly + manual) that ran on a given UTC day, each with its quarantined rows. */
export const useImportBatchesByDate = (date: string) =>
  useQuery({
    queryKey: ['importBatches', 'byDate', date],
    queryFn: () => api.get<ImportBatchLogDto[]>(`/import/batches/by-date?date=${date}`),
    enabled: date.length === 10,
  });

/**
 * Run the whole nightly batch now: roster sync + folder import + commission recalculation
 * for every touched period. Same code path as the scheduled job.
 */
export function useRunNightlyBatch() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () => api.post<NightlyBatchResult>('/import/end-of-day/run-nightly'),
    onSuccess: () => {
      invalidateAfterImport(qc);
      qc.invalidateQueries({ queryKey: keys.users });
      qc.invalidateQueries({ queryKey: keys.employees });
    },
  });
}
