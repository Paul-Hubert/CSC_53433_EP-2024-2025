# 04 — Genome & evolution

## Genome structure

> **Update (Laya):** with Laya, the natural loci are the **action options**:
> each action's criteria text *is* a gene (see
> [05 §4](05-decision-backend.md#4-where-the-genes-go-mapping-the-genome-onto-layas-input)),
> plus a few **temperament genes** placed in the state. Laya's short
> context means genes should be ≈ 8–15 words. The table below is being
> re-cut along those lines. Roles such as hunger → `eat` and fear → `flee`
> map directly; terrain/risk/attention become temperament genes.

- **Fixed set of loci** (proposal: 12), each with a semantic role and a
  fixed position in the prompt. A gene = one short sentence (≤ 25 words).
- Optional **free loci** (2–4) with no assigned role for open-ended content.
- Each gene carries metadata: `alleleId`, `parentAlleleId`, `origin`
  (founder / mutation operator), `birthTick`.

| Locus | Role | Example founder alleles |
|---|---|---|
| L1 | Hunger | "Eat whenever food is close." / "Only look for food when energy is low." |
| L2 | Fear | "Run from anything larger than you." / "Ignore others unless they attack." |
| L3 | Exploration | "Keep moving to new places." / "Stay near where you last found food." |
| L4 | Social | "Stay close to others of your kind." / "Keep your distance from everyone." |
| L5 | Aggression | "Attack weak animals near you." / "Never fight." |
| L6 | Mating | "Look for a partner when energy is high." / "Mate with any nearby adult." |
| L7 | Terrain | "Avoid steep slopes." / "Stay near water edges." |
| L8 | Energy economy | "Rest when tired." / "Never stop moving." |
| L9 | Attention | "Look around often." / "Focus straight ahead." |
| L10 | Risk | "Take risks when starving." / "Always play it safe." |
| L11–12 | Free | — |

*(Loci and alleles are placeholders for discussion.)*

## Founder pool

- For each locus, **3–6 hand-reviewed alleles** of basic-instinct flavour.
  They may be LLM-drafted but must be **frozen, versioned, and committed**
  (`founder_pool_v1.json`) so every run starts identically.
- Initial population: each agent samples one allele per locus from the pool
  with a seeded RNG → diverse but meaningful starting genomes.
- Include one **neutral allele** per locus ("No preference.") so evolution
  can "switch off" a drive.

## Reproduction

- **Sexual:** two agents, both above an energy threshold, within range,
  both choosing `Mate` (or one choosing and the other accepting). Parents
  pay an energy cost. Child spawns nearby.
- **Crossover:** uniform per locus (50/50 for each locus). Alternatives for
  students: one-point, locus-block, "dominant/recessive" (keep both alleles,
  express one).
- Keep a flag for **asexual mode** to reproduce the old lab and compare.

## Mutation

Per child, per locus, probability `pMut` (≈1–5 %). Operator chosen from a
weighted menu:

| Operator | Uses LLM | Notes |
|---|---|---|
| `LlmRewrite(style)` | yes | style ∈ {random change, invert, exaggerate, soften, add condition, specialise, generalise}. No fitness context given. |
| `WordSwap` | no | replace one content word from a small lexicon. |
| `Negate` | no | toggle "always/never", "avoid/seek". |
| `Intensity` | no | "sometimes" ↔ "often" ↔ "always". |
| `FounderReintroduce` | no | replace with a random founder allele of the same locus (immigration). |
| `Duplicate/Delete` (free loci only) | no | structural variation. |

Guard-rails: length cap; reject empty/non-sentence output; strip
meta-text ("Here is the modified prompt:"); seeded sampling; **every
mutation cached and logged** so identical `(gene, operator, seed)` yields
identical output across runs.

## Selection

- **Implicit** (ecological): survive, eat, mate. No explicit fitness
  function drives reproduction.
- **Measured** (for analysis only): lifespan, offspring count, energy
  gathered, per-allele frequency and mean offspring of carriers.
- Population floor respawns from the founder pool (or optional hall of fame).

## What students get at the end of a run

- Allele frequency plots per locus over time.
- Lineage tree with gene diffs at each mutation.
- "Top surviving genes" table and final dominant genome(s).
- Comparison across seeds from the same founder pool.
