# 0010. Snapshots: the full drawable state, as JSON, after every change

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

[0005](0005-render-channel.md) left open what the simulator sends to renderers, and in what format. Until now, `SimulationSnapshot` only carried the tick count. The simulator can now build a map (land, terrain heights and roads, [0009](0009-3d-world.md)), and the renderer needs it to draw anything.

The constraints:

- **The renderer keeps no state of its own and runs no simulation logic** ([0004](0004-console-front-end.md)). It has to be able to draw from what it's sent, without working anything out from the simulation's rules.
- **The renderer can connect at any time**, and it reconnects when the simulator restarts ([0005](0005-render-channel.md)). Whatever it's sent first has to be enough to draw.
- `Simulacity.Contracts` **doesn't reference** `Simulacity.Core` ([0006](0006-solution-layout.md)), so snapshots can't contain Core types.
- **Godot comes later**, and it draws in 3D.
- Maps are small for now: the default is 64 × 32 cells. An OpenStreetMap import would be much larger.

## Options considered

- **Full snapshots or changes only.** Sending only what changed is smaller, but the renderer would have to keep the previous state and apply changes to it, and a renderer that connects late would need a full snapshot anyway. Full snapshots are bigger but keep the renderer stateless.
- **Shapes or drawable geometry.** The simulator could send each segment's shape (a polyline now, a spline later), or the polyline the shape produces. With shapes, every renderer would have to sample splines itself, which is geometry logic outside the simulator.
- **JSON or a binary format.** JSON is already used on the pipe, is easy to read when debugging, and is fast enough at this size. A binary format is smaller and faster but harder to debug, and isn't needed yet.

## Decision

- The simulator sends a **full snapshot** after every change, and the publisher keeps the latest one for renderers that connect later (as it already does).
- A snapshot holds everything needed to draw the world, in types of its own in `Simulacity.Contracts`:
  - **Tick**: the number of steps simulated.
  - **Land**: the grid's width, height and cell size; one land type per cell; and one ground elevation per cell corner. Cells and corners are listed row by row, starting at the south-west corner.
  - **Roads**: every node, with its ID and 3D position; and every segment, with its ID, its two node IDs, and its **drawable polyline** (3D points from the start node to the end node).
  - Nodes and segments are sorted by ID, so the same world always gives the same snapshot.
- Segments are sent as **polylines, never as shapes**. When spline segments arrive, the simulator samples them into polylines before sending.
- The format stays **length-prefixed UTF-8 JSON** on the pipe, as it was.

## Consequences

- Renderers stay stateless: any snapshot is enough to draw the whole world, including the first one a late renderer gets.
- Adding a new kind of segment shape doesn't change the snapshot or the renderers.
- Snapshots carry heights even though the console renderer draws top-down and ignores them, so Godot can use the same snapshot.
- The simulator does a little extra work, copying the world into a snapshot on each change. That's fine for commands typed by hand. Once the simulation runs continuously, or maps get large, snapshots should go out at a limited rate, and may need to be split into a map that rarely changes and things that move every tick, or switched to a binary format. That would be a new decision.
- Snapshot types use plain IDs (`int`) and their own position type, so renderers never need `Simulacity.Core`.
