import numpy as np

from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.evolution.mutation import Mutator, clean, op_intensity, op_negate, valid
from promptevo.sim import Simulation


def test_word_operators():
    rng = np.random.default_rng(0)
    assert op_negate("Always run away.", rng) == "Never run away."
    new = op_intensity("Sometimes rest.", rng)
    assert new in ("Rarely rest.", "Often rest.")
    assert clean('Here is the new sentence: "Eat fast when hungry" and more') == "Eat fast when hungry."
    assert not valid("Eat.", "eat.", 12) and not valid("x " * 20, "y", 12)


def test_mutator_with_fake_llm(cfg, reg_pools):
    reg, pools = reg_pools
    cfg2 = load_config("small", {"evolution": {"p_mut": 1.0, "operators": {
        "intensity": 0, "negate": 0, "condition_swap": 0, "synonym": 0, "founder_reintroduce": 0,
        "llm_rewrite": 1.0}}})
    calls = []

    def fake(text, style, seed):
        calls.append(style)
        return 'Sure! Here is it: "Run fast from every shadow."'
    m = Mutator(cfg2, reg, pools.founders, rewriter=fake, rewriter_model="fake")
    g, events = m.mutate(pools.sample_founder(np.random.default_rng(1)), np.random.default_rng(2))
    assert calls and events and all(e["op"] in ("llm_rewrite", "intensity", "negate", "condition_swap")
                                    for e in events)
    assert any(reg.text(g.at(e["locus"])) == "Run fast from every shadow." for e in events)


def _run(cfg, seed, ticks=600):
    sim = Simulation(cfg, RuleBasedBackend(), seed=seed)
    return sim.run(ticks), sim


def test_simulation_is_deterministic(cfg):
    s1, _ = _run(cfg, 11)
    s2, _ = _run(cfg, 11)
    s3, _ = _run(cfg, 12)
    assert s1["events_sha"] == s2["events_sha"]
    assert s1["events_sha"] != s3["events_sha"]


def test_population_bounds_and_births(cfg):
    s, sim = _run(cfg, 4, ticks=1500)
    assert cfg.agents.floor <= s["pop_final"] <= cfg.agents.cap
    assert s["births"] > 0 and s["memo_hit_rate"] > 0.3


def test_shuffled_control_uses_other_genomes(cfg):
    cfg2 = load_config("small", {"evolution": {"shuffled": True}})
    seen = []

    class Spy(RuleBasedBackend):
        def decide(self, queries):
            seen.extend(q.genome_key for q in queries)
            return super().decide(queries)
    sim = Simulation(cfg2, Spy(), seed=2)
    own = {sim.registry.genome_key(a.genome) for a in sim.agents}
    sim.decide()
    assert seen and set(seen) <= own          # only real genomes, but chosen at random


def test_no_mutation_means_no_new_alleles(cfg):
    cfg2 = load_config("small", {"evolution": {"p_mut": 0.0}})
    sim = Simulation(cfg2, RuleBasedBackend(), seed=5)
    n0 = len(sim.registry)
    sim.run(800)
    assert len(sim.registry) == n0
