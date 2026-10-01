"""ASCII snapshot for debugging (downsampled; keeps context small)."""
from __future__ import annotations

from .world import MOUNTAIN, WATER

ACTION_CHAR = {"eat": "E", "flee": "F", "follow": "L", "wander": "W",
               "rest": "R", "mate": "M", "attack": "A"}
LEGEND = ("animals by current action: E eat, F flee, L follow, W wander, R rest, M mate, "
          "A attack | P predator | . food | ~ water | ^ mountain")


def ascii_map(world, agents=(), max_w: int = 48, max_h: int = 24) -> str:
    sy = max(1, -(-world.h // max_h))
    sx = max(1, -(-world.w // max_w))
    grid = []
    for y in range(0, world.h, sy):
        row = []
        for x in range(0, world.w, sx):
            block_t = world.terrain[y:y + sy, x:x + sx]
            if (block_t == WATER).mean() > 0.5:
                ch = "~"
            elif (block_t == MOUNTAIN).mean() > 0.5:
                ch = "^"
            elif world.food[y:y + sy, x:x + sx].any():
                ch = "."
            else:
                ch = " "
            row.append(ch)
        grid.append(row)
    for a in agents:
        grid[a.y // sy][a.x // sx] = ACTION_CHAR.get(a.action, "?")
    for p in world.predators:
        grid[p.y // sy][p.x // sx] = "P"
    return "\n".join("".join(r) for r in grid)
