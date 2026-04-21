# Validated Requirements — GitHub Copilot Workflow Setup

**Source**: `docs/github-copilot-workflow-requirements.md`, `docs/implementation-plan-commands-requirements.md`
**Validated**: 2026-04-21 (rev 2)
**Status**: VALIDATED — Ready for task planning

---

## 1. Requirement Decomposition

The raw requirements have been decomposed into discrete, testable requirements grouped by capability.
Revision 2 incorporates gaps 3–14 from `docs/implementation-plan-commands-requirements.md`.

---

### REQ-CW-001: Custom Prompt Files (Slash Commands)

**Description**: Create VS Code GitHub Copilot custom prompt files (`.github/prompts/*.prompt.md`) that act as reusable slash commands. Do NOT use spec-kit or any external tooling.

**Acceptance Criteria**:
- [ ] Prompt files are placed under `.github/prompts/` directory
- [ ] Each prompt file uses `.prompt.md` extension (VS Code Copilot convention)
- [ ] Prompt files are invocable from Copilot Chat via `/` command or `#` file reference

**Validation Notes**:
- VS Code Copilot supports custom prompt files in `.github/prompts/*.prompt.md` — this is the standard mechanism replacing the deprecated slash command API.
- No external dependencies required.

---

### REQ-CW-002: Validate Requirements Command

**Description**: A prompt command (`validate-requirements`) that accepts any file as input, analyzes requirements for completeness/clarity/feasibility/testability, and produces a validated requirements document.

**Acceptance Criteria**:
- [ ] Prompt file: `.github/prompts/validate-requirements.prompt.md`
- [ ] Accepts any requirement file as input context (via `#file` reference)
- [ ] Output includes: decomposed requirements, unique IDs, acceptance criteria, ambiguity flags, assumptions, and a validation status
- [ ] Validated output is saved under `artifacts/validated/` as `validated-requirements-<source-name>.md`

**Validation Notes**:
- Clear and feasible. The prompt instructs Copilot to perform structured analysis.
- Assumption: The developer attaches the requirement file as context when invoking.

---

### REQ-CW-003: Create Implementation Plan Command

**Description**: A prompt command (`create-implementation-plan`) that reads a design context or validated requirements document, reads the codebase for existing patterns, and produces a structured implementation plan with atomic, ordered, git-committable tasks. Plan follows the Implementation Plan Markdown Schema (REQ-CW-009).

**Acceptance Criteria**:
- [ ] Prompt file: `.github/prompts/create-implementation-plan.prompt.md`
- [ ] Accepts a design context description or validated requirements document as input
- [ ] If input is empty or too vague, stops and asks for clarification
- [ ] **Mandatory codebase reading** before writing anything (Gap 8): domain models, application services, repository interfaces, infrastructure, existing test patterns, related plans in `artifacts/pending/` or `artifacts/done/`
- [ ] Generates a plan file under `artifacts/pending/` with filename convention: `<yyyy-MM-dd-HH-mm>-<logical-name>.md` (Gap 12) where `<logical-name>` is 2–5 kebab-case words derived from the context
- [ ] Plan file follows the Implementation Plan Markdown Schema exactly (REQ-CW-009)
- [ ] Each task is a single, atomic, git-committable unit — one logical concern per task
- [ ] Tasks are ordered so each builds only on code introduced by previous tasks
- [ ] Acceptance criteria are concrete and testable — not vague
- [ ] Commit messages in the Progress Tracker are in imperative mood
- [ ] No tasks for hypothetical future requirements
- [ ] **Guiding principles** (Gap 7) are embedded in every plan: DDD, TDD, DRY, SOLID
- [ ] Creates missing output directories automatically (`artifacts/pending/`, `artifacts/done/`, `artifacts/context/planning/`, `artifacts/context/implemented/`)
- [ ] Saves a planning context snapshot to `artifacts/context/planning/<filename>` (Gap 10) containing: codebase observations, plan rationale, risks and open questions
- [ ] If context file already exists, appends numeric suffix (`-2.md`, `-3.md`) — never overwrites (Gap 11)
- [ ] Commits plan + planning context locally: `git commit -m "Add implementation plan: <logical-name>"`
- [ ] Does NOT push
- [ ] Reports to user: plan file path, task count, first task name, reminder to run `/implement-plan`

