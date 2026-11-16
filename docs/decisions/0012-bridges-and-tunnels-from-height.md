# 0012. Water is crossed on bridges or in tunnels, worked out from height

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

Two questions were open:

- What happens when a road runs over water at ground level? Until now it was allowed, and drawn as an ordinary road.
- Are bridges and tunnels only worked out from the road's height compared with the terrain, or also stored on the segment ([0009](0009-3d-world.md))?

Also, the console renderer was the one working out which stretches of road were bridges and tunnels, with its own 1 m tolerance. That's a rule of the simulation, and [0004](0004-console-front-end.md) and [0010](0010-snapshot-contents.md) say renderers run no simulation logic.

## Options considered

- **A road at ground level over water**
  - *Allow it as a ford*: no rule to enforce, but rivers stop being an obstacle.
  - *Refuse it*: crossing water takes a bridge or a tunnel, which matters for building and later for cost.
- **How a stretch becomes a bridge or a tunnel**
  - *Store a flag or a type on the segment*: explicit, but it can disagree with the heights, and the player has to set it.
  - *Work it out from the road's height against the terrain*: always matches the geometry, and needs no extra command. The user asked for this: "if the road is high enough or low enough, the bridge or tunnel should be established automatically".

## Decision

The user decided that crossing water requires a bridge or a tunnel, and that these are established automatically from height.

- A point of road is **on the ground** if it's within **1 m** of the terrain (`RoadProfile.GradeTolerance`), a **bridge** if it's higher than that, and a **tunnel** if it's lower. Off the map, where there's no terrain, it counts as on the ground. Water cells use the terrain height for their surface and bed, as in 0009.
- Nothing about bridges or tunnels is stored. `RoadProfile.Stretches` splits a segment into stretches at one level each, cutting where the level changes part-way along a straight piece.
- **A road can't run at ground level over water.** `World.AddRoad` refuses it, and nothing changes. A road can run along the water's edge, or end at it.
- **Land and terrain changes keep the rule.** `World.SetLand` and `World.SetCornerElevation` are refused if they would leave a road at ground level over water. Other changes can turn a road into a bridge or a tunnel, or back, with no further action: lowering the ground under a road makes it a bridge.
- Rules that involve both land and roads live on `World`. Changing `World.Land` or `World.Roads` directly skips them. That's kept for tests and low-level code.
- **The simulator sends the stretches.** This adds to what 0010 lists: each segment in a snapshot keeps its full polyline and also has its stretches, each with a level (ground, bridge or tunnel) and its points. Renderers draw the levels they're given, and no longer compare roads with the terrain themselves.

## Consequences

- Rivers and lakes are obstacles. The player has to raise a road onto a bridge or lower it into a tunnel to cross them, by giving heights.
- There is no minimum clearance yet beyond the 1 m tolerance, and no maximum slope. A bridge 1.1 m over the water is allowed. Both can come later, for example with building costs.
- The level is checked by sampling along each piece: at every cell edge, and at least every quarter of a cell. A very short dip to ground level between samples could be missed.
- Land and height changes check every road. That's fine at the current map size, and can use the spatial index from 0011 later.
- Godot can use the same stretches to choose bridge and tunnel meshes.
- An OpenStreetMap import will have to turn `bridge` and `tunnel` tags into heights that produce the right levels (0009).
