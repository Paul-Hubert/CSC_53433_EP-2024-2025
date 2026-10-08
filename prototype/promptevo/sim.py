"""Lockstep simulation loop (plan §A4). Two species since 2026-10-07: prey animals and
predators are both genetic animals (genes, brain, energy, breeding, mutation).

Each tick: decide every D ticks (newcomers at once; one backend batch per species), prey act
and breed, predators act (a hunt can kill a prey animal or eat from a carcass) and breed,
deaths (killed, starvation, old age), food regrowth, carcasses rot, newcomers for each
species below its floor.

Stamina (since 2026-10-07): each cell moved costs one point of stamina and energy
(cost_move); a tick without moving brings stamina back (stamina_regen), which costs extra
energy (cost_regen) until stamina is full.

Against boom-bust crashes (2026-10-08, off by default; experiments/crash_sweep.py):
predators.prey_per_predator q lets predators breed only while there are at least q prey per
predator (a cap that follows the prey, Leslie-Gower), predators.breed_prey_seen m only with at
least m prey within their vision (the same, local); evolution.egg_bank: below the floor an
egg laid by a pair in the last evolution.egg_ticks ticks hatches (a delayed birth with real
parents and generation) before any newcomer with founder genes.

The backend is only called for (species, genome, observation) triples not yet seen in this
run; identical situations reuse the stored distribution (big speed-up with an LLM brain).
Events of predators carry "species": "predator"; prey events keep their earlier format.
"""
from __future__ import annotations

import json
import time
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np

from .actions import EXECUTORS
from .backends.base import Query
from .eventlog import EventLog, replace_file
from .evolution.mutation import Mutator
from .founder import AllelePools
from .genome import AlleleRegistry, Genome, crossover_uniform
from .perception import mate_ready, sense
from .rng import Streams
from .species import PREDATOR, PREY, SPECIES, Species, species_cfg
from .world import World, cheb


def _recover(a, sc) -> None:
    """A tick without moving brings stamina back; until it is full that costs extra energy."""
    if a.stamina < sc.stamina_max:
        a.stamina = min(float(sc.stamina_max), a.stamina + sc.stamina_regen)
        a.energy -= sc.cost_regen


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
    bred: bool = False
    food_eaten: int = 0          # food eaten (prey) or prey killed (predators)
    offspring: int = 0
    immigrant: bool = False
    killed: bool = False
    killer: int | None = None    # the predator that killed this prey animal
    digest: int = 0              # predators: ticks left digesting a meal (no decision, no move)
    stamina: float = 0.0         # cells it can move before it must stop; the simulation starts it full

    @property
    def species(self) -> str:
        return self.genome.species


