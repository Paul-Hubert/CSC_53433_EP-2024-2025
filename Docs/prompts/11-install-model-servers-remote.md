Install the **EvoSim model servers from scratch on a new machine** — JEV (vLLM in Docker, port 8000) and the Ollama
mutator (port 11434) — so that **other machines on the network can use them**, then write down every step that broke.
You are testing the instructions, not just getting it to work: a step you had to fix is the result.

## Settings

| Setting | Value |
|---|---|
| Machine | The new machine you run on: Windows 11, an NVIDIA GPU with about 16 GB, nothing of EvoSim installed. Say so if it isn't. |
| Client | A second machine on the same network with the repository and Unity (the owner's PC), or this machine's LAN IP if none. |
| Branch | `llm-evolution`. Clone it fresh (`git clone … ; git checkout llm-evolution`); never force-push. |
| Ports | JEV `8000`, Ollama `11434`, both reachable from the client's subnet only. |
| Output | The report `Docs/Setup/remote-model-servers-report.md` (replace the dry-run sections with what you saw), and fixes to `Tools/Install-ModelServers.ps1`. |
| Commit | `EvoSim setup: model servers installed from scratch on <machine>`, with the attribution lines your session requires; push (pull first). |

## Steps

Do **phase A by hand first, without the script**: the point is to find what the written instructions miss.

### Phase A — follow the documentation literally

1. Record the machine: Windows build, GPU and driver (`nvidia-smi`), RAM, free disk, what is already installed
   (Docker, WSL, Python, Ollama, Git). Don't install anything yet.
2. Follow `Docs/Site/guides/getting-started.md` § *Model servers (for L1 and L2)* **exactly as written**, top to
   bottom, in PowerShell from the repository root. Install each prerequisite it names the way a student would
   (winget or the vendor installer) and note which ones the page never told you to install.
3. For every command, log: the command, how long it took, and its result (ok / failed + the first error line).
   When a step fails, **stop, record it, then fix it** with the smallest change, and record the fix. Don't skip ahead.
4. Make both servers listen on the network: JEV is published by Docker on all interfaces; Ollama needs
   `OLLAMA_HOST=0.0.0.0:11434` (the page doesn't say so). Open the Windows firewall for the two ports, client subnet
   only, and record whether Windows showed a prompt instead.
5. From the client, check both ports: `curl.exe http://<server>:8000/v1/models` (lists `autotrust/JEV-9B` and
   `jev-decision`) and `curl.exe http://<server>:11434/api/tags` (lists `gemma4:26b`). Then in Unity on the client,
   *Window ▸ EvoSim ▸ Brain Health* ▸ **Test connection** for JEV and the mutator. The scenes' hosts are
   `localhost`: record what you had to do to point them at the server.
6. Run one short L2 run on the client against the server (`Lab1_Small`, 200 ticks) and note the pace and any
   time-outs in Brain Health.

### Phase B — the script

7. Undo phase A on the server where it is cheap (stop the container, quit Ollama; keep the downloaded weights and
   image), then run `Tools/Install-ModelServers.ps1 -CheckOnly`, then the full script (and `-Client` on the client).
   Compare with phase A: every break of phase A must be handled or clearly reported by the script.
8. Fix the script where it is wrong or misses a break. It must stay Windows PowerShell 5.1 compatible and ASCII only.

## The report

Rewrite `Docs/Setup/remote-model-servers-report.md`:

1. **The machine** (one table) and the client.
2. **Timeline**: one row per step: step, command, minutes, result.
3. **What broke**: one section per break, numbered, with the error text, the cause, the fix you used, and the fix
   the repository should take (a doc line, a compose change, code). Keep the dry-run breaks that you confirmed, mark
   those you could not reproduce, add new ones.
4. **Not checked**, and why.

## Rules

1. **Evidence only.** Each break quotes what you saw; no guessed causes stated as fact.
2. Don't change the pinned model revision or the vLLM build (`compose.yaml` pins it by digest).
3. Don't touch `Assets/` or the scenes; changes to Unity code are proposals in the report.
4. No secrets in the repository: if a Hugging Face token turns out to be needed, it stays in the environment.
5. Never expose the ports beyond the local subnet; the servers have no authentication.

## Hand over

Tell the owner, in 8 lines at most: whether the client ran L2 against the server, the breaks in order of how much
time they cost, what the script now handles, and what still needs a change in the repository.
