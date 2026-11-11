import { useMutation, useQuery } from '@tanstack/react-query';
import { Copy, KeyRound, RefreshCw } from 'lucide-react';
import { useState } from 'react';

import { roleLabels } from '@/app/access';
import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { FieldShell, Input, Select } from '@/components/forms/field';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { ConfirmDialog } from '@/components/ui/confirm-dialog';
import { useAuth } from '@/features/auth/hooks/use-auth';
import { generateTemporaryPassword } from '@/features/new-hire/utils';
import { toAppError } from '@/lib/api-error';
import type { Role } from '@/types/api';

import { createUser, getEmployeeLogin, updateUser } from '../api/administration-api';
import type { ManagedUser } from '../types';
import { roleDescriptions, roleOptions } from '../utils/roles';

/** HR only: the person's login on their profile. Create it, change the role, or turn it off when they leave. */
export function EmployeeLoginTab({
  employeeId,
  workEmail,
  name
}: {
  employeeId: string;
  workEmail: string;
  name: string;
}) {
  const login = useQuery({
    queryKey: ['users', 'by-employee', employeeId],
    queryFn: () => getEmployeeLogin(employeeId)
  });
  if (login.isLoading)
    return (
      <Card>
        <Skeleton count={3} />
      </Card>
    );
  if (login.isError) return <ErrorState onRetry={() => void login.refetch()} />;
  const account = login.data?.login;
  return account ? (
    // Remount after a save so the role picker starts from the saved role.
    <ManageLogin key={`${account.id}-${account.isActive}-${account.roles[0]}`} account={account} name={name} />
  ) : (
    <CreateLogin employeeId={employeeId} workEmail={workEmail} name={name} />
  );
}

function useLoginMutation<TVariables, TData>(mutationFn: (variables: TVariables) => Promise<TData>, success: string) {
  const { notify } = useNotification();
  return useMutation({
    mutationFn,
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['users'] });
      notify({ title: success, tone: 'success' });
    },
    onError: (error) => notify({ title: 'Could not save the login', message: toAppError(error).message, tone: 'error' })
  });
}

function ManageLogin({ account, name }: { account: ManagedUser; name: string }) {
  const { user } = useAuth();
  const isSelf = user?.userId === account.id;
  const current = (account.roles[0] as Role | undefined) ?? 'Employee';
  const [role, setRole] = useState<Role>(current);
  const [confirm, setConfirm] = useState<'off' | 'on' | null>(null);
  const save = useLoginMutation(
    (input: { isActive: boolean; role: Role }) =>
      updateUser(account, { ...input, employeeId: account.employeeId ?? undefined }),
    'Login updated'
  );

  return (
    <Card className="login-card">
      <div className="login-card__summary">
        <span className="login-card__icon" aria-hidden="true">
          <KeyRound size={18} />
        </span>
        <div>
          <strong>{account.email}</strong>
          <small>
            {account.isActive
              ? `${name} signs in with this email.`
              : `The login is turned off, so ${name} cannot sign in.`}
          </small>
        </div>
        <Badge tone={account.isActive ? 'positive' : 'negative'}>{account.isActive ? 'Active' : 'Turned off'}</Badge>
      </div>

      <div className="login-card__row">
        <FieldShell
          label="Role"
          htmlFor="loginRole"
          hint={isSelf ? 'You cannot change your own role.' : roleDescriptions[role]}
        >
          <Select
            id="loginRole"
            value={role}
            disabled={isSelf}
            onChange={(event) => setRole(event.target.value as Role)}
          >
            {roleOptions.map((option) => (
              <option key={option} value={option}>
                {roleLabels[option]}
              </option>
            ))}
          </Select>
        </FieldShell>
        <Button
          variant="secondary"
          disabled={role === current || isSelf}
          loading={save.isPending && confirm === null}
          onClick={() => save.mutate({ isActive: account.isActive, role })}
        >
          Save role
        </Button>
      </div>

      <div className="login-card__footer">
        {isSelf ? (
          <p>This is your own login. Another HR user can turn it off.</p>
        ) : account.isActive ? (
          <>
            <p>When {name} leaves, turn the login off so they can no longer sign in. Their records stay.</p>
            <Button variant="danger" onClick={() => setConfirm('off')}>
              Turn off login
            </Button>
          </>
        ) : (
          <>
            <p>Turn it back on if {name} returns.</p>
            <Button variant="secondary" onClick={() => setConfirm('on')}>
              Turn login back on
            </Button>
          </>
        )}
      </div>

      <ConfirmDialog
        open={confirm !== null}
        title={confirm === 'off' ? `Turn off ${name}'s login?` : `Turn ${name}'s login back on?`}
        description={
          confirm === 'off'
            ? 'They are signed out and cannot sign in again. Nothing else changes, and you can turn it back on at any time.'
            : `They can sign in again with ${account.email} and their current password.`
        }
        confirmLabel={confirm === 'off' ? 'Turn off' : 'Turn on'}
        loading={save.isPending}
        onConfirm={() =>
          save.mutate({ isActive: confirm === 'on', role: current }, { onSuccess: () => setConfirm(null) })
        }
        onClose={() => setConfirm(null)}
      />
    </Card>
  );
}

function CreateLogin({ employeeId, workEmail, name }: { employeeId: string; workEmail: string; name: string }) {
  const [email, setEmail] = useState(workEmail);
  const [role, setRole] = useState<Role>('Employee');
  const [password, setPassword] = useState(() => generateTemporaryPassword());
  const create = useLoginMutation(
    () => createUser({ email: email.trim(), temporaryPassword: password, role, employeeId }),
    'Login created'
  );

  return (
    <Card className="login-card">
      <div className="login-card__summary">
        <span className="login-card__icon" aria-hidden="true">
          <KeyRound size={18} />
        </span>
        <div>
          <strong>{name} has no login yet</strong>
          <small>
            Create one so they can sign in. Give them the temporary password; they change it after signing in.
          </small>
        </div>
      </div>
      <div className="form-grid login-card__form">
        <FieldShell label="Email" htmlFor="newLoginEmail" required>
          <Input id="newLoginEmail" type="email" value={email} onChange={(event) => setEmail(event.target.value)} />
        </FieldShell>
        <FieldShell label="Role" htmlFor="newLoginRole" hint={roleDescriptions[role]}>
          <Select id="newLoginRole" value={role} onChange={(event) => setRole(event.target.value as Role)}>
            {roleOptions.map((option) => (
              <option key={option} value={option}>
                {roleLabels[option]}
              </option>
            ))}
          </Select>
        </FieldShell>
        <FieldShell label="Temporary password" htmlFor="newLoginPassword" hint="Copy it now; it is not shown again.">
          <div className="row-actions">
            <Input id="newLoginPassword" value={password} readOnly />
            <Button
              type="button"
              size="small"
              variant="secondary"
              aria-label="Generate a new password"
              onClick={() => setPassword(generateTemporaryPassword())}
            >
              <RefreshCw size={14} />
            </Button>
            <Button
              type="button"
              size="small"
              variant="secondary"
              aria-label="Copy password"
              onClick={() => void navigator.clipboard?.writeText(password)}
            >
              <Copy size={14} />
            </Button>
          </div>
        </FieldShell>
      </div>
      <div className="login-card__footer">
        <p>The person must use this email to sign in.</p>
        <Button loading={create.isPending} disabled={!email.trim()} onClick={() => create.mutate(undefined)}>
          Create login
        </Button>
      </div>
    </Card>
  );
}
