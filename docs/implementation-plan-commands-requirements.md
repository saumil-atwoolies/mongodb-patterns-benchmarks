# Requirements: `create-implementation-plan` and `implement-plan` Slash Commands

> **Purpose:** This document fully specifies two complementary AI-agent slash commands for trackable, resumable implementation planning and execution. Feed this document to any project to recreate identical commands.

---

## Table of Contents

1. [Overview](#1-overview)
2. [Prerequisite Directory Structure](#2-prerequisite-directory-structure)
3. [File Manifest](#3-file-manifest)
4. [Command 1 — `/create-implementation-plan`](#4-command-1--create-implementation-plan)
5. [Command 2 — `/implement-plan`](#5-command-2--implement-plan)
6. [Implementation Plan Markdown Schema](#6-implementation-plan-markdown-schema)
7. [Saved-Context File Schemas](#7-saved-context-file-schemas)
8. [Git Conventions](#8-git-conventions)
9. [Platform-Specific Conventions (Customise Per Project)](#9-platform-specific-conventions-customise-per-project)
10. [How to Adapt to a New Project](#10-how-to-adapt-to-a-new-project)

---

## 1. Overview

These two commands form a **plan-then-execute** workflow for AI-assisted development:

| Command | Role |
|---------|------|
| `/create-implementation-plan` | Analyse a design context, read the codebase, and produce a structured implementation plan with atomic, ordered, git-committable tasks. |
| `/implement-plan` | Pick up pending plans, execute tasks sequentially (implement → build/test → mark done → save context → commit), and loop until complete. Fully resumable. |

### Design Principles Baked In

- **DDD** — Aggregates own invariants; repository interfaces in Domain, implementations in Infrastructure; no domain-to-infrastructure dependency.
- **TDD** — Every behavioural change has at least one test written before or alongside the implementation. Tests must pass before a task is marked done.
- **DRY** — Extract shared logic only when the same concept appears in three or more places.
- **SOLID** — Single-responsibility classes; depend on interfaces, not concretions; extension over modification.

---

## 2. Prerequisite Directory Structure

The following directories must exist (or be created on first use):

```
<project-root>/
├── implementation/
│   ├── pending/          # Active plans live here
│   └── completed/        # Plans moved here when all tasks are done
└── Saved-Contexts/
    ├── implement/        # Planning-phase context snapshots
    └── implemented/      # Per-task execution context snapshots
```

Both commands must create any missing directories automatically before writing files.

---

## 3. File Manifest

Each command requires files in **three** locations to support VS Code Copilot Chat (`.github/`), VS Code Copilot prompt files (`.github/prompts/`), and Claude Code (`.claude/commands/`).

### 3.1 VS Code Copilot Agent Definitions

Location: `.github/agents/`

#### `.github/agents/create-implementation-plan.agent.md`

This is the **full agent system prompt**. It must contain:

- YAML front matter with `description`, `tools`, and `argument-hint`.
- The complete step-by-step instructions (Steps 1–8, documented below in §4).

```yaml
---
description: >
  Create a trackable implementation plan markdown file in implementation/pending/ for a given design
  context. The plan complies with DDD, TDD, DRY, and SOLID principles. Each task is atomic,
  ordered, and resumable. Saves the plan to Saved-Contexts/implement/ and commits all changes.
  USE WHEN: user says "create implementation plan", "plan this feature", "create a plan for",
  or invokes /create-implementation-plan with a design context.
tools: [read, edit, search, execute, todo, agent]
argument-hint: "Describe the design context: what is being built, why, and how it fits the existing architecture."
---
```

#### `.github/agents/implement-plan.agent.md`

Same structure — YAML front matter + full step-by-step instructions (Steps 1–10, documented below in §5).

```yaml
---
description: >
  Review all pending implementation plans in implementation/pending/. Move fully-completed plans to
  implementation/completed/. Sort remaining plans by timestamp (oldest first) and implement tasks
  sequentially — implement → build/test → mark done → save context → commit. With no argument,
  continue until no pending tasks remain. With a positive integer argument, implement only that
  many next pending tasks. With an inclusive range argument like 3-5, implement only those task
  numbers in the active plan. Fully resumable.
  USE WHEN: user says "implement plan", "run next task", "continue implementation",
  "implement pending tasks", or invokes /implement-plan.
tools: [read, edit, search, execute, todo, agent]
argument-hint: "Optional: blank = all tasks, N = next N tasks, start-end = exact task range in the active plan"
---
```

### 3.2 VS Code Copilot Prompt Files (Slash-Command Wrappers)

Location: `.github/prompts/`

These are thin wrappers that delegate to the agent.

#### `.github/prompts/create-implementation-plan.prompt.md`

```yaml
---
description: "Create a trackable DDD/TDD/DRY/SOLID-compliant implementation plan in implementation/pending/. Provide the design context as your message."
agent: "create-implementation-plan"
---

Create a trackable implementation plan for the design context described in this message.
```

#### `.github/prompts/implement-plan.prompt.md`

```yaml
---
description: "Review pending implementation plans and execute tasks. Leave the argument blank to run everything, pass a positive integer to execute that many next tasks, or pass an inclusive task range like 3-5 for the active plan."
agent: "implement-plan"
argument-hint: "Optional: blank = all tasks, N = next N tasks, start-end = exact task range in the active plan"
---

Review pending implementation plans and execute tasks according to any optional slash-command argument supplied after `/implement-plan`.

- No argument: continue until no pending tasks remain.
- Positive integer `N`: implement only the next `N` pending tasks from the normal processing queue.
- Inclusive range `start-end`: implement only tasks `start` through `end` in the active plan selected by the normal processing queue.
- Any other argument format: stop and explain the accepted forms before doing any work.
```

### 3.3 Claude Code Command Files

Location: `.claude/commands/`

These use `$ARGUMENTS` as the placeholder for user input.

#### `.claude/commands/create-implementation-plan.md`

Body contains `$ARGUMENTS` followed by the full step-by-step instructions (identical logic to the `.agent.md` but formatted for Claude Code's command runner).

#### `.claude/commands/implement-plan.md`

Body contains `$ARGUMENTS` followed by the full step-by-step instructions.

---

## 4. Command 1 — `/create-implementation-plan`

### Trigger Phrases

- `/create-implementation-plan <design context>`
- "create implementation plan", "plan this feature", "create a plan for"

### Input

A **design context** describing what is being built, why, and how it fits the existing architecture. If the input is empty or too vague, the agent must stop and ask for clarification.

### Processing Steps

#### Step 1: Determine the plan filename

- Format: `<yyyy-MM-dd-HH-mm>-<logical-name>.md`
- `<logical-name>`: 2–5 kebab-case words derived from the design context.
- Example: `2026-04-10-14-35-order-aggregate-refactor.md`

#### Step 2: Read existing project context

**Mandatory** — the agent must read enough of the codebase before writing anything:

- Domain models, aggregates, value objects relevant to the design context.
- Application services, repository interfaces, infrastructure implementations.
- Existing test patterns (structure, naming, fixtures).
- Related plans already in `implementation/pending/` or `implementation/completed/`.

#### Step 3: Produce the implementation plan

Write a markdown file following the **Implementation Plan Markdown Schema** (§6).

Rules for tasks:
- Each task is a single, atomic, git-committable unit — one logical concern per task.
- Tasks are ordered so each builds only on code introduced by previous tasks.
- Acceptance criteria are concrete and testable — not vague ("implement X correctly").
- Commit messages are in imperative mood and describe the change, not the ticket.
- No tasks for hypothetical future requirements.

#### Step 4: Ensure output directories exist

Create if missing:
- `implementation/pending/`
- `implementation/completed/`
- `Saved-Contexts/implement/`

#### Step 5: Write the plan file

Write to `implementation/pending/<filename>`.

#### Step 6: Save the planning context

Write a context file to `Saved-Contexts/implement/<filename>` containing:

1. **Codebase observations** — relevant existing files, patterns, dependencies.
2. **Plan rationale** — why tasks are scoped/ordered this way; trade-offs considered.
3. **Risks and open questions** — ambiguities, potential blockers, assumptions.

If the target path already exists, append a numeric suffix (e.g., `-2.md`, `-3.md`).

#### Step 7: Commit

```bash
git add implementation/pending/<filename> Saved-Contexts/implement/<filename>
git commit -m "Add implementation plan: <logical-name>"
```

Do **NOT** push.

#### Step 8: Report to the user

Print:
- Full path of the plan file.
- Number of tasks.
- First task name and acceptance-criteria count.
- Reminder to run `/implement-plan` to begin execution.

---

## 5. Command 2 — `/implement-plan`

### Trigger Phrases

- `/implement-plan` (no argument = run all)
- `/implement-plan 3` (implement next 3 tasks)
- `/implement-plan 3-5` (implement tasks 3 through 5 in the active plan)
- "implement plan", "run next task", "continue implementation"

### Execution Modes

| Argument | Mode | Behaviour |
|----------|------|-----------|
| *(empty)* | `run-all` | Execute all pending tasks across all pending plans until none remain. |
| `N` (positive integer) | `count-limited` | Execute only the next `N` tasks. Spans plans if the current one finishes before the budget is exhausted. Plan-completion commits do not consume the budget. |
| `start-end` (e.g., `3-5`) | `range` | Execute only tasks `start` through `end` in the active plan. Cannot skip earlier pending tasks — if any task with a number < `start` is still pending, stop and explain. |
| *(anything else)* | *(invalid)* | Stop immediately and explain accepted formats. |

### Stopping Conditions

The agent stops **only** when:

1. **`run-all`**: Every plan moved to `implementation/completed/` → print "All implementation plans complete."
2. **`count-limited`**: Requested task count reached. Still move plan to completed if the final task finished it.
3. **`range`**: Every task in `[start, end]` is done. Still move plan to completed if fully finished.
4. **Build/test failure** that cannot be resolved after reasonable diagnosis — leave task `pending`, explain.
5. **User action required** — decision, credential, or external action needed — stop and ask.

The agent must **not** stop after a single task unless a stopping condition is met. It loops automatically.

### Processing Steps

#### Step 1: Audit the pending folder

- List all `.md` files in `implementation/pending/`.
- For each file: read the Progress Tracker. If every task is `done`, move to `implementation/completed/` and commit:
  ```bash
  git mv implementation/pending/<filename> implementation/completed/<filename>
  git commit -m "Complete implementation plan: <filename without extension>"
  ```
- Re-list pending. If empty → "All implementation plans complete." → stop.

#### Step 2: Sort remaining plans

Sort by `yyyy-MM-dd-HH-mm` timestamp prefix ascending (oldest first). This is the processing queue.

#### Step 3: Find the active plan and next task

- Take the **first** (oldest) plan in the queue → active plan.
- **`run-all` / `count-limited`**: Find the first row with status `pending` → active task.
- **`range`**: Validate `start` and `end` exist. Ensure no task < `start` is still pending. Find first pending row in `[start, end]`.

#### Step 4: Read context before implementing

**Mandatory reads** before writing any code:

1. The full active plan file — Design Context and all prior tasks.
2. The Task Details section for the active task — scope, spec, tests.
3. Every source file the task will create or modify.
4. Files created by earlier tasks in this plan.
5. Related existing project files for patterns/conventions.

#### Step 5: Implement the task

- Follow the task's **Spec** and **Tests to write** precisely.
- Write tests first or alongside implementation.
- Create/modify only files in the task Scope.
- Do NOT add features, refactoring, or improvements beyond the task.
- Follow project code conventions (see §9).

#### Step 6: Build and test

Run the project's build and test commands. Specifics depend on the project's tech stack (see §9).

- Build failure → diagnose, fix, rebuild. Do not proceed until green.
- Test failure → diagnose, fix, re-test. Do not proceed until all pass.
- **Never commit broken code.**

#### Step 7: Mark the task done

In the plan file:
- Change the task's status cell from `` `pending` `` to `` `done` ``.
- Tick all checkboxes under "Definition of done" for that task.

#### Step 8: Save the task context

Write to `Saved-Contexts/implemented/<plan-name>-Task-<N>-of-<total>-<task-name>.md`:

1. **What was implemented** — files created/modified, approach, deviations.
2. **Key decisions** — non-obvious choices and rationale.
3. **Issues encountered** — build/test problems and resolutions.
4. **State for next task** — new interfaces, naming conventions, things the next task should know.

If path exists, append numeric suffix.

#### Step 9: Commit

```bash
git add -A
git commit -m "Task-<N>-of-<total>-<task-name>: <exact commit message from Progress Tracker>"
```

Example: `Task-3-of-7-add-repository-interface: Add IOrderRepository to Domain`

#### Step 10: Loop

- **Do NOT ask the user for permission** to continue.
- Re-read the Progress Tracker. If all tasks done → move plan to completed.
- Check stopping conditions. If not met → go back to Step 1 (run-all), Step 1 (count-limited with budget > 0), or Step 3 (range with pending rows in range).

### Constraints (Non-Negotiable)

- **One task per commit.** Never bundle multiple tasks.
- Every commit must build and pass all tests.
- Use **exact** commit messages from the Progress Tracker.
- Do NOT push — only local commits.
- Do NOT modify files outside the current task's scope.
- Do NOT add abstractions, helpers, or "improvements" the task doesn't specify.
- If interrupted mid-task, leave plan unchanged (task still `pending`) for clean retry.
- In `count-limited` mode, only successful task implementations consume the budget.
- In `range` mode, never silently skip earlier pending tasks.

---

## 6. Implementation Plan Markdown Schema

Every plan file in `implementation/pending/` must follow this exact structure:

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

## Progress Tracker

| # | Task | Status | Commit Message |
|---|------|--------|----------------|
| 1 | <task name> | `pending` | `<imperative-mood commit message>` |
| 2 | <task name> | `pending` | `<imperative-mood commit message>` |
| … | … | … | … |

## Task Details

### Task 1 — <Task Name>

**Scope:** <What files to create or modify. Be specific: project, namespace, class.>

**Spec:**
- <Bullet-point acceptance criteria. Each verifiable by a test or build step.>
- <Reference relevant domain concepts, interfaces, patterns.>

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

### Key Schema Rules

- **YAML front matter** is required with `created`, `context`, and `status` fields.
- **Progress Tracker** is the single source of truth for task status. Status values: `` `pending` `` or `` `done` ``.
- **Commit messages** in the tracker are in imperative mood and are used verbatim by the implement agent.
- **Task numbering** in the `#` column is sequential starting from 1 and must match the `### Task N` headings.

---

## 7. Saved-Context File Schemas

### 7.1 Planning Context (`Saved-Contexts/implement/<filename>`)

Created by `/create-implementation-plan` at plan-creation time.

Required sections:
1. **Codebase observations** — relevant files, patterns, dependencies found during analysis.
2. **Plan rationale** — why tasks are scoped and ordered this way; trade-offs considered.
3. **Risks and open questions** — ambiguities, blockers, assumptions.

### 7.2 Task Execution Context (`Saved-Contexts/implemented/<plan>-Task-<N>-of-<total>-<task-name>.md`)

Created by `/implement-plan` after each task is completed.

Required sections:
1. **What was implemented** — files created/modified, approach, deviations from plan.
2. **Key decisions** — non-obvious choices and rationale.
3. **Issues encountered** — problems diagnosed and resolved.
4. **State for next task** — new interfaces, naming patterns, or context the next task needs.

### Collision Handling

Both context types: if the target file already exists, append a numeric suffix before the extension (`-2.md`, `-3.md`) until unique. Never overwrite.

---

## 8. Git Conventions

| Action | Commit Message Format |
|--------|-----------------------|
| Plan creation | `Add implementation plan: <logical-name>` |
| Task completion | `Task-<N>-of-<total>-<task-name>: <exact commit message from tracker>` |
| Plan completion (move) | `Complete implementation plan: <filename without extension>` |

- All commits are **local only** — never push.
- One task = one commit. Never bundle.
- Every commit must build and pass tests.

---

## 9. Platform-Specific Conventions (Customise Per Project)

This section must be **adapted for each target project**. Replace the examples below with the project's actual build/test commands and code conventions.

### Build and Test Commands

```bash
# Example for .NET 8
dotnet build
dotnet test --settings test.runsettings

# Example for Python
python -m pytest tests/ -v

# Example for Node.js
npm run build
npm test
```

### Code Conventions

| Concern | Convention |
|---------|-----------|
| Indentation | 4 spaces (adapt as needed) |
| Encoding | UTF-8 |
| Line endings | CRLF for C#, LF for Python/JS (adapt as needed) |
| Domain rules | No domain entity depends on infrastructure |
| Repository pattern | Interfaces in Domain, implementations in Infrastructure |
| Collections | `ImmutableArray` (C#) or equivalent immutable types |
| Type annotations | Required (C# nullable refs, Python type hints, TypeScript strict) |

---

## 10. How to Adapt to a New Project

### Step-by-step setup

1. **Create the directory structure** from §2.

2. **Copy the six command files** from §3:
   - `.github/agents/create-implementation-plan.agent.md`
   - `.github/agents/implement-plan.agent.md`
   - `.github/prompts/create-implementation-plan.prompt.md`
   - `.github/prompts/implement-plan.prompt.md`
   - `.claude/commands/create-implementation-plan.md`
   - `.claude/commands/implement-plan.md`

3. **Customise §9 content** inside the agent/command files:
   - Replace build/test commands with the project's actual commands.
   - Replace code conventions with the project's style guide.
   - Update language-specific references (e.g., if it's a Go project, remove C#/Python conventions and add Go conventions).

4. **Register agents in `AGENTS.md`** (if used):
   ```markdown
   ## Agents
   - `create-implementation-plan` — Creates trackable implementation plans.
   - `implement-plan` — Executes pending implementation plans task by task.
   ```

5. **Test the workflow:**
   - Run `/create-implementation-plan` with a small design context.
   - Verify the plan appears in `implementation/pending/` and context in `Saved-Contexts/implement/`.
   - Run `/implement-plan 1` to execute just the first task.
   - Verify the task was implemented, tests pass, task is marked `done`, context saved to `Saved-Contexts/implemented/`, and a commit was made.

### What NOT to change

- The **Progress Tracker table format** — the implement agent parses it.
- The **YAML front matter fields** (`created`, `context`, `status`) — used for plan identification.
- The **filename convention** (`yyyy-MM-dd-HH-mm-<name>.md`) — used for chronological sorting.
- The **commit message format** — used verbatim by the implement agent.
- The **stopping conditions** and **execution modes** — these are the core resumability contract.
- The **mandatory codebase reading steps** — skipping these produces plans/code disconnected from reality.

---

## Appendix: Quick Reference

```
/create-implementation-plan <describe what to build>
  → Reads codebase
  → Writes plan to implementation/pending/
  → Saves context to Saved-Contexts/implement/
  → Commits locally

/implement-plan              # Run all pending tasks
/implement-plan 3            # Run next 3 tasks
/implement-plan 3-5          # Run tasks 3 through 5 in active plan
  → Picks oldest pending plan
  → For each task: implement → build → test → mark done → save context → commit
  → Loops until stopping condition met
  → Moves completed plans to implementation/completed/
```
