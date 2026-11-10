# Document Onboarding Assessment — Manual End-to-End Test

This runbook verifies the complete backend-enforced workflow:

```text
read every required document -> unlock quiz -> pass and continue
                                            -> fail and restart all reading
```

Use synthetic local data only. AI generation is optional because manual quiz authoring is a supported fallback.

## 1. Start the application

```bash
docker compose up -d --build
docker compose ps
```

Open `http://127.0.0.1:3000`. Core API readiness is available at `http://127.0.0.1:8080/health/ready`.

Sign in as HR. Ensure the local organization contains a manager and an employee account linked to separate employee records. Create a project such as `Client Portal`, make the manager/project lead a member with the appropriate responsibility, and add the employee as an active project member.

## 2. Create source knowledge and documents

1. Open **Knowledge** and create published company and project articles for the synthetic onboarding content.
2. Upload several PDF/DOCX files to those articles. Multiple files may be selected in the upload control.
3. Wait for each file to move from `PendingScan` to `Clean`. Rejected, infected, archived, or pending files cannot become onboarding document steps.
4. Download one file through the article page to confirm that access uses the protected API rather than a public path.
5. For AI quiz generation only, approve each eligible synthetic file as `NormalInternal` and wait for its AI index to become `Processed`. Image-only PDFs become `NeedsOcr` and cannot be used as AI quiz evidence.

## 3. Build the package and its reading modules

Onboarding is split into packages. A **Company package** covers how the company works and is assigned to every new employee. Each project can have one **Project package** with its own documents. When HR assigns the company package together with a project, that project's published package is added to the same journey automatically: its sections follow the company sections, and one quiz at the end draws from both packages' question banks using the stricter passing percentage.

1. Open **Onboarding -> Manage onboarding** and choose **New template**. Choose **Package type**: *Company package* for the company documents, or *Project package* plus the project for project documents. Create both for this scenario.
2. Create sections such as **Company Introduction**, **Company Policies**, and **Client Portal Project**.
3. Add one step for every file that must be reread after a failure:
   - choose **Read document**;
   - select a clean file from **Required document**;
   - mark the step **Required**;
   - set its section, order, instructions, expected outcome, and due day.
4. Optional files may be added with **Required** cleared; they do not block the quiz.
5. Add any later practical or manager-reviewed stages. Passing the quiz must not automatically complete those stages.
6. Publish the template. The published template version and referenced document versions are immutable for assignments.

For the formal acceptance scenario, add ten required document steps across the three modules.

## 4. Create and publish the quiz

1. On the published template card choose **Assessment**.
2. Create a draft with a passing percentage such as `80` and a question count such as `15`. AI generation fills a bank of roughly twice that size (up to 40) spread evenly across all required documents; for manual quizzes, add at least twice the per-attempt count so retakes differ.
3. Choose one of these paths:
   - **Generate with AI** after all required documents are approved and indexed. The request returns immediately with the status *generating*; the worker must be running. The builder refreshes every few seconds and shows the questions, or the failure reason; or
   - **Add manually** when AI is disabled, unavailable, or source material is insufficient.
4. Review every question, four options, marked correct answer, explanation, topic/difficulty, and source.
5. Add/edit/delete draft questions as needed, then choose **Publish immutable quiz**.

AI output is always a draft. Publishing is an explicit HR action, and runtime attempts never generate new questions.

## 5. Assign and verify the locked state

To test automatic assignment instead: on the company package card choose **Make default for new employees**, create a new employee, and set their employment to **Active**. The employee must receive exactly one journey with the employment start date; changing the active employment again must not create a second one.

1. Assign the published **company package** to the employee with the project and manager selected. The dialog must say it includes the project package. The assignment row shows `Company package + Project package · Project`.
2. Sign in as the employee and open **My Onboarding**.
3. Confirm the current reading cycle is `1`, every required document is `Unread`, progress is `0%`, and the quiz is `Locked`.
4. Try calling `POST /api/v1/onboarding/assignments/{assignmentId}/quiz-attempts` before finishing the reading. The API must reject it even if React is bypassed.

## 6. Read all required documents

For each required item:

1. Choose **Open**. The file streams from `/api/v1/onboarding/assignments/{assignmentId}/reading/{stepId}/download` after ownership and current-cycle checks.
2. Confirm the UI moves the item to `InProgress`.
3. Choose **Mark as read** and confirm the state is saved as `Read`. **Mark as read** stays disabled until the item has been opened, and the API rejects `Read` for an `Unread` item.

At nine of ten required items, progress must show `90%` and the quiz must remain `Locked`. After the tenth, progress must show `100%` and the quiz must become `Available`.

## 7. Fail the first attempt and verify the mandatory restart

1. Start the assessment and confirm the browser receives question and option text but no `isCorrect`/answer-key property.
2. Answer every question, submit after confirmation, and intentionally score below the threshold.
3. Confirm the result explains the actual score, required score, and full-reading restart, and lists the documents with wrong answers. It must **not** show any correct answers; the full answer review appears only after a pass.
4. Return to **My Onboarding** and verify all of the following:
   - failed attempt 1 and its score remain visible;
   - current reading cycle is `2`;
   - all ten required files are `Unread`;
   - reading progress is `0%`;
   - quiz state is `Locked`;
   - no direct attempt URL or API call can skip rereading.

This transition is one database unit of work: the scored attempt is finalized, cycle 1 is failed, assessed step state is reset, cycle 2 and all fresh `Unread` rows are inserted, and the assignment is locked. Historical cycle 1 is not deleted.

## 8. Reread and pass

1. Open and mark all ten files as read again in cycle 2.
2. Start a new attempt. It must be attempt 2 and belong to cycle 2. When the question bank is larger than the questions per attempt, attempt 2 uses questions not shown in attempt 1 wherever possible, and every attempt draws questions from every source document.
3. Score at or above the threshold. Exactly `80%` passes when the threshold is `80`.
4. Confirm the assessment is `Passed`, both attempt histories remain visible, and later required practical/manager steps are now available but are not falsely marked complete.
5. Sign in as authorized HR/Manager and confirm the employee's current assessment result and journey progress are visible only within normal employee scope.

## 9. Security and resilience checks

- Sign in as another employee and verify direct reading, attempt, document, project, and assignment identifiers return an unavailable/not-found response.
- Submit the same attempt twice and verify the second request is rejected.
- Retry with a stale `If-Match` value and verify a concurrency conflict.
- Disable AI and restart API/Worker; manual quiz authoring and the entire reading/pass/fail workflow must still work.
- Replace a source file after assignment and confirm the active reading cycle still points to its snapshotted immutable document version.
- Confirm document binaries, prompts, answer keys, and extracted chunks are absent from audit payloads and ordinary logs.
