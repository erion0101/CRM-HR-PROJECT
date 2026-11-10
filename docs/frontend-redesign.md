# HR Platform Frontend Redesign

## Objective

The web application uses one production-oriented application shell, navigation model, spacing system, and set of reusable controls. BambooHR screenshots are used only as product and information-architecture references; the visual identity, data model, and implementation remain specific to PeopleSpring.

## Gap analysis

| Area | Original status | Decision |
|---|---|---|
| Clean Architecture, CQRS, MediatR, validation | Implemented | Preserve existing boundaries and feature folders. |
| Scoped authorization | Implemented | Continue applying scope in backend list and detail queries. |
| Shared shell and UI primitives | Partially implemented | Standardize sidebar groups, top bar, headers, cards, inputs, states, and responsive behavior. |
| Employee directory | Needs refactor | Replace the administrative table with a grouped, relationship-aware directory. |
| Employee profile | Partially implemented | Keep the profile route and effective-dated data; add consistent avatar and org-chart navigation. |
| Manager relationships | Implemented | Use effective-dated `ReportingLine`; do not duplicate `ManagerId` on `Employee`. |
| Organization hierarchy | Implemented | Use hierarchical departments with Business Unit, Division, Department, and Team types. |
| Organization chart | Missing | Build from authorized employees and their current reporting lines. |
| Employee avatars | Missing | Use deterministic illustrated placeholders with initials; add stored profile images in a later backend increment. |
| Local document storage, scan, versions | Implemented | Preserve quarantine, ClamAV, immutable versions, and authorized downloads. |
| Document folders and global workspace | Missing | Add persisted role-aware folders and a cross-employee document workspace. |
| Time off, training, certifications, performance | Missing | Do not expose dead navigation until their domain models and APIs exist. |
| AI assistant and RAG | Deferred | Do not expose a fake assistant. Implement only after the structured HR and extraction APIs exist. |

## Target application structure

```text
AppShell
├── AppSidebar
│   ├── Dashboard
│   ├── People
│   │   ├── Employee Directory
│   │   ├── Organization Chart
│   │   ├── Departments & Teams
│   │   └── Positions
│   ├── Records
│   │   └── Documents
│   └── Administration
│       ├── User Access
│       └── Audit History
├── TopNavigation
│   ├── Global Employee Search
│   └── Account Menu
└── Routed Page Content
```

Modules are added to navigation only when their backend use cases exist.

## Routes

| Route | Purpose |
|---|---|
| `/` | Role-aware dashboard |
| `/employees` | Searchable and grouped employee directory |
| `/employees/:employeeId` | Employee profile and HR records |
| `/organization-chart` | Reporting hierarchy with search, zoom, and collapse |
| `/departments` | Business unit, division, department, and team management |
| `/positions` | Position catalog |
| `/documents` | Folder-backed document workspace |
| `/administration/users` | Admin-only user access |
| `/administration/audit` | Authorized audit history |
| `/account/security` | Current-user password management |

## Backend additions for the document workspace

- `DocumentFolder` is an organization-owned, soft-deletable aggregate with scope, optional employee ownership, allowed roles, and system-folder protection.
- `EmployeeDocument.FolderId` associates the logical document with a folder while immutable `DocumentVersion` rows continue to own file metadata and storage keys.
- `GET /api/v1/document-folders` returns only role- and employee-scope-visible folders with item counts.
- `POST /api/v1/document-folders` creates a validated custom folder for authorized HR roles.
- `GET /api/v1/documents` searches documents across employees already visible to the caller.
- `POST /api/v1/employees/{employeeId}/documents` accepts an optional `folderId` and still performs quarantine and malware scanning.

## Shared component direction

`PageHeader`, `SearchInput`, `Avatar`, `Button`, `Card`, `Badge`, `Dialog`, `EmptyState`, `ErrorState`, and `Skeleton` define common behavior. Feature components such as directory rows, document folder cards, upload dropzones, and org-chart nodes compose those primitives instead of defining a separate visual language.

## Deferred API work

The following capabilities remain intentionally deferred rather than represented by non-functional pages:

- Profile-image upload, replacement, and authorized image delivery.
- Paginated server-side directory grouping and an optimized large-company org-chart projection.
- Folder rename, archive, move, and per-folder permission administration.
- Document preview, tagging, signatures, text extraction, summaries, and embeddings.
- Time off, training, certifications, education, performance, reporting, and AI assistant domains.

Each deferred module should add its Domain entities, Application commands/queries and validators, Infrastructure persistence, authorized API endpoints, typed frontend client, hooks, states, and tests before appearing in primary navigation.
