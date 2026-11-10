import { useMutation, useQuery } from '@tanstack/react-query';
import { ArrowLeft, BookOpen, CheckCircle2, Clock3, Download, FileText, Plus, Search, Upload } from 'lucide-react';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { Link, useNavigate, useParams } from 'react-router';

import { canManageKnowledge } from '@/app/access';
import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { FieldShell, Input, Select, Textarea } from '@/components/forms/field';
import { PageHeader } from '@/components/navigation/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';
import { useAuth } from '@/features/auth/hooks/use-auth';
import { getProject } from '@/features/projects/api/projects-api';
import { OnboardingWorkspaceTabs } from '@/features/onboarding/components/onboarding-workspace-tabs';
import { toAppError } from '@/lib/api-error';

import {
  approveKnowledgeDocumentForAi,
  createKnowledgeArticle,
  downloadKnowledgeDocument,
  getKnowledgeArticle,
  getKnowledgeArticles,
  getKnowledgeDocuments,
  publishKnowledgeArticle,
  submitKnowledgeForReview,
  uploadKnowledgeDocument
} from '../api/knowledge-api';
import type { KnowledgeArticleInput, KnowledgeAudience } from '../types';

export function KnowledgePage() {
  const { user } = useAuth();
  const { notify } = useNotification();
  const [search, setSearch] = useState('');
  const [creating, setCreating] = useState(false);
  const canEdit = canManageKnowledge(user);
  const query = useQuery({
    queryKey: ['knowledge', search, canEdit],
    queryFn: () => getKnowledgeArticles({ search: search || undefined, includeDrafts: canEdit })
  });
  const create = useMutation({
    mutationFn: createKnowledgeArticle,
    onSuccess: async (article) => {
      await queryClient.invalidateQueries({ queryKey: ['knowledge'] });
      setCreating(false);
      notify({ title: 'Knowledge draft created', tone: 'success' });
      window.location.assign(`/knowledge/articles/${article.id}`);
    },
    onError: (error) => notify({ title: 'Could not create article', message: toAppError(error).message, tone: 'error' })
  });

  return (
    <div className="page-stack knowledge-page">
      <PageHeader
        eyebrow="Onboarding"
        title="Company knowledge"
        description="Published, reviewed guidance about how your organization works."
        actions={
          canEdit ? (
            <Button onClick={() => setCreating(true)}>
              <Plus size={17} /> New article
            </Button>
          ) : undefined
        }
      >
        <OnboardingWorkspaceTabs />
      </PageHeader>
      <div className="knowledge-toolbar">
        <label>
          <Search size={17} />
          <Input
            value={search}
            onChange={(event) => setSearch(event.target.value)}
            placeholder="Search company knowledge"
            aria-label="Search company knowledge"
          />
        </label>
      </div>
      {query.isLoading ? (
        <Card>
          <Skeleton count={5} />
        </Card>
      ) : query.isError ? (
        <Card>
          <ErrorState
            title="Knowledge could not be loaded"
            description="Try again."
            onRetry={() => void query.refetch()}
          />
        </Card>
      ) : query.data?.length ? (
        <div className="knowledge-grid">
          {query.data.map((article) => (
            <Link className="knowledge-card" to={`/knowledge/articles/${article.id}`} key={article.id}>
              <span>
                <BookOpen size={20} />
              </span>
              <div>
                <small>{article.category}</small>
                <h2>{article.title}</h2>
                <p>{article.description}</p>
                <footer>
                  <Badge tone={article.status === 'Published' ? 'positive' : 'warning'}>{article.status}</Badge>
                  <span>Revision {article.revision}</span>
                </footer>
              </div>
            </Link>
          ))}
        </div>
      ) : (
        <Card>
          <EmptyState
            title="No knowledge articles found"
            description={search ? 'Try another search term.' : 'Published company guidance will appear here.'}
            actionLabel={canEdit && !search ? 'Create first article' : undefined}
            onAction={canEdit ? () => setCreating(true) : undefined}
          />
        </Card>
      )}
      <KnowledgeEditor
        open={creating}
        loading={create.isPending}
        onClose={() => setCreating(false)}
        onSave={(value) => create.mutateAsync(value)}
      />
    </div>
  );
}

