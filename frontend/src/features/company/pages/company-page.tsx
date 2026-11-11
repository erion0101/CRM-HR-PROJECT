import { useMutation, useQueries, useQuery } from '@tanstack/react-query';
import {
  ArrowRight,
  BookOpenCheck,
  Building2,
  Download,
  FileCheck2,
  FileText,
  FolderKanban,
  Plus,
  Upload,
  Users
} from 'lucide-react';
import { useState } from 'react';
import { Link } from 'react-router';

import { useNotification } from '@/app/providers/notification-provider';
import { queryClient } from '@/app/providers/query-provider';
import { EmptyState } from '@/components/feedback/empty-state';
import { ErrorState } from '@/components/feedback/error-state';
import { Skeleton } from '@/components/feedback/skeleton';
import { FieldShell, Input, Select } from '@/components/forms/field';
import { PageHeader } from '@/components/navigation/page-header';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card } from '@/components/ui/card';
import { Dialog } from '@/components/ui/dialog';
import {
  createKnowledgeArticle,
  downloadKnowledgeDocument,
  getKnowledgeArticles,
  getKnowledgeDocuments,
  publishKnowledgeArticle,
  uploadKnowledgeDocument
} from '@/features/knowledge/api/knowledge-api';
import type { KnowledgeArticle, KnowledgeAudience } from '@/features/knowledge/types';
import { getProjects } from '@/features/projects/api/projects-api';
import { OnboardingWorkspaceTabs } from '@/features/onboarding/components/onboarding-workspace-tabs';
import { toAppError } from '@/lib/api-error';

import type { EmployeeDocument } from '@/features/documents/types';

type PdfDestination = 'company' | string;

interface PdfUploadInput {
  file: File;
  destination: PdfDestination;
  category: string;
  audience: KnowledgeAudience;
}

function documentTone(status: EmployeeDocument['status']): 'positive' | 'negative' | 'warning' {
  if (status === 'Clean') return 'positive';
  if (status === 'Rejected' || status === 'Archived') return 'negative';
  return 'warning';
}