**Validation Notes**:
- Renamed from `create-plan` to `create-implementation-plan` to match detailed spec.
- Mandatory codebase reading prevents plans disconnected from reality.
- Planning context (separate from task execution context) preserves the reasoning behind the plan.

---

### REQ-CW-004: Implement Plan Command

**Description**: A prompt command (`implement-plan`) that reviews pending plans, executes tasks sequentially (implement → build/test → mark done → save context → commit), and loops until a stopping condition is met. Supports three execution modes. Fully resumable.

**Acceptance Criteria**:

#### Execution Modes (Gap 4)
- [ ] Prompt file: `.github/prompts/implement-plan.prompt.md`
- [ ] **`run-all`** (no argument): Execute all pending tasks across all pending plans until none remain
- [ ] **`count-limited`** (`N`): Execute only the next N tasks; spans plans if current one finishes before budget is exhausted; plan-completion commits do not consume budget
- [ ] **`range`** (`start-end`, e.g., `3-5`): Execute only tasks start through end in the active plan; cannot skip earlier pending tasks — if any task < start is still pending, stop and explain
- [ ] Any other argument format: stop immediately and explain accepted formats

#### Processing Steps
- [ ] **Step 1 — Audit**: List all `.md` files in `artifacts/pending/`. For each file where every task is `done`, move to `artifacts/done/` and commit: `Complete implementation plan: <filename without extension>` (Gap 14)
- [ ] **Step 2 — Sort**: Sort remaining plans by `yyyy-MM-dd-HH-mm` timestamp prefix ascending (oldest first) (Gap 12)
- [ ] **Step 3 — Find active task**: Take first (oldest) plan as active plan. Find first pending task per mode rules
- [ ] **Step 4 — Mandatory reads before implementing** (Gap 8): full active plan file, Task Details for the active task, every source file the task will create or modify, files from earlier tasks, related project files for patterns
- [ ] **Step 5 — Implement**: Follow the task's Spec and Tests precisely. Write tests first or alongside. Create/modify only files in the task scope
- [ ] **Step 6 — Build and test** (Gap 9): Run project build/test commands. Build failure → diagnose, fix, rebuild. Test failure → diagnose, fix, re-test. **Never commit broken code**
- [ ] **Step 7 — Mark done**: Change task status from `pending` to `done` in Progress Tracker. Tick all Definition of Done checkboxes
- [ ] **Step 8 — Save task context** (Gap 10): Write to `artifacts/context/implemented/<plan-name>-Task-<N>-of-<total>-<task-name>.md` containing: what was implemented, key decisions, issues encountered, state for next task. If path exists, append numeric suffix (Gap 11)
- [ ] **Step 9 — Commit**: `git add -A && git commit -m "Task-<N>-of-<total>-<task-name>: <exact commit message from Progress Tracker>"`
- [ ] **Step 10 — Loop**: Do NOT ask user for permission. Re-read Progress Tracker. If all done → move plan to `artifacts/done/`. Check stopping conditions. If not met → loop back

#### Stopping Conditions (Gap 5)
- [ ] **`run-all`**: Every plan moved to `artifacts/done/` → print "All implementation plans complete."
- [ ] **`count-limited`**: Requested task count reached (still move plan if final task completed it)
- [ ] **`range`**: Every task in [start, end] is done (still move plan if fully finished)
- [ ] **Build/test failure**: Cannot be resolved after reasonable diagnosis → leave task `pending`, explain
- [ ] **User action required**: Decision, credential, or external action needed → stop and ask

#### Non-Negotiable Constraints (Gap 6)
- [ ] **One task per commit** — never bundle multiple tasks
- [ ] Every commit must build and pass all tests
- [ ] Use **exact** commit messages from the Progress Tracker
- [ ] Do NOT push — only local commits
- [ ] Do NOT modify files outside the current task's scope
- [ ] Do NOT add abstractions, helpers, or "improvements" the task doesn't specify
- [ ] If interrupted mid-task, leave plan unchanged (task still `pending`) for clean retry
- [ ] In `count-limited` mode, only successful task implementations consume the budget
- [ ] In `range` mode, never silently skip earlier pending tasks

**Validation Notes**:
- Major upgrade from previous single-task invocation model. Now supports auto-looping with 3 modes.
- Commit format updated to `Task-<N>-of-<total>-<task-name>: <commit message>` for richer git history.
- Build/test gate ensures no broken commits in the repository.

