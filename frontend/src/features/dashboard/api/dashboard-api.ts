import { http } from '@/lib/api-client';

import type { DashboardSummary } from '../types';

export async function getDashboardSummary(): Promise<DashboardSummary> {
  const { data } = await http.get<DashboardSummary>('/dashboard/summary');
  return data;
}
