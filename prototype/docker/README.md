# Model servers

| what | where | model | used by | config |
|---|---|---|---|---|
| `jev` (vLLM, Docker, GPU) | :8000 | autotrust/JEV-9B in FP8, System 1 LoRA `jev-decision` | `--backend jev` (decisions) | `jev.host` |
| mutator (Ollama, CPU) | :11434 | qwen3.5:0.8b, `num_gpu: 0` | gene mutation | `mutator.*` (`api: ollama`) |
| `mutator` (vLLM, optional) | :8001 | Qwen/Qwen3.5-0.8B in FP8 | gene mutation | `mutator.api: openai`, `mutator.host` |

The addresses are independent: point `jev.host` or `mutator.host` in `configs/base.yaml` at any
machine. On the 16 GB RTX 5080 JEV in FP8 fills the GPU (14.4 GB in use), so the default mutator
runs on the CPU in Ollama (`ollama pull qwen3.5:0.8b`; ≈ 0.3-0.8 s per mutation, mutations are rare).
The vLLM mutator service (`--profile gpu-mutator`) is for a card with more memory, or with
`JEV_QUANT=bitsandbytes` (4-bit JEV, less accurate).

## Start / stop

```bash
docker compose -f docker/compose.yaml up -d        # JEV only (5-8 min until healthy)
docker compose -f docker/compose.yaml ps           # "healthy" = ready
python -m experiments.jev_check                    # speed, contrast genes, mutator
docker compose -f docker/compose.yaml logs -f jev
docker compose -f docker/compose.yaml down
```

Weights come from the Windows Hugging Face cache (`%USERPROFILE%\.cache\huggingface`); the
containers run offline, so download once (pinned revisions, as in compose.yaml):

```bash
.venv/Scripts/python -c "from huggingface_hub import snapshot_download as d; \
d('autotrust/JEV-9B', revision='b63f651ce8ed64481d3f5e73ecdb05f740042f01', \
  allow_patterns=['*.json', '*.jinja', 'model-*.safetensors', 'head.safetensors', 'adapter/*', 'adapter_vllm/*']); \
d('Qwen/Qwen3.5-0.8B', revision='2fc06364715b967f1860aea9cf38778875588b17')"
```

JEV start-up takes ≈ 8 min: ≈ 4 min reading 17 GB through Docker's Windows file share, ≈ 2.5 min
CUDA-graph capture.

## Memory (RTX 5080, 16 GB)

JEV FP8 weights take 10.5 GiB. vLLM's own estimate of the room left for its cache came out
negative at any `--gpu-memory-utilization` (it counts Windows' ≈ 1.3 GiB and a 2 GiB activation
peak), although ≈ 1.9 GiB stayed free; so the cache size is set directly: `JEV_KV` (default
`768M`: 5 888 tokens, 5.75 full 1 024-token requests at once). If start-up fails with
"out of memory", close GPU-heavy apps or lower `JEV_KV`.

## Check by hand

```bash
curl -s localhost:8000/v1/models
curl -s localhost:8001/v1/chat/completions -H 'content-type: application/json' \
  -d '{"model":"Qwen/Qwen3.5-0.8B","messages":[{"role":"user","content":"Say hi"}],"max_tokens":10,
       "chat_template_kwargs":{"enable_thinking":false}}'
```
