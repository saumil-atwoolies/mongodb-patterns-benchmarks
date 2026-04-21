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
