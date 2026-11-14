# 0004. Console front end: a simulator that takes commands, and a separate renderer

- **Status:** Accepted
- **Date:** 2026-11-14
- **Amended:** 2026-11-14, to settle which program runs the simulation (formerly an open question)

## Context

The simulation core needs a first front end so it can be driven and watched while it's built. Godot is the long-term front end ([0003](0003-csharp-dotnet.md)), but setting it up early would slow down work on the core.

A single console that both reads commands and redraws the map gets messy: the redraws keep overwriting what's being typed.

## Options considered

- **Godot now**: the eventual target, but heavier to set up and iterate on.
- **One console**: the simplest, but input and rendering fight over the same screen.
- **Two consoles, one for commands and one for rendering**: each screen has a single job.

## Decision

Start with a console front end split across two programs, each with its own console:

- **The simulator** reads commands from its console (load a map, step or run the simulation, inspect things). It owns and runs the simulation and is the only program that changes its state.
- **The renderer** only draws. It receives the simulation state from the simulator and displays it. It doesn't run the simulation, keep its own copy of the state, or accept commands.

## Consequences

- A .NET process has only one console, so the simulator and the renderer are two processes, and the simulator has to send the state to the renderer. How it does that is still open (see below).
- Data goes in one direction only, from the simulator to the renderer. The renderer can be closed or restarted without affecting the simulation.
- Godot can later take the renderer's place, or sit beside it as another renderer.
- Rendering stays out of the simulation core, as [0003](0003-csharp-dotnet.md) requires.

## Open questions

These still need deciding, each in its own record:

- **How the simulator sends state to the renderer.** Decided in [0005](0005-render-channel.md): through an interface, using named pipes first.
- **How the renderer is started.** Decided in [0005](0005-render-channel.md): by hand, in a second terminal.
- **What gets sent.** Still open in [0005](0005-render-channel.md). To be decided once work on the simulation model has started.
