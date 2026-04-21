# Validated Requirements — GitHub Copilot Workflow Setup

**Source**: `docs/github-copilot-workflow-requirements.md`
**Validated**: 2026-04-21
**Status**: VALIDATED — Ready for task planning

---

## 1. Requirement Decomposition

The raw requirement has been decomposed into discrete, testable requirements grouped by capability.

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

### REQ-CW-003: Create Task Plan Command

**Description**: A prompt command (`create-plan`) that reads a validated requirements document and generates a trackable implementation plan with discrete tasks, saved under a `pending/` folder.

**Acceptance Criteria**:
- [ ] Prompt file: `.github/prompts/create-plan.prompt.md`
- [ ] Reads validated requirements document as input context
- [ ] Generates a plan file under `artifacts/pending/` directory (e.g., `artifacts/pending/plan-<name>-<datetime>.md`)
- [ ] Each task in the plan has: unique ID, title, description, linked requirement ID(s), status (`pending`/`in-progress`/`done`), and acceptance criteria
- [ ] Plan file uses a parseable format (markdown with consistent structure)

**Validation Notes**:
- Clear and feasible. The `pending/` folder convention is straightforward.
- Assumption: One plan per validated requirements file. Multiple plans can coexist.

---

### REQ-CW-004: Implement Plan Command

**Description**: A prompt command (`implement-plan`) that reads a pending plan, identifies the next pending task, and implements it. Designed for iterative invocation — run repeatedly until all tasks are done.

**Acceptance Criteria**:
- [ ] Prompt file: `.github/prompts/implement-plan.prompt.md`
- [ ] Reads a plan file from `artifacts/pending/` as input context
- [ ] Identifies the first task with status `pending` and implements it
- [ ] After implementation, updates the task status to `done` in the plan file
- [ ] Saves a context snapshot after each task (see REQ-CW-005)
- [ ] After marking a task `done`, creates a git commit with message format: `task(<TASK-ID>): <task title>` (e.g., `task(TASK-003): Add docker-compose for MongoDB replica set`)
- [ ] The commit includes all files created or modified during the task implementation plus the updated plan file and context snapshot
- [ ] When all tasks in a plan are `done`, moves the plan file from `artifacts/pending/` to `artifacts/done/` folder and commits with message: `plan(<plan-name>): all tasks complete`
- [ ] Provides clear output of what was implemented, what was committed, and what remains

**Validation Notes**:
- Feasible with iterative developer-driven invocation.
- Clarification applied: "while any task pending" means the developer re-invokes the command; Copilot does not auto-loop. Each invocation handles one task.
- Commit is performed automatically after each task — developer reviews the diff before re-invoking for the next task.

---

### REQ-CW-005: Context Persistence (Session Saving)

**Description**: All significant Copilot interactions (requirement validations, plan creation, task implementations) produce a context snapshot saved under a `context/` folder with datetime in the filename.

**Acceptance Criteria**:
- [ ] Context directory: `artifacts/context/` at repository root
- [ ] Each snapshot file named: `artifacts/context/<action>-<YYYY-MM-DD-HHmm>.md`
- [ ] Snapshot contains: action performed, files modified, decisions made, current plan status, what to do next
- [ ] Each prompt command includes instructions to save context after execution
- [ ] Context snapshot is included in the same git commit as the task it documents (see REQ-CW-004)

**Validation Notes**:
- Feasible. Each prompt file will include a "save context" instruction block.
- Context files are committed to git as part of the task commit — no separate commit needed.

---

### REQ-CW-006: Context Resumption

**Description**: When starting a new Copilot session, the developer (or Copilot) can resume from the last saved context. A `resume` prompt command reads the latest context snapshot and provides a summary of state.

**Acceptance Criteria**:
- [ ] Prompt file: `.github/prompts/resume.prompt.md`
- [ ] Reads the most recent file in `artifacts/context/` folder (by datetime in filename)
- [ ] Outputs: summary of last action, current plan status, next steps
- [ ] Works across different developers (team handoff)

**Validation Notes**:
- Feasible. The prompt instructs Copilot to scan `artifacts/context/` and read the latest file.
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
- [ ] Contains: project description, folder structure, technology stack, workflow conventions (`artifacts/context`, `artifacts/pending`, `artifacts/done`, `artifacts/validated` folders), how to use the custom prompts
- [ ] Automatically loaded by VS Code Copilot for all interactions in this repo

**Validation Notes**:
- This is a standard VS Code Copilot feature. The file is auto-loaded when present.
- Ties together all other requirements by giving Copilot ambient context.

---

## 2. Ambiguities Resolved

| # | Original Ambiguity | Resolution Applied |
|---|---|---|
| 1 | "slash commands (but don't use spec-kit)" — what mechanism? | Use VS Code Copilot custom prompt files (`.github/prompts/*.prompt.md`) |
| 2 | "implement pending plan's pending task iteratively while any task pending" — auto-loop or manual? | Manual iterative invocation. Developer runs `implement-plan` once per task, reviews, commits, repeats. |
| 3 | "All github copilot interactions should be saved" — literally every chat message? | Significant workflow actions only (validate, plan, implement). Not every conversational exchange. |
| 4 | "resumed next time from last context" — automatic or on-demand? | On-demand via `resume` prompt command. Developer invokes it at session start. |
| 5 | Folder names not specified | `artifacts/context/`, `artifacts/validated/`, `artifacts/pending/`, `artifacts/done/` under repository root |

---

## 3. Assumptions

1. VS Code is the IDE with GitHub Copilot Chat extension installed.
2. Custom prompt files (`.github/prompts/*.prompt.md`) are the supported mechanism for reusable commands.
3. Developers commit context and plan files to git for team visibility.
4. The `artifacts/` folder and its subfolders may be gitignored if the team prefers — but default is tracked.
5. This workflow is additive to the existing project requirements in `docs/requirements.md`.

---

## 4. Dependencies

| Dependency | Required For |
|---|---|
| VS Code + GitHub Copilot Chat extension | All requirements |
| Git repository (already initialized) | Context persistence, team handoff |
| Existing `docs/requirements.md` | First usage of validate-requirements command |

---

## 5. Implementation Order (Recommended)

1. **REQ-CW-008** — Copilot instructions file (foundation for all other prompts)
2. **REQ-CW-001** — Prompts directory structure
3. **REQ-CW-002** — Validate requirements prompt
4. **REQ-CW-003** — Create plan prompt
5. **REQ-CW-005** — Context persistence (embedded in all prompts)
6. **REQ-CW-004** — Implement plan prompt
7. **REQ-CW-006** — Resume prompt
8. **REQ-CW-007** — Team handoff (verified via review, not a separate deliverable)

---

## 6. Validation Summary

| Req ID | Title | Status | Notes |
|---|---|---|---|
| REQ-CW-001 | Custom Prompt Files | VALID | Standard VS Code Copilot feature |
| REQ-CW-002 | Validate Requirements Command | VALID | Clear scope and output |
| REQ-CW-003 | Create Task Plan Command | VALID | Clear scope and output |
| REQ-CW-004 | Implement Plan Command | VALID | Iterative manual invocation |
| REQ-CW-005 | Context Persistence | VALID | Datetime-named snapshots |
| REQ-CW-006 | Context Resumption | VALID | Reads latest context file |
| REQ-CW-007 | Team Handoff Support | VALID | Quality attribute of CW-005/006 |
| REQ-CW-008 | Copilot Instructions File | VALID | Auto-loaded by VS Code Copilot |

**Overall**: All 8 requirements are **VALID** and ready for task planning.
