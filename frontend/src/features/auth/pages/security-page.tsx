import { useMutation } from '@tanstack/react-query';
import { KeyRound, ShieldCheck } from 'lucide-react';
import { useForm } from 'react-hook-form';

import { useNotification } from '@/app/providers/notification-provider';
import { FieldShell, Input } from '@/components/forms/field';
import { PageHeader } from '@/components/navigation/page-header';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { toAppError } from '@/lib/api-error';

import { changePassword } from '../api/auth-api';

interface PasswordForm {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export function SecurityPage() {
  const { notify } = useNotification();
  const form = useForm<PasswordForm>();
  const mutation = useMutation({
    mutationFn: (input: PasswordForm) =>
      changePassword({ currentPassword: input.currentPassword, newPassword: input.newPassword }),
    onSuccess: () => {
      form.reset();
      notify({ title: 'Password changed', message: 'Your new password is active.', tone: 'success' });
    },
    onError: (error) => notify({ title: 'Password change failed', message: toAppError(error).message, tone: 'error' })
  });
  return (
    <div className="page-stack page-stack--narrow">
      <PageHeader
        eyebrow="Your account"
        title="Security settings"
        description="Update the password used to access your local HR workspace."
      />
      <Card className="security-card">
        <span className="security-card__icon">
          <ShieldCheck size={22} />
        </span>
        <div>
          <h2>Password</h2>
          <p>Use at least 12 characters with a mix of uppercase, lowercase, numbers, and symbols.</p>
        </div>
        <form className="form-stack" onSubmit={form.handleSubmit((input) => mutation.mutate(input))}>
          <FieldShell
            label="Current password"
            htmlFor="currentPassword"
            error={form.formState.errors.currentPassword?.message}
          >
            <Input
              id="currentPassword"
              type="password"
              autoComplete="current-password"
              {...form.register('currentPassword', { required: 'Current password is required.' })}
            />
          </FieldShell>
          <FieldShell label="New password" htmlFor="newPassword" error={form.formState.errors.newPassword?.message}>
            <Input
              id="newPassword"
              type="password"
              autoComplete="new-password"
              {...form.register('newPassword', {
                required: 'New password is required.',
                minLength: { value: 12, message: 'Use at least 12 characters.' }
              })}
            />
          </FieldShell>
          <FieldShell
            label="Confirm new password"
            htmlFor="confirmPassword"
            error={form.formState.errors.confirmPassword?.message}
          >
            <Input
              id="confirmPassword"
              type="password"
              autoComplete="new-password"
              {...form.register('confirmPassword', {
                validate: (value) => value === form.getValues('newPassword') || 'Passwords do not match.'
              })}
            />
          </FieldShell>
          <Button type="submit" loading={mutation.isPending}>
            <KeyRound size={17} /> Change password
          </Button>
        </form>
      </Card>
    </div>
  );
}
