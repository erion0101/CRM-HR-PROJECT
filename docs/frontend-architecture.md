# HR Frontend Architecture

## Reference analysis

The local reference project is `/Users/erionshahini/wp-full-cloud/wp-frontend-cloud`. Its web application uses React 19 and TypeScript in an Nx/Yarn workspace, React Router 7, Vite, Tailwind CSS 4, TanStack Query, Axios, React Hook Form, i18next, a shared component library, and Vitest/Testing Library.

The reusable engineering patterns carried into this project are:

- feature-owned components, hooks, types, and API operations;
- an Axios client separated from TanStack Query hooks;
- stable query-key factories and targeted cache invalidation;
- provider composition at the application root;
- a protected layout route with nested pages;
- thin pages that compose focused feature components;
- React Hook Form for accessible form state and field validation;
- explicit loading, error, empty, and mutation states;
- shared notification, dialog, table, badge, avatar, and layout primitives;
- environment-owned API configuration and no secrets in browser bundles;
- Vitest and Testing Library for component behavior.

The HR frontend does not reuse the reference project's Amplify/JWT implementation because the HR backend uses an HTTP-only ASP.NET Core Identity cookie. It also does not import reference-project business code or couple the HR system to its tenant, hospital, device, or case-management models.

## Backend capability map

### Available

- Authentication: login, logout, current user, password change, antiforgery token.
- Employees: cursor-paginated scoped search, display-ready profile summaries, direct reports, effective-dated employment/assignment/reporting history, create, update, archive, employment status, position assignments, and reporting lines.
- Organization: cursor-paginated typed organization units and positions with create, update, and safe archive operations. Units form a validated Business Unit → Division → Department → Team hierarchy.
- Contracts: per-employee list, create, revise, activate, and terminate.
- Compensation: protected per-employee history and creation.
- Documents: scoped per-employee list and download; HR upload, immutable replacement version, and archive.
- Administration: user list, user creation/update, role assignment, employee linking, and data scopes.
- Audit: permission-protected cursor-paginated audit search.
- Dashboard: authoritative server-side totals and recent employees filtered to the caller's data scope.
- Employee activity: scoped, cursor-paginated activity assembled from employee-related audit records, with sensitive compensation events restricted to HR.
- Operations: liveness, readiness, and Development OpenAPI.

Authentication is cookie-based. The browser never receives or stores an access token. Before an authenticated unsafe request, the API client obtains `/auth/antiforgery` and sends its token through `X-CSRF-TOKEN`. Update and archive operations send the entity version through `If-Match`.

### Not currently available

The frontend will not simulate the following functionality:

| Feature | Missing backend capability |
| --- | --- |
| Global contract workspace | Organization-scoped contract search with employee/department/status/expiry filters and cursor pagination. |
| Training | Training definitions, assignments, completion, expiration, and evidence endpoints. |
| Certificates | Certificate records, expiration search, and document association endpoints. |
| Skills | Employee skills and organization skill-search endpoints. |
| AI assistant | Conversation, message, structured result, citation, and history endpoints. |
| Notifications | Notification list, unread count, read, and delete endpoints. |
| Reports | Report definitions, asynchronous export, and download endpoints. |
| Role editor | Role and permission CRUD endpoints; current roles are fixed backend roles. |
| Document preview/categories | Preview generation and document-category management endpoints. |

Recommended future API prefixes are `/api/v1/dashboard`, `/api/v1/contracts`, `/api/v1/training`, `/api/v1/certificates`, `/api/v1/skills`, `/api/v1/ai`, `/api/v1/notifications`, and `/api/v1/reports`.

## Frontend structure

```text
frontend/
├── src/
│   ├── app/
│   │   ├── access.ts
│   │   ├── providers/
│   │   └── router.tsx
│   ├── components/
│   │   ├── feedback/
│   │   ├── forms/
│   │   ├── navigation/
│   │   └── ui/
│   ├── features/
│   │   ├── auth/
│   │   ├── dashboard/
│   │   ├── employees/
│   │   ├── organization/
│   │   ├── contracts/
│   │   ├── documents/
│   │   ├── administration/
│   │   └── system/
│   ├── hooks/
│   ├── lib/
│   ├── styles/
│   └── types/
├── nginx.conf
├── Dockerfile
└── vite.config.ts
```

## Routes

| Route | Access | Purpose |
| --- | --- | --- |
| `/login` | Public | Local account login. |
| `/` | Authenticated | Workforce snapshot using only supported employee/organization queries. |
| `/employees` | Authenticated, backend-scoped | Employee search and management. |
| `/employees/:employeeId` | Authenticated, backend-scoped | Persistent employee summary with overview, employment editing, effective-dated history, contracts, documents, permitted compensation, and scoped activity. |
| `/departments` | Authenticated | Organization structure; write actions for HR. |
| `/positions` | Authenticated | Position list; write actions for HR. |
| `/administration/users` | HR | User and role assignment. |
| `/administration/audit` | HR | Audit search. |
| `/account/security` | Authenticated | Password change. |
| `/forbidden` | Public | Permission feedback. |
| `*` | Public | Not-found feedback. |

Unsupported training, certificate, notification, reporting, and AI routes are deliberately not registered.

## State and API model

TanStack Query owns all server state. Query keys are feature-local and mutations invalidate only affected resources. UI state remains local except for authentication, notification, and responsive-shell contexts. Axios is configured with `withCredentials: true`, an environment-based base URL, consistent RFC 9457 error normalization, antiforgery handling for unsafe requests, and binary document download support.

The Vite development server and production Nginx image proxy `/api` to the .NET API. This maintains a same-origin browser boundary for the authentication and antiforgery cookies. Backend authorization remains authoritative; role-aware navigation only improves the user experience.

## Reusable UI

The local HR design system includes buttons, icon buttons, fields, selects, badges, status badges, cards, dialogs, tables, avatars, tabs, skeletons, empty states, error states, page headers, statistic cards, cursor pagination, breadcrumbs, toast notifications, and the responsive application shell. Components use semantic HTML, visible focus states, labelled controls, keyboard-accessible actions, and readable status text rather than color alone.
