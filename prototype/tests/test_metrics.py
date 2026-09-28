import numpy as np

from promptevo import metrics as M


def test_entropy_and_jsd():
    u = np.full(4, 0.25)
    assert abs(M.entropy(u) - 2.0) < 1e-9
    assert abs(M.jsd(u, u)) < 1e-12
    assert abs(M.jsd(np.array([1, 0.]), np.array([0, 1.])) - 1.0) < 1e-9


def test_mi_identical_genomes_is_zero():
    rng = np.random.default_rng(0)
    p = rng.dirichlet(np.ones(7), size=10)
    P = np.stack([p, p, p])                          # 3 identical genomes
    assert abs(M.mi_genome(P)) < 1e-9
    assert M.mi_obs(P) > 0


def test_mi_genome_max_when_deterministic_and_distinct():
    P = np.zeros((2, 5, 2))
    P[0, :, 0] = 1
    P[1, :, 1] = 1
    assert abs(M.mi_genome(P) - 1.0) < 1e-9


def test_directed_and_spearman():
    pro = np.array([[0.8, 0.2], [0.7, 0.3]])
    anti = np.array([[0.2, 0.8], [0.9, 0.1]])
    d = M.directed(pro, anti, 0, np.array([True, True]))
    assert d["sign_acc"] == 0.5 and abs(d["mean_dp"] - 0.2) < 1e-9
    assert abs(M.spearman([1, 2, 3], [10, 20, 30]) - 1) < 1e-9
    assert M.bootstrap_ci([1, 1, 1])[1:] == (1.0, 1.0)
