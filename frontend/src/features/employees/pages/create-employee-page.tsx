import { useMutation } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';
import { Link, useNavigate } from 'react-router';

import { queryClient } from '@/app/providers/query-provider';
import { PageHeader } from '@/components/navigation/page-header';
import { Card } from '@/components/ui/card';
import { useNotification } from '@/app/providers/notification-provider';
import { toAppError } from '@/lib/api-error';

import { createEmployee } from '../api/employees-api';
import { EmployeeForm } from '../components/employee-form';

export function CreateEmployeePage() {
  const navigate = useNavigate();
  const { notify } = useNotification();
  const mutation = useMutation({
    mutationFn: createEmployee,
    onSuccess: async (employee) => {
      await queryClient.invalidateQueries({ queryKey: ['employees'] });
      notify({
        title: 'Employee created',
        message: `${employee.firstName} ${employee.lastName} is now in the directory.`,
        tone: 'success'
      });
      navigate(`/employees/${employee.id}`);
    },
    onError: (error) =>
      notify({ title: 'Could not create employee', message: toAppError(error).message, tone: 'error' })
  });
  return (
    <div className="page-stack page-stack--narrow">
      <Link className="back-link" to="/employees">
        <ArrowLeft size={16} /> Back to employees
      </Link>
      <PageHeader
        eyebrow="New record"
        title="Add an employee"
        description="Create the core profile first. Employment and organization details can be added from the employee record."
      />
      <Card className="form-card">
        <EmployeeForm
          loading={mutation.isPending}
          onSubmit={async (values) => {
            await mutation.mutateAsync(values);
          }}
          submitLabel="Create employee"
        />
      </Card>
    </div>
  );
}
