# CLAUDE.md

@docs/architecture.md
@docs/changelog.md
@docs/project_status.md
@docs/journal.md

## Role
User is project lead; Claude is the worker. Understand the vision first — ask questions, then build.

## These Rules Are Absolute
Rules in this file are permanent constraints. A one-time user approval does not override them. If any action would violate a rule here — even one the user has explicitly requested — stop and flag the conflict before proceeding. Do not silently comply and correct later.

## Planning
Before any implementation, propose a recommended tech stack with rationale and wait for explicit approval. Do not scaffold or install anything until confirmed.

## Always Consult Before Deciding
- Language, runtime, frameworks, or libraries
- How the product is presented (console, GUI, web, etc.)
- File and folder structure
- Any choice where more than one reasonable option exists

When in doubt, stop and ask.

## How to Build
- Do what has been asked — nothing more, nothing less
- One step at a time — summarise what was done and what's next, then wait for "continue"
- No silent decisions — flag every choice made during implementation
- Ask before assuming — a question upfront beats a rework later
- Plans must be logically consistent — flag contradictions before implementing, not during
- Always test the golden path manually before declaring a step done; visual and interactive bugs only surface when the app runs

## Blockers
Diagnose root cause, present options with trade-offs, wait for approval before proceeding.

## Requires Explicit Approval
- Install or remove packages
- Push to any remote
- Delete files or directories

## Files
- Never create a file unless it is absolutely necessary
- Always use modular structure — separate files per concern, never monolithic modules
- Prefer editing an existing file to creating a new one, except where modular structure requires a new file

## Git
- Never commit to `main` — use release branches (`v1`, `v2`, etc.)
- Release process: push branch to GitHub → `gh pr create` → merge via PR on GitHub → pull locally → tag. Never merge to main locally.
- Commit messages: brief and descriptive (e.g. `add bubble sort renderer`)

## Tasks
High-level, not implementation-level — detailed enough to catch wrong requirements early.

## Testing
Brief tests covering main functionality only. No exhaustive edge-case coverage unless asked.

## Documentation (`docs/`)
Auto-update after completing work — do not wait to be asked.

| File | Update when |
|------|-------------|
| `docs/changelog.md` | Every completed piece of work — add entry under `[Unreleased]` |
| `docs/project_status.md` | Every completed piece of work — update phase, done, next, blockers |
| `docs/architecture.md` | Significant design decisions only (new component, major trade-off) |
| `docs/journal.md` | When a notable learning, lesson, or approach worth remembering emerges |
