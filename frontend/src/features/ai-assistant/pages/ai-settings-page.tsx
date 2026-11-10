import { useMutation, useQueryClient } from '@tanstack/react-query';
import { Bot, Database, KeyRound, ShieldCheck, TestTube2 } from 'lucide-react';

import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { PageHeader } from '@/components/navigation/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { useNotification } from '@/app/providers/notification-provider';
import { toAppError } from '@/lib/api-error';

import { testAiProvider } from '../api/ai-api';
import { useAiSettings, useAiUsage } from '../hooks/use-ai';

export function AiSettingsPage() {
  const settings = useAiSettings();
  const usage = useAiUsage();
  const { notify } = useNotification();
  const queryClient = useQueryClient();
  const test = useMutation({
    mutationFn: testAiProvider,
    onSuccess: (connected) => {
      notify({
        title: connected ? 'Provider connected' : 'Provider did not respond',
        tone: connected ? 'success' : 'error'
      });
      void queryClient.invalidateQueries({ queryKey: ['ai'] });
    },
    onError: (error) => notify({ title: 'Connection test failed', message: toAppError(error).message, tone: 'error' })
  });
  if (settings.isLoading) return <Skeleton count={5} />;
  if (settings.isError || !settings.data)
    return <ErrorState title="AI settings could not be loaded" onRetry={() => void settings.refetch()} />;
  const value = settings.data;
  return (
    <div className="page-stack">
      <PageHeader
        eyebrow="Administration"
        title="AI assistant"
        description="Review provider health, governance gates, local retention, quotas, and usage. Credentials are never shown in the browser."
        actions={
          <Button variant="secondary" onClick={() => test.mutate()} loading={test.isPending}>
            <TestTube2 size={16} /> Test connection
          </Button>
        }
      />
      <div className="stats-grid">
        <Card className="ai-setting-card">
          <Bot />
          <span>
            <small>Assistant</small>
            <strong>{value.enabled ? 'Enabled' : 'Disabled'}</strong>
          </span>
          <Badge tone={value.enabled ? 'positive' : 'neutral'}>{value.chatModel}</Badge>
        </Card>
        <Card className="ai-setting-card">
          <ShieldCheck />
          <span>
            <small>Governance</small>
            <strong>{value.realDataApproved ? 'Approved' : 'Not approved'}</strong>
          </span>
        </Card>
        <Card className="ai-setting-card">
          <KeyRound />
          <span>
            <small>Provider credential</small>
            <strong>{value.providerConfigured ? 'Configured' : 'Missing'}</strong>
          </span>
        </Card>
        <Card className="ai-setting-card">
          <Database />
          <span>
            <small>Monthly usage</small>
            <strong>
              ${usage.data?.costThisMonthUsd.toFixed(2) ?? '0.00'} / ${value.monthlyBudgetUsd.toFixed(2)}
            </strong>
          </span>
        </Card>
      </div>
      <Card className="detail-card">
        <header>
          <div>
            <span className="detail-icon">
              <ShieldCheck size={17} />
            </span>
            <h2>Privacy and limits</h2>
          </div>
        </header>
        <dl className="detail-list">
          <div>
            <dt>Provider</dt>
            <dd>{value.provider}</dd>
          </div>
          <div>
            <dt>Embedding model</dt>
            <dd>{value.embeddingModel}</dd>
          </div>
          <div>
            <dt>Local history</dt>
            <dd>{value.conversationRetentionDays} days</dd>
          </div>
          <div>
            <dt>User daily limit</dt>
            <dd>{value.requestsPerDayPerUser} requests</dd>
          </div>
          <div>
            <dt>Organization daily limit</dt>
            <dd>{value.requestsPerDayPerOrganization} requests</dd>
          </div>
          <div>
            <dt>Requests today</dt>
            <dd>{usage.data?.requestsToday ?? 0}</dd>
          </div>
          <div>
            <dt>Tokens today</dt>
            <dd>{(usage.data?.inputTokensToday ?? 0) + (usage.data?.outputTokensToday ?? 0)}</dd>
          </div>
        </dl>
      </Card>
    </div>
  );
}
