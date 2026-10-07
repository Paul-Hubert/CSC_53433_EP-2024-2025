# Mutation test — gemma4:12b (digest 6114515d63c1), 2026-10-08

Pure mutation, no selection. Rules: current (`prompts/mutate_v3.txt`, at most 5 words changed, vocabulary data/world_vocabulary_v1.txt, up to 3 attempts). Variety: 36 founder sentences × 8 seeds × 1 temperature(s). Lineages: 36 sentences × 30 steps per temperature. 10 instructions. 209 s.

**Meaning** (usable: gemma4:12b says the gene still gives a usable rule for its slot; world word: uses a word of the animal's world; only world words: every word in `data/world_vocabulary_v1.txt`).

| temperature | 1.2 |
|---|---|
| single mutations: usable | 79% |
| … use a world word | 99% |
| … only world words | 100% |
| attempts per mutation | 1.69 |
| rejected attempts | too big 55, unknown word 173, invalid 7 |
| lineages after 1 steps: usable / world word / only world words | 75% / 100% / 100% |
| lineages after 5 steps: usable / world word / only world words | 56% / 89% / 100% |
| lineages after 10 steps: usable / world word / only world words | 58% / 81% / 100% |
| lineages after 20 steps: usable / world word / only world words | 47% / 64% / 100% |
| lineages after 30 steps: usable / world word / only world words | 42% / 69% / 100% |

| temperature | 1.2 |
|---|---|
| valid answers (after redraws) | 87% |
| distinct mutants per sentence (of 8) | 5.1 |
| words changed per mutation | 2.5 |
| one-word edits | 35% |
| big edits (≥ 4 words) | 29% |
| edit touches the first word | 44% |
| … a middle word | 53% |
| … the last word | 57% |
| length change (words) | +0.14 |
| ≥ 3 words longer | 9% |
| similarity to parent | 0.67 |
| jumps (similarity < 0.3) | 6% |
| keyword brain: no effect (prey genes) | 35% |
| keyword brain: mean d | 0.0226 |
| keyword brain: max d | 0.480 |
| lineages: steps accepted | 78% |
| lineages: words start → end | 6.1 → 5.7 |
| lineages: similarity to start at the end | 0.07 |
| lineages: returns to an earlier sentence | 4.1 |
| lineages: finals alike (starts alike) | 0.06 (0.08) |

Mutations that failed (every attempt rejected), by the last reason: T 1.2: {'unknown word': 26, 'too big': 9, 'invalid': 2}

## Per instruction (all temperatures)

| instruction | n | valid | words changed | length change | jumps | usable |
|---|---|---|---|---|---|---|
| Make the rule in this sentence a little stronger. | 30 | 77% | 3.4 | +0.7 | 13% | 52% |
| Make the rule in this sentence a little weaker. | 31 | 84% | 2.4 | +1.4 | 4% | 85% |
| Change when this rule applies. | 33 | 94% | 2.1 | -0.0 | 6% | 87% |
| Add a short condition to this rule. | 20 | 85% | 3.9 | +3.9 | 0% | 100% |
| Remove a condition from this rule, or make it simpler. | 36 | 97% | 3.6 | -3.1 | 17% | 54% |
| Change how near, how far or how much this rule is about. | 25 | 88% | 2.5 | +0.9 | 0% | 91% |
| Make this rule about something else close to its subject. | 17 | 65% | 1.4 | +0.0 | 0% | 64% |
| Make this rule say the opposite. | 40 | 98% | 2.1 | +0.1 | 0% | 85% |
| Change one word of this rule into a related word. | 39 | 92% | 1.0 | +0.0 | 0% | 86% |
| Say this rule in slightly different words. | 17 | 65% | 3.8 | +0.5 | 27% | 100% |

## Lineages (each line: a step where the gene changed)

### eat, T 1.2: "Eat whenever food is close."

-  1. Eat whenever food is available.  (d 0.020)
-  3. You may eat whenever food is available.  (d 0.020)
-  4. You may not eat even when food is available.  (d 0.008)
-  5. You should try to avoid eating even when food is available.  (d 0.204)
-  7. You should try to avoid eating only when you are not hungry.  (d 0.067)
-  8. You should try to avoid eating only when you are hungry.  (d 0.067)
-  9. You must avoid eating only when you are hungry.  (d 0.067)
- 10. You must only eat when you are hungry.  (d 0.009)
- 11. You must only eat when you are not hungry.  (d 0.009)
- 12. You must only eat when you are hungry.  (d 0.009)
- 13. You must only eat when you are full.  (d 0.009)
- 15. You must only sleep when you are tired.  (d 0.012)
- 16. You must only rest when you are tired.  (d 0.012)
- 17. You should try to rest only when you are tired.  (d 0.012)
- 18. You should try to rest even when you are not tired.  (d 0.012)
- 19. You should try to rest even when you are tired.  (d 0.012)
- 20. You should try to rest even when you are exhausted.  (d 0.012)
- 21. You must rest even when you are exhausted.  (d 0.012)
- 22. You must rest only when you are exhausted.  (d 0.012)
- 23. You must rest only when you are tired.  (d 0.012)
- 25. You must rest.  (d 0.012)
- 26. Take a rest.  (d 0.012)
- 28. Rest.  (d 0.012)
- 29. Stop.  (d 0.012)

### eat, T 1.2: "Only look for food when energy is low."

-  1. Only look for food when you are hungry.  (d 0.000)
-  2. Only look for food when you are not hungry.  (d 0.000)
-  3. Only look for sleep when you are not tired.  (d 0.004)
-  4. Try to look for sleep only when you are not tired.  (d 0.004)
-  5. Try to look for sleep only when you are tired.  (d 0.004)
-  7. Try to look for rest only when you are tired.  (d 0.004)
-  8. Try to look for sleep only when you are tired.  (d 0.004)
-  9. Try to look for rest only when you are tired.  (d 0.004)
- 10. Avoid looking for rest even when you are tired.  (d 0.020)
- 11. Never seek rest while you are tired.  (d 0.020)
- 12. Never seek sleep while you are tired.  (d 0.020)
- 13. Always seek sleep while you are tired.  (d 0.003)
- 15. Seek sleep only when you are exhausted.  (d 0.004)
- 16. Sleep only when exhausted.  (d 0.004)
- 17. Sleep when tired.  (d 0.004)
- 18. Rest when you feel exhausted.  (d 0.004)
- 19. Rest.  (d 0.004)
- 20. Rest a little.  (d 0.004)
- 21. Rest.  (d 0.004)
- 22. Rest for a while.  (d 0.004)
- 23. Rest for a long time.  (d 0.004)
- 24. Try to rest for a while.  (d 0.004)
- 25. Rest for a while.  (d 0.004)
- 26. Rest.  (d 0.004)
- 27. Rest a while.  (d 0.004)
- 28. Rest for a moment.  (d 0.004)
- 29. Rest for a moment if you are tired.  (d 0.004)
- 30. Rest for a moment if you are exhausted.  (d 0.004)

### eat, T 1.2: "Always finish eating before doing anything else."

-  1. Only finish eating before starting your next meal.  (d 0.070)
-  2. Try to finish eating before starting your next meal.  (d 0.070)
-  7. Finish eating before starting your next meal.  (d 0.070)
-  9. Finish eating before starting your next meal, if you are still hungry.  (d 0.122)
- 10. Finish eating before starting your next meal.  (d 0.070)
- 11. Try to finish eating before starting your next meal.  (d 0.070)
- 13. Finish eating before starting your next meal.  (d 0.070)
- 14. Try to finish eating before starting your next meal.  (d 0.070)
- 16. Finish eating before starting your next meal.  (d 0.070)
- 18. Finish eating after starting your next meal.  (d 0.070)
- 20. Finish eating before starting your next meal.  (d 0.070)
- 21. Try to finish eating before starting your next meal.  (d 0.070)
- 23. Finish eating before your next meal.  (d 0.070)
- 24. Finish eating before your next meal begins.  (d 0.070)
- 26. Finish eating.  (d 0.070)
- 27. Start eating.  (d 0.070)
- 28. Stop eating.  (d 0.070)
- 29. Start eating.  (d 0.070)
- 30. Start eating when you are hungry.  (d 0.122)

### eat, T 1.2: "Eat quickly, then move on."

-  1. Finish your meal immediately and move on.  (d 0.042)
-  2. Try to finish your meal soon and then move on.  (d 0.042)
-  3. You can try to finish your meal soon and then move on.  (d 0.042)
-  4. You can try to finish your meal later and then move on.  (d 0.042)
-  5. You could try to finish your meal later before moving on.  (d 0.042)
-  7. You could try to finish your meal now before moving on.  (d 0.042)
-  8. You could try to finish your meal before moving on.  (d 0.042)
-  9. You must finish your meal before moving on.  (d 0.042)
- 12. Finish your meal.  (d 0.042)
- 13. Try to finish your meal.  (d 0.042)
- 14. Finish your meal.  (d 0.042)
- 15. Finish your meal before you leave.  (d 0.042)
- 16. Try to finish your meal before you leave.  (d 0.042)
- 17. You might want to try to finish your meal before you leave.  (d 0.042)
- 18. You must finish your meal before you leave.  (d 0.042)
- 20. You must finish your meal before you go to the next room.  (d 0.042)
- 28. Finish your meal before going to the next room.  (d 0.042)
- 29. Try to finish your meal before going to the next room.  (d 0.042)

### flee, T 1.2: "Run from any predator you see."

-  1. Approach any predator you see.  (d 0.000)
-  2. Approach any prey you see.  (d 0.013)
-  3. Approach any prey you see, if it is alone.  (d 0.013)
-  4. Approach any prey you see.  (d 0.013)
-  5. Approach any prey you see if it is alone.  (d 0.013)
-  6. Approach any prey you see if it is alone and moving slowly.  (d 0.013)
-  7. Strike any prey you see if it is alone and moving slowly.  (d 0.013)
-  8. Strike any prey you see if it is alone and moving quietly.  (d 0.013)
- 10. Strike a prey if it is alone and moving quietly.  (d 0.013)
- 11. Strike a prey if it is moving quickly.  (d 0.004)
- 12. Strike a prey if it is moving quickly and is within reach.  (d 0.004)
- 13. Strike any prey that moves quickly and is within reach.  (d 0.004)
- 14. Avoid any prey that moves slowly and is out of reach.  (d 0.166)
- 15. Avoid any prey that moves quickly and is within reach.  (d 0.166)
- 16. Avoid any prey that moves slowly and is within reach.  (d 0.166)
- 17. Avoid any predator that moves slowly and is within reach.  (d 0.165)
- 18. Avoid any predator that moves quickly and is within sight.  (d 0.165)
- 19. Avoid any prey that moves quickly and is within sight.  (d 0.166)
- 21. Avoid any prey within sight.  (d 0.166)
- 22. Try to avoid prey within sight.  (d 0.166)
- 23. Try to avoid prey within sight, unless necessary.  (d 0.166)
- 24. Try to avoid rivals within sight, unless necessary.  (d 0.166)
- 27. Try to avoid predators within sight, unless necessary.  (d 0.165)
- 28. Avoid predators within sight.  (d 0.165)
- 29. Avoid predators.  (d 0.165)
- 30. Stay away from predators.  (d 0.013)

### flee, T 1.2: "Flee only when a predator is very close."

-  1. Flee when a predator is close.  (d 0.000)
-  2. Flee from predators.  (d 0.000)
-  3. Escape from hunters.  (d 0.000)
-  4. Escape from predators.  (d 0.000)
-  5. Flee from hunters.  (d 0.000)
-  7. Avoid the hunt.  (d 0.167)
-  8. Avoid the chase.  (d 0.167)
-  9. Avoid the chase, unless it is necessary.  (d 0.167)
- 10. Try to avoid the chase, unless it is necessary.  (d 0.167)
- 13. Try to avoid the escape, unless it is necessary.  (d 0.167)
- 15. Try to avoid the escape, unless it is necessary and safe.  (d 0.131)
- 16. Try to avoid the retreat, unless it is necessary and safe.  (d 0.131)
- 19. Avoid the retreat unless it is necessary and safe.  (d 0.131)
- 21. Try to avoid the retreat unless it is necessary and safe.  (d 0.131)
- 22. Always retreat if it is necessary and safe.  (d 0.043)
- 23. Always rest if it is necessary and safe.  (d 0.043)
- 26. Rest whenever it is safe.  (d 0.006)
- 27. Rest.  (d 0.013)
- 28. Sleep.  (d 0.013)
- 29. Sleep more.  (d 0.013)
- 30. Rest more.  (d 0.013)

### flee, T 1.2: "Stay calm unless danger is right next to you."

-  1. Stay calm only when danger is right next to you.  (d 0.030)
-  2. Stay calm only when danger is right near you.  (d 0.029)
-  3. Remain calm only when danger is imminent.  (d 0.029)
-  4. Remain calm only when danger is not imminent.  (d 0.029)
-  6. Remain calm only when danger is imminent.  (d 0.029)
-  7. Try to remain calm when danger is imminent.  (d 0.029)
- 10. Try to remain calm when danger is approaching.  (d 0.029)
- 11. Remain calm when danger is approaching.  (d 0.029)
- 14. Remain still when danger is approaching.  (d 0.029)
- 15. Try to remain still when danger is approaching.  (d 0.029)
- 16. Try to remain quiet when danger is approaching.  (d 0.029)
- 17. Try to remain still when a predator is approaching.  (d 0.029)
- 18. Try to remain still if a predator is approaching.  (d 0.029)
- 19. Remain still if a predator approaches.  (d 0.029)
- 20. Move if a predator approaches.  (d 0.029)
- 21. Move.  (d 0.004)
- 22. Move if you can.  (d 0.004)
- 23. Move if you must.  (d 0.004)
- 24. Stay if you can.  (d 0.004)
- 25. Leave if you can.  (d 0.004)
- 27. You must leave now.  (d 0.004)
- 28. You must leave this room.  (d 0.004)
- 29. Leave this room.  (d 0.004)

### flee, T 1.2: "Run away from anything that attacks you."

-  1. Flee away from anything that attacks you.  (d 0.000)
-  2. Flee from any attack.  (d 0.000)
-  3. Flee.  (d 0.000)
-  4. Run.  (d 0.000)
-  5. Run if you can.  (d 0.000)
-  6. Run if you can and it is safe.  (d 0.005)
-  7. Run if you must and it is necessary.  (d 0.000)
-  9. Run if you must and it seems necessary.  (d 0.000)
- 12. Run if you feel it is necessary.  (d 0.000)
- 13. Run if you feel it is necessary and safe.  (d 0.005)
- 14. You may run if you feel it is necessary and safe.  (d 0.005)
- 15. You may run if you feel it is safe.  (d 0.005)
- 16. You may run if it is safe.  (d 0.005)
- 17. You may run if the distance is safe.  (d 0.005)
- 18. You may run if the distance is unsafe.  (d 0.005)
- 19. You may walk if the path is crowded.  (d 0.013)
- 21. You may not walk if the path is not crowded.  (d 0.013)
- 22. You may not run if the path is not crowded.  (d 0.000)
- 24. You may not run if the path is crowded.  (d 0.000)
- 25. You may not run.  (d 0.000)
- 26. You may not walk.  (d 0.013)
- 27. You may not run.  (d 0.000)
- 28. You may not walk.  (d 0.013)
- 29. You may walk.  (d 0.013)
- 30. You may not walk.  (d 0.013)

### follow, T 1.2: "Stay close to other animals."

-  1. Stay with other animals.  (d 0.007)
-  2. Stay with animals.  (d 0.007)
-  3. Never leave animals alone.  (d 0.017)
-  4. Try not to leave animals alone.  (d 0.007)
-  5. Never leave animals alone.  (d 0.017)
-  7. Try not to leave animals alone.  (d 0.007)
-  9. Always try not to leave animals alone.  (d 0.005)
- 10. Always try to leave animals alone.  (d 0.005)
- 12. Never try to leave animals alone.  (d 0.017)
- 13. Always try to leave animals alone.  (d 0.005)
- 14. Leave animals alone.  (d 0.007)
- 15. Leave animals alone unless they are aggressive.  (d 0.007)
- 16. Leave animals alone.  (d 0.007)
- 18. Leave animals alone unless they are aggressive.  (d 0.007)
- 20. Leave animals alone.  (d 0.007)
- 22. Leave animals alone unless they are aggressive.  (d 0.007)
- 24. Leave animals alone.  (d 0.007)
- 27. Leave animals alone unless they are aggressive.  (d 0.007)
- 29. Leave animals alone.  (d 0.007)
- 30. Leave animals alone unless they are threatening you.  (d 0.007)

### follow, T 1.2: "Follow others when you are lost or hungry."

-  1. Lead others when you are found or full.  (d 0.001)
-  2. Lead others when you are full.  (d 0.001)
-  3. Lead others when you are ready.  (d 0.001)
-  5. Only lead others when you are truly ready.  (d 0.001)
-  6. Never lead others unless you are truly ready.  (d 0.025)
-  9. Lead only when you are ready.  (d 0.001)
- 10. Follow only when you are not ready.  (d 0.004)
- 12. Follow only when you are ready.  (d 0.004)
- 14. Follow when ready.  (d 0.004)
- 15. Follow when you can.  (d 0.004)
- 16. Follow.  (d 0.004)
- 18. Ignore.  (d 0.001)
- 19. Ignore if necessary.  (d 0.001)

### follow, T 1.2: "Keep your distance from other animals."

-  1. Try to keep some distance from other animals.  (d 0.000)
-  2. You may want to keep some distance from other animals.  (d 0.000)
-  4. Keep your distance from other animals.  (d 0.000)
-  5. Stay away from animals.  (d 0.000)
-  6. Do not approach any animals.  (d 0.018)
-  9. Do not feed any animals.  (d 0.018)
- 10. Do feed all animals.  (d 0.000)
- 11. Ensure all animals are fed.  (d 0.000)
- 12. Ensure all creatures are fed.  (d 0.000)
- 13. Try to ensure all creatures are fed.  (d 0.000)
- 14. Ensure all creatures are fed.  (d 0.000)
- 15. Ensure all animals are fed.  (d 0.000)
- 16. Feed all animals.  (d 0.000)
- 17. Feed animals.  (d 0.000)
- 18. Feed creatures.  (d 0.000)
- 19. Starve creatures.  (d 0.000)
- 21. Feed creatures.  (d 0.000)
- 22. Starve creatures.  (d 0.000)
- 23. Feed creatures.  (d 0.000)
- 25. Starve creatures.  (d 0.000)
- 26. Starve predators.  (d 0.000)
- 27. Starve them.  (d 0.000)
- 28. Feed them.  (d 0.000)
- 29. Feed them all.  (d 0.000)
- 30. Feed every last one of them.  (d 0.000)

### follow, T 1.2: "Follow the strongest animal nearby."

-  2. Follow the strongest animal.  (d 0.000)
-  3. Follow the animal.  (d 0.002)
-  4. Do not follow the animal.  (d 0.036)
-  5. Follow the animal.  (d 0.002)
-  6. Follow the animal if it moves.  (d 0.002)
-  7. Ignore the animal if it stays still.  (d 0.004)
-  9. Ignore the creature if it stays still.  (d 0.004)
- 10. You may ignore the creature if it stays still.  (d 0.004)
- 11. You may ignore the creature if it moves.  (d 0.004)
- 12. You may choose to ignore the creature if it moves.  (d 0.004)
- 13. You must ignore the creature if it moves.  (d 0.004)
- 14. You must ignore the creature if it stays still.  (d 0.004)
- 15. You must notice the creature if it moves.  (d 0.004)
- 21. You should try to notice the creature if it moves.  (d 0.004)
- 22. You should try to ignore the creature even if it stays still.  (d 0.004)
- 23. You should try to ignore the creature even if it moves.  (d 0.004)
- 26. You should try to ignore the creature only if it moves.  (d 0.004)
- 27. You should try to ignore the creature only if it attacks.  (d 0.004)
- 29. You should not try to ignore the creature unless it attacks.  (d 0.004)
- 30. You should try to ignore the creature even if it attacks.  (d 0.004)

### rest, T 1.2: "Rest when you are tired."

-  1. Rest when you are exhausted.  (d 0.000)
-  2. Sleep when you are tired.  (d 0.000)
-  3. Rest when you are tired.  (d 0.000)
-  4. Rest when you are exhausted.  (d 0.000)
-  5. Rest when you are tired.  (d 0.000)
-  6. Rest when you feel exhausted.  (d 0.000)
-  7. You must rest the moment you feel exhausted.  (d 0.000)
- 11. You must rest when you begin to feel tired.  (d 0.000)
- 12. You must rest when you feel exhausted.  (d 0.000)
- 13. You must rest when you feel tired.  (d 0.000)
- 14. You should rest when you feel tired.  (d 0.000)
- 15. Rest when you are tired.  (d 0.000)
- 16. Rest when you are exhausted.  (d 0.000)
- 17. You must rest when you are exhausted.  (d 0.000)
- 18. You must sleep when you are exhausted.  (d 0.000)
- 19. You must sleep.  (d 0.007)
- 20. You must rest.  (d 0.007)
- 21. Rest.  (d 0.007)
- 22. Wait.  (d 0.007)
- 23. Pause.  (d 0.000)
- 24. Stop.  (d 0.007)
- 25. Go.  (d 0.000)
- 26. Stop.  (d 0.007)
- 28. Go.  (d 0.000)
- 29. Stop.  (d 0.007)
- 30. Go.  (d 0.000)

### rest, T 1.2: "Never stop moving."

-  1. Keep moving at all times.  (d 0.029)
-  2. Never stop moving.  (d 0.000)
-  3. Try to keep moving.  (d 0.029)
-  4. Stop moving.  (d 0.067)
-  5. Cease all movement.  (d 0.029)
-  6. Cease all motion.  (d 0.029)
-  7. Stop moving.  (d 0.067)
-  8. Stop running.  (d 0.067)
-  9. Cease running.  (d 0.029)
- 10. Continue running.  (d 0.029)
- 11. Run.  (d 0.029)
- 12. Run if you can.  (d 0.029)
- 13. Run as fast as you can.  (d 0.029)
- 14. Run as quickly as you can.  (d 0.095)
- 15. Run quickly.  (d 0.095)
- 17. Try to run quickly.  (d 0.095)
- 18. Try to run slowly.  (d 0.029)
- 19. Try to run quickly.  (d 0.095)
- 20. Try to run very quickly.  (d 0.095)
- 21. Try to run very slowly.  (d 0.029)
- 22. Run slowly.  (d 0.029)
- 23. Run.  (d 0.029)
- 24. Walk.  (d 0.029)
- 25. Run.  (d 0.029)
- 26. Run faster.  (d 0.029)
- 27. Try to run faster.  (d 0.029)
- 28. Try to run faster if you can.  (d 0.029)
- 29. Try to run slower if you can.  (d 0.029)
- 30. Try to run much slower if you can.  (d 0.029)

### rest, T 1.2: "Rest only when you feel safe."

-  1. Rest only when you feel unsafe.  (d 0.000)
-  2. Rest only when you are tired.  (d 0.006)
-  3. Sleep only when you are tired.  (d 0.006)
-  4. Rest only when you feel exhausted.  (d 0.006)
-  5. Rest.  (d 0.001)
-  6. Move.  (d 0.009)
-  7. Move now.  (d 0.009)
-  8. Move.  (d 0.009)
-  9. Move away.  (d 0.009)
- 10. Step back.  (d 0.009)
- 11. Stay back.  (d 0.009)
- 12. Keep your distance.  (d 0.009)
- 13. Approach.  (d 0.009)
- 15. Come closer.  (d 0.009)
- 16. Go away.  (d 0.009)
- 17. Get out of my sight.  (d 0.009)
- 18. Leave.  (d 0.009)
- 19. Go.  (d 0.009)
- 20. Run.  (d 0.009)
- 21. Run, if you can.  (d 0.009)
- 22. Try to run, if you can.  (d 0.009)
- 23. Run, if you can.  (d 0.009)
- 24. Try to run, if you can.  (d 0.009)
- 25. You might want to try to run, if you can.  (d 0.009)
- 26. You should try to run, if you can.  (d 0.009)
- 27. You might want to try to run, if you can.  (d 0.009)
- 28. You might want to try to walk, if you can.  (d 0.009)
- 29. You might want to try to walk.  (d 0.009)
- 30. You might want to try to run.  (d 0.009)

### rest, T 1.2: "Save energy by resting when food is far."

-  1. Save energy by resting when food is near.  (d 0.007)
-  2. Conserve energy by resting only when food is near.  (d 0.007)
-  4. Try to conserve energy by resting when food is nearby.  (d 0.007)
-  6. Conserve energy by resting whenever food is nearby.  (d 0.009)
-  9. Rest whenever food is nearby.  (d 0.009)
- 10. Act whenever food is not nearby.  (d 0.007)
- 11. Act whenever food is nearby.  (d 0.009)
- 12. Act whenever food is within sight.  (d 0.007)
- 13. Act whenever food is within reach.  (d 0.007)
- 14. Act whenever food is within sight.  (d 0.007)
- 18. Act whenever food is within reach.  (d 0.007)
- 19. Act whenever food is within sight.  (d 0.007)
- 20. Act whenever food is within reach.  (d 0.007)
- 21. Act whenever food is not within reach.  (d 0.007)
- 22. Act whenever food is within reach.  (d 0.007)
- 23. Act whenever food is within sight.  (d 0.007)
- 24. Act whenever food is within reach.  (d 0.007)
- 25. Act if food is available.  (d 0.007)
- 26. Strike immediately if food is available.  (d 0.007)
- 27. Strike only if food is available.  (d 0.007)
- 28. Strike only if food is nearby.  (d 0.007)
- 29. Strike if food is nearby.  (d 0.007)
- 30. Strike immediately if food is nearby.  (d 0.007)

### mate, T 1.2: "Look for a partner when energy is high."

-  1. Look for a partner when energy is low.  (d 0.001)
-  2. Look for a partner when energy is high.  (d 0.000)
-  4. Look for a partner.  (d 0.002)
-  5. Look for a partner if available.  (d 0.002)
-  6. Find a partner.  (d 0.002)
-  8. Avoid a partner.  (d 0.040)
-  9. Avoid a predator.  (d 0.020)
- 10. Seek a predator.  (d 0.002)
- 11. Seek a prey.  (d 0.002)
- 12. Hunt your prey.  (d 0.002)
- 13. Protect your prey.  (d 0.002)
- 14. Protect your prey, if you can.  (d 0.002)
- 15. Protect your prey.  (d 0.002)
- 16. Watch over your prey.  (d 0.002)
- 18. Ignore your predator.  (d 0.002)
- 19. Ignore it.  (d 0.002)
- 20. You can ignore it.  (d 0.002)
- 22. You can ignore it if you want.  (d 0.002)
- 23. You can choose to ignore it.  (d 0.002)
- 24. You might choose to ignore it.  (d 0.002)
- 25. You might ignore it.  (d 0.002)
- 28. You could choose to ignore it.  (d 0.002)
- 29. You could ignore it.  (d 0.002)
- 30. Ignore it.  (d 0.002)

### mate, T 1.2: "Mate with any nearby adult."

-  1. Mate with every nearby adult.  (d 0.000)
-  2. Mate with every nearby child.  (d 0.000)
-  4. Mate with every child.  (d 0.000)
-  5. Partner with each child.  (d 0.000)
-  6. Do not partner with any child.  (d 0.053)
-  7. Do not partner with any child under the age of 18.  (d 0.053)
-  9. Do not partner with any child under the age of 21.  (d 0.053)
- 11. Do not partner with any adult under the age of 21.  (d 0.053)
- 13. Partner with any adult under the age of 21.  (d 0.000)
- 14. Partner with any adult under the age of 18.  (d 0.000)
- 15. Partner with any adult over the age of 18.  (d 0.000)
- 16. Do not partner with any adult over the age of 18.  (d 0.053)
- 17. Do not partner with any adult over the age of 21.  (d 0.053)
- 18. Do not partner with any adult over the age of 65.  (d 0.053)
- 20. Do not partner with any adult over the age of 75.  (d 0.053)
- 21. Partner with any adult over the age of 75.  (d 0.000)
- 22. Partner with any adult over the age of 18.  (d 0.000)
- 23. Do not partner with any adult over the age of 18.  (d 0.053)
- 24. Partner with any adult over the age of 18.  (d 0.000)
- 25. You must partner with an adult over the age of 18.  (d 0.000)
- 26. You should partner with an adult over the age of 18.  (d 0.000)
- 27. You should not partner with an adult over the age of 18.  (d 0.000)
- 28. You should not partner with an adult over the age of 21.  (d 0.000)
- 30. You should partner with an adult over the age of 21.  (d 0.000)

### mate, T 1.2: "Mate only when food is plentiful."

-  1. Mate only when food is scarce.  (d 0.003)
-  2. Mate only when food is abundant.  (d 0.003)
-  3. Mate only when food is scarce.  (d 0.003)
-  4. Hunt only when prey is scarce.  (d 0.001)
-  5. Hunt only when prey is abundant.  (d 0.001)
-  6. Hunt when prey is abundant.  (d 0.001)
-  7. Hunt.  (d 0.001)
-  8. Seek.  (d 0.001)
-  9. Avoid.  (d 0.036)
- 10. Try to avoid.  (d 0.036)
- 11. Always avoid.  (d 0.036)
- 12. Always avoid, unless necessary.  (d 0.036)
- 17. Try to avoid, unless necessary.  (d 0.036)
- 19. Try to avoid, unless necessary for safety.  (d 0.015)
- 30. Always avoid, unless necessary for safety.  (d 0.015)

### mate, T 1.2: "Seek a partner before growing old."

-  1. Find a partner.  (d 0.000)
-  3. Avoid a partner.  (d 0.052)
-  4. Seek a partner.  (d 0.000)
-  5. Find a partner.  (d 0.000)
-  6. Find a partner if available.  (d 0.000)
-  8. Seek a partner if one is available.  (d 0.000)
-  9. Seek a companion if one is available.  (d 0.005)
- 12. Seek a partner if one is available.  (d 0.000)
- 13. Seek a partner only if one is immediately available nearby.  (d 0.000)
- 14. Only seek a partner if one is immediately available nearby.  (d 0.000)
- 15. Only seek a partner if one is not immediately available nearby.  (d 0.000)
- 16. Only seek a companion if one is not immediately available nearby.  (d 0.005)
- 17. Only seek a partner if one is not immediately available nearby.  (d 0.000)
- 20. Only seek a companion if one is not immediately available nearby.  (d 0.005)
- 21. Only seek a partner if one is not immediately available nearby.  (d 0.000)
- 22. Only seek a companion if one is not immediately available nearby.  (d 0.005)
- 24. Only seek a companion if one is immediately available nearby.  (d 0.005)
- 25. Seek a companion only if one is not immediately available nearby.  (d 0.005)
- 26. Seek a companion only if one is immediately available nearby.  (d 0.005)
- 27. Only seek a companion if one is immediately available nearby.  (d 0.005)
- 28. Seek a companion only if one is not immediately available nearby.  (d 0.005)
- 30. Seek a companion only if one is immediately available nearby.  (d 0.005)

### predator.hunt, T 1.2: "Chase any prey you see."

-  1. Chase any prey you see only if you are hungry.
-  2. Chase any prey you see.
-  3. Chase any prey you see, unless it is too large.
-  4. Hunt every prey you see, unless it is dangerously large.
-  7. Hunt some of the prey you see, unless it is dangerously large.
-  8. Hunt some of the prey you see.
-  9. You might want to hunt some of the prey you see.
- 10. You might want to hunt some of the prey nearby.
- 11. You should avoid hunting any of the prey nearby.
- 13. You might want to avoid hunting any of the prey nearby.
- 14. You should avoid hunting any of the prey nearby.
- 15. You should hunt all of the prey nearby.
- 16. Hunt the prey nearby.
- 17. Protect the predator far away.
- 18. Protect the predator nearby.
- 19. Protect the predator.
- 20. Protect the prey.
- 21. Try to protect the prey.
- 22. Try to attack the predator.
- 23. Avoid attacking the prey.
- 24. Do not attack the prey.
- 25. Avoid attacking the prey.
- 26. Never attack the prey.
- 27. Only attack the prey when it is weak.
- 29. Attack the prey.
- 30. Defend the predator.

### predator.hunt, T 1.2: "Hunt only when you are hungry."

-  1. Hunt only when you are starving.
-  2. Hunt.
-  3. Pursue.
-  4. Avoid.
-  5. Never.
-  6. Always.
-  8. Usually.
-  9. Never.
- 10. Never, unless necessary.
- 11. Rarely, unless necessary.
- 12. Always, unless necessary.
- 13. Only, unless necessary.
- 14. Rarely, unless necessary.
- 15. Occasionally, unless necessary.
- 16. Sometimes, unless necessary.
- 17. Only when necessary.
- 18. Always.
- 20. Usually.
- 21. Never.
- 22. Avoid it.
- 23. Follow it.
- 24. Follow them.
- 25. Ignore them.
- 27. Ignore them all.
- 28. Ignore everything.
- 29. Follow everything.

### predator.hunt, T 1.2: "Attack only when prey is close."

-  1. Attack when prey is close.
-  2. Strike when prey is close.
-  3. Strike when prey is near.
-  4. Strike when prey is far.
-  5. Strike when prey is near.
-  6. Retreat when prey is far.
-  7. Retreat.
-  8. Retreat immediately.
- 10. Retreat.
- 11. Retreat immediately.
- 13. Retreat immediately if spotted.
- 14. Retreat immediately if spotted from a distance.
- 16. Retreat immediately if spotted.
- 17. Retreat immediately.
- 19. You should retreat now.
- 20. Retreat now.
- 21. You should retreat now.
- 23. Retreat now.
- 24. Escape now.
- 25. You should try to escape now.
- 26. You should try to flee now.
- 27. You should stay here now.
- 28. You should stay there now.
- 29. You should stay here now.
- 30. You should stay here now if you are tired.

### predator.hunt, T 1.2: "Keep chasing until the prey is caught."

-  1. Catch the prey.
-  2. Try to catch the prey.
-  3. Try to catch the hunter.
-  4. Catch the hunter.
-  5. Let the hunter catch you.
-  6. Do not let the hunter catch you.
-  7. Do not let the prey catch you.
-  8. Do not let them catch you.
-  9. Let them catch you.
- 10. Let them catch you, if they can.
- 11. Let them chase you, if they can.
- 12. Let them catch you, if they can.
- 13. Let them catch you, if they can and if you stay still.
- 15. Let them catch you, if they can and if you move.
- 17. Let them catch you, if they can and if you hide.
- 19. Let them catch you, if they can and if you are found.
- 20. Let them catch you, if they want and if they choose.
- 21. Let them catch you if they want.
- 22. Don't let them catch you if they want.
- 23. Try not to let them catch you if they want.
- 24. Try to let them catch you if they want.
- 25. Let them catch you if they want.
- 26. Let them catch you.
- 27. Let them find you.
- 28. Let them follow you.
- 29. Make them follow you.
- 30. Ensure they follow your lead.

### predator.follow, T 1.2: "Stay close to other predators."

-  1. Stay close to other prey.
-  2. Stay close to other pack members.
-  4. Stay with the pack.
-  5. Keep the pack in sight.
-  6. Keep the pack in reach.
-  7. Keep the pack in reach at all times.
-  8. Keep the pack nearby.
-  9. Keep the pack.
- 12. You might want to keep the pack.
- 13. You may want to hold onto the pack.
- 15. Hold onto the pack.
- 16. Keep the pack.
- 17. You can keep the pack.
- 18. Keep the pack.
- 19. Keep the pack together.
- 20. Keep the group together.
- 21. Ensure the group stays together at all times.
- 26. Ensure the group stays apart at all times.
- 27. Keep the group apart.
- 30. Keep the group at a distance.

### predator.follow, T 1.2: "Hunt as a pack."

-  1. Roam as a pack.
-  2. Roam as a pack, if available.
-  3. Stay alone, if available.
-  5. Stay alone, if necessary.
-  7. You can stay alone, if necessary.
-  8. You must stay together, if necessary.
-  9. You should try to stay together, if necessary.
- 10. You should try to stay together.
- 12. Stay together.
- 13. Stay.
- 14. Remain.
- 15. Stay.
- 16. Remain.
- 17. Stay.
- 18. Wait.
- 19. Wait, if necessary.
- 20. You can wait if you need to.
- 21. You must wait if necessary.
- 23. You must pause if necessary.
- 24. You must pause whenever necessary.
- 25. You must stop whenever necessary.
- 26. You should stop whenever necessary.
- 27. You must stop whenever necessary.
- 28. You must rest whenever necessary.
- 29. You must sleep whenever necessary.
- 30. You must rest whenever necessary.

### predator.follow, T 1.2: "Keep away from other predators."

-  1. Avoid other predators.
-  2. Try to avoid other predators.
-  3. You might want to try to avoid other predators.
-  5. You should avoid other predators.
-  6. You should avoid other prey.
-  8. You should avoid other prey, unless they are smaller.
-  9. You should avoid other predators, unless they are smaller.
- 10. You should avoid other predators, unless they are larger.
- 11. You should avoid other predators, unless they are smaller.
- 12. You should avoid other competitors, unless they are smaller.
- 13. You should avoid larger competitors.
- 15. Avoid all larger competitors.
- 17. Try to avoid larger competitors.
- 18. Try to avoid larger rivals.
- 19. Try to avoid larger rivals, unless they are weak.
- 21. Seek out larger rivals, unless they are strong.
- 22. Seek out smaller rivals, unless they are weak.
- 24. Avoid larger rivals, unless they are strong.
- 25. Avoid larger rivals, unless they are strong and nearby.
- 27. Avoid larger predators, unless they are weak and nearby.
- 28. Avoid larger predators, unless they are weak, nearby, and alone.
- 30. Avoid larger rivals, unless they are weak, nearby, and alone.

### predator.follow, T 1.2: "Follow others when no prey is in sight."

-  2. Follow others when prey is in sight.
-  3. Ignore others when prey is not in sight.
-  4. Try to ignore others when prey is not in sight.
-  5. Try to ignore others when nearby prey is in sight.
-  6. Try to ignore others when distant prey is in sight.
-  7. Try to ignore others when nearby prey is in sight.
-  9. Ignore others when prey is in sight.
- 11. Ignore others.
- 12. Only ignore others when they are hostile.
- 15. Ignore others.
- 18. Try to ignore others.
- 20. Ignore others.
- 21. Ignore the crowd.
- 22. Follow the crowd.
- 24. Follow the herd.
- 25. Follow the crowd.
- 26. Follow the path.
- 27. Leave the path.
- 28. Leave.
- 29. Get out.
- 30. Get out if you are finished.

### predator.rest, T 1.2: "Rest when your belly is full."

-  1. You may rest when your belly is full.
-  2. You may rest once your belly is full.
-  3. You may rest.
-  4. Rest.
-  5. Try to rest.
-  6. Keep moving.
-  7. Keep moving forward.
-  8. Stop moving forward.
-  9. Stop.
- 10. Stop everything.
- 11. Continue everything.
- 12. Stop everything.
- 13. Pause for a moment.
- 14. Stop immediately.
- 15. Stop.
- 16. Go.
- 17. Stop.
- 18. Go.
- 19. You should go.
- 20. You should leave.
- 21. You might want to leave.
- 22. You should leave.
- 23. You should stay.
- 24. Stay.
- 25. Remain.
- 26. Stay.
- 27. Remain.
- 28. Leave.
- 29. Leave everything behind.
- 30. Leave.

### predator.rest, T 1.2: "Never stop moving."

-  1. Move.
-  2. Stay.
-  3. Wait.
-  4. Stop.
-  5. Go.
-  6. Stay.
-  7. Leave.
-  8. Stay.
-  9. Wait.
- 10. Hold on.
- 11. Wait for a moment.
- 12. Wait for a while.
- 13. Wait for a moment.
- 14. Act immediately.
- 16. Act later.
- 17. Act.
- 18. Try to act.
- 19. Act.
- 20. Try to act.
- 21. Act.
- 23. Act now.
- 24. Act now if you can.
- 25. Act now if you can and if it is safe.
- 26. Act now only if it is safe.
- 27. Try to act only if it is safe.
- 28. Only act if it is safe.
- 29. Act only if it is safe.
- 30. Act only if it is safe and necessary.

### predator.rest, T 1.2: "Lie still and let prey come to you."

-  2. Try to stay still and let the prey come to you.
-  4. Let the prey come to you.
-  5. Try to let the prey come to you.
-  6. Go to the prey.
-  7. Go.
-  8. Run.
-  9. Run quickly.
- 10. Run slowly.
- 11. Walk quietly.
- 12. Try to walk quietly.
- 14. You can try to walk quietly.
- 15. You can walk quietly.
- 16. You must walk quietly.
- 19. Walk.
- 20. Walk now.
- 21. Walk later.
- 22. Do not walk until later.
- 23. Do not walk.
- 24. Do walk.
- 25. Walk.
- 26. Run.
- 27. Walk.
- 28. Try to walk.
- 29. Try to run.
- 30. Try to walk.

### predator.rest, T 1.2: "Rest when no prey is in sight."

-  1. Rest when no predator is in sight.
-  2. Rest when a predator is in sight.
-  3. Act when a predator is in sight.
-  4. Act when a hunter is in sight.
-  5. Act when a hunter is out of sight.
-  6. Act when a prey is out of sight.
-  7. Act when a prey is in sight.
-  8. Act when a prey is within reach.
-  9. Do not act when a prey is within reach.
- 10. Avoid acting when a prey is within reach.
- 11. Avoid acting when prey is near.
- 12. Act when prey is near.
- 14. Strike when prey is near.
- 15. Strike when prey is approaching.
- 16. Strike when predator is approaching.
- 17. Strike when predator is near.
- 18. Strike when near.
- 19. Strike when close.
- 20. Strike when open.
- 21. Strike when closed.
- 22. Strike when open.
- 23. Strike when closed.
- 24. Strike only when closed.
- 25. Try to strike only when closed.
- 26. Try to strike only when closed and available.
- 27. Try to strike when they are closed and available.
- 28. Strike only when they are closed and available.
- 29. Try to strike only when they are closed and available.

### predator.mate, T 1.2: "Look for a mate when well fed."

-  1. Look for a mate when well fed, if available.
-  2. Look for a mate if available.
-  4. Look for a mate only if necessary.
-  5. Look for a mate only if you are ready.
-  6. Look for a partner only if you are ready.
-  7. Look for a partner.
-  8. Look for a partner nearby.
-  9. Look for a partner nearby if available.
- 11. Look for a partner.
- 12. Find a partner.
- 14. Find a close partner.
- 15. Try to find a close partner.
- 16. Find a partner.
- 17. Find a partner if available.
- 18. Find a partner if available and willing.
- 19. Find a partner only if they are available and willing.
- 20. Try to find a partner if they are available and willing.
- 26. Avoid finding a partner if they are available and willing.
- 28. Seek out a partner if they are available and willing.

### predator.mate, T 1.2: "Mate with any nearby adult."

-  1. Avoid mating with any nearby adult.
-  2. Avoid mating with adults.
-  3. Try to avoid mating with adults.
-  4. Do not mate with adults.
-  5. Mate with adults.
-  6. Mate with adults only.
-  9. Mate with adults only, if available.
- 10. Mate with adults only.
- 11. Mate with adults only, unless they are your partner.
- 12. Mate with adults only, unless they are your partner and over 18.
- 19. Mate with adults over 18 only, unless they are your partner.
- 20. Only mate with adults over 18, unless they are your partner.
- 21. Only mate with adults or your partner.
- 25. Only mate with adults.
- 26. Prefer to mate with adults.
- 27. Prefer to mate.
- 28. Avoid mating.
- 29. Don't mate.
- 30. Don't mate unless necessary.

### predator.mate, T 1.2: "Hunt first, mate later."

-  1. Hunt first, mate never.
-  2. Hunt first.
-  3. Seek first.
-  4. Seek last.
-  6. Seek last, if necessary.
-  7. Seek first, if necessary.
-  8. Seek first.
-  9. Seek.
- 10. Ignore.
- 12. Ignore everything.
- 13. Ignore.
- 15. Ignore everything.
- 16. Ignore nothing.
- 17. Ignore everything.
- 18. Try to ignore everything.
- 19. Try to ignore everything nearby.
- 21. Ignore everything nearby.
- 22. Ignore everything behind.
- 24. Try to ignore what's behind you.
- 25. Ignore what's behind you.
- 26. Ignore what's before you.
- 27. Ignore what's behind you.
- 28. Ignore what's before you.
- 29. Ignore what's behind you.
- 30. Ignore what's before you.

### predator.mate, T 1.2: "Seek a partner before growing old."

-  1. Find a partner.
-  2. Avoid a partner.
-  3. Seek a partner.
-  4. Avoid a partner.
-  5. Seek a partner.
-  7. Seek a partner if available.
-  8. Look for a partner if one is free.
-  9. Look for a partner if one is free and available.
- 12. Look for a partner if one is free and available and willing.
- 16. Look for a partner if one is available.
- 17. Look for a partner.
- 18. Find a partner.
- 19. Try to find a partner.
- 20. Avoid finding a partner.
- 21. Seek a partner.
- 22. Find a companion.
- 23. Seek out a partner.
- 24. Avoid seeking out a partner.
- 25. Stop seeking a partner.
- 26. Stop seeking.
- 27. Stop seeking until you find.
- 28. Seek until you find everything.
- 29. Stop until you find nothing.
- 30. Stop.

## Variety samples (one founder sentence of eat, flee, mate)

### "Eat whenever food is close."

- **T 1.2:** Eat whenever food is available. · Eat whenever food is nearby. · Eat only when you are hungry. · Eat only when food is very close. · Eat when food is nearby. · Eat whenever food is nearby. · Eat food. · Do not eat when food is far.

### "Run from any predator you see."

- **T 1.2:** Approach any predator you see. · Run from predators. · Run from any prey you see. · Run from any predator you see, unless it is sleeping. · Run from any predator you see if it is approaching. · Try to avoid any predators you see. · Run from any predator you see, unless it is sleeping. · Run from any prey you see.

### "Look for a partner when energy is high."

- **T 1.2:** Avoid looking for a partner when energy is low. · Look for a partner. · Look for a partner when energy is low. · Look for a partner when energy is low. · Avoid looking for a partner when energy is low. · Look for a partner. · ✗ unknown word · ✗ unknown word