export function KnowledgeArticlePage() {
  const { articleId = '' } = useParams();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { notify } = useNotification();
  const [uploadFile, setUploadFile] = useState<File | null>(null);
  const query = useQuery({
    queryKey: ['knowledge', articleId],
    queryFn: () => getKnowledgeArticle(articleId),
    enabled: Boolean(articleId)
  });
  const project = useQuery({
    queryKey: ['projects', query.data?.projectId],
    queryFn: () => getProject(query.data!.projectId!),
    enabled: Boolean(query.data?.projectId)
  });
  const documents = useQuery({
    queryKey: ['knowledge', articleId, 'documents'],
    queryFn: () => getKnowledgeDocuments(articleId),
    enabled: Boolean(articleId) && query.isSuccess
  });
  const transition = useMutation({
    mutationFn: async (action: 'review' | 'publish') => {
      if (!query.data) throw new Error('Article unavailable');
      return action === 'review' ? submitKnowledgeForReview(query.data) : publishKnowledgeArticle(query.data);
    },
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['knowledge'] });
      notify({ title: 'Article status updated', tone: 'success' });
    },
    onError: (error) => notify({ title: 'Could not update article', message: toAppError(error).message, tone: 'error' })
  });
  const upload = useMutation({
    mutationFn: async () => {
      if (!uploadFile) throw new Error('Choose a PDF or DOCX file.');
      return uploadKnowledgeDocument(articleId, uploadFile, 'Knowledge');
    },
    onSuccess: async () => {
      setUploadFile(null);
      await queryClient.invalidateQueries({ queryKey: ['knowledge', articleId, 'documents'] });
      notify({ title: 'Document queued for security scanning', tone: 'success' });
    },
    onError: (error) =>
      notify({ title: 'Could not upload document', message: toAppError(error).message, tone: 'error' })
  });
  const approveAi = useMutation({
    mutationFn: (documentId: string) => approveKnowledgeDocumentForAi(articleId, documentId),
    onSuccess: () => notify({ title: 'Document queued for AI indexing', tone: 'success' }),
    onError: (error) =>
      notify({ title: 'Could not approve AI indexing', message: toAppError(error).message, tone: 'error' })
  });

  if (query.isLoading)
    return (
      <Card>
        <Skeleton count={7} />
      </Card>
    );
  if (query.isError || !query.data)
    return (
      <Card>
        <ErrorState
          title="Article unavailable"
          description="It may not be published for your audience."
          onRetry={() => void query.refetch()}
        />
      </Card>
    );
  const article = query.data;
  const canEdit = canManageKnowledge(user) || project.data?.canManage === true;
  return (
    <div className="page-stack knowledge-article-page">
      <button className="text-link" onClick={() => navigate(-1)}>
        <ArrowLeft size={16} /> Back to knowledge
      </button>
      <PageHeader
        eyebrow={article.category}
        title={article.title}
        description={article.description}
        actions={
          canEdit ? (
            <div className="row-actions">
              {article.status === 'Draft' ? (
                <Button variant="secondary" loading={transition.isPending} onClick={() => transition.mutate('review')}>
                  <Clock3 size={16} /> Submit for review
                </Button>
              ) : null}
              {article.status === 'InReview' || article.status === 'Draft' ? (
                <Button loading={transition.isPending} onClick={() => transition.mutate('publish')}>
                  <CheckCircle2 size={16} /> Publish
                </Button>
              ) : null}
            </div>
          ) : undefined
        }
      />
      <Card className="knowledge-article">
        <div className="knowledge-article__meta">
          <Badge tone={article.status === 'Published' ? 'positive' : 'warning'}>{article.status}</Badge>
          <span>Revision {article.revision}</span>
          {article.lastReviewedAtUtc ? (
            <span>Reviewed {new Date(article.lastReviewedAtUtc).toLocaleDateString()}</span>
          ) : null}
        </div>
        <div className="knowledge-markdown">{article.content}</div>
      </Card>
      <Card>
        <div className="section-heading section-heading--padded">
          <div>
            <h2>Related documents</h2>
            <p>
              PDF and DOCX attachments use the existing quarantine, ClamAV, hashing, and immutable storage pipeline.
            </p>
          </div>
          {canEdit ? (
            <div className="upload-control">
              <Input
                aria-label="Knowledge document"
                type="file"
                accept=".pdf,.docx"
                onChange={(event) => setUploadFile(event.target.files?.[0] ?? null)}
              />
              <Button size="small" disabled={!uploadFile} loading={upload.isPending} onClick={() => upload.mutate()}>
                <Upload size={15} /> Upload
              </Button>
            </div>
          ) : null}
        </div>
        <div className="document-list">
          {documents.isLoading ? (
            <Skeleton count={2} />
          ) : documents.data?.length ? (
            documents.data.map((document) => (
              <div className="document-row" key={document.id}>
                <span className="document-row__icon">
                  <FileText size={18} />
                </span>
                <div>
                  <strong>{document.displayName}</strong>
                  <small>
                    {document.documentType} · {document.status}
                  </small>
                </div>
                <Badge
                  tone={
                    document.status === 'Clean' ? 'positive' : document.status === 'Rejected' ? 'negative' : 'warning'
                  }
                >
                  {document.status}
                </Badge>
                <div className="row-actions">
                  {canEdit && article.status === 'Published' && document.status === 'Clean' ? (
                    <Button
                      size="small"
                      variant="ghost"
                      loading={approveAi.isPending}
                      onClick={() => approveAi.mutate(document.id)}
                    >
                      Approve for AI
                    </Button>
                  ) : null}
                  <Button
                    size="small"
                    variant="secondary"
                    disabled={document.status !== 'Clean'}
                    onClick={() => void downloadKnowledgeDocument(article.id, document)}
                  >
                    <Download size={14} /> Download
                  </Button>
                </div>
              </div>
            ))
          ) : (
            <EmptyState title="No related documents" description="Approved supporting documents will appear here." />
          )}
        </div>
      </Card>
    </div>
  );
}

