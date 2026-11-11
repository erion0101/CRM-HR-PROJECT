# HR Modules Plan — Employee, Manager, HR

Plan for the features that come after onboarding. Each phase is a vertical slice (domain → application → API → React → tests) and follows the existing patterns: feature-oriented CQRS with MediatR, FluentValidation, thin controllers, one EF configuration per entity, ETag/`If-Match` on updates, audit events, in-app notifications, and `UserScope` (Self, DirectReports, Department, Organization) for who can see whom.

**Out of scope (decided):** offboarding, reports/export, expiry alerts (contracts, probation, permits).

## Order

| # | Phase | Roles | Size |
|---|---|---|---|
| 1 | Time off (leave) ✅ | HR, Employee, Manager | L |
| 2 | Self-service profile and My documents | Employee, HR | M |
| 3 | My team page | Manager | S |
| 4 | Probation and performance reviews | Manager, Employee, HR | L |
| 5 | My onboarding redesign | Employee | S |
| 6 | Equipment | HR, Employee | M |
| 7 | Bulk employee import (CSV) | HR | M |
| 8 | Timesheets and overtime (optional) | Employee, Manager | M |

Phase 1 comes first because the three roles depend on each other: HR defines the leave types and allowances, employees request leave, and managers approve it. Phase 3 is better once leave exists, because "who is out" is one of the main parts of the team page.

---

## Phase 1 — Time off ✅ built

### Company rules (decided)

| Leave type | Days per year | How days are earned | Unused days at year end |
|---|---|---|---|
| Vacation | 20 | Monthly accrual: 20 ÷ 12 ≈ 1.67 days for each month worked | **All carried over**, for example 7 unused → 20 + 7 = 27 next year. Carried-over days **expire after 30 June** if not used by then. |
| Sick leave | 20 | Full amount on 1 January | Not carried over (resets to 20) |

- Sick leave has its own balance and never uses vacation days.
- Whole days only. No half days.
- A month counts when the person is employed for at least one day of it, so someone starting mid-year earns only for the months they work.
- Both types are seeded at startup when an organization has no leave types. They are settings HR can change in **Settings → Time off and holidays**, not values in the code. Both need the manager's approval by default.

### Changes from the first plan

- **Balances are calculated, not stored.** `LeaveBalanceCalculator` works out each balance from the rules, the employment periods, the requests and HR's adjustments. There is no `LeaveBalance` table and no year-start or 30-June worker job. Cancelling, rejecting or correcting a past request therefore always gives the right result, and nothing can run twice.
- **The allowance rules live on `LeaveType`.** There is no separate `LeavePolicy` and no per-department rules, because one set of company rules was enough.
- **No sick-note attachment yet** (`RequiresAttachment` is not built).
- Leave settings are at `/settings/time-off`, with the company's other settings.

### Domain

- `LeaveType`: name, color, paid, needs approval, has an allowance, days per year, accrual method (`Monthly` or `YearlyUpfront`), carries over, carry-over limit (empty means no limit), carry-over expiry month and day (empty means never). It is archived rather than deleted.
- `PublicHoliday`: date, name, `RepeatsYearly`. A yearly holiday applies from its first year onward, and 29 February repeats only in leap years.
- `LeaveRequest`: employee, type, whole-day start and end within one calendar year (up to 3 months), working days (fixed when sent), reason, status (`Pending`, `Approved`, `Rejected`, `Cancelled`), the manager at the time of the request, who decided, the comment, and when. A type that needs no approval is approved straight away.
- `LeaveBalanceAdjustment`: employee, type, year, ± days, reason, made by. Never edited, only added.
- `WorkingDayCalendar` counts Monday to Friday minus public holidays.

### Rules

- Working days are calculated on the server. Requests already sent keep their count if holidays change later.
- A request cannot overlap the person's other pending or approved requests.
- Pending requests reserve days. The balance is checked on the request's first day: days earned up to and including that month, plus carried-over days if they are still valid.
- Days taken up to 30 June use carried-over days first. A request that spans 30 June uses carried-over days only for the days up to that date.
- The decider is the employee's **current** manager (`ReportingLine`) or HR. Nobody decides their own request. Rejecting needs a reason.
- The employee can cancel a pending request, or an approved one that has not started. HR can cancel for them. Cancelling gives the days back.
- **"On leave" today = an employment marked On leave, or approved time off that covers today.** `Employment.Status` is not changed. Home's "Who's out" uses this.

