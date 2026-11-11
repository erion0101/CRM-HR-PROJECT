# HR experience review

Reviewed on 10 November 2026 against the running app (`docker compose up`, demo data from `scripts/demo/`),
signed in as the HR administrator and as a demo employee. Screenshots, where taken, are in `docs/ux/screenshots/`
(`before-*` and `after-*`, at 1280px and 360px wide).

The owner's complaint, in their words: *"I have company documents, project documents, the user list, user details,
but no clear flow. When I invite a user it should be clear how to give them onboarding."*

## 1. What HR has to do today, and where it breaks

The product's job is: documents → packages → quiz → new hire → follow progress. Here is how each task actually goes.

| Task | Where it lives today | What goes wrong |
|---|---|---|
| Add company / project documents | **Work → Company** (HR only, "Add PDF"), and separately **Onboarding → Company knowledge** (articles with uploads), and **Onboarding → Projects** (project knowledge) | Three places for the same thing, with three names: *PDF knowledge*, *knowledge article*, *company document*. "Add PDF" silently creates an "article" and publishes it; the Knowledge page then shows that article with an editor nobody asked for. Nothing says these documents feed onboarding, or whether a document is ready for the AI quiz. |
| Build a package and its quiz | **Onboarding → Manage** ("Journeys and progress") | Called *templates*. The page mixes two jobs: setup (templates, assessments) and daily work (people's progress, approvals). "Assignments always snapshot a published version" is engineering language. Nothing says *"next: publish the quiz"*. |
| Add a new hire and give them onboarding | **People → New hire** (added recently) and **Administration → User access** | The guided flow is good, but it is the only place that links the pieces. From the **user list** there is no way to reach onboarding: the row shows "Linked employee: Mila Demo" as plain text. The **employee profile** has seven tabs (Overview, Employment, History, Contracts, Compensation, Documents, Activity) and not one word about onboarding, and no "Start onboarding" action. |
| Follow progress | **Onboarding → Manage** (bottom half) and **Onboarding → Insights** | Progress is hidden below the templates. Insights is good but is the fifth tab. |
| Land on "Onboarding" from the menu | **Work → Onboarding** → `/onboarding` | For the HR account this shows an **error**: *"Onboarding is not available. Your account may not yet be linked to an employee profile."* The HR admin has no employee record, so the first screen HR sees under Onboarding is a dead end. The tabs on that page start with *My journey*, which HR does not have. |
| Know what is missing | — | Nothing in the app answers "what do I do next?". The Home page for HR shows celebrations, who's out, a holidays widget that says "module not configured", and a link "Add an employee" that goes to the old bare form, not the New hire flow. |

### Why it feels unclear

1. **No home for the HR onboarding job.** The flow exists in the data model but no screen shows it as a sequence.
2. **Same concept, several names.** Template / package / journey / assignment; PDF / document / knowledge article; assessment / quiz. HR cannot tell which words are the same thing.
3. **Entry points are organised by data type, not by task.** "Company", "Knowledge", "Projects", "Onboarding", "User access" are database tables turned into menu items. HR thinks in tasks: *get documents in, build the package, add the person, see how they are doing.*
4. **Dead ends and silent gaps.** The error landing page; the user list that does not lead anywhere; a profile with no onboarding.

## 2. Problems, ranked by how much they hurt

1. **The Onboarding menu item lands HR on an error page.** (Blocking; happens on every visit.)
2. **No overview of the onboarding setup and no "next step".** HR cannot tell whether documents, packages and quizzes are ready, or who is stuck. (Blocking for first-time use.)
3. **The employee profile and the user list say nothing about onboarding** and offer no way to start it. This is exactly the "I invited a user, now what?" gap. (Blocking for the daily task.)
4. **Documents live in three places with three names.** (Confusing; costs time every time.)
5. **Setup and progress share one page**, with setup first and in engineering words. (Confusing.)
6. **Inconsistent vocabulary** across pages and dialogs. (Erodes trust.)
7. **Home page is not useful for HR.** Placeholder widgets; the wrong "add" link. (Minor but first impression.)
8. **Minor:** some quiz-builder actions use `window.confirm`; a few hints are long; the mobile sidebar covers content behind a scrim without a visible focus trap. (Noted; not in scope unless cheap.)

## 3. The target experience

### 3.1 Information architecture and menu

Organise the menu by task, keep it short, and use the existing "workspace tabs" pattern inside each area.

```
Workspace      Home · My info
People         Directory · New hire (HR)
Onboarding     Onboarding (HR/Manager: overview; employees: my onboarding) · Documents (HR)
Intelligence   AI assistant
Administration User access · Audit history · AI settings (HR)
```

Inside **Onboarding**, tabs depend on the role:

- HR / Manager: **Overview · Packages & quizzes · Progress · Insights · Company knowledge · Projects** (plus *My onboarding* first, only for accounts linked to an employee).
- Employee: **My onboarding · Company knowledge · Projects**.

**Documents** is the former "Company" page, renamed to what it is: *"Company and project documents used in onboarding."*
Company knowledge (articles) and Projects stay where employees already find them.

### 3.2 One name per concept

| Use | Instead of |
|---|---|
| **package** (company package, project package) | template, journey template |
| **onboarding** (someone's onboarding, "start onboarding") | journey, assignment, assign a journey |
| **quiz** | assessment |
| **document** | PDF, knowledge, knowledge article (articles keep their name only on the Company knowledge page, which is about written guidance) |
| **progress** | assignments list |

### 3.3 The main flows

**A. Setting up (first week, then rarely).** Onboarding → Overview shows a checklist with live status and one next action:

```
Set up onboarding
 ✓ Company documents      5 documents, all scanned          Add documents
 ✓ Project documents      Pulse One: 4 documents            Add documents
 ✓ Company package        published, 6 steps                Open
 ✗ Company quiz           draft, 0 questions   ← next       Build the quiz
 ✓ Project package        Pulse One: published
 ✗ Project quiz           not started                       Build the quiz
```

Each line links to the exact screen. The first unfinished line is marked as the next step. When everything is done the
checklist collapses to one green line and the page is about people.

**B. Adding someone (every hire).** People → New hire (exists). From now on the same action is also reachable from the
places HR actually is: the Overview page ("Add a new hire"), Home, and the employee profile ("Start onboarding" for
someone already created, for example through the old form or an import).

**C. Following people (daily).** Onboarding → Progress: everyone in onboarding, with status, reading progress, quiz
result, overdue flags and the "Approve" actions. Onboarding → Insights keeps the numbers and the "needs attention"
list. The employee profile gets an **Onboarding** tab with the same detail for that one person.

### 3.4 Page layouts

- **Overview** (HR/Manager): page header with the primary action *Add a new hire*; the setup checklist card; a
  "People in onboarding" card (counts: in progress, waiting for review, overdue, completed this month) with a
  link to Progress; "Needs attention" rows reused from Insights.
- **Packages & quizzes**: the package cards as today, with package type, "Default for new employees", and a clear
  quiz status line per card (*No quiz yet → Build quiz*, *Draft, 12 questions → Review and publish*, *Published*).
  Empty state explains the order: *"First add documents, then create a package from them."*
- **Progress**: one row per person: name, package(s), start date, progress bar, status, quiz result, actions.
  Row click opens the person's profile on the Onboarding tab.
- **Employee profile → Onboarding tab** (HR/Manager): current onboarding card (progress, documents read, quiz
  status, attempts, reviewer), or an empty state with **Start onboarding** that opens the existing assignment
  dialog with this person preselected. Also shown as a short line in the profile summary.
- **Home** (HR): a top widget "Onboarding" with the setup status in one sentence, the attention count, and the
  buttons *Add a new hire* / *Open onboarding*. The "Add an employee" link now opens New hire.
- **User access**: the linked employee becomes a link to the profile, and the description says where onboarding
  is started.

### 3.5 Empty states and next-step guidance

Every list that can be empty says what to do first and offers that action: no documents → *Add documents*; no
packages → *Add documents first, then create a package*; no quiz → *Build the quiz*; no one in onboarding →
*Add a new hire*. The Overview checklist is the single place that always shows the next step.

### 3.6 Accessibility and small screens

Keep the current tokens. Checklist and progress states use icon + text, never colour alone. All new controls are
real buttons/links with labels; tab lists use `nav` + `aria-current`. Layouts stack at 360px with no sideways
scrolling.

## 4. What was changed

Filled in as the work lands (one entry per problem above). See the commit history on `feature/hr-ux`.

## 5. Not done / not verified

Filled in at the end.