---

### REQ-CW-005: Context Persistence (Session Saving)

**Description**: All significant Copilot interactions produce context snapshots. Context is split into two categories: **planning context** (created during plan creation) and **task execution context** (created after each task implementation). Both live under `artifacts/context/` in distinct subfolders.

**Acceptance Criteria**:

#### Directory Structure (Gap 10)
- [ ] `artifacts/context/planning/` — planning-phase context snapshots (created by `/create-implementation-plan`)
- [ ] `artifacts/context/implemented/` — per-task execution context snapshots (created by `/implement-plan`)

#### Planning Context Schema (Gap 10)
- [ ] File: `artifacts/context/planning/<plan-filename>.md`
- [ ] Required sections: (1) Codebase observations — relevant files, patterns, dependencies; (2) Plan rationale — why tasks are scoped/ordered this way, trade-offs; (3) Risks and open questions — ambiguities, blockers, assumptions

#### Task Execution Context Schema (Gap 10)
- [ ] File: `artifacts/context/implemented/<plan-name>-Task-<N>-of-<total>-<task-name>.md`
- [ ] Required sections: (1) What was implemented — files created/modified, approach, deviations; (2) Key decisions — non-obvious choices and rationale; (3) Issues encountered — problems diagnosed and resolved; (4) State for next task — new interfaces, naming patterns, context the next task needs

#### Collision Handling (Gap 11)
- [ ] If the target context file already exists, append a numeric suffix before the extension (`-2.md`, `-3.md`) until unique
- [ ] Never overwrite an existing context file

#### General
- [ ] Each prompt command includes instructions to save context after execution
- [ ] Context snapshot is included in the same git commit as the artifact it documents

**Validation Notes**:
- Split into planning vs execution context provides richer resumability.
- Collision handling ensures no data loss if a plan is re-run or tasks share names.

---

### REQ-CW-006: Context Resumption

**Description**: When starting a new Copilot session, the developer (or Copilot) can resume from the last saved context. A `resume` prompt command reads the latest context snapshot and provides a summary of state.

**Acceptance Criteria**:
- [ ] Prompt file: `.github/prompts/resume.prompt.md`
- [ ] Reads the most recent file across both `artifacts/context/planning/` and `artifacts/context/implemented/` (by datetime or filename sort)
- [ ] Outputs: summary of last action, current plan status, next steps
- [ ] Works across different developers (team handoff)

**Validation Notes**:
- Updated to scan both context subfolders.
- Team handoff is supported by committing context files to git.

---

### REQ-CW-007: Team Handoff Support

**Description**: The context and plan files must contain enough information for any team member's Copilot instance to understand the project state and continue work.

**Acceptance Criteria**:
- [ ] Context snapshots include: who performed the action (optional, from git), what was done, what remains, blockers/decisions
- [ ] Plan files are self-contained with full task descriptions (not just references)
- [ ] A Copilot instructions file (`.github/copilot-instructions.md`) provides project-level context pointing to docs and artifacts subfolders

**Validation Notes**:
- Feasible. This is a quality attribute of the context/plan files rather than a separate feature.
- The `.github/copilot-instructions.md` file is key — it gives any Copilot session the project map.

---

### REQ-CW-008: Copilot Instructions File

**Description**: A `.github/copilot-instructions.md` file that provides Copilot with project context, folder structure conventions, and references to documentation and workflow artifacts.

**Acceptance Criteria**:
- [ ] File: `.github/copilot-instructions.md`
- [ ] Contains: project description, folder structure, technology stack, workflow conventions (`artifacts/context/planning/`, `artifacts/context/implemented/`, `artifacts/pending/`, `artifacts/done/`, `artifacts/validated/` folders), how to use the custom prompts
- [ ] Automatically loaded by VS Code Copilot for all interactions in this repo

**Validation Notes**:
- This is a standard VS Code Copilot feature. The file is auto-loaded when present.
- Ties together all other requirements by giving Copilot ambient context.

---

### REQ-CW-009: Implementation Plan Markdown Schema (NEW — Gap 3)

**Description**: Every plan file in `artifacts/pending/` must follow a strict markdown schema so the implement-plan command can parse it reliably.

