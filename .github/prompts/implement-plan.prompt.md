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

Run the build and test commands from the plan's Platform Conventions section:

```bash
dotnet build
dotnet test
```

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
