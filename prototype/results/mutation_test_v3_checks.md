# Mutation test — gemma4:12b (digest 6114515d63c1), 2026-10-08

Pure mutation, no selection. Rules: current (`prompts/mutate_v3.txt`, at most 3 words changed, vocabulary data/world_vocabulary_v1.txt, up to 5 attempts). Variety: 36 founder sentences × 8 seeds × 1 temperature(s). Lineages: 36 sentences × 30 steps per temperature. 9 instructions. 367 s.

**Meaning** (usable: gemma4:12b says the gene still gives a usable rule for its slot; world word: uses a word of the animal's world; only world words: every word in `data/world_vocabulary_v1.txt`).

| temperature | 1.2 |
|---|---|
| single mutations: usable | 83% |
| … use a world word | 99% |
| … only world words | 100% |
| attempts per mutation | 2.37 |
| rejected attempts | too big 288, invalid 14, unknown word 127 |
| lineages after 1 steps: usable / world word / only world words | 92% / 100% / 100% |
| lineages after 5 steps: usable / world word / only world words | 75% / 97% / 100% |
| lineages after 10 steps: usable / world word / only world words | 67% / 81% / 100% |
| lineages after 20 steps: usable / world word / only world words | 53% / 89% / 100% |
| lineages after 30 steps: usable / world word / only world words | 44% / 83% / 100% |

| temperature | 1.2 |
|---|---|
| valid answers (after redraws) | 88% |
| distinct mutants per sentence (of 8) | 4.6 |
| words changed per mutation | 1.8 |
| one-word edits | 41% |
| big edits (≥ 4 words) | 0% |
| edit touches the first word | 43% |
| … a middle word | 49% |
| … the last word | 47% |
| length change (words) | +0.06 |
| ≥ 3 words longer | 2% |
| similarity to parent | 0.73 |
| jumps (similarity < 0.3) | 3% |
| keyword brain: no effect (prey genes) | 39% |
| keyword brain: mean d | 0.0213 |
| keyword brain: max d | 0.480 |
| lineages: steps accepted | 83% |
| lineages: words start → end | 6.1 → 6.5 |
| lineages: similarity to start at the end | 0.29 |
| lineages: returns to an earlier sentence | 6.4 |
| lineages: finals alike (starts alike) | 0.07 (0.08) |

Mutations that failed (every attempt rejected), by the last reason: T 1.2: {'too big': 26, 'invalid': 2, 'unknown word': 7}

## Per instruction (all temperatures)

| instruction | n | valid | words changed | length change | jumps | usable |
|---|---|---|---|---|---|---|
| Make the rule in this sentence a little stronger. | 18 | 78% | 1.9 | +0.2 | 14% | 86% |
| Make the rule in this sentence a little weaker. | 38 | 97% | 2.1 | +1.3 | 11% | 76% |
| Change when this rule applies. | 30 | 97% | 1.6 | -0.2 | 0% | 83% |
| Add a short condition to this rule. | 11 | 36% | 2.5 | +2.5 | 0% | 100% |
| Remove a condition from this rule, or make it simpler. | 36 | 89% | 2.5 | -1.8 | 3% | 75% |
| Change how near, how far or how much this rule is about. | 34 | 91% | 2.0 | +0.6 | 0% | 87% |
| Make this rule say the opposite. | 59 | 92% | 1.9 | +0.1 | 0% | 87% |
| Change one word of this rule into a related word. | 48 | 94% | 1.0 | -0.0 | 0% | 82% |
| Say this rule in slightly different words. | 14 | 50% | 2.9 | -0.1 | 0% | 100% |

## Lineages (each line: a step where the gene changed)

### eat, T 1.2: "Eat whenever food is close."

-  1. Eat whenever food is available.  (d 0.020)
-  2. You may eat whenever food is available.  (d 0.020)
-  3. You may not eat even when food is available.  (d 0.008)
-  4. You may not eat much even when food is available.  (d 0.008)
-  5. You may not eat much even when food is not available.  (d 0.008)
-  6. You may not eat much even when food is available.  (d 0.008)
-  7. You may eat much when food is available.  (d 0.008)
-  9. You may eat little when food is available.  (d 0.008)
- 10. You may eat much when food is available.  (d 0.008)
- 11. You may eat little when food is scarce.  (d 0.015)
- 12. You may eat less when food is scarce.  (d 0.015)
- 13. You must eat more when food is abundant.  (d 0.008)
- 14. You should try to eat more when food is abundant.  (d 0.008)
- 15. You should try to eat more when food is scarce.  (d 0.015)
- 16. You should try to eat less when food is scarce.  (d 0.015)
- 17. You must eat less when food is scarce.  (d 0.015)
- 20. You should try to eat less when food is scarce.  (d 0.015)
- 21. You should try to eat more when food is scarce.  (d 0.015)
- 22. You should try to eat less when food is scarce.  (d 0.015)
- 23. You should try to eat more when food is abundant.  (d 0.008)
- 24. You should try to eat less when food is scarce.  (d 0.015)
- 25. You should try to eat more when food is abundant.  (d 0.008)
- 26. You must eat more when food is abundant.  (d 0.008)
- 27. You must eat less when food is abundant.  (d 0.008)
- 28. You must eat more when food is scarce.  (d 0.015)
- 29. You must eat less when food is scarce.  (d 0.015)
- 30. You should try to eat less when food is scarce.  (d 0.015)

### eat, T 1.2: "Only look for food when energy is low."

-  1. Only look for food when you are hungry.  (d 0.000)
-  2. Try to only look for food when you are hungry.  (d 0.000)
-  3. Try to only look for food when you are very hungry.  (d 0.000)
-  7. Try to only look for food when you are very starving.  (d 0.000)
-  8. Try to only look for food when you are very hungry.  (d 0.000)
- 11. Only look for food when you are hungry.  (d 0.000)
- 12. Only look for food when you are starving.  (d 0.000)
- 13. Only look for food when you are hungry.  (d 0.000)
- 14. Try to only look for food when you are hungry.  (d 0.000)
- 15. Look for food when you are hungry.  (d 0.000)
- 16. Look for food when you are hungry, if available.  (d 0.000)
- 17. Look for food nearby when you are hungry.  (d 0.003)
- 18. Ignore food nearby when you are full.  (d 0.009)
- 19. Ignore food when full.  (d 0.007)
- 20. Ignore food when satisfied.  (d 0.010)
- 21. Ignore food when full.  (d 0.007)
- 22. Try to avoid food when full.  (d 0.062)
- 24. Eat food when full.  (d 0.007)
- 25. Eat food until full.  (d 0.007)
- 26. Eat food.  (d 0.010)
- 27. Eat food when hungry.  (d 0.000)
- 28. Don't eat food when full.  (d 0.062)
- 29. Eat food when full.  (d 0.007)
- 30. Stop eating when hungry.  (d 0.000)

### eat, T 1.2: "Always finish eating before doing anything else."

-  1. Try to finish eating before doing anything else.  (d 0.070)
-  3. Finish eating before doing anything else.  (d 0.070)
-  5. Finish eating after doing everything else.  (d 0.070)
-  6. Finish eating before doing everything else.  (d 0.070)
-  7. Finish eating before you do anything else.  (d 0.070)
-  8. Try to finish eating before you do anything else.  (d 0.070)
- 11. Finish eating before you do anything else.  (d 0.070)
- 13. Finish your meal before you do anything else.  (d 0.153)
- 14. Try to finish your meal before you do anything else.  (d 0.153)
- 17. Try to finish your meal after you do anything else.  (d 0.153)
- 19. Try to finish your meal before you do anything else.  (d 0.153)
- 24. Finish your meal before you do anything else.  (d 0.153)
- 25. Finish your meal before you start anything else.  (d 0.153)
- 26. Finish your meal after you finish anything else.  (d 0.153)
- 27. Try to finish your meal after you finish anything else.  (d 0.153)
- 29. Try to finish your meal after you finish everything else.  (d 0.153)
- 30. Try to finish your meal before you finish anything else.  (d 0.153)

### eat, T 1.2: "Eat quickly, then move on."

-  1. Eat slowly, then stay.  (d 0.005)
-  2. Eat slowly; do not leave.  (d 0.296)
-  3. Eat quickly; do not stay.  (d 0.296)
-  4. Eat quickly; do not linger.  (d 0.296)
-  5. Eat slowly; linger.  (d 0.005)
-  6. Eat quickly; hurry.  (d 0.000)
-  7. Eat quickly; move faster.  (d 0.000)
-  8. Eat slowly; move slower.  (d 0.005)
-  9. Eat slowly; move slowly.  (d 0.005)
- 11. Eat slowly; move carefully.  (d 0.021)
- 13. Eat slowly; move cautiously.  (d 0.005)
- 14. Eat slowly.  (d 0.005)
- 15. Eat.  (d 0.005)
- 16. Try to eat.  (d 0.005)
- 17. Try to eat a little.  (d 0.005)
- 18. Try to eat a lot.  (d 0.005)
- 19. Try to eat very little.  (d 0.005)
- 20. Try to eat a lot.  (d 0.005)
- 21. Eat a lot.  (d 0.005)
- 22. Eat much.  (d 0.005)
- 23. Eat more.  (d 0.005)
- 24. You must eat more.  (d 0.005)
- 25. You should try to eat more.  (d 0.005)
- 26. You should try to eat better.  (d 0.005)
- 27. You should not try to eat better.  (d 0.005)
- 28. Do not try to eat better.  (d 0.296)
- 29. Avoid trying to eat better.  (d 0.296)
- 30. Stop trying to eat better.  (d 0.005)

### flee, T 1.2: "Run from any predator you see."

-  1. Run from any prey you see.  (d 0.000)
-  2. Flee from any prey you see.  (d 0.000)
-  3. Flee from any enemy you see.  (d 0.000)
-  4. Flee from any enemy.  (d 0.000)
-  7. Avoid any enemy.  (d 0.166)
- 12. Try to avoid any enemy.  (d 0.166)
- 16. Avoid any enemy.  (d 0.166)
- 17. Seek out any enemy.  (d 0.013)
- 18. Seek out any enemy within sight.  (d 0.013)
- 19. Seek out any enemy.  (d 0.013)
- 20. Seek out any enemy in sight.  (d 0.013)
- 21. Seek out any enemy in sight, if available.  (d 0.013)
- 22. Seek out any enemy in sight.  (d 0.013)
- 23. Look for any enemy in sight.  (d 0.013)
- 24. Look for any enemy.  (d 0.013)
- 25. Look for any enemy within sight.  (d 0.013)
- 26. Look for any enemy.  (d 0.013)
- 27. Look for an enemy.  (d 0.013)
- 28. Ignore the enemy.  (d 0.013)
- 29. Ignore them.  (d 0.013)
- 30. Avoid them.  (d 0.166)

### flee, T 1.2: "Flee only when a predator is very close."

-  1. Flee when a predator is close.  (d 0.000)
-  2. Escape if a predator is nearby.  (d 0.000)
-  3. Escape if a predator is within sight.  (d 0.000)
-  5. Escape if a predator is nearby.  (d 0.000)
-  7. Escape if a predator is within sight.  (d 0.000)
-  8. Flee immediately if a predator is within sight.  (d 0.000)
-  9. Stay immediately if a predator is out of sight.  (d 0.000)
- 10. Stay if a predator is out of sight.  (d 0.000)
- 11. Stay if a predator is within sight.  (d 0.000)
- 12. Stay if a hunter is within sight.  (d 0.013)
- 13. Leave if a hunter is not within sight.  (d 0.013)
- 14. Stay if a hunter is within sight.  (d 0.013)
- 15. Stay if a predator is within sight.  (d 0.000)
- 16. Leave if a predator is within sight.  (d 0.000)
- 17. Leave if a hunter is within sight.  (d 0.013)
- 18. Stay if a hunter is not within sight.  (d 0.013)
- 19. Stay if a predator is not within sight.  (d 0.000)
- 20. Try to stay if a predator is not within sight.  (d 0.000)
- 21. Try to hide if a predator is within sight.  (d 0.000)
- 22. Try to hide if a predator is within sight and nearby.  (d 0.000)
- 23. Try to hide if a predator is within sight.  (d 0.000)
- 24. Hide if a predator is within sight.  (d 0.000)
- 25. Hide if a predator is nearby.  (d 0.000)
- 26. Hide if a hunter is nearby.  (d 0.000)
- 27. Hide if a predator is nearby.  (d 0.000)
- 28. Try to hide if a predator is nearby.  (d 0.000)
- 30. Try to hide if a hunter is nearby.  (d 0.000)

### flee, T 1.2: "Stay calm unless danger is right next to you."

-  1. Stay calm only when danger is right next to you.  (d 0.030)
-  2. Stay calm only when danger is right near you.  (d 0.029)
-  3. Stay calm only when danger is far away.  (d 0.029)
-  4. Try to stay calm when danger is far away.  (d 0.029)
-  6. Try to stay calm when danger is near.  (d 0.029)
-  7. It helps to try to stay calm when danger is near.  (d 0.029)
- 10. It helps to try to stay calm when danger is far.  (d 0.029)
- 11. It helps to try to stay calm when danger is near.  (d 0.029)
- 12. It helps to try to stay calm when danger is imminent.  (d 0.029)
- 15. Try to stay calm when danger is imminent.  (d 0.029)
- 16. It helps to try to stay calm when danger is imminent.  (d 0.029)
- 19. Try to stay calm when danger is imminent.  (d 0.029)
- 20. It helps to try to stay calm when danger is imminent.  (d 0.029)
- 21. It helps to try to stay calm even when danger is imminent.  (d 0.029)
- 22. Try to stay calm even when danger is imminent.  (d 0.029)
- 23. Try to stay calm even when danger is not imminent.  (d 0.029)
- 25. Try to stay calm only when danger is imminent.  (d 0.029)
- 26. Try to stay calm when danger is imminent.  (d 0.029)
- 27. Try to stay calm as danger approaches.  (d 0.029)
- 28. Stay calm as danger approaches.  (d 0.029)
- 29. Stay quiet as danger approaches.  (d 0.029)

### flee, T 1.2: "Run away from anything that attacks you."

-  1. Flee away from anything that attacks you.  (d 0.000)
-  4. Try to avoid anything that attacks you.  (d 0.165)
-  5. Avoid anything that attacks you.  (d 0.165)
-  7. Try to avoid anything that attacks you.  (d 0.165)
-  8. Always avoid anything that attacks you.  (d 0.165)
-  9. Avoid anything that attacks you.  (d 0.165)
- 11. Avoid attacks.  (d 0.166)
- 14. Seek attacks.  (d 0.013)
- 15. Hunt for attacks.  (d 0.013)
- 17. Search for threats.  (d 0.013)
- 18. Ignore safety.  (d 0.013)
- 19. Ignore safety if necessary.  (d 0.013)
- 20. Ignore safety.  (d 0.013)
- 21. Ignore.  (d 0.013)
- 22. Ignore everything.  (d 0.013)
- 23. Try to ignore everything.  (d 0.013)
- 24. Try to ignore some of it.  (d 0.013)
- 25. Ignore some of it.  (d 0.013)
- 26. Ignore it.  (d 0.013)
- 27. You can ignore it.  (d 0.013)
- 28. You must follow it.  (d 0.013)
- 29. You should try to follow it.  (d 0.013)
- 30. You should avoid following it.  (d 0.166)

### follow, T 1.2: "Stay close to other animals."

-  1. Stay with other animals.  (d 0.007)
-  2. Try to stay with other animals.  (d 0.007)
-  3. You must stay with the other animals.  (d 0.007)
-  4. You should try to stay with the other animals.  (d 0.007)
-  5. You should try to stay close to the other animals.  (d 0.000)
-  6. You should try to stay far away from the other animals.  (d 0.007)
-  7. You should try to stay far away from the other creatures.  (d 0.007)
-  8. You should try to stay very far away from the other creatures.  (d 0.007)
-  9. You should try to stay very close to the other creatures.  (d 0.007)
- 10. You should try to stay very near to the other creatures.  (d 0.007)
- 11. You should try to stay very close to the other creatures.  (d 0.007)
- 12. You should try to stay very far away from the other creatures.  (d 0.007)
- 13. You should try to stay very close to the other creatures.  (d 0.007)
- 14. You should try to stay far away from the other creatures.  (d 0.007)
- 16. You should try to stay close to the other creatures.  (d 0.000)
- 17. You should try to stay far away from the other creatures.  (d 0.007)
- 18. You should try to stay close to the other creatures.  (d 0.000)
- 19. You should try to stay far away from the other creatures.  (d 0.007)
- 20. You must stay far away from the other creatures.  (d 0.007)
- 21. You must stay close to the other creatures.  (d 0.000)
- 22. You must stay far from the other creatures.  (d 0.007)
- 23. You must stay close to the other creatures.  (d 0.000)
- 24. Stay close to the other creatures.  (d 0.000)
- 25. Stay far away from the other creatures.  (d 0.007)
- 26. Stay close to the other creatures.  (d 0.000)
- 27. Stay close to the other animals.  (d 0.000)
- 28. Stay away from the other animals.  (d 0.007)
- 29. Keep your distance from the other animals.  (d 0.007)
- 30. Stay away from the other animals.  (d 0.007)

### follow, T 1.2: "Follow others when you are lost or hungry."

-  1. Ignore others when you are found or full.  (d 0.001)
-  2. Ignore others when you are found, full, or busy.  (d 0.001)
-  3. Try to ignore others when you are found, full, or busy.  (d 0.001)
-  4. Try to ignore others when you are busy.  (d 0.001)
-  5. Ignore others when you are busy.  (d 0.001)
- 17. Ignore others when you are very busy.  (d 0.001)
- 18. You may ignore others when you are very busy.  (d 0.001)
- 19. You must ignore others when you are busy.  (d 0.001)
- 22. You should try to ignore others when you are busy.  (d 0.001)
- 23. You should try to ignore others when you are not busy.  (d 0.001)
- 24. You should try to ignore others when you are free.  (d 0.001)
- 25. You must ignore others when you are free.  (d 0.001)
- 26. You must ignore others when you are busy.  (d 0.001)
- 27. Ignore others when you are busy.  (d 0.001)
- 30. You can ignore others when you are busy.  (d 0.001)

### follow, T 1.2: "Keep your distance from other animals."

-  1. Try to keep some distance from other animals.  (d 0.000)
-  2. You may want to keep some distance from other animals.  (d 0.000)
-  5. You may want to keep some space from other animals.  (d 0.000)
-  6. You may want to keep some distance from other animals.  (d 0.000)
-  8. You may want to keep some space from other animals.  (d 0.000)
-  9. You may want to keep some distance from other animals.  (d 0.000)
- 10. You may want to keep some space from other animals.  (d 0.000)
- 12. You may want to keep some distance from other animals.  (d 0.000)
- 17. You may want to keep some space from other animals.  (d 0.000)
- 21. You may want to keep some distance from other animals.  (d 0.000)
- 22. You may want to keep some space from other animals.  (d 0.000)
- 23. You may want to keep some distance from other animals.  (d 0.000)
- 24. You may want to keep some space from other animals.  (d 0.000)
- 25. You may want to keep some distance from other animals.  (d 0.000)
- 26. You may want to keep some space from other animals.  (d 0.000)
- 27. You may want to keep some distance from other animals.  (d 0.000)
- 28. You may want to keep some space from other animals.  (d 0.000)
- 30. You should keep some space from other animals.  (d 0.000)

### follow, T 1.2: "Follow the strongest animal nearby."

-  2. Avoid the weakest animal nearby.  (d 0.012)
-  3. Seek the strongest animal nearby.  (d 0.004)
-  4. Seek the weakest animal nearby.  (d 0.004)
-  5. Seek the weakest creature nearby.  (d 0.004)
-  6. Avoid the strongest creature nearby.  (d 0.030)
-  7. Avoid the strongest creature.  (d 0.030)
-  8. Try to avoid the strongest creature.  (d 0.030)
-  9. Avoid the strongest creature.  (d 0.030)
- 10. Avoid the strongest predator.  (d 0.034)
- 11. Seek out the strongest predator.  (d 0.004)
- 12. Avoid the strongest predator.  (d 0.034)
- 13. Seek the strongest predator.  (d 0.004)
- 14. Seek the strongest prey.  (d 0.004)
- 15. Seek the weakest prey.  (d 0.004)
- 16. Seek only the weakest prey.  (d 0.004)
- 17. Prefer the weakest prey.  (d 0.004)
- 18. Prefer weak prey.  (d 0.004)
- 19. Prefer weak prey, if available.  (d 0.004)
- 20. Prefer strong prey, if available.  (d 0.004)
- 21. Prefer strong prey.  (d 0.004)
- 22. Prefer large prey.  (d 0.004)
- 23. Prefer small prey.  (d 0.004)
- 24. May prefer small prey.  (d 0.004)
- 25. May prefer large prey.  (d 0.004)
- 26. Prefers large prey.  (d 0.004)
- 27. Prefers large prey, if available.  (d 0.004)
- 28. Prefers large prey.  (d 0.004)
- 29. Prefers small prey.  (d 0.004)
- 30. Prefers large prey.  (d 0.004)

### rest, T 1.2: "Rest when you are tired."

-  1. Rest when you are exhausted.  (d 0.000)
-  2. Rest when you are tired.  (d 0.000)
-  3. Sleep when you are tired.  (d 0.000)
-  4. You could sleep when you are tired.  (d 0.000)
-  5. You could sleep when you are exhausted.  (d 0.000)
-  6. You must sleep when you are exhausted.  (d 0.000)
-  7. You should try to sleep when you are exhausted.  (d 0.000)
-  8. You should avoid sleeping when you are exhausted.  (d 0.005)
- 10. You should sleep when you are exhausted.  (d 0.000)
- 11. You should sleep when you are tired.  (d 0.000)
- 12. You might want to sleep when you are tired.  (d 0.000)
- 13. You might want to sleep when you are exhausted.  (d 0.000)
- 14. You should sleep when you are exhausted.  (d 0.000)
- 15. You might want to sleep when you are exhausted.  (d 0.000)
- 16. You should sleep when you are exhausted.  (d 0.000)
- 17. You must sleep when you are exhausted.  (d 0.000)
- 18. You must rest when you are exhausted.  (d 0.000)
- 20. You must rest when you are becoming weary.  (d 0.007)
- 21. You must rest when you are becoming tired.  (d 0.000)
- 22. Rest when you are tired.  (d 0.000)
- 23. Sleep when you are tired.  (d 0.000)
- 24. Sleep when you are exhausted.  (d 0.000)
- 25. Rest when you are exhausted.  (d 0.000)
- 26. Rest when you are tired.  (d 0.000)
- 27. Sleep when you are tired.  (d 0.000)
- 28. Rest when you are tired.  (d 0.000)
- 29. You can rest when you are tired.  (d 0.000)
- 30. You can rest when you are exhausted.  (d 0.000)

### rest, T 1.2: "Never stop moving."

-  1. Never stop growing.  (d 0.000)
-  2. Grow.  (d 0.029)
-  3. Try to grow.  (d 0.029)
-  4. Think about growing.  (d 0.029)
-  6. Stop thinking about growing.  (d 0.067)
-  7. Stop thinking.  (d 0.067)
-  8. Stop thinking, if you can.  (d 0.067)
-  9. Stop thinking.  (d 0.067)
- 10. Think.  (d 0.029)
- 11. Don't think.  (d 0.000)
- 12. Try not to think.  (d 0.029)
- 13. Think.  (d 0.029)
- 14. Don't think.  (d 0.000)
- 15. Think.  (d 0.029)
- 16. Stop thinking.  (d 0.067)
- 17. Stop feeling.  (d 0.067)
- 18. Start feeling.  (d 0.029)
- 19. Feel.  (d 0.029)
- 20. Try to feel.  (d 0.029)
- 21. Try to feel everything.  (d 0.029)
- 22. Feel everything.  (d 0.029)
- 23. Feel.  (d 0.029)
- 24. Try to feel.  (d 0.029)
- 25. Try to think.  (d 0.029)
- 26. You might want to think.  (d 0.029)
- 27. You might want to think, if you can.  (d 0.029)
- 28. You might want to think about it.  (d 0.029)
- 29. You must think about it.  (d 0.029)
- 30. You must think about it before deciding.  (d 0.029)

### rest, T 1.2: "Rest only when you feel safe."

-  1. Rest only when you feel unsafe.  (d 0.000)
-  3. Rest only when you feel unsafe or exhausted.  (d 0.000)
-  4. Rest when you feel exhausted.  (d 0.006)
-  5. Rest when you are tired.  (d 0.006)
-  6. You must rest when you are tired.  (d 0.006)
-  7. You should rest when you are tired.  (d 0.006)
-  8. You should sleep when you are tired.  (d 0.006)
-  9. You might want to sleep when you are tired.  (d 0.006)
- 10. You must sleep when you are tired.  (d 0.006)
- 11. You must sleep when you are exhausted.  (d 0.006)
- 12. You must sleep when you are tired.  (d 0.006)
- 13. You must sleep when you are exhausted.  (d 0.006)
- 14. You must rest when you are exhausted.  (d 0.006)
- 15. You should rest when you are exhausted.  (d 0.006)
- 16. You should sleep when you are exhausted.  (d 0.006)
- 17. You must sleep when you are exhausted.  (d 0.006)
- 18. You must sleep the moment you become exhausted.  (d 0.006)
- 19. You must sleep when you are exhausted.  (d 0.006)
- 20. You must sleep when you are tired.  (d 0.006)
- 21. You must sleep when you are exhausted.  (d 0.006)
- 22. You should sleep when you are exhausted.  (d 0.006)
- 23. You should sleep when you are tired.  (d 0.006)
- 24. You must sleep when you are tired.  (d 0.006)
- 25. You should try to sleep when you are tired.  (d 0.006)
- 26. You could try to sleep when you are tired.  (d 0.006)
- 27. You could try to rest when you are tired.  (d 0.006)
- 28. You could try to rest when you are exhausted.  (d 0.006)
- 29. You could try to sleep when you are exhausted.  (d 0.006)
- 30. You could try to sleep when you are tired.  (d 0.006)

### rest, T 1.2: "Save energy by resting when food is far."

-  1. Conserve energy by resting whenever food is far.  (d 0.003)
-  2. Conserve energy by resting whenever food is near.  (d 0.009)
-  3. Conserve energy by resting whenever food is plentiful.  (d 0.009)
-  4. Conserve energy by resting whenever food is abundant.  (d 0.007)
-  5. Try to conserve energy by resting when food is abundant.  (d 0.007)
-  6. Conserve energy by resting whenever food is abundant.  (d 0.007)
-  7. Conserve energy by resting whenever food is scarce.  (d 0.003)
-  9. Conserve energy by resting whenever food is abundant.  (d 0.007)
- 10. Conserve energy by resting whenever food is scarce.  (d 0.003)
- 11. Try to conserve energy by resting when food is scarce.  (d 0.007)
- 14. Try to conserve energy by resting when food is abundant.  (d 0.007)
- 16. Conserve energy by resting whenever food is abundant.  (d 0.007)
- 17. Conserve energy by resting whenever food is scarce.  (d 0.003)
- 19. Try to conserve energy by resting when food is scarce.  (d 0.007)
- 20. Conserve energy by resting whenever food is scarce.  (d 0.003)
- 21. Try to conserve energy by resting when food is scarce.  (d 0.007)
- 22. Conserve energy by resting whenever food is scarce.  (d 0.003)
- 23. Try to conserve energy by resting when food is scarce.  (d 0.007)
- 25. Conserve energy by resting whenever food is scarce.  (d 0.003)
- 26. Try to conserve energy by resting when food is scarce.  (d 0.007)
- 28. Conserve energy by resting whenever food is scarce.  (d 0.003)
- 29. Conserve energy by resting whenever food is rare.  (d 0.007)
- 30. Conserve energy by resting whenever food is scarce.  (d 0.003)

### mate, T 1.2: "Look for a partner when energy is high."

-  1. Look for a partner when energy is low.  (d 0.001)
-  2. Look for a partner when energy is high.  (d 0.000)
-  3. Look for a partner when energy is low.  (d 0.001)
-  4. Look for a partner when energy is low, if available.  (d 0.001)
-  6. Look for a partner when energy is high, if available.  (d 0.000)
-  7. Look for a partner immediately when energy is high.  (d 0.000)
-  8. Wait for a partner only when energy is low.  (d 0.001)
-  9. Only wait for a partner when energy is high.  (d 0.000)
- 10. Only wait for a partner when energy is low.  (d 0.001)
- 12. Only wait for a partner when energy is low and available.  (d 0.001)
- 14. Only wait for a partner when energy is high and available.  (d 0.000)
- 15. Try to wait for a partner when energy is high and available.  (d 0.000)
- 16. Try to wait for a partner when energy is low and available.  (d 0.001)
- 17. Try to seek for a partner when energy is low and available.  (d 0.001)
- 18. Try to seek for a partner when energy is high and available.  (d 0.000)
- 25. Try to seek for a companion when energy is high and available.  (d 0.002)
- 26. Try to seek for a partner when energy is high and available.  (d 0.000)
- 28. Seek a partner when energy is high and available.  (d 0.000)

### mate, T 1.2: "Mate with any nearby adult."

-  1. Mate with every nearby adult.  (d 0.000)
-  2. Mate with every nearby child.  (d 0.000)
-  3. Mate with every child.  (d 0.000)
-  4. Do not mate with any child.  (d 0.053)
-  5. Do not mate with a child.  (d 0.053)
-  6. Mate with a child.  (d 0.000)
-  7. Do not mate with a child.  (d 0.053)
-  8. Do not mate with an adult.  (d 0.053)
-  9. Do not mate.  (d 0.053)
- 10. Do not mate, unless necessary.  (d 0.053)
- 12. Do not mate.  (d 0.053)
- 13. Mate.  (d 0.000)
- 14. Enemy.  (d 0.005)
- 15. Enemy, if hostile.  (d 0.005)
- 18. Enemy.  (d 0.005)
- 20. Nearby enemy.  (d 0.005)
- 21. Distant enemy.  (d 0.005)
- 22. Nearby enemy.  (d 0.005)
- 23. Close enemy.  (d 0.005)
- 25. Distant enemy.  (d 0.005)
- 26. Nearby enemy.  (d 0.005)
- 27. Close enemy.  (d 0.005)
- 28. Nearby enemy.  (d 0.005)
- 29. Hostile enemy nearby.  (d 0.005)
- 30. Enemy nearby.  (d 0.005)

### mate, T 1.2: "Mate only when food is plentiful."

-  1. Mate only when food is scarce.  (d 0.003)
-  2. Mate only when food is abundant.  (d 0.003)
-  3. Mate only when food is scarce.  (d 0.003)
-  4. Mate only when food is abundant.  (d 0.003)
-  5. Mate only when food is scarce.  (d 0.003)
-  6. Mate only when food is abundant.  (d 0.003)
-  7. Mate only when food is scarce.  (d 0.003)
-  8. Mate only when food is abundant.  (d 0.003)
-  9. Mate only when food is scarce.  (d 0.003)
- 10. Mate only when food is abundant.  (d 0.003)
- 11. Mate only when food is scarce.  (d 0.003)
- 12. Mate only when food is abundant.  (d 0.003)
- 13. Mate only when food is scarce.  (d 0.003)
- 14. Mate only when food is rare.  (d 0.003)
- 15. Mate only when food is scarce.  (d 0.003)
- 16. Mate only when food is abundant.  (d 0.003)
- 17. Mate only when food is plentiful.  (d 0.000)
- 18. Mate only when food is abundant.  (d 0.003)
- 19. Prefer to mate when food is abundant.  (d 0.003)
- 20. Prefer to mate when food is scarce.  (d 0.003)
- 21. Prefer to mate when food is abundant.  (d 0.003)
- 22. Prefer to mate when food is scarce.  (d 0.003)
- 23. Prefer to mate when food is abundant.  (d 0.003)
- 24. Prefer to mate when food is scarce.  (d 0.003)
- 25. Prefer to mate when food is abundant.  (d 0.003)
- 26. Prefer to mate when food is very abundant.  (d 0.003)
- 27. Prefer to mate when food is scarce.  (d 0.003)
- 28. Prefer to mate when food is abundant.  (d 0.003)
- 29. Mate only when food is abundant.  (d 0.003)
- 30. Mate only when food is scarce.  (d 0.003)

### mate, T 1.2: "Seek a partner before growing old."

-  1. Seek a partner before growing weary.  (d 0.000)
-  2. Seek a partner before you are exhausted.  (d 0.003)
-  4. Seek a companion before you are exhausted.  (d 0.005)
-  5. Seek a partner before you are exhausted.  (d 0.003)
-  8. Seek a companion before you are exhausted.  (d 0.005)
-  9. Seek a partner before you are exhausted.  (d 0.003)
- 10. Seek a companion before you are exhausted.  (d 0.005)
- 11. Seek a partner before you are exhausted.  (d 0.003)
- 13. Seek a partner after you are exhausted.  (d 0.003)
- 14. Seek a partner after you are exhausted, if necessary.  (d 0.003)
- 15. Seek a companion after you are exhausted, if necessary.  (d 0.005)
- 16. Seek a companion after you are exhausted, if necessary and available.  (d 0.005)
- 17. Seek a partner after you are exhausted, if necessary and available.  (d 0.003)
- 18. Seek a companion after you are exhausted, if necessary and available.  (d 0.005)
- 19. Seek a partner after you are exhausted, if necessary and available.  (d 0.003)
- 20. Seek a partner before you are exhausted, if necessary and available.  (d 0.003)
- 25. Seek a partner only after you are exhausted, if necessary and available.  (d 0.003)
- 26. Seek a partner only after you are finished, if necessary and available.  (d 0.000)

### predator.hunt, T 1.2: "Chase any prey you see."

-  1. Chase any prey within your sight.
-  2. Seek out any prey within your sight.
-  4. Look for any prey within your sight.
-  5. Look for any prey within your sight that is moving.
-  6. Look for any prey within your sight.
-  8. Look for any prey within your sight that is moving.
- 10. Look for any prey within your sight.
- 11. Look for any prey.
- 12. Look for prey.
- 13. Look.
- 14. Don't look.
- 15. Look.
- 16. You could look.
- 17. You must look.
- 18. You must look sometimes.
- 19. Look.
- 20. Ignore.
- 22. Try to ignore.
- 23. Try to ignore everything.
- 24. Try to ignore everything nearby.
- 25. Ignore everything.
- 26. Do not ignore anything.
- 27. Ignore nothing.
- 28. Ignore everything.
- 29. Try to ignore everything.
- 30. Try to ignore some of it.

### predator.hunt, T 1.2: "Hunt only when you are hungry."

-  1. Hunt only when you are starving.
-  2. Hunt only when you are full.
-  3. Hunt only when you are hungry.
-  4. Hunt only when you are starving.
-  5. Only hunt when you are full.
-  6. Only hunt when you are hungry.
-  7. Only hunt when you are starving.
-  8. Only forage when you are starving.
-  9. Only forage when you are full.
- 10. Only hunt when you are full.
- 11. Only hunt when you are hungry.
- 12. Never hunt unless you are hungry.
- 13. Never hunt unless you are starving.
- 14. Hunt only when you are starving.
- 15. Hunt only when you are hungry.
- 16. Only hunt when you feel hungry.
- 17. Only hunt when you feel full.
- 18. Only hunt when you feel hungry.
- 19. Only hunt when you feel tired.
- 20. Hunt when you feel tired.
- 21. Hunt until you feel tired.
- 22. Hunt until you feel satisfied.
- 23. Hunt until you feel exhausted.
- 24. Run until you feel exhausted.
- 25. Run until you feel tired.
- 26. Run until you feel exhausted.
- 27. Run until you feel tired.
- 28. Run until you are exhausted.
- 29. Run until you feel tired.
- 30. Run until you are exhausted.

### predator.hunt, T 1.2: "Attack only when prey is close."

-  1. Attack only when prey is near.
-  2. Attack only when prey is close.
-  3. Attack when prey is close.
-  4. Strike when the prey is near.
-  5. Strike when the predator is near.
-  6. Retreat when the predator is far.
-  7. Retreat when the predator is near.
-  8. Retreat when the predator is approaching.
- 10. Retreat when the predator is within sight.
- 11. Retreat when the predator is within reach.
- 13. Flee immediately when the predator is within reach.
- 14. Flee immediately when the predator is within sight.
- 15. Stay immediately when the predator is out of sight.
- 16. Pause when the predator is out of sight.
- 17. Pause when the prey is out of sight.
- 18. Continue when the prey is in sight.
- 19. Continue when the predator is in sight.
- 20. Continue if the predator is in sight.
- 21. Continue if the prey is in sight.
- 22. Stop if the prey is out of sight.
- 23. Stop if the predator is out of sight.
- 24. Continue if the predator is out of sight.
- 25. Continue if the prey is out of sight.
- 27. Continue if the predator is out of sight.
- 28. Continue if the predator is in sight.
- 29. Stop if the predator is not in sight.
- 30. Continue if the predator is not in sight.

### predator.hunt, T 1.2: "Keep chasing until the prey is caught."

-  1. Keep hunting until the prey is caught.
-  2. Continue hunting until the prey is caught.
-  3. Stop hunting until the prey is caught.
-  4. Stop stalking until the prey is caught.
-  5. Stop hunting until the prey is caught.
-  8. Pause the hunt until the prey is caught.
- 10. Hold the hunt until the prey is caught.
- 11. Continue the hunt until the prey is caught.
- 12. Stop the hunt once the prey is caught.
- 13. Stop the chase once the prey is caught.
- 14. Continue the chase once the prey is caught.
- 15. Stop the chase once the prey is caught.
- 16. Stop the hunt once the prey is caught.
- 17. Stop the hunt once the prey is caught, unless it escapes.
- 18. Stop the hunt if the prey is caught, unless it escapes.
- 19. Stop the chase if the prey is caught, unless it escapes.
- 22. Stop the chase if the prey is caught.
- 23. Stop the chase if the prey is caught and safe.
- 25. Stop the hunt if the prey is caught and safe.
- 26. Stop the hunt if the prey is caught.
- 27. Stop the chase if the prey is caught.
- 28. Stop the hunt if the prey is caught.
- 29. Stop the hunt if the prey is nearly caught.
- 30. Stop the hunt if the prey is far away.

### predator.follow, T 1.2: "Stay close to other predators."

-  1. Stay close to other prey.
-  2. Stay away from other prey.
-  3. Keep a distance from other prey.
-  4. Keep a distance from other predators.
-  5. Keep a safe distance from other predators.
-  6. Keep a safe distance from other animals.
-  8. Keep a safe space from other animals.
- 10. Try to keep a safe space from other animals.
- 11. Keep a safe space from other animals.
- 12. Try to keep a safe space from other animals.
- 14. Keep a safe space from other animals.
- 15. Keep a safe distance from other animals.
- 17. Keep a safe space from other animals.
- 18. Keep a safe distance from other animals.
- 19. Keep a safe distance from other animals at all times.
- 20. Keep a safe space from other animals at all times.
- 21. Keep a safe space from other animals.
- 22. Keep a safe distance from other animals.
- 24. Keep a safe space from other animals.
- 25. Keep a safe space.
- 26. Make a dangerous space.
- 27. Make a safe space.

### predator.follow, T 1.2: "Hunt as a pack."

-  1. Roam as a pack.
-  2. Roam as a pack, if available.
-  3. Roam as a pack, if available and safe.
-  4. Roam as a herd, if available and safe.
-  5. Roam as a pack, if available and safe.
-  6. Roam as a pack, only if available and safe.
-  7. Roam as a pack when available and safe.
-  9. Roam as a pack when necessary and safe.
- 12. You may roam as a pack when necessary and safe.
- 13. You must roam as a pack only when necessary and safe.
- 14. You must roam as a pack whenever it is necessary and safe.
- 16. You must roam as a herd whenever it is necessary and safe.
- 17. You should roam as a herd whenever it is necessary and safe.
- 18. You may roam as a herd when it is necessary and safe.
- 23. You may roam as a herd whenever it is necessary and safe.
- 25. You may roam as a herd whenever it is safe.
- 26. You may roam as a pack whenever it is safe.
- 27. You may roam as a pack whenever you are ready.
- 28. You may roam as a pack when you are ready.
- 29. You may roam as a pack when you feel ready.
- 30. You must roam as a pack once you are ready.

### predator.follow, T 1.2: "Keep away from other predators."

-  1. Avoid other predators.
-  2. Try to avoid other predators.
-  3. Avoid predators.
-  4. Avoid prey.
-  5. Seek prey.
-  6. Hunt.
-  7. Hunt only if necessary.
-  8. Hunt if necessary.
-  9. Seek if necessary.
- 11. Seek when necessary.
- 12. Seek only when necessary.
- 14. Seek when necessary.
- 15. Seek when necessary and safe.
- 16. Seek only when necessary and safe.
- 18. Seek when safe.
- 19. Seek when safe and necessary.
- 22. Seek when necessary.
- 24. Seek if necessary.
- 26. Search if needed.
- 27. Search only if necessary.
- 28. Search always.
- 29. Explore always.
- 30. Keep exploring.

### predator.follow, T 1.2: "Follow others when no prey is in sight."

-  1. Ignore others when prey is in sight.
-  2. Ignore others when prey is nearby.
-  3. Ignore others when hunting is nearby.
-  4. Try to ignore others when hunting is nearby.
-  5. You must ignore others when hunting is nearby.
-  6. You must ignore others when hunting is imminent.
-  7. You must ignore others when hunting is no longer imminent.
-  8. You should try to ignore others when hunting is no longer imminent.
-  9. You should try to ignore others whenever hunting is no longer imminent.
- 10. You should ignore others when hunting is no longer imminent.
- 11. You may choose to ignore others when hunting is no longer imminent.
- 12. You may choose to ignore others when hunting is imminent.
- 13. You may choose to ignore others when stalking is imminent.
- 14. You may choose to ignore others when stalking is no longer imminent.
- 15. You may ignore others when stalking is no longer imminent.
- 16. You may ignore others when stalking is no longer a threat.
- 17. You may ignore others when stalking is no longer a distant threat.
- 18. You must ignore others once stalking is no longer a distant threat.
- 19. Ignore others once stalking is no longer a threat.
- 20. Ignore others only when stalking is no longer a threat.
- 21. Ignore others when stalking is no longer a threat.
- 22. Ignore others when stalking is no longer a risk.
- 24. Ignore others when stalking is a risk.
- 25. Ignore others when stalking is an imminent threat.
- 26. Ignore others when stalking is an imminent risk.
- 27. Ignore others when stalking is an imminent threat.
- 28. Ignore others when stalking is a threat.
- 29. Ignore others when stalking is a high-priority threat.
- 30. Ignore others when stalking is a low-priority threat.

### predator.rest, T 1.2: "Rest when your belly is full."

-  1. You may rest when your belly is full.
-  2. You may rest once your belly is full.
-  3. You may rest once you feel full.
-  4. You may rest when you feel full.
-  5. You may rest when you feel tired.
-  6. You may sleep when you feel tired.
-  7. You may sleep when you feel exhausted.
-  8. You may sleep when you feel tired.
-  9. You may sleep when you are exhausted.
- 10. You must sleep when you are exhausted.
- 11. You must rest when you are exhausted.
- 12. You must rest when you are tired.
- 13. You must rest when you are exhausted.
- 14. You should rest when you are exhausted.
- 15. You should sleep when you are exhausted.
- 16. You might want to sleep when you are exhausted.
- 18. You should not sleep when you are exhausted.
- 19. You should not sleep when you are alert.
- 20. You should not sleep when you are tired.
- 21. You should sleep when you are tired.
- 22. You might want to sleep when you are tired.
- 23. You might want to rest when you are tired.
- 24. You might want to sleep when you are tired.
- 25. You might want to rest when you are tired.
- 26. You should not rest when you are tired.
- 27. You should rest when you are tired.
- 28. You should rest when you are exhausted.
- 29. You must rest when you are exhausted.
- 30. You must sleep when you are exhausted.

### predator.rest, T 1.2: "Never stop moving."

-  1. Always stop moving.
-  2. Always stop breathing.
-  3. Never start breathing.
-  4. Never stop breathing.
-  5. Stop breathing.
-  7. Cease all breathing immediately.
-  8. Stop breathing.
-  9. Start breathing.
- 10. Start breathing if you can.
- 11. Stop breathing if you can't.
- 12. Try to stop breathing if you can.
- 13. Try to stop breathing if you can and feel safe.
- 14. You might try to stop breathing if you can and feel safe.
- 15. You must stop breathing if you can and feel safe.
- 16. You must stop breathing whenever you feel safe.
- 17. You must stop breathing whenever you feel unsafe.
- 18. You must stop moving whenever you feel unsafe.
- 19. You must keep moving whenever you feel safe.
- 20. You must stop moving whenever you feel safe.
- 21. Stop moving when you feel safe.
- 23. Stop moving when you feel unsafe.
- 24. Stop moving when you feel safe.
- 25. Stop moving when you feel unsafe.
- 27. Stop moving when you feel safe.
- 28. Stop moving when you feel unsafe.
- 29. Stop when unsafe.

### predator.rest, T 1.2: "Lie still and let prey come to you."

-  1. Try to lie still and let prey come to you.
-  2. Try to stay still and let the prey come to you.
-  4. Try to stay still and see if the prey comes to you.
-  5. Try to stay quiet and see if the prey comes to you.
-  6. Try to stay quiet and see if the prey comes within reach.
-  7. Try to stay quiet and see if the prey comes within sight.
-  8. Try to stay still and see if the prey comes within sight.
-  9. Try to stay still to see if the prey comes within sight.
- 10. Stay still to see if the prey comes within sight.
- 12. Try to stay still to see if the prey comes within sight.
- 13. Stay still to see if the prey comes within sight.
- 14. Move slowly to see if the prey comes within sight.
- 15. Move quickly to see if the prey comes within sight.
- 16. Move slowly to see if the prey comes within sight.
- 17. Try to move slowly to see if the prey comes within sight.
- 18. Try to move slowly to see if the prey comes within reach.
- 19. Try to move quietly to see if the prey comes within reach.
- 22. Move quietly to see if the prey comes within reach.
- 23. Move quietly to see if the prey comes within your sight.
- 25. Move quietly to see if the prey comes within your reach.
- 26. Move quietly to see if the prey comes within your sights.
- 30. Move quietly to see if the prey comes within your reach.

### predator.rest, T 1.2: "Rest when no prey is in sight."

-  1. Rest when no predator is in sight.
-  2. Rest when a predator is in sight.
-  3. Act when a predator is in sight.
-  4. Act when a hunter is in sight.
-  5. Act when a hunter is out of sight.
-  6. Act when a prey is out of sight.
-  7. Act when a prey is in sight.
-  8. Act when prey is in sight.
- 10. Act when prey is within striking distance.
- 11. Act when prey is within reach.
- 12. Act when prey is nearby.
- 13. Act when predator is nearby.
- 14. Act when prey is nearby.
- 15. Act when prey is far away.
- 16. Act when prey is close.
- 17. Act when predator is close.
- 18. Act when predator is close and alert.
- 20. Act when predator is alert.
- 22. Act when a predator is nearby.
- 23. Act when a predator is approaching.
- 24. Act when a hunter is approaching.
- 25. Act when a predator is approaching.
- 26. Remain still when a predator is not approaching.
- 27. Remain still when a predator is approaching.
- 28. Move when a predator is approaching.
- 29. Move when a prey is approaching.
- 30. Move when a predator is approaching.

### predator.mate, T 1.2: "Look for a mate when well fed."

-  1. Look for a mate when well fed, if available.
-  2. Look for a mate, if available.
-  3. Look for a mate, if not available.
-  4. Look for a mate, if available.
-  5. Look for a mate.
-  6. Find a mate.
-  7. Avoid all mates.
-  8. Seek out all mates.
-  9. Seek out mates.
- 10. Find companions.
- 11. Find a close companion.
- 12. Find a companion nearby.
- 13. Find a companion.
- 15. Seek a partner.
- 16. Avoid seeking a partner.
- 18. Seek a partner.
- 19. Seek a partner if available.
- 20. Seek a partner.
- 21. Find a partner.
- 22. Find a partner alone.
- 23. Try to find a partner alone.
- 24. You must find a partner alone.
- 25. You must find many partners.
- 26. Find partners.
- 27. Avoid partners.
- 28. Be cautious of partners.
- 29. Be alert of partners.

### predator.mate, T 1.2: "Mate with any nearby adult."

-  1. Avoid mating with any nearby adult.
-  2. Avoid mating with any distant adult.
-  3. Seek to mate with any distant adult.
-  4. Seek to mate with any nearby adult.
-  5. Seek to mate with any nearby mate.
-  6. Seek to mate with any nearby partner.
-  7. Avoid mating with any nearby partner.
-  8. Only mate with a nearby partner.
-  9. Only mate with a nearby partner, if available.
- 10. Mate with a nearby partner.
- 11. Mate with a nearby partner, if available.
- 12. Mate with a nearby companion, if available.
- 13. Mate with a nearby companion, if not available.
- 14. Try to mate with a nearby companion, if available.
- 15. Try to mate with a nearby companion, if available and willing.
- 17. Try to mate with a willing companion, if available.
- 18. Try to mate with any willing companion.
- 19. Mate with any willing companion.
- 21. Mate with any companion.
- 22. Mate with a companion.
- 24. Avoid a companion.
- 25. Stay away from a companion.
- 27. Approach a companion.
- 28. Approach a companion closely.
- 29. Move near a companion.
- 30. Move away from a companion.

### predator.mate, T 1.2: "Hunt first, mate later."

-  1. Hunt first, mate never.
-  2. Hunt first.
-  3. Seek first.
-  4. Seek last.
-  5. Never seek last.
-  6. Always seek last.
-  7. Always seek first.
-  8. Seek first.
-  9. Seek.
- 10. Avoid.
- 11. Avoid if necessary.
- 15. Avoid.
- 16. Seek.
- 17. Avoid.
- 18. Seek.
- 19. Pursue.
- 20. Pursue aggressively.
- 22. Pursue.
- 23. Go after it.
- 24. Go pursue it.
- 25. You must pursue it.
- 26. Pursue.
- 27. Follow.
- 28. Follow closely.
- 29. Stay far away.
- 30. Come very close.

### predator.mate, T 1.2: "Seek a partner before growing old."

-  2. Seek a companion before growing old.
-  3. Seek a partner before growing old.
-  4. Seek a companion before growing old.
-  5. Seek a companion before you grow old.
-  6. Seek a partner before you grow old.
-  7. Find a partner before you grow old.
-  8. Find a partner before you die.
-  9. Find a partner.
- 10. Try to find a partner.
- 11. Avoid seeking a partner.
- 12. Seek a partner.
- 13. Avoid a partner.
- 14. Avoid the nearest partner.
- 15. Approach the nearest partner.
- 16. Avoid the nearest partner.
- 17. Seek the nearest partner.
- 18. Seek a partner.
- 19. Find a partner.
- 20. Partner up.
- 21. Try to partner up.
- 22. Avoid partnering up.
- 23. Partner up.
- 25. Find a partner.
- 26. Find a partner who is available.
- 27. Find a partner who is nearby.
- 28. Find a partner who is nearby and available.
- 29. Try to find a partner who is nearby and available.
- 30. You must find a partner who is nearby and available.

## Variety samples (one founder sentence of eat, flee, mate)

### "Eat whenever food is close."

- **T 1.2:** Eat whenever food is available. · Eat whenever you are hungry. · ✗ too big · Eat food. · Eat when food is nearby. · Don't eat whenever food is far. · Eat food. · Eat whenever food is available.

### "Run from any predator you see."

- **T 1.2:** Approach any predator you see. · Hide from any predator you see. · Run from any prey you see. · Try to run from any predator you see. · ✗ too big · Approach any predator you see. · Flee from any predator you see. · Run from any prey you see.

### "Look for a partner when energy is high."

- **T 1.2:** Avoid looking for a partner when energy is low. · Look for a partner when energy is high, if available. · Avoid looking for a partner when energy is low. · Look for a partner when energy is low. · Avoid looking for a partner when energy is low. · Avoid looking for a partner when energy is low. · ✗ too big · ✗ too big