@dataclass
class Counters:
    decisions: int = 0
    backend_queries: int = 0
    memo_hits: int = 0
    invalid: int = 0
    animal_ticks: int = 0        # one per living animal per tick
    exhausted: int = 0           # animal-ticks that began without stamina to move a cell
    births: int = 0
    immigrants: int = 0
    hatched: int = 0             # egg bank: eggs hatched below the floor
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
        ws = Streams(int(cfg.seed if world_seed is None else world_seed))
        self.streams = Streams(self.seed)
        self.rng_agents = self.streams.get("agents")        # prey founders and newcomers
        self.rng_pred = self.streams.get("predators")       # predator founders and newcomers
        self.rng_sampling = self.streams.get("sampling")
        self.rng_act = self.streams.get("actions")          # moves and kill rolls
        self.rng_mut = self.streams.get("mutation")
        self.rng_litter = self.streams.get("litter")        # litter sizes
        self.rng_food = self.streams.get("food")
        self.world = World(cfg, ws.get("world"))
        self.registry = registry or AlleleRegistry()
        self.pools = pools or AllelePools(self.registry, cfg.paths.data_dir)
        self.pred_pools = AllelePools(self.registry, cfg.paths.data_dir, species=PREDATOR)
        self.mutator = Mutator(cfg, self.registry, rewriter, rewriter_model)   # no rewriter: no mutation
        self.log = EventLog(out_dir)
        self.out_dir = Path(out_dir) if out_dir else None
        self.progress = progress
        self.memo: dict[tuple[str, str, object], np.ndarray] = {}
        self.cs = {name: Counters() for name in SPECIES}
        self.c = self.cs["prey"]                    # prey counters (their names from before 2026-10-07)
        self.t = 0
        self.next_id = 0
        self.agents: list[Agent] = []               # living prey animals
        self.predators: list[Agent] = []            # living predators
        self.lifespans: dict[str, list[int]] = {name: [] for name in SPECIES}
        self.eggs: dict[str, list[tuple]] = {name: [] for name in SPECIES}   # (t laid, parents, generation)
        for _ in range(int(cfg.agents.init_pop)):
            self._spawn_founder(PREY, immigrant=False)
        for _ in range(int(cfg.predators.init_pop)):
            self._spawn_founder(PREDATOR, immigrant=False)

    # --- population ----------------------------------------------------------
    def members(self, sp: Species) -> list[Agent]:
        return self.agents if sp.name == "prey" else self.predators

    def _set_members(self, sp: Species, agents: list[Agent]) -> None:
        if sp.name == "prey":
            self.agents = agents
        else:
            self.predators = agents

    def _rng(self, sp: Species) -> np.random.Generator:
        return self.rng_agents if sp.name == "prey" else self.rng_pred

    def _log(self, kind: str, sp: Species, **fields) -> None:
        if sp.name != "prey":
            fields["species"] = sp.name
        self.log.event(kind, self.t, **fields)

    def _founder_genome(self, sp: Species) -> Genome:
        pools = self.pools if sp.name == "prey" else self.pred_pools
        if self.cfg.evolution.random_founders:
            return pools.sample_control(self._rng(sp))
        return pools.sample_founder(self._rng(sp))

    def _new_agent(self, y, x, energy, genome, generation=0, parents=()) -> Agent:
        a = Agent(self.next_id, y, x, float(energy), genome, generation, parents, born=self.t,
                  heading=int(self._rng(genome.sp).integers(8)),
                  stamina=float(species_cfg(self.cfg, genome.sp).stamina_max))
        self.next_id += 1
        self.members(genome.sp).append(a)
        return a

    def _spawn_founder(self, sp: Species, immigrant: bool) -> Agent:
        y, x = self.world.random_cell(self._rng(sp))
        a = self._new_agent(y, x, species_cfg(self.cfg, sp).energy_start, self._founder_genome(sp))
        a.immigrant = immigrant
        self._log("immigrant" if immigrant else "founder", sp, id=a.id, genome=list(a.genome.alleles))
        if immigrant:
            self.cs[sp.name].immigrants += 1
        return a

    def _litter_size(self, parents: list[Agent], sc, room: int) -> int:
        """Babies from one mating (rev. 2026-10-08, before 1): drawn uniformly from sc.litter
        [min, max], cut (not below min) so that no parent pays more energy than it has (each baby
        costs the parents child_energy, shared), and cut to the room left under the cap."""
        lo, hi = (int(x) for x in (sc.get("litter") or (1, 1)))
        k = int(self.rng_litter.integers(lo, hi + 1))
        share = sc.child_energy / len(parents)
        affordable = min(int((p.energy - 1e-9) // share) for p in parents)
        return max(0, min(max(lo, min(k, affordable)), room))

    def _birth(self, parents: list[Agent], room: int = 1) -> None:
        """A litter (see _litter_size); every baby gets its own crossover and its own mutations."""
        sp = parents[0].genome.sp
        sc = species_cfg(self.cfg, sp)
        k = self._litter_size(parents, sc, room)
        share = sc.child_energy / len(parents)
        gen = max(p.generation for p in parents) + 1
        for p in parents:
            p.energy -= share * k
            p.offspring += k
            p.bred = True
        for _ in range(k):
            if len(parents) == 2:
                g = crossover_uniform(parents[0].genome, parents[1].genome, self.rng_mut)
            else:
                g = parents[0].genome
            g, muts = self.mutator.mutate(g, self.rng_mut)
            child = self._new_agent(parents[0].y, parents[0].x, sc.child_energy, g, gen,
                                    tuple(p.id for p in parents))
            self.cs[sp.name].births += 1
            self._log("birth", sp, id=child.id, parents=list(child.parents), gen=gen,
                      genome=list(g.alleles), mutations=muts, litter=k)
            if self.cfg.evolution.get("egg_bank"):
                self.eggs[sp.name].append((self.t, [p.genome for p in parents], child.parents, gen))

    def _hatch(self, sp: Species) -> bool:
        """Egg bank: hatch a random egg laid in the last egg_ticks ticks (crossover and mutation
        now, as at a birth). False if the bank is off or empty."""
        ec = self.cfg.evolution
        if not ec.get("egg_bank"):
            return False
        eggs = self.eggs[sp.name]
        eggs[:] = [e for e in eggs if self.t - e[0] <= int(ec.get("egg_ticks") or 2000)]
        if not eggs:
            return False
        laid, genomes, parents, gen = eggs.pop(int(self._rng(sp).integers(len(eggs))))
        g = crossover_uniform(genomes[0], genomes[1], self.rng_mut) if len(genomes) == 2 else genomes[0]
        g, muts = self.mutator.mutate(g, self.rng_mut)
        y, x = self.world.random_cell(self._rng(sp))
        a = self._new_agent(y, x, species_cfg(self.cfg, sp).energy_start, g, gen, parents)
        self.cs[sp.name].hatched += 1
        self._log("birth", sp, id=a.id, parents=list(parents), gen=gen, genome=list(g.alleles),
                  mutations=muts, laid=laid)
        return True

    def _die(self, a: Agent, cause: str) -> None:
        sp = a.genome.sp
        self.cs[sp.name].deaths[cause] += 1
        self.lifespans[sp.name].append(a.age)
        extra = {} if a.killer is None else {"killer": a.killer}
        self._log("death", sp, id=a.id, cause=cause, age=a.age, gen=a.generation,
                  food=a.food_eaten, offspring=a.offspring, **extra)

    # --- decisions -----------------------------------------------------------
    def decide(self, agents: list[Agent] | None = None) -> None:
        """Choose actions for `agents` (default: every living animal), one backend batch per
        species. A predator digesting a meal doesn't decide."""
        for sp in (PREY, PREDATOR):
            group = self.members(sp) if agents is None else [a for a in agents if a.species == sp.name]
            group = [a for a in group if a.digest == 0]
            if group:
                self._decide(sp, group)

    def _decide(self, sp: Species, agents: list[Agent]) -> None:
        members, c = self.members(sp), self.cs[sp.name]
        shuffled = self.cfg.evolution.shuffled and len(members) > 1
        pending: dict[tuple, Query] = {}
        keys = []
        for a in agents:
            g = a.genome
            if shuffled:  # C3 control: behave according to someone else's genes (same species)
                others = [b for b in members if b is not a]
                g = others[int(self.rng_agents.integers(len(others)))].genome
            obs = sense(a, self.world, self.agents, self.predators, self.cfg)
            gk = self.registry.genome_key(g)
            key = (sp.name, gk, obs)
            keys.append(key)
            if key in self.memo:
                c.memo_hits += 1
            elif key not in pending:
                pending[key] = Query(gk, self.registry.genes(g), obs, sp.name)
        if pending:
            t0 = time.perf_counter()
            probs = self.backend.decide(list(pending.values()))
            c.backend_time += time.perf_counter() - t0
            c.backend_queries += len(pending)
            for k, p in zip(pending, probs):
                self.memo[k] = np.asarray(p, dtype=float)
        tau = float(self.cfg.sim.sampling_temperature)
        for a, key in zip(agents, keys):
            p = self.memo[key]
            if tau != 1.0:
                p = p ** (1.0 / tau)
                p = p / p.sum()
            a.action = sp.actions[int(self.rng_sampling.choice(len(sp.actions), p=p))]
            a.invalid = a.bred = False
            c.decisions += 1
            c.actions[a.action] += 1

    # --- one tick ------------------------------------------------------------
    def step(self) -> None:
        cfg = self.cfg
        if self.t % int(cfg.sim.decision_period) == 0:
            self.decide()
        else:
            newcomers = [a for a in self.agents + self.predators   # born, immigrated, done digesting
                         if a.action is None and a.digest == 0]
            if newcomers:
                self.decide(newcomers)
        self._act(PREY)
        self._breed(PREY)
        self._act(PREDATOR)                          # predators act after the prey
        self._breed(PREDATOR)
        killed = [a for a in self.agents if a.killed]
        for a in killed:                             # removed as "predator" deaths before ageing
            self._die(a, "predator")
        if killed:
            self.agents = [a for a in self.agents if not a.killed]
        for sp in (PREY, PREDATOR):
            sc, dead = species_cfg(cfg, sp), []
            for a in self.members(sp):
                a.age += 1
                if a.energy <= 0:
                    dead.append((a, "starvation"))
                elif a.age > sc.max_age:
                    dead.append((a, "old_age"))
            for a, cause in dead:
                self._die(a, cause)
            if dead:
                gone = {id(a) for a, _ in dead}
                self._set_members(sp, [a for a in self.members(sp) if id(a) not in gone])
        self.world.regrow_food(self.rng_food)
        self.world.age_carcasses()
        for sp in (PREY, PREDATOR):
            while len(self.members(sp)) < int(species_cfg(cfg, sp).floor):
                if not self._hatch(sp):
                    self._spawn_founder(sp, immigrant=True)
        self.t += 1
        if self.t % int(cfg.sim.stats_every) == 0:
            self.log.stats(self.stats_row())

    def _act(self, sp: Species) -> None:
        sc, c, members = species_cfg(self.cfg, sp), self.cs[sp.name], self.members(sp)
        for i in self.rng_act.permutation(len(members)):
            a = members[i]
            c.animal_ticks += 1
            if a.stamina < 1:
                c.exhausted += 1                 # out of breath: can't move this tick
            if a.digest > 0:                     # digesting a meal: stays still, decides again when done
                a.digest -= 1
                if a.digest == 0:
                    a.action = None
                a.energy -= sc.cost_rest
                _recover(a, sc)
                continue
            was_invalid = a.invalid
            moved = EXECUTORS[a.action](a, self.world, self.agents, self.predators, self.cfg, self.rng_act)
            if a.invalid and not was_invalid:
                c.invalid += 1
            if moved:                            # each cell costs energy and a point of stamina
                a.energy -= sc.cost_base + sc.cost_move * moved
                a.stamina -= moved
            else:
                a.energy -= sc.cost_rest if a.action == "rest" else sc.cost_base
                _recover(a, sc)

    def _breed(self, sp: Species) -> None:
        sc = species_cfg(self.cfg, sp)
        cap = int(sc.cap)
        q = float(sc.get("prey_per_predator") or 0) if sp.name == "predator" else 0.0
        if q:                                     # predators breed only while there are q prey each
            cap = min(cap, int(len(self.agents) / q))
        members = self.members(sp)
        if not self.cfg.evolution.sexual:
            for a in list(members):
                if len(self.members(sp)) >= cap:
                    return
                if a.action == "mate" and not a.bred and mate_ready(a, sc):
                    self._birth([a], room=cap - len(self.members(sp)))
            return
        # One partner's choice is enough (rev. 2026-10-07): an animal that chose mate breeds with a
        # ready partner next to it, whatever that partner is doing. Before, both had to choose mate.
        seekers = [a for a in members if a.action == "mate" and not a.bred and mate_ready(a, sc)
                   and self._hunting_ground(a, sc)]
        for a in seekers:
            if len(self.members(sp)) >= cap or a.bred:
                continue
            for b in list(members):
                if b is not a and not b.bred and mate_ready(b, sc) and cheb(a.y, a.x, b.y, b.x) <= 1:
                    self._birth([a, b], room=cap - len(self.members(sp)))
                    break

    def _hunting_ground(self, a: Agent, sc) -> bool:
        """predators.breed_prey_seen m (off when 0): a predator breeds only with at least m prey
        within its vision (a local version of prey_per_predator)."""
        m = int(sc.get("breed_prey_seen") or 0) if a.species == "predator" else 0
        if not m:
            return True
        v = int(sc.vision)
        return sum(1 for b in self.agents if cheb(a.y, a.x, b.y, b.x) <= v) >= m

    # --- running -------------------------------------------------------------
    def stats_row(self) -> dict:
        """Prey columns keep their names; predator columns start with pred_."""
        row: dict = {"t": self.t}
        for sp, pre in ((PREY, ""), (PREDATOR, "pred_")):
            ms, c = self.members(sp), self.cs[sp.name]
            n = len(ms)
            row.update({f"{pre}pop": n,
                        f"{pre}mean_energy": round(float(np.mean([a.energy for a in ms])) if n else 0.0, 2),
                        f"{pre}mean_stamina": round(float(np.mean([a.stamina for a in ms])) if n else 0.0, 2),
                        f"{pre}exhausted": c.exhausted,
                        f"{pre}mean_gen": round(float(np.mean([a.generation for a in ms])) if n else 0.0, 2),
                        f"{pre}max_gen": max((a.generation for a in ms), default=0),
                        f"{pre}births": c.births, f"{pre}immigrants": c.immigrants,
                        f"{pre}deaths_starve": c.deaths["starvation"]})
            if sp.name == "prey":
                row["deaths_pred"] = c.deaths["predator"]          # prey killed by predators
            else:
                row["pred_portions"] = self.world.portions_eaten   # carcass portions eaten
            row.update({f"{pre}deaths_age": c.deaths["old_age"], f"{pre}decisions": c.decisions,
                        f"{pre}backend_queries": c.backend_queries, f"{pre}invalid": c.invalid})
            if sp.name == "prey":
                row["alleles"] = len(self.registry)                 # both species
            row.update({f"{pre}act_{a}": c.actions[a] for a in sp.actions})   # cumulative decisions per action
        return row

    def run(self, ticks: int, progress_every: int = 500) -> dict:
        for _ in range(ticks):
            self.step()
            if self.progress and self.t % progress_every == 0:
                self.progress.update(self.t, pop=len(self.agents), predators=len(self.predators))
        return self.finish()

    def checkpoint(self) -> None:
        """Save what a hard stop would lose: events and stats to disk, alleles.jsonl rewritten."""
        self.log.flush(sync=True)
        if self.out_dir:
            tmp = self.out_dir / "alleles.jsonl.tmp"
            self.registry.dump_jsonl(tmp)
            replace_file(tmp, self.out_dir / "alleles.jsonl")

    def finish(self) -> dict:
        s = self.summary()
        if self.out_dir:
            self.checkpoint()
            (self.out_dir / "summary.json").write_text(json.dumps(s, indent=1))
            living = [{"id": a.id, "gen": a.generation, "genome": list(a.genome.alleles)}
                      for a in self.agents]
            living += [{"id": a.id, "gen": a.generation, "genome": list(a.genome.alleles), "species": "predator"}
                       for a in self.predators]
            (self.out_dir / "final_population.json").write_text(json.dumps(living))
        self.log.close()
        return s

    def _species_summary(self, sp: Species) -> dict:
        c, ms, life = self.cs[sp.name], self.members(sp), self.lifespans[sp.name]
        return {"pop_final": len(ms), "births": c.births, "immigrants": c.immigrants, "hatched": c.hatched,
                "deaths": dict(c.deaths),
                "mean_lifespan": round(float(np.mean(life)), 1) if life else None,
                "max_gen": max((a.generation for a in ms), default=0),
                "decisions": c.decisions, "backend_queries": c.backend_queries,
                "memo_hit_rate": round(c.memo_hits / max(1, c.decisions), 3),
                "backend_s": round(c.backend_time, 2),
                "invalid_rate": round(c.invalid / max(1, c.decisions), 3),
                "exhausted_share": round(c.exhausted / max(1, c.animal_ticks), 3),
                "action_share": {k: round(v / max(1, c.decisions), 3) for k, v in c.actions.most_common()}}

    def summary(self) -> dict:
        """Prey figures at the top level (as before 2026-10-07), predators under "predators"."""
        pred = self._species_summary(PREDATOR)
        pred["kills"] = self.c.deaths["predator"]
        pred["portions"] = self.world.portions_eaten                # carcass portions eaten
        return {"ticks": self.t, "seed": self.seed, "backend": getattr(self.backend, "name", "?"),
                **self._species_summary(PREY),
                "predators": pred,
                "mutations": dict(self.mutator.stats),
                "alleles": len(self.registry), "events_sha": self.log.digest()[:16]}
