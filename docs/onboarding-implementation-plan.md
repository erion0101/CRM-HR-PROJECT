# Employee Onboarding and Knowledge Management — Implementation Status

The original delivery plan has been implemented. The authoritative architecture is in `docs/architecture.md`; the executable manual acceptance flow is in `docs/onboarding-manual-test.md`.

## Delivered vertical slices

1. Tenant-scoped Knowledge, Project, Onboarding, reading-cycle, and assessment aggregates with EF Core mappings and non-destructive migrations.
2. Purpose-built repositories and MediatR/FluentValidation use cases for article/document publication, projects and memberships, versioned templates, assignments, progress, quiz authoring, grounded AI draft generation, publication, attempts, and grading.
3. React company/project knowledge, project, onboarding administration, My Onboarding, and quiz experiences.
4. Fixed read-only onboarding/knowledge AI tools plus provider-neutral quiz generation over approved indexed evidence.
5. Mandatory backend invariant: all required items must be read in the current cycle before an attempt; failure atomically creates a new cycle with every item unread and preserves all history.

## Current hardening backlog

- Browser-driven automation of the complete ten-document, fail, reset, reread, and pass scenario.
- Bulk document-to-module creation and drag/drop reordering in the template builder.
- Configurable question distribution across modules and configurable attempt/rate controls.
- Richer HR/Manager assessment analytics and filters.
- Expanded adversarial tenant, scope-revocation, and duplicate-submission concurrency coverage.
- Production controls listed in `docs/architecture.md`, including HTTPS, MFA/OIDC, secrets management, encrypted backups, monitoring, and governance approval for real HR data.

This module intentionally remains part of the existing modular monolith. It does not add a second AI architecture, message broker, external vector database, or browser-managed provider credential.
