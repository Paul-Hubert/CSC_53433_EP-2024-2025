import json

import numpy as np

from promptevo.cache import KVCache, make_key
from promptevo.config import load_config
from promptevo.progress import Progress
from promptevo.rng import Streams


def test_config_profile_merge():
    c = load_config("small")
    assert c.world.width == 48 and c.agents.vision == 20 and c.profile == "small"
    c2 = load_config("small", {"agents": {"vision": 15}})
    assert c2.agents.vision == 15 and c2.predators.vision == 20 and c2.perception.bands == [1, 4, 10]


def test_streams_reproducible_and_independent():
    a, b = Streams(1), Streams(1)
    assert np.array_equal(a.get("x").random(5), b.get("x").random(5))
    assert not np.array_equal(Streams(1).get("x").random(5), Streams(1).get("y").random(5))
    s = Streams(2)
    first = s.get("z").random(3)
    assert np.array_equal(s.fresh("z").random(3), first)


def test_cache_roundtrip(tmp_path):
    c = KVCache(tmp_path / "c.sqlite")
    k = make_key("a", 1, {"b": [1, 2]})
    assert c.get(k) is None
    c.put(k, {"p": [0.5, 0.5]})
    assert KVCache(tmp_path / "c.sqlite").get(k) == {"p": [0.5, 0.5]}
    assert make_key("a", 1, {"b": [1, 2]}) == k


def test_progress_file(tmp_path):
    p = Progress(tmp_path, "job", total=10)
    p.update(5)
    p.finish(10)
    r = json.loads((tmp_path / "job.progress.json").read_text())
    assert r["state"] == "done" and r["done"] == 10
