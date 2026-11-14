# 0005. Send state to renderers through an interface, using named pipes first

- **Status:** Accepted
- **Date:** 2026-11-14
- **Amended:** 2026-11-14, to settle how the renderer is started (formerly an open question)

## Context

The simulator and the renderer are separate processes, and data flows only from the simulator to the renderer ([0004](0004-console-front-end.md)). The simulator therefore needs a way to send state to the renderer. Godot will later need the simulation state too ([0003](0003-csharp-dotnet.md)), possibly over a different transport or within the same process.

## Options considered

- **Named pipes** (`System.IO.Pipes`): built into .NET and work on Windows, Linux and macOS. On Linux and macOS they use Unix domain sockets.
- **Local TCP**: also works everywhere, but it means managing ports and, in theory, exposes the connection to the network.
- **Calling a transport directly from the simulator**: simpler at first, but then adding Godot means changing the simulator.

## Decision

- The simulator sends state to renderers through an **interface**. It doesn't know which transport is behind it.
- The first implementation of that interface uses **named pipes**, connecting the simulator to the console renderer.
- Godot will use the **same interface** later. It can then connect over a named pipe like the console renderer, or receive state in-process, through an implementation that needs no pipe.
- The console renderer is **started by hand**, in a second terminal. The simulator doesn't launch it.

## Consequences

- The interface and the data it carries belong in a shared library that the simulator, the console renderer and Godot can all reference. That library has no dependency on Godot or on a particular transport.
- The named-pipe code is kept separate from the simulation logic, so it can be replaced or run alongside another implementation without touching the simulation.
- The interface follows the one-way flow from [0004](0004-console-front-end.md): it sends state out and never receives commands.
- Data sent through a pipe has to be serialized, so the state needs a format that can be sent over the wire. The format is chosen together with what gets sent (still open).
- Because the renderer is started by hand, the two programs can start in either order. The simulator has to run normally with no renderer connected. The renderer has to wait for the simulator if it isn't running yet, and has to cope with the simulator stopping.

## Still open

- **What gets sent, and in what format.** For example, full snapshots or only the changes, as JSON or a binary format. To be decided once work on the simulation model has started.
- **The interface's exact shape and name.** Settled when the code is written.
