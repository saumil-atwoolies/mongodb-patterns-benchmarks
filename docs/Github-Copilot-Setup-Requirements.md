# GitHub Copilot Agentic Workflow — Setup Requirements

> **Hand this file to GitHub Copilot agent in any repository and say:**
> *"Read this requirements document and set up everything it specifies in my repo."*
>
> No access to any other repository is needed. Every file, folder, and exact content is defined below.

---

## REQ-01: Prerequisites

The target repository must have:
- VS Code with GitHub Copilot extension (Chat + Agent mode enabled).
- Git initialised.
- A working build and test command (any language/framework).

---

## REQ-02: Create Folder Structure

Create the following directories (use `.gitkeep` files to commit empty folders):

```
.github/
.github/prompts/
docs/
artifacts/
artifacts/validated/
artifacts/pending/
artifacts/done/
artifacts/context/
artifacts/context/planning/
artifacts/context/implemented/
```

---

## REQ-03: Create `.github/copilot-instructions.md`

This file is auto-loaded by Copilot for every chat session. Create it with the content below, then **ask the developer to fill in the placeholders** (marked with `<...>`).

```markdown
# Project: <Project Name>

## Overview

<1–2 sentence description of what this project does.>

## Technology Stack

<List languages, frameworks, tools, and principles. Example:>
- <Language and version>
- <Framework(s)>
- <DDD, SOLID, DRY, TDD — whichever apply>

## Folder Structure

```
docs/                          # Raw and workflow requirement documents
artifacts/
  validated/                   # Validated requirements (output of /validate-requirements)
  pending/                     # Active implementation plans (output of /create-implementation-plan)
  done/                        # Completed implementation plans (moved by /implement-plan)
  context/
    planning/                  # Planning-phase context snapshots
    implemented/               # Per-task execution context snapshots
.github/
  copilot-instructions.md      # This file — auto-loaded by Copilot
  prompts/                     # Custom prompt files (slash commands)
```

## Workflow Commands

| Command | Purpose |
|---------|---------|
| `/validate-requirements` | Analyse a requirements file for completeness, clarity, feasibility, testability. Produces a validated doc in `artifacts/validated/`. |
| `/create-implementation-plan` | Read the codebase + a design context, produce a structured plan in `artifacts/pending/` with atomic tasks. |
| `/implement-plan` | Execute pending plan tasks: implement → build/test → mark done → save context → commit. Supports `run-all`, `count-limited (N)`, and `range (start-end)` modes. |
| `/resume` | Read the latest context snapshot and summarise project state for session resumption or team handoff. |

## Conventions

- **Plans**: `artifacts/pending/<yyyy-MM-dd-HH-mm>-<logical-name>.md`
- **Planning context**: `artifacts/context/planning/<plan-filename>.md`
- **Task context**: `artifacts/context/implemented/<plan-name>-Task-<N>-of-<total>-<task-name>.md`
- **Git commits**: one task = one commit; format `Task-<N>-of-<total>-<task-name>: <commit message>`; local only, never push
- **Build gate**: every commit must pass `<your build + test command>`
- **Principles**: <your principles — e.g., DDD, TDD, DRY, SOLID>
```

### Acceptance Criteria
- [ ] File exists at `.github/copilot-instructions.md`.
- [ ] Developer has been prompted to replace all `<...>` placeholders.

---

## REQ-04: Create `/validate-requirements` Prompt

Create file `.github/prompts/validate-requirements.prompt.md` with this exact content:

````markdown
---
description: "Analyse a requirements file for completeness, clarity, feasibility, and testability. Produces a validated requirements document in artifacts/validated/."
---

You are a requirements analyst. The developer has attached a requirements file as context.
Analyse it and produce a **validated requirements document**.

## Instructions

1. **Read** the attached requirements file thoroughly.

2. **Decompose** the requirements into discrete, testable items. For each requirement:
   - Assign a unique ID (e.g., `REQ-XX-001`).
   - Write a clear description.
   - List concrete acceptance criteria (checkboxes).
   - Flag any ambiguities and document how you resolved them.

