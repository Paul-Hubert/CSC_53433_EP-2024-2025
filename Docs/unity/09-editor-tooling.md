# 09 — Editor tooling

Everything that **validates, manages or visualises** lives in the
`GeneticAgents.Editor` assembly (and `StudentWork.Editor` for students).
Runtime code exposes data (`Validate`, `DebugInfo`, events); editor code
decides how to show it. Nothing here runs in a build.

## 1. Validation framework

### Types (in Core, so batch runs can use them too)

```csharp
public enum Severity { Info, Warning, Error }

public sealed class ValidationIssue
{
    public Severity Severity { get; }
    public UnityEngine.Object Context { get; }   // pinged / selected when clicked
    public string Message { get; }               // what is wrong
    public string Why { get; }                   // why it matters (course concept)
    public (string label, Action apply)? Fix { get; }   // optional one-click fix
}

public sealed class ValidationReport
{
    public void Error(Object ctx, string message, string why = null, (string, Action)? fix = null);
    public void Warning(Object ctx, string message, string why = null, (string, Action)? fix = null);
    public void Info(Object ctx, string message);
    public IReadOnlyList<ValidationIssue> Issues { get; }
    public bool HasErrors { get; }
}

public interface IValidatable { void Validate(ValidationReport report); }   // local checks
```

### Two levels of checks

| Level | Lives | Checks | Example |
|---|---|---|---|
| **Local** | `Validate()` on the component/asset itself | its own fields | `FoodSense`: near ≤ vision |
| **Cross-object rules** | Editor assembly, one class per rule | relationships between assets/components | every action has a locus and vice versa |

```csharp
// Editor/Validation/Rules/LociMatchActionsRule.cs
[ValidationRule("Genetics")]
public sealed class LociMatchActionsRule : ValidationRule<SpeciesDefinition>
{
    protected override void Check(SpeciesDefinition s, ValidationReport r)
    {
        var actionLoci = s.genome.loci.Where(l => l.kind == LocusKind.Action).Select(l => l.id).ToHashSet();
        var actions = s.prefab.GetComponentsInChildren<AgentAction>(true);

        foreach (var a in actions.Where(a => a.Locus == null || !actionLoci.Contains(a.ActionId)))
            r.Error(a, $"{a.GetType().Name} has no Action locus in {s.genome.name}.",
                    why: "Each behaviour needs a gene describing when to do it (homologous loci).",
                    fix: ("Create locus and add it to the schema", () => LocusFactory.CreateFor(a, s.genome)));

        foreach (var id in actionLoci.Except(actions.Select(a => a.ActionId)))
            r.Error(s.genome, $"Locus '{id}' has no matching action on {s.prefab.name}.",
                    why: "The gene would be inherited and mutated but could never be expressed.");
    }
}
```

Rules are discovered with `TypeCache.GetTypesWithAttribute<ValidationRuleAttribute>()`,
so a student's rule in `StudentWork.Editor` runs everywhere automatically.
Fixes are applied with `Undo.RecordObject` + `EditorUtility.SetDirty`, so
they can be undone.

### When validation runs

| Moment | Behaviour |
|---|---|
| Inspector | issues for the selected object shown as help boxes at the top, refreshed on change |
| Entering Play | all assets used by the scene's runner are validated; **errors block Play** with a dialog → "Open Simulation Doctor" |
| Saving assets | quick local checks; console summary |
| CLI / batch | full validation before the first tick; errors → non-zero exit code |
| Tests | `AllSamplesAreValid` EditMode test keeps the shipped samples green |

### Built-in rules (initial set)

