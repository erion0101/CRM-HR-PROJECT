# PeopleSpring HR Management

Local-first HR management system with a .NET 10 Clean Architecture backend and a production React 19/TypeScript web application. PostgreSQL, Redis, ClamAV, the API, worker, migrations, and frontend all run locally through Docker Compose. The product includes company/project knowledge, effective-dated project membership, personalized onboarding journeys, and a read-only permission-scoped OpenAI/RAG assistant that is disabled by default behind governance gates.

The backend uses feature-oriented CQRS with MediatR, FluentValidation pipeline behaviors, thin ASP.NET Core controllers, focused repository ports, and one EF Core configuration per entity.

## Prerequisites

- Docker Desktop with Docker Compose
- No host .NET SDK is required

## Start the stack

```bash
cp .env.example .env
# Replace every placeholder password in .env before continuing.
docker compose up --build
```

Open the web application at `http://127.0.0.1:3000`. The API listens only on `http://127.0.0.1:8080` by default. Readiness is available at `/health/ready`; the OpenAPI document is available in Development at `/openapi/v1.json`.

Sign in with the administrator email and password configured in `.env`. The browser uses secure HTTP-only cookie authentication; no API credential is stored in browser storage.

When `DEMO_USERS_PASSWORD` is set in the ignored local `.env`, Development startup also creates four linked demo accounts: `erion.employe@gmail.com` (Employee), `erion.manger@gmail.com` (Manager), `erion.hr@gmail.com` (HR), and `erion.pm@gmail.com` (Product Manager). Their shared password is read only from `.env`; leave this setting empty outside local development.

## Frontend development

The frontend lives in `frontend/` and uses React, TypeScript, Vite, React Router, TanStack Query, Axios, React Hook Form, and Tailwind CSS tooling. The full frontend structure and backend capability map are in [docs/frontend-architecture.md](docs/frontend-architecture.md).

Run it against the Dockerized API from the host:

```bash
cd frontend
corepack yarn install
corepack yarn dev
```

The Vite development server is available at `http://127.0.0.1:4200` and proxies API requests to port 8080.

Build, lint, and test the web application:

```bash
cd frontend
corepack yarn build
corepack yarn lint
corepack yarn test
```

PostgreSQL is exposed on `127.0.0.1:5433` by the example configuration so it does not collide with a common host installation. Redis is available only inside the Compose network. Change `POSTGRES_PORT` or `API_PORT` in `.env` if either selected port is already occupied.

ClamAV downloads its signature database on first start, so the initial startup can take several minutes. The API and worker wait until it is healthy.

## Authenticate from curl

Cookie-authenticated state-changing requests require an antiforgery token.

```bash
curl -i -c cookies.txt \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@example.local","password":"YOUR_ADMIN_PASSWORD","rememberMe":false}' \
  http://127.0.0.1:8080/api/v1/auth/login

curl -b cookies.txt -c cookies.txt \
  http://127.0.0.1:8080/api/v1/auth/antiforgery
```

Send the returned token in `X-CSRF-TOKEN` and the cookie jar on POST, PUT, and DELETE calls. Updates and archives also require the current response ETag, for example `If-Match: "1"`.

## Documents

Uploaded PDF and DOCX files first enter `data/quarantine`. The worker validates the signature, calculates SHA-256, checks duplicates, scans with ClamAV, and then moves clean files to:

```text
data/documents/{organizationId}/{documentId}/{versionId}/original.ext
```

The `data` directory is intentionally ignored by Git. Back up it together with PostgreSQL.

## Knowledge, projects, and onboarding

Authenticated users have three connected workspaces:

