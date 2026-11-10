import { useMutation } from '@tanstack/react-query';
import { Plus } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';

import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { roleLabels } from '@/app/access';
import { EmptyState } from '@/components/feedback/empty-state';
import { FieldShell, Input, Select } from '@/components/forms/field';
import { PageHeader } from '@/components/navigation/page-header';
import { Avatar } from '@/components/ui/avatar';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';
import { useEmployees } from '@/features/employees/hooks/use-employees';
import { toAppError } from '@/lib/api-error';
import type { Role } from '@/types/api';

import { createUser, updateUser } from '../api/administration-api';
import { useUsers } from '../hooks/use-administration';
import type { ManagedUser } from '../types';

const roles: Role[] = ['Employee', 'Manager', 'HR', 'ProductManager'];

const roleDescriptions: Record<Role, string> = {
  Employee: 'Self-service access to personal records, documents, onboarding, knowledge, and assigned projects.',
  Manager: 'Employee access plus direct-report visibility and team onboarding responsibilities.',
  HR: 'Organization-wide people, organization, compensation, document, user, audit, and AI administration.',
  ProductManager: 'Project, membership, and project-knowledge management without access to restricted HR data.'
};
interface UserFormInput {
  email: string;
  temporaryPassword: string;
  role: Role;
  employeeId: string;
  isActive: boolean;
}

export function UsersPage() {
  const [editing, setEditing] = useState<ManagedUser | 'new' | null>(null);
  const { notify } = useNotification();
  const users = useUsers(undefined, 100);
  const employees = useEmployees({ limit: 100 });
  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Administration"
        title="User access"
        description="Create accounts, assign one role, and link users to employee records."
        actions={
          <Button onClick={() => setEditing('new')}>
            <Plus size={17} /> Create user
          </Button>
        }
      />
      <Card className="table-card">
        {users.data?.items.length ? (
          <div className="data-table-wrap">
            <table className="data-table">
              <thead>
                <tr>
                  <th>User</th>
                  <th>Role</th>
                  <th>Linked employee</th>
                  <th>Status</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {users.data.items.map((account) => {
                  const linked = employees.data?.items.find((item) => item.id === account.employeeId);
                  return (
                    <tr key={account.id}>
                      <td>
                        <div className="table-person">
                          <Avatar name={account.email} size="small" />
                          <span>
                            <strong>{account.email}</strong>
                            <small className="mono">{account.id.slice(0, 8)}</small>
                          </span>
                        </div>
                      </td>
                      <td>{account.roles.map((role) => roleLabels[role as Role] ?? role).join(', ')}</td>
                      <td>{linked ? `${linked.firstName} ${linked.lastName}` : 'Not linked'}</td>
                      <td>
                        <Badge tone={account.isActive ? 'positive' : 'negative'}>
                          {account.isActive ? 'Active' : 'Suspended'}
                        </Badge>
                      </td>
                      <td>
                        <Button variant="ghost" size="small" onClick={() => setEditing(account)}>
                          Manage
                        </Button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        ) : (
          <EmptyState
            title="No user accounts"
            description="Create the first account for a team member."
            actionLabel="Create user"
            onAction={() => setEditing('new')}
          />
        )}
      </Card>
      <UserDialog
        account={editing}
        employees={employees.data?.items ?? []}
        onClose={() => setEditing(null)}
        onSaved={() => setEditing(null)}
        notify={notify}
      />
    </div>
  );
}

function UserDialog({
  account,
  employees,
  onClose,
  onSaved,
  notify
}: {
  account: ManagedUser | 'new' | null;
  employees: { id: string; firstName: string; lastName: string }[];
  onClose: () => void;
  onSaved: () => void;
  notify: ReturnType<typeof useNotification>['notify'];
}) {
  const isNew = account === 'new';
  const form = useForm<UserFormInput>({
    values: {
      email: !isNew && account ? account.email : '',
      temporaryPassword: '',
      role: (!isNew && (account?.roles[0] as Role)) || 'Employee',
      employeeId: !isNew && account ? (account.employeeId ?? '') : '',
      isActive: !isNew && account ? account.isActive : true
    }
  });
  const mutation = useMutation({
    mutationFn: async (input: UserFormInput) =>
      isNew
        ? createUser({ ...input, employeeId: input.employeeId || undefined })
        : updateUser(account as ManagedUser, {
            isActive: input.isActive,
            role: input.role,
            employeeId: input.employeeId || undefined
          }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['users'] });
      onSaved();
      notify({ title: isNew ? 'User created' : 'User access updated', tone: 'success' });
    },
    onError: (error) => notify({ title: 'Could not save user', message: toAppError(error).message, tone: 'error' })
  });
  const selectedRole = form.watch('role');
  return (
    <Dialog
      open={Boolean(account)}
      onClose={onClose}
      title={isNew ? 'Create user account' : 'Manage user account'}
      description="Access remains subject to server-side role and data-scope policies."
    >
      <form className="form-stack" onSubmit={form.handleSubmit((input) => mutation.mutate(input))}>
        <FieldShell label="Email" htmlFor="userEmail">
          <Input id="userEmail" type="email" disabled={!isNew} {...form.register('email', { required: true })} />
        </FieldShell>
        {isNew ? (
          <FieldShell
            label="Temporary password"
            htmlFor="temporaryPassword"
            hint="The user should change this after signing in."
          >
            <Input
              id="temporaryPassword"
              type="password"
              {...form.register('temporaryPassword', { required: true, minLength: 12 })}
            />
          </FieldShell>
        ) : null}
        <FieldShell label="Role" htmlFor="userRole" hint={roleDescriptions[selectedRole]}>
          <Select id="userRole" {...form.register('role')}>
            {roles.map((role) => (
              <option key={role} value={role}>
                {role === 'ProductManager' ? 'Product Manager' : role}
              </option>
            ))}
          </Select>
        </FieldShell>
        <FieldShell label="Linked employee" htmlFor="linkedEmployee">
          <Select id="linkedEmployee" {...form.register('employeeId')}>
            <option value="">Not linked</option>
            {employees.map((employee) => (
              <option key={employee.id} value={employee.id}>
                {employee.firstName} {employee.lastName}
              </option>
            ))}
          </Select>
        </FieldShell>
        {!isNew ? (
          <label className="checkbox-row">
            <input type="checkbox" {...form.register('isActive')} />
            <span>Account is active</span>
          </label>
        ) : null}
        <Button type="submit" loading={mutation.isPending}>
          {isNew ? 'Create user' : 'Save access'}
        </Button>
      </form>
    </Dialog>
  );
}
