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
