# AsterCare Medical demo (fictional)

Loads a fictional medical-device company into a running local system, through the same API HR uses:
5 company documents, the **Pulse One** project (wearable ECG patch) with 4 project documents, both onboarding
packages, and draft quizzes. All content is synthetic and marked as fictional in every document.

```bash
docker compose up --build -d migrate api worker web
python3 scripts/demo/seed_medical_device_demo.py
```

The script signs in with `ADMIN_EMAIL` / `ADMIN_PASSWORD` from `.env` (never printed), generates the PDF/DOCX files
with the Python standard library, uploads them, waits for the virus scan, and approves them for AI as
`NormalInternal`. It is safe to run again: existing items are reused.

| Company documents | Pulse One documents |
|---|---|
| Company Handbook (PDF) | Product Overview and Intended Use (PDF) |
| Quality Management System Essentials (PDF) | Software Development Process (PDF) |
| Information Security and Patient Data (DOCX) | Risk Management Plan (DOCX) |
| Code of Conduct and Healthcare Professionals (PDF) | Team and Ways of Working (PDF) |
| Health, Safety and Cleanroom Rules (DOCX) | |

## AI questions

AI can only read the documents once they are indexed, which needs `OPENAI_API_KEY` in `.env`. After adding it,
restart (`docker compose up -d api worker`), wait a minute for indexing, then in **Onboarding → Manage → Assessment**
choose **Generate with AI** for each package, review the questions, and publish. Without a key, add questions manually.

## Try the HR flow

1. **People**: create the new employee, set employment to Active, and create their user account (Employee role).
2. **Projects**: add them to Pulse One.
3. **Onboarding → Manage → Assign journey**: choose *AsterCare Medical company onboarding* and the Pulse One project,
   untick documents this employee does not need, and assign.
4. Sign in as the employee: they read only the ticked documents, and their quiz only asks about those.
