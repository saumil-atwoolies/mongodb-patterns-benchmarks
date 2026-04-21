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
dotnet build
dotnet test
\```

### Code Conventions

| Concern | Convention |
|---------|-----------|
| Indentation | 4 spaces |
| Encoding | UTF-8 |
| Line endings | CRLF |
| Nullable refs | Enabled |
| Domain rules | No domain entity depends on infrastructure |
| Repository pattern | Interfaces in Domain, implementations in Infrastructure |

> Adapt this section if the project's technology stack differs.

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
