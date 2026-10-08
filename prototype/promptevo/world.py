"""Headless grid world: terrain (water/mountain), food and carcasses. Plan §A4.

Predators were scripted here until 2026-10-07; they are now genetic animals (sim.py, actions.py).
A predator's kill leaves a carcass (since 2026-10-07) that other predators can eat from.

Two options against boom-bust crashes (2026-10-08, off by default; experiments/crash_sweep.py):
- world.cover_fraction: patches of cover (thickets) where predators can't see or kill a prey
  animal; fleeing prey run into cover within world.cover_seek cells.
- world.patches: ridges (mountain, 1 cell thick) split the world into patches x patches areas,
  each joined to its neighbours by one gap of world.wall_gap cells."""
from __future__ import annotations

from collections import deque
from dataclasses import dataclass, field

import numpy as np

GRASS, WATER, MOUNTAIN = 0, 1, 2
DIRS = [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)]  # (dy, dx)


def cheb(ay: int, ax: int, by: int, bx: int) -> int:
    return max(abs(ay - by), abs(ax - bx))


@dataclass(eq=False)
class Carcass:
    """The remains of a killed prey animal: one portion each for other predators."""
    y: int
    x: int
    portions: int
    killer: int                 # the predator that made the kill doesn't eat from it again
    rot: int                    # ticks left before it rots away
    eaters: set = field(default_factory=set)   # predators that ate a portion

    def edible_by(self, predator) -> bool:
        return self.portions > 0 and predator.id != self.killer and predator.id not in self.eaters


def value_noise(h: int, w: int, octaves: list[int], rng: np.random.Generator) -> np.ndarray:
    out = np.zeros((h, w))
    for amp, cell in enumerate(octaves):
        gh, gw = h // cell + 2, w // cell + 2
        grid = rng.random((gh, gw))
        ys, xs = np.arange(h) / cell, np.arange(w) / cell
        y0, x0 = ys.astype(int), xs.astype(int)
        fy, fx = ys - y0, xs - x0
        fy, fx = fy * fy * (3 - 2 * fy), fx * fx * (3 - 2 * fx)       # smoothstep
        a = grid[np.ix_(y0, x0)]; b = grid[np.ix_(y0, x0 + 1)]
        c = grid[np.ix_(y0 + 1, x0)]; d = grid[np.ix_(y0 + 1, x0 + 1)]
        top = a + (b - a) * fx[None, :]
        bot = c + (d - c) * fx[None, :]
        out += (top + (bot - top) * fy[:, None]) / (2 ** amp)
    return (out - out.min()) / (np.ptp(out) + 1e-12)


def largest_component(walk: np.ndarray) -> np.ndarray:
    h, w = walk.shape
    label = np.full(walk.shape, -1, dtype=np.int32)
    best, best_size, cur = -1, 0, 0
    for sy, sx in zip(*np.nonzero(walk)):
        if label[sy, sx] >= 0:
            continue
        size, q = 0, deque([(sy, sx)])
        label[sy, sx] = cur
        while q:
            y, x = q.popleft()
            size += 1
            for dy, dx in DIRS:
                ny, nx = y + dy, x + dx
                if 0 <= ny < h and 0 <= nx < w and walk[ny, nx] and label[ny, nx] < 0:
                    label[ny, nx] = cur
                    q.append((ny, nx))
        if size > best_size:
            best, best_size = cur, size
        cur += 1
    return label == best


