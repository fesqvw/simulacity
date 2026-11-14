# 0006. Solution layout and target framework

- **Status:** Accepted
- **Date:** 2026-11-14

## Context

Decisions [0003](0003-csharp-dotnet.md), [0004](0004-console-front-end.md) and [0005](0005-render-channel.md) call for:

- a simulation core with no Godot, renderer or transport dependency
- a shared interface for sending state to renderers, also free of Godot and of any transport
- a named-pipe implementation of that interface
- two console apps: the simulator, which takes commands, and the renderer, which only draws
- a target framework that a Godot project can reference

## Decision

One solution, `Simulacity.slnx`, with these projects:

| Project | Kind | Depends on | Role |
| --- | --- | --- | --- |
| `src/Simulacity.Core` | Class library | nothing | The simulation |
| `src/Simulacity.Contracts` | Class library | nothing | `IStatePublisher`, `IStateReceiver` and the data they carry |
| `src/Simulacity.Transport.NamedPipes` | Class library | Contracts | Named-pipe implementations of the Contracts interfaces |
| `src/Simulacity.Simulator` | Console app | Core, Contracts, Transport.NamedPipes | Reads commands, runs the simulation, publishes state |
| `src/Simulacity.Renderer.Console` | Console app | Contracts, Transport.NamedPipes | Draws the state it receives |
| `tests/Simulacity.Tests` | xUnit tests | all of the above except the renderer | Tests |

`Simulacity.Core` and `Simulacity.Contracts` don't reference each other. The simulator maps the simulation's state onto the snapshot it publishes. The renderer never references `Simulacity.Core`, so it can't run simulation logic even by accident.

Shared build settings are in `Directory.Build.props`:

- **Target framework:** `net10.0`, the current long-term support release of .NET.
- **Nullable reference types:** on.
- **Implicit usings:** on.
- **Warnings:** treated as errors.

`global.json` pins the .NET 10 SDK.

## Consequences

- A Godot project can later reference `Simulacity.Core` and `Simulacity.Contracts` without pulling in the console apps or the pipes. It can either implement `IStatePublisher` itself (in-process) or use `Simulacity.Transport.NamedPipes` to receive state from a separately running simulator.
- The Godot project will have to target `net10.0` or later to reference these libraries. Check that the Godot version in use supports this before integrating. If it doesn't, the libraries can be set to an older target framework in `Directory.Build.props`.
- The renderer interface and the snapshot format are provisional. `SimulationSnapshot` only carries the tick count, and the pipe sends length-prefixed JSON. Both will change once what gets sent is decided (see [0005](0005-render-channel.md)).
