import { useForm } from 'react-hook-form';

import { FieldShell, Input, Select } from '@/components/forms/field';
import { Button } from '@/components/ui/button';
import { Dialog } from '@/components/ui/dialog';

import { createLeaveType, updateLeaveType } from '../api/time-off-api';
import { useTimeOffMutation } from '../hooks/use-time-off-mutation';
import type { LeaveType, LeaveTypeInput } from '../types';

interface FormValues {
  name: string;
  color: string;
  isPaid: boolean;
  requiresApproval: boolean;
  deductsFromBalance: boolean;
  daysPerYear: string;
  accrualMethod: LeaveTypeInput['accrualMethod'];
  carriesOver: boolean;
  maxCarryOverDays: string;
  /** "mm-dd", or empty when carried-over days never expire. */
  expiry: string;
}

const toForm = (type?: LeaveType): FormValues => ({
  name: type?.name ?? '',
  color: type?.color ?? '#16a34a',
  isPaid: type?.isPaid ?? true,
  requiresApproval: type?.requiresApproval ?? true,
  deductsFromBalance: type?.deductsFromBalance ?? true,
  daysPerYear: String(type?.daysPerYear ?? 5),
  accrualMethod: type?.accrualMethod ?? 'YearlyUpfront',
  carriesOver: type?.carriesOver ?? false,
  maxCarryOverDays: type?.maxCarryOverDays != null ? String(type.maxCarryOverDays) : '',
  expiry:
    type?.carryOverExpiryMonth && type.carryOverExpiryDay
      ? `${String(type.carryOverExpiryMonth).padStart(2, '0')}-${String(type.carryOverExpiryDay).padStart(2, '0')}`
      : ''
});

function toLeaveTypeInput(values: FormValues): LeaveTypeInput {
  const [month, day] = values.expiry ? values.expiry.split('-').map(Number) : [null, null];
  return {
    name: values.name.trim(),
    color: values.color,
    isPaid: values.isPaid,
    requiresApproval: values.requiresApproval,
    deductsFromBalance: values.deductsFromBalance,
    daysPerYear: values.deductsFromBalance ? Number(values.daysPerYear) : 0,
    accrualMethod: values.accrualMethod,
    carriesOver: values.deductsFromBalance && values.carriesOver,
    maxCarryOverDays: values.carriesOver && values.maxCarryOverDays !== '' ? Number(values.maxCarryOverDays) : null,
    carryOverExpiryMonth: values.carriesOver ? month : null,
    carryOverExpiryDay: values.carriesOver ? day : null
  };
}

/** Add or change a kind of leave and its allowance rules. */
export function LeaveTypeDialog({ type, onClose }: { type?: LeaveType; onClose: () => void }) {
  const form = useForm<FormValues>({ defaultValues: toForm(type) });
  const save = useTimeOffMutation({
    mutationFn: (input: LeaveTypeInput) =>
      type ? updateLeaveType(type.id, type.version, input) : createLeaveType(input),
    success: {
      title: type ? 'Leave type saved' : 'Leave type added',
      message: 'Balances use the new rules straight away.'
    },
    errorTitle: 'Could not save the leave type',
    onSuccess: onClose
  });
  const deducts = form.watch('deductsFromBalance');
  const carries = form.watch('carriesOver');
  const accrual = form.watch('accrualMethod');
  const days = Number(form.watch('daysPerYear')) || 0;
  const submit = form.handleSubmit((values) => save.mutate(toLeaveTypeInput(values)));

  return (
    <Dialog
      open
      onClose={onClose}
      title={type ? `Change ${type.name}` : 'Add a leave type'}
      footer={
        <>
          <Button variant="secondary" onClick={onClose}>
            Close
          </Button>
          <Button loading={save.isPending} onClick={() => void submit()}>
            Save
          </Button>
        </>
      }
    >
      <form className="form-stack" onSubmit={submit}>
        <div className="form-grid">
          <FieldShell
            label="Name"
            htmlFor="typeName"
            required
            error={form.formState.errors.name ? 'Give it a name' : undefined}
          >
            <Input
              id="typeName"
              maxLength={80}
              {...form.register('name', { required: true, validate: (v) => v.trim() !== '' })}
            />
          </FieldShell>
          <FieldShell label="Color" htmlFor="typeColor">
            <Input id="typeColor" type="color" {...form.register('color')} />
          </FieldShell>
        </div>
        <label className="checkbox-row">
          <input type="checkbox" {...form.register('isPaid')} /> Paid
        </label>
        <label className="checkbox-row">
          <input type="checkbox" {...form.register('requiresApproval')} /> The manager must approve it
        </label>
        <label className="checkbox-row">
          <input type="checkbox" {...form.register('deductsFromBalance')} /> Has a yearly allowance
        </label>
        {deducts ? (
          <>
            <div className="form-grid">
              <FieldShell label="Days per year" htmlFor="typeDays" required>
                <Input
                  id="typeDays"
                  type="number"
                  min={0.5}
                  max={366}
                  step={0.5}
                  {...form.register('daysPerYear', { required: true })}
                />
              </FieldShell>
              <FieldShell
                label="How the days are earned"
                htmlFor="typeAccrual"
                hint={
                  accrual === 'Monthly'
                    ? `${Math.round((days / 12) * 100) / 100} days for each month worked`
                    : 'All days on 1 January'
                }
              >
                <Select id="typeAccrual" {...form.register('accrualMethod')}>
                  <option value="Monthly">Each month</option>
                  <option value="YearlyUpfront">All at the start of the year</option>
                </Select>
              </FieldShell>
            </div>
            <label className="checkbox-row">
              <input type="checkbox" {...form.register('carriesOver')} /> Unused days carry over to next year
            </label>
            {carries ? (
              <div className="form-grid">
                <FieldShell label="Most days carried over" htmlFor="typeMaxCarry" hint="Leave empty for no limit">
                  <Input id="typeMaxCarry" type="number" min={0} step={0.5} {...form.register('maxCarryOverDays')} />
                </FieldShell>
                <FieldShell label="Carried-over days expire" htmlFor="typeExpiry">
                  <Select id="typeExpiry" {...form.register('expiry')}>
                    <option value="">Never</option>
                    <option value="03-31">After 31 March</option>
                    <option value="06-30">After 30 June</option>
                    <option value="09-30">After 30 September</option>
                    <option value="12-31">After 31 December</option>
                  </Select>
                </FieldShell>
              </div>
            ) : null}
          </>
        ) : null}
      </form>
    </Dialog>
  );
}
