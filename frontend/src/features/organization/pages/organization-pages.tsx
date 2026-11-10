import { useMutation, useQuery } from '@tanstack/react-query';
import { Archive, BriefcaseBusiness, Building2, Mail, Pencil, Plus, ShieldCheck, Trash2, UserCog, Users } from 'lucide-react';
import { useMemo, useState, type ReactNode } from 'react';
import { useForm } from 'react-hook-form';
import { Link } from 'react-router';

import { canManageOrganization } from '@/app/access';
import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { FieldShell, Input, Select, Textarea } from '@/components/forms/field';
import { SearchInput } from '@/components/forms/search-input';
import { PageHeader } from '@/components/navigation/page-header';
import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { Dialog } from '@/components/ui/dialog';
import { DropdownMenu } from '@/components/ui/dropdown-menu';
import { useAuth } from '@/features/auth/hooks/use-auth';
import { useEmployees } from '@/features/employees/hooks/use-employees';
import { toAppError } from '@/lib/api-error';
import type { PageResult } from '@/types/api';

import { archivePosition, assignDepartmentEmployee, createDepartment, createPosition, getCurrentDepartment, getDepartments, getPositions, removeDepartmentEmployee, updateDepartment, updatePosition } from '../api/organization-api';
import { PeopleWorkspaceTabs } from '../components/people-workspace-tabs';
import type { Department, DepartmentInput, DepartmentOverview, Position, PositionInput } from '../types';

const currentDate = () => new Date().toISOString().slice(0, 10);

