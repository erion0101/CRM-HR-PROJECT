import { useForm } from 'react-hook-form';

import { FieldShell, Input } from '@/components/forms/field';
import { Button } from '@/components/ui/button';

import type { Employee, EmployeeInput } from '../types';
import { normalizeEmployeeInput } from '../utils/normalize-employee-input';

export function EmployeeForm({ employee, loading, onSubmit, submitLabel = 'Save employee' }: { employee?: Employee; loading?: boolean; onSubmit: (input: EmployeeInput) => Promise<void>; submitLabel?: string }) {
  const { register, handleSubmit, formState: { errors } } = useForm<EmployeeInput>({ defaultValues: employee ? { employeeNumber: employee.employeeNumber, firstName: employee.firstName, lastName: employee.lastName, workEmail: employee.workEmail, phone: employee.phone ?? '', dateOfBirth: employee.dateOfBirth ?? '' } : {} });
  return <form className="form-stack" onSubmit={handleSubmit((input) => onSubmit(normalizeEmployeeInput(input)))} noValidate>
    <div className="form-grid">
      <FieldShell label="Employee number" htmlFor="employeeNumber" required error={errors.employeeNumber?.message}><Input id="employeeNumber" disabled={Boolean(employee)} {...register('employeeNumber', { required: 'Employee number is required.' })} /></FieldShell>
      <FieldShell label="Work email" htmlFor="workEmail" required error={errors.workEmail?.message}><Input id="workEmail" type="email" {...register('workEmail', { required: 'Work email is required.', pattern: { value: /^\S+@\S+\.\S+$/, message: 'Enter a valid email.' } })} /></FieldShell>
      <FieldShell label="First name" htmlFor="firstName" required error={errors.firstName?.message}><Input id="firstName" {...register('firstName', { required: 'First name is required.' })} /></FieldShell>
      <FieldShell label="Last name" htmlFor="lastName" required error={errors.lastName?.message}><Input id="lastName" {...register('lastName', { required: 'Last name is required.' })} /></FieldShell>
      <FieldShell label="Phone" htmlFor="phone" hint="Optional"><Input id="phone" type="tel" {...register('phone')} /></FieldShell>
      <FieldShell label="Date of birth" htmlFor="dateOfBirth" hint="Private"><Input id="dateOfBirth" type="date" max={new Date().toISOString().slice(0, 10)} {...register('dateOfBirth')} /></FieldShell>
    </div>
    <div className="form-actions"><Button type="submit" loading={loading}>{submitLabel}</Button></div>
  </form>;
}
