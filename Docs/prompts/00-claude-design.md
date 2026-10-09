Design a visual explainer for a university teaching lab called **"Genes Are Prompts"**. In this simulation, each animal's genes are short English sentences. A local LLM reads the sentences to decide what the animal does, and evolution (mating, mutation, survival) reshapes the sentences over generations. It is **Lab 1** of a Master 2 course: the animals start on a flat world with food scattered at random. In the following labs, students build terrain and then vegetation, which reshape the world the animals have to adapt to.

The one idea to get across: **nobody programs the behaviour. It adapts to the world through evolution, and because the genes are plain sentences, students can read what evolved and why.**

## Audience and goal
- Main audience: Master 2 students in AI and computer graphics seeing the lab for the first time. They know what an LLM and a neural network are, but may not know evolutionary algorithms.
- Second audience: teaching staff deciding whether to adopt the lab.
- After five minutes with the design, a student should be able to explain: (1) what a gene is here, (2) how an animal decides, (3) how genes change between generations, (4) why the outcome is readable, and (5) what they will build and measure in the lab.

## Format
- A set of 16:9 frames (1920×1080) that work both as projected slides and as a scrolling page, plus one overview poster frame showing the whole loop on a single canvas.
- Body text at least 24 px at 1920 width so it reads when projected. One idea per frame. Diagrams carry the explanation and text labels them.

## Frames
1. **Title.** "Genes Are Prompts: evolving animal behaviour you can read." Hero image: a top-down view of the world with a few animals, each with a small card showing one of its gene sentences.

2. **Why redesign: before and after.**
   Before (current lab): the genome is the weights of a tiny neural network (eyes → 5 hidden neurons → 1 output). Its only output is a turn angle. Reproduction is an asexual copy plus noise, the result is a population count, and students cannot read why anything works.
   After: the genome is 10 short sentences, there are 7 behaviours and sexual reproduction, and at the end you can read which sentences survived.

3. **The world: Lab 1 starts flat.** Show a grid map of flat ground: food appears at random cells and regrows at random, and predators chase animals that come within 6 cells. Every animal has energy and an age: living costs energy, moving costs more, eating restores it, and an animal dies at zero energy or of old age. Add a small "next labs" strip: flat world (Lab 1) → terrain (water and mountains become impassable, food grows faster near water) → foliage (plants become food and cover), with the same animals having to adapt each time. This is the link to world building.

4. **Anatomy of a genome.** 10 gene slots ("loci") in a fixed order: 7 action genes (eat, flee, follow, wander, rest, mate, attack) and 3 temperament genes (risk, social, place). Show the example genome below as a stack of cards, one per slot. Every slot also has a neutral version, "No preference.", so evolution can switch a drive off. Founder pool: 4 instinct-like sentences plus the neutral one per slot, frozen and shared by every run, so all runs start from the same origin (5^10 ≈ 9.8 million possible starting genomes).

5. **How an animal decides: one decision, step by step.**
   a. Perception: the world becomes a short situation line ("Energy: low. Food: near. Predator: near. Animal: none. Age: adult.").
   b. Prompt: the list of actions + the animal's 10 genes + the situation + "Distribute 100 points across the actions…".
   c. A local LLM (an open-weights model served by Ollama on one desktop GPU) answers with points in JSON.
   d. The points are turned into probabilities and one action is drawn at random.
   e. The action runs for a few ticks. The LLM chooses behaviours and ordinary code does the moving. A new decision comes every 4 ticks.
   Use the real example below, including the fact that the model's points added up to 90, not 100, so the code normalises them. This is an honest detail students enjoy.

6. **How genes change between generations.**
   - Mating: two mature animals with enough energy have a child. For each gene slot, the child takes the sentence from one parent or the other (50/50). Slot 3 is always "follow", so crossover always mixes like with like.
   - Mutation: each inherited gene has a 3 % chance to change, using one operator from a menu: intensity (sometimes ↔ always), negate (always ↔ never, avoid ↔ seek), swap the condition, swap a word for a synonym, bring back a founder sentence, or an LLM rewrite in a given style (invert, add a condition, random change…). The LLM is never told what is "good".
   - Selection: there is no fitness function. Animals that eat, escape predators and mate leave more children, so their sentences spread. If the population falls below a minimum, newcomers are drawn from the founder pool.
   Show the real mutation examples below as before → after cards.

