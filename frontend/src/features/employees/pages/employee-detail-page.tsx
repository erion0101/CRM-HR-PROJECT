import { useMutation } from '@tanstack/react-query';
import {
  ArrowLeft,
  BriefcaseBusiness,
  Download,
  FileText,
  Folder,
  FolderOpen,
  Pencil,
  Plus,
  ShieldCheck,
  Trash2,
  Upload
} from 'lucide-react';
import { useEffect, useRef, useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useParams, useSearchParams } from 'react-router';

import { canManagePeople, canViewSalary } from '@/app/access';
import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { FieldShell, Input, Select, Textarea } from '@/components/forms/field';
import { SearchInput } from '@/components/forms/search-input';
import { Tabs } from '@/components/navigation/tabs';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { Dialog } from '@/components/ui/dialog';
import { useAuth } from '@/features/auth/hooks/use-auth';
import {
  addCompensation,
  activateContract,
  createContract,
  terminateContract
} from '@/features/contracts/api/contracts-api';
import { useEmployeeCompensation, useEmployeeContracts } from '@/features/contracts/hooks/use-contracts';
import type { Contract } from '@/features/contracts/types';
import { archiveDocument, downloadDocument, uploadDocument } from '@/features/documents/api/documents-api';
import { DocumentPreviewDialog } from '@/features/documents/components/document-preview-dialog';
import { documentKeys, useDocumentFolders, useEmployeeDocuments } from '@/features/documents/hooks/use-documents';
import type { EmployeeDocument } from '@/features/documents/types';
import { useDepartments, usePositions } from '@/features/organization/hooks/use-organization';
import { toAppError } from '@/lib/api-error';

import { addAssignment, archiveEmployee, getEmployee, setEmployment, updateEmployee } from '../api/employees-api';
import { EmployeeForm } from '../components/employee-form';
import { EmployeeActivityTab } from '../components/employee-activity-tab';
import { EmployeeProfileSummary } from '../components/employee-profile-summary';
import { EmploymentHistoryTab } from '../components/employment-history-tab';
import { useEmployee, useEmployeeProfile } from '../hooks/use-employees';
import { employeeKeys } from '../hooks/use-employees';
import type { Employee, EmployeeInput, EmployeeProfile, EmploymentStatus } from '../types';
import type { Department, Position } from '@/features/organization/types';

const today = () => new Date().toISOString().slice(0, 10);
const formatDate = (value: string | null) =>
  value
    ? new Intl.DateTimeFormat(undefined, { dateStyle: 'medium' }).format(new Date(`${value}T00:00:00`))
    : 'No end date';

