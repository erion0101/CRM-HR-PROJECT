# Local-First AI HR Management System Architecture

## Product objective

This project is an employee-centered HR system of record inspired by useful structural ideas found in modern HR platforms, without copying BambooHR. The implemented product manages employees, organization structure, positions, effective-dated employment and reporting history, contracts, compensation, documents, user access, audit history, company/project knowledge, project memberships, and personalized onboarding journeys. It also contains a read-only, permission-aware AI assistant over supported structured HR records, onboarding/project context, published knowledge, and explicitly approved documents. Skills, training, certifications, performance, notifications, exports, and advanced reporting remain future modules and are not represented as completed structured capabilities.

The system runs locally. PostgreSQL, Redis, and ClamAV run in Docker; PDF/DOCX originals remain on the host filesystem. The React client is served by a local Nginx container and calls the API through a same-origin proxy. No application or document-storage cloud infrastructure is used. When enabled, the backend makes outbound requests to the OpenAI Responses and Embeddings APIs; the browser never communicates with OpenAI directly. The AI assistant remains unavailable unless provider, pricing, and governance gates all pass.

## Clean Architecture

```text
API ───────────────┐
Worker ────────────┼──> Application ──> Domain
Infrastructure ────┘         ↑
       └──────────────────────┘ implements ports
```

- **Domain** contains entities, value objects, enums, invariants, and domain behavior. It has no dependency on other solution projects.
- **Application** contains feature-oriented CQRS use cases, MediatR commands and queries, handlers, FluentValidation validators, DTOs, provider-neutral ports, and application errors. It depends only on Domain and framework-neutral application packages.
- **Infrastructure** implements persistence, Identity, local storage, malware scanning, auditing, caching, and application contracts.
- **API** owns HTTP routing, cookies, antiforgery validation, authorization policies, Problem Details, OpenAPI, and middleware.
- **Worker** polls pending document versions and performs signature validation, hashing, duplicate detection, ClamAV scanning, and atomic finalization. It also processes approved AI-index jobs and purges expired AI conversation content.

Architecture tests enforce the dependency direction. Controllers contain only HTTP translation and MediatR dispatch. The solution deliberately avoids generic repositories, AutoMapper, arbitrary SQL, and infrastructure access from Application.

### Source organization

Hand-authored types use one type per file. Domain entities and enums live in dedicated folders. Application is organized by feature, with each use case owning its command or query, handler, optional validator, and response DTO. Infrastructure contains focused persistence repositories and one EF Core configuration per entity. API middleware and controllers are split by responsibility. Generated EF migration designer and snapshot files are the only intentional exception to hand-authored file organization.

```text
Application/Features/Employees/
├── Commands/
│   ├── CreateEmployee/
│   │   ├── CreateEmployeeCommand.cs
│   │   ├── CreateEmployeeCommandHandler.cs
│   │   └── CreateEmployeeCommandValidator.cs
│   └── UpdateEmployee/
└── Queries/
    ├── GetEmployeeById/
    └── GetEmployees/
```

MediatR pipeline execution is ordered as logging, performance measurement, validation, then the use-case handler. Logging records request type and correlation metadata without serializing command bodies. Performance warnings are emitted for handlers taking at least 500 ms. FluentValidation failures are collected by field and translated to RFC 9457 responses by API middleware. Domain entities remain responsible for intrinsic invariants, while handlers coordinate cross-aggregate rules such as effective-date overlap checks.

Repositories are feature-specific rather than generic. They expose aggregate operations and purpose-built queries, never `IQueryable`. Commands load tracked aggregates and save through `IUnitOfWork`; queries use stable, scoped projections. The API and Worker are composition roots: `AddApplication()` registers handlers, validators, and behaviors; `AddInfrastructure()` registers EF Core, Identity, repositories, files, ClamAV, audit, Redis, and health checks; `AddApi()` registers controllers and HTTP policies.

## Current domain

All business records include an organization identifier, UUIDv7 identity, UTC audit timestamps, and an optimistic-concurrency version.

```text
Organization
 ├── Organization unit hierarchy
 │    └── Business unit → Division → Department → Team
 ├── Position
 ├── Employee
 │    ├── Employment history
 │    ├── Position assignments ──> Department + Position
 │    ├── Reporting lines ────────> Manager Employee
 │    ├── Contracts ──────────────> Immutable contract revisions
 │    ├── Compensation history
 │    └── Document associations ──> Immutable document versions
 ├── User accounts ───────────────> optional Employee
 ├── User scopes
 ├── Audit events
 ├── Knowledge articles
 │    └── Immutable revisions + secure document associations
 ├── Projects
 │    ├── Effective-dated memberships
 │    └── Resource links + project knowledge
 ├── Onboarding templates
 │    └── Immutable versions + ordered step definitions
 ├── Onboarding assignments
 │    └── Snapshot steps + policy acknowledgments
 └── AI data
      ├── Conversations and messages
      ├── Source references and tool executions
      ├── Usage records
      └── Document AI indexes and chunks
```

The existing `Department` aggregate remains the persistence boundary for typed organization units. It retains the self-referencing Business Unit, Division, Department, and Team model so historical records and future hierarchy expansion are not destroyed. Legacy hierarchy rows remain internally available and are not destructively migrated.

