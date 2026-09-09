// Mirrors the Gloria.Api DTOs.

export type RoleCode = 'ADMIN' | 'ACCOUNTING' | 'EMPLOYEE';

export interface Me {
  username: string;
  fullName: string;
  roleCode: RoleCode | 'NONE';
  employeeId: number | null;
  mustChangePassword: boolean;
}

export interface RoleDto {
  code: string;
  name: string;
  description: string | null;
}

export interface RuleTypeDto {
  code: string;
  name: string;
  handlerKey: string;
  isActive: boolean;
}

export interface SourceSystemDto {
  code: string;
  name: string;
}

export interface DepartmentDto {
  id: number;
  code: string;
  name: string;
}

export interface EmployeeDto {
  id: number;
  employeeNo: string;
  fullName: string;
  departmentId: number;
  hotel: string | null;
}

export interface UserDto {
  username: string;
  fullName: string;
  roleCode: string;
  employeeNo: string | null;
  isActive: boolean;
  mustChangePassword: boolean;
  createdAt: string;
}

export interface CreateUserRequest {
  username: string;
  roleCode: string;
  employeeNo?: string | null;
}

export interface CreatedUserResult {
  username: string;
  fullName: string;
  roleCode: string;
  password: string;
  credentialFilePath: string | null;
}

export interface SyncedCredential {
  username: string;
  password: string;
}

export interface PersonnelSyncResult {
  departmentsAdded: number;
  employeesAdded: number;
  usersAdded: number;
  skipped: string[];
  errors: string[];
  credentials: SyncedCredential[];
}

export interface BracketTierDto {
  order: number;
  lowerBound: number;
  upperBound: number | null;
  rate: number;
}

export interface CommissionRuleDto {
  id: number;
  ruleGroupCode: string;
  version: number;
  name: string;
  ruleTypeCode: string;
  scopeDepartmentCode: string | null;
  scopeProductCodePattern: string | null;
  scopeSourceSystemCode: string | null;
  priority: number;
  effectiveFrom: string;
  effectiveTo: string | null;
  isActive: boolean;
  rate: number | null;
  amountPerTransaction: number | null;
  bracketBasis: string | null;
  bracketTiers: BracketTierDto[];
  createdByUsername: string;
  createdAt: string;
}

export interface CreateCommissionRuleRequest {
  ruleGroupCode: string;
  name: string;
  ruleTypeCode: string;
  scopeDepartmentCode?: string | null;
  scopeProductCodePattern?: string | null;
  scopeSourceSystemCode?: string | null;
  priority: number;
  effectiveFrom: string;
  rate?: number | null;
  amountPerTransaction?: number | null;
  bracketBasis?: string | null;
  bracketTiers?: BracketTierDto[] | null;
}

export interface NewVersionRequest {
  newEffectiveFrom: string;
  name?: string | null;
  scopeDepartmentCode?: string | null;
  scopeProductCodePattern?: string | null;
  scopeSourceSystemCode?: string | null;
  priority?: number | null;
  rate?: number | null;
  amountPerTransaction?: number | null;
  bracketBasis?: string | null;
  bracketTiers?: BracketTierDto[] | null;
}

export interface CalculationStepDto {
  order: number;
  saleRecordId: number;
  commissionRuleId: number;
  commissionRuleVersion: number;
  ruleTypeCode: string;
  productCode: string;
  base: number;
  appliedRate: number | null;
  appliedAmount: number | null;
  stepAmount: number;
  runningTotal: number;
  isRefund: boolean;
  explanation: string;
}

export interface CommissionResultDto {
  periodId: number;
  year: number;
  month: number;
  periodStatus: 'OPEN' | 'CLOSED';
  employeeId: number;
  employeeNo: string;
  employeeName: string;
  totalCommission: number;
  saleCount: number;
  refundCount: number;
  unmatchedCount: number;
  status: 'DRAFT' | 'FINALIZED';
  calculatedAt: string;
  calculatedByUsername: string;
  steps: CalculationStepDto[];
}

export interface CommissionCalculationRequest {
  year: number;
  month: number;
  employeeId?: number | null;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface CommissionListItemDto {
  periodId: number;
  year: number;
  month: number;
  periodStatus: 'OPEN' | 'CLOSED';
  employeeId: number;
  employeeNo: string;
  employeeName: string;
  totalCommission: number;
  saleCount: number;
  refundCount: number;
  unmatchedCount: number;
  status: 'DRAFT' | 'FINALIZED';
  calculatedAt: string;
  calculatedByUsername: string;
}

export interface PeriodCommissionSummary {
  employeeCount: number;
  totalCommission: number;
  totalRefunds: number;
  totalUnmatched: number;
}

export interface PeriodDto {
  id: number;
  year: number;
  month: number;
  status: 'OPEN' | 'CLOSED';
  closedByUsername: string | null;
  closedAt: string | null;
}

export interface ImportErrorDto {
  sourceSystemCode: string;
  rowNumber: number;
  errorCode: string;
  errorMessage: string;
  rawContent: string;
}

export interface AffectedPeriod {
  year: number;
  month: number;
}

export interface EndOfDayImportResult {
  batchId: number;
  businessDay: string;
  status: string;
  rowsRead: number;
  rowsInserted: number;
  rowsDuplicate: number;
  rowsFailed: number;
  rowsReconciled: number;
  errors: ImportErrorDto[];
  affectedPeriods: AffectedPeriod[];
}

export interface ImportBatchLogDto {
  batchId: number;
  businessDay: string;
  description: string;
  status: string;
  startedAt: string;
  finishedAt: string | null;
  runByUsername: string;
  rowsRead: number;
  rowsInserted: number;
  rowsDuplicate: number;
  rowsFailed: number;
  rowsReconciled: number;
  errors: ImportErrorDto[];
}

export interface PeriodCalculationOutcome {
  year: number;
  month: number;
  calculated: boolean;
  skippedReason: string | null;
  employeeCount: number;
  totalCommission: number;
  totalRefunds: number;
  totalUnmatched: number;
}

export interface NightlyBatchResult {
  ranAtUtc: string;
  personnelSync: PersonnelSyncResult;
  import: EndOfDayImportResult | null;
  importSkippedReason: string | null;
  calculations: PeriodCalculationOutcome[];
}

export interface EndOfDayImportRequest {
  businessDay: string;
  filesBySource: Record<string, string>;
}

export interface ReferenceImportResult {
  roles: number;
  sourceSystems: number;
  ruleTypes: number;
  departments: number;
  employees: number;
  users: number;
  notes: string[];
}