export function EmployeeDetailPage() {
  const { employeeId = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const { user } = useAuth();
  const { notify } = useNotification();
  const navigate = useNavigate();
  const [confirmArchive, setConfirmArchive] = useState(false);
  const canEdit = canManagePeople(user);
  const canUploadDocuments = canEdit || user?.employeeId === employeeId;
  const salaryAccess = canViewSalary(user);
  const tab = params.get('tab') ?? 'overview';
  const employee = useEmployee(employeeId);
  const profile = useEmployeeProfile(employeeId);
  const departments = useDepartments(undefined, 100);
  const positions = usePositions(undefined, 100);
  const archive = useMutation({
    mutationFn: (target: Employee) => archiveEmployee(target.id, target.version),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: employeeKeys.all });
      notify({ title: 'Employee archived', tone: 'success' });
      navigate('/employees');
    },
    onError: (error) => notify({ title: 'Archive failed', message: toAppError(error).message, tone: 'error' })
  });
  const tabs = [
    { id: 'overview', label: 'Overview' },
    { id: 'employment', label: 'Employment' },
    { id: 'history', label: 'History' },
    { id: 'contracts', label: 'Contracts' },
    ...(salaryAccess ? [{ id: 'compensation', label: 'Compensation' }] : []),
    { id: 'documents', label: 'Documents' },
    { id: 'activity', label: 'Activity' }
  ];

  if (employee.isLoading || profile.isLoading)
    return (
      <div className="page-stack">
        <Skeleton count={6} />
      </div>
    );
  if (employee.isError || profile.isError || !employee.data || !profile.data)
    return (
      <ErrorState
        title="Employee record unavailable"
        description="This record may not exist or may be outside your access scope."
        onRetry={() => {
          void employee.refetch();
          void profile.refetch();
        }}
      />
    );
  const item = employee.data;

  return (
    <div className="page-stack">
      <Link className="back-link" to="/employees">
        <ArrowLeft size={16} /> Employee directory
      </Link>
      <div className="employee-profile-layout">
        <aside>
          <EmployeeProfileSummary profile={profile.data} />
        </aside>
        <section className="employee-profile-main">
          <header className="employee-profile-heading">
            <div>
              <span className="eyebrow">Employee profile</span>
              <h1>
                {item.firstName} {item.lastName}
              </h1>
              <p>
                {profile.data.position?.name ?? 'Position not assigned'} ·{' '}
                {profile.data.department?.name ?? 'Department not assigned'}
              </p>
            </div>
            {canEdit ? (
              <Button variant="secondary" size="small" onClick={() => setConfirmArchive(true)}>
                <Trash2 size={15} /> Archive employee
              </Button>
            ) : null}
          </header>
          <Tabs
            items={tabs}
            activeId={tab}
            onChange={(value) => setParams(value === 'overview' ? {} : { tab: value })}
          />
          {tab === 'overview' ? <OverviewTab employee={item} profile={profile.data} canEdit={canEdit} /> : null}
          {tab === 'employment' ? (
            <EmploymentTab
              employeeId={employeeId}
              status={item.status}
              currentDepartmentId={profile.data.department?.id ?? null}
              currentPositionId={profile.data.position?.id ?? null}
              canEdit={canEdit}
              departments={departments.data?.items ?? []}
              positions={positions.data?.items ?? []}
            />
          ) : null}
          {tab === 'history' ? <EmploymentHistoryTab employeeId={employeeId} /> : null}
          {tab === 'contracts' ? <ContractsTab employeeId={employeeId} canEdit={canEdit} /> : null}
          {tab === 'compensation' && salaryAccess ? (
            <CompensationTab employeeId={employeeId} canEdit={canEdit} />
          ) : null}
          {tab === 'documents' ? (
            <DocumentsTab employeeId={employeeId} canEdit={canEdit} canUpload={canUploadDocuments} />
          ) : null}
          {tab === 'activity' ? <EmployeeActivityTab employeeId={employeeId} /> : null}
        </section>
      </div>
      <ConfirmDialog
        open={confirmArchive}
        title="Archive employee?"
        description="The employee will be removed from active lists. Historical and audit records will be preserved."
        confirmLabel="Archive employee"
        loading={archive.isPending}
        onClose={() => setConfirmArchive(false)}
        onConfirm={() => archive.mutate(item)}
      />
    </div>
  );
}

function OverviewTab({
  employee,
  profile,
  canEdit
}: {
  employee: Awaited<ReturnType<typeof getEmployee>>;
  profile: EmployeeProfile;
  canEdit: boolean;
}) {
  const [editing, setEditing] = useState(false);
  const { notify } = useNotification();
  const mutation = useMutation({
    mutationFn: (input: EmployeeInput) => updateEmployee(employee.id, employee.version, input),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: employeeKeys.all });
      setEditing(false);
      notify({ title: 'Profile updated', tone: 'success' });
    },
    onError: (error) => notify({ title: 'Update failed', message: toAppError(error).message, tone: 'error' })
  });
  return (
    <>
      <div className="detail-grid">
        <Card className="detail-card">
          <header>
            <div>
              <span className="detail-icon">
                <ShieldCheck size={18} />
              </span>
              <h2>Personal and contact</h2>
            </div>
            {canEdit ? (
              <Button variant="secondary" size="small" onClick={() => setEditing(true)}>
                <Pencil size={15} /> Edit
              </Button>
            ) : null}
          </header>
          <dl className="detail-list">
            <div>
              <dt>Full name</dt>
              <dd>
                {employee.firstName} {employee.lastName}
              </dd>
            </div>
            <div>
              <dt>Work email</dt>
              <dd>{employee.workEmail}</dd>
            </div>
            <div>
              <dt>Phone</dt>
              <dd>{employee.phone ?? 'Not provided'}</dd>
            </div>
            <div>
              <dt>Employee number</dt>
              <dd className="mono">{employee.employeeNumber}</dd>
            </div>
          </dl>
        </Card>
        <Card className="detail-card">
          <header>
            <div>
              <span className="detail-icon">
                <BriefcaseBusiness size={18} />
              </span>
              <h2>Organization</h2>
            </div>
          </header>
          <dl className="detail-list">
            <div>
              <dt>Structure</dt>
              <dd>{profile.organizationPath.map((unit) => unit.name).join(' / ') || 'Not assigned'}</dd>
            </div>
            <div>
              <dt>Position</dt>
              <dd>{profile.position?.name ?? 'Not assigned'}</dd>
            </div>
            <div>
              <dt>Employment status</dt>
              <dd>{profile.status ?? 'Not set'}</dd>
            </div>
            <div>
              <dt>Manager</dt>
              <dd>{profile.manager?.fullName ?? 'Not assigned'}</dd>
            </div>
          </dl>
        </Card>
      </div>
      <Dialog
        open={editing}
        onClose={() => setEditing(false)}
        title="Edit employee profile"
        description="Update contact and identity details."
      >
        <EmployeeForm
          employee={employee}
          loading={mutation.isPending}
          onSubmit={async (values) => {
            await mutation.mutateAsync(values);
          }}
        />
      </Dialog>
    </>
  );
}