- `/knowledge` shows published company guidance allowed for the user's audience. Authorized HR editors can create, review, publish, and attach secure PDF/DOCX resources.
- `/projects` shows only projects the caller administers, leads, or actively belongs to. Project leads can manage memberships and project-only knowledge.
- Onboarding content is organized as a **Company package** (assigned to every new employee) and one **Project package** per project. Assigning the company package with a project automatically adds that project's published package to the same journey, with one combined quiz at the end.
- HR can mark one published company package as **Default for new employees**. When an employee's employment first becomes Active, that package is assigned automatically with the employment start date and current manager. The template builder's **Add documents** action turns several clean documents into required reading steps at once.
- **Generate with AI** queues quiz generation; the worker runs it in the background on behalf of the requesting HR user (same quotas and audit) and the quiz builder updates when it finishes. `Ai:BackgroundRequestTimeoutSeconds` (default 180) bounds that provider call separately from the 45-second chat timeout.
- `/onboarding` shows the signed-in employee's assigned projects, manager/mentor context, progress, deadlines, resources, and next step. Authorized HR users and managers use `/onboarding/manage` to publish templates, assign journeys, and review milestones within their employee scope.

Onboarding remains fully usable when AI is disabled. Published template versions and assigned steps are snapshotted so later edits do not rewrite employee history. Policy acknowledgments reference the exact published knowledge revision. See [docs/onboarding-manual-test.md](docs/onboarding-manual-test.md) for an end-to-end verification scenario.

## AI assistant

The read-only AI assistant is disabled by default. It uses the official OpenAI .NET SDK on the backend; React never receives a provider key. An OpenAI Platform project API key is required—not a separate Codex credential.

Keep these settings disabled until the HR-AI governance and production-security launch gates are complete:

```dotenv
AI_ENABLED=false
AI_REAL_DATA_APPROVED=false
ORGANIZATION_TIMEZONE=Europe/Belgrade
OPENAI_API_KEY=
OPENAI_MODEL=gpt-6.1-sol
OPENAI_EMBEDDING_MODEL=text-embedding-3-small
AI_INPUT_COST_PER_MILLION_USD=0
AI_OUTPUT_COST_PER_MILLION_USD=0
```

Before enabling AI, replace both zero cost values with the current per-million-token prices for your configured model. If pricing is missing, AI stays disabled so budget enforcement cannot silently report zero cost; the core HR API continues to start normally.

For local testing with synthetic data only, set both gates to `true`, configure the key, and rebuild/restart `api` and `worker`:

```bash
docker compose up --build -d migrate api worker web
```

All authenticated roles can use `/ai-assistant` when capabilities report it enabled. HR can review provider state, governance gates, quotas, and usage at `/administration/ai-settings`. Document content remains unavailable to AI until HR classifies and approves a clean PDF or DOCX. Sensitive and Highly Restricted documents can never be approved.

The provider request uses `store:false`; this does not by itself guarantee Zero Data Retention. Do not activate real HR data until the DPIA/legal review, provider data terms and retention posture, HTTPS, encrypted storage/backups, access review, and production authentication controls are approved.

## Database migrations

Migrations run through the one-shot `migrate` service during startup:

```bash
docker compose run --rm migrate
```

To inspect migration status using the SDK image:

```bash
docker run --rm -v "$PWD:/src" -w /src --network hr-management_default \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet ef migrations list --project src/HrManagement.Infrastructure --startup-project src/HrManagement.Api
```

## Tests

```bash
docker run --rm -v "$PWD:/src" -w /src \
  -v /var/run/docker.sock:/var/run/docker.sock \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test HrManagement.sln --configuration Release
```

The integration suite uses Testcontainers and therefore needs access to the Docker socket.

When running the test SDK itself inside Docker Desktop, use the following flags if the Testcontainers resource reaper cannot connect:

```bash
docker run --rm \
  -e TESTCONTAINERS_RYUK_DISABLED=true \
  -e TESTCONTAINERS_HOST_OVERRIDE=host.docker.internal \
  -v "$PWD:/src" -w /src \
  -v /var/run/docker.sock:/var/run/docker.sock \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test HrManagement.sln --configuration Release
```

## Reset local data

This permanently deletes the local database and Redis data. Documents under `data/` are intentionally retained:

```bash
docker compose down -v
```

Delete retained documents manually only when they are no longer needed.

See [docs/architecture.md](docs/architecture.md) for the system design, security model, and next milestones.
