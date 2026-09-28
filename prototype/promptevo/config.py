"""YAML config: base.yaml deep-merged with a profile and optional overrides."""
from __future__ import annotations

import copy
from pathlib import Path
from typing import Any

import yaml

ROOT = Path(__file__).resolve().parent.parent
CONFIG_DIR = ROOT / "configs"


class Cfg(dict):
    """dict with attribute access for nested sections (cfg.agents.vision)."""

    def __getattr__(self, name: str) -> Any:
        try:
            v = self[name]
        except KeyError as e:
            raise AttributeError(name) from e
        return Cfg(v) if isinstance(v, dict) and not isinstance(v, Cfg) else v


def deep_merge(a: dict, b: dict) -> dict:
    out = copy.deepcopy(a)
    for k, v in (b or {}).items():
        if isinstance(v, dict) and isinstance(out.get(k), dict):
            out[k] = deep_merge(out[k], v)
        else:
            out[k] = copy.deepcopy(v)
    return out


def _wrap(d: dict) -> Cfg:
    return Cfg({k: _wrap(v) if isinstance(v, dict) else v for k, v in d.items()})


def load_config(profile: str = "small", overrides: dict | None = None,
                extra_files: list[str | Path] | None = None) -> Cfg:
    data = yaml.safe_load((CONFIG_DIR / "base.yaml").read_text())
    prof = CONFIG_DIR / f"{profile}.yaml"
    if prof.exists():
        data = deep_merge(data, yaml.safe_load(prof.read_text()) or {})
    for f in extra_files or []:
        data = deep_merge(data, yaml.safe_load(Path(f).read_text()) or {})
    data = deep_merge(data, overrides or {})
    data["profile"] = profile
    return _wrap(data)


def resolve(path: str | Path) -> Path:
    """Paths in configs are relative to prototype/."""
    p = Path(path)
    return p if p.is_absolute() else ROOT / p