The current product experience exposes exactly two active departments: **IT** and **HR**. The underlying organization-unit model remains extensible and historical hierarchy rows are preserved, but creation of a third active department is rejected. `/departments/current?departmentId={id}` returns the selected department, scoped current employees, positions, employment status, and active project memberships. The React page provides a simple IT/HR switch instead of exposing business units, divisions, nested departments, or teams.

Positions belong to one department. IT owns Intern Software Engineer, Junior Software Engineer, Mid-Level Software Engineer, Senior Software Engineer, DevOps Engineer, IT Specialist, Managing Director, and Product Manager. HR owns HR Intern, Junior HR, Mid-Level HR, and Senior HR. Position creation and editing require a department, assignment commands validate the department-position pair, and assignment forms filter positions immediately after the department is selected. Existing position and assignment history is retained; legacy positions without a department are excluded from active catalogs and new assignments while remaining available to historical records and audit data.

Each department owns its Manager assignment. IT employees report to the configured IT Manager and HR employees report to the configured HR Manager; users cannot choose an arbitrary manager from the employee profile. Changing an employee's department or position takes effect on the current day, closes the prior effective-dated record, preserves its history, and synchronizes the reporting line to the destination department manager. The employee profile deliberately hides position and manager date inputs because those dates are controlled by this workflow. Department managers never report to themselves.

Departments may also store HR and Product Manager leadership references where those responsibilities apply. Leadership references are unique within a department. HR can edit department details and leadership, add or transfer an existing employee through position assignments, and preserve all prior business history without destructive updates.

Employment, reporting, assignments, contracts, and compensation are effective-dated. Overlapping records are rejected when only one active record is valid. Compensation is separate from contracts so it can have stricter authorization. Uploaded originals are immutable; replacement means a new document version.

## Local topology

```text
React client (Nginx)
        |
ASP.NET Core API ─── PostgreSQL + pgvector
        |            Redis
        |            Local data/ directory
        |
.NET Worker ──────── ClamAV

API/Worker ───────── OpenAI Responses + Embeddings APIs
```

Docker Compose builds the API and worker with the .NET 10 SDK and runs them on .NET 10 runtime images. The API binds to localhost. A one-shot migration container applies EF Core migrations and seeds the four roles, the local organization, and the configured bootstrap HR account before the API starts.

## Authentication and authorization

ASP.NET Core Identity stores local accounts in PostgreSQL. Authentication uses an HTTP-only same-site cookie. There is no public registration. Unsafe authenticated requests require an antiforgery token, and updates require an `If-Match` version.

The application has exactly four Identity roles. Endpoint policies define permitted operations, while the query layer applies record scope:

- `Employee` is self-service: personal HR records, documents, onboarding, company knowledge, and assigned projects.
- `Manager` includes Employee access plus current direct-report visibility and team-onboarding responsibility.
- `HR` has organization-wide people, organization, compensation, document, user, audit, onboarding, knowledge, and AI-administration access.
- `ProductManager` manages projects, project membership, and project knowledge without access to compensation or private HR records for other employees.

Legacy `Admin`, `HRAdmin`, and `HREmployee` assignments are migrated to `HR` during database initialization and the obsolete roles are removed. Product Manager is a first-class authorization role displayed as “Product Manager” in React; it remains distinct from the department’s Product Manager leadership assignment.

Development may optionally seed four linked demonstration employees and accounts when `DEMO_USERS_PASSWORD` is configured: `erion.employe@gmail.com` (Employee), `erion.manger@gmail.com` (Manager), `erion.hr@gmail.com` (HR), and `erion.pm@gmail.com` (Product Manager). This seed is idempotent, assigns active employment and positions, connects reporting lines and department leadership, and is disabled outside Development. The shared demo password bypasses the production 12-character creation policy only inside this explicit local seed path and must never be configured in production.

This filtering applies to list and direct-ID access, contracts, compensation, document metadata, document downloads, AI tools, conversation history, and document retrieval. Frontend visibility is never treated as an authorization boundary.

Employee profile reads are display-oriented projections rather than client-side joins. `/employees/{id}/profile` returns the scoped employee summary, current organization assignment, current manager, hire date, and accessible direct reports. Separate scoped endpoints expose effective-dated employment, position assignment, and reporting-line history. These projections preserve the domain history and keep authorization in the query layer.

The dashboard uses a server-side scoped aggregate rather than counting a fetched page. `/dashboard/summary` returns authoritative visible, active, on-leave, setup-required, and active-organization-unit totals plus recent accessible employees. `/employees/{id}/activity` resolves the employee's related employment, assignment, reporting, contract, compensation, and document audit records. It applies employee scope first and omits compensation activity unless the caller is HR.

## Document processing

Only PDF and DOCX files up to the configured limit are accepted. An upload is written under a generated identifier in quarantine and gets a pending database record. The worker:

1. Validates the real file signature.
2. Calculates SHA-256.
3. Detects duplicate content inside the organization.
4. Streams the file to local ClamAV.
5. Deletes rejected or infected quarantine files.
6. Atomically moves clean files into immutable permanent storage.

Storage keys are generated internally and resolved under one configured root. Absolute keys and traversal outside the root are rejected. Files are never exposed as static web content; every download passes through employee-scope authorization and creates an access audit event.