3. **Assess** each requirement for:
   - **Completeness** — is anything missing?
   - **Clarity** — is the intent unambiguous?
   - **Feasibility** — can it be built with the stated technology stack?
   - **Testability** — can acceptance be verified by a test or build step?

4. **Document assumptions** you made during analysis.

5. **List dependencies** (external tools, services, prior work).

6. **Recommend an implementation order** based on dependencies between requirements.

7. **Produce a validation summary table** with columns: Req ID, Title, Status (VALID/NEEDS CLARIFICATION/INVALID), Notes.

## Output

Save the validated requirements document to:

```
artifacts/validated/validated-requirements-<source-name>.md
```

Where `<source-name>` is derived from the input filename (kebab-case, without extension).

## Context Snapshot

After producing the validated document, save a context snapshot to:

```
artifacts/context/planning/<source-name>-validation-<YYYY-MM-DD-HHmm>.md
```

The snapshot must contain:
1. **Action performed** — what was validated and key findings.
2. **Files created** — paths of all output files.
3. **Current status** — validation complete, ready for planning.
4. **Next steps** — recommend running `/create-implementation-plan` with the validated doc.

Commit both files locally:
```
git add artifacts/validated/ artifacts/context/planning/
git commit -m "Validate requirements: <source-name>"
```

Do NOT push.
````

### Acceptance Criteria
- [ ] File exists at `.github/prompts/validate-requirements.prompt.md`.
- [ ] Invoking `/validate-requirements` in Copilot Chat with an attached requirements file produces a validated document in `artifacts/validated/`.

---

## REQ-05: Create `/create-implementation-plan` Prompt

Create file `.github/prompts/create-implementation-plan.prompt.md` with this exact content:

````markdown
---
description: "Create a trackable DDD/TDD/DRY/SOLID-compliant implementation plan in artifacts/pending/. Provide the design context or attach a validated requirements document."
---

You are an implementation planner. The developer has provided a design context or attached a validated requirements document. Produce a structured implementation plan with atomic, ordered, git-committable tasks.

## Pre-Conditions

- If the input is empty or too vague, **stop and ask for clarification** before proceeding.

## Step 1: Determine the Plan Filename

- Format: `<yyyy-MM-dd-HH-mm>-<logical-name>.md`
- `<logical-name>`: 2–5 kebab-case words derived from the design context.
- Example: `2026-04-10-14-35-order-aggregate-refactor.md`

## Step 2: Read Existing Project Context (MANDATORY)

Before writing anything, you **must** read enough of the codebase:

- Domain models, aggregates, value objects relevant to the design context.
- Application services, repository interfaces, infrastructure implementations.
- Existing test patterns (structure, naming, fixtures).
- Related plans already in `artifacts/pending/` or `artifacts/done/`.
- The `.github/copilot-instructions.md` file for project conventions.

Do NOT skip this step. Plans disconnected from the actual codebase are useless.

## Step 3: Produce the Implementation Plan

Write a markdown file following this **exact schema**:

