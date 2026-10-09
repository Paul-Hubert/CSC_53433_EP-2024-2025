"""experiments.mutation_list: every mutation of a run, both species, once."""
import json

from experiments.mutation_list import rows_of
from promptevo.backends.rule_based import RuleBasedBackend
from promptevo.config import load_config
from promptevo.sim import Simulation


def test_every_mutant_gene_is_listed_once_with_its_parent(tmp_path):
    cfg = load_config("small", {"evolution": {"p_mut": 0.3}})
    sim = Simulation(cfg, RuleBasedBackend(), seed=8, out_dir=tmp_path,
                     rewriter=lambda prompt, seed: f"Mutant rule {seed % 997}.", rewriter_model="fake")
    for _ in range(600):
        sim.step()
    sim.checkpoint()
    alleles = [json.loads(line) for line in (tmp_path / "alleles.jsonl").read_text(encoding="utf-8").splitlines()]
    text = {a["id"]: a["text"] for a in alleles}
    mutants = [a for a in alleles if a["origin"] == "mutant"]
    rows = rows_of(tmp_path, 100, cfg)
    assert mutants and len(rows) == len(mutants)
    assert {r["species"] for r in rows} <= {"prey", "predator"}
    by_text = {(r["slot"], r["text"]): r for r in rows}
    for a in mutants:
        r = by_text[(a["locus"], a["text"])]
        assert r["parent"] == text[a["parent_id"]] and r["instr"] != "?" and r["peak"] >= r["end"] >= 0
