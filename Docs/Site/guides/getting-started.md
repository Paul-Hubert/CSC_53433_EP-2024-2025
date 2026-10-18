---
uid: guide-getting-started
---

# Getting started

This page takes you from opening the project to watching a run, inspecting it and finding its output files. It takes
about ten minutes.

## What you need

- **Unity 6000.3.9f1**, installed with the Unity Hub, on Windows (the course's machines).
- This repository, opened as a Unity project. The EvoSim code is under `Assets/EvoSim`.
- *Optional:* the two model servers. Without them you can still run everything with the random brain.

| Server | Address | What it does | Start it with |
|---|---|---|---|
| **JEV** (vLLM in Docker, GPU) | `localhost:8000` | the brain: reads genes and situations, gives a probability per action | `docker compose -f prototype/docker/compose.yaml up -d` (5–8 minutes until healthy), after a one-time download: see [Model servers](#model-servers-for-l1-and-l2) |
| **Ollama** with `gemma4:26b` (CPU) | `localhost:11434` | the mutator: rewrites a gene sentence when it mutates | `ollama serve` with two settings, then `ollama pull gemma4:26b` once: see [Model servers](#model-servers-for-l1-and-l2) |

Run level **L0** (the random brain, no mutation) needs neither server. **L1** needs JEV, **L2** needs JEV and Ollama
(see [Run levels](running-and-building.md#run-levels)).

> [!NOTE]
> The Windows player build is **not** in the repository: `Builds/` is ignored by git. To watch runs outside the editor,
> build it yourself once with *EvoSim ▸ Build ▸ Visual Player* (see [Player builds](running-and-building.md#player-builds)).
> Everything on this page works in the editor without it.

## Model servers (for L1 and L2)

Skip this section if you only run L0. All commands run in PowerShell from the repository root.

### What the machine needs

- **Docker Desktop** with the **WSL 2** backend and GPU support (an up-to-date NVIDIA driver on Windows; nothing to install
  inside WSL).
- An **NVIDIA GPU with about 16 GB** of memory: JEV in FP8 fills an RTX 5080 (14.4 GB in use).
- About **17 GB of free disk** for the JEV weights, in your Hugging Face cache (`%USERPROFILE%\.cache\huggingface`).
- For Ollama: about **20 GB of free RAM**, since `gemma4:26b` runs on the CPU.
- Python 3 from python.org, started with the **`py -3`** launcher. Don't type bare `python`: on these machines it can
  open the Windows Store stub instead of Python.

### JEV: download the weights once

The JEV container runs **offline** (`HF_HUB_OFFLINE=1` in `prototype/docker/compose.yaml`): it reads the weights from your
Hugging Face cache and never downloads them. Download them once, at the revision the container is pinned to:

```powershell
py -3 -m pip install --user huggingface_hub
py -3 -c "from huggingface_hub import snapshot_download as d; d('autotrust/JEV-9B', revision='b63f651ce8ed64481d3f5e73ecdb05f740042f01', allow_patterns=['*.json', '*.jinja', 'model-*.safetensors', 'head.safetensors', 'adapter/*', 'adapter_vllm/*'])"
```

The download is about 17 GB. It must include the `adapter_vllm/` folder: the container loads JEV's decision adapter from
it. If the download stops with a 401 or 403 error, the model needs a Hugging Face token: ask the course staff.

### JEV: start it and check it

```powershell
docker compose -f prototype/docker/compose.yaml up -d
docker compose -f prototype/docker/compose.yaml ps
```

Start-up takes 5–8 minutes. Run the `ps` command again until the `jev` service shows **(healthy)**, then check that it
answers:

```powershell
curl.exe http://localhost:8000/v1/models
```

The answer lists `autotrust/JEV-9B` and `jev-decision`. Finally, in Unity, open *Window ▸ EvoSim ▸ Brain Health* and
press **Test connection** next to JEV. If the service never becomes healthy, read its log with
`docker compose -f prototype/docker/compose.yaml logs jev`; a "not found" or "offline" error means the weights are
missing or at another revision. Stop it with `docker compose -f prototype/docker/compose.yaml down`.

### Ollama: the mutator

Quit the Ollama tray app if it is running (it starts its own server with other settings), then start the server with one
model loaded at a time, kept in memory for four hours:

```powershell
$env:OLLAMA_MAX_LOADED_MODELS = "1"; $env:OLLAMA_KEEP_ALIVE = "4h"; ollama serve
```

In a second window, once: `ollama pull gemma4:26b`. Without `OLLAMA_KEEP_ALIVE`, Ollama unloads the model after a few
idle minutes, and reloading 26 B parameters from disk can take longer than the mutator's 60 s time-out: that mutation then
fails and the gene is copied unchanged. Check it in *Brain Health* with **Test connection** next to the mutator.

> [!NOTE]
> `prototype/docker/README.md` describes the servers for the Python prototype, whose mutator is a different model. For
> EvoSim, follow this page.

## 1. Open a scene

The reference scenes are in `Assets/EvoSim/Scenes`:

| Scene | What lives there |
|---|---|
| `Lab1_Small` | a 48 m world with prey and predators: the quickest to watch |
| `Lab1_Full` | the full Lab 1 ecology (164 animals at the start, up to about 340) |
| `HideVsFlee` | prey that can flee or hide in thickets, and the predators that hunt them |
| `ThreeSpecies` | three species in one food web |
| `Terrain_Locomotion` | a Unity Terrain with slopes, water and trees |
| `Sandbox` | grazers only, with the random brain: a place to try your own modules |

## 2. Choose a brain

The scenes ask JEV by default. If JEV is not running, select the `World` object and set **Default Brain** to
`Brains/Random`, the null model that picks actions uniformly at random. Everything else works the same, and the run
still repeats exactly with its seed.

Without Ollama, mutations fail quietly: the gene is copied unchanged (MUT-04). Tick **No Mutation** on the World to
switch mutation off altogether.

## 3. Press Play

Before Play mode starts, the World's whole tree is checked. If something is wrong, the **play-mode gate** cancels Play
and the Console shows one line per error, naming the object to fix (for example *V-04 … Species 'prey' has no action*).
Click the line to select that object. Fix it by hand, or select the World, press **Validate** in its inspector and
click the fix button next to the message, then press Play again. Warnings never block Play.

While it runs:

- The **panel** at the top left shows the tick, the pace in ticks per second, and for each species its living animals,
  births, deaths and kills. It also counts mutations and model calls. **Tab** hides it.
- The **ball above each animal** shows its current action: green *eat*, yellow *flee*, blue *hide*, cyan *follow*,
  grey *rest*, pink *mate*, red *hunt*, white *searching* (nothing in sight).
- **Camera**: WASD or the arrow keys pan, the mouse wheel zooms, a right-drag or Q/E orbits, a middle-drag pans, **F**
  frames the whole ground. **Click an animal** to follow it and see its stats; **Esc** stops following.

When the brain is slow, the World waits for its answers before going on (a decision is never made on old observations).
The panel then says what it waits for, for example *waiting for JEV: 6 queries*.

## 4. Inspect

- Select the **World**: its state and tick, the run controls (Play, Pause, Step, Run N, Stop), its phases in order, its
  services, and **Validate** with the validation messages.
- Select a **species** and press **Show the computed tables**: its actions, senses, genes, stats and traits, food and
  population, and the **Prompt preview**, the exact text its animals send to the brain.
- Open the windows under *Window ▸ EvoSim*:

| Window | Shows |
|---|---|
| Animal Inspector | the animal you click: stats, genes, last situation and the brain's probabilities |
| Ecology Monitor | populations over time; births, deaths by cause and action shares per species |
| Event Log | the events of the run as they happen, filtered by kind, species or animal |
| Food Web | who eats whom, and the threats each species flees |
| Gene Pool | the alleles alive in each species, how common they are, and where each came from |
| Brain Health | each brain and mutator: host, *Test connection*, calls, failures, latency; the answer cache |
| Ask the Brain | genes and a situation of your choice, and the answers of the brains you pick |
| Scenario Runner | the scenario assets (S00–S27): run their seeds headless and compare the hashes |

See [Editor tooling](editor-tooling.md) for every inspector, window and menu.

## 5. Find the run's files

Each run writes its files to `Logs/EvoSim/runs/<run name>/`:

| File | Content |
|---|---|
| `events.jsonl` | every event, one JSON object per line: births, deaths, kills, meals, mutations… |
| `stats.csv` | one row of statistics every few ticks: populations, deaths by cause, means |
| `alleles.jsonl` | every allele (gene variant) that appeared, with its text and parent |
| `final_population.json` | the animals alive at the end: id, generation, species and genome (allele ids) |
| `summary.json` | the run's totals, why it stopped, and the **events hash** that identifies it |
| `run_info.json` | the seed, the settings and the versions needed to replay it |

Two runs with the same seed, the same scene and the same brain answers have the same events hash. See
[Recording and outputs](recording-and-outputs.md) and [Determinism and replay](determinism.md).

## Next

- [How EvoSim works](overview.md): the whole loop on one page.
- [Extending EvoSim](../extending/index.md): add your own sense, action, gene, brain or phase.
- [The teaching path](../extending/exercises.md): rewrite a reference module from a stub.
