# 0011. Roads meet where they cross at the same height

- **Status:** Accepted
- **Date:** 2026-11-15

## Context

[0009](0009-3d-world.md) says two segments that cross in plan only meet if they are at the same height there, and otherwise one passes over the other. Until now, adding a road never looked at other roads, so two roads drawn across each other on the ground weren't connected. Nothing could travel from one to the other.

To keep the graph connected, adding a road has to find where it meets existing roads and put a node there. That needs a few rules: how close in height counts as "the same height", how close in plan counts as "the same place", what happens to the IDs of a segment that gets split, and what to do when two roads lie along each other.

## Options considered

- **Where the splitting happens**
  - *In the simulator's `road` command*: keeps the core simple, but every other caller (tests, a future OpenStreetMap import, Godot) would have to repeat it.
  - *In `RoadNetwork`*: one place, testable without the simulator.
- **IDs of a split segment**
  - *The first piece keeps the old ID*: fewer ID changes, but a segment's shape and end nodes can then change under the same ID.
  - *The old segment is removed and every piece gets a new ID*: a segment never changes once added, which keeps it simple for anything that caches segment data. The caller is told which segment was replaced by which pieces.
- **Two roads that lie along each other at the same height**
  - *Merge them into shared segments*: correct, but complex, and rarely what someone drawing roads wants.
  - *Refuse the new road*: simple, and nothing is changed.

## Decision

- `RoadNetwork.AddRoad` adds a road and joins it to every road it crosses or touches at the same height. `AddSegment` stays as a low-level operation that adds a segment exactly as given.
- Two roads are at **the same height** where they cross if their heights there differ by at most **1 m** (`MeetingHeightTolerance`). The node goes halfway between the two heights. Further apart than that, they pass over or under each other and neither is split.
- Two points are in **the same place** if they are within **1 cm** of each other in plan (`SnapDistance`). A road that ends this close to another road makes a T-junction with it, using the road's own end node.
- Where the meeting is at an existing node (a segment's end, or a junction), that node is used. Otherwise a new node is added there.
- A segment that is split is **removed, and every piece gets a new ID**. `AddRoad` returns the new road's segments, each split segment with its pieces, and the nodes it added.
- A road that would **lie along** an existing road at the same height for more than the snap distance is **refused**, and nothing changes.
- Pieces of a split segment are polylines. Interior points go to the piece they lie on.
- For now every new road is checked against every existing segment. A spatial index comes later, when maps get big enough to need it.

## Consequences

- Roads drawn across each other on the ground form a connected network, so agents can later route through junctions.
- Bridges and tunnels keep working with no special case: a road more than 1 m above or below another doesn't meet it.
- Segment IDs change when a road is split. Anything that holds a segment ID has to handle `RoadSplit`, or look it up again.
- Removing a segment doesn't merge the two pieces around a junction back together. A node with two segments stays in the graph.
- Not handled yet: a road that crosses itself, two different nodes at the same place (they aren't merged), and splitting spline segments when they arrive (0009).
- Adding a road costs time proportional to the number of segment pieces in the map, until the spatial index is added.