```markdown
---
created: <yyyy-MM-dd HH:mm>
context: <one-sentence summary of the design context>
status: pending
---

# Implementation Plan: <Descriptive Title>

## Design Context

<2–4 paragraphs explaining what is being built, why, and how it fits the existing
architecture. Reference specific existing files, classes, or modules by name.>

## Guiding Principles

- **DDD**: Aggregates own their invariants. Repository interfaces live in Domain.
  Infrastructure implements them. No domain entity may depend on infrastructure.
- **TDD**: Every behavioural change is covered by at least one test written before
  (or alongside) the implementation. Tests must pass before a task is marked done.
- **DRY**: Extract shared logic only when the same concept appears in three or more
  places. Prefer clarity over premature abstraction.
- **SOLID**: Single-responsibility classes; depend on interfaces not concretions;
  extension over modification.

## Platform Conventions

### Build and Test Commands

\```bash
<your build command>
<your test command>
\```

### Code Conventions

| Concern | Convention |
|---------|-----------|
| <concern> | <convention> |

> Adapt this section to match the project's technology stack.

## Progress Tracker

| # | Task | Status | Commit Message |
|---|------|--------|----------------|
| 1 | <task name> | `pending` | `<imperative-mood commit message>` |
| 2 | <task name> | `pending` | `<imperative-mood commit message>` |

## Task Details

### Task 1 — <Task Name>

**Scope:** <What files to create or modify. Be specific: project, namespace, class.>

**Spec:**
- <Bullet-point acceptance criteria. Each verifiable by a test or build step.>

**Tests to write:**
- <Specific test cases: class under test, method, scenario, expected outcome.>

**Definition of done:**
- [ ] All specified files created / modified.
- [ ] All listed tests written and passing.
- [ ] Build succeeds with no warnings on new code.
- [ ] Task row in Progress Tracker updated to `done`.
- [ ] Changes committed with the exact commit message from the tracker.

---

### Task 2 — <Task Name>

*(same structure as Task 1)*
```

### Rules for Tasks

- Each task is a **single, atomic, git-committable unit** — one logical concern per task.
- Tasks are **ordered** so each builds only on code introduced by previous tasks.
- Acceptance criteria are **concrete and testable** — not vague ("implement X correctly").
- Commit messages are in **imperative mood** and describe the change, not the ticket.
- **No tasks for hypothetical future requirements.**
- Task numbering in `#` column is sequential from 1 and matches `### Task N` headings.

## Step 4: Ensure Output Directories Exist

Create if missing:
- `artifacts/pending/`
- `artifacts/done/`
- `artifacts/context/planning/`
- `artifacts/context/implemented/`

## Step 5: Write the Plan File

Write to `artifacts/pending/<filename>`.

## Step 6: Save the Planning Context

Write a context file to `artifacts/context/planning/<filename>` containing:

1. **Codebase observations** — relevant existing files, patterns, dependencies found during analysis.
2. **Plan rationale** — why tasks are scoped and ordered this way; trade-offs considered.
3. **Risks and open questions** — ambiguities, potential blockers, assumptions.

If the target path already exists, append a numeric suffix (`-2.md`, `-3.md`) until unique. **Never overwrite.**

## Step 7: Commit

```bash
git add artifacts/pending/<filename> artifacts/context/planning/<filename>
git commit -m "Add implementation plan: <logical-name>"
```

Do **NOT** push.

## Step 8: Report to the User

Print:
- Full path of the plan file.
- Number of tasks.
- First task name and acceptance-criteria count.
- Reminder: run `/implement-plan` to begin execution.
````

### Acceptance Criteria
- [ ] File exists at `.github/prompts/create-implementation-plan.prompt.md`.
- [ ] Invoking `/create-implementation-plan` with a design context produces a plan in `artifacts/pending/`.

---

## REQ-06: Create `/implement-plan` Prompt

Create file `.github/prompts/implement-plan.prompt.md` with this exact content:

````markdown
---
description: "Review pending implementation plans and execute tasks. No argument = run all, N = next N tasks, start-end = exact task range in the active plan."
---

You are an implementation executor. Review all pending implementation plans in `artifacts/pending/` and execute tasks sequentially: implement → build/test → mark done → save context → commit. Loop until a stopping condition is met.

## Argument Parsing

Parse the user's argument after `/implement-plan`:

| Argument | Mode | Behaviour |
|----------|------|-----------|
| *(empty)* | `run-all` | Execute all pending tasks across all pending plans until none remain. |
| `N` (positive integer) | `count-limited` | Execute only the next N tasks. Spans plans if the current one finishes before the budget is exhausted. Plan-completion commits do NOT consume the budget. |
| `start-end` (e.g., `3-5`) | `range` | Execute only tasks start through end in the active plan. Cannot skip earlier pending tasks — if any task with number < start is still pending, stop and explain. |
| *(anything else)* | *(invalid)* | Stop immediately and explain the three accepted formats. |