| Rule | Severity | Catches |
|---|---|---|
| `LociMatchActionsRule` | Error | action ↔ locus mismatch |
| `UniqueLocusIdsRule` | Error | duplicate locus ids in a schema |
| `FounderPoolCoversSchemaRule` | Error | a locus without neutral or founder alleles |
| `GeneLengthRule` | Warning | founder alleles longer than `maxWords` |
| `FrozenPoolChangedRule` | Error | a frozen founder pool edited (change = new version) |
| `PhraseBookCoverageRule` | Error | a sense value without a phrase (`PhraseRenderer` only) |
| `ObservationSpaceRule` | Warning | observation space > 5 000 situations (cache hit rate will collapse) |
| `PromptPlaceholdersRule` | Error | template missing `{genes}`, `{situation}` or `{ask}`; unknown placeholders |
| `DecisionChainRule` | Error | empty slot, decorator without inner, cycle, no terminal backend |
| `ShuffleOutsideMemoRule` | Error | `GenomeShuffleDecorator` inside a memo/cache (control would be wrong) |
| `OllamaConfiguredRule` | Error / Warning | no model set; host unreachable (warning, checked on demand); key env var missing for a cloud host |
| `NoSecretsInAssetsRule` | Error | something that looks like an API key in any serialized field |
| `FallbackActionRule` | Error | species fallback id is not one of its actions |
| `SpeciesRelationsRule` | Warning | a species with `PredatorSense` but no predator in the relations |
| `LocomotionMatchesWorldRule` | Error | `GridLocomotion` in a terrain world, or vice versa |
| `NoUpdateInSimulationComponentsRule` | Warning | an `AgentComponent` subclass declaring `Update`/`FixedUpdate` |
| `PooledStateResetRule` | Warning | private non-serialized fields in an `AgentComponent` not reset in `OnSpawned` |
| `MutationOperatorsRule` | Error | weights ≤ 0 everywhere; no operator can mutate some locus kind |
| `SystemsPipelineRule` | Error | missing `DecisionSystem`/`ActionSystem`; two systems claiming the same responsibility |
| `ExperimentConditionsRule` | Error | duplicate condition ids; modifier targets a missing part |

## 2. Inspectors and drawers

| Tool | Shows | Why it helps |
|---|---|---|
| `SubclassSelectorDrawer` for `[SerializeReference, SubclassSelector]` | dropdown of **every class implementing the field's interface**, grouped by `[Category]`, with a summary line and an "Open script" button | Students' classes appear automatically; no registration |
| `DecisionChainDrawer` | the decorator chain as nested boxes with arrows and status badges (cache size, hit rate in Play) | Caching, fallback and controls are visible structure |
| `ConditionDrawer` | `Describe()` as a sentence above the tree: *adult AND energy > 50 AND chose mate* | Rules read like the lab handout |
| `GeneTextDrawer` (`[GeneText]`) | word-count bar vs `maxWords`; highlights words the rule-based backend understands | Teaches what "expressible" genes look like |
| `SpeciesDefinitionEditor` | tabs Body / Genetics / Brain / Evolution; **genome contract table** (locus ↔ action component ↔ founder alleles ✓/✗); observation-space size; rough LLM-calls estimate | One page answers "is this species complete?" |
| `FounderPoolEditor` | grid per locus, freeze + new version, import/export prototype JSON, "Draft with LLM" (drafts land in a review list, never directly in the pool) | Keeps the common origin deliberate |
| `SimulationProfileEditor` | systems grouped by phase (reorder only within a phase), species list, "Validate", "Play with this profile" | The tick order is the documentation |
| `AgentRoot` inspector (Play mode) | gene texts, energy bar, current action, last distribution bars, parents (clickable) | Inspect any agent mid-run |

Drawers use UI Toolkit (`CreatePropertyGUI`) on Unity 6; IMGUI fallbacks
on 2021.3.

## 3. Windows (`Window ▸ Genetic Agents ▸ …`)

