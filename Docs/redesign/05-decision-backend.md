# 05 — Decision backend & local inference

## Which model family is "J-Laya"? (TBC)

The brief mentioned a *"type-safe"* model family that *"takes in text,
classifies it, and makes decisions based on type"*, name heard as
"J-Laya". Candidates — **please confirm which one was meant**:

| Candidate | What it is | Fit |
|---|---|---|
| **GLiClass / GLiNER** (knowledgator / urchade) | Small encoder models: text + candidate labels → score per label in one pass. Zero-shot classification / extraction. | Very fast, CPU-friendly, ONNX → could run **inside Unity** (Inference Engine/Sentis). But **weak at following instructions** → high risk for R1 (genes ignored). Short context (~512 tokens). |
| **Constrained decoding on a small LLM** (llama.cpp GBNF/JSON-schema, Ollama structured outputs, Outlines, XGrammar) | Any small instruct LLM, output forced to match a type/enum/JSON schema. | Best instruction following; output guaranteed valid. Needs more compute. |
| **BAML** (BoundaryML) | "Type-safe prompting" framework: typed functions over LLM calls, generated clients. | Nice dev ergonomics; it's a layer on top of a model, not a model. C# client support to check. |

Whichever it is, the interface is the same: **(genome, observation) →
distribution over a closed action enum**.

## Recommended default stack

- **Model:** a small instruct model in the 0.5B–3B range, GGUF quantised
  (Q4/Q5). Try several; pick by the gene-sensitivity test, not by
  benchmarks.
- **Runtime:** `llama.cpp` server (`llama-server`) or Ollama, running as a
  separate local process; Unity talks HTTP. Pros: cross-platform, GPU if
  present, CPU otherwise, students can swap models without touching Unity.
  (An in-process option such as the LLMUnity package or Unity's Inference
  Engine can be an advanced track.)
- **Output:** grammar/schema-constrained to the enum; request
  **log-probabilities** over the action tokens and sample in C# with the
  sim's seeded RNG (deterministic, and graded behaviour).
- **Prompt layout:** `[system instructions][genome — fixed per agent][observation][answer:]`
  — genome as a cached prefix per agent (one server slot / cached prompt
  per agent or per genome hash).

## Performance budget (to be measured, not assumed)

| Quantity | Target |
|---|---|
| Population | 20–60 (option A), 100–500 (option B) |
| Decision rate | 0.5–2 Hz sim time per agent |
| Observation suffix | ≤ 60 tokens |
| Generated tokens per decision | 1 (enum) |
| Genome prefix | ≤ 600 tokens, processed once per agent (A) or once per birth (B) |

Plan to benchmark on (1) a typical student laptop CPU, (2) a mid-range GPU,
(3) the lab server if one exists.

## Mutation model

Can be the same model with a different prompt. Mutation is rare, so it is
cheap; run it asynchronously and let the child spawn once the mutated gene
returns (or spawn with the parent allele and swap in — simpler: wait).