**Acceptance Criteria**:
- [ ] **YAML front matter** is required with fields: `created` (`yyyy-MM-dd HH:mm`), `context` (one-sentence summary), `status` (`pending` or `done`)
- [ ] **Design Context** section: 2–4 paragraphs explaining what is being built, why, and how it fits the existing architecture with references to specific existing files/classes
- [ ] **Guiding Principles** section (Gap 7): DDD (aggregates own invariants, repo interfaces in Domain, infra implements), TDD (every behavioural change has at least one test), DRY (extract shared logic only at 3+ occurrences), SOLID (single-responsibility, depend on interfaces)
- [ ] **Progress Tracker** table with columns: `#`, `Task`, `Status`, `Commit Message`. Status values: `pending` or `done`. Commit messages in imperative mood, used verbatim by implement-plan
- [ ] **Task Details** sections for each task, each containing: Scope (specific files/classes), Spec (bullet-point acceptance criteria), Tests to write (specific test cases), Definition of done (checkboxes)
- [ ] Task numbering in `#` column is sequential from 1 and matches `### Task N` headings
- [ ] Plan filename convention: `<yyyy-MM-dd-HH-mm>-<logical-name>.md` for chronological sorting (Gap 12)

**Validation Notes**:
- This schema is the contract between `create-implementation-plan` and `implement-plan`.
- The Progress Tracker is the single source of truth for task status.

---

### REQ-CW-010: Platform-Specific Conventions (NEW — Gap 13)

**Description**: Each plan must include a platform-specific conventions section that is customized per project. This section specifies build/test commands and code conventions so the implement-plan command knows how to verify its work.

**Acceptance Criteria**:
- [ ] The `create-implementation-plan` prompt detects the project's technology stack and populates conventions accordingly
- [ ] **Build and test commands**: exact shell commands to build and run tests (e.g., `dotnet build && dotnet test` for .NET, `npm run build && npm test` for Node.js)
- [ ] **Code conventions table**: indentation, encoding, line endings, domain rules, repository pattern, type annotations
- [ ] For this project specifically: .NET 10, C#, `dotnet build`, `dotnet test`, 4-space indentation, UTF-8, CRLF, nullable reference types enabled
- [ ] The implement-plan command uses these commands in its build/test gate (REQ-CW-004 Step 6)

**Validation Notes**:
- This section must be adapted per project. The prompt should auto-detect where possible.
- Ensures build/test gate is not generic but project-specific.

---

### REQ-CW-011: Git Conventions (NEW — Gap 14)

**Description**: All git operations performed by the workflow commands follow strict conventions.

**Acceptance Criteria**:
- [ ] Plan creation commit: `Add implementation plan: <logical-name>`
- [ ] Task completion commit: `Task-<N>-of-<total>-<task-name>: <exact commit message from Progress Tracker>`
- [ ] Plan completion (move to done): `Complete implementation plan: <filename without extension>`
- [ ] All commits are **local only** — never push
- [ ] One task = one commit — never bundle multiple tasks
- [ ] Every commit must build and pass all tests

**Validation Notes**:
- Consolidates all git conventions into one reference requirement.
- Commit message format is used verbatim by implement-plan — must not be altered.

---

## 2. Ambiguities Resolved

| # | Original Ambiguity | Resolution Applied |
|---|---|---|
| 1 | "slash commands (but don't use spec-kit)" — what mechanism? | Use VS Code Copilot custom prompt files (`.github/prompts/*.prompt.md`) |
| 2 | "implement pending plan's pending task iteratively while any task pending" — auto-loop or manual? | Auto-loop within a single invocation, with 3 execution modes: run-all, count-limited, range. |
| 3 | "All github copilot interactions should be saved" — literally every chat message? | Significant workflow actions only (validate, plan, implement). Not every conversational exchange. |
| 4 | "resumed next time from last context" — automatic or on-demand? | On-demand via `resume` prompt command. Developer invokes it at session start. |
| 5 | Folder names not specified | `artifacts/context/planning/`, `artifacts/context/implemented/`, `artifacts/validated/`, `artifacts/pending/`, `artifacts/done/` under repository root |
| 6 | Context file format — single or split? | Split: planning context (`artifacts/context/planning/`) vs task execution context (`artifacts/context/implemented/`) |
| 7 | Build/test enforcement — advisory or blocking? | Blocking. Never commit broken code (build/test gate). |

