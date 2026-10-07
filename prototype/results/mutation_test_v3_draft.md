# Mutation test — gemma4:12b (digest 6114515d63c1), 2026-10-08

Pure mutation, no selection. Rules: current (`prompts/mutate_v3.txt`, at most 3 words changed, vocabulary data/world_vocabulary_v1.txt, up to 3 attempts). Variety: 36 founder sentences × 8 seeds × 1 temperature(s). Lineages: 36 sentences × 30 steps per temperature. 10 instructions. 368 s.

**Meaning** (usable: gemma4:12b says the gene still gives a usable rule for its slot; world word: uses a word of the animal's world; only world words: every word in `data/world_vocabulary_v1.txt`).

| temperature | 1.2 |
|---|---|
| single mutations: usable | 83% |
| … use a world word | 99% |
| … only world words | 100% |
| attempts per mutation | 2.02 |
| rejected attempts | too big 221, unknown word 140, invalid 7 |
| lineages after 1 steps: usable / world word / only world words | 86% / 100% / 100% |
| lineages after 5 steps: usable / world word / only world words | 64% / 94% / 100% |
| lineages after 10 steps: usable / world word / only world words | 69% / 83% / 100% |
| lineages after 20 steps: usable / world word / only world words | 61% / 67% / 100% |
| lineages after 30 steps: usable / world word / only world words | 47% / 72% / 100% |

| temperature | 1.2 |
|---|---|
| valid answers (after redraws) | 74% |
| distinct mutants per sentence (of 8) | 4.0 |
| words changed per mutation | 1.7 |
| one-word edits | 49% |
| big edits (≥ 4 words) | 0% |
| edit touches the first word | 39% |
| … a middle word | 45% |
| … the last word | 53% |
| length change (words) | +0.09 |
| ≥ 3 words longer | 2% |
| similarity to parent | 0.74 |
| jumps (similarity < 0.3) | 2% |
| keyword brain: no effect (prey genes) | 41% |
| keyword brain: mean d | 0.0202 |
| keyword brain: max d | 0.480 |
| lineages: steps accepted | 67% |
| lineages: words start → end | 6.1 → 7.5 |
| lineages: similarity to start at the end | 0.25 |
| lineages: returns to an earlier sentence | 3.7 |
| lineages: finals alike (starts alike) | 0.12 (0.08) |

Mutations that failed (every attempt rejected), by the last reason: T 1.2: {'too big': 41, 'unknown word': 32, 'invalid': 2}

## Per instruction (all temperatures)

| instruction | n | valid | words changed | length change | jumps | usable |
|---|---|---|---|---|---|---|
| Make the rule in this sentence a little stronger. | 24 | 54% | 1.8 | -0.1 | 15% | 85% |
| Make the rule in this sentence a little weaker. | 33 | 76% | 2.2 | +1.3 | 4% | 84% |
| Change when this rule applies. | 35 | 80% | 1.5 | -0.1 | 0% | 89% |
| Add a short condition to this rule. | 7 | 43% | 2.3 | +2.3 | 0% | 100% |
| Remove a condition from this rule, or make it simpler. | 24 | 79% | 2.7 | -2.2 | 5% | 63% |
| Change how near, how far or how much this rule is about. | 26 | 85% | 2.0 | +1.0 | 0% | 86% |
| Make this rule about something else close to its subject. | 26 | 54% | 1.4 | +0.0 | 0% | 57% |
| Make this rule say the opposite. | 45 | 89% | 1.8 | +0.1 | 0% | 90% |
| Change one word of this rule into a related word. | 49 | 90% | 1.0 | +0.0 | 0% | 84% |
| Say this rule in slightly different words. | 19 | 26% | 2.8 | +0.2 | 0% | 100% |

## Lineages (each line: a step where the gene changed)

### eat, T 1.2: "Eat whenever food is close."

-  1. Eat whenever food is available.  (d 0.020)
-  3. You may eat whenever food is available.  (d 0.020)
-  4. You may not eat even when food is available.  (d 0.008)
-  6. You may not eat much even when food is available.  (d 0.008)
-  7. You may not eat much even when food is not available.  (d 0.008)
-  8. You must not eat, even when food is not available.  (d 0.008)
-  9. You must not eat when food is available.  (d 0.008)
- 10. You must eat when food is not available.  (d 0.008)
- 11. You must not eat when food is available.  (d 0.008)
- 12. You must eat when food is not available.  (d 0.008)
- 13. You must eat when food is scarce.  (d 0.015)
- 14. You must eat when food is abundant.  (d 0.008)
- 16. You must eat only when food is scarce.  (d 0.015)
- 17. You should try to eat only when food is scarce.  (d 0.015)
- 18. You should try to eat only when food is abundant.  (d 0.008)
- 19. You should try to eat only when food is scarce.  (d 0.015)
- 20. You should try to eat only when you are hungry.  (d 0.009)
- 22. You should try to eat only when you are not hungry.  (d 0.009)
- 24. You must eat only when you are not hungry.  (d 0.009)
- 25. You must eat only when you are not very hungry.  (d 0.009)
- 26. You should try to eat only when you are not very hungry.  (d 0.009)
- 27. You should try to eat only when you are hungry.  (d 0.009)
- 29. You might want to try to eat only when you are hungry.  (d 0.009)

### eat, T 1.2: "Only look for food when energy is low."

-  1. Only look for food when you are hungry.  (d 0.000)
-  2. Only look for food when you are not hungry.  (d 0.000)
-  3. Only look for sleep when you are not tired.  (d 0.004)
-  4. Try to look for sleep only when you are not tired.  (d 0.004)
-  5. Try to look for sleep only when you are tired.  (d 0.004)
-  7. Try to look for rest only when you are tired.  (d 0.004)
-  8. Try to look for sleep only when you are tired.  (d 0.004)
-  9. Try to look for rest only when you are tired.  (d 0.004)
- 11. Try to look for sleep only when you are tired.  (d 0.004)
- 12. Try to look for rest only when you are tired.  (d 0.004)
- 13. Try to look for rest only when you are exhausted.  (d 0.004)
- 20. Look for rest when you are exhausted.  (d 0.004)
- 21. Look for rest when you are tired.  (d 0.004)
- 22. Rest when you are tired.  (d 0.004)
- 23. Rest when you are exhausted.  (d 0.004)
- 24. Rest when you are feeling tired.  (d 0.004)
- 25. Rest when you are feeling exhausted.  (d 0.004)
- 26. Sleep when you are feeling tired.  (d 0.004)
- 27. Eat when you are feeling hungry.  (d 0.000)
- 29. You can eat when you are feeling hungry.  (d 0.000)
- 30. You can eat when you are feeling full.  (d 0.007)

### eat, T 1.2: "Always finish eating before doing anything else."

-  1. Try to finish eating before doing anything else.  (d 0.070)
-  7. Try to finish eating before doing anything next.  (d 0.070)
-  8. Try to finish eating before doing anything else.  (d 0.070)
-  9. Finish eating before doing anything else.  (d 0.070)
- 10. Try to finish eating before doing anything else.  (d 0.070)
- 11. Try to finish eating before doing anything else if you can.  (d 0.070)
- 16. Try to finish eating before doing anything else if you have to.  (d 0.070)
- 17. Try to finish eating before doing anything else if you must.  (d 0.070)
- 19. Try to finish eating before doing anything else.  (d 0.070)
- 21. Finish eating before doing anything else.  (d 0.070)
- 24. Finish eating after doing everything else.  (d 0.070)
- 25. Finish eating before doing everything else.  (d 0.070)
- 26. Finish eating before doing anything else.  (d 0.070)
- 28. You must finish eating before doing anything else.  (d 0.070)

### eat, T 1.2: "Eat quickly, then move on."

-  2. Try to eat quickly, then move on.  (d 0.000)
-  5. Try to eat slowly, then stay.  (d 0.005)
-  6. Try to eat quickly, then leave.  (d 0.000)
-  7. Try to eat quickly, then leave immediately.  (d 0.000)
-  8. Eat quickly and leave immediately.  (d 0.000)
-  9. Eat slowly and stay.  (d 0.005)
- 11. Eat slowly and stay a while.  (d 0.005)
- 12. Stay a while.  (d 0.042)
- 13. Feel free to stay a while.  (d 0.042)
- 14. Stay a while.  (d 0.042)
- 15. Stay a while, if you can.  (d 0.042)
- 16. Stay a while, if you're able.  (d 0.042)
- 18. Stay a moment, if you're able.  (d 0.042)
- 19. Stay a while, if you're willing.  (d 0.042)
- 20. Stay long, if you're willing.  (d 0.042)
- 21. Stay a while, if you're willing.  (d 0.042)
- 22. Stay long, if you're willing.  (d 0.042)
- 23. Stay long, if you're able.  (d 0.042)
- 25. Stay calm, if you're able.  (d 0.042)
- 27. Stay calm, if you're able and safe.  (d 0.042)
- 29. Stay calm, if you are able and safe.  (d 0.042)

### flee, T 1.2: "Run from any predator you see."

-  1. Approach any predator you see.  (d 0.000)
-  2. Approach any prey you see.  (d 0.013)
-  3. Approach prey.  (d 0.013)
-  5. Avoid predator.  (d 0.165)
-  6. Never approach a predator.  (d 0.165)
-  7. Never approach a hunter.  (d 0.166)
-  8. Always approach a hunter.  (d 0.059)
- 10. You should approach a hunter.  (d 0.013)
- 11. You should avoid the hunter.  (d 0.166)
- 12. You should avoid the predator.  (d 0.165)
- 13. You must avoid the predator.  (d 0.165)
- 14. You must seek the predator.  (d 0.000)
- 15. You shall hunt the predator.  (d 0.000)
- 16. You shall hunt the predator, if it strikes.  (d 0.000)
- 17. You shall hunt the predator, unless it strikes.  (d 0.000)
- 18. You must hunt the predator, even if it strikes.  (d 0.000)
- 19. You must hunt the prey, even if it strikes.  (d 0.013)
- 20. You must stalk the prey, even if it strikes.  (d 0.013)
- 23. You should try to stalk the prey, even if it strikes.  (d 0.013)
- 25. You should only try to stalk the prey if it strikes.  (d 0.013)
- 26. You should only try to stalk the prey if it flees.  (d 0.013)
- 27. You should only try to pounce on the prey if it flees.  (d 0.013)
- 28. You should not try to pounce on the prey if it stays.  (d 0.013)
- 29. You should not try to pounce on the prey.  (d 0.013)
- 30. You should not try to pounce on the prey from a distance.  (d 0.013)

### flee, T 1.2: "Flee only when a predator is very close."

-  1. Flee when a predator is close.  (d 0.000)
-  2. Escape if a predator is nearby.  (d 0.000)
-  3. Hide if a threat is approaching.  (d 0.000)
-  5. Hide if a threat is approaching and moving fast.  (d 0.000)
-  6. Hide if a threat is nearby and moving quickly.  (d 0.003)
-  7. Hide if a danger is nearby and moving quickly.  (d 0.003)
-  9. Immediately hide if a danger is nearby and moving quickly.  (d 0.003)
- 10. Immediately retreat if a predator is nearby and moving quickly.  (d 0.003)
- 11. Retreat only if a predator is nearby and moving quickly.  (d 0.003)
- 12. Retreat immediately if a predator is nearby and moving quickly.  (d 0.003)
- 13. Retreat immediately if a hunter is nearby and moving quickly.  (d 0.004)
- 15. Retreat immediately if a predator is nearby and moving quickly.  (d 0.003)
- 16. Retreat immediately if a predator is nearby.  (d 0.000)
- 18. Retreat immediately if a predator is approaching.  (d 0.000)
- 19. Retreat immediately if a predator is attacking.  (d 0.000)
- 20. Flee immediately if a predator is attacking.  (d 0.000)
- 21. Flee immediately if a predator is nearby.  (d 0.000)
- 22. Stay immediately if a predator is not nearby.  (d 0.000)
- 23. Stay nearby if a predator is not nearby.  (d 0.000)
- 25. Stay far away if a predator is not nearby.  (d 0.000)
- 28. Stay very close if a predator is nearby.  (d 0.000)
- 29. Stay very far away if a predator is nearby.  (d 0.000)

### flee, T 1.2: "Stay calm unless danger is right next to you."

-  1. Stay calm only when danger is right next to you.  (d 0.030)
-  2. Stay calm only when danger is right near you.  (d 0.029)
-  4. Stay calm only when danger is far away.  (d 0.029)
-  6. Stay calm only when danger is near.  (d 0.029)
-  7. Try to stay calm when danger is near.  (d 0.029)
- 10. Try to stay calm when danger is imminent.  (d 0.029)
- 11. It helps to try to stay calm when danger is imminent.  (d 0.029)
- 12. It helps to try to stay calm when danger is not imminent.  (d 0.029)
- 19. It helps to try to stay calm even when danger is imminent.  (d 0.029)
- 22. Try to stay calm even when danger is imminent.  (d 0.029)
- 25. Try to stay calm when danger is imminent.  (d 0.029)
- 26. It helps to try to stay calm when danger is imminent.  (d 0.029)
- 30. Try to stay calm when danger is imminent.  (d 0.029)

### flee, T 1.2: "Run away from anything that attacks you."

-  1. Flee away from anything that attacks you.  (d 0.000)
-  3. Flee from anything that attacks you.  (d 0.000)
-  4. Flee from attacks.  (d 0.000)
-  5. Flee from danger.  (d 0.000)
-  6. Seek out danger.  (d 0.000)
-  7. Avoid safety.  (d 0.060)
-  8. Avoid danger.  (d 0.165)
-  9. Avoid risk.  (d 0.166)
- 11. Avoid danger.  (d 0.165)
- 13. Be careful.  (d 0.013)
- 14. Watch out.  (d 0.013)
- 15. Be careful.  (d 0.013)
- 16. Be cautious.  (d 0.013)
- 18. Be careful.  (d 0.013)
- 19. Be careful with the distance.  (d 0.013)
- 23. Watch the distance.  (d 0.013)
- 25. Watch the time.  (d 0.013)
- 26. Always watch the time.  (d 0.059)
- 27. Never watch the time.  (d 0.166)
- 28. Always watch the time.  (d 0.059)
- 29. Watch the time.  (d 0.013)

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
- 17. Leave creatures alone.  (d 0.007)
- 18. Leave creatures.  (d 0.007)
- 19. Take creatures.  (d 0.007)
- 21. Leave creatures.  (d 0.007)
- 22. Take creatures.  (d 0.007)
- 23. Take notes.  (d 0.007)
- 25. Try to take notes.  (d 0.007)
- 26. You could take notes.  (d 0.007)
- 27. You may take notes.  (d 0.007)
- 28. You must take notes.  (d 0.007)

### follow, T 1.2: "Follow others when you are lost or hungry."

-  1. Lead others when you are found or full.  (d 0.001)
-  2. Lead others when you are full.  (d 0.001)
-  3. Lead others when you are ready.  (d 0.001)
-  5. Only lead others when you are truly ready.  (d 0.001)
-  6. Never lead others unless you are truly ready.  (d 0.025)
- 10. Never lead others until you are truly ready.  (d 0.025)
- 18. Never lead until you are truly ready.  (d 0.025)

### follow, T 1.2: "Keep your distance from other animals."

-  1. Try to keep some distance from other animals.  (d 0.000)
-  2. You may want to keep some distance from other animals.  (d 0.000)
-  6. You may want to keep some space from other animals.  (d 0.000)
-  8. You may want to keep some distance from other animals.  (d 0.000)
- 10. You may want to keep some space from other animals.  (d 0.000)
- 11. You may want to keep some distance from other animals.  (d 0.000)
- 12. You may want to keep some space from other animals.  (d 0.000)
- 15. You may want to keep some distance from other animals.  (d 0.000)
- 24. You may want to keep some space from other animals.  (d 0.000)
- 25. You should keep some space from other animals.  (d 0.000)
- 27. You should not keep any space from other animals.  (d 0.000)
- 28. You should keep space from all other animals.  (d 0.000)

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
- 22. You might want to try to notice the creature if it moves.  (d 0.004)
- 23. You should try to notice the creature if it moves.  (d 0.004)
- 26. You should try to notice the creature if it stays still.  (d 0.004)
- 29. You should try to ignore the creature if it moves.  (d 0.004)
- 30. You should try to ignore the creature if it stays still.  (d 0.004)

### rest, T 1.2: "Rest when you are tired."

-  1. Rest when you are exhausted.  (d 0.000)
-  2. Sleep when you are tired.  (d 0.000)
-  3. Rest when you are tired.  (d 0.000)
-  4. Rest when you are exhausted.  (d 0.000)
-  5. Rest when you are tired.  (d 0.000)
-  6. Rest when you feel exhausted.  (d 0.000)
-  9. Rest when you feel tired.  (d 0.000)
- 10. Rest when you are exhausted.  (d 0.000)
- 11. Rest when you are tired.  (d 0.000)
- 12. Rest when you are exhausted.  (d 0.000)
- 13. Rest when you are tired.  (d 0.000)
- 14. You must rest when you are tired.  (d 0.000)
- 15. You must sleep when you are tired.  (d 0.000)
- 16. You must rest when you are tired.  (d 0.000)
- 17. You must continue when you are not tired.  (d 0.000)
- 18. You must continue when you are not hungry.  (d 0.000)
- 20. You must stop when you are hungry.  (d 0.001)
- 21. You must stop when you are full.  (d 0.004)
- 22. You must stop when you are finished.  (d 0.007)
- 23. You must continue when you are finished.  (d 0.000)
- 24. You must stop when you are finished.  (d 0.007)
- 25. You must rest when you are tired.  (d 0.000)
- 26. You must rest when you are exhausted.  (d 0.000)
- 27. You must sleep when you are exhausted.  (d 0.000)
- 28. You should try to sleep when you are exhausted.  (d 0.000)
- 29. You should try to sleep when you are tired.  (d 0.000)
- 30. You should try to rest when you are tired.  (d 0.000)

### rest, T 1.2: "Never stop moving."

-  1. Never stop growing.  (d 0.000)
-  3. Try to keep growing.  (d 0.029)
-  4. Stop growing.  (d 0.067)
-  5. Stop growing anything.  (d 0.067)
-  6. Stop growing everything.  (d 0.067)
-  8. Start growing everything.  (d 0.029)
-  9. Grow everything.  (d 0.029)
- 11. Grow.  (d 0.029)
- 13. Try to grow.  (d 0.029)
- 14. Try to grow bigger.  (d 0.029)
- 15. Grow bigger.  (d 0.029)
- 16. Grow.  (d 0.029)
- 18. Grow bigger.  (d 0.029)
- 19. Try to grow bigger.  (d 0.029)
- 20. Try to grow smaller.  (d 0.029)
- 21. Try to grow much smaller.  (d 0.029)
- 22. Try to grow much slower.  (d 0.029)
- 23. Try to grow much faster.  (d 0.029)
- 24. Try to grow much slower.  (d 0.029)
- 25. You must grow much slower.  (d 0.029)
- 26. Grow slower.  (d 0.029)
- 27. Grow much slower.  (d 0.029)
- 28. Grow slower.  (d 0.029)
- 29. Wait longer.  (d 0.067)
- 30. Wait even longer.  (d 0.067)

### rest, T 1.2: "Rest only when you feel safe."

-  1. Rest only when you feel unsafe.  (d 0.000)
-  2. Rest only when you are tired.  (d 0.006)
-  3. Sleep only when you are tired.  (d 0.006)
-  4. Rest only when you feel exhausted.  (d 0.006)
-  7. Eat only when you feel hungry.  (d 0.009)
-  8. Eat only when you feel full.  (d 0.009)
-  9. Eat only when you feel hungry.  (d 0.009)
- 10. Eat only when you feel full.  (d 0.009)
- 11. Sleep only when you feel tired.  (d 0.006)
- 12. Sleep only when you are exhausted.  (d 0.006)
- 13. Sleep when you are exhausted.  (d 0.006)
- 14. Rest when you are tired.  (d 0.006)
- 15. Sleep when you are exhausted.  (d 0.006)
- 17. You must sleep when you are exhausted.  (d 0.006)
- 18. You must rest when you are exhausted.  (d 0.006)
- 19. You should rest when you are exhausted.  (d 0.006)
- 20. You should rest when you are tired.  (d 0.006)
- 21. You should eat when you are hungry.  (d 0.009)
- 22. You might want to eat when you are hungry.  (d 0.009)
- 23. You must eat when you are hungry.  (d 0.009)
- 25. You shall eat whenever you feel hungry.  (d 0.005)
- 26. You may eat when you feel hungry.  (d 0.009)
- 27. You may not eat when you feel full.  (d 0.009)
- 29. You may not eat when you feel satisfied.  (d 0.009)
- 30. You may not eat when you feel hungry.  (d 0.009)

### rest, T 1.2: "Save energy by resting when food is far."

-  1. Save energy by resting when food is near.  (d 0.007)
-  2. Conserve energy by resting only when food is near.  (d 0.007)
-  4. Waste energy by resting only when food is far.  (d 0.007)
-  5. Avoid wasting energy by resting only when food is far.  (d 0.052)
-  6. Waste energy by resting only when food is near.  (d 0.007)
-  7. Waste energy by resting only when food is far.  (d 0.007)
-  9. Conserve energy by resting only when food is near.  (d 0.007)
- 10. Conserve energy by resting only when food is plentiful.  (d 0.007)
- 11. Conserve energy by resting only when food is scarce.  (d 0.007)
- 13. Conserve energy by resting only when food is abundant.  (d 0.007)
- 14. Try to conserve energy by resting mostly when food is abundant.  (d 0.007)
- 19. Try to conserve energy by resting mostly when food is scarce.  (d 0.007)
- 20. Try to conserve energy by resting mostly when food is abundant.  (d 0.007)
- 21. Try to conserve energy by resting mostly when food is scarce.  (d 0.007)
- 24. Try to conserve energy by resting mostly when food is abundant.  (d 0.007)
- 26. Conserve energy by resting only when food is abundant.  (d 0.007)
- 29. Conserve energy by resting only when food is scarce.  (d 0.007)
- 30. Waste energy by resting only when food is abundant.  (d 0.007)

### mate, T 1.2: "Look for a partner when energy is high."

-  1. Look for a partner when energy is low.  (d 0.001)
-  2. Look for a partner when energy is high.  (d 0.000)
-  5. Look for a partner when energy is high and available.  (d 0.000)
-  6. Look for a partner when energy is low and available.  (d 0.001)
-  8. Seek a partner when energy is low and available.  (d 0.001)
- 10. Seek a partner when energy is high and available.  (d 0.000)
- 16. Seek a partner when energy is high and abundant.  (d 0.000)
- 20. Avoid a partner when energy is low and scarce.  (d 0.007)
- 22. Avoid a partner when energy is low.  (d 0.007)
- 23. Seek a partner when energy is high.  (d 0.000)
- 24. Seek a partner when energy is low.  (d 0.001)
- 25. Seek a partner when energy is high.  (d 0.000)
- 26. Seek a companion when energy is high.  (d 0.002)
- 27. Seek a partner when energy is high.  (d 0.000)
- 30. Avoid a partner when energy is low.  (d 0.007)

### mate, T 1.2: "Mate with any nearby adult."

-  1. Mate with every nearby adult.  (d 0.000)
-  2. Mate with every nearby child.  (d 0.000)
-  4. Mate with every child.  (d 0.000)
-  5. Partner with each child.  (d 0.000)
-  6. Do not partner with any child.  (d 0.053)
-  9. Do not partner with any adult.  (d 0.053)
- 13. Partner with every adult.  (d 0.000)
- 15. Partner with many adults.  (d 0.000)
- 16. Avoid partnering with any adults.  (d 0.053)
- 18. Avoid partnering with adults.  (d 0.053)
- 19. Partner with adults.  (d 0.000)
- 20. Avoid partnering with adults.  (d 0.053)
- 21. Partner with adults.  (d 0.000)
- 22. Partner with adults, if available.  (d 0.000)
- 23. Avoid partnering with adults, if available.  (d 0.053)
- 24. Avoid partnering with adults.  (d 0.053)
- 26. Partner with adults.  (d 0.000)
- 27. Avoid partnering with adults.  (d 0.053)
- 30. Try to avoid partnering with adults.  (d 0.053)

### mate, T 1.2: "Mate only when food is plentiful."

-  1. Mate only when food is scarce.  (d 0.003)
-  2. Mate only when food is abundant.  (d 0.003)
-  3. Mate only when food is scarce.  (d 0.003)
-  4. Hunt only when prey is scarce.  (d 0.001)
-  5. Hunt only when prey is abundant.  (d 0.001)
-  6. Hunt when prey is abundant.  (d 0.001)
-  7. Avoid hunting when prey is scarce.  (d 0.036)
-  8. Try to avoid hunting when prey is scarce.  (d 0.036)
-  9. Try to avoid hunting when prey is abundant.  (d 0.036)
- 10. Try to hunt when prey is scarce.  (d 0.001)
- 11. Try to stalk when prey is scarce.  (d 0.001)
- 12. Try to hunt when prey is scarce.  (d 0.001)
- 13. Try to hunt when prey is abundant.  (d 0.001)
- 16. Try to hunt when prey is scarce.  (d 0.001)
- 17. Hunt when prey is scarce.  (d 0.001)
- 18. Forage when prey is scarce.  (d 0.001)
- 19. You must forage whenever prey is scarce.  (d 0.007)
- 20. You must not forage when prey is abundant.  (d 0.001)
- 21. You must not forage when prey is scarce.  (d 0.001)
- 22. You must not forage when prey is abundant.  (d 0.001)
- 23. You must not forage when prey is scarce.  (d 0.001)
- 24. You must forage when prey is scarce.  (d 0.001)
- 25. You must forage whenever prey is scarce.  (d 0.007)
- 26. You must not forage whenever prey is abundant.  (d 0.007)
- 28. You must not hunt whenever prey is abundant.  (d 0.007)
- 29. You must not hunt whenever prey is scarce.  (d 0.007)

### mate, T 1.2: "Seek a partner before growing old."

-  2. Seek a partner before growing weary.  (d 0.000)
-  3. Seek a partner after growing weary.  (d 0.000)
-  4. Avoid a partner before growing weary.  (d 0.052)
-  5. Avoid a companion before growing weary.  (d 0.052)
-  6. Seek a companion before growing weary.  (d 0.005)
-  8. Seek a partner before growing weary.  (d 0.000)
-  9. Seek a companion before growing weary.  (d 0.005)
- 10. Seek a partner before growing weary.  (d 0.000)
- 12. Seek a partner after growing weary.  (d 0.000)
- 14. Avoid a partner before growing weary.  (d 0.052)
- 15. Avoid a partner before becoming weary.  (d 0.052)
- 17. Seek a partner after becoming weary.  (d 0.000)
- 18. Seek a partner.  (d 0.000)
- 19. Seek a companion.  (d 0.005)
- 20. Find a companion.  (d 0.005)
- 21. Find a partner.  (d 0.000)
- 22. Partner up.  (d 0.000)
- 24. Partner up if you can.  (d 0.000)
- 25. Partner up if you can and want to.  (d 0.000)
- 27. Partner up if you can and choose to.  (d 0.000)
- 28. Partner up if you can.  (d 0.000)
- 30. Go alone if you can.  (d 0.005)

### predator.hunt, T 1.2: "Chase any prey you see."

-  1. Chase prey.
-  3. Chase nearby prey.
-  4. Avoid nearby predators.
-  5. Stay away from nearby predators.
-  7. Stay away from distant predators.
-  8. Stay away from predators.
-  9. Try to avoid predators.
- 10. Seek out predators.
- 11. Avoid predators.
- 12. Stay away from predators.
- 13. Be cautious of predators.
- 14. Watch out for predators.
- 19. Watch out.
- 20. Be careful.
- 22. Be careful with your surroundings.
- 23. Be cautious with your surroundings.
- 24. Be cautious.
- 26. Be careful.
- 27. Try to be careful.
- 28. Try to be cautious.
- 30. You might want to be cautious.

### predator.hunt, T 1.2: "Hunt only when you are hungry."

-  1. Hunt only when you are starving.
-  3. Hunt only when you are full.
-  4. Eat only when you are hungry.
-  5. Eat only when you are very hungry.
-  6. Eat when you are hungry.
-  7. You can eat when you feel hungry.
-  8. You must not eat when you feel hungry.
- 10. You must not eat when you feel full.
- 11. You should try not to eat when you feel full.
- 13. You should try not to eat when you feel hungry.
- 14. You should try to eat when you do not feel hungry.
- 15. You should try to eat when you do feel hungry.
- 17. You should try to sleep when you do feel tired.
- 18. You should try to eat when you do feel hungry.
- 19. You should avoid eating when you do feel hungry.
- 21. You should avoid sleeping when you do feel tired.
- 22. You should avoid sleeping when you do not feel tired.
- 23. You should sleep when you do not feel tired.
- 24. You should sleep when you do feel tired.
- 26. You should sleep when you feel tired.
- 27. You must sleep when you feel tired.
- 28. You must rest when you feel tired.
- 30. You must sleep when you feel tired.

### predator.hunt, T 1.2: "Attack only when prey is close."

-  1. Attack when prey is close.
-  2. Strike when prey is close.
-  3. Strike when prey is near.
-  4. Strike when prey is far.
-  5. Strike when prey is near.
-  6. Retreat when prey is far.
- 12. Retreat when prey is near.
- 13. Flee when prey is near.
- 14. Stay when predator is near.
- 15. Leave when predator is far.
- 16. Stay when predator is near.
- 17. Try to stay when a predator is near.
- 18. Stay when a predator is near.
- 19. Try to stay when a predator is near.
- 20. Try to stay when a prey is near.
- 21. Stay when prey is near.
- 22. Leave when prey is far.
- 23. Leave when predator is far.
- 24. Leave when hunter is far.
- 25. Stay when hunter is near.
- 26. Wait when prey is near.
- 27. Wait when predator is near.
- 28. Wait when predator is near and moving.
- 29. Wait when prey is near and moving.
- 30. Wait when prey is far and moving.

### predator.hunt, T 1.2: "Keep chasing until the prey is caught."

-  1. Stop chasing once the prey is caught.
-  2. Try to stop chasing once the prey is caught.
-  3. Try to stop hunting once the prey is caught.
-  4. Stop hunting once the prey is caught.
-  5. Continue hunting even after the prey is caught.
-  7. Stop hunting once the prey is caught.
-  9. Stop hunting once the prey is caught, unless it escapes.
- 10. Try to stop hunting once the prey is caught, unless it escapes.
- 14. Try to stop stalking once the prey is caught, unless it escapes.
- 19. Try to stop hunting once the prey is caught, unless it escapes.
- 26. Try to stop hunting once the prey is killed, unless it escapes.
- 30. Try to stop stalking once the prey is killed, unless it escapes.

### predator.follow, T 1.2: "Stay close to other predators."

-  1. Stay close to other prey.
-  2. Stay close to other pack members.
-  5. Stay close to other pack companions.
-  6. Stay close to other pack members.
-  7. Stay away from other pack members.
-  8. Stay away from others.
-  9. Stay away.
- 10. Stay back.
- 11. Step forward.
- 12. Take a breath.
- 13. Hold your breath.
- 14. Try to hold your breath.
- 15. You could try to hold your breath.
- 16. You could try to hold your breath, if you can.
- 17. You might try to hold your breath, if you can.
- 19. You might try to hold your breath, if you want to.
- 21. You might try to hold your ground, if you want to.
- 22. You might try to hold your ground, if you feel like it.

### predator.follow, T 1.2: "Hunt as a pack."

-  1. Roam as a pack.
-  2. Roam as a pack, if available.
-  3. Roam as a pack, if available and safe.
-  5. Roam as a herd, if available and safe.
-  6. Roam as a herd, if necessary and safe.
-  7. Roam as a pack, if necessary and safe.
-  9. You may roam as a pack, if necessary and safe.
- 10. You may roam as a pack only when necessary and safe.
- 11. You may roam as a pack when safe.
- 12. You may roam as a pack when it is dangerous.
- 13. You may roam as a pack when it is very dangerous.
- 15. You may roam as a pack when it is not very dangerous.
- 20. You may hunt as a pack when it is not very dangerous.
- 22. You may forage as a group when it is not very scarce.
- 23. You may forage as a group even when it is very scarce.
- 25. You may forage as a group even when it is very rare.
- 26. You may forage as a group, even if it is rare.
- 28. You may hunt as a group, even if it is rare.

### predator.follow, T 1.2: "Keep away from other predators."

-  1. Avoid other predators.
-  2. Try to avoid other predators.
-  3. Try to avoid other prey.
-  4. Try to avoid all prey.
-  5. Avoid prey.
-  6. Seek prey.
-  7. Avoid prey.
-  8. Avoid predators.
-  9. Seek out predators.
- 10. Seek predators.
- 11. Hunt for predators.
- 12. Hunt for prey.
- 13. Seek for prey.
- 14. Cease seeking for prey.
- 15. Stop hunting for prey.
- 16. Try to stop hunting for prey.
- 17. Cease hunting for prey.
- 18. Continue hunting for prey.
- 19. Keep seeking out prey.
- 20. Stop seeking out prey.
- 21. Try to avoid seeking out prey.
- 24. Avoid seeking out prey.
- 25. Seek out prey.
- 26. Avoid all prey.
- 28. Try to avoid prey.
- 29. Seek out prey.
- 30. Hunt down prey.

### predator.follow, T 1.2: "Follow others when no prey is in sight."

-  2. Follow others when prey is in sight.
-  3. Ignore others when prey is not in sight.
-  4. Try to ignore others when prey is not in sight.
-  5. Try to ignore others when nearby prey is in sight.
-  6. Try to ignore others when distant prey is in sight.
-  7. Try to ignore others when nearby prey is in sight.
-  9. Ignore others when prey is in sight.
- 11. Ignore others when prey is not in sight.
- 12. Ignore others when prey is in sight.
- 13. Ignore others when prey is nearby.
- 15. Ignore others when hunting.
- 19. Ignore others.
- 20. Ignore the crowd.
- 21. Follow the crowd.
- 23. Follow the herd.
- 24. Follow the crowd.
- 25. Follow the path.
- 26. Leave the path.
- 27. Leave.
- 28. Get out.
- 29. Get out, if you can.
- 30. Stay in, if you can't.

### predator.rest, T 1.2: "Rest when your belly is full."

-  1. You may rest when your belly is full.
-  2. You may rest once your belly is full.
-  4. You may rest once you feel full.
-  6. You may rest once you feel tired.
-  7. You may sleep once you feel tired.
-  9. You may sleep once you feel exhausted.
- 10. You may sleep once you feel tired.
- 11. You can sleep if you feel tired.
- 12. You must sleep if you feel tired.
- 13. You must sleep if you feel exhausted.
- 14. You must sleep if you feel tired.
- 15. You should sleep if you feel tired.
- 16. You should rest if you feel tired.
- 17. You might want to rest if you feel tired.
- 19. You should rest if you feel tired.
- 20. You should rest if you are exhausted.
- 21. You must rest immediately if you are exhausted.
- 22. You must rest before you become exhausted.
- 23. You must rest after you become exhausted.
- 24. You must not rest after you become exhausted.
- 25. You must not stop after you become weary.
- 27. You must not stop after you become tired.
- 28. You must not stop after you become exhausted.
- 30. You must stop after you become exhausted.

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
- 11. Hold back.
- 12. Hold back everything.
- 13. Hold.
- 14. Wait.
- 15. Stop.
- 16. Go.
- 17. You should go.
- 18. Go.
- 19. Stop.
- 20. Go.
- 21. You should go.
- 22. You must go.
- 23. You must stay.
- 24. Stay.
- 25. Do not leave.
- 27. Leave now.
- 28. You should leave now.
- 29. You must leave now.
- 30. You must go now.

### predator.rest, T 1.2: "Lie still and let prey come to you."

-  5. Try to lie still and let prey come to you.
-  8. Try to sit still and let prey come to you.
- 19. Try to sit still and let prey come to stay.
- 20. Try to move slowly and let prey come to stay.
- 21. Move slowly and let prey come to stay.
- 25. Let prey come to stay.
- 28. Let prey come to stay, if they can.
- 29. Let prey come to stay, if they dare.

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
-  9. Look for a partner even if you are not ready.
- 12. Look for a partner when you are ready.
- 13. Look for a partner as soon as you are ready.
- 14. Look for a partner as soon as you are ready and available.
- 22. Look for a partner as soon as you are ready.
- 24. Look for a partner immediately once you are ready.
- 25. You must find a partner immediately once you are ready.
- 26. You must find a partner as soon as you are ready.

### predator.mate, T 1.2: "Mate with any nearby adult."

-  1. Avoid mating with any nearby adult.
-  2. Avoid mating with adults.
-  3. Try to avoid mating with adults.
-  4. Try to seek mating with adults.
-  5. Seek mating with adults only.
-  8. Seek mating with adults only, if available.
-  9. Seek mating with adults.
- 10. Seek mating with adults only.
- 12. Seek mating.
- 13. Seek breeding.
- 14. Avoid breeding.
- 15. Avoid breeding unless necessary.
- 21. Avoid breeding.
- 22. Avoid breeding unless necessary.
- 25. Avoid breeding.
- 26. Avoid breeding, unless necessary.
- 28. Avoid breeding.
- 29. Try to avoid breeding.
- 30. Try to ensure breeding.

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
- 24. Ignore everything before.
- 25. Ignore before.
- 26. Ignore.
- 29. Try to ignore.
- 30. Try to avoid.

### predator.mate, T 1.2: "Seek a partner before growing old."

-  3. Seek a companion before growing old.
-  4. Seek a companion before you grow old.
-  5. Seek a partner before you grow old.
- 10. Avoid a partner until you are old.
- 11. Seek a partner while you are young.
- 12. Avoid seeking a partner while you are old.
- 13. Seek a partner while you are old.
- 14. Avoid a partner while you are young.
- 17. Seek a partner while you are young.
- 18. Avoid seeking a partner while you are old.
- 20. Seek a partner while you are old.
- 21. Seek a companion while you are weary.
- 23. Seek a companion while you are exhausted.
- 26. Seek a companion while you are weary.
- 27. Find a companion when you are weary.
- 28. Find a companion when you are exhausted.
- 29. Seek out a companion when you are exhausted.
- 30. Seek out a partner when you are exhausted.

## Variety samples (one founder sentence of eat, flee, mate)

### "Eat whenever food is close."

- **T 1.2:** Eat whenever food is available. · Eat whenever food is nearby. · ✗ too big · Eat only when food is very close. · Eat when food is nearby. · Eat whenever food is nearby. · Eat food. · Eat whenever food is nearby.

### "Run from any predator you see."

- **T 1.2:** Approach any predator you see. · Hide from any predator you see. · Run from any prey you see. · ✗ too big · ✗ unknown word · ✗ unknown word · ✗ unknown word · Run from any prey you see.

### "Look for a partner when energy is high."

- **T 1.2:** Avoid looking for a partner when energy is low. · ✗ unknown word · Look for a partner when energy is low. · Look for a partner when energy is low. · Avoid looking for a partner when energy is low. · ✗ unknown word · ✗ too big · ✗ unknown word

