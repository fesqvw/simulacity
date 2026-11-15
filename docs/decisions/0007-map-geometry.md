# 0007. Map and geometry: a road graph with polyline segments, and a land grid

- **Status:** Superseded by [0009](0009-3d-world.md)
- **Date:** 2026-11-15

## Context

The map has to hold land (ground, water), roads and buildings. Transport, pathfinding and a later import from OpenStreetMap all depend on how it is represented, so it is hard to change afterwards.

The previous attempt used spline-based road geometry and reworked the model several times. Its next unsolved problem was finding where roads intersect (bounding boxes, a spatial index, segment intersection). This choice should keep the first version simple without blocking realistic roads later.

The first renderer is a console app ([0004](0004-console-front-end.md)), so whatever model is chosen has to be drawable as characters on a grid.

## Options considered

- **Pure tile grid**: everything, roads included, is a cell on a 2D grid. It's the simplest to build and draw in a console, and intersections come for free. But road shapes are limited to the grid, OpenStreetMap data would have to be snapped onto it, and the network for pathfinding has to be pulled out of the tiles.
- **Road graph with spline segments**: realistic curves from the start. This is what the previous attempt kept reworking, and curves make intersection tests and lengths harder before they're needed.
- **Road graph with polyline segments, plus a land grid**: roads form a graph of intersections and segments, and each segment's shape is a polyline in continuous world coordinates. Land is a separate grid. The graph is what pathfinding and OpenStreetMap already use. Curves can be approximated with more points, and intersection tests stay straight-line tests.

## Decision

Use a **road graph with polyline segments, plus a land grid**.

- **World coordinates** are continuous, in metres, as `double`s on a 2D plane (x east, y north). There is no elevation for now.
- **Roads** form a graph:
  - A **node** is an intersection or a dead end, with a position.
  - A **segment** is an edge between two nodes. Its shape is a polyline: the two end node positions plus any number of points in between.
  - Nodes and segments have stable IDs, so other parts of the simulation and the renderer can refer to them.
  - Polylines are only the starting shape. **Spline segments are planned for later**, for smooth curves. Keep the segment's shape behind its own type so a spline can be added as a second kind of shape without changing the graph.
- **Land** is a grid of square cells laid over the same world coordinates, each with a land type (ground or water to start with). The cell size is a property of the map, not a constant.
- **Buildings** will sit on land and connect to a road segment. Their exact shape is left for when buildings are designed.

## Consequences

- Pathfinding and traffic work directly on the graph.
- OpenStreetMap ways map onto segments and their points without being snapped to a grid.
- Spline segments will be added later as a second segment shape behind the same graph, without changing the topology. Operations that need straight lines (intersection tests, rendering) can work on a polyline sampled from the spline.
- Adding a road that crosses an existing one means splitting both segments at a new node. That needs segment intersection tests and, once the map is large, a spatial index. Both are still to do, as the previous attempt warned.
- The console renderer has to rasterize polylines onto its character grid. The land grid can be drawn as it is.
- The model lives in `Simulacity.Core`. What the renderer receives is still decided separately (see [0005](0005-render-channel.md)).