class World:
    def __init__(self, cfg, rng_world: np.random.Generator):
        self.cfg = cfg
        wc = cfg.world
        self.h, self.w = int(wc.height), int(wc.width)
        for _ in range(50):
            terrain = self._make_terrain(rng_world)
            walk = terrain == GRASS
            comp = largest_component(walk)
            if comp.sum() >= wc.min_walkable_connected * self.h * self.w:
                terrain[walk & ~comp] = MOUNTAIN   # unreachable pockets become rock
                break
        else:
            raise RuntimeError("could not generate a connected world; relax fractions")
        self.terrain = terrain
        self.walkable = terrain == GRASS
        self.walk_cells = np.argwhere(self.walkable)
        r = int(wc.water_bonus_radius)
        water = terrain == WATER
        padded = np.pad(water, r)
        near = np.zeros_like(water)
        for dy in range(2 * r + 1):
            for dx in range(2 * r + 1):
                near |= padded[dy:dy + self.h, dx:dx + self.w]
        self.near_water = near & self.walkable
        self.regrow_p = np.where(self.near_water, wc.food_regrow_p * wc.food_water_bonus,
                                 wc.food_regrow_p) * self.walkable
        self.food = (rng_world.random((self.h, self.w)) < wc.food_initial_fraction) & self.walkable
        self.carcasses: list[Carcass] = []
        self.portions_eaten = 0             # carcass portions eaten so far (stats)
        # drawn last, so worlds without cover are the same as before
        self.cover = self._make_cover(rng_world) if float(wc.get("cover_fraction") or 0) > 0 else None

    def _make_terrain(self, rng: np.random.Generator) -> np.ndarray:
        wc = self.cfg.world
        hmap = value_noise(self.h, self.w, list(wc.noise_octaves), rng)
        lo = np.quantile(hmap, wc.water_fraction)
        hi = np.quantile(hmap, 1 - wc.mountain_fraction)
        t = np.full((self.h, self.w), GRASS, dtype=np.int8)
        t[hmap < lo] = WATER
        t[hmap > hi] = MOUNTAIN
        k = int(wc.get("patches") or 0)
        if k > 1:                              # ridges between k x k patches, one gap per ridge segment
            g = int(wc.get("wall_gap") or 4)
            for j in range(1, k):
                t[j * self.h // k, :] = MOUNTAIN
                t[:, j * self.w // k] = MOUNTAIN
            for i in range(k):
                for j in range(1, k):
                    cx, cy = (2 * i + 1) * self.w // (2 * k) - g // 2, (2 * i + 1) * self.h // (2 * k) - g // 2
                    t[j * self.h // k, cx:cx + g] = GRASS
                    t[cy:cy + g, j * self.w // k] = GRASS
        return t

    def _make_cover(self, rng: np.random.Generator) -> np.ndarray:
        """Clustered cover on walkable cells: the top cover_fraction of a noise map."""
        noise = value_noise(self.h, self.w, [8, 4], rng)
        thr = np.quantile(noise[self.walkable], 1 - float(self.cfg.world.cover_fraction))
        return (noise >= thr) & self.walkable

    def in_cover(self, y: int, x: int) -> bool:
        return self.cover is not None and bool(self.cover[y, x])

    # --- queries -----------------------------------------------------------
    def is_walkable(self, y: int, x: int) -> bool:
        return 0 <= y < self.h and 0 <= x < self.w and bool(self.walkable[y, x])

    def random_cell(self, rng: np.random.Generator) -> tuple[int, int]:
        y, x = self.walk_cells[rng.integers(len(self.walk_cells))]
        return int(y), int(x)

    def nearest_food(self, y: int, x: int, radius: int) -> tuple[int, int, int] | None:
        """(fy, fx, distance) of the nearest food within radius (Chebyshev), else None."""
        return self._nearest_cell(self.food, y, x, radius)

    def nearest_cover(self, y: int, x: int, radius: int) -> tuple[int, int, int] | None:
        return None if self.cover is None else self._nearest_cell(self.cover, y, x, radius)

    def _nearest_cell(self, mask: np.ndarray, y: int, x: int, radius: int) -> tuple[int, int, int] | None:
        y0, y1 = max(0, y - radius), min(self.h, y + radius + 1)
        x0, x1 = max(0, x - radius), min(self.w, x + radius + 1)
        pts = np.argwhere(mask[y0:y1, x0:x1])
        if len(pts) == 0:
            return None
        pts = pts + (y0, x0)
        d = np.maximum(np.abs(pts[:, 0] - y), np.abs(pts[:, 1] - x))
        i = int(np.argmin(d))
        return int(pts[i, 0]), int(pts[i, 1]), int(d[i])

    # --- movement ----------------------------------------------------------
    def step_toward(self, y: int, x: int, ty: int, tx: int, rng: np.random.Generator,
                    away: bool = False) -> tuple[int, int]:
        """Greedy 8-neighbour step toward (or away from) a target over walkable cells."""
        here = np.hypot(ty - y, tx - x)
        best, cands = None, []
        for dy, dx in DIRS:
            ny, nx = y + dy, x + dx
            if not self.is_walkable(ny, nx):
                continue
            d = np.hypot(ty - ny, tx - nx)
            score = d if away else -d
            if best is None or score > best + 1e-9:
                best, cands = score, [(ny, nx)]
            elif abs(score - best) <= 1e-9:
                cands.append((ny, nx))
        if not cands:
            return y, x
        improving = (best > here) if away else (-best < here)
        if not improving:                      # stuck (e.g. behind water): random side step
            opts = [(y + dy, x + dx) for dy, dx in DIRS if self.is_walkable(y + dy, x + dx)]
            return opts[rng.integers(len(opts))]
        return cands[rng.integers(len(cands))]

    def step_heading(self, y: int, x: int, heading: int, turn_p: float,
                     rng: np.random.Generator) -> tuple[int, int, int]:
        """Persistent random walk. Returns (y, x, heading)."""
        if rng.random() < turn_p:
            heading = (heading + int(rng.choice([-1, 1, -2, 2]))) % 8
        for attempt in range(8):
            dy, dx = DIRS[heading]
            if self.is_walkable(y + dy, x + dx):
                return y + dy, x + dx, heading
            heading = int(rng.integers(8))
        return y, x, heading

    # --- dynamics ----------------------------------------------------------
    def regrow_food(self, rng: np.random.Generator) -> None:
        self.food |= rng.random((self.h, self.w)) < self.regrow_p

    def age_carcasses(self) -> None:
        """Carcasses rot; eaten-up and rotten ones are removed."""
        for c in self.carcasses:
            c.rot -= 1
        self.carcasses = [c for c in self.carcasses if c.portions > 0 and c.rot > 0]