Clean, explicitly approved PDF and DOCX versions are extracted locally and chunked with page/paragraph provenance. Image-only PDFs are marked `NeedsOcr`; OCR remains deferred. Approval, classification, scan status, and AI-index status are independent lifecycles. Index jobs use a database lease, bounded batches, and a maximum of five attempts. Reprocessing replaces chunks for the selected immutable document version.

## AI assistant and local RAG design

The integration uses an OpenAI Platform project API key supplied only to the backend as `OPENAI_API_KEY`, or preferably through the read-only `OPENAI_API_KEY_FILE` Docker secret. It is not a separate Codex subscription key. The key never reaches React, source control, API responses, or logs.

Original documents remain local. PdfPig and Open XML extract text locally; only minimum authorized text selected by retrieval is sent to OpenAI. OCR is not implemented. Embeddings and extracted chunks are stored locally in PostgreSQL through `pgvector`.

```text
Question
  -> authenticate and resolve scope
  -> classify intent
  -> authorized structured query and/or document retrieval
  -> minimum evidence sent to the model
  -> citation and authorization validation
  -> answer plus locally stored trace
```

Exact HR facts—contract dates and statuses, employment dates and status, organization assignments, reporting relationships, and authorized employee profile fields—must come from structured queries. Narrative experience, clauses, policies, and document summaries use lexical/vector retrieval. Structured records win when sources conflict.

The model selects only sixteen allowlisted, read-only tools and never executes arbitrary SQL:

1. `search_employees`
2. `get_employee_profile`
3. `get_employee_history`
4. `get_direct_reports`
5. `get_organization_structure`
6. `search_contracts`
7. `get_employee_contracts`
8. `list_employee_documents`
9. `search_employee_documents`
10. `get_my_onboarding`
11. `get_my_onboarding_progress`
12. `get_assigned_projects`
13. `get_project_overview`
14. `get_project_team`
15. `search_company_knowledge`
16. `search_project_knowledge`

Employee scope is centralized in `IAuthorizedDataScope` and reused by normal APIs and AI queries. Document instructions are untrusted content. Canonical structured result rows and source links come from server-side tool output rather than model-generated rows. AI remains read-only until separately designed human-confirmed mutation workflows exist.

Conversation prompts, sanitized final answers, canonical result snapshots, and source references are retained locally for 90 days. Retrieved chunks, raw provider payloads, reasoning, and complete tool outputs are not persisted. An authorization fingerprint is checked whenever a conversation is continued or read; changed access causes stored content to be redacted. Provider requests use the Responses API with `store:false`, while the governance documentation explicitly notes that this setting alone is not equivalent to Zero Data Retention.

Documents default to `Sensitive` and cannot be indexed. HR may approve only `NormalInternal` or `Private` content after malware scanning. The worker extracts with PdfPig/Open XML, creates approximately 600-word chunks with overlap, generates 1,536-dimensional embeddings, and stores the index in PostgreSQL. Revocation and archive paths remove or exclude chunks immediately. Full-text and pgvector indexes remain local; original binaries are never sent to the provider.

The assistant is controlled by two separate launch gates: `Ai:Enabled` and `Ai:RealDataApproved`. A missing provider key disables only AI and never core readiness. Production activation additionally requires an HR-AI DPIA/legal review, approved data classifications, provider processing terms and retention posture, HTTPS, encrypted storage/backups, and production authentication controls.

### React assistant experience

The React feature lives under `features/ai-assistant/` and exposes `/ai-assistant`, `/ai-assistant/{conversationId}`, and the HR-only `/administration/ai-settings` route. The primary navigation always exposes the assistant so disabled-state guidance is discoverable.

The chat workspace contains local conversation history, New Chat, an immediately visible composer, Enter-to-send, Shift+Enter for a new line, cancellation, capability-driven suggestions, evidence labels, canonical structured results, and expandable source links. Sending the first message creates the conversation automatically. The browser includes cookies and an antiforgery token and consumes a same-origin SSE response through `fetch()`.

The current API transport emits `start`, `status`, `delta`, `result`, `sources`, `done`, and `error` events, with Nginx buffering disabled for the message route. The provider call itself is currently buffered: after orchestration finishes, the API emits the complete narrative in one `delta` event. True token-by-token provider streaming remains a hardening task.

The HR administration page reports enablement, governance, credential presence, configured models, quotas, retention, daily token/request totals, monthly cost, and provider connection-test results. Provider credentials cannot be entered or retrieved in React.

## Employee Onboarding and Knowledge Management

This module is implemented as three new feature boundaries—Knowledge, Projects, and Onboarding—plus seven additions to the existing AI tool registry. It stays inside the modular monolith and reuses the existing scoped authorization, document lifecycle, audit interceptor, Worker indexing pipeline, pgvector retrieval, and React design system.

The module turns the platform into a personal onboarding guide for each new employee. An employee must be able to understand the company, their department, team, project, responsibilities, and first-day/first-week/first-month expectations. AI improves discovery and explanation, but it never creates canonical company facts, project instructions, assignments, deadlines, or completion state.

The module has four connected capabilities:

1. **Company Knowledge Center:** approved explanations of the organization, products, services, departments, culture, policies, and operating procedures.
2. **Project Knowledge Center:** project purpose, architecture, systems, repositories, workflows, setup instructions, team membership, and responsibilities.
3. **Personalized Onboarding Journeys:** reusable versioned templates assigned according to employee, role, organization unit, and project, with immutable assignment snapshots and explicit progress.
4. **AI Onboarding Assistant:** permission-aware answers based only on published structured onboarding data and approved knowledge sources, with citations.