function titleFromFile(fileName: string): string {
  const withoutExtension = fileName
    .replace(/\.pdf$/i, '')
    .replace(/[_-]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();
  return (withoutExtension || 'Company document').slice(0, 200);
}

function slugFromTitle(title: string): string {
  const base =
    title
      .toLowerCase()
      .normalize('NFKD')
      .replace(/[^a-z0-9]+/g, '-')
      .replace(/^-+|-+$/g, '')
      .slice(0, 125) || 'company-document';
  return `${base}-${Date.now().toString(36)}`;
}

export function CompanyPage() {
  const { notify } = useNotification();
  const [uploadOpen, setUploadOpen] = useState(false);
  const projects = useQuery({ queryKey: ['projects'], queryFn: getProjects });
  const companyArticles = useQuery({
    queryKey: ['knowledge', 'company-workspace'],
    queryFn: () => getKnowledgeArticles({ includeDrafts: true })
  });
  const projectArticleQueries = useQueries({
    queries: (projects.data ?? []).map((project) => ({
      queryKey: ['knowledge', 'project', project.id, 'company-workspace'],
      queryFn: () => getKnowledgeArticles({ projectId: project.id, includeDrafts: true })
    }))
  });
  const projectArticles = projectArticleQueries.flatMap((query) => query.data ?? []);
  const allArticles = [...(companyArticles.data ?? []), ...projectArticles];
  const documentQueries = useQueries({
    queries: allArticles.map((article) => ({
      queryKey: ['knowledge', article.id, 'documents'],
      queryFn: () => getKnowledgeDocuments(article.id),
      refetchInterval: (query: { state: { data?: EmployeeDocument[] } }) =>
        query.state.data?.some((document) => document.status === 'PendingScan') ? 5_000 : false
    }))
  });
  const documentsByArticle = new Map<string, EmployeeDocument[]>(
    allArticles.map((article, index) => [article.id, documentQueries[index]?.data ?? []])
  );
  const companyDocuments = (companyArticles.data ?? []).flatMap((article) =>
    (documentsByArticle.get(article.id) ?? []).map((document) => ({ article, document }))
  );
  const projectDocuments = projectArticles.flatMap((article) => documentsByArticle.get(article.id) ?? []);
  const onboardingReady = allArticles.reduce(
    (count, article) =>
      count +
      (article.status === 'Published'
        ? (documentsByArticle.get(article.id) ?? []).filter((document) => document.status === 'Clean').length
        : 0),
    0
  );
  const initialLoading = projects.isLoading || companyArticles.isLoading;
  const initialError = projects.isError || companyArticles.isError;

  const uploadPdf = useMutation({
    mutationFn: async (input: PdfUploadInput) => {
      const title = titleFromFile(input.file.name);
      const projectId = input.destination === 'company' ? undefined : input.destination;
      const article = await createKnowledgeArticle({
        slug: slugFromTitle(title),
        title,
        description: `Document-based knowledge maintained from ${input.file.name}.`,
        category: input.category,
        content: `This knowledge record is maintained from the attached source document: ${input.file.name}.`,
        audience: projectId ? 'ProjectMembers' : input.audience,
        projectId,
        sortOrder: 0
      });
      await uploadKnowledgeDocument(article.id, input.file, projectId ? 'Project knowledge' : 'Company knowledge');
      return publishKnowledgeArticle(article);
    },
    onSuccess: async () => {
      setUploadOpen(false);
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['knowledge'] }),
        queryClient.invalidateQueries({ queryKey: ['onboarding'] })
      ]);
      notify({
        title: 'Document added',
        message: 'It can be used in a package as soon as its security scan is clean, usually within a minute.',
        tone: 'success'
      });
    },
    onError: (error) =>
      notify({ title: 'Could not add the document', message: toAppError(error).message, tone: 'error' })
  });

  if (initialLoading)
    return (
      <Card className="company-loading">
        <Skeleton count={8} />
      </Card>
    );
  if (initialError)
    return (
      <Card>
        <ErrorState
          title="Company workspace could not be loaded"
          description="Projects or company documents are temporarily unavailable."
          onRetry={() => {
            void projects.refetch();
            void companyArticles.refetch();
          }}
        />
      </Card>
    );

  return (
    <div className="page-stack company-page">
      <PageHeader
        eyebrow="Onboarding"
        title="Documents"
        description="Company and project documents that new hires read during onboarding. Add a PDF; once its security scan is clean you can put it in a package."
        actions={
          <Button onClick={() => setUploadOpen(true)}>
            <Upload size={17} /> Add document
          </Button>
        }
      >
        <OnboardingWorkspaceTabs />
      </PageHeader>

      <div className="stats-grid company-stats">
        <Card className="stat-card">
          <span className="stat-card__icon">
            <FileText size={20} />
          </span>
          <div>
            <p>Company documents</p>
            <strong>{companyDocuments.length}</strong>
            <small>Shared with every employee</small>
          </div>
        </Card>
        <Card className="stat-card">
          <span className="stat-card__icon stat-card__icon--blue">
            <FolderKanban size={20} />
          </span>
          <div>
            <p>Projects</p>
            <strong>{projects.data?.length ?? 0}</strong>
            <small>Projects visible to HR</small>
          </div>
        </Card>
        <Card className="stat-card">
          <span className="stat-card__icon stat-card__icon--purple">
            <Building2 size={20} />
          </span>
          <div>
            <p>Project documents</p>
            <strong>{projectDocuments.length}</strong>
            <small>Only for project members</small>
          </div>
        </Card>
        <Card className="stat-card">
          <span className="stat-card__icon stat-card__icon--amber">
            <FileCheck2 size={20} />
          </span>
          <div>
            <p>Ready to use</p>
            <strong>{onboardingReady}</strong>
            <small>Scanned and can go in a package</small>
          </div>
        </Card>
      </div>

      <Card className="company-section">
        <header className="company-section__header">
          <div>
            <span className="company-section__icon">
              <FileText size={20} />
            </span>
            <div>
              <h2>Company documents</h2>
              <p>Policies, how the company works, and shared procedures. Every employee can read these.</p>
            </div>
          </div>
          <Button size="small" variant="secondary" onClick={() => setUploadOpen(true)}>
            <Plus size={15} /> Add company document
          </Button>
        </header>
        {companyDocuments.length ? (
          <div className="company-document-list">
            {companyDocuments.map(({ article, document }) => (
              <DocumentRow article={article} document={document} key={document.id} />
            ))}
          </div>
        ) : (
          <EmptyState
            title="No company documents yet"
            description="Add your first company document as a PDF. You do not need to retype its content."
            actionLabel="Add company document"
            onAction={() => setUploadOpen(true)}
          />
        )}
      </Card>

      <section className="company-projects">
        <div className="section-heading">
          <div>
            <h2>Projects and their documents</h2>
            <p>Documents only members of that project need to read.</p>
          </div>
          <Link to="/projects">
            Manage projects <ArrowRight size={15} />
          </Link>
        </div>
        {projects.data?.length ? (
          <div className="company-project-grid">
            {projects.data.map((project) => {
              const articles = projectArticles.filter((article) => article.projectId === project.id);
              const files = articles.flatMap((article) =>
                (documentsByArticle.get(article.id) ?? []).map((document) => ({ article, document }))
              );
              return (
                <Card className="company-project-card" key={project.id}>
                  <header>
                    <span>
                      <FolderKanban size={20} />
                    </span>
                    <div>
                      <small>
                        {project.code} · {project.departmentName ?? 'Cross-functional'}
                      </small>
                      <h3>{project.name}</h3>
                    </div>
                    <Badge tone={project.status === 'Active' ? 'positive' : 'neutral'}>{project.status}</Badge>
                  </header>
                  <p>{project.purpose}</p>
                  <div className="company-project-card__meta">
                    <span>
                      <Users size={14} /> {project.members.length} members
                    </span>
                    <span>
                      <FileText size={14} /> {files.length} documents
                    </span>
                  </div>
                  {files.length ? (
                    <div className="company-project-files">
                      {files.slice(0, 4).map(({ article, document }) => (
                        <div key={document.id}>
                          <FileText size={15} />
                          <span>
                            <strong>{document.displayName}</strong>
                            <small>{article.category}</small>
                          </span>
                          <Badge tone={documentTone(document.status)}>
                            {document.status === 'PendingScan' ? 'Scanning' : document.status}
                          </Badge>
                        </div>
                      ))}
                      {files.length > 4 ? <small>+{files.length - 4} more documents</small> : null}
                    </div>
                  ) : (
                    <div className="company-project-empty">No documents for this project yet.</div>
                  )}
                  <footer>
                    <Button size="small" variant="secondary" onClick={() => setUploadOpen(true)}>
                      Add document
                    </Button>
                    <Link to={`/projects/${project.id}`}>
                      Open project <ArrowRight size={14} />
                    </Link>
                  </footer>
                </Card>
              );
            })}
          </div>
        ) : (
          <Card>
            <EmptyState
              title="No projects yet"
              description="Create a project first, then attach its PDF knowledge from this page."
            />
          </Card>
        )}
      </section>

      <Card className="company-onboarding-link">
        <span>
          <BookOpenCheck size={24} />
        </span>
        <div>
          <h2>Next: put documents in a package</h2>
          <p>
            Documents with a clean scan can be added to a company or project package. The package decides what a new
            hire reads, and its quiz checks they understood it.
          </p>
        </div>
        <Link className="button button--secondary button--medium" to="/onboarding/packages">
          Go to packages & quizzes <ArrowRight size={15} />
        </Link>
      </Card>

      <PdfUploadDialog
        open={uploadOpen}
        projects={projects.data ?? []}
        loading={uploadPdf.isPending}
        onClose={() => setUploadOpen(false)}
        onUpload={(input) => uploadPdf.mutate(input)}
      />
    </div>
  );
}

