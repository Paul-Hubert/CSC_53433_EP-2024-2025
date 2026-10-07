"""Headless grid world: terrain (water/mountain) and food. Plan §A4.

Predators were scripted here until 2026-10-07; they are now genetic animals (sim.py, actions.py)."""
from __future__ import annotations

from collections import deque

import numpy as np

GRASS, WATER, MOUNTAIN = 0, 1, 2
DIRS = [(-1, -1), (-1, 0), (-1, 1), (0, -1), (0, 1), (1, -1), (1, 0), (1, 1)]  # (dy, dx)


def cheb(ay: int, ax: int, by: int, bx: int) -> int:
    return max(abs(ay - by), abs(ax - bx))


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

    def _make_terrain(self, rng: np.random.Generator) -> np.ndarray:
        wc = self.cfg.world
        hmap = value_noise(self.h, self.w, list(wc.noise_octaves), rng)
        lo = np.quantile(hmap, wc.water_fraction)
        hi = np.quantile(hmap, 1 - wc.mountain_fraction)
        t = np.full((self.h, self.w), GRASS, dtype=np.int8)
        t[hmap < lo] = WATER
        t[hmap > hi] = MOUNTAIN
        return t

    # --- queries -----------------------------------------------------------
    def is_walkable(self, y: int, x: int) -> bool:
        return 0 <= y < self.h and 0 <= x < self.w and bool(self.walkable[y, x])

    def random_cell(self, rng: np.random.Generator) -> tuple[int, int]:
        y, x = self.walk_cells[rng.integers(len(self.walk_cells))]
        return int(y), int(x)

    def nearest_food(self, y: int, x: int, radius: int) -> tuple[int, int, int] | None:
        """(fy, fx, distance) of the nearest food within radius (Chebyshev), else None."""
        y0, y1 = max(0, y - radius), min(self.h, y + radius + 1)
        x0, x1 = max(0, x - radius), min(self.w, x + radius + 1)
        pts = np.argwhere(self.food[y0:y1, x0:x1])
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