### API (`/api/v1/leave`)

| Endpoint | Who |
|---|---|
| `GET types`, `GET holidays` | Everyone signed in |
| `POST/PUT/DELETE types`, `POST/DELETE holidays` | HR (`LeaveAdmin`) |
| `GET me`, `GET me/preview`, `POST requests`, `POST requests/{id}/cancel` | The signed-in employee (HR can also cancel) |
| `GET approvals`, `POST requests/{id}/approve`, `POST requests/{id}/reject` | Managers (their current direct reports) and HR (`LeaveApprove`) |
| `GET calendar?from&to&departmentId` | Everyone. HR sees everyone (or one department); others see their team: themselves, their manager, colleagues with the same manager, and their own reports. The kind of leave is shown only to the person, their manager and HR. |
| `GET employees/{id}` | Anyone who can see that employee |
| `POST employees/{id}/adjustments` | HR |

### Screens

- **My time off** (`/time-off`): a balance card per leave type with where the days come from ("5 days earned of 20 · 7 days from last year, use by 30 Jun"), **Request time off** with a live count and the balance after the request, and upcoming and earlier requests with Cancel.
- **Approvals** (`/time-off/approvals`, Manager and HR): pending requests with the balance after the request and the teammates already off then. Approve, or Reject with a reason.
- **Team calendar** (`/time-off/calendar`): a month grid with public holidays, weekends and the number of people out each day, plus the list of who is out. HR can filter by department.
- **Settings → Time off and holidays** (`/settings/time-off`, HR): leave types with their rules in plain words, and the public holidays.
- **Employee profile → Time off tab**: balances per year, requests, HR's adjustments, and **Adjust balance** for HR.
- **Home**: "Company holidays" shows the next 90 days, and "Who's out" includes approved time off.
- Menu: a **Time off** group. The HR admin, who has no employee profile, lands on Approvals.

### Notifications

`LeaveRequested` goes to the manager. `LeaveApproved` and `LeaveRejected` go to the employee. `LeaveCancelled` goes to the manager when the employee cancels, and to the employee when HR cancels for them. HR is not notified when someone has no manager; HR sees those requests in Approvals.

### Tests

- Domain (`LeaveTests`): working days, accrual at ≈1.67 a month, a mid-year start, 7 unused → 27, carried-over days used first and expiring after 30 June (including a request across that date), the sick-leave reset, pending versus rejected, adjustments, the carry-over limit, request limits, cancellation and decisions.
- Integration (`LeaveApiTests`): the full flow against PostgreSQL, covering the seeded defaults, preview with carry-over (25 → 20 → 27), overlap, not enough days, who may see and decide, notifications, calendar visibility, HR adjustment, cancel, and settings being HR-only.
- Frontend (`time-off/utils.test.ts`): formatting, balance explanations, month grids and upcoming holidays.

---

## Phase 2 — Self-service profile and My documents

### Profile

- Add to `Employee`: personal email, home address (line, city, postal code, country), emergency contact (name, relation, phone) and bank account (IBAN, bank name). Mark the bank fields `[AuditRedacted]` and show them only to the employee and HR, the same way salary is restricted.
- `ProfileChangeRequest`: employee, the requested field values (JSON), status (`Pending`, `Approved`, `Rejected`), reviewer, comment.
- Rule: the employee can change phone, personal email, address and emergency contact directly. A change to the **bank account** or **legal name** becomes a change request that HR approves.
- API: `GET/PUT employees/me/profile`, `POST employees/me/change-requests`, plus `GET change-requests` and approve/reject for HR.
- Screens: **My info** gets an **Edit** button. HR gets a **Profile changes** queue under People that shows old and new values side by side.
- Notifications: `ProfileChangeRequested` to HR, and `ProfileChangeApproved` or `ProfileChangeRejected` to the employee.

### My documents

- Add `VisibleToEmployee` to `EmployeeDocument`, default `false`, so nothing becomes visible by accident.
- The employee sees clean documents linked to their own Employee, Contract or Employment records where `VisibleToEmployee` is true. This covers contracts and certificates.
- **Payslips are not part of this system (decided).**
- Screens: a **My documents** tab on My info that is download-only. On the HR document upload, add a "Visible to the employee" switch.