function KnowledgeEditor({
  open,
  loading,
  onClose,
  onSave
}: {
  open: boolean;
  loading: boolean;
  onClose: () => void;
  onSave: (value: KnowledgeArticleInput) => Promise<unknown>;
}) {
  const form = useForm<{
    slug: string;
    title: string;
    description: string;
    category: string;
    content: string;
    audience: KnowledgeAudience;
  }>({
    defaultValues: {
      slug: '',
      title: '',
      description: '',
      category: 'Company overview',
      content: '',
      audience: 'AllEmployees'
    }
  });
  return (
    <Dialog
      open={open}
      onClose={onClose}
      title="Create company knowledge"
      description="Create a draft. Publish it only after the content has been reviewed."
      width="large"
    >
      <form className="form-stack" onSubmit={form.handleSubmit(async (value) => onSave({ ...value, sortOrder: 0 }))}>
        <div className="form-grid">
          <FieldShell label="Title" htmlFor="knowledgeTitle" required>
            <Input id="knowledgeTitle" {...form.register('title', { required: true })} />
          </FieldShell>
          <FieldShell label="URL slug" htmlFor="knowledgeSlug" required hint="Letters, numbers, and hyphens">
            <Input id="knowledgeSlug" {...form.register('slug', { required: true, pattern: /^[a-zA-Z0-9-]+$/ })} />
          </FieldShell>
          <FieldShell label="Category" htmlFor="knowledgeCategory" required>
            <Input id="knowledgeCategory" {...form.register('category', { required: true })} />
          </FieldShell>
          <FieldShell label="Audience" htmlFor="knowledgeAudience" required>
            <Select id="knowledgeAudience" {...form.register('audience')}>
              <option value="AllEmployees">All employees</option>
              <option value="Managers">Managers</option>
              <option value="HrOnly">HR only</option>
            </Select>
          </FieldShell>
        </div>
        <FieldShell label="Description" htmlFor="knowledgeDescription" required>
          <Textarea id="knowledgeDescription" {...form.register('description', { required: true })} />
        </FieldShell>
        <FieldShell
          label="Content"
          htmlFor="knowledgeContent"
          required
          hint="Markdown is stored safely and rendered as text in this first authoring experience."
        >
          <Textarea id="knowledgeContent" rows={14} {...form.register('content', { required: true })} />
        </FieldShell>
        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={onClose}>
            Cancel
          </Button>
          <Button type="submit" loading={loading}>
            Create draft
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
