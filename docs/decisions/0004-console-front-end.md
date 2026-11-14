# 0004. Console front end, with separate command and render consoles

- **Status:** Accepted
- **Date:** 2026-11-14

## Context

The simulation core needs a first front end so it can be driven and watched while it's built. Godot is the long-term front end ([0003](0003-csharp-dotnet.md)), but setting it up early would slow down work on the core.

A single console that both reads commands and redraws the map gets messy: the redraws keep overwriting what's being typed.

## Options considered

- **Godot now**: the eventual target, but heavier to set up and iterate on.
- **One console**: the simplest, but input and rendering fight over the same screen.
- **Two consoles, one for commands and one for rendering**: each screen has a single job.

## Decision

Start with a console front end split across two consoles:

- a **command console** that reads and runs commands (load a map, step or run the simulation, inspect things)
- a **render console** that only draws the current state of the simulation

## Consequences

- A .NET process has only one console, so the two consoles are two processes and need to talk to each other. How they do that is still open (see below).
- Separating "control" from "view" is the shape Godot will take over later: Godot becomes another view, and can also be a control.
- Rendering stays out of the simulation core, as [0003](0003-csharp-dotnet.md) requires.

## Open questions

These still need deciding, each in its own record:

- **Which process runs the simulation.** Suggestion: the command console hosts the simulation, and the render console is a thin viewer that only receives state to draw.
- **How the processes communicate.** Suggestion: .NET named pipes (`System.IO.Pipes`), which work on Windows, Linux and macOS. The alternative is local TCP.
- **How the render console is started.** Suggestion: start it by hand in a second terminal. Opening a new terminal window automatically depends on the platform.
- **What gets sent between them.** For example, full snapshots of the state or only the changes.