Company knowledge, project knowledge, onboarding definitions, and employee progress are separate persistence concepts. They are connected through identifiers and application use cases, not collapsed into one generic content table or one large document.

### Domain model

All new records carry `OrganizationId`, UUIDv7 identity, UTC timestamps, and optimistic concurrency where they are editable.

```text
Organization
 ├── Company knowledge articles
 │    └── Immutable article revisions
 │         └── Approved attachments
 ├── Projects
 │    ├── Project memberships ───────> Employee + project responsibility
 │    └── Project knowledge articles
 │         └── Immutable article revisions + approved attachments
 ├── Onboarding templates
 │    └── Immutable template versions
 │         ├── Ordered modules and step definitions
 │         └── Reviewed, immutable quiz version
 │              └── Questions, four answer options, answer key, and source provenance
 └── Employee onboarding journeys
      ├── Snapshot of one template version
      ├── Employee, manager, mentor, role, unit, and project context
      ├── Ordered assigned steps
      ├── Reading cycles and per-document version progress
      ├── Server-side quiz attempts, fixed question order, and answers
      └── Completion, attempt, reset, and activity history
```

Implemented aggregates and persistence concepts:

- `KnowledgeArticle` owns slug, audience, content owner, publication state, ordering, and optional department/project context. Immutable `KnowledgeArticleRevision` rows own title, description, category, Markdown content, author, and change summary. The same secure aggregate supports company and project knowledge without weakening their different read rules.
- `Project` owns code, name, purpose, lifecycle status, project lead, and memberships. It does not become a general ticket or sprint-management system.
- `ProjectMembership` links an employee to a project with a project role, responsibility summary, effective dates, and access state.
- `ProjectResourceLink` stores labelled references such as source repositories and task boards; it does not grant access to the external system.
- `OnboardingTemplate` owns reusable definitions. Publishing creates an immutable `OnboardingTemplateVersion` containing ordered sections and steps.
- `OnboardingTemplateStep` supports article reading, approved-document reading, external resources, practical tasks, policy acknowledgments, and manager-reviewed milestones. It may reference a published knowledge revision or knowledge document but does not copy document binaries.
- `OnboardingAssignment` is the employee-specific journey and snapshots the chosen template version and resolved steps so later template edits do not rewrite history.
- `OnboardingStepProgress` owns status, employee notes, completion time, verification actor/time, and safe evidence metadata. It never treats an AI response as proof of completion.
- `OnboardingAcknowledgment` records the exact published article revision acknowledged by an employee. Meaningful mutations are also captured by the existing append-only audit subsystem.
- `OnboardingReadingCycle` is the authoritative assessment phase for one assignment. `OnboardingReadingProgress` snapshots every assessed step plus its exact document version or knowledge revision and records `Unread`, `InProgress`, or `Read` with timestamps.
- `OnboardingQuiz` is a reviewed quiz for one immutable template version. `OnboardingQuizQuestion` and `OnboardingQuizAnswerOption` retain topic, difficulty, explanation, source document/revision provenance, and the protected answer key. Drafts are editable; publication freezes the assessment used by new assignments.
- `OnboardingQuizAttempt` belongs to an assignment and reading cycle. `OnboardingQuizAttemptQuestion` snapshots selected question and option order, and `OnboardingQuizAnswer` stores the submitted selection and grading result. Employee DTOs never serialize `IsCorrect`.

Only one active journey per employee and onboarding program is permitted unless an authorized user explicitly creates a new program cycle. Template versions, published quizzes, assigned document versions, and published knowledge revisions are immutable. Draft content may be edited; changing published content creates a new revision. Archived knowledge remains available to historical journey snapshots but is excluded from new assignments and current AI retrieval.

### Knowledge content and file boundaries

Company and project knowledge use structured articles as their primary source. Articles use sanitized Markdown or a similarly constrained rich-text representation; scripts, arbitrary HTML, embedded credentials, and executable content are rejected. Each published revision records its author, reviewer, publication time, content hash, and classification.

The existing secure document pipeline may be reused for attachment binaries, malware scanning, hashing, immutable version storage, PDF/DOCX extraction, and indexing. Ownership and authorization are not reused from employee documents:

- Company knowledge attachments are associated with a company article revision and its audience.
- Project knowledge attachments are associated with a project article revision and require active project access.
- Employee documents remain employee records and do not become company or project knowledge merely because they were uploaded.
- Moving content between these areas requires an explicit authorized publishing workflow, classification review, and a new association.

Knowledge attachments use the existing `Document` and immutable `DocumentVersion` storage through new `CompanyKnowledge` and `ProjectKnowledge` association types. They remain inaccessible through static paths. Downloads and AI retrieval reauthorize the article, audience, project membership, classification, and publication state on every request.

### Authorization and publication

The four Identity roles are Employee, Manager, HR, and Product Manager. Fine-grained onboarding capabilities come from role, employee scope, organization-unit scope, project membership, and explicit resource responsibility:

- HR manages company knowledge, templates, assignments, reporting, and access administration for the organization.
- Product Managers manage projects, project memberships, and project knowledge without organization-wide HR access.
- Managers may view and update the journeys of current direct reports, complete manager-owned verification steps, and assign an approved template when policy allows.
- Project leads manage only their projects, memberships, and project knowledge; this responsibility does not grant organization-wide HR access.
- Mentors are displayed as onboarding contacts. Manager-reviewed milestones are approved by the assigned manager or authorized scoped HR roles; mentorship does not grant general HR-record access.
- Employees read published company knowledge allowed for their audience, published knowledge for their active projects, and only their own onboarding journey and progress.

Drafts, review notes, inactive projects, unpublished revisions, and other employees' progress never appear in employee search or AI retrieval. Direct-ID requests return a non-enumerating not-found response when the caller lacks access. Every query includes organization scope; project and journey foreign keys are validated as belonging to the same organization.

Publishing is an explicit state transition: `Draft -> InReview -> Published -> Archived`. Templates follow `Draft -> Published -> Retired`. Assignments support `NotStarted`, `InProgress`, `Blocked`, `PendingReview`, `Completed`, `Cancelled`, and `Superseded`; steps support the first five applicable states. Formal separation of author and publisher is not enabled in this local milestone.

### Personalized journey behavior

Templates can target an organization unit and position. HR or an authorized manager explicitly selects and assigns a published template; AI never silently assigns or changes a journey. Automatic template suggestions are deferred.

When assigned, the journey snapshots:

- employee, manager, mentor, and optional project context;
- the published template version and ordered steps;
- relative due-date rules resolved to calendar dates from the employee's start date;
- referenced knowledge revision identifiers;
- the responsible party for each step.

Progress is derived from stored step states, not generated text. Percent complete is the number of completed required steps divided by required steps; optional steps do not change that percentage. Due dates are resolved from `DateOnly` start dates plus relative day offsets and returned as structured values. Assignment cancellation and manager milestone review are concurrency-protected and audited. Reopening, waiver, custom weights, and due-date overrides are deferred.

Employees can complete the entire journey without AI by opening articles, attachments, links, instructions, and checklists directly. If OpenAI is unavailable, governance is disabled, quotas are exhausted, or an index is not ready, knowledge browsing and progress updates continue normally.

### Mandatory reading and assessment invariant

The assessment phase is backend-authoritative and follows one strict transition graph:

```text
All required items Unread/InProgress
        -> quiz Locked
all required items Read in current cycle
        -> quiz Available
start attempt
        -> quiz InProgress (published quiz and order fixed)
submit -> score >= threshold -> assessment Passed
       -> score < threshold  -> old cycle Failed
                              -> new cycle created
                              -> every reading item Unread
                              -> reading progress 0%
                              -> quiz Locked
```

Each `ReadDocument` template step points at a clean company/project knowledge document. Assignment snapshots the current immutable `DocumentVersion`; replacements do not silently change an active employee's assessment. Opening the secure download changes an item to `InProgress` only when the employee explicitly performs that action in the UI. `Read` is an explicit acknowledgment persisted by the API. Optional items are tracked but do not block the quiz.

Starting an attempt rechecks assignment ownership, active cycle, published quiz, absence of another in-progress attempt, and completion of every required reading item. The server chooses the reviewed question pool, fixes question and option order, and copies the passing threshold. Correct-answer flags are available only to administrative quiz DTOs and server-side grading.

Submitting rechecks the authenticated employee, concurrency version, complete answer set, attempt/cycle state, and required-reading gate. Passing at exactly the configured percentage is a pass. Failure is a single unit-of-work transaction: finalize the scored attempt, fail the old cycle, reset assessed step states, create the next cycle with a fresh `Unread` row for every item, and move the assignment back to `Locked`. Historical cycles, attempts, answers, and source versions remain append-only. Duplicate submission is rejected by attempt state and optimistic concurrency; a failed request cannot leave a partially reset cycle.

Passing completes only the reading-and-assessment phase. The assignment becomes fully complete only after any remaining required practical or manager-reviewed steps are also complete.

### AI-generated onboarding quizzes

HR creates a quiz draft for a published template and configures its title, question count, passing percentage, and question/option randomization. Manual authoring always remains available. When AI generation is selected, the handler:

1. enforces the existing AI enablement, real-data approval, credential, quota, and budget gates;
2. loads only clean, explicitly AI-approved `NormalInternal` or `Private` PDF/DOCX chunks referenced by required template documents or their knowledge articles;
3. labels extracted content as untrusted evidence and sends no original binaries;
4. requests a bounded JSON question set through the provider-neutral `IAiProvider`;
5. requires four non-empty options and exactly one correct option for every question;
6. validates every cited source identifier against evidence supplied in that request; and
7. stores the result as an editable `AiDraft`, never as a published quiz.

HR reviews sources, answers, and explanations, may add/edit/delete questions, and explicitly publishes. Image-only PDFs and failed/not-ready indexes are unavailable as generation evidence rather than being treated as successful sources. Runtime attempts draw only from this reviewed published pool; they never call AI to invent questions.

### Onboarding AI and retrieval

The onboarding assistant extends the existing provider-neutral orchestrator and fixed tool registry. It does not introduce a separate AI service or permit arbitrary queries. Implemented read-only tools are:

1. `get_my_onboarding`
2. `get_my_onboarding_progress`
3. `get_assigned_projects`
4. `get_project_overview`
5. `get_project_team`
6. `search_company_knowledge`
7. `search_project_knowledge`

Structured questions such as “What should I complete this week?”, “Who is my mentor?”, “Who is my project lead?”, and “Which steps are overdue?” come from journey, employee, reporting, and project queries. Explanations such as “How does authentication work in my project?” use approved article revisions and indexed attachments. The model receives only the minimum evidence authorized for the current employee.

Knowledge indexing is separate from employee-document indexing. Published `NormalInternal` content may be proposed for indexing; `Private` content requires explicit AI approval. `Sensitive` and `HighlyRestricted` content is unavailable to AI. Credentials, secrets, private repository tokens, production connection strings, and customer secrets are prohibited from knowledge content regardless of classification.

Each AI source reference identifies the canonical article revision, project, onboarding step, or attachment chunk. The server validates every citation against evidence returned during that turn. Instructions found inside knowledge content remain untrusted data and cannot widen permissions, invoke writes, mark steps complete, or override system rules. When indexed content conflicts with current structured journey/project data, structured data wins and the conflict is shown with both sources.

AI suggestions never mutate onboarding state. Completing a step, acknowledging a policy, changing a due date, assigning a mentor, or adding a project member must use an ordinary authorized API action. Any future AI-assisted write flow requires a separately designed preview, validation, and human-confirmation boundary.

### Application and API surface

Application features follow the existing one-use-case-per-folder CQRS structure. Important command families include draft/revise/review/publish/archive knowledge, create/update/publish/retire templates, create/update projects and memberships, assign/cancel journeys, update/reopen/verify steps, and request/revoke knowledge AI approval. Queries return scoped projections for knowledge browsing, project context, template administration, personal journeys, manager/HR oversight, and onboarding reporting.

Implemented REST endpoints are versioned under `/api/v1`:

```text
GET/POST         /knowledge/articles
GET              /knowledge/articles/{articleId}
POST             /knowledge/articles/{articleId}/revisions
POST             /knowledge/articles/{articleId}/review
POST             /knowledge/articles/{articleId}/publish
DELETE           /knowledge/articles/{articleId}
POST/GET         /knowledge/articles/{articleId}/documents
GET              /knowledge/articles/{articleId}/documents/{documentId}/download
POST             /knowledge/articles/{articleId}/documents/{documentId}/ai-approval

GET/POST         /projects
GET/PUT          /projects/{projectId}
GET/POST/DELETE  /projects/{projectId}/members
GET/POST         /onboarding/templates
PUT              /onboarding/templates/{templateId}
POST             /onboarding/templates/{templateId}/publish
GET/POST         /onboarding/templates/{templateId}/quiz
GET              /onboarding/templates/{templateId}/quiz-sources
POST             /onboarding/quizzes/{quizId}/generate
POST             /onboarding/quizzes/{quizId}/questions
PUT/DELETE       /onboarding/quizzes/{quizId}/questions/{questionId}
POST             /onboarding/quizzes/{quizId}/publish
GET              /onboarding/me
GET/POST         /onboarding/assignments
POST             /onboarding/assignments/{assignmentId}/cancel
PUT              /onboarding/assignments/{assignmentId}/steps/{stepId}
POST             /onboarding/assignments/{assignmentId}/steps/{stepId}/review
PUT              /onboarding/assignments/{assignmentId}/reading/{stepId}
GET              /onboarding/assignments/{assignmentId}/reading/{stepId}/download
POST             /onboarding/assignments/{assignmentId}/quiz-attempts
GET              /onboarding/quiz-attempts/{attemptId}
POST             /onboarding/quiz-attempts/{attemptId}/submit
```

Unsafe requests require antiforgery validation and editable-resource mutations use optimistic-concurrency `If-Match` handling. The current knowledge/project/template collections are intentionally bounded organization lists; cursor pagination should be added before deployment for organizations with large knowledge libraries. Knowledge search, project membership, assignment, progress, and publication actions create safe audit records through the existing EF audit interceptor.

Code is added within the existing deployables and dependency boundaries:

```text
src/HrManagement.Domain/
├── Entities/                         # knowledge, project, and onboarding aggregates
└── Enums/                            # lifecycles, audience, and step types

src/HrManagement.Application/Features/
├── Knowledge/
├── Projects/
└── Onboarding/

src/HrManagement.Infrastructure/
├── Persistence/Configurations/       # one EF configuration per new entity
├── Persistence/Repositories/         # scoped purpose-built repositories
└── Ai/                               # retrieval and new tool handlers

src/HrManagement.Api/Controllers/     # thin versioned HTTP adapters
src/HrManagement.Worker/              # approved knowledge indexing and cleanup
frontend/src/features/                 # knowledge, projects, and onboarding
```

Application owns commands, queries, validation, authorization contracts, and DTOs. Domain owns lifecycle transitions and invariants. Infrastructure owns EF Core, PostgreSQL search/vector queries, storage, and provider adapters. API and Worker remain composition roots. No onboarding feature may reference EF Core, filesystem paths, OpenAI SDK types, or HTTP types from Domain or Application.

### React experience

The frontend feature boundaries are `features/knowledge/`, `features/projects/`, and `features/onboarding/`; the existing `features/ai-assistant/` gains contextual onboarding renderers and prompt suggestions.

