# 0015. The console renderer shows height as colour bands

- **Status:** Accepted
- **Date:** 2026-11-16

## Context

The console renderer draws the map from above with one character per place, so terrain height couldn't be seen. The starting map's hill ([0013](0013-map-scripts.md)) was invisible, except for the tunnel through it. The snapshot already carries the elevation of every cell corner ([0010](0010-snapshot-contents.md)), so the renderer has what it needs.

## Options considered

- **Characters for height** (digits, or `.` `:` `+` `*` for rising ground): work in any terminal, but they clash with the road symbols, and the land would no longer read as land.
- **Colour bands in the background**: the characters stay as they are (roads, nodes and water on top), and the height shows behind them. It needs a terminal with 256 colours, which current terminals have.
- **Bands at fixed heights** (every 10 m) or **bands across the map's own range**: fixed bands mean the same colour always means the same height, but a gentle map would be all one colour and a mountainous one would run out of colours. Bands across the map's range always use the whole scale, and the key under the map gives the heights.

## Decision

- The renderer colours the **background of each land character by the ground height under it**, interpolated from the corner elevations as in [0009](0009-3d-world.md).
- The map's range, from its lowest corner to its highest, is split into **six equal bands**: dark green, green, olive, gold, brown and light grey. A map with less than 1 m between lowest and highest is flat, and drawn in a single green.
- **Water stays blue**, whatever its height. Anything off the map has no colour.
- A **key under the map** shows each band's colour and the height it starts at.
- **`H` turns the colours off and on.** Like the other keys ([0014](0014-renderer-view-controls.md)), it only changes the view. With output redirected to a file, there are no colours.

## Consequences

- Hills and valleys can be seen, and roads over or through them can be compared with the ground.
- Colours are relative to each map, so the same colour can mean different heights on different maps. The key says which.
- Working out the ground height under a character is drawing, not simulation: it uses only the snapshot, and changes nothing.
- A terminal without 256 colours shows the escape codes as noise, or ignores them. `H` turns them off.