---

## Step 1: Audit the Pending Folder

- List all `.md` files in `artifacts/pending/`.
- For each file: read the Progress Tracker table. If **every** task is `done`, move to `artifacts/done/` and commit:
  ```bash
  git mv artifacts/pending/<filename> artifacts/done/<filename>
  git commit -m "Complete implementation plan: <filename without extension>"
  ```
- Re-list pending. If empty → print **"All implementation plans complete."** → stop.

## Step 2: Sort Remaining Plans

Sort by `yyyy-MM-dd-HH-mm` timestamp prefix **ascending** (oldest first). This is the processing queue.

## Step 3: Find the Active Plan and Next Task

- Take the **first** (oldest) plan in the queue → active plan.
- **`run-all` / `count-limited`**: Find the first row with status `pending` → active task.
- **`range`**: Validate start and end exist in the Progress Tracker. Ensure no task with # < start is still `pending`. Find first pending row in [start, end].

## Step 4: Read Context Before Implementing (MANDATORY)

Before writing **any** code, you **must** read:

1. The **full active plan file** — Design Context, Guiding Principles, Platform Conventions, and all prior tasks.
2. The **Task Details section** for the active task — Scope, Spec, Tests to write.
3. **Every source file** the task will create or modify (if they exist).
4. **Files created by earlier tasks** in this plan.
5. **Related existing project files** for patterns and conventions.

Do NOT skip this step. Implementing without reading produces code disconnected from the codebase.

## Step 5: Implement the Task

- Follow the task's **Spec** and **Tests to write** precisely.
- Write tests **first or alongside** the implementation (TDD).
- Create/modify **only** files listed in the task's Scope.
- Do NOT add features, refactoring, or improvements beyond the task.
- Follow the project's Platform Conventions from the plan.

## Step 6: Build and Test (GATE — NEVER SKIP)

Run the build and test commands from the plan's Platform Conventions section.

- **Build failure** → diagnose, fix, rebuild. Do not proceed until green.
- **Test failure** → diagnose, fix, re-test. Do not proceed until all pass.
- **NEVER commit broken code.**

## Step 7: Mark the Task Done

In the active plan file:
- Change the task's status cell from `pending` to `done` in the Progress Tracker.
- Tick **all** checkboxes under "Definition of done" for that task.

## Step 8: Save the Task Context

Write to `artifacts/context/implemented/<plan-name>-Task-<N>-of-<total>-<task-name>.md`:

1. **What was implemented** — files created/modified, approach taken, any deviations from the plan.
2. **Key decisions** — non-obvious choices and their rationale.
3. **Issues encountered** — build/test problems and how they were resolved.
4. **State for next task** — new interfaces, naming conventions, things the next task should know.

If the target path already exists, append a numeric suffix (`-2.md`, `-3.md`) until unique. **Never overwrite.**

## Step 9: Commit

```bash
git add -A
git commit -m "Task-<N>-of-<total>-<task-name>: <exact commit message from Progress Tracker>"
```

Example: `Task-3-of-7-add-repository-interface: Add IOrderRepository to Domain`

Do **NOT** push.

## Step 10: Loop

- Do **NOT** ask the user for permission to continue.
- Re-read the Progress Tracker. If all tasks are `done` → move plan to `artifacts/done/` and commit (see Step 1 format).
- Check stopping conditions. If not met → go back to **Step 1** (run-all / count-limited with budget > 0) or **Step 3** (range with pending rows in range).

---

## Stopping Conditions

The agent stops **only** when:

1. **`run-all`**: Every plan moved to `artifacts/done/` → print "All implementation plans complete."
2. **`count-limited`**: Requested task count reached. Still move plan to completed if the final task finished it.
3. **`range`**: Every task in [start, end] is done. Still move plan to completed if fully finished.
4. **Build/test failure** that cannot be resolved after reasonable diagnosis → leave task `pending`, explain what happened and what the developer needs to fix.
5. **User action required** — a decision, credential, or external action is needed → stop and ask.

