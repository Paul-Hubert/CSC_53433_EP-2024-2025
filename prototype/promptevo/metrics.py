"""Sensitivity metrics (plan §A9). Distributions are arrays whose last axis = actions.

Shapes used below: P[g, o, a] = p(action a | observation o, genome g).
All information quantities are in bits.
"""
from __future__ import annotations

import numpy as np

EPS = 1e-12


def entropy(p: np.ndarray) -> np.ndarray:
    p = np.asarray(p, dtype=float)
    return -(p * np.log2(np.clip(p, EPS, 1))).sum(-1)


def jsd(p: np.ndarray, q: np.ndarray) -> np.ndarray:
    """Jensen–Shannon divergence in bits (0..1), along the last axis."""
    p, q = np.asarray(p, float), np.asarray(q, float)
    m = 0.5 * (p + q)
    return entropy(m) - 0.5 * (entropy(p) + entropy(q))


def mi_genome(P: np.ndarray) -> float:
    """MI_G = mean_o [ H(mean_g p) − mean_g H(p) ]: how much the genome decides the action."""
    return float(np.mean(entropy(P.mean(0)) - entropy(P).mean(0)))


def mi_obs(P: np.ndarray) -> float:
    """MI_O = mean_g [ H(mean_o p) − mean_o H(p) ]: how much the situation decides the action."""
    return float(np.mean(entropy(P.mean(1)) - entropy(P).mean(1)))


def behaviour_distance(Pa: np.ndarray, Pb: np.ndarray) -> float:
    """d(g, g') = mean_o JSD(p(.|o,g), p(.|o,g')); inputs are [O, A]."""
    return float(np.mean(jsd(Pa, Pb)))


def directed(P_pro: np.ndarray, P_anti: np.ndarray, action: int,
             relevant: np.ndarray) -> dict:
    """ΔP over relevant observations for one contrast pair (inputs [O, A], mask [O])."""
    d = P_pro[relevant, action] - P_anti[relevant, action]
    return {"mean_dp": float(d.mean()) if len(d) else float("nan"),
            "sign_acc": float((d > 0).mean()) if len(d) else float("nan"), "n": int(len(d))}


def rankdata(x: np.ndarray) -> np.ndarray:
    x = np.asarray(x, float)
    order = np.argsort(x, kind="mergesort")
    ranks = np.empty(len(x))
    ranks[order] = np.arange(1, len(x) + 1)
    for v in np.unique(x):                      # average ties
        m = x == v
        if m.sum() > 1:
            ranks[m] = ranks[m].mean()
    return ranks


def spearman(x, y) -> float:
    rx, ry = rankdata(x), rankdata(y)
    if rx.std() == 0 or ry.std() == 0:
        return float("nan")
    return float(np.corrcoef(rx, ry)[0, 1])


def locality(edit_d: list[float], unrelated_d: list[float]) -> dict:
    e, u = np.median(edit_d), np.median(unrelated_d)
    return {"median_edit": float(e), "median_unrelated": float(u),
            "ratio": float(e / u) if u > 0 else float("nan")}


def shannon_diversity(counts) -> float:
    c = np.asarray(list(counts), float)
    c = c[c > 0]
    return float(entropy(c / c.sum())) if len(c) else 0.0


def bootstrap_ci(x, n: int = 2000, alpha: float = 0.05, rng=None) -> tuple[float, float, float]:
    rng = rng or np.random.default_rng(0)
    x = np.asarray(x, float)
    means = rng.choice(x, size=(n, len(x)), replace=True).mean(1)
    return float(x.mean()), float(np.quantile(means, alpha / 2)), float(np.quantile(means, 1 - alpha / 2))
