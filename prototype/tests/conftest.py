import pytest

from promptevo.config import load_config
from promptevo.founder import AllelePools
from promptevo.genome import AlleleRegistry


@pytest.fixture
def cfg():
    return load_config("small")


@pytest.fixture
def reg_pools(cfg):
    reg = AlleleRegistry()
    return reg, AllelePools(reg, cfg.paths.data_dir)