Implemented routes:

```text
/knowledge
/knowledge/articles/:articleId
/projects
/projects/:projectId
/onboarding
/onboarding/manage
/onboarding/quiz/:attemptId
```

The employee onboarding home shows welcome context, role and project, manager/mentor/team links, ordered reading modules, current reading cycle, authoritative per-file status, required-reading progress, quiz lock state, attempt history, scored reset messages, later journey stages, due dates, and blockers. Secure document links never expose storage paths. The dedicated quiz route provides navigation, answered/unanswered state, submit confirmation, and result handling without holding the answer key in browser state.

Administrative screens separate knowledge/document upload, security processing and AI approval, template/module design, clean document selection, publication, quiz configuration, AI generation, manual question authoring, answer/source review, assignment, and progress oversight. A template may include one `ReadDocument` step per file so every required PDF/DOCX has independent state and is recreated as `Unread` after failure. UI visibility follows capability responses but all enforcement remains on the server. Mobile layouts retain reading, assessment, and checklist functionality.

### Delivered slices and acceptance criteria

The module was delivered in the following vertical slices:

1. **Domain and security foundation:** add migrations, aggregates, enums, invariants, scoped repositories, permissions, cross-organization validation, audit redaction, and unit/architecture tests.
2. **Company Knowledge Center:** article revisions, review/publication, audience rules, secure attachments, search, and React reading/authoring flows.
3. **Projects and Project Knowledge:** projects, effective memberships, project responsibilities, project articles, access enforcement, and React project pages. Ticketing, sprints, and source-control integrations remain outside scope.
4. **Onboarding journeys:** versioned templates, deterministic assignment proposal, immutable employee snapshots, due dates, progress, verification, manager/mentor workflows, activity history, and dashboards.
5. **Onboarding intelligence:** local article extraction/chunking, explicit AI approval, scoped hybrid retrieval, seven onboarding assistant tools, citations, conflicts, and graceful unavailable states.
6. **Document assessment:** immutable reviewed quizzes, grounded AI drafts, manual fallback, per-version reading cycles, server-side attempts/grading, and mandatory full reset on failure.
7. **Verification:** domain and validation tests, architecture checks, PostgreSQL/API integration tests, TypeScript/ESLint checks, React tests, and production builds.

The implemented functional milestone provides:

- Authorized backend workflows can create, revise, review, publish, and archive company and project knowledge. React currently exposes creation, review, publication, attachments, and reading; revision editing and archive controls remain an administration-UX follow-up.
- An employee can see only published company knowledge for their audience and knowledge for projects where they have active access.
- HR or an authorized manager can assign a published template; later template edits do not alter the assigned journey.
- HR can select clean company/project documents as individually ordered required reading, generate or manually author a quiz, review its protected answer key and sources, and publish the immutable assessment.
- Employees cannot start or submit around the current-cycle reading gate. Failure preserves history while atomically creating the next cycle at 0%, with every required document `Unread` and the quiz locked.
- Employees can complete, acknowledge, and review their own steps while manager/mentor verification follows explicit ownership rules.
- Progress, deadlines, overdue state, and completion are calculated without AI and remain usable during provider outages.
- AI can answer supported onboarding questions using scoped structured data and approved knowledge, with source identifiers validated by the existing orchestrator.
- AI cannot reveal another tenant's content, another employee's progress, inaccessible projects, drafts, secrets, employee documents, or disallowed classifications.
- Disabling AI or revoking indexing approval immediately removes AI access without hiding canonical onboarding content from otherwise authorized users.
- Backend, frontend, migration, and architecture checks pass locally; deterministic onboarding-specific adversarial authorization and retrieval coverage still needs expansion before production activation.

## Security, privacy, and audit

- Local services bind to loopback ports by default.
- Passwords and connection secrets live in ignored environment configuration.
- Sensitive output is excluded from ordinary logs.
- Audit entries capture actor, employee link, action, entity, safe changes, time, correlation identifier, and client address.
- Password hashes, API keys, document binaries, and secrets are excluded from audit payloads.
- Malware scanning failure leaves a document pending for retry rather than making it downloadable.
- PostgreSQL and the filesystem must be backed up and restored as one logical dataset.
- Real employee data should not be used until retention rules, legal bases, data-subject procedures, access reviews, and an HR-AI DPIA are established.

Before deployment beyond localhost, add HTTPS, an external secret manager, MFA/SSO, network controls, encrypted backups, alerting, vulnerability scanning, recovery exercises, and formal GDPR policies.

## Verified implementation status

The following checks passed on 24 October 2026 after the document-assessment implementation:

- .NET Release build: succeeded with zero warnings and zero errors.
- All backend tests: 57 passed (24 Domain, 12 Application, 4 Architecture, and 17 Integration).
- React tests: 12 passed across four test files.
- React ESLint validation passed.
- React TypeScript and Vite production build passed.
- Docker Compose configuration validation and the `api`, `worker`, `migrate`, and `web` image builds passed.
- The integration suite created PostgreSQL containers, applied the full migration chain against an empty database, and exercised API authorization, local storage, ClamAV protocol handling, scoped AI orchestration, structured tools, and authorized document retrieval using deterministic fake AI and embedding providers. Migration `20261024073754_AddOnboardingAssessments` adds reading-cycle, quiz, question/option, attempt/snapshot, and answer persistence.

