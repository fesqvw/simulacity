# CLAUDE.md

This file gives Claude Code guidance for working in this repository.

## Project

Simulacity is a city simulation (transport, buildings, land, optionally utilities). See [`README.md`](README.md) for the vision and feature list.

It's written in **C# / .NET**, with a Godot front end planned for later ([decision 0003](docs/decisions/0003-csharp-dotnet.md)). Keep the simulation core free of any Godot or rendering dependency, so a Godot project can reference it.

The project is **starting over from scratch**. The earlier attempt (C# / .NET 10 console app with spline-based road geometry and a console renderer) still exists in git history before commit `9e0210f`. Look at it for reference when useful, but don't restore code from it unless asked.

## Documentation workflow

All project knowledge goes in `docs/`. Keep it current as part of the work, not afterwards.

- **`docs/TODO.md`**: the working backlog.
  - Check it at the start of a task to see where the task fits.
  - Tick items off (`- [x]`) when they're done, and add new items you discover.
  - Keep it short. Remove long-finished sections once they no longer help.
- **`docs/decisions/`**: decision records, one file per decision.
  - Write one whenever a choice is made that is hard to reverse or that someone would later ask "why?" about (tech stack, architecture, data model, simulation approach, conventions).
  - Name files `NNNN-short-kebab-title.md`, numbered in sequence, and start from [`docs/decisions/TEMPLATE.md`](docs/decisions/TEMPLATE.md).
  - Add each new record to the index in [`docs/decisions/README.md`](docs/decisions/README.md).
  - Don't rewrite an accepted decision. To change it, write a new one and mark the old one `Superseded by NNNN`.
  - If a decision is the user's to make, propose it as `Proposed` and ask, rather than marking it `Accepted` yourself.

## Conventions

- Don't commit build output, IDE files or local settings. Extend `.gitignore` when a new toolchain adds any.
- Keep commits focused, with messages that say what changed and why.
- When the build, test or run commands are set up, document them in the **Commands** section below and in the README.

## Commands

None yet. The .NET solution hasn't been set up (see `docs/TODO.md`).