export function DepartmentsPage() {
  const { user } = useAuth();
  const canEdit = canManageOrganization(user);
  const { notify } = useNotification();
  const [search, setSearch] = useState('');
  const [editorOpen, setEditorOpen] = useState(false);
  const [assignmentOpen, setAssignmentOpen] = useState(false);
  const [selectedDepartmentId, setSelectedDepartmentId] = useState('');
  const [pendingRemoval, setPendingRemoval] = useState<DepartmentOverview['employees'][number] | null>(null);
  const departmentList = useQuery({ queryKey: ['departments', 'workspace'], queryFn: () => getDepartments(undefined, 100) });
  const departmentOptions = departmentList.data?.items.filter((item) => item.type === 'Department') ?? [];
  const query = useQuery({ queryKey: ['departments', 'current', selectedDepartmentId], queryFn: () => getCurrentDepartment(selectedDepartmentId || undefined), retry: false });
  const employees = useEmployees({ limit: 100 });
  const positions = useQuery({ queryKey: ['positions', { limit: 100 }], queryFn: () => getPositions(undefined, 100) });
  const overview = query.data;
  const department = overview?.department;
  const visibleEmployees = useMemo(() => {
    const term = search.trim().toLowerCase();
    if (!term) return overview?.employees ?? [];
    return (overview?.employees ?? []).filter((employee) =>
      [employee.fullName, employee.employeeNumber, employee.workEmail, employee.position, ...employee.projects]
        .some((value) => value?.toLowerCase().includes(term)));
  }, [overview?.employees, search]);

  const refresh = async () => {
    await Promise.all([
      queryClient.invalidateQueries({ queryKey: ['departments'] }),
      queryClient.invalidateQueries({ queryKey: ['employees'] }),
    ]);
  };
  const save = useMutation({
    mutationFn: (input: DepartmentInput) => department ? updateDepartment(department, input) : createDepartment(input),
    onSuccess: async () => { await refresh(); setEditorOpen(false); notify({ title: 'Department saved', tone: 'success' }); },
    onError: (error) => notify({ title: 'Could not save department', message: toAppError(error).message, tone: 'error' }),
  });
  const assign = useMutation({
    mutationFn: (input: { employeeId: string; positionId: string; effectiveFrom: string }) => assignDepartmentEmployee(department!.id, input.employeeId, input),
    onSuccess: async () => { await refresh(); setAssignmentOpen(false); notify({ title: 'Department assignment updated', tone: 'success' }); },
    onError: (error) => notify({ title: 'Could not assign employee', message: toAppError(error).message, tone: 'error' }),
  });
  const remove = useMutation({
    mutationFn: (employeeId: string) => removeDepartmentEmployee(department!.id, employeeId, currentDate()),
    onSuccess: async () => { await refresh(); setPendingRemoval(null); notify({ title: 'Employee removed from department', tone: 'success' }); },
    onError: (error) => notify({ title: 'Could not remove employee', message: toAppError(error).message, tone: 'error' }),
  });

  if (query.isLoading || departmentList.isLoading) return <DepartmentLoading />;
  const queryError = query.isError ? toAppError(query.error) : null;
  if (!overview && queryError?.status !== 404) return <div className="page-stack"><PageHeader eyebrow="Organization" title="Departments & teams" description="Your IT and HR departments, leadership, and employees."><PeopleWorkspaceTabs /></PageHeader><Card><ErrorState title="Department could not be loaded" description={queryError?.message} onRetry={() => void query.refetch()} /></Card></div>;
  if (!overview || !department) return <div className="page-stack"><PageHeader eyebrow="Organization" title="Departments & teams" description="Configure the IT and HR departments."><PeopleWorkspaceTabs /></PageHeader><Card><EmptyState title="No department configured" description="Create the IT department first; a second HR department can then be added." actionLabel={canEdit ? 'Create department' : undefined} onAction={canEdit ? () => setEditorOpen(true) : undefined} /></Card><DepartmentEditor open={editorOpen} item={null} employees={employees.data?.items ?? []} loading={save.isPending} onClose={() => setEditorOpen(false)} onSave={(input) => save.mutateAsync(input)} /></div>;

  const leadershipIds = new Set([department.managerEmployeeId, department.hrEmployeeId, department.productManagerEmployeeId].filter(Boolean));
  return <div className="page-stack department-page">
    <PageHeader eyebrow="Organization" title="Departments & teams" description="Switch between IT and HR to manage their positions, leadership, and employees." actions={<><div className="row-actions" aria-label="Choose department">{departmentOptions.map((option) => <Button key={option.id} size="small" variant={option.id === department.id ? 'primary' : 'secondary'} onClick={() => { setSelectedDepartmentId(option.id); setSearch(''); }}>{option.name}</Button>)}</div>{canEdit ? <><Button variant="secondary" onClick={() => setEditorOpen(true)}><Pencil size={16} /> Edit department</Button><Button onClick={() => setAssignmentOpen(true)}><Plus size={16} /> Add employee</Button></> : null}</>}><PeopleWorkspaceTabs /></PageHeader>

    <Card className="department-overview-card"><div className="department-overview-card__identity"><span><Building2 size={24} /></span><div><div className="department-title-line"><h2>{department.name}</h2><Badge tone="positive">Active</Badge></div><p>{department.description ?? 'No department description has been added yet.'}</p></div></div><div className="department-overview-card__metric"><strong>{overview.employees.length}</strong><span>Total employees</span></div></Card>

    <section className="department-section"><div className="section-heading"><div><span className="eyebrow">Department leadership</span><h2>People responsible for the department</h2><p>Each responsibility has one accountable employee.</p></div></div><div className="leadership-grid"><LeadershipCard role="Manager" name={department.managerName} icon={<Users size={19} />} /><LeadershipCard role="HR" name={department.hrName} icon={<ShieldCheck size={19} />} /><LeadershipCard role="Product Manager" name={department.productManagerName} icon={<BriefcaseBusiness size={19} />} /></div></section>

    <section className="department-section"><div className="section-heading department-employees-heading"><div><span className="eyebrow">Department employees</span><h2>Team directory</h2><p>{overview.employees.length} people currently assigned.</p></div><SearchInput value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search employees" aria-label="Search department employees" /></div><Card className="department-employee-card">{visibleEmployees.length ? <div className="data-table-wrap"><table className="data-table department-employee-table"><thead><tr><th>Employee</th><th>Position</th><th>Status</th><th>Assigned project</th>{canEdit ? <th><span className="sr-only">Actions</span></th> : null}</tr></thead><tbody>{visibleEmployees.map((employee) => <tr key={employee.id}><td><Link className="department-employee" to={`/employees/${employee.id}`}><Avatar name={employee.fullName} size="small" /><span><strong>{employee.fullName}</strong><small><Mail size={12} /> {employee.workEmail}</small></span></Link></td><td>{employee.position ?? <span className="muted">Not assigned</span>}</td><td><Badge tone={employee.employmentStatus === 'Active' ? 'positive' : employee.employmentStatus === 'Terminated' ? 'negative' : 'warning'}>{employee.employmentStatus ?? 'Not set'}</Badge></td><td>{employee.projects.length ? employee.projects.join(', ') : <span className="muted">No active project</span>}</td>{canEdit ? <td className="row-actions"><Button variant="ghost" size="small" disabled={leadershipIds.has(employee.id)} title={leadershipIds.has(employee.id) ? 'Replace this leadership assignment before removing the employee.' : 'Remove from department'} onClick={() => setPendingRemoval(employee)}><Trash2 size={15} /> Remove</Button></td> : null}</tr>)}</tbody></table></div> : <EmptyState title="No employees found" description={search ? 'Try a different search.' : 'Assign the first employee to this department.'} actionLabel={canEdit && !search ? 'Add employee' : undefined} onAction={canEdit ? () => setAssignmentOpen(true) : undefined} />}</Card></section>

    <DepartmentEditor open={editorOpen} item={department} employees={employees.data?.items ?? []} loading={save.isPending} onClose={() => setEditorOpen(false)} onSave={(input) => save.mutateAsync(input)} />
    <AssignmentDialog open={assignmentOpen} department={department} currentEmployeeIds={new Set(overview.employees.map((employee) => employee.id))} employees={employees.data?.items ?? []} positions={positions.data?.items ?? []} loading={assign.isPending} onClose={() => setAssignmentOpen(false)} onSave={(input) => assign.mutateAsync(input)} />
    <ConfirmDialog open={Boolean(pendingRemoval)} title="Remove employee from department?" description={`${pendingRemoval?.fullName ?? 'This employee'}'s current department assignment will end immediately. Historical assignment records will be preserved.`} confirmLabel="Remove employee" loading={remove.isPending} onClose={() => setPendingRemoval(null)} onConfirm={() => { if (pendingRemoval) remove.mutate(pendingRemoval.id); }} />
  </div>;
}