function DocumentRow({ article, document }: { article: KnowledgeArticle; document: EmployeeDocument }) {
  return (
    <div className="company-document-row">
      <span className="company-document-row__icon">
        <FileText size={19} />
      </span>
      <div>
        <strong>{document.displayName}</strong>
        <small>
          {article.category} ·{' '}
          {article.audience === 'AllEmployees'
            ? 'All employees'
            : article.audience === 'HrOnly'
              ? 'HR only'
              : 'Managers'}
        </small>
      </div>
      <Badge tone={documentTone(document.status)}>
        {document.status === 'PendingScan' ? 'Scanning' : document.status}
      </Badge>
      <Badge tone={article.status === 'Published' ? 'positive' : 'warning'}>{article.status}</Badge>
      <div className="row-actions">
        <Link className="button button--ghost button--small" to={`/knowledge/articles/${article.id}`}>
          View
        </Link>
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
  );
}

function PdfUploadDialog({
  open,
  projects,
  loading,
  onClose,
  onUpload
}: {
  open: boolean;
  projects: Awaited<ReturnType<typeof getProjects>>;
  loading: boolean;
  onClose: () => void;
  onUpload: (input: PdfUploadInput) => void;
}) {
  const [file, setFile] = useState<File | null>(null);
  const [destination, setDestination] = useState<PdfDestination>('company');
  const [category, setCategory] = useState('Company information');
  const [audience, setAudience] = useState<KnowledgeAudience>('AllEmployees');
  const close = () => {
    setFile(null);
    setDestination('company');
    setCategory('Company information');
    setAudience('AllEmployees');
    onClose();
  };
  return (
    <Dialog
      open={open}
      onClose={close}
      title="Add a document"
      description="Choose the PDF and where it belongs. The title is taken from the file name, so no duplicate article writing is required."
      width="large"
    >
      <form
        className="form-stack"
        onSubmit={(event) => {
          event.preventDefault();
          if (file) onUpload({ file, destination, category, audience });
        }}
      >
        <FieldShell
          label="PDF file"
          htmlFor="companyPdf"
          required
          hint={
            file
              ? `Knowledge title: ${titleFromFile(file.name)}`
              : 'PDF only. The existing malware scan and immutable document storage are used.'
          }
        >
          <Input
            id="companyPdf"
            type="file"
            accept="application/pdf,.pdf"
            required
            onChange={(event) => setFile(event.target.files?.[0] ?? null)}
          />
        </FieldShell>
        <div className="form-grid">
          <FieldShell label="Belongs to" htmlFor="pdfDestination" required>
            <Select
              id="pdfDestination"
              value={destination}
              onChange={(event) => {
                setDestination(event.target.value);
                setCategory(event.target.value === 'company' ? 'Company information' : 'Project documentation');
              }}
            >
              <option value="company">Company — shared knowledge</option>
              {projects.map((project) => (
                <option value={project.id} key={project.id}>
                  {project.name} — project knowledge
                </option>
              ))}
            </Select>
          </FieldShell>
          <FieldShell label="Category" htmlFor="pdfCategory" required>
            <Select id="pdfCategory" value={category} onChange={(event) => setCategory(event.target.value)}>
              {destination === 'company' ? (
                <>
                  <option>Company information</option>
                  <option>Policy</option>
                  <option>People process</option>
                  <option>Operations</option>
                  <option>Security and compliance</option>
                </>
              ) : (
                <>
                  <option>Project documentation</option>
                  <option>Architecture</option>
                  <option>Development setup</option>
                  <option>Process and workflow</option>
                  <option>Product guide</option>
                </>
              )}
            </Select>
          </FieldShell>
        </div>
        {destination === 'company' ? (
          <FieldShell label="Who can use this document" htmlFor="pdfAudience" required>
            <Select
              id="pdfAudience"
              value={audience}
              onChange={(event) => setAudience(event.target.value as KnowledgeAudience)}
            >
              <option value="AllEmployees">All employees</option>
              <option value="Managers">Managers</option>
              <option value="HrOnly">HR only</option>
            </Select>
          </FieldShell>
        ) : (
          <div className="company-upload-note">
            <FolderKanban size={18} />
            <span>
              <strong>Project members only</strong>
              <small>Project documents inherit project access. Other employees cannot retrieve them.</small>
            </span>
          </div>
        )}
        <div className="form-actions">
          <Button type="button" variant="secondary" onClick={close}>
            Cancel
          </Button>
          <Button type="submit" loading={loading} disabled={!file}>
            <Upload size={16} /> Upload and publish
          </Button>
        </div>
      </form>
    </Dialog>
  );
}