function EmploymentTab({
  employeeId,
  status,
  currentDepartmentId,
  currentPositionId,
  canEdit,
  departments,
  positions
}: {
  employeeId: string;
  status: EmploymentStatus | null;
  currentDepartmentId: string | null;
  currentPositionId: string | null;
  canEdit: boolean;
  departments: Department[];
  positions: Position[];
}) {
  const { notify } = useNotification();
  const employment = useForm<{ status: EmploymentStatus; startDate: string; endDate: string }>({
    defaultValues: { status: status ?? 'Active', startDate: today(), endDate: '' }
  });
  const assignment = useForm<{ departmentId: string; positionId: string }>({
    defaultValues: { departmentId: currentDepartmentId ?? '', positionId: currentPositionId ?? '' }
  });
  const selectedDepartmentId = assignment.watch('departmentId');
  const availableDepartments = departments.filter((department) => department.type === 'Department');
  const availablePositions = positions.filter((position) => position.departmentId === selectedDepartmentId);
  const currentDepartment = availableDepartments.find((department) => department.id === currentDepartmentId) ?? null;
  const departmentManagerIsEmployee = currentDepartment?.managerEmployeeId === employeeId;
  useEffect(() => {
    assignment.reset({ departmentId: currentDepartmentId ?? '', positionId: currentPositionId ?? '' });
  }, [assignment, currentDepartmentId, currentPositionId]);
  const finish = async (message: string) => {
    await queryClient.invalidateQueries({ queryKey: employeeKeys.all });
    notify({ title: message, tone: 'success' });
  };
  const employmentMutation = useMutation({
    mutationFn: (input: { status: EmploymentStatus; startDate: string; endDate: string }) =>
      setEmployment(employeeId, { ...input, endDate: input.endDate || undefined }),
    onSuccess: () => finish('Employment updated'),
    onError: (error) => notify({ title: 'Update failed', message: toAppError(error).message, tone: 'error' })
  });
  const assignmentMutation = useMutation({
    mutationFn: (input: { departmentId: string; positionId: string }) =>
      addAssignment(employeeId, { ...input, effectiveFrom: today() }),
    onSuccess: () => finish('Position and department manager updated'),
    onError: (error) => notify({ title: 'Assignment failed', message: toAppError(error).message, tone: 'error' })
  });
  if (!canEdit)
    return (
      <Card>
        <EmptyState
          title="Employment details are read-only"
          description="Your role can view this employee, but only HR roles can change employment and assignments."
        />
      </Card>
    );
  return (
    <div className="employment-editor-grid">
      <div className="employment-editor-column">
        <Card className="form-card">
          <div className="section-heading">
            <div>
              <h2>Employment status</h2>
              <p>Record an effective-dated employment change.</p>
            </div>
          </div>
          <form className="form-stack" onSubmit={employment.handleSubmit((input) => employmentMutation.mutate(input))}>
            <FieldShell label="Status" htmlFor="status">
              <Select id="status" {...employment.register('status')}>
                <option>Active</option>
                <option>OnLeave</option>
                <option>Suspended</option>
                <option>Terminated</option>
              </Select>
            </FieldShell>
            <div className="form-grid">
              <FieldShell label="Effective from" htmlFor="startDate">
                <Input id="startDate" type="date" {...employment.register('startDate', { required: true })} />
              </FieldShell>
              <FieldShell label="Effective to" htmlFor="endDate">
                <Input id="endDate" type="date" {...employment.register('endDate')} />
              </FieldShell>
            </div>
            <Button type="submit" loading={employmentMutation.isPending}>
              Save status
            </Button>
          </form>
        </Card>
        <Card className="form-card">
          <div className="section-heading">
            <div>
              <h2>Manager assignment</h2>
              <p>The reporting manager is assigned automatically from the employee's department.</p>
            </div>
          </div>
          <div className="form-stack">
            <FieldShell label="Department" htmlFor="managerDepartment">
              <Input
                id="managerDepartment"
                value={currentDepartment?.name ?? 'Department not assigned'}
                disabled
                readOnly
              />
            </FieldShell>
            <FieldShell label="Department manager" htmlFor="departmentManager">
              <Input
                id="departmentManager"
                value={currentDepartment?.managerName ?? 'Manager not configured'}
                disabled
                readOnly
              />
            </FieldShell>
            <p className="form-hint">
              Change the department or position above to update this reporting relationship automatically.
            </p>
            {departmentManagerIsEmployee ? (
              <p className="form-hint">
                This employee leads the {currentDepartment?.name} department and cannot report to themselves.
              </p>
            ) : null}
          </div>
        </Card>
      </div>
      <Card className="form-card">
        <div className="section-heading">
          <div>
            <h2>Position assignment</h2>
            <p>Changing the position takes effect today and automatically applies the selected department manager.</p>
          </div>
        </div>
        <form className="form-stack" onSubmit={assignment.handleSubmit((input) => assignmentMutation.mutate(input))}>
          <FieldShell label="Department" htmlFor="departmentId">
            <Select
              id="departmentId"
              {...assignment.register('departmentId', {
                required: true,
                onChange: () => assignment.setValue('positionId', '')
              })}
            >
              <option value="">Select department</option>
              {availableDepartments.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.name}
                </option>
              ))}
            </Select>
          </FieldShell>
          <FieldShell
            label="Position"
            htmlFor="positionId"
            hint={
              selectedDepartmentId
                ? `${availablePositions.length} positions available in this department`
                : 'Select a department first'
            }
          >
            <Select
              id="positionId"
              disabled={!selectedDepartmentId}
              {...assignment.register('positionId', { required: true })}
            >
              <option value="">{selectedDepartmentId ? 'Select position' : 'Select department first'}</option>
              {availablePositions.map((item) => (
                <option key={item.id} value={item.id}>
                  {item.title}
                </option>
              ))}
            </Select>
          </FieldShell>
          <Button type="submit" loading={assignmentMutation.isPending}>
            Change position
          </Button>
        </form>
      </Card>
    </div>
  );
}

