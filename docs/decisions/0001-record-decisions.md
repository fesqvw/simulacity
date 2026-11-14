# 0001. Record decisions in `docs/decisions/`

- **Status:** Accepted
- **Date:** 2026-11-14

## Context

The project is restarting, and much of the work will be done with Claude Code. The reasons behind design choices need to outlast any single session or conversation, so that later work, by a person or by Claude, builds on them instead of revisiting them.

## Decision

Keep one short Markdown decision record per significant decision in `docs/decisions/`, using [`TEMPLATE.md`](TEMPLATE.md). Track work in `docs/TODO.md`. `CLAUDE.md` tells Claude Code to read and maintain both.

## Consequences

- Decisions and their reasons sit in the repository next to the code.
- Writing and updating the records is a small, ongoing cost that's part of each change.
