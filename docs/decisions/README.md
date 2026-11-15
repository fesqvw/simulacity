# Decision records

Each file records one decision: the context, what was decided, and the consequences.

## How to add one

1. Copy [`TEMPLATE.md`](TEMPLATE.md) to `NNNN-short-kebab-title.md`, using the next number.
2. Fill it in, starting with status `Proposed`.
3. Set it to `Accepted` once agreed.
4. Add it to the index below.

Don't edit an accepted decision to change it. Write a new record and set the old one to `Superseded by NNNN`.

## Index

| # | Decision | Status |
| --- | --- | --- |
| [0001](0001-record-decisions.md) | Record decisions in `docs/decisions/` | Accepted |
| [0002](0002-start-over.md) | Start the project over from scratch | Accepted |
| [0003](0003-csharp-dotnet.md) | Use C# and .NET | Accepted |
| [0004](0004-console-front-end.md) | Console front end: a simulator that takes commands, and a separate renderer | Accepted |
| [0005](0005-render-channel.md) | Send state to renderers through an interface, using named pipes first | Accepted |
| [0006](0006-solution-layout.md) | Solution layout and target framework | Accepted |
| [0007](0007-map-geometry.md) | Map and geometry: a road graph with polyline segments, and a land grid | Superseded by 0009 |
| [0008](0008-simulation-approach.md) | Simulation approach: a shared world, with agent-based simulation first | Accepted |
| [0009](0009-3d-world.md) | A 3D world, so roads can cross on bridges and in tunnels | Accepted |
| [0010](0010-snapshot-contents.md) | Snapshots: the full drawable state, as JSON, after every change | Accepted |
