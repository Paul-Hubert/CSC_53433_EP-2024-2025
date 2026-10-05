"""Lockstep simulation loop (plan §A4): decide every D ticks, execute, reproduce, die.

The backend is only called for (genome, observation) pairs not yet seen in this
run; identical situations reuse the stored distribution (big speed-up with Laya).
"""
from __future__ import annotations

import json
import platform
import subprocess
import time
from datetime import datetime, timezone
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np

from .actions import EXECUTORS
from .backends.base import Query, fallback_rows
from .eventlog import EventLog
from .evolution.mutation import Mutator
from .founder import AllelePools
from .genome import ACTIONS, AlleleRegistry, Genome, crossover_uniform
from .perception import mate_ready, sense
from .rng import Streams
from .world import World, cheb


@dataclass(eq=False)
class Agent:
    id: int
    y: int
    x: int
    energy: float
    genome: Genome
    generation: int = 0
    parents: tuple = ()
    born: int = 0
    age: int = 0
    heading: int = 0
    action: str | None = None
    invalid: bool = False
    attacked: bool = False
    bred: bool = False
    food_eaten: int = 0
    offspring: int = 0
    steals: int = 0
    immigrant: bool = False
    killed: bool = False
    robbed: bool = False        # lost energy to an attack this tick (death cause attribution)


@dataclass
class Counters:
    decisions: int = 0
    backend_queries: int = 0
    memo_hits: int = 0
    fallbacks: int = 0          # backend rows answered by a stand-in (failed call), not memoised
    invalid: int = 0
    births: int = 0
    immigrants: int = 0
    deaths: Counter = field(default_factory=Counter)
    actions: Counter = field(default_factory=Counter)
    backend_time: float = 0.0


