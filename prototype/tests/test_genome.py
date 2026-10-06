import numpy as np

from promptevo.genome import LOCI, AlleleRegistry, crossover_uniform


def test_pools_loaded(reg_pools, cfg):
    reg, pools = reg_pools
    for l in LOCI:
        assert len(pools.founders[l]) == 5                      # 4 alleles + neutral
        assert pools.neutral[l] in pools.founders[l]
        for aid in pools.founders[l]:
            assert len(reg.text(aid).split()) <= cfg.evolution.max_words, reg.text(aid)
    for l in LOCI:
        pro, anti = pools.contrast_pair(l)
        diff = [x for x, y in zip(pro.alleles, anti.alleles) if x != y]
        assert len(diff) == 1


def test_registry_dedup_and_key(reg_pools):
    reg, pools = reg_pools
    a = reg.add("eat", "Eat  whenever food is close.", "mutant")
    assert a.origin == "founder"                                 # same text → existing allele
    g = pools.sample_founder(np.random.default_rng(0))
    reg2 = AlleleRegistry()
    g2 = reg2.make_genome(reg.genes(g))
    assert reg.genome_key(g) == reg2.genome_key(g2)              # text-based, run-independent


def test_crossover_takes_each_locus_from_a_parent(reg_pools):
    reg, pools = reg_pools
    rng = np.random.default_rng(3)
    a, b = pools.sample_founder(rng), pools.sample_control(rng)
    for _ in range(20):
        c = crossover_uniform(a, b, rng)
        assert all(x in (y, z) for x, y, z in zip(c.alleles, a.alleles, b.alleles))