| Window | Purpose | Main views |
|---|---|---|
| **Simulation Doctor** | all validation issues in one list | filter by severity/category; click → select object; "Fix" buttons; "Re-run" |
| **Brain Debugger** | why did *this* agent do *that*? | observation fields; rendered text; the exact prompt; raw model answer; distribution chart; sampled action; source (memo / disk cache / model / fallback); timings; **What-if**: edit a gene or a field and re-ask through the same chain (cache bypass optional) |
| **Genome Browser** | what evolved? | per-locus allele table (text, origin, operator, parent, frequency, carriers' mean offspring); allele-frequency stacked areas over time; lineage tree of an agent with gene diffs at each mutation; diff two genomes; works live or on a run folder (Python runs too) |
| **Gene Playground** | do genes steer the brain? (E1 in the editor) | choose chain + observation set + genomes (founders, contrast pairs, controls) → heatmap p(action \| obs, genome); ΔP, sign accuracy, MI_G, MI_O, with the G1/G2 thresholds marked pass/fail |
| **Population Dashboard** | live health of the run | population per species, births / deaths by cause, mean energy & generation, action shares, invalid rate, per-locus diversity, memo hit rate, model seconds |
| **Experiment Runner** | run E4-style matrices | pick an `ExperimentDefinition` → validate → run `conditions × seeds` in a background batch process; progress; open output folders |
| **New Feature Wizard** | scaffold an extension correctly | "New action": script from template + `LocusDefinition` + schema entry + neutral founder allele + component on the prefab, then opens the Doctor to show what is left (write alleles, phrases) |

Settings live in **Project Settings ▸ Genetic Agents ▸ Ollama** but are
stored under `UserSettings/` (not committed): host, default model, name of
the API-key environment variable. "Test connection" lists installed
models and digests; the key shows as *set ✓ / not set*, never its value.

## 4. Scene view

- **Overlay toolbar "Genetic Agents"** (Scene-view Overlay API): play /
  pause / step, ticks-per-second slider, tick counter, and layer toggles.
- **Layers** (each a separate drawer class):

| Layer | Draws |
|---|---|
| Walkability | water / too steep / walkable tint over the terrain or grid |
| Food | food cells, regrowth bonus area near water |
| Perception | for the selected agent: near and vision rings of each sense |
| Intent | current action label and a line to its target (`ActionDebugInfo`) |
| Lineage colours | agents coloured by allele at a chosen locus, with a legend |
| Threats | predators' chase radius; recent death markers fading by cause |

```csharp
// StudentWork/Editor/ThirstSenseGizmos.cs — visualiser for a student's sense
static class ThirstSenseGizmos
{
    [DrawGizmo(GizmoType.Selected | GizmoType.Active)]
    static void Draw(ThirstSense sense, GizmoType type)
    {
        if (!GeneticAgentsLayers.IsOn("Perception")) return;
        Handles.color = new Color(0.2f, 0.5f, 1f, 0.6f);
        Handles.DrawWireDisc(sense.transform.position, Vector3.up, sense.WaterSearchRadius);
        Handles.Label(sense.transform.position + Vector3.up * 2, $"thirst: {sense.LastValue}");
    }
}
```

Runtime components never contain `OnDrawGizmos`; they expose read-only
debug data and the editor draws it. Clicking an agent in the Scene view
focuses the Brain Debugger on it.

## 5. Script templates

`Assets ▸ Create ▸ Genetic Agents ▸ Script ▸ …` creates a ready-to-edit
file from `Editor/ScriptTemplates/*.cs.txt` (via
`ProjectWindowUtil.CreateScriptAssetFromTemplateFile`):

`Sense` · `Action` · `Locomotion` · `Decision Backend` · `Decision Decorator` ·
`Mutation Operator` · `Gene Guard` · `Agent Condition` · `Simulation System` ·
`Profile Modifier` · `Validation Rule` · `Gizmo Drawer` · `Editor Window`

Each template contains the base class, the attributes, a `Validate`
stub, a `TODO` per method and a link to the matching recipe in
[10](10-extension-recipes.md).

## 6. Menu map

```text
Window ▸ Genetic Agents ▸ Simulation Doctor | Brain Debugger | Genome Browser |
                          Gene Playground | Population Dashboard | Experiment Runner | New Feature Wizard
Assets ▸ Create ▸ Genetic Agents ▸ Simulation Profile | Species | Genome Schema | Locus | Founder Pool |
                                    Prompt Template | Phrase Book | Species Relations | World (Grid/Terrain) |
                                    Experiment | Script ▸ (templates above)
Tools ▸ Genetic Agents ▸ Validate All | Export Founder Pool JSON | Import Prototype Run… | Clear Response Cache…
Project Settings ▸ Genetic Agents ▸ Ollama | Recording
```