---

## 3. Assumptions

1. VS Code is the IDE with GitHub Copilot Chat extension installed.
2. Custom prompt files (`.github/prompts/*.prompt.md`) are the supported mechanism for reusable commands.
3. Developers commit context and plan files to git for team visibility.
4. The `artifacts/` folder and its subfolders may be gitignored if the team prefers — but default is tracked.
5. This workflow is additive to the existing project requirements in `docs/requirements.md`.
6. DDD, TDD, DRY, SOLID principles are non-negotiable for all generated plans and implementations.

---

## 4. Dependencies

| Dependency | Required For |
|---|---|
| VS Code + GitHub Copilot Chat extension | All requirements |
| Git repository (already initialized) | Context persistence, team handoff, commits |
| Existing `docs/requirements.md` | First usage of validate-requirements command |
| Project build toolchain (.NET 10 SDK) | Build/test gate in implement-plan |

---

## 5. Implementation Order (Recommended)

1. **REQ-CW-008** — Copilot instructions file (foundation for all other prompts)
2. **REQ-CW-001** — Prompts directory structure
3. **REQ-CW-009** — Implementation Plan Markdown Schema (must be defined before prompts reference it)
4. **REQ-CW-010** — Platform-specific conventions (must be defined before implement-plan uses them)
5. **REQ-CW-011** — Git conventions (must be defined before prompts embed them)
6. **REQ-CW-002** — Validate requirements prompt
7. **REQ-CW-003** — Create implementation plan prompt
8. **REQ-CW-005** — Context persistence (embedded in CW-003 and CW-004)
9. **REQ-CW-004** — Implement plan prompt
10. **REQ-CW-006** — Resume prompt
11. **REQ-CW-007** — Team handoff (verified via review, not a separate deliverable)

---

## 6. Validation Summary

| Req ID | Title | Status | Notes |
|---|---|---|---|
| REQ-CW-001 | Custom Prompt Files | VALID | Standard VS Code Copilot feature |
| REQ-CW-002 | Validate Requirements Command | VALID | Clear scope and output |
| REQ-CW-003 | Create Implementation Plan Command | VALID | Renamed; adds codebase reading, guiding principles, planning context |
| REQ-CW-004 | Implement Plan Command | VALID | 3 execution modes, auto-loop, build/test gate, 5 stopping conditions |
| REQ-CW-005 | Context Persistence | VALID | Split into planning + task execution; collision handling |
| REQ-CW-006 | Context Resumption | VALID | Reads latest from both context subfolders |
| REQ-CW-007 | Team Handoff Support | VALID | Quality attribute of CW-005/006 |
| REQ-CW-008 | Copilot Instructions File | VALID | Auto-loaded by VS Code Copilot |
| REQ-CW-009 | Implementation Plan Markdown Schema | VALID | New — YAML front matter, Progress Tracker, Task Details |
| REQ-CW-010 | Platform-Specific Conventions | VALID | New — build/test commands, code conventions per project |
| REQ-CW-011 | Git Conventions | VALID | New — commit message formats, local-only, one task per commit |

**Overall**: All 11 requirements are **VALID** and ready for task planning.

---

## 7. Gap Coverage Traceability

| Gap # | Description | Addressed In |
|---|---|---|
| 3 | Implementation Plan Markdown Schema | REQ-CW-009 |
| 4 | 3 execution modes (run-all, count-limited, range) | REQ-CW-004 Execution Modes |
| 5 | 5 stopping conditions | REQ-CW-004 Stopping Conditions |
| 6 | Non-negotiable constraints | REQ-CW-004 Constraints |
| 7 | DDD/TDD/DRY/SOLID guiding principles | REQ-CW-003, REQ-CW-009 |
| 8 | Mandatory codebase reading | REQ-CW-003 Step 2, REQ-CW-004 Step 4 |
| 9 | Build/test gate | REQ-CW-004 Step 6 |
| 10 | Saved-context split (planning vs execution) | REQ-CW-005 |
| 11 | Collision handling (numeric suffix) | REQ-CW-005 |
| 12 | Plan filename convention (chronological sort) | REQ-CW-003, REQ-CW-009 |
| 13 | Platform-specific conventions | REQ-CW-010 |
| 14 | Plan completion commit | REQ-CW-004 Step 1, REQ-CW-011 |
