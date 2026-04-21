# Project: MongoDB Patterns & Benchmarks

## Overview

A .NET 10 C# console application that benchmarks various MongoDB usage patterns — specifically outbox pattern variants for DDD aggregates — with and without change streams, under configurable concurrency and load.

## Technology Stack

- .NET 10, C#
- MongoDB official driver, BenchmarkDotNet
- Docker Desktop, docker-compose
- DDD, SOLID, DRY, TDD

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
- **Build gate**: every commit must pass `dotnet build && dotnet test`
- **Principles**: DDD, TDD, DRY, SOLID are non-negotiable in all plans and implementations
