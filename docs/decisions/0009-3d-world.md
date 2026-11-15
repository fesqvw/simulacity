# 0009. A 3D world, so roads can cross on bridges and in tunnels

- **Status:** Accepted
- **Date:** 2026-11-15
- **Supersedes:** [0007](0007-map-geometry.md)

## Context

[0007](0007-map-geometry.md) chose a road graph with polyline segments plus a land grid, on a flat 2D plane with no elevation. With no elevation, two roads that cross in plan always meet at an intersection. Bridges, tunnels, overpasses and later subway and train lines can't be modelled.

Changing a 2D model to 3D gets harder the more code depends on it, and there isn't much yet. The console renderer ([0004](0004-console-front-end.md)) can't show height properly, but the model shouldn't be limited by the first renderer.

Everything else in 0007 still holds. This record restates it with elevation added, so 0007 can be fully superseded.

## Options considered

- **Stay 2D, and mark segments as bridges or tunnels with a flag or layer number**: it's cheap, but there are no real heights. Slopes, clearances and vertical lengths can't be computed, and terrain can't decide what's a bridge.
- **3D coordinates everywhere, with a terrain height**: every position has an elevation, and the land has a ground height. Bridges and tunnels come from geometry: a road above the ground or below it. It costs a third coordinate and a terrain model.

## Decision

Use **3D coordinates everywhere, with a terrain height**. The rest of the model is as in 0007.

- **World coordinates** are continuous, in metres, as `double`s: x east, y north and **z up**. z is the elevation above a fixed datum (think sea level).
- **Roads** form a graph:
  - A **node** is an intersection or a dead end, with a 3D position.
  - A **segment** is an edge between two nodes. Its shape is a polyline: the two end node positions plus any number of 3D points in between.
  - Nodes and segments have stable IDs.
  - Polylines are only the starting shape. **Spline segments are planned for later**, behind the same segment shape type.
- **Land** is a grid of square cells over x and y. Each cell has a land type (ground or water to start with), and the cell size is a property of the map.
- **Terrain height** is a heightmap: one ground elevation at each cell corner, so a grid of `Width × Height` cells has `(Width + 1) × (Height + 1)` heights. The ground elevation anywhere in between is interpolated bilinearly within the cell. Water cells use the same heights for their bed or surface until water is modelled properly.
- **Two segments that cross in plan only meet if they are at the same height there.** Otherwise one passes over the other, and neither is split.
- **Buildings** will sit on land and connect to a road segment. Their shape is left for when buildings are designed.

## Consequences

- Bridges, tunnels and overpasses are ordinary segments whose points are above or below the terrain. Whether a stretch counts as a bridge or a tunnel can be worked out by comparing it with the terrain. Whether to also store it explicitly is left for when they are built.
- The road intersection work (splitting segments where roads cross, then a spatial index) has to compare heights, not only plan positions. A spatial index can still be 2D, with heights checked on the candidates it finds.
- Segment lengths are measured in 3D, so slopes make roads slightly longer.
- The console renderer draws a top-down view and ignores z. Where roads overlap in plan, it has to choose which one to draw.
- OpenStreetMap gives no elevations by default, only `bridge`, `tunnel` and `layer` tags. An import will need either elevation data or a rule that turns those tags into heights.
- Godot is 3D, so a Godot front end can show the model as it is.