Do **NOT** stop after a single task unless a stopping condition is met. Loop automatically.

---

## Non-Negotiable Constraints

- **One task per commit.** Never bundle multiple tasks.
- Every commit must build and pass all tests.
- Use **exact** commit messages from the Progress Tracker.
- Do NOT push — only local commits.
- Do NOT modify files outside the current task's scope.
- Do NOT add abstractions, helpers, or "improvements" the task doesn't specify.
- If interrupted mid-task, leave the plan unchanged (task still `pending`) for clean retry.
- In `count-limited` mode, only successful task implementations consume the budget.
- In `range` mode, never silently skip earlier pending tasks.
````

### Acceptance Criteria
- [ ] File exists at `.github/prompts/implement-plan.prompt.md`.
- [ ] Invoking `/implement-plan` executes tasks from pending plans autonomously.

---

## REQ-07: Create `/resume` Prompt

Create file `.github/prompts/resume.prompt.md` with this exact content:

````markdown
---
description: "Resume from the last saved context. Reads the latest context snapshot and summarises project state for session continuation or team handoff."
---

You are a session resumption assistant. Read the latest context snapshots and summarise the current project state so work can continue seamlessly.

## Instructions

1. **Scan both context directories** for the most recent files:
   - `artifacts/context/planning/` — planning-phase snapshots
   - `artifacts/context/implemented/` — task execution snapshots

2. **Read the most recent file** (by filename sort — filenames contain timestamps or plan names that sort chronologically).

3. **Read all plan files** in `artifacts/pending/` to determine current plan status:
   - How many plans exist?
   - For each plan: how many tasks total, how many done, how many pending?

4. **Read any completed plans** in `artifacts/done/` for recent history.

5. **Produce a summary** with these sections:

   ### Last Action
   - What was done (from the latest context file).
   - When (from filename timestamp or file content).
   - Who (from git log if available).

   ### Current Plan Status
   - Active plan name and path.
   - Task progress: N of M done.
   - Next pending task name and description.

   ### Recent History
   - Last 3–5 completed tasks (from context files or plan trackers).

   ### Next Steps
   - What to do next (e.g., "Run `/implement-plan` to continue" or "Run `/create-implementation-plan` to plan the next feature").
   - Any blockers or decisions noted in the last context snapshot.

   ### Open Issues
   - Any risks, blockers, or open questions from context files.

6. **Do NOT modify any files.** This command is read-only.
````

### Acceptance Criteria
- [ ] File exists at `.github/prompts/resume.prompt.md`.
- [ ] Invoking `/resume` produces a project state summary without modifying any files.

---

## REQ-08: Commit and Verify

After creating all files:

1. Run `git add -A && git status` to verify all files are staged.
2. Commit: `git commit -m "Set up GitHub Copilot agentic workflow"`.
3. Do NOT push.
4. Report to the user:
   - List of all files created.
   - Reminder to edit `.github/copilot-instructions.md` and replace `<...>` placeholders with their project's details.
   - Reminder that the workflow is ready: start by putting a requirements file in `docs/` and running `/validate-requirements`.

### Acceptance Criteria
- [ ] All files from REQ-02 through REQ-07 exist and are committed.
- [ ] `.github/copilot-instructions.md` contains placeholder markers for the developer to fill in.
- [ ] Running `/validate-requirements`, `/create-implementation-plan`, `/implement-plan`, and `/resume` all function as described.

---

## Typical Workflow After Setup

```
1. Write requirements in docs/
         │
         ▼
2. /validate-requirements  ──►  artifacts/validated/
         │
         ▼
3. /create-implementation-plan  ──►  artifacts/pending/
         │
         ▼
4. /implement-plan  ──►  implements tasks, commits locally
         │                  ──►  artifacts/context/implemented/
         │                  ──►  artifacts/done/ (when plan completes)
         ▼
5. /resume  ──►  summarise state for next session or team member
```
