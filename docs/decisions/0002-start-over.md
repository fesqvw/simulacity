# 0002. Start the project over from scratch

- **Status:** Accepted
- **Date:** 2026-11-13

## Context

The first attempt was a C# / .NET 10 console application. It had:

- a spline-based geometry model and an adaptive spline rasterizer for console rendering
- stub types for roads, streets, water, parcels, and road and pedestrian graphs
- a basic simulator and simulation state

The geometry model was revised several times and didn't feel right, and build output (`bin/`, `obj/`) had been committed.

## Decision

Clear the repository and start again. The tech stack, simulation approach and geometry model are all open again and will each get their own decision record.

## Consequences

- The old code is only in git history, before commit `9e0210f`, for reference.
- Build output is ignored from the start (`.gitignore`).
- The foundational choices are listed in [`../TODO.md`](../TODO.md).