function DepartmentLoading() {
  return <div className="page-stack"><PageHeader eyebrow="Organization" title="Departments & teams" description="Your IT and HR departments, leadership, and employees."><PeopleWorkspaceTabs /></PageHeader><Card className="skeleton-table"><Skeleton count={8} /></Card></div>;
}

function LeadershipCard({ role, name, icon }: { role: string; name: string | null; icon: ReactNode }) {
  return <Card className={`leadership-card ${name ? '' : 'leadership-card--empty'}`}><span className="leadership-card__icon">{icon}</span>{name ? <Avatar name={name} size="medium" /> : <span className="leadership-card__placeholder"><UserCog size={18} /></span>}<div><small>{role}</small><strong>{name ?? 'Not assigned'}</strong></div></Card>;
}

function DepartmentEditor({ open, item, employees, loading, onClose, onSave }: { open: boolean; item: Department | null; employees: { id: string; firstName: string; lastName: string }[]; loading: boolean; onClose: () => void; onSave: (input: DepartmentInput) => Promise<unknown> }) {
  const form = useForm<{ name: string; description: string; managerEmployeeId: string; hrEmployeeId: string; productManagerEmployeeId: string }>({ values: { name: item?.name ?? '', description: item?.description ?? '', managerEmployeeId: item?.managerEmployeeId ?? '', hrEmployeeId: item?.hrEmployeeId ?? '', productManagerEmployeeId: item?.productManagerEmployeeId ?? '' } });
  const submit = form.handleSubmit(async (values) => {
    const leaders = [values.managerEmployeeId, values.hrEmployeeId, values.productManagerEmployeeId];
    if (new Set(leaders).size !== leaders.length) { form.setError('managerEmployeeId', { message: 'Choose a different employee for each leadership responsibility.' }); return; }
    await onSave({ name: values.name, description: values.description || undefined, code: item?.code ?? undefined, type: 'Department', parentDepartmentId: item?.parentDepartmentId ?? undefined, managerEmployeeId: values.managerEmployeeId, hrEmployeeId: values.hrEmployeeId, productManagerEmployeeId: values.productManagerEmployeeId });
  });
  const employeeOptions = (placeholder: string) => <><option value="">{placeholder}</option>{employees.map((employee) => <option key={employee.id} value={employee.id}>{employee.firstName} {employee.lastName}</option>)}</>;
  return <Dialog open={open} onClose={onClose} title={item ? 'Edit department' : 'Create department'} description="Maintain the department details and assign one person to each leadership responsibility." width="large"><form className="form-stack" onSubmit={submit}><FieldShell label="Department name" htmlFor="departmentName" required error={form.formState.errors.name?.message}><Input id="departmentName" autoFocus {...form.register('name', { required: 'Department name is required.' })} /></FieldShell><FieldShell label="Description" htmlFor="departmentDescription" required><Textarea id="departmentDescription" rows={4} maxLength={500} {...form.register('description', { required: true })} /></FieldShell><div className="leadership-form-grid"><FieldShell label="Manager" htmlFor="departmentManager" required error={form.formState.errors.managerEmployeeId?.message}><Select id="departmentManager" {...form.register('managerEmployeeId', { required: true })}>{employeeOptions('Choose manager')}</Select></FieldShell><FieldShell label="HR" htmlFor="departmentHr" required><Select id="departmentHr" {...form.register('hrEmployeeId', { required: true })}>{employeeOptions('Choose HR')}</Select></FieldShell><FieldShell label="Product Manager" htmlFor="departmentProductManager" required><Select id="departmentProductManager" {...form.register('productManagerEmployeeId', { required: true })}>{employeeOptions('Choose product manager')}</Select></FieldShell></div><div className="form-actions"><Button type="button" variant="secondary" onClick={onClose}>Cancel</Button><Button type="submit" loading={loading}>Save department</Button></div></form></Dialog>;
}