function ContractsTab({ employeeId, canEdit }: { employeeId: string; canEdit: boolean }) {
  const { notify } = useNotification();
  const [open, setOpen] = useState(false);
  const form = useForm<{ contractNumber: string; startDate: string; endDate: string; notes: string }>({
    defaultValues: { contractNumber: '', startDate: today(), endDate: '', notes: '' }
  });
  const query = useEmployeeContracts(employeeId);
  const refresh = async () => queryClient.invalidateQueries({ queryKey: ['contracts', employeeId] });
  const create = useMutation({
    mutationFn: (input: { contractNumber: string; startDate: string; endDate: string; notes: string }) =>
      createContract(employeeId, { ...input, endDate: input.endDate || undefined, notes: input.notes || undefined }),
    onSuccess: async () => {
      await refresh();
      setOpen(false);
      form.reset();
      notify({ title: 'Contract created', tone: 'success' });
    },
    onError: (error) => notify({ title: 'Contract failed', message: toAppError(error).message, tone: 'error' })
  });
  const activate = useMutation({
    mutationFn: (contract: Contract) => activateContract(employeeId, contract),
    onSuccess: refresh
  });
  const terminate = useMutation({
    mutationFn: (contract: Contract) => terminateContract(employeeId, contract, today()),
    onSuccess: refresh
  });
  return (
    <>
      <Card className="table-card">
        <header className="section-heading section-heading--padded">
          <div>
            <h2>Contracts</h2>
            <p>Structured contract records and effective dates.</p>
          </div>
          {canEdit ? (
            <Button size="small" onClick={() => setOpen(true)}>
              <Plus size={15} /> New contract
            </Button>
          ) : null}
        </header>
        {query.data?.length ? (
          <div className="data-table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Contract</th>
                  <th>Status</th>
                  <th>Dates</th>
                  <th>Revision</th>
                  <th>Actions</th>
                </tr>
              </thead>
              <tbody>
                {query.data.map((contract) => (
                  <tr key={contract.id}>
                    <td className="mono">{contract.contractNumber}</td>
                    <td>
                      <Badge
                        tone={
                          contract.status === 'Active'
                            ? 'positive'
                            : contract.status === 'Terminated'
                              ? 'negative'
                              : 'neutral'
                        }
                      >
                        {contract.status}
                      </Badge>
                    </td>
                    <td>
                      {formatDate(contract.startDate)} — {formatDate(contract.endDate)}
                    </td>
                    <td>v{contract.revision}</td>
                    <td>
                      <div className="row-actions">
                        {canEdit && contract.status === 'Draft' ? (
                          <Button variant="secondary" size="small" onClick={() => activate.mutate(contract)}>
                            Activate
                          </Button>
                        ) : null}
                        {canEdit && contract.status === 'Active' ? (
                          <Button variant="ghost" size="small" onClick={() => terminate.mutate(contract)}>
                            Terminate today
                          </Button>
                        ) : null}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState
            title="No contracts recorded"
            description="Create a structured contract record to track dates and status."
            actionLabel={canEdit ? 'New contract' : undefined}
            onAction={canEdit ? () => setOpen(true) : undefined}
          />
        )}
      </Card>
      <Dialog
        open={open}
        onClose={() => setOpen(false)}
        title="Create contract"
        description="Contract dates are stored as structured data and drive expiration logic."
      >
        <form className="form-stack" onSubmit={form.handleSubmit((input) => create.mutate(input))}>
          <FieldShell label="Contract number" htmlFor="contractNumber">
            <Input id="contractNumber" {...form.register('contractNumber', { required: true })} />
          </FieldShell>
          <div className="form-grid">
            <FieldShell label="Start date" htmlFor="contractStart">
              <Input id="contractStart" type="date" {...form.register('startDate')} />
            </FieldShell>
            <FieldShell label="End date" htmlFor="contractEnd">
              <Input id="contractEnd" type="date" {...form.register('endDate')} />
            </FieldShell>
          </div>
          <FieldShell label="Notes" htmlFor="contractNotes">
            <Textarea id="contractNotes" {...form.register('notes')} />
          </FieldShell>
          <Button type="submit" loading={create.isPending}>
            Create contract
          </Button>
        </form>
      </Dialog>
    </>
  );
}

function CompensationTab({ employeeId, canEdit }: { employeeId: string; canEdit: boolean }) {
  const { notify } = useNotification();
  const [open, setOpen] = useState(false);
  const form = useForm<{
    amount: number;
    currency: string;
    frequency: string;
    effectiveFrom: string;
    effectiveTo: string;
  }>({ defaultValues: { amount: 0, currency: 'EUR', frequency: 'Monthly', effectiveFrom: today(), effectiveTo: '' } });
  const query = useEmployeeCompensation(employeeId);
  const mutation = useMutation({
    mutationFn: (input: {
      amount: number;
      currency: string;
      frequency: string;
      effectiveFrom: string;
      effectiveTo: string;
    }) =>
      addCompensation(employeeId, {
        ...input,
        amount: Number(input.amount),
        effectiveTo: input.effectiveTo || undefined
      }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['compensation', employeeId] });
      setOpen(false);
      notify({ title: 'Compensation added', tone: 'success' });
    },
    onError: (error) =>
      notify({ title: 'Could not add compensation', message: toAppError(error).message, tone: 'error' })
  });
  return (
    <>
      <Card className="table-card">
        <header className="section-heading section-heading--padded">
          <div>
            <h2>Compensation history</h2>
            <p>Sensitive salary information is visible only to authorized roles.</p>
          </div>
          {canEdit ? (
            <Button size="small" onClick={() => setOpen(true)}>
              <Plus size={15} /> Add record
            </Button>
          ) : null}
        </header>
        {query.data?.length ? (
          <div className="data-table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>Amount</th>
                  <th>Frequency</th>
                  <th>Effective period</th>
                </tr>
              </thead>
              <tbody>
                {query.data.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>
                        {new Intl.NumberFormat(undefined, { style: 'currency', currency: item.currency }).format(
                          item.amount
                        )}
                      </strong>
                    </td>
                    <td>{item.frequency}</td>
                    <td>
                      {formatDate(item.effectiveFrom)} — {formatDate(item.effectiveTo)}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState
            title="No compensation history"
            description="Compensation records are separate from contract details."
          />
        )}
      </Card>
      <Dialog open={open} onClose={() => setOpen(false)} title="Add compensation record">
        <form className="form-stack" onSubmit={form.handleSubmit((input) => mutation.mutate(input))}>
          <div className="form-grid">
            <FieldShell label="Amount" htmlFor="amount">
              <Input
                id="amount"
                type="number"
                step="0.01"
                {...form.register('amount', { valueAsNumber: true, min: 0 })}
              />
            </FieldShell>
            <FieldShell label="Currency" htmlFor="currency">
              <Input id="currency" maxLength={3} {...form.register('currency')} />
            </FieldShell>
            <FieldShell label="Frequency" htmlFor="frequency">
              <Select id="frequency" {...form.register('frequency')}>
                <option>Monthly</option>
                <option>Annual</option>
                <option>Hourly</option>
              </Select>
            </FieldShell>
            <FieldShell label="Effective from" htmlFor="compFrom">
              <Input id="compFrom" type="date" {...form.register('effectiveFrom')} />
            </FieldShell>
            <FieldShell label="Effective to" htmlFor="compTo">
              <Input id="compTo" type="date" {...form.register('effectiveTo')} />
            </FieldShell>
          </div>
          <Button type="submit" loading={mutation.isPending}>
            Add compensation
          </Button>
        </form>
      </Dialog>
    </>
  );
}

function DocumentsTab({
  employeeId,
  canEdit,
  canUpload
}: {
  employeeId: string;
  canEdit: boolean;
  canUpload: boolean;
}) {
  const { notify } = useNotification();
  const [selectedFolderId, setSelectedFolderId] = useState<string | null>(null);
  const [search, setSearch] = useState('');
  const [uploadOpen, setUploadOpen] = useState(false);
  const [preview, setPreview] = useState<EmployeeDocument | null>(null);
  const query = useEmployeeDocuments(employeeId);
  const folders = useDocumentFolders();
  const documents = query.data ?? [];
  const availableFolders = (folders.data ?? []).filter(
    (folder) => folder.scope === 'Global' || folder.employeeId === employeeId
  );
  const hasEmployeeUploads = availableFolders.some((folder) => folder.name.toLowerCase() === 'employee uploads');
  const folderCards = [
    ...availableFolders.map((folder) => ({
      id: folder.id,
      folderId: folder.id,
      name: folder.name,
      description: folder.description,
      includesUnfiled: folder.name.toLowerCase() === 'employee uploads'
    })),
    ...(!hasEmployeeUploads && documents.some((document) => !document.folderId)
      ? [
          {
            id: 'unfiled',
            folderId: null,
            name: 'Employee uploads',
            description: 'Documents not assigned to a specific folder.',
            includesUnfiled: true
          }
        ]
      : [])
  ].map((folder) => ({
    ...folder,
    count: documents.filter(
      (document) => document.folderId === folder.folderId || (folder.includesUnfiled && !document.folderId)
    ).length
  }));
  const selectedFolder = folderCards.find((folder) => folder.id === selectedFolderId) ?? null;
  const term = search.trim().toLowerCase();
  const visibleDocuments = documents.filter((document) => {
    const inFolder =
      !selectedFolder ||
      document.folderId === selectedFolder.folderId ||
      (selectedFolder.includesUnfiled && !document.folderId);
    return inFolder && (!term || `${document.displayName} ${document.documentType}`.toLowerCase().includes(term));
  });
  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: documentKeys.employee(employeeId) }),
      queryClient.invalidateQueries({ queryKey: documentKeys.folders }),
      queryClient.invalidateQueries({ queryKey: ['documents', 'workspace'] })
    ]);
  };
  const archive = useMutation({
    mutationFn: archiveDocument.bind(null, employeeId),
    onSuccess: async () => {
      await refresh();
      notify({ title: 'Document archived', tone: 'success' });
    },
    onError: (error) => notify({ title: 'Archive failed', message: toAppError(error).message, tone: 'error' })
  });
  const showFiles = Boolean(selectedFolder || term);

  return (
    <div className="employee-documents">
      <header className="employee-documents__header">
        <div>
          <span className="detail-icon">
            <FileText size={18} />
          </span>
          <div>
            <h2>Documents</h2>
            <p>Secure employee files stored and scanned on your local infrastructure.</p>
          </div>
        </div>
        {canUpload ? (
          <Button size="small" onClick={() => setUploadOpen(true)}>
            <Upload size={15} /> Upload
          </Button>
        ) : null}
      </header>
      <div className="employee-documents__toolbar">
        <SearchInput
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          placeholder="Search documents"
          aria-label="Search employee documents"
        />
        {selectedFolder ? (
          <Button
            variant="ghost"
            size="small"
            onClick={() => {
              setSelectedFolderId(null);
              setSearch('');
            }}
          >
            <ArrowLeft size={15} /> All folders
          </Button>
        ) : null}
      </div>
      {!showFiles ? (
        query.isLoading || folders.isLoading ? (
          <Card>
            <Skeleton count={5} />
          </Card>
        ) : query.isError || folders.isError ? (
          <Card>
            <ErrorState
              title="Documents could not be loaded"
              description="Try loading this employee's documents again."
              onRetry={() => {
                void query.refetch();
                void folders.refetch();
              }}
            />
          </Card>
        ) : folderCards.length ? (
          <div className="employee-folder-grid">
            {folderCards.map((folder) => (
              <button
                type="button"
                className="employee-folder-card"
                key={folder.id}
                onClick={() => setSelectedFolderId(folder.id)}
              >
                <span>
                  <Folder size={25} />
                </span>
                <strong>{folder.name}</strong>
                <small>
                  {folder.count} {folder.count === 1 ? 'item' : 'items'}
                </small>
              </button>
            ))}
          </div>
        ) : (
          <Card>
            <EmptyState
              title="No document folders available"
              description="Folders configured for this employee will appear here."
            />
          </Card>
        )
      ) : (
        <Card className="employee-document-browser">
          <header>
            <button
              type="button"
              onClick={() => {
                setSelectedFolderId(null);
                setSearch('');
              }}
            >
              <ArrowLeft size={15} /> All folders
            </button>
            <div>
              <FolderOpen size={20} />
              <strong>{selectedFolder?.name ?? 'Search results'}</strong>
            </div>
            <Badge tone="info">{visibleDocuments.length} files</Badge>
          </header>
          {visibleDocuments.length ? (
            <div className="employee-document-grid">
              {visibleDocuments.map((document) => (
                <article className="employee-document-card" key={document.id}>
                  <button
                    className="employee-document-card__open"
                    type="button"
                    disabled={document.status !== 'Clean'}
                    onClick={() => setPreview(document)}
                  >
                    <span>
                      <FileText size={25} />
                    </span>
                    <strong title={document.displayName}>{document.displayName}</strong>
                    <small>
                      {document.documentType} · Version {document.currentVersion}
                    </small>
                  </button>
                  <div>
                    <Badge
                      tone={
                        document.status === 'Clean'
                          ? 'positive'
                          : document.status === 'Rejected'
                            ? 'negative'
                            : 'warning'
                      }
                    >
                      {document.status === 'PendingScan' ? 'Scanning' : document.status}
                    </Badge>
                    <span className="row-actions">
                      <Button
                        variant="ghost"
                        size="small"
                        disabled={document.status !== 'Clean'}
                        onClick={() => void downloadDocument(employeeId, document)}
                        aria-label={`Download ${document.displayName}`}
                      >
                        <Download size={15} />
                      </Button>
                      {canEdit ? (
                        <Button
                          variant="ghost"
                          size="small"
                          loading={archive.isPending}
                          onClick={() => archive.mutate(document)}
                          aria-label={`Archive ${document.displayName}`}
                        >
                          <Trash2 size={15} />
                        </Button>
                      ) : null}
                    </span>
                  </div>
                </article>
              ))}
            </div>
          ) : (
            <EmptyState
              title={term ? 'No matching documents' : 'This folder is empty'}
              description={
                term ? 'Try a different document name or type.' : 'Upload a document into this folder to get started.'
              }
              actionLabel={canUpload ? 'Upload document' : undefined}
              onAction={canUpload ? () => setUploadOpen(true) : undefined}
            />
          )}
        </Card>
      )}
      <EmployeeDocumentUploadDialog
        key={selectedFolder?.id ?? 'all-folders'}
        open={uploadOpen}
        employeeId={employeeId}
        folders={folderCards
          .filter((folder) => folder.folderId)
          .map((folder) => ({ id: folder.folderId as string, name: folder.name }))}
        initialFolderId={selectedFolder?.folderId ?? ''}
        onClose={() => setUploadOpen(false)}
        onUploaded={async () => {
          await refresh();
          setUploadOpen(false);
          notify({ title: 'Document queued for scanning', tone: 'success' });
        }}
      />
      <DocumentPreviewDialog employeeId={employeeId} document={preview} onClose={() => setPreview(null)} />
    </div>
  );
}

