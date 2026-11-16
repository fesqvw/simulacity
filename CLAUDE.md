# CLAUDE.md

This file gives Claude Code guidance for working in this repository.

## Project

Simulacity is a city simulation (transport, buildings, land, optionally utilities). See [`README.md`](README.md) for the vision and feature list.

It's written in **C# / .NET**, with a Godot front end planned for later ([decision 0003](docs/decisions/0003-csharp-dotnet.md)). Keep the simulation core free of any Godot or rendering dependency, so a Godot project can reference it. The first front end is two console apps ([decision 0014](docs/decisions/0014-renderer-view-controls.md), which supersedes 0004):

- **The simulator** reads commands, owns and runs the simulation, and is the only thing that changes its state.
- **The renderer** only draws the state the simulator sends it. It never runs simulation logic or changes the simulation. Its only state of its own is its view (which part of the map is on screen, and the scale), moved with WASD/arrows, `+`/`-` and `F`, and whether height colours are on (`H`, [decision 0015](docs/decisions/0015-height-as-colour.md)).

The simulator sends state through an interface, never by calling a transport directly ([decision 0005](docs/decisions/0005-render-channel.md)). The first implementation uses named pipes, and Godot will use the same interface later. The renderer is started by hand, so either program can start first: the simulator must run fine with no renderer connected, and the renderer must wait for the simulator if it isn't running yet. Keep the interface in a shared library that has no Godot or transport dependency.

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
- Commit as the project owner, not as Claude: set `git config user.name "Abel Vexina Wilkinson"` and `git config user.email "fesqvw@hotmail.com"` in the repository before the first commit of a session.
- Don't add attribution to commit messages or pull request descriptions: no `Co-Authored-By` line, no `Claude-Session` link, no "Generated with Claude Code" footer and no session link.
- When the build, test or run commands are set up, document them in the **Commands** section below and in the README.

## Project layout

See [decision 0006](docs/decisions/0006-solution-layout.md) for the full rationale.

- `src/Simulacity.Core`: the simulation. It references nothing else in the solution. Never add a Godot, renderer or transport dependency to it.
- `src/Simulacity.Contracts`: `IStatePublisher`, `IStateReceiver` and `SimulationSnapshot`. Also free of Godot and transport dependencies.
- `src/Simulacity.Transport.NamedPipes`: the named-pipe implementations of the Contracts interfaces.
- `src/Simulacity.Simulator`: the simulator console app. `CommandInterpreter` holds the commands.
- `src/Simulacity.Renderer.Console`: the renderer console app. Must never reference `Simulacity.Core`. `MapView` draws a snapshot as text, through a `MapViewport`, and `HeightShading` colours it by height.
- `tests/Simulacity.Tests`: xUnit tests.
- `maps`: map scripts, copied next to the simulator. `maps/starter.txt` is the starting map the simulator loads ([decision 0013](docs/decisions/0013-map-scripts.md)). When you add a feature, add a section for it there; `StarterMapTests` checks every command in it succeeds. Name road ends by position, never by node ID.

Change the world through `World.AddRoad`, `World.SetLand` and `World.SetCornerElevation`. They keep the rules: roads join where they cross at the same height ([decision 0011](docs/decisions/0011-roads-meet-at-crossings.md)), and water is only crossed on bridges or in tunnels, which are worked out from height and never stored ([decision 0012](docs/decisions/0012-bridges-and-tunnels-from-height.md)). `RoadNetwork.AddSegment` and the `LandGrid` setters skip those rules.

Shared build settings (`net10.0`, nullable, warnings as errors) are in `Directory.Build.props`. Don't repeat them in project files.

A `SimulationSnapshot` is the full drawable state of the world (land, terrain heights, and roads as polylines split into ground, bridge and tunnel stretches), sent as length-prefixed JSON after every change ([decision 0010](docs/decisions/0010-snapshot-contents.md)). The simulator builds it in `SnapshotBuilder`. Renderers must be able to draw from any single snapshot.

## Commands

```sh
dotnet build                                          # build the solution
dotnet test                                           # run all tests
dotnet run --project src/Simulacity.Simulator         # run the simulator with the starting map (reads commands from stdin)
dotnet run --project src/Simulacity.Simulator -- --empty   # ...or with an empty map (or --map <file>)
dotnet run --project src/Simulacity.Renderer.Console  # run the renderer (in another terminal; WASD/arrows, +/-, F, H)
```

Build and test before committing. The build treats warnings as errors.