function AssignmentDialog({ open, department, currentEmployeeIds, employees, positions, loading, onClose, onSave }: { open: boolean; department: Department; currentEmployeeIds: Set<string>; employees: { id: string; firstName: string; lastName: string; departmentId: string | null }[]; positions: Position[]; loading: boolean; onClose: () => void; onSave: (input: { employeeId: string; positionId: string; effectiveFrom: string }) => Promise<unknown> }) {
  const candidates = employees.filter((employee) => !currentEmployeeIds.has(employee.id));
  const departmentPositions = positions.filter((position) => position.departmentId === department.id);
  const form = useForm<{ employeeId: string; positionId: string; effectiveFrom: string }>({ values: { employeeId: '', positionId: '', effectiveFrom: currentDate() } });
  return <Dialog open={open} onClose={onClose} title="Add employee to department" description={`Assign an existing employee to ${department.name}. If they belong to another unit, their current assignment will end the day before this transfer.`}><form className="form-stack" onSubmit={form.handleSubmit(onSave)}>{candidates.length ? <><FieldShell label="Employee" htmlFor="departmentEmployee" required><Select id="departmentEmployee" {...form.register('employeeId', { required: true })}><option value="">Choose employee</option>{candidates.map((employee) => <option key={employee.id} value={employee.id}>{employee.firstName} {employee.lastName}{employee.departmentId ? ' · transfer' : ''}</option>)}</Select></FieldShell><FieldShell label="Position" htmlFor="departmentPosition" required hint={`${departmentPositions.length} positions available in ${department.name}`}><Select id="departmentPosition" {...form.register('positionId', { required: true })}><option value="">Choose position</option>{departmentPositions.map((position) => <option key={position.id} value={position.id}>{position.title}</option>)}</Select></FieldShell><FieldShell label="Effective from" htmlFor="departmentEffectiveFrom" required><Input id="departmentEffectiveFrom" type="date" {...form.register('effectiveFrom', { required: true })} /></FieldShell></> : <p className="dialog-empty-copy">Every accessible employee is already assigned to this department.</p>}<div className="form-actions"><Button type="button" variant="secondary" onClick={onClose}>Cancel</Button><Button type="submit" loading={loading} disabled={!candidates.length || !departmentPositions.length}>Assign employee</Button></div></form></Dialog>;
}