---

## Phase 3 — My team (Manager)

- Route `/my-team`, shown in the menu for Managers.
- One row per direct report: photo, position, **status today** (working, on leave until a date, public holiday), onboarding progress, probation end date (from Phase 4), and the number of pending requests.
- At the top: pending leave approvals, pending reviews, and who is out this week.
- Uses the existing `direct-reports` endpoint plus one summary query, `GET /api/v1/my-team/summary`, so the page does not make one call per person.

---

## Phase 4 — Probation and performance reviews

### Probation

- Add `ProbationEndDate` to `Employment`. Set it in the New hire flow. The default is **6 months** after the start date (decided), and HR can change it for a person.
- `ProbationReview`: employee, reviewer (manager), due date, decision (`Passed`, `Extended` with a new end date, `NotPassed`), notes.
- The manager sees due probation reviews on My team. HR sees every decision. `NotPassed` only records the decision (offboarding is out of scope).

### Performance

- `ReviewCycle` (HR): name, period, self-review due date, manager-review due date, participants chosen by department or all active employees, status (`Draft`, `Open`, `Closed`).
- `PerformanceReview`: cycle, employee, reviewer, self-assessment answers, manager assessment answers, rating from 1 to 5, status (`NotStarted`, `SelfReview`, `ManagerReview`, `Shared`, `Acknowledged`).
- `ReviewQuestion` on the cycle: the question text and whether the answer is a rating or free text.
- `Goal`: employee, title, description, due date, progress %, status. The employee creates goals and the manager can comment on them.
- Rules: the employee sees the manager's assessment only after it is **Shared**. HR can see all reviews.
- Screens: **Reviews** for the employee (my review, my goals), **Team reviews** for the manager, and **Review cycles** for HR (create, open, watch completion, close).
- Notifications: `ReviewAssigned` and `ReviewShared` only. These are not deadline alerts.
- Later: 1:1 notes with a private manager section and a shared section.

---

## Phase 5 — My onboarding redesign

Follow-up to `docs/ux/hr-ux-review.md`:

- One **Next step** card at the top, and a progress bar that stays visible on phones.
- Steps grouped by package, with finished steps collapsed into "5 done".
- When a package has no published quiz, reading steps still use the reading layout instead of generic Start/Complete rows.
- Test at 375 px width and take before and after screenshots, as in the UX review.

---

## Phase 6 — Equipment

- `Asset`: type (laptop, phone, badge, monitor, other), name, serial number, purchase date, status (`Available`, `Assigned`, `InRepair`, `Retired`), notes.
- `AssetAssignment`: asset, employee, handed-over date, returned date, condition notes. An asset has at most one open assignment.
- Screens: an **Equipment** list for HR (filter by status and type, assign, return), an **Equipment** tab on the employee profile, and a read-only list on My info.
- Link to onboarding: an optional **Equipment** step in New hire to assign available assets, and the new hire confirms receipt as an onboarding step.

---

## Phase 7 — Bulk employee import (CSV)

- Downloadable template with these columns: employee number, first name, last name, work email, phone, department, position, manager employee number, start date, create login (yes/no).
- Two steps: **Preview** checks every row and shows errors and duplicates, reusing the duplicate rules from New hire. **Import** creates only the valid rows.
- The import is idempotent by employee number, so running the same file twice does not create duplicates. It runs in the worker for large files and shows the result per row.
- Each created employee gets an employment record, a department and position, a reporting line, an optional login with a temporary password, and the default company onboarding package (this happens automatically when the employment becomes Active).

---

## Phase 8 — Timesheets and overtime (optional)

Decide later whether the company needs this.

- `TimeEntry`: employee, date, hours, project (optional), note. Entered per week.
- `Timesheet`: employee and week, status (`Draft`, `Submitted`, `Approved`, `Rejected`). The manager approves it.
- `OvertimeRequest`: date, hours, reason, approved by the manager. Approved overtime can optionally be added to a "Time in lieu" leave balance from Phase 1.

---

## Decisions

- Vacation: 20 days a year, earned at ≈1.67 days a month. All unused days carry over to the next year and expire on 30 June.
- Sick leave: 20 days a year, a separate balance, not carried over.
- No half days.
- No payslips in the system.
- Probation: 6 months.

## Open questions

1. Timesheets: needed at all?
