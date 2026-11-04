# Model servers on another machine: what breaks

**Status: dry run, 2026-11-04.** These checks ran on the owner's PC (RTX 5080, Windows 11 26200, Docker 29.9.2,
Ollama 0.35.1), with each step of a fresh install tested in isolation: anonymous Hugging Face access, pulling the
image from Docker Hub, the compose file without a Windows profile, Ollama bound to the LAN, LAN-address requests. No
second, clean machine was used yet. The real run is `Docs/prompts/11-install-model-servers-remote.md`. It replaces
this file with what it saw. The script is `Tools/Install-ModelServers.ps1`.

The setup under test: JEV (vLLM in Docker, `prototype/docker/compose.yaml`) on port **8000** and the Ollama mutator
(`gemma4:26b`) on port **11434**, used by Unity on other machines.

## What broke

### 1. The pinned vLLM image can't be pulled (blocks JEV on every new machine)

`compose.yaml` pins `vllm/vllm-openai:nightly-81198e97ba7eee2a22540caaa756b7fdddcb4d93`. Docker Hub has deleted that
nightly tag:

```
docker manifest inspect vllm/vllm-openai:nightly-81198e97…  ->  no such manifest
hub.docker.com/v2/repositories/vllm/vllm-openai/tags/nightly-81198e97…  ->  404
```

So `docker compose up` fails on any machine that doesn't already have the image. It still works here only because the
image is cached locally (33 GB). The **digest** can still be pulled:
`vllm/vllm-openai@sha256:a83c7f976879e0c6101f734fd5b84a098d12d6d8d12baa41e31eb323b84b0802`.

- **Fixed (2026-11-04):** `compose.yaml` now names the image by that digest (same build). The script pulls the same
  digest.

### 2. `py -3` doesn't exist, and `python` is the Store stub

The getting-started page says to use `py -3`. A Windows machine without the python.org installer has no `py`, and its
`python` / `python3` are `WindowsApps` aliases. They print *"Python was not found; run without arguments to install
from the Microsoft Store"*. Both failed on this PC, which has Python only through Miniconda. The page never says to
install Python.

- *Script:* finds a real Python 3.10 or later (py, python, python.org paths, Miniconda), skips the stub, and installs
  `huggingface_hub` in its own venv. Without a real Python it stops and prints `winget install -e --id Python.Python.3.12`.
- *Doc fix:* add Python to *What the machine needs* with the winget line.

### 3. Ollama only listens on 127.0.0.1 (other machines can't reach the mutator)

```
Get-NetTCPConnection -LocalPort 11434  ->  127.0.0.1
http://192.168.1.31:11434/  ->  Unable to connect to the remote server
```

The page's `ollama serve` command doesn't set `OLLAMA_HOST`. The tray app also starts its own server on 127.0.0.1.
With `OLLAMA_HOST=0.0.0.0:11435` (a test port, so the running server was left alone), the LAN address answered `200`.

- *Script:* stops the tray app and starts `ollama serve` with `OLLAMA_HOST=0.0.0.0:<port>`, `OLLAMA_KEEP_ALIVE=4h` and
  `OLLAMA_MAX_LOADED_MODELS=1`. With `-Persist` it saves them as user variables, so the tray app listens on the LAN
  too after the next sign-in.
- *Doc fix:* add `$env:OLLAMA_HOST = "0.0.0.0:11434"` to the serve line, for a server used by other machines.

### 4. Clients can't be pointed at another machine without editing scenes

The server address is saved per component: **33 `host:` fields** across the scenes and assets (22 ×
`http://localhost:11434`, 11 × `http://localhost:8000`). The player's command line (`PlayerArgs`: `-scene -seed -ticks
-brain -cache …`) has no option for a host. Using a remote server means editing each component or rebuilding the player.

- *Script:* `-Client -Server <ip>` (administrator) forwards `127.0.0.1` and `::1` ports 8000 and 11434 to the server
  with `netsh interface portproxy`, so scenes and players work unchanged. `-Client -Remove` undoes it. It refuses if a
  local Ollama or JEV already holds the port.
- *Repository fix (proposal):* one project-wide host override, such as an `EVOSIM_JEV_HOST` / `EVOSIM_MUTATOR_HOST`
  environment variable or `-jevhost` / `-mutatorhost` player arguments, read by `HttpBrain` and the mutator client.

### 5. The compose file assumes a Windows profile

The volume is `${USERPROFILE}/.cache/huggingface`. On a Linux or macOS server `USERPROFILE` is empty:
`docker compose config` resolves the source to `/.cache/huggingface`. The container then starts with no weights and
fails with "offline / not found".

- *Repository fix:* `${HF_CACHE:-${USERPROFILE}/.cache/huggingface}`, or a documented `.env` line for Linux.

### 6. `HF_HOME` sends the download somewhere the container doesn't look

`snapshot_download` follows `HF_HOME` (and `HF_HUB_CACHE`). The container always mounts `%USERPROFILE%\.cache\huggingface`.
On a machine where those variables point elsewhere, the download succeeds but JEV fails offline.

- *Script:* warns about `HF_HOME` and downloads into the mounted folder. It then checks that `adapter_vllm/` and the
  head and calibration files are there.

### 7. Smaller differences from the page

- The download is **18.3 GB in 47 files**, not "about 17 GB": `*.json` also matches the `vl/` sub-model's files.
- The model is **not gated** (`gated False`, anonymous access works). The page's "ask for a token on 401/403" is not
  needed today.
- The image (33 GB) is missing from the disk budget. A fresh server needs about 19 + 33 + 18 GB.
- The ports have no authentication, and Docker publishes 8000 on all interfaces. The firewall rule should be limited
  to the local subnet. The script's `-OpenFirewall` uses `-RemoteAddress LocalSubnet`.

### 8. Firewall: depends on a prompt someone has to accept

This PC has no rule for ports 8000 or 11434. It does have *program* rules for `ollama.exe` and *Docker Desktop
Backend*, for the **Public** profile only, created when Windows asked on first listen. The network profile is Public.
On a new machine the prompt may be dismissed, or the machine may be headless. Then other machines are blocked even
though the LAN-address check on the server itself passes: requests a machine sends to its own LAN address don't cross
its inbound firewall.

- *Script:* `-OpenFirewall` (administrator) adds port rules for every profile, local subnet only. Without it, the
  script warns.

## Verified working

- From the LAN address on the same machine: `GET /v1/models` lists `autotrust/JEV-9B` and `jev-decision`, and
  `POST /v1/completions` returns 200.
- `Install-ModelServers.ps1 -CheckOnly` on this PC: every prerequisite passes. It reports break 3 (Ollama on
  127.0.0.1) as a failure and the missing port rules as warnings. It parses under Windows PowerShell 5.1 and is ASCII only.

## Not checked (needs the clean machine)

- Installing WSL 2 and Docker Desktop, with the reboot, and first-run GPU passthrough on another driver or GPU.
- A GPU under 16 GB (`-Quant bitsandbytes`), the time to pull 33 GB, and JEV's first start on a slower disk.
- A real second machine crossing the firewall, `-Client` portproxy with Unity's `HttpClient` (IPv4 against `::1`
  for "localhost"), and an L2 run over the network.
- The script's full install path, `-InstallPrereqs`, and `-Persist`. Only `-CheckOnly` was run.