7. **What comes out of a run: readable evolution.** Show four outputs: how often each sentence appears in each gene slot over time; a family tree where every mutation is a text diff; a "top surviving genes" table; and several runs from the same founders drifting apart.
   IMPORTANT: no long evolution runs have been done yet. Draw these as mock-ups labelled "illustrative" with placeholder data, and phrase findings as questions to test, for example: "When the terrain lab adds water and food grows faster near it, will 'Prefers staying near water.' spread?" and "Do flee genes get stronger when we add predators?"

8. **Does the LLM really read the genes? The scientific method.** The prototype must pass five gates before the classroom version is built:
   - G1 Meaning: change one gene. Does behaviour move the right way?
   - G2 Signal vs noise: do real genes matter more than irrelevant sentences?
   - G3 Locality: do small edits cause small behaviour changes, so that children resemble their parents?
   - G4 Evolution: do evolved animals survive better than the founders, and by more than in a shuffled control?
   - G5 Speed: are there enough decisions per second to run a class?
   Status (prototype, October 2026): G1 passed a first small test (behaviour moved the right way in 96–99 % of directed tests). G2 has failed so far: irrelevant sentences such as "Trains leave from the north platform." shift behaviour about as much as real genes. G3–G5 have not been measured yet. Present G2 as an open problem students can work on, not as a defeat. Put the status in a small badge that is easy to update.

9. **What students learn and build.**
   Learn: getting structured (typed) output from LLMs, running models locally, prompt design, LLMs as tools inside an algorithm, and evolutionary computation (genotype → phenotype → behaviour, heredity, variation, selection, crossover between matching gene slots, drift, loss of diversity, sentences growing longer and blander). Also experimental method: controls, random seeds, reproducibility, and pass/fail criteria fixed in advance.
   Build (example exercises): add a sense and choose how it is worded; add an action and a gene slot (e.g. drink and thirst); design a mutation operator and measure its effect on diversity; compare a neural-network genome with a prompt genome in the same world; add a predator species and watch fear genes co-evolve; change only the wording of observations and see what evolves.
   Related research, in small type: Evolution through Large Models (Lehman et al., 2022); Promptbreeder (Fernando et al., 2023); EvoPrompt (Guo et al., 2023); Generative Agents (Park et al., 2023).

10. **Overview poster.** One canvas showing the full loop: World → Perception → Genome + situation → LLM → Action → Survive, eat, mate → Crossover + mutation → Next generation → back to World. Mark every step as a part students can replace: world, perception, observation wording, decision backend, action code, crossover, mutation operators, reproduction rule. Note that a rule-based backend lets students without a GPU work on everything except the model.

## Verbatim material (use exactly; do not invent other numbers or results)
Example genome, one sentence per slot:
- eat: "Eat whenever food is close."
- flee: "Always run away, whatever happens."
- follow: "Stay close to other animals."
- wander: "Keep moving to new places."
- rest: "Rest when you are tired."
- mate: "Look for a partner when energy is high."
- attack: "Never fight."
- risk: "Cautious: safety comes before food."
- social: "Social: feels safer in a group."
- place: "Prefers staying near water."

Situation: "Energy: low. Food: near. Predator: near. Animal: none. Age: adult."

Model answer (Gemma 4 12B, running locally): eat 25, flee 45, follow 5, wander 5, rest 10, mate 0, attack 0, a total of 90. Normalised: flee ≈ 50 %, eat ≈ 28 %, rest ≈ 11 %, follow ≈ 6 %, wander ≈ 6 %.

Mutations the LLM made from "Eat whenever food is close." (Gemma 4 26B):
- invert → "Avoid food whenever it is near."
- add a condition → "Eat whenever food is close and you are hungry."
- random change → "Snack when snacks are nearby."

Other founder sentences you may use: "Flee only when a predator is very close." · "Attack weaker animals when you are hungry." · "Roam far when food is scarce." · "Bold: take risks when the reward is food." · "Restless: always wants somewhere new."

## Visual direction
- Concept: a naturalist's field guide crossed with a lab notebook. Genes are index cards, the world is a clean top-down map, the decision is an annotated "prompt anatomy" diagram, and generations form a family tree with text diffs.
- Map colours: light ground, green dots for food, red for predators, dark neutral animals with one highlighted focus animal; blue water and grey-brown mountains appear only in the "next labs" illustrations. Use shape as well as colour so it works for colour-blind viewers.
- Give action genes and temperament genes two distinct, quiet colour families. Do not give each of the 10 slots its own bright colour.
- Diagrams must show the real mechanism, with arrows in the actual order of the loop. No robots, glowing brains, neon gradients or stock "AI" imagery. Animals can be simple, friendly shapes.
- Tone: calm, clear, slightly playful, and readable from the back of a classroom.
- No university logos or branding.