function EmployeeDocumentUploadDialog({
  open,
  employeeId,
  folders,
  initialFolderId,
  onClose,
  onUploaded
}: {
  open: boolean;
  employeeId: string;
  folders: { id: string; name: string }[];
  initialFolderId: string;
  onClose: () => void;
  onUploaded: () => Promise<void>;
}) {
  const fileRef = useRef<HTMLInputElement>(null);
  const [folderId, setFolderId] = useState(initialFolderId);
  const [documentType, setDocumentType] = useState('Other');
  const [file, setFile] = useState<File | null>(null);
  const mutation = useMutation({
    mutationFn: () => uploadDocument(employeeId, file as File, documentType, folderId || undefined),
    onSuccess: onUploaded
  });
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Upload employee document"
      description="The file is quarantined and scanned locally before it becomes available."
    >
      <form
        className="form-stack"
        onSubmit={(event) => {
          event.preventDefault();
          if (file) mutation.mutate();
        }}
      >
        <FieldShell label="Folder" htmlFor="employeeDocumentFolder">
          <Select id="employeeDocumentFolder" value={folderId} onChange={(event) => setFolderId(event.target.value)}>
            <option value="">Employee uploads</option>
            {folders.map((folder) => (
              <option key={folder.id} value={folder.id}>
                {folder.name}
              </option>
            ))}
          </Select>
        </FieldShell>
        <FieldShell label="Document type" htmlFor="employeeDocumentType" required>
          <Select
            id="employeeDocumentType"
            value={documentType}
            onChange={(event) => setDocumentType(event.target.value)}
          >
            <option>Contract</option>
            <option>Identity</option>
            <option>Certificate</option>
            <option>Signed document</option>
            <option>Other</option>
          </Select>
        </FieldShell>
        <FieldShell
          label="File"
          htmlFor="employeeDocumentFile"
          required
          hint={file ? `${file.name} · ${(file.size / 1024).toFixed(1)} KB` : 'PDF or DOCX'}
        >
          <Input
            ref={fileRef}
            id="employeeDocumentFile"
            type="file"
            accept=".pdf,.docx,application/pdf,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
          />
        </FieldShell>
        {mutation.isError ? <p className="form-alert">{toAppError(mutation.error).message}</p> : null}
        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" disabled={!file} loading={mutation.isPending}>
            Upload and scan
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
