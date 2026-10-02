import re

import numpy as np

from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.evolution.mutation import TEMPLATE, Mutator, clean, load_instructions, valid
from promptevo.sim import Simulation


def test_guards_and_instructions():
    assert clean('Here is the new sentence: "Eat fast when hungry" and more') == "Eat fast when hungry."
    assert not valid("Eat.", "eat.", 12) and not valid("x " * 20, "y", 12)
    ins = load_instructions("prompts/mutate_v2.txt")
    assert len(ins) >= 10 and len(set(ins)) == len(ins) and not any(i.startswith("#") for i in ins)


def test_mutator_sends_only_the_gene(reg_pools):
    reg, pools = reg_pools
    cfg2 = load_config("small", {"evolution": {"p_mut": 1.0}})
    g0 = pools.sample_founder(np.random.default_rng(1))
    genes = reg.genes(g0)

    def run():
        sent = []

        def fake(prompt, seed):
            sent.append((prompt, seed))
            return 'Sure! Here it is: "Run fast from every shadow."'
        m = Mutator(cfg2, reg, fake, "fake")
        return m, sent, m.mutate(g0, np.random.default_rng(2))
    m, sent, (g, events) = run()
    assert len(events) == 10 and m.stats == {"tried": 10, "ok": 10}
    for (prompt, seed), e in zip(sent, events):
        # the instruction and the gene sentence, nothing else: no world, no other genes
        assert prompt == TEMPLATE.format(instruction=m.instructions[e["prompt"]], text=genes[e["locus"]])
        al = reg.get(g.at(e["locus"]))
        assert al.text == "Run fast from every shadow." and al.operator == f"llm#{e['prompt']}"
        assert al.seed == seed and al.model == "fake"
    assert len({e["prompt"] for e in events}) > 1                   # instructions are drawn at random
    assert run()[1] == sent                                          # same rng -> same prompts and seeds


def test_mutator_rejects_bad_answers_and_needs_an_llm(reg_pools):
    reg, pools = reg_pools
    cfg2 = load_config("small", {"evolution": {"p_mut": 1.0}})
    g0 = pools.sample_founder(np.random.default_rng(1))
    too_long = Mutator(cfg2, reg, lambda prompt, seed: "word " * 30, "fake")
    assert too_long.mutate(g0, np.random.default_rng(3)) == (g0, [])
    assert too_long.stats == {"tried": 10, "ok": 0}
    unchanged = Mutator(cfg2, reg, lambda prompt, seed: re.search(r'"(.*)"', prompt).group(1), "fake")
    assert unchanged.mutate(g0, np.random.default_rng(3)) == (g0, [])
    off = Mutator(cfg2, reg)                                         # no mutator LLM: no mutation
    assert off.p_mut == 0 and off.mutate(g0, np.random.default_rng(3)) == (g0, [])


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