export function PositionsPage() {
  const { user } = useAuth();
  const canEdit = canManageOrganization(user);
  const { notify } = useNotification();
  const [editing, setEditing] = useState<Position | 'new' | null>(null);
  const [pendingArchive, setPendingArchive] = useState<Position | null>(null);
  const query = useQuery<PageResult<Position>>({ queryKey: ['positions'], queryFn: () => getPositions(undefined, 100) });
  const departments = useQuery({ queryKey: ['departments', 'position-options'], queryFn: () => getDepartments(undefined, 100) });
  const departmentOptions = departments.data?.items.filter((department) => department.type === 'Department') ?? [];
  const departmentNames = new Map(departmentOptions.map((department) => [department.id, department.name]));
  const save = useMutation({ mutationFn: (input: PositionInput) => editing === 'new' ? createPosition(input) : updatePosition(editing as Position, input), onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['positions'] }); setEditing(null); notify({ title: 'Position saved', tone: 'success' }); }, onError: (error) => notify({ title: 'Could not save position', message: toAppError(error).message, tone: 'error' }) });
  const archive = useMutation({ mutationFn: archivePosition, onSuccess: async () => { await queryClient.invalidateQueries({ queryKey: ['positions'] }); setPendingArchive(null); notify({ title: 'Position archived', tone: 'success' }); } });
  return <div className="page-stack">
    <PageHeader eyebrow="Organization" title="Positions" description="Maintain the position catalog used in effective-dated assignments." actions={canEdit ? <Button onClick={() => setEditing('new')}><Plus size={17} /> Add position</Button> : undefined}><PeopleWorkspaceTabs /></PageHeader>
    <Card className="table-card">{query.isLoading || departments.isLoading ? <div className="skeleton-table"><Skeleton count={6} /></div> : query.isError || departments.isError ? <ErrorState onRetry={() => { void query.refetch(); void departments.refetch(); }} /> : query.data?.items.length ? <div className="data-table-wrap"><table className="data-table"><thead><tr><th>Position</th><th>Department</th><th>Code</th><th><span className="sr-only">Actions</span></th></tr></thead><tbody>{query.data.items.map((position) => <tr key={position.id}><td><div className="organization-name"><span><UserCog size={17} /></span><strong>{position.title}</strong></div></td><td>{position.departmentId ? departmentNames.get(position.departmentId) ?? 'Unknown department' : <span className="muted">Legacy / unassigned</span>}</td><td className="mono">{position.code ?? '—'}</td><td><DropdownMenu label={`Actions for ${position.title}`} items={[{ label: 'Edit position', icon: Pencil, onSelect: () => setEditing(position) }, { label: 'Archive position', icon: Archive, danger: true, separatorBefore: true, onSelect: () => setPendingArchive(position) }]} /></td></tr>)}</tbody></table></div> : <EmptyState title="No positions yet" description="Create the first position when your organization structure is ready." actionLabel={canEdit ? 'Add position' : undefined} onAction={canEdit ? () => setEditing('new') : undefined} />}</Card>
    <PositionDialog item={editing} departments={departmentOptions} loading={save.isPending} onClose={() => setEditing(null)} onSave={(input) => save.mutateAsync(input)} />
    <ConfirmDialog open={Boolean(pendingArchive)} title="Archive position?" description="The position will no longer appear in active lists. Historical assignments are preserved." confirmLabel="Archive position" loading={archive.isPending} onClose={() => setPendingArchive(null)} onConfirm={() => { if (pendingArchive) archive.mutate(pendingArchive); }} />
  </div>;
}

function PositionDialog({ item, departments, loading, onClose, onSave }: { item: Position | 'new' | null; departments: Department[]; loading: boolean; onClose: () => void; onSave: (input: PositionInput) => Promise<unknown> }) {
  const form = useForm<{ departmentId: string; title: string; code: string }>({ values: { departmentId: item && item !== 'new' ? item.departmentId ?? '' : '', title: item && item !== 'new' ? item.title : '', code: item && item !== 'new' ? item.code ?? '' : '' } });
  return <Dialog open={Boolean(item)} onClose={onClose} title={`${item === 'new' ? 'Add' : 'Edit'} position`} description="Every position belongs to either the IT or HR department."><form className="form-stack" onSubmit={form.handleSubmit((values) => onSave({ departmentId: values.departmentId, title: values.title, code: values.code || undefined }))}><FieldShell label="Department" htmlFor="positionDepartment" required><Select id="positionDepartment" autoFocus {...form.register('departmentId', { required: true })}><option value="">Choose department</option>{departments.map((department) => <option key={department.id} value={department.id}>{department.name}</option>)}</Select></FieldShell><FieldShell label="Position title" htmlFor="positionTitle" required><Input id="positionTitle" {...form.register('title', { required: true })} /></FieldShell><FieldShell label="Internal code" htmlFor="positionCode"><Input id="positionCode" {...form.register('code')} /></FieldShell><div className="form-actions"><Button type="button" variant="secondary" onClick={onClose}>Cancel</Button><Button type="submit" loading={loading}>Save position</Button></div></form></Dialog>;
}
