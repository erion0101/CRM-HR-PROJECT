export type LeaveAccrualMethod = 'Monthly' | 'YearlyUpfront';
export type LeaveRequestStatus = 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';

export interface LeaveTypeInput {
  name: string;
  color: string;
  isPaid: boolean;
  requiresApproval: boolean;
  /** False for kinds of leave without an allowance (days are only counted). */
  deductsFromBalance: boolean;
  daysPerYear: number;
  accrualMethod: LeaveAccrualMethod;
  carriesOver: boolean;
  /** Null means no limit. */
  maxCarryOverDays: number | null;
  /** Both null means carried-over days never expire. */
  carryOverExpiryMonth: number | null;
  carryOverExpiryDay: number | null;
}

export interface LeaveType extends LeaveTypeInput {
  id: string;
  isArchived: boolean;
  version: number;
}

export interface PublicHoliday {
  id: string;
  /** ISO date (yyyy-mm-dd). For a yearly holiday, the first year it applies. */
  date: string;
  name: string;
  repeatsYearly: boolean;
}

export interface LeaveBalance {
  leaveTypeId: string;
  leaveTypeName: string;
  color: string;
  deductsFromBalance: boolean;
  year: number;
  fullYearAllowance: number;
  earnedToDate: number;
  carriedOver: number;
  carryOverExpiresOn?: string | null;
  carryOverExpired: number;
  adjustments: number;
  approved: number;
  pending: number;
  available: number;
}

export interface LeaveRequest {
  id: string;
  employeeId: string;
  employeeName: string;
  leaveTypeId: string;
  leaveTypeName: string;
  color: string;
  startDate: string;
  endDate: string;
  workingDays: number;
  reason?: string | null;
  status: LeaveRequestStatus;
  approverName?: string | null;
  decisionComment?: string | null;
  decidedAtUtc?: string | null;
  createdAtUtc: string;
  canCancel: boolean;
}

export interface LeaveAdjustment {
  id: string;
  leaveTypeId: string;
  leaveTypeName: string;
  year: number;
  days: number;
  reason: string;
  createdAtUtc: string;
}

export interface EmployeeLeave {
  employeeId: string;
  employeeName: string;
  year: number;
  balances: LeaveBalance[];
  requests: LeaveRequest[];
  adjustments: LeaveAdjustment[];
}

export interface LeavePreview {
  workingDays: number;
  availableBefore?: number | null;
  availableAfter?: number | null;
  holidays: PublicHoliday[];
  /** Why the request cannot be sent, in plain words. */
  problem?: string | null;
}

export interface LeaveApproval {
  request: LeaveRequest;
  availableAfter?: number | null;
  teammatesOut: string[];
}

export interface LeaveCalendarEntry {
  employeeId: string;
  employeeName: string;
  startDate: string;
  endDate: string;
  status: LeaveRequestStatus;
  /** Only shown to the person, their manager and HR. */
  leaveTypeName?: string | null;
  color?: string | null;
}

export interface LeaveCalendar {
  from: string;
  to: string;
  holidays: PublicHoliday[];
  entries: LeaveCalendarEntry[];
}
