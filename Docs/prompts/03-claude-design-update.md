Update the existing "Genes Are Prompts" design with the project changes below. Keep the visual concept, palette, typography, layout and every frame I don't mention. Use only the facts and numbers in this message. Where an earlier frame contradicts them, this message wins.

## 1. The lab is now Lab 1, on a flat world

- In the intro, title subtitle and overview poster: the animals start in **Lab 1 on a flat world with food scattered at random**. In the following labs, students build terrain and then vegetation, and those change the world the animals must adapt to. Remove any wording that says this lab comes after the terrain and vegetation labs.
- Add three short reasons for starting flat:
  1. One thing at a time: every difference in survival comes from behaviour.
  2. A baseline for the later labs: run the same founders in the new world and compare.
  3. No "good places" to find, only good decisions.

## 2. The world frame

- **Lab 1 map:** flat ground everywhere, no water or mountains. Food dots appear and regrow at random cells. A few predators chase animals that come within 6 cells. Animals spend energy to live and move, eat to regain it, and die at zero energy or of old age. If population numbers appear, a Lab 1 population settles around 25–28 animals with the keyword brain, limited by food. A random brain can't sustain itself: the population drops to the minimum of 10 and survives only on newcomers.
- **"Next labs" strip:**
  - Lab 1, flat world →
  - terrain lab: water and mountains become impassable; in the current preview, food grows twice as fast near water →
  - foliage lab: plants become food and cover.

  Show the same animals having to adapt at each step. Use blue water and grey-brown mountains only in this strip.
- If the design shows the simulation's text map, its legend is: E eat, F flee, L follow, W wander, R rest, M mate, A attack, ? not decided yet, P predator, . food.

## 3. The decision frame

- The brain is **Gemma 4 12B**, an open model running locally through Ollama on one desktop GPU, taking about 0.5 s per decision. Keep the real example (points total 90, normalised to flee ≈ 50 %, eat ≈ 28 %, rest ≈ 11 %, follow and wander ≈ 6 %).
- Add a small callout, **"Check the model's output"**: in 60 decisions, 73 % of the answers added up to exactly 100; 3 were all zeros, all for nonsense genes, and the code then turns them into a completely random animal.

## 4. The mutation frame

Mutation is now **blind and has one operator**. Replace the old operator cards (intensity, negate, condition swap, synonym, bring back a founder sentence, LLM rewrite styles) and the "Word edits are blunt" note with this:

- **How it works.** For each mutating gene, the code draws one instruction from a deck of 16 variants of "make a random change" and sends it to the LLM with the gene sentence, and nothing else. The LLM never sees the world, the other genes or what helps; selection alone decides what stays. Show it as a card drawn from a deck plus the gene, giving the mutant. Rate: each inherited gene has a 3 % chance to mutate, so about one child in four gets a mutation.
- **Real single mutations** (Gemma 4 12B, verbatim, with the instruction drawn):
  - "Change one random detail in this sentence." "Run from any predator you see." → "Run from any butterfly you see."
  - "Randomly add a word to this sentence." "Run from any predator you see." → "Run quickly from any predator you see."
  - "Randomly change the meaning of this sentence a lot." "Run from any predator you see." → "Run to any predator you see."
  - "Randomly remove a word from this sentence." "Never fight." → "Fight."
  - "Make one random change to this sentence." "Stay close to other animals." → "Stay far from other animals."
  - "Make an unexpected change to this sentence." "Stay close to other animals." → "Stay close to the microwave."
- **A small "Mutation without selection" strip.** One gene mutated again and again in a test, with no selection (real lineage):
  - "Rest when you are tired." → 1 "Run when you are tired." → 3 "Stop when you are hungry." → 9 "Stay, when are you toaster?" → 21 "Please, is the pizza ready?" → 30 "The dancing is a banana wave."
  - Caption: "Without selection, genes leave the animal's world after 10–15 mutations. The LLM's 'random' has favourite words: 'toaster' turned up in 18 of 24 test lineages. Selection is what keeps genes meaningful."
- **One honest number.** 99 % of mutations pass the form checks; the checks never look at meaning.

Keep the mutation explanation in one self-contained component.

## 5. The "what comes out of a run" frame

Keep the illustrative mock-ups and their "illustrative" labels. Replace the example questions with:

- "Which founder sentences win on a flat world?"
- "Do flee genes get stronger when we add predators?"
- "When the terrain lab adds water and food grows faster near it, will 'Prefers staying near water.' spread?"

## 6. The gates frame (scientific method)

Update the status badge to **"Status: October 2026"**:

| Gate | Status |
|---|---|
| G1 Meaning | ✔ behaviour moved the right way in 96–99 % of directed tests |
| G2 Signal vs noise | ✘ irrelevant sentences such as "Trains leave from the north platform." shift behaviour as much as real genes. Suspected causes: nonsense genes sometimes get all-zero answers (which become a random animal), and the control sentences contain world words like "mountains" and "bread". |
| G3 Locality | not measured yet |
| G4 Evolution | not measured yet |
| G5 Speed | ✘ for now: about 4 decisions per second on one 16 GB GPU, against 50 targeted |

Add one callout, **"First run with the LLM brain"**: over 500 ticks on the flat world, the LLM-read founders attacked 16 % of the time and fled only 5 %. The population fell to its minimum of 10, while the keyword brain kept 17 animals over the same ticks. Mark it as one seed only, a lead rather than a result, and pose it as the open question: **can evolution fix it?**

## 7. The "what students learn and build" frame

Replace the exercise list with the Lab 1 activities:

1. meet the ecology;
2. does the brain matter? (random vs keyword vs LLM);
3. read and write genes, then ask the brain directly;
4. watch evolution and write the analysis;
5. mutation: change the instruction deck or the temperature and measure what it does;
6. change only the wording of situations;
7. controls and the scientific method;
8. the open problem G2.

Add a small cost note: the keyword brain runs 5 000 ticks in about 12 s on a laptop, and the LLM brain takes about 7 minutes per 500 ticks on a 16 GB GPU.

## 8. Footer

Add to the overview poster and the last frame: "Full documentation: Docs/prompt-genome/ in the course repository."

After the changes, list each frame you modified with one line on what changed.
