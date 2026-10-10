# 15 — Controls, metrics and gates

The system exists to show that behaviour *evolves* and that the genes *mean*
something. Controls remove one ingredient at a time; metrics measure how much
genes steer a brain; gates are pass/fail criteria fixed in advance. They are part
of the contract because the tests ([30](30-tests.md)) and the integrity checks
([32](32-integrity-prompts-and-ci.md)) depend on them.

## 1. Controls

- **CTRL-01 (MUST)** Each control is a setting that applies to every species and
  is recorded in `run_info.json`:

  | Control | Setting | Question it answers |
  |---|---|---|
  | C1 FULL | nothing changed | the experiment |
  | C2 NO-MUT | mutation rate 0 (or no mutator) | how far does selection get with founder variation alone? |
  | C3 SHUFFLED | at each decision the brain reads the genes of a random *other* living animal of the same species; number genes are expressed from a random other animal's genome at birth | genes are inherited but don't affect their carrier, so any change is drift |
  | C4 RANDOM-FOUNDERS | founders get control sentences (text genes) and uniform random values in range (number genes) | can evolution climb out of nonsense? |
  | C5 KEYWORD | the keyword brain instead of an LLM (prototype only: the Unity system has no keyword brain; a student's rule-based brain could fill this slot) | the same experiment with a transparent brain |
  | NULL | the random brain | does behaviour matter at all? |
  | C7 ASEXUAL | one parent, copy and mutation | the previous lab's regime |

- **CTRL-02 (MUST)** The C3 draw uses the species' sampling stream, so a shuffled
  run is reproducible.

## 2. Sensitivity metrics

P[g, o, a] is the probability of action a for genome g in observation o;
information is in bits.

| Metric | Definition | Question |
|---|---|---|
| Entropy H(p) | −Σ p log₂ p | how uncertain is a decision? |
| **MI_G** | mean over o of [H(mean over g of p) − mean over g of H(p)] | how much does the genome decide the action? |
| **MI_O** | the same with genome and observation swapped | how much does the situation decide the action? |
| JSD(p, q) | H((p+q)/2) − ½(H(p) + H(q)), in [0, 1] | how different are two decisions? |
| Behaviour distance d(g, g′) | mean over o of JSD | how differently do two genomes behave? |
| **Directed ΔP** | for a contrast pair at a locus: mean over the *relevant* observations of p(action \| pro) − p(action \| anti); **sign accuracy** = share with ΔP > 0 | does a gene push its own action the way it says? |
| **Locality ratio** | median d(parent, child with one gene mutated) ÷ median d(two unrelated founders) | are small edits small changes? |
| **Spearman ρ** | rank correlation between text distance (1 − similarity) and behaviour distance | do more different texts behave more differently? |
| Gibberish → neutral | mean d(random-text genome, neutral genome), compared with founder → neutral | does nonsense stay close to "no preference"? |
| Shannon diversity | entropy of a locus' allele frequencies | how diverse is a gene? |
| Bootstrap CI | resampled confidence interval of a mean | is a difference real? |

- **CTRL-10 (SHOULD)** Each action declares when an observation is **relevant**
  for its directed test: eat needs food in sight, flee a threat, follow kin,
  mate kin in the near bands, hunt prey in sight, hide cover in sight, rest any
  observation.

**Test material.** An observation set of 48 situations (32 adult combinations
of energy × food × threat × kin, plus the 16 most frequent other situations of a
reference run); founder genomes drawn from the pools; random-text genomes
from the control sentences; the all-neutral genome; one contrast pair per locus;
single edits (founders with one gene mutated by the mutator).

## 3. Gates

| Gate | Pass criterion | Failure means |
|---|---|---|
| **G1 Semantics** | directed sign accuracy ≥ 85 % and mean ΔP ≥ 0.25 | the brain ignores genes |
| **G2 Information** | MI_G ≥ 0.25 bits and MI_O ≥ 0.25 bits, and gibberish → neutral ≤ ½ × founder → neutral (quick form: MI_G(founders) ≥ 2 × MI_G(random text)) | behaviour follows any text, not meaning |
| **G3 Locality** | locality ratio ≤ 0.5 and Spearman ρ ≥ 0.3 | a chaotic map from text to behaviour |
| **G4 Evolution** | in a common garden, evolved genomes outlive the founders (bootstrap 95 % CI of the difference > 0) in ≥ 4 of 5 seeds, and gain more than in the C3 control | "evolution" that is only drift |
| **G5 Throughput** | ≥ 50 effective decisions/s on a GPU and ≥ 10 on a CPU, at 40 animals, cache included | a lab too slow to run |

**Status in the prototype (2026-10-09).** G1 passes (gemma4 12b and 26b, JEV-9B).
G2 fails: gemma is moved by random text as much as by real genes; JEV ignores
random text but genes move it too little (MI_G 0.14 < 0.25). G2 stays a gate even
though these models fail it (owner decision); it is tracked, not blocking
([32](32-integrity-prompts-and-ci.md)). G3 passes on JEV
(locality 0.10, ρ 0.39). G4 not measured; a 10-hour JEV run showed gene sweeps at
the rate drift predicts. G5 fails (gemma ≈ 4, JEV ≈ 12 decisions/s). In the
prototype the keyword brain passed G1 by construction and gave the reference
numbers; in Unity the reference numbers come from the first accepted JEV runs.

## 4. The evolution experiment

The matrix the prototype planned and the Unity system should make easy to run:
C1–C5 (and C7) × several seeds × one world, each run long enough for tens of
generations, then a **common garden**: evolved genomes and founder genomes
released together into a fresh world, comparing their survival and offspring
(G4). The reference world for it is the 192 × 192 evolution-test world, where
most deaths depend on behaviour (prey: 39 % killed, 38 % starved, 20–23 %
migrated with JEV).