class Simulation:
    def __init__(self, cfg, backend, seed: int | None = None, world_seed: int | None = None,
                 out_dir: str | Path | None = None, registry: AlleleRegistry | None = None,
                 pools: AllelePools | None = None, rewriter=None, rewriter_model=None,
                 progress=None):
        self.cfg, self.backend = cfg, backend
        self.seed = int(cfg.seed if seed is None else seed)
        self.world_seed = int(cfg.seed if world_seed is None else world_seed)
        ws = Streams(self.world_seed)
        self.streams = Streams(self.seed)
        self.rng_agents = self.streams.get("agents")
        self.rng_sampling = self.streams.get("sampling")
        self.rng_act = self.streams.get("actions")
        self.rng_mut = self.streams.get("mutation")
        self.rng_food = self.streams.get("food")
        self.world = World(cfg, ws.get("world"), self.streams.get("predators"))
        self.registry = registry or AlleleRegistry()
        self.pools = pools or AllelePools(self.registry, cfg.paths.data_dir)
        # One founder source for founders, immigrants AND founder_reintroduce, so control C4
        # (random_founders) never lets real founder genes back in through mutation.
        self.founder_pool = (self.pools.control_pool() if cfg.evolution.random_founders
                             else self.pools.founders)
        self.mutator = Mutator(cfg, self.registry, self.founder_pool, rewriter, rewriter_model)
        self.log = EventLog(out_dir)
        self.out_dir = Path(out_dir) if out_dir else None
        self.progress = progress
        self.rewriter_model = rewriter_model
        self.memo: dict[tuple[str, object], np.ndarray] = {}
        self.c = Counters()
        self.t = 0
        self.next_id = 0
        self.agents: list[Agent] = []
        self.dead_lifespans: list[int] = []
        self.run_info = self._provenance() if self.out_dir else {}   # only runs that write files
        self._write_run_info()
        for _ in range(int(cfg.agents.init_pop)):
            self._spawn_founder(immigrant=False)

    # --- population ----------------------------------------------------------
    def _founder_genome(self) -> Genome:
        if self.cfg.evolution.random_founders:
            return self.pools.sample_control(self.rng_agents)
        return self.pools.sample_founder(self.rng_agents)

    def _new_agent(self, y, x, energy, genome, generation=0, parents=()) -> Agent:
        a = Agent(self.next_id, y, x, float(energy), genome, generation, parents, born=self.t,
                  heading=int(self.rng_agents.integers(8)))
        self.next_id += 1
        self.agents.append(a)
        return a

    def _spawn_founder(self, immigrant: bool) -> Agent:
        y, x = self.world.random_cell(self.rng_agents)
        a = self._new_agent(y, x, self.cfg.agents.energy_start, self._founder_genome())
        a.immigrant = immigrant
        self.log.event("immigrant" if immigrant else "founder", self.t, id=a.id,
                       genome=list(a.genome.alleles))
        if immigrant:
            self.c.immigrants += 1
        return a

    def _birth(self, parents: list[Agent]) -> None:
        ac = self.cfg.agents
        if len(parents) == 2:
            g = crossover_uniform(parents[0].genome, parents[1].genome, self.rng_mut)
        else:
            g = parents[0].genome
        g, muts = self.mutator.mutate(g, self.rng_mut)
        share = ac.child_energy / len(parents)
        for p in parents:
            p.energy -= share
            p.offspring += 1
            p.bred = True
        gen = max(p.generation for p in parents) + 1
        child = self._new_agent(parents[0].y, parents[0].x, ac.child_energy, g, gen,
                                tuple(p.id for p in parents))
        self.c.births += 1
        self.log.event("birth", self.t, id=child.id, parents=list(child.parents), gen=gen,
                       genome=list(g.alleles), mutations=muts)

    def _die(self, a: Agent, cause: str) -> None:
        self.c.deaths[cause] += 1
        self.dead_lifespans.append(a.age)
        self.log.event("death", self.t, id=a.id, cause=cause, age=a.age, gen=a.generation,
                       food=a.food_eaten, offspring=a.offspring, steals=a.steals)

    # --- decisions -----------------------------------------------------------
    def decide(self, agents: list[Agent] | None = None) -> None:
        """Choose actions for `agents` (default: everyone) in one backend batch."""
        agents = self.agents if agents is None else agents
        shuffled = self.cfg.evolution.shuffled and len(self.agents) > 1
        pending: dict[tuple, Query] = {}
        keys = []
        for a in agents:
            g = a.genome
            if shuffled:  # C3 control: behave according to someone else's genes
                others = [b for b in self.agents if b is not a]
                g = others[int(self.rng_agents.integers(len(others)))].genome
            obs = sense(a, self.world, self.agents, self.cfg)
            gk = self.registry.genome_key(g)
            key = (gk, obs)
            keys.append(key)
            if key in self.memo:
                self.c.memo_hits += 1
            elif key not in pending:
                pending[key] = Query(gk, self.registry.genes(g), obs)
        fresh: dict[tuple, np.ndarray] = {}
        if pending:
            t0 = time.perf_counter()
            probs = self.backend.decide(list(pending.values()))
            self.c.backend_time += time.perf_counter() - t0
            self.c.backend_queries += len(pending)
            failed = fallback_rows(self.backend)
            self.c.fallbacks += len(failed)
            for i, (k, p) in enumerate(zip(pending, probs)):
                fresh[k] = np.asarray(p, dtype=float)
                if i not in failed:          # a stand-in answer is used now but never memoised
                    self.memo[k] = fresh[k]
        tau = float(self.cfg.sim.sampling_temperature)
        for a, key in zip(agents, keys):
            p = fresh[key] if key in fresh else self.memo[key]
            if tau != 1.0:
                p = p ** (1.0 / tau)
                p = p / p.sum()
            a.action = ACTIONS[int(self.rng_sampling.choice(len(ACTIONS), p=p))]
            a.invalid = a.attacked = a.bred = False
            self.c.decisions += 1
            self.c.actions[a.action] += 1

    # --- one tick ------------------------------------------------------------
    def step(self) -> None:
        ac, cfg = self.cfg.agents, self.cfg
        if self.t % int(cfg.sim.decision_period) == 0:
            self.decide()
        else:
            newcomers = [a for a in self.agents if a.action is None]   # born / immigrated
            if newcomers:
                self.decide(newcomers)
        for a in self.agents:
            a.robbed = False
        order = self.rng_act.permutation(len(self.agents))
        for i in order:
            a = self.agents[i]
            was_invalid = a.invalid
            moved = EXECUTORS[a.action](a, self.world, self.agents, cfg, self.rng_act)
            if a.invalid and not was_invalid:
                self.c.invalid += 1
            if a.action == "rest" and not moved:
                a.energy -= ac.cost_rest
            else:
                a.energy -= ac.cost_base + (ac.cost_move if moved else 0.0)
        self._breed()
        self.world.move_predators(self.agents)
        self._predation()
        dead = []
        for a in self.agents:
            a.age += 1
            if a.energy <= 0:          # robbed this tick → the attack is the cause, not hunger
                dead.append((a, "attacked" if a.robbed else "starvation"))
            elif a.age > ac.max_age:
                dead.append((a, "old_age"))
        for a, cause in dead:
            self._die(a, cause)
        gone = {id(a) for a, _ in dead}
        self.agents = [a for a in self.agents if id(a) not in gone]
        self.world.regrow_food(self.rng_food)
        while len(self.agents) < int(ac.floor):
            self._spawn_founder(immigrant=True)
        self.t += 1
        if self.t % int(cfg.sim.stats_every) == 0:
            self.log.stats(self.stats_row())

    def _breed(self) -> None:
        ac = self.cfg.agents
        cap = int(ac.cap)
        if not self.cfg.evolution.sexual:
            for a in list(self.agents):
                if len(self.agents) >= cap:
                    return
                if a.action == "mate" and not a.bred and mate_ready(a, ac):
                    self._birth([a])
            return
        ready = [a for a in self.agents if a.action == "mate" and not a.bred and mate_ready(a, ac)]
        for a in ready:
            if len(self.agents) >= cap or a.bred:
                continue
            for b in ready:
                if b is not a and not b.bred and cheb(a.y, a.x, b.y, b.x) <= 1:
                    self._birth([a, b])
                    break

    def _predation(self) -> None:
        pc = self.cfg.predators
        for p in self.world.predators:
            if p.rest > 0:
                continue
            for a in self.agents:
                if a.y == p.y and a.x == p.x and not a.killed:
                    if self.world.rng_pred.random() < pc.kill_p:
                        a.killed = True
                        p.rest = int(pc.rest_after_kill)
                    break
        # killed agents are removed as "predator" deaths before the starvation check
        killed = [a for a in self.agents if a.killed]
        for a in killed:
            self._die(a, "predator")
        if killed:
            self.agents = [a for a in self.agents if not a.killed]

    # --- running -------------------------------------------------------------
    def stats_row(self) -> dict:
        n = len(self.agents)
        return {"t": self.t, "pop": n,
                "mean_energy": round(float(np.mean([a.energy for a in self.agents])) if n else 0.0, 2),
                "mean_gen": round(float(np.mean([a.generation for a in self.agents])) if n else 0.0, 2),
                "max_gen": max((a.generation for a in self.agents), default=0),
                "births": self.c.births, "immigrants": self.c.immigrants,
                "deaths_starve": self.c.deaths["starvation"], "deaths_pred": self.c.deaths["predator"],
                "deaths_age": self.c.deaths["old_age"], "deaths_attacked": self.c.deaths["attacked"],
                "decisions": self.c.decisions,
                "backend_queries": self.c.backend_queries, "invalid": self.c.invalid,
                "alleles": len(self.registry)}

    def run(self, ticks: int, progress_every: int = 500) -> dict:
        for _ in range(ticks):
            self.step()
            if self.progress and self.t % progress_every == 0:
                self.progress.update(self.t, pop=len(self.agents))
        return self.finish()

    # --- provenance --------------------------------------------------------------
    def _provenance(self) -> dict:
        """Everything needed to reproduce or audit this run (written to run_info.json)."""
        describe = getattr(self.backend, "describe", None)
        return {"created_utc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
                "seed": self.seed, "world_seed": self.world_seed,
                "profile": self.cfg.get("profile"), "config": self.cfg,
                "backend": describe() if callable(describe) else {"name": getattr(self.backend, "name", "?")},
                "mutator_model": self.rewriter_model,
                "founder_pool": "control" if self.cfg.evolution.random_founders else "founder",
                "git": _git_state(), "python": platform.python_version(), "numpy": np.__version__}

    def _write_run_info(self, **extra) -> None:
        if self.out_dir:
            self.out_dir.mkdir(parents=True, exist_ok=True)
            info = {**self.run_info, **extra}
            (self.out_dir / "run_info.json").write_text(json.dumps(info, indent=1, default=str))

    def finish(self) -> dict:
        s = self.summary()
        self._write_run_info(finished_utc=datetime.now(timezone.utc).isoformat(timespec="seconds"),
                             ticks=self.t, events_sha=s["events_sha"])
        if self.out_dir:
            self.registry.dump_jsonl(self.out_dir / "alleles.jsonl")
            (self.out_dir / "summary.json").write_text(json.dumps(s, indent=1))
            living = [{"id": a.id, "gen": a.generation, "genome": list(a.genome.alleles)}
                      for a in self.agents]
            (self.out_dir / "final_population.json").write_text(json.dumps(living))
        self.log.close()
        return s

    def summary(self) -> dict:
        c = self.c
        return {"ticks": self.t, "seed": self.seed, "world_seed": self.world_seed,
                "backend": getattr(self.backend, "name", "?"),
                "pop_final": len(self.agents), "births": c.births, "immigrants": c.immigrants,
                "deaths": dict(c.deaths),
                "mean_lifespan": round(float(np.mean(self.dead_lifespans)), 1) if self.dead_lifespans else None,
                "max_gen": max((a.generation for a in self.agents), default=0),
                "decisions": c.decisions, "backend_queries": c.backend_queries,
                "memo_hit_rate": round(c.memo_hits / max(1, c.decisions), 3),
                "backend_s": round(c.backend_time, 2), "fallbacks": c.fallbacks,
                "invalid_rate": round(c.invalid / max(1, c.decisions), 3),
                "action_share": {k: round(v / max(1, c.decisions), 3) for k, v in c.actions.most_common()},
                "mutations": {k: v for k, v in self.mutator.stats.items() if v["tried"]},
                "alleles": len(self.registry), "events_sha": self.log.digest()[:16]}


def _git_state() -> dict:
    """Commit and dirty flag of the prototype's repository, or {} if git is unavailable."""
    from .config import ROOT
    try:
        run = lambda *a: subprocess.run(["git", *a], cwd=ROOT, capture_output=True, text=True,
                                        timeout=5, check=True).stdout.strip()
        return {"commit": run("rev-parse", "HEAD"), "dirty": bool(run("status", "--porcelain", "--", "."))}
    except (OSError, subprocess.SubprocessError):
        return {}