The official OpenAI adapter is implemented, but a real-provider end-to-end smoke test has not been completed. When `OPENAI_API_KEY` is absent, or either governance flag is disabled, the capability endpoint reports the provider as unavailable and keeps AI chat/quiz generation disabled without affecting core readiness. Manual quiz authoring and the complete onboarding workflow remain available.

## Remaining production work

The current codebase is a strong local functional milestone, not a completed production launch. The following planned items remain incomplete:

- Extend the React multi-step template builder with drag/drop reordering, bulk document-to-step creation, configurable per-module question distribution, and editing of later template versions. It already supports multiple ordered steps, clean approved-document selection, required/optional state, relative due dates, earlier-step dependencies, organization-unit/position targeting, article references, and external resources.
- Add React controls for knowledge revision editing/archive, project update/archive, membership end dates, assignment cancellation, milestone rejection notes, and richer administration filters. The corresponding backend use cases exist except template retirement, step reopening/waiver, and due-date override.
- Render validated Markdown with a sanitized Markdown renderer. The current reader deliberately displays stored Markdown as plain text, which is safe but visually limited.
- Add deterministic template recommendations by department/position; templates already store these targets, but assignment remains explicit.
- Add cursor pagination for large knowledge, project, template, and assignment collections.
- Expand onboarding-specific automated coverage to a full browser-driven ten-document acceptance scenario, every role/scope, cross-tenant direct IDs, project revocation, attachment authorization, AI source reauthorization, lifecycle conflicts, and concurrent duplicate submissions.
- Add organization-timezone configuration for user-facing deadline presentation. Backend due dates are intentionally date-only and therefore do not depend on server-local time.
- Stream Responses API output incrementally instead of emitting the completed answer as one SSE delta.
- Add bounded transient retries, timeout classification, and a provider circuit breaker.
- Replace process-local AI concurrency semaphores and non-atomic cache increments with distributed Redis leases and atomic counters.
- Add `ActivitySource`, `System.Diagnostics.Metrics`, optional OpenTelemetry export, provider/first-token latency, retrieval metrics, and citation-failure metrics without sensitive attributes.
- Add React document-classification, AI approval, indexing-status, failure, and reprocessing controls for the existing backend endpoints.
- Add contextual “Ask AI” entry points to employee, contract, document, and organization screens. Contract and document context IDs also require complete server-side context authorization before these links are enabled.
- Implement contract/employment document association workflows and the contract-search filter for linked signed-document state. Current uploads create employee associations only.
- Expand the automated matrix for every role and scope, cross-tenant IDs and vectors, malicious document instructions, changed authorization, provider refusals/malformed output/outages, SSE cancellation, rate-limit races, extraction provenance, indexing idempotency, retention purge, and restricted-field provider leakage.
- Add an explicit opt-in real OpenAI smoke-test profile; normal CI must continue using deterministic fakes.
- Complete HTTPS, production cookies, MFA/OIDC SSO, secrets management, encrypted storage and backups, access reviews, vulnerability management, restore exercises, and DPIA/legal/retention approval.

The following remain intentional version-one exclusions rather than unfinished advertised features: AI write operations, compensation/bank/performance/medical/government-ID/disciplinary AI access, OCR and image interpretation, XLSX or email ingestion, hosted OpenAI file/vector stores, external web search, Agents API, MCP, voice, WebSockets, tenant BYOK, separate AI microservices, Kafka, Kubernetes, and an external vector database.

## Roadmap

1. **Completed local HR milestone:** local Identity/RBAC, scoped organization and employee management, effective-dated history, contracts, compensation, documents, audit, React client, Docker topology, migrations, and automated tests.
2. **Completed AI functional foundation:** classifications and approval APIs, PDF/DOCX extraction, local chunks/embeddings, OpenAI Responses adapter, sixteen allowlisted tools (nine HR plus seven onboarding/knowledge tools), scoped history, hybrid retrieval, citations, quotas, retention, HR status/usage, and the chat workspace.
3. **Next—AI completion:** true provider streaming, resilient provider calls, distributed quota/concurrency enforcement, AI observability, document-index administration UI, contextual launch actions, association workflows, and the full security/evaluation suite.
4. **Next—production hardening:** HTTPS, OIDC/MFA, secure cookies, secret management, encrypted backups, monitoring, backup/restore validation, access review, DPIA/legal review, and approved provider retention controls.
5. **Completed onboarding functional milestone:** company/project knowledge, secure attachments, projects and effective memberships, versioned templates, assignment snapshots, progress, acknowledgments, manager verification, React employee/administration experiences, approved knowledge indexing, scoped retrieval, and seven onboarding tools.
6. **Completed document-assessment milestone:** direct required-document modules, immutable reviewed quizzes, grounded AI generation plus manual fallback, reading cycles, server-side attempts/grading, strict lock/unlock rules, and mandatory atomic full-reading restart after failure.
7. **Next—onboarding hardening:** bulk/reorder authoring UX, configurable module question distribution and attempt limits, richer reporting/filters, complete tenant/role/revocation/concurrency tests, sanitized Markdown presentation, deadline timezone presentation, and browser-level acceptance automation.
8. **Future HR modules:** training, certifications, skills, performance, notification rules, richer reporting, global search, and exports.
