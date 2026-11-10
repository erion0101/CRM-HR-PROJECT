import { BriefcaseBusiness, Building2, Network, Users } from 'lucide-react';

import { WorkspaceTabs } from '@/components/navigation/workspace-tabs';

const items = [
  { label: 'Directory', to: '/employees', icon: Users, end: true },
  { label: 'Organization chart', to: '/organization-chart', icon: Network },
  { label: 'Department & team', to: '/departments', icon: Building2 },
  { label: 'Positions', to: '/positions', icon: BriefcaseBusiness },
];

export function PeopleWorkspaceTabs() {
  return <WorkspaceTabs label="People workspace" items={items} />;
}
