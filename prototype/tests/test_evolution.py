import csv
import re

import numpy as np

from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.evolution.mutation import TEMPLATE, Mutator, clean, load_instructions, valid
from promptevo.genome import ACTIONS, LOCI
from promptevo.sim import Simulation
from promptevo.species import PREDATOR

OLD_RULES = {"p_mut": 1.0, "mutation_prompts": "prompts/mutate_v2.txt", "mutation_tries": 1,
             "mutation_context": None}                                       # mutation before 2026-10-08


def test_guards_and_instructions():
    assert clean('Here is the new sentence: "Eat fast when hungry" and more') == "Eat fast when hungry."
    assert not valid("Eat.", "eat.", 12) and not valid("x " * 20, "y", 12)
    for f in ("prompts/mutate_v2.txt", "prompts/mutate_v3.txt", "prompts/mutate_v4.txt"):
        ins = load_instructions(f)
        assert len(ins) >= 7 and len(set(ins)) == len(ins) and not any(i.startswith("#") for i in ins)


def test_mutator_sends_only_the_gene(reg_pools):
    reg, pools = reg_pools
    cfg2 = load_config("small", {"evolution": OLD_RULES})
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
    assert len(events) == len(LOCI) and (m.stats["tried"], m.stats["ok"]) == (len(LOCI), len(LOCI))
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
    assert (too_long.stats["tried"], too_long.stats["ok"]) == (len(LOCI), 0)
    assert too_long.tries == 5                                       # evolution.mutation_tries
    assert too_long.stats["calls"] == 5 * len(LOCI) == too_long.stats["rejected"]["invalid"]   # every attempt rejected
    unchanged = Mutator(cfg2, reg, lambda prompt, seed: re.search(r'"(.*)"', prompt).group(1), "fake")
    assert unchanged.mutate(g0, np.random.default_rng(3)) == (g0, [])
    off = Mutator(cfg2, reg)                                         # no mutator LLM: no mutation
    assert off.p_mut == 0 and off.mutate(g0, np.random.default_rng(3)) == (g0, [])


def test_rejected_mutants_are_drawn_again(reg_pools):
    reg, pools = reg_pools
    cfg2 = load_config("small", {"evolution": {"p_mut": 1.0}})
    answers = iter(["word " * 30,                                       # too long
                    "Eat whenever food is close!",                      # only the punctuation changed
                    "Eat whenever food is very close."])                # accepted
    m = Mutator(cfg2, reg, lambda prompt, seed: next(answers), "fake")
    new, k, seed = m.mutate_text("eat", "Eat whenever food is close.", np.random.default_rng(0))
    assert new == "Eat whenever food is very close." and m.stats["calls"] == 3
    assert dict(m.stats["rejected"]) == {"invalid": 1, "unchanged": 1}


def test_context_line_comes_first(reg_pools):
    reg, _ = reg_pools
    sent = []
    m = Mutator(load_config("small", {"evolution": {"p_mut": 1.0}}), reg,
                lambda prompt, seed: sent.append(prompt) or "Eat whenever food is very close.", "fake")
    m.mutate_text("eat", "Eat whenever food is close.", np.random.default_rng(0))
    context = "The sentence below is a rule that a wild animal follows."
    assert m.context == context and sent[0].startswith(context + "\n") and '"Eat whenever food is close."' in sent[0]
    old = Mutator(load_config("small", {"evolution": OLD_RULES}), reg, lambda prompt, seed: "x", "fake")
    assert old.prompt(0, "Eat.") == TEMPLATE.format(instruction=old.instructions[0], text="Eat.")   # no context


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
            seen.extend((q.species, q.genome_key) for q in queries)
            return super().decide(queries)
    sim = Simulation(cfg2, Spy(), seed=2)
    own = {sim.registry.genome_key(a.genome) for a in sim.agents}
    own_pred = {sim.registry.genome_key(a.genome) for a in sim.predators}
    sim.decide()
    prey_seen = {k for sp, k in seen if sp == "prey"}
    pred_seen = {k for sp, k in seen if sp == "predator"}
    assert prey_seen and prey_seen <= own         # only real genomes, but chosen at random
    assert pred_seen and pred_seen <= own_pred    # each species behaves like one of its own


def test_no_mutation_means_no_new_alleles(cfg):
    cfg2 = load_config("small", {"evolution": {"p_mut": 0.0}})
    sim = Simulation(cfg2, RuleBasedBackend(), seed=5)
    n0 = len(sim.registry)
    sim.run(800)
    assert len(sim.registry) == n0


def test_stats_count_decisions_per_action(cfg, tmp_path):
    sim = Simulation(cfg, RuleBasedBackend(), seed=6, out_dir=tmp_path)
    sim.run(400)
    rows = list(csv.DictReader((tmp_path / "stats.csv").open()))
    last = rows[-1]
    assert int(last["t"]) == 400
    assert sum(int(last[f"act_{a}"]) for a in ACTIONS) == int(last["decisions"]) == sim.c.decisions
    assert sum(int(last[f"pred_act_{a}"]) for a in PREDATOR.actions) == int(last["pred_decisions"]) > 0
