import json

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


def test_random_founders_control_never_reintroduces_real_founders(reg_pools):
    cfg4 = load_config("small", {"evolution": {"random_founders": True, "p_mut": 1.0, "operators": {
        "intensity": 0, "negate": 0, "condition_swap": 0, "synonym": 0, "founder_reintroduce": 1.0,
        "llm_rewrite": 0}}})
    sim = Simulation(cfg4, RuleBasedBackend(), seed=3)
    controls = set(sim.pools.control_texts)
    real = {sim.registry.text(a) for ids in sim.pools.founders.values() for a in ids}
    rng = np.random.default_rng(0)
    for a in sim.agents[:10]:
        g, events = sim.mutator.mutate(a.genome, rng)
        assert events and all(e["text"] in controls for e in events)
        assert not {sim.registry.text(x) for x in g.alleles} & (real - controls)


def test_death_after_robbery_is_attributed_to_attack(cfg):
    cfg2 = load_config("small", {"predators": {"count": 0}})
    sim = Simulation(cfg2, RuleBasedBackend(), seed=1)
    attacker, victim, hungry = sim.agents[:3]
    sim.agents = [attacker, victim, hungry]
    victim.y, victim.x = attacker.y, attacker.x
    hungry.y, hungry.x = sim.world.walk_cells[-1]           # far away from the other two
    attacker.energy, victim.energy, hungry.energy = 100.0, 0.5, 0.1
    attacker.action, victim.action, hungry.action = "attack", "rest", "rest"
    sim.t = 1                                                # not a decision tick: actions stay
    sim.step()
    assert sim.c.deaths["attacked"] == 1 and sim.c.deaths["starvation"] == 1
    assert sim.stats_row()["deaths_attacked"] == 1


def test_run_info_records_provenance(cfg, tmp_path):
    sim = Simulation(cfg, RuleBasedBackend(), seed=7, world_seed=99, out_dir=tmp_path)
    assert (tmp_path / "run_info.json").exists()               # written at start (survives crashes)
    s = sim.run(50)
    info = json.loads((tmp_path / "run_info.json").read_text())
    assert info["seed"] == 7 and info["world_seed"] == 99 and s["world_seed"] == 99
    assert info["profile"] == "small" and info["config"]["agents"]["cap"] == cfg.agents.cap
    assert info["backend"] == {"name": "rule_based"} and info["founder_pool"] == "founder"
    assert info["ticks"] == 50 and info["events_sha"] == s["events_sha"] and "finished_utc" in info


def test_llm_rewrite_gets_the_locus_word_limit(reg_pools):
    from promptevo.genome import ACTION_LOCI, LOCI
    reg, pools = reg_pools
    cfg2 = load_config("small", {"evolution": {"p_mut": 1.0, "operators": {
        "intensity": 0, "negate": 0, "condition_swap": 0, "synonym": 0, "founder_reintroduce": 0,
        "llm_rewrite": 1.0}}})
    limits = {}

    def rewriter(text, style, seed, max_words=None):
        limits[text] = max_words
        return "Run fast from every shadow."
    m = Mutator(cfg2, reg, pools.founders, rewriter=rewriter, rewriter_model="fake")
    g = pools.sample_founder(np.random.default_rng(1))
    m.mutate(g, np.random.default_rng(2))
    for locus, aid in zip(LOCI, g.alleles):
        want = cfg2.evolution.max_action_words if locus in ACTION_LOCI else cfg2.evolution.max_temperament_words
        assert limits[reg.text(aid)] == want


def test_make_rewriter_fills_the_word_limit():
    from promptevo.llm.ollama_client import OllamaClient, make_rewriter
    from promptevo.config import resolve
    prompts = []

    def t(method, path, payload):
        if path == "/api/tags":
            return {"models": []}
        prompts.append(payload["messages"][0]["content"])
        return {"message": {"content": "Rest."}}
    rw = make_rewriter(OllamaClient(transport=t), "m", resolve("prompts/mutate_v1.md"), max_words=12)
    rw("Eat.", "invert", 1)
    rw("Bold.", "invert", 1, max_words=15)
    assert "at most 12 words" in prompts[0] and "at most 15 words" in prompts[1]
