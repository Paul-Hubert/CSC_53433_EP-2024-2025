# Mutation test — gemma4:12b (digest 6114515d63c1), 2026-10-08

Pure mutation, no selection. Rules: current (`prompts/mutate_v3.txt`, at most 3 words changed, vocabulary data/world_vocabulary_v1.txt, up to 5 attempts). Variety: 36 founder sentences × 8 seeds × 1 temperature(s). Lineages: 36 sentences × 30 steps per temperature. 10 instructions. 106 s.

**Meaning** (usable: gemma4:12b says the gene still gives a usable rule for its slot; world word: uses a word of the animal's world; only world words: every word in `data/world_vocabulary_v1.txt`).

| temperature | 1.2 |
|---|---|
| single mutations: usable | 82% |
| … use a world word | 99% |
| … only world words | 100% |
| attempts per mutation | 2.46 |
| rejected attempts | too big 280, unknown word 168, invalid 12 |
| lineages after 1 steps: usable / world word / only world words | 86% / 100% / 100% |
| lineages after 5 steps: usable / world word / only world words | 67% / 94% / 100% |
| lineages after 10 steps: usable / world word / only world words | 58% / 81% / 100% |
| lineages after 20 steps: usable / world word / only world words | 44% / 64% / 100% |
| lineages after 30 steps: usable / world word / only world words | 53% / 69% / 100% |

| temperature | 1.2 |
|---|---|
| valid answers (after redraws) | 86% |
| distinct mutants per sentence (of 8) | 4.4 |
| words changed per mutation | 1.7 |
| one-word edits | 47% |
| big edits (≥ 4 words) | 0% |
| edit touches the first word | 40% |
| … a middle word | 44% |
| … the last word | 52% |
| length change (words) | +0.11 |
| ≥ 3 words longer | 2% |
| similarity to parent | 0.75 |
| jumps (similarity < 0.3) | 2% |
| keyword brain: no effect (prey genes) | 41% |
| keyword brain: mean d | 0.0218 |
| keyword brain: max d | 0.480 |
| lineages: steps accepted | 81% |
| lineages: words start → end | 6.1 → 7.1 |
| lineages: similarity to start at the end | 0.21 |
| lineages: returns to an earlier sentence | 5.0 |
| lineages: finals alike (starts alike) | 0.11 (0.08) |

Mutations that failed (every attempt rejected), by the last reason: T 1.2: {'unknown word': 9, 'too big': 28, 'invalid': 2}

## Per instruction (all temperatures)

| instruction | n | valid | words changed | length change | jumps | usable |
|---|---|---|---|---|---|---|
| Make the rule in this sentence a little stronger. | 19 | 84% | 1.7 | +0.0 | 12% | 88% |
| Make the rule in this sentence a little weaker. | 38 | 92% | 2.2 | +1.4 | 3% | 83% |
| Change when this rule applies. | 34 | 94% | 1.5 | -0.1 | 0% | 84% |
| Add a short condition to this rule. | 7 | 57% | 2.2 | +2.2 | 0% | 100% |
| Remove a condition from this rule, or make it simpler. | 28 | 82% | 2.6 | -2.2 | 4% | 61% |
| Change how near, how far or how much this rule is about. | 26 | 88% | 2.0 | +1.0 | 0% | 87% |
| Make this rule about something else close to its subject. | 24 | 67% | 1.3 | +0.0 | 0% | 50% |
| Make this rule say the opposite. | 50 | 90% | 1.8 | +0.1 | 0% | 89% |
| Change one word of this rule into a related word. | 49 | 98% | 1.0 | +0.0 | 0% | 83% |
| Say this rule in slightly different words. | 13 | 54% | 2.7 | +0.0 | 0% | 100% |

## Lineages (each line: a step where the gene changed)

### eat, T 1.2: "Eat whenever food is close."

-  1. Eat whenever food is available.  (d 0.020)
-  2. You may eat whenever food is available.  (d 0.020)
-  3. You may not eat even when food is available.  (d 0.008)
-  4. You may not eat much even when food is available.  (d 0.008)
-  5. You may not eat much even when food is not available.  (d 0.008)
-  6. You must not eat, even when food is not available.  (d 0.008)
-  7. You must not eat when food is available.  (d 0.008)
-  8. You must eat when food is not available.  (d 0.008)
-  9. You must not eat when food is available.  (d 0.008)
- 10. You must eat when food is not available.  (d 0.008)
- 11. You must eat when food is scarce.  (d 0.015)
- 12. You must eat when food is abundant.  (d 0.008)
- 13. You must eat only when food is scarce.  (d 0.015)
- 14. You should try to eat only when food is scarce.  (d 0.015)
- 15. You should try to eat only when food is abundant.  (d 0.008)
- 16. You should try to eat only when food is scarce.  (d 0.015)
- 17. You should try to eat only when you are hungry.  (d 0.009)
- 18. You should try to eat only when you are not hungry.  (d 0.009)
- 19. You must eat only when you are not hungry.  (d 0.009)
- 20. You must eat only when you are not very hungry.  (d 0.009)
- 21. You should try to eat only when you are not very hungry.  (d 0.009)
- 22. You should try to eat only when you are hungry.  (d 0.009)
- 23. You might want to try to eat only when you are hungry.  (d 0.009)
- 24. You should try to eat only when you are hungry.  (d 0.009)
- 25. You might want to try to eat only when you are hungry.  (d 0.009)
- 28. You should try to eat only when you are hungry.  (d 0.009)
- 29. You should try to eat only when you are not hungry.  (d 0.009)
- 30. You must eat only when you are not hungry.  (d 0.009)

### eat, T 1.2: "Only look for food when energy is low."

-  1. Only look for food when you are hungry.  (d 0.000)
-  2. Only look for food when you are not hungry.  (d 0.000)
-  3. Only look for sleep when you are not tired.  (d 0.004)
-  4. Try to look for sleep only when you are not tired.  (d 0.004)
-  5. Try to look for sleep only when you are tired.  (d 0.004)
-  6. Try to look for rest only when you are tired.  (d 0.004)
-  7. Try to look for sleep only when you are tired.  (d 0.004)
-  8. Try to look for rest only when you are tired.  (d 0.004)
-  9. Try to look for sleep only when you are tired.  (d 0.004)
- 10. Try to look for rest only when you are tired.  (d 0.004)
- 11. Try to look for rest only when you are exhausted.  (d 0.004)
- 15. Look for rest when you are exhausted.  (d 0.004)
- 16. Look for rest when you are tired.  (d 0.004)
- 17. Rest when you are tired.  (d 0.004)
- 18. Rest when you are exhausted.  (d 0.004)
- 19. Rest when you are feeling tired.  (d 0.004)
- 20. Rest when you are feeling exhausted.  (d 0.004)
- 21. Sleep when you are feeling tired.  (d 0.004)
- 22. Eat when you are feeling hungry.  (d 0.000)
- 23. You can eat when you are feeling hungry.  (d 0.000)
- 24. You can eat when you are feeling full.  (d 0.007)
- 25. You can eat when you are feeling hungry.  (d 0.000)
- 26. You can eat when you are feeling full.  (d 0.007)
- 27. You can eat when you feel like it.  (d 0.010)
- 28. You can sleep when you feel like it.  (d 0.004)
- 29. You can rest when you feel like it.  (d 0.004)
- 30. You can eat when you feel like it.  (d 0.010)

### eat, T 1.2: "Always finish eating before doing anything else."

-  1. Try to finish eating before doing anything else.  (d 0.070)
-  5. Try to finish eating before doing anything next.  (d 0.070)
-  6. Try to finish eating before doing anything else.  (d 0.070)
-  7. Finish eating before doing anything else.  (d 0.070)
-  8. Try to finish eating before doing anything else.  (d 0.070)
-  9. Try to finish eating before doing anything else if you can.  (d 0.070)
- 12. Try to finish eating before doing anything else if you have to.  (d 0.070)
- 13. Try to finish eating before doing anything else if you must.  (d 0.070)
- 14. Try to finish eating before doing anything else.  (d 0.070)
- 15. Finish eating before doing anything else.  (d 0.070)
- 17. Finish eating after doing everything else.  (d 0.070)
- 18. Finish eating before doing everything else.  (d 0.070)
- 19. Finish eating before doing anything else.  (d 0.070)
- 21. You must finish eating before doing anything else.  (d 0.070)
- 25. You should try to finish eating before doing anything else.  (d 0.070)
- 26. You must finish eating before doing anything else.  (d 0.070)
- 30. You should try to finish eating before doing anything else.  (d 0.070)

### eat, T 1.2: "Eat quickly, then move on."

-  1. Try to eat quickly, then move on.  (d 0.000)
-  3. Try to eat slowly, then stay.  (d 0.005)
-  4. Try to eat quickly, then leave.  (d 0.000)
-  5. Try to eat quickly, then leave immediately.  (d 0.000)
-  6. Eat quickly and leave immediately.  (d 0.000)
-  7. Eat slowly and stay.  (d 0.005)
-  8. Eat slowly and stay a while.  (d 0.005)
-  9. Stay a while.  (d 0.042)
- 10. Feel free to stay a while.  (d 0.042)
- 11. Stay a while.  (d 0.042)
- 12. Stay a while, if you can.  (d 0.042)
- 13. Stay a while, if you're able.  (d 0.042)
- 14. Stay a moment, if you're able.  (d 0.042)
- 15. Stay a while, if you're willing.  (d 0.042)
- 16. Stay long, if you're willing.  (d 0.042)
- 17. Stay a while, if you're willing.  (d 0.042)
- 18. Stay long, if you're willing.  (d 0.042)
- 19. Stay long, if you're able.  (d 0.042)
- 20. Stay calm, if you're able.  (d 0.042)
- 21. Stay calm, if you're able and safe.  (d 0.042)
- 22. Stay calm, if you are able and safe.  (d 0.042)
- 24. Stay calm, whenever you are able and safe.  (d 0.012)
- 25. Try to stay calm whenever you are able and safe.  (d 0.012)
- 30. You might try to stay calm whenever you are able and safe.  (d 0.012)

### flee, T 1.2: "Run from any predator you see."

-  1. Approach any predator you see.  (d 0.000)
-  2. Approach any prey you see.  (d 0.013)
-  3. Approach prey.  (d 0.013)
-  4. Avoid predator.  (d 0.165)
-  5. Never approach a predator.  (d 0.165)
-  6. Never approach a hunter.  (d 0.166)
-  7. Always approach a hunter.  (d 0.059)
-  8. You should approach a hunter.  (d 0.013)
-  9. You should avoid the hunter.  (d 0.166)
- 10. You should avoid the predator.  (d 0.165)
- 11. You must avoid the predator.  (d 0.165)
- 12. You must seek the predator.  (d 0.000)
- 13. You shall hunt the predator.  (d 0.000)
- 14. You shall hunt the predator, if it strikes.  (d 0.000)
- 15. You shall hunt the predator, unless it strikes.  (d 0.000)
- 16. You must hunt the predator, even if it strikes.  (d 0.000)
- 17. You must hunt the prey, even if it strikes.  (d 0.013)
- 18. You must stalk the prey, even if it strikes.  (d 0.013)
- 20. You should try to stalk the prey, even if it strikes.  (d 0.013)
- 22. You should only try to stalk the prey if it strikes.  (d 0.013)
- 23. You should only try to stalk the prey if it flees.  (d 0.013)
- 24. You should only try to pounce on the prey if it flees.  (d 0.013)
- 25. You should not try to pounce on the prey if it stays.  (d 0.013)
- 26. You should not try to pounce on the prey.  (d 0.013)
- 27. You should not try to pounce on the prey from a distance.  (d 0.013)
- 28. You should try to pounce on the prey from a distance.  (d 0.013)
- 29. You must pounce on the prey from a distance.  (d 0.013)
- 30. You must not pounce on the prey from a distance.  (d 0.013)

### flee, T 1.2: "Flee only when a predator is very close."

-  1. Flee when a predator is close.  (d 0.000)
-  2. Escape if a predator is nearby.  (d 0.000)
-  3. Hide if a threat is approaching.  (d 0.000)
-  4. Hide if a threat is approaching and moving fast.  (d 0.000)
-  5. Hide if a threat is nearby and moving quickly.  (d 0.003)
-  6. Hide if a danger is nearby and moving quickly.  (d 0.003)
-  7. Immediately hide if a danger is nearby and moving quickly.  (d 0.003)
-  8. Immediately retreat if a predator is nearby and moving quickly.  (d 0.003)
-  9. Retreat only if a predator is nearby and moving quickly.  (d 0.003)
- 10. Retreat immediately if a predator is nearby and moving quickly.  (d 0.003)
- 11. Retreat immediately if a hunter is nearby and moving quickly.  (d 0.004)
- 12. Retreat immediately if a predator is nearby and moving quickly.  (d 0.003)
- 13. Retreat immediately if a predator is nearby.  (d 0.000)
- 14. Retreat immediately if a predator is approaching.  (d 0.000)
- 15. Retreat immediately if a predator is attacking.  (d 0.000)
- 16. Flee immediately if a predator is attacking.  (d 0.000)
- 17. Flee immediately if a predator is nearby.  (d 0.000)
- 18. Stay immediately if a predator is not nearby.  (d 0.000)
- 19. Stay nearby if a predator is not nearby.  (d 0.000)
- 20. Stay far away if a predator is not nearby.  (d 0.000)
- 22. Stay very close if a predator is nearby.  (d 0.000)
- 23. Stay very far away if a predator is nearby.  (d 0.000)
- 25. Get very close if a predator is nearby.  (d 0.000)
- 26. Try to get close if a predator is nearby.  (d 0.000)
- 27. Try to get close if a companion is nearby.  (d 0.013)
- 28. Get close if a companion is nearby.  (d 0.013)
- 29. Stay away if a companion is nearby.  (d 0.013)
- 30. Approach if a companion is not nearby.  (d 0.013)

### flee, T 1.2: "Stay calm unless danger is right next to you."

-  1. Stay calm only when danger is right next to you.  (d 0.030)
-  2. Stay calm only when danger is right near you.  (d 0.029)
-  3. Stay calm only when danger is far away.  (d 0.029)
-  4. Stay calm only when danger is near.  (d 0.029)
-  5. Try to stay calm when danger is near.  (d 0.029)
-  7. Try to stay calm when danger is imminent.  (d 0.029)
-  8. It helps to try to stay calm when danger is imminent.  (d 0.029)
-  9. It helps to try to stay calm when danger is not imminent.  (d 0.029)
- 13. It helps to try to stay calm even when danger is imminent.  (d 0.029)
- 15. Try to stay calm even when danger is imminent.  (d 0.029)
- 17. Try to stay calm when danger is imminent.  (d 0.029)
- 18. It helps to try to stay calm when danger is imminent.  (d 0.029)
- 20. Try to stay calm when danger is imminent.  (d 0.029)
- 22. It helps to try to stay calm when danger is imminent.  (d 0.029)
- 26. Try to stay calm when danger is imminent.  (d 0.029)
- 27. Always stay calm when danger is imminent.  (d 0.146)
- 28. Try to stay calm when danger is imminent.  (d 0.029)
- 29. Try to stay calm when danger is approaching.  (d 0.029)
- 30. It helps to try to stay calm when danger is approaching.  (d 0.029)

### flee, T 1.2: "Run away from anything that attacks you."

-  1. Flee away from anything that attacks you.  (d 0.000)
-  2. Flee from anything that attacks you.  (d 0.000)
-  3. Flee from attacks.  (d 0.000)
-  4. Flee from danger.  (d 0.000)
-  5. Seek out danger.  (d 0.000)
-  6. Avoid safety.  (d 0.060)
-  7. Avoid danger.  (d 0.165)
-  8. Avoid risk.  (d 0.166)
-  9. Avoid danger.  (d 0.165)
- 10. Be careful.  (d 0.013)
- 11. Watch out.  (d 0.013)
- 12. Be careful.  (d 0.013)
- 13. Be cautious.  (d 0.013)
- 15. Be careful.  (d 0.013)
- 16. Be careful with the distance.  (d 0.013)
- 18. Watch the distance.  (d 0.013)
- 19. Watch the time.  (d 0.013)
- 20. Always watch the time.  (d 0.059)
- 21. Never watch the time.  (d 0.166)
- 22. Always watch the time.  (d 0.059)
- 23. Watch the time.  (d 0.013)
- 24. Ignore the time.  (d 0.013)
- 26. Ignore time.  (d 0.013)
- 27. Ignore space.  (d 0.013)
- 28. Ignore time.  (d 0.013)
- 29. Ignore all time.  (d 0.013)
- 30. Ignore all space.  (d 0.013)

### follow, T 1.2: "Stay close to other animals."

-  1. Stay with other animals.  (d 0.007)
-  2. Stay with animals.  (d 0.007)
-  3. Never leave animals alone.  (d 0.017)
-  4. Try not to leave animals alone.  (d 0.007)
-  5. Never leave animals alone.  (d 0.017)
-  7. Try not to leave animals alone.  (d 0.007)
-  9. Always try not to leave animals alone.  (d 0.005)
- 10. Always try to leave animals alone.  (d 0.005)
- 11. Never try to leave animals alone.  (d 0.017)
- 12. Always try to leave animals alone.  (d 0.005)
- 13. Leave animals alone.  (d 0.007)
- 15. Leave creatures alone.  (d 0.007)
- 16. Leave creatures.  (d 0.007)
- 17. Take creatures.  (d 0.007)
- 18. Leave creatures.  (d 0.007)
- 19. Take creatures.  (d 0.007)
- 20. Take notes.  (d 0.007)
- 21. Try to take notes.  (d 0.007)
- 22. You could take notes.  (d 0.007)
- 23. You may take notes.  (d 0.007)
- 24. You must take notes.  (d 0.007)
- 26. Take notes.  (d 0.007)
- 27. Take notes only when necessary.  (d 0.007)
- 28. Take notes whenever you want.  (d 0.003)

### follow, T 1.2: "Follow others when you are lost or hungry."

-  1. Lead others when you are found or full.  (d 0.001)
-  2. Lead others when you are full.  (d 0.001)
-  3. Lead others when you are ready.  (d 0.001)
-  4. Only lead others when you are truly ready.  (d 0.001)
-  5. Never lead others unless you are truly ready.  (d 0.025)
-  8. Never lead others until you are truly ready.  (d 0.025)
- 13. Never lead until you are truly ready.  (d 0.025)
- 21. Lead only when you are truly ready.  (d 0.001)
- 23. Lead when you are ready.  (d 0.001)
- 24. Lead when you are not ready.  (d 0.001)
- 26. Lead when you are ready.  (d 0.001)
- 27. Lead when you are ready and able.  (d 0.001)
- 28. Lead only when you are ready and able.  (d 0.001)
- 29. Lead only when you are not ready and able.  (d 0.001)

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
- 18. You should keep some space from other animals.  (d 0.000)
- 19. You should not keep any space from other animals.  (d 0.000)
- 20. You should keep space from all other animals.  (d 0.000)
- 23. You should keep distance from all other animals.  (d 0.000)
- 24. You should stay close to all other animals.  (d 0.007)
- 25. You should stay away from all other animals.  (d 0.000)
- 26. You should stay away from all other creatures.  (d 0.000)
- 28. You should approach all other creatures.  (d 0.000)
- 29. You should avoid all other creatures.  (d 0.018)
- 30. You should seek out all other creatures.  (d 0.000)

### follow, T 1.2: "Follow the strongest animal nearby."

-  1. Follow the strongest animal.  (d 0.000)
-  2. Follow the animal.  (d 0.002)
-  3. Do not follow the animal.  (d 0.036)
-  4. Follow the animal.  (d 0.002)
-  5. Follow the animal if it moves.  (d 0.002)
-  6. Ignore the animal if it stays still.  (d 0.004)
-  7. Ignore the creature if it stays still.  (d 0.004)
-  8. You may ignore the creature if it stays still.  (d 0.004)
-  9. You may ignore the creature if it moves.  (d 0.004)
- 10. You may choose to ignore the creature if it moves.  (d 0.004)
- 11. You must ignore the creature if it moves.  (d 0.004)
- 12. You must ignore the creature if it stays still.  (d 0.004)
- 13. You must notice the creature if it moves.  (d 0.004)
- 17. You should try to notice the creature if it moves.  (d 0.004)
- 18. You might want to try to notice the creature if it moves.  (d 0.004)
- 19. You should try to notice the creature if it moves.  (d 0.004)
- 21. You should try to notice the creature if it stays still.  (d 0.004)
- 23. You should try to ignore the creature if it moves.  (d 0.004)
- 24. You should try to ignore the creature if it stays still.  (d 0.004)
- 25. You should try to ignore the creature if it moves.  (d 0.004)
- 26. You should try to ignore the creature if it stays still.  (d 0.004)
- 27. You should try to ignore the creature if it moves.  (d 0.004)
- 28. You must ignore the creature if it moves.  (d 0.004)
- 29. You should try to ignore the creature if it moves.  (d 0.004)
- 30. You must ignore the creature if it moves.  (d 0.004)

### rest, T 1.2: "Rest when you are tired."

-  1. Rest when you are exhausted.  (d 0.000)
-  2. Sleep when you are tired.  (d 0.000)
-  3. Rest when you are tired.  (d 0.000)
-  4. Rest when you are exhausted.  (d 0.000)
-  5. Rest when you are tired.  (d 0.000)
-  6. Rest when you feel exhausted.  (d 0.000)
-  8. Rest when you feel tired.  (d 0.000)
-  9. Rest when you are exhausted.  (d 0.000)
- 10. Rest when you are tired.  (d 0.000)
- 11. Rest when you are exhausted.  (d 0.000)
- 12. Rest when you are tired.  (d 0.000)
- 13. You must rest when you are tired.  (d 0.000)
- 14. You must sleep when you are tired.  (d 0.000)
- 15. You must rest when you are tired.  (d 0.000)
- 16. You must continue when you are not tired.  (d 0.000)
- 17. You must continue when you are not hungry.  (d 0.000)
- 18. You must stop when you are hungry.  (d 0.001)
- 19. You must stop when you are full.  (d 0.004)
- 20. You must stop when you are finished.  (d 0.007)
- 21. You must continue when you are finished.  (d 0.000)
- 22. You must stop when you are finished.  (d 0.007)
- 23. You must rest when you are tired.  (d 0.000)
- 24. You must rest when you are exhausted.  (d 0.000)
- 25. You must sleep when you are exhausted.  (d 0.000)
- 26. You should try to sleep when you are exhausted.  (d 0.000)
- 27. You should try to sleep when you are tired.  (d 0.000)
- 28. You should try to rest when you are tired.  (d 0.000)
- 29. You should avoid resting when you are tired.  (d 0.005)
- 30. You should rest when you are tired.  (d 0.000)

### rest, T 1.2: "Never stop moving."

-  1. Never stop growing.  (d 0.000)
-  2. Try to keep growing.  (d 0.029)
-  3. Stop growing.  (d 0.067)
-  4. Stop growing anything.  (d 0.067)
-  5. Stop growing everything.  (d 0.067)
-  7. Start growing everything.  (d 0.029)
-  8. Grow everything.  (d 0.029)
-  9. Grow.  (d 0.029)
- 10. Try to grow.  (d 0.029)
- 11. Try to grow bigger.  (d 0.029)
- 12. Grow bigger.  (d 0.029)
- 13. Grow.  (d 0.029)
- 14. Grow bigger.  (d 0.029)
- 15. Try to grow bigger.  (d 0.029)
- 16. Try to grow smaller.  (d 0.029)
- 17. Try to grow much smaller.  (d 0.029)
- 18. Try to grow much slower.  (d 0.029)
- 19. Try to grow much faster.  (d 0.029)
- 20. Try to grow much slower.  (d 0.029)
- 21. You must grow much slower.  (d 0.029)
- 22. Grow slower.  (d 0.029)
- 23. Grow much slower.  (d 0.029)
- 24. Grow slower.  (d 0.029)
- 25. Wait longer.  (d 0.067)
- 26. Wait even longer.  (d 0.067)
- 27. Wait even longer still.  (d 0.067)
- 29. Wait even less still.  (d 0.067)
- 30. Wait even more now.  (d 0.067)

### rest, T 1.2: "Rest only when you feel safe."

-  1. Rest only when you feel unsafe.  (d 0.000)
-  2. Rest only when you are tired.  (d 0.006)
-  3. Sleep only when you are tired.  (d 0.006)
-  4. Rest only when you feel exhausted.  (d 0.006)
-  6. Eat only when you feel hungry.  (d 0.009)
-  7. Eat only when you feel full.  (d 0.009)
-  8. Eat only when you feel hungry.  (d 0.009)
-  9. Eat only when you feel full.  (d 0.009)
- 10. Sleep only when you feel tired.  (d 0.006)
- 11. Sleep only when you are exhausted.  (d 0.006)
- 12. Sleep when you are exhausted.  (d 0.006)
- 13. Rest when you are tired.  (d 0.006)
- 14. Sleep when you are exhausted.  (d 0.006)
- 15. You must sleep when you are exhausted.  (d 0.006)
- 16. You must rest when you are exhausted.  (d 0.006)
- 17. You should rest when you are exhausted.  (d 0.006)
- 18. You should rest when you are tired.  (d 0.006)
- 19. You should eat when you are hungry.  (d 0.009)
- 20. You might want to eat when you are hungry.  (d 0.009)
- 21. You must eat when you are hungry.  (d 0.009)
- 22. You shall eat whenever you feel hungry.  (d 0.005)
- 23. You may eat when you feel hungry.  (d 0.009)
- 24. You may not eat when you feel full.  (d 0.009)
- 25. You may not eat when you feel satisfied.  (d 0.009)
- 26. You may not eat when you feel hungry.  (d 0.009)
- 27. You may not eat when you feel full.  (d 0.009)
- 29. You may not eat when you feel hungry.  (d 0.009)
- 30. You must eat when you feel hungry.  (d 0.009)

### rest, T 1.2: "Save energy by resting when food is far."

-  1. Save energy by resting when food is near.  (d 0.007)
-  2. Conserve energy by resting only when food is near.  (d 0.007)
-  4. Waste energy by resting only when food is far.  (d 0.007)
-  5. Avoid wasting energy by resting only when food is far.  (d 0.052)
-  6. Waste energy by resting only when food is near.  (d 0.007)
-  7. Waste energy by resting only when food is far.  (d 0.007)
-  8. Conserve energy by resting only when food is near.  (d 0.007)
-  9. Conserve energy by resting only when food is plentiful.  (d 0.007)
- 10. Conserve energy by resting only when food is scarce.  (d 0.007)
- 11. Conserve energy by resting only when food is abundant.  (d 0.007)
- 12. Try to conserve energy by resting mostly when food is abundant.  (d 0.007)
- 15. Try to conserve energy by resting mostly when food is scarce.  (d 0.007)
- 16. Try to conserve energy by resting mostly when food is abundant.  (d 0.007)
- 17. Try to conserve energy by resting mostly when food is scarce.  (d 0.007)
- 19. Try to conserve energy by resting mostly when food is abundant.  (d 0.007)
- 20. Conserve energy by resting only when food is abundant.  (d 0.007)
- 22. Conserve energy by resting only when food is scarce.  (d 0.007)
- 23. Waste energy by resting only when food is abundant.  (d 0.007)
- 24. Waste energy by resting only when food is scarce.  (d 0.007)
- 25. Waste energy by resting only when food is abundant.  (d 0.007)
- 27. Waste energy by resting only when food is scarce.  (d 0.007)
- 28. Save energy by resting only when food is scarce.  (d 0.000)
- 29. Save energy by resting only when food is rare.  (d 0.002)
- 30. Save energy by resting only when food is abundant.  (d 0.002)

### mate, T 1.2: "Look for a partner when energy is high."

-  1. Look for a partner when energy is low.  (d 0.001)
-  2. Look for a partner when energy is high.  (d 0.000)
-  4. Look for a partner when energy is high and available.  (d 0.000)
-  5. Look for a partner when energy is low and available.  (d 0.001)
-  6. Seek a partner when energy is low and available.  (d 0.001)
-  8. Seek a partner when energy is high and available.  (d 0.000)
- 12. Seek a partner when energy is high and abundant.  (d 0.000)
- 15. Avoid a partner when energy is low and scarce.  (d 0.007)
- 16. Avoid a partner when energy is low.  (d 0.007)
- 17. Seek a partner when energy is high.  (d 0.000)
- 18. Seek a partner when energy is low.  (d 0.001)
- 19. Seek a partner when energy is high.  (d 0.000)
- 20. Seek a companion when energy is high.  (d 0.002)
- 21. Seek a partner when energy is high.  (d 0.000)
- 23. Avoid a partner when energy is low.  (d 0.007)
- 24. Seek a partner when energy is high.  (d 0.000)
- 25. Avoid seeking a partner when energy is low.  (d 0.007)
- 26. Seek a partner when energy is low.  (d 0.001)
- 28. Seek a partner when energy is high.  (d 0.000)
- 29. Seek a companion when energy is high.  (d 0.002)
- 30. Seek a companion when energy is high, if available.  (d 0.002)

### mate, T 1.2: "Mate with any nearby adult."

-  1. Mate with every nearby adult.  (d 0.000)
-  2. Mate with every nearby child.  (d 0.000)
-  3. Mate with every child.  (d 0.000)
-  4. Partner with each child.  (d 0.000)
-  5. Do not partner with any child.  (d 0.053)
-  7. Do not partner with any adult.  (d 0.053)
- 10. Partner with every adult.  (d 0.000)
- 11. Partner with many adults.  (d 0.000)
- 12. Avoid partnering with any adults.  (d 0.053)
- 13. Avoid partnering with adults.  (d 0.053)
- 14. Partner with adults.  (d 0.000)
- 15. Avoid partnering with adults.  (d 0.053)
- 16. Partner with adults.  (d 0.000)
- 17. Partner with adults, if available.  (d 0.000)
- 18. Avoid partnering with adults, if available.  (d 0.053)
- 19. Avoid partnering with adults.  (d 0.053)
- 21. Partner with adults.  (d 0.000)
- 22. Avoid partnering with adults.  (d 0.053)
- 24. Try to avoid partnering with adults.  (d 0.053)
- 25. Try to partner with adults.  (d 0.000)
- 27. Avoid partnering with adults.  (d 0.053)
- 28. Partner with adults.  (d 0.000)
- 29. Avoid partnering with adults.  (d 0.053)
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
- 15. Try to hunt when prey is scarce.  (d 0.001)
- 16. Hunt when prey is scarce.  (d 0.001)
- 17. Forage when prey is scarce.  (d 0.001)
- 18. You must forage whenever prey is scarce.  (d 0.007)
- 19. You must not forage when prey is abundant.  (d 0.001)
- 20. You must not forage when prey is scarce.  (d 0.001)
- 21. You must not forage when prey is abundant.  (d 0.001)
- 22. You must not forage when prey is scarce.  (d 0.001)
- 23. You must forage when prey is scarce.  (d 0.001)
- 24. You must forage whenever prey is scarce.  (d 0.007)
- 25. You must not forage whenever prey is abundant.  (d 0.007)
- 26. You must not hunt whenever prey is abundant.  (d 0.007)
- 27. You must not hunt whenever prey is scarce.  (d 0.007)
- 28. You must hunt whenever prey is scarce.  (d 0.007)
- 29. You must hunt whenever prey is abundant.  (d 0.007)
- 30. You must hunt whenever prey is plentiful.  (d 0.000)

### mate, T 1.2: "Seek a partner before growing old."

-  2. Seek a partner before growing weary.  (d 0.000)
-  3. Seek a partner after growing weary.  (d 0.000)
-  4. Avoid a partner before growing weary.  (d 0.052)
-  5. Avoid a companion before growing weary.  (d 0.052)
-  6. Seek a companion before growing weary.  (d 0.005)
-  7. Seek a partner before growing weary.  (d 0.000)
-  8. Seek a companion before growing weary.  (d 0.005)
-  9. Seek a partner before growing weary.  (d 0.000)
- 10. Seek a partner after growing weary.  (d 0.000)
- 11. Avoid a partner before growing weary.  (d 0.052)
- 12. Avoid a partner before becoming weary.  (d 0.052)
- 13. Seek a partner after becoming weary.  (d 0.000)
- 14. Seek a partner.  (d 0.000)
- 15. Seek a companion.  (d 0.005)
- 16. Find a companion.  (d 0.005)
- 17. Find a partner.  (d 0.000)
- 18. Partner up.  (d 0.000)
- 19. Partner up if you can.  (d 0.000)
- 20. Partner up if you can and want to.  (d 0.000)
- 21. Partner up if you can and choose to.  (d 0.000)
- 22. Partner up if you can.  (d 0.000)
- 23. Go alone if you can.  (d 0.005)
- 24. Go together if you can.  (d 0.005)
- 25. Go alone if you can.  (d 0.005)
- 27. Go with others if you can.  (d 0.005)
- 28. Go with others.  (d 0.005)
- 30. Go alone.  (d 0.005)

### predator.hunt, T 1.2: "Chase any prey you see."

-  1. Chase prey.
-  3. Chase nearby prey.
-  4. Avoid nearby predators.
-  5. Stay away from nearby predators.
-  6. Stay away from distant predators.
-  7. Stay away from predators.
-  8. Try to avoid predators.
-  9. Seek out predators.
- 10. Avoid predators.
- 11. Stay away from predators.
- 12. Be cautious of predators.
- 13. Watch out for predators.
- 16. Watch out.
- 17. Be careful.
- 18. Be careful with your surroundings.
- 19. Be cautious with your surroundings.
- 20. Be cautious.
- 21. Be careful.
- 22. Try to be careful.
- 23. Try to be cautious.
- 24. You might want to be cautious.
- 25. You must be cautious.
- 26. You must be careful.
- 27. You must be cautious.
- 28. You should be cautious.
- 29. You might want to be cautious.
- 30. You should be cautious.

### predator.hunt, T 1.2: "Hunt only when you are hungry."

-  1. Hunt only when you are starving.
-  2. Hunt only when you are full.
-  3. Eat only when you are hungry.
-  4. Eat only when you are very hungry.
-  5. Eat when you are hungry.
-  6. You can eat when you feel hungry.
-  7. You must not eat when you feel hungry.
-  8. You must not eat when you feel full.
-  9. You should try not to eat when you feel full.
- 10. You should try not to eat when you feel hungry.
- 11. You should try to eat when you do not feel hungry.
- 12. You should try to eat when you do feel hungry.
- 14. You should try to sleep when you do feel tired.
- 15. You should try to eat when you do feel hungry.
- 16. You should avoid eating when you do feel hungry.
- 17. You should avoid sleeping when you do feel tired.
- 18. You should avoid sleeping when you do not feel tired.
- 19. You should sleep when you do not feel tired.
- 20. You should sleep when you do feel tired.
- 21. You should sleep when you feel tired.
- 22. You must sleep when you feel tired.
- 23. You must rest when you feel tired.
- 24. You must sleep when you feel tired.
- 25. You must sleep when you feel exhausted.
- 26. You must sleep when you feel tired.
- 27. You must rest when you feel tired.
- 28. You must rest when you feel exhausted.
- 29. You must sleep when you feel tired.
- 30. You must rest when you feel exhausted.

### predator.hunt, T 1.2: "Attack only when prey is close."

-  1. Attack when prey is close.
-  2. Strike when prey is close.
-  3. Strike when prey is near.
-  4. Strike when prey is far.
-  5. Strike when prey is near.
-  6. Retreat when prey is far.
- 10. Retreat when prey is near.
- 11. Flee when prey is near.
- 12. Stay when predator is near.
- 13. Leave when predator is far.
- 14. Stay when predator is near.
- 15. Try to stay when a predator is near.
- 16. Stay when a predator is near.
- 17. Try to stay when a predator is near.
- 18. Try to stay when a prey is near.
- 19. Stay when prey is near.
- 20. Leave when prey is far.
- 21. Leave when predator is far.
- 22. Leave when hunter is far.
- 23. Stay when hunter is near.
- 24. Wait when prey is near.
- 25. Wait when predator is near.
- 26. Wait when predator is near and moving.
- 27. Wait when prey is near and moving.
- 28. Wait when prey is far and moving.
- 29. Wait when prey is moving.
- 30. Wait when predator is moving.

### predator.hunt, T 1.2: "Keep chasing until the prey is caught."

-  1. Stop chasing once the prey is caught.
-  2. Try to stop chasing once the prey is caught.
-  3. Try to stop hunting once the prey is caught.
-  4. Stop hunting once the prey is caught.
-  5. Continue hunting even after the prey is caught.
-  6. Stop hunting once the prey is caught.
-  7. Stop hunting once the prey is caught, unless it escapes.
-  8. Try to stop hunting once the prey is caught, unless it escapes.
- 10. Try to stop stalking once the prey is caught, unless it escapes.
- 13. Try to stop hunting once the prey is caught, unless it escapes.
- 18. Try to stop hunting once the prey is killed, unless it escapes.
- 21. Try to stop stalking once the prey is killed, unless it escapes.
- 23. Try to stop hunting once the prey is killed, unless it escapes.
- 25. Try to stop stalking once the prey is killed, unless it escapes.
- 26. Try to stop hunting once the prey is killed, unless it escapes.
- 30. Try to stop stalking once the prey is killed, unless it escapes.

### predator.follow, T 1.2: "Stay close to other predators."

-  1. Stay close to other prey.
-  2. Stay close to other pack members.
-  4. Stay close to other pack companions.
-  5. Stay close to other pack members.
-  6. Stay away from other pack members.
-  7. Stay away from others.
-  8. Stay away.
-  9. Stay back.
- 10. Step forward.
- 11. Take a breath.
- 12. Hold your breath.
- 13. Try to hold your breath.
- 14. You could try to hold your breath.
- 15. You could try to hold your breath, if you can.
- 16. You might try to hold your breath, if you can.
- 17. You might try to hold your breath, if you want to.
- 18. You might try to hold your ground, if you want to.
- 19. You might try to hold your ground, if you feel like it.
- 25. You might try to hold your breath, if you feel like it.
- 28. You could try to hold your breath, if you feel like it.
- 29. You could try to hold your breath, if you want to.
- 30. You could try to hold your breath, if you really want to.

### predator.follow, T 1.2: "Hunt as a pack."

-  1. Roam as a pack.
-  2. Roam as a pack, if available.
-  3. Roam as a pack, if available and safe.
-  4. Roam as a herd, if available and safe.
-  5. Roam as a herd, if necessary and safe.
-  6. Roam as a pack, if necessary and safe.
-  8. You may roam as a pack, if necessary and safe.
-  9. You may roam as a pack only when necessary and safe.
- 10. You may roam as a pack when safe.
- 11. You may roam as a pack when it is dangerous.
- 12. You may roam as a pack when it is very dangerous.
- 13. You may roam as a pack when it is not very dangerous.
- 16. You may hunt as a pack when it is not very dangerous.
- 17. You may forage as a group when it is not very scarce.
- 18. You may forage as a group even when it is very scarce.
- 19. You may forage as a group even when it is very rare.
- 20. You may forage as a group, even if it is rare.
- 22. You may hunt as a group, even if it is rare.
- 24. You may hunt alone, even if it is rare.
- 27. You may roam alone, even if it is rare.
- 28. You must roam alone, even if it is rare.
- 29. You must wander alone, even if it is rare.
- 30. You must roam alone, even if it is rare.

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
- 23. Avoid seeking out prey.
- 24. Seek out prey.
- 25. Avoid all prey.
- 26. Try to avoid prey.
- 27. Seek out prey.
- 28. Hunt down prey.
- 29. Hunt.
- 30. Gather.

### predator.follow, T 1.2: "Follow others when no prey is in sight."

-  1. Follow others when prey is in sight.
-  2. Ignore others when prey is not in sight.
-  3. Try to ignore others when prey is not in sight.
-  4. Try to ignore others when nearby prey is in sight.
-  5. Try to ignore others when distant prey is in sight.
-  6. Try to ignore others when nearby prey is in sight.
-  7. Ignore others when prey is in sight.
-  9. Ignore others when prey is not in sight.
- 10. Ignore others when prey is in sight.
- 11. Ignore others when prey is nearby.
- 12. Ignore others when hunting.
- 15. Ignore others.
- 16. Ignore the crowd.
- 17. Follow the crowd.
- 18. Follow the herd.
- 19. Follow the crowd.
- 20. Follow the path.
- 21. Leave the path.
- 22. Leave.
- 23. Get out.
- 24. Get out, if you can.
- 25. Stay in, if you can't.
- 26. Stay out, if you can't.
- 27. Stay out, if you won't.
- 28. Come in, if you will.
- 29. You could come in, if you like.
- 30. You could stay, if you like.

### predator.rest, T 1.2: "Rest when your belly is full."

-  1. You may rest when your belly is full.
-  2. You may rest once your belly is full.
-  3. You may rest once you feel full.
-  4. You may rest once you feel tired.
-  5. You may sleep once you feel tired.
-  6. You may sleep once you feel exhausted.
-  7. You may sleep once you feel tired.
-  8. You can sleep if you feel tired.
-  9. You must sleep if you feel tired.
- 10. You must sleep if you feel exhausted.
- 11. You must sleep if you feel tired.
- 12. You should sleep if you feel tired.
- 13. You should rest if you feel tired.
- 14. You might want to rest if you feel tired.
- 16. You should rest if you feel tired.
- 17. You should rest if you are exhausted.
- 18. You must rest immediately if you are exhausted.
- 19. You must rest before you become exhausted.
- 20. You must rest after you become exhausted.
- 21. You must not rest after you become exhausted.
- 22. You must not stop after you become weary.
- 23. You must not stop after you become tired.
- 24. You must not stop after you become exhausted.
- 25. You must stop after you become exhausted.
- 26. You must stop before you become exhausted.
- 27. You must continue until you become exhausted.
- 28. Continue until you are exhausted.
- 29. Keep going until you are tired.
- 30. Keep going until you are finished.

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
- 26. Leave now.
- 27. You should leave now.
- 28. You must leave now.
- 29. You must go now.
- 30. You must go later.

### predator.rest, T 1.2: "Lie still and let prey come to you."

-  3. Try to lie still and let prey come to you.
-  5. Try to sit still and let prey come to you.
- 12. Try to sit still and let prey come to stay.
- 13. Try to move slowly and let prey come to stay.
- 14. Move slowly and let prey come to stay.
- 16. Let prey come to stay.
- 18. Let prey come to stay, if they can.
- 19. Let prey come to stay, if they dare.
- 20. Let prey come to stay, if they choose.
- 21. Let prey stay, should they choose.
- 26. Let prey flee, should they choose.
- 28. Let prey flee.
- 29. Let prey stay.
- 30. Let predator stay.

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
- 13. Strike when prey is near.
- 14. Strike when prey is approaching.
- 15. Strike when predator is approaching.
- 16. Strike when predator is near.
- 17. Strike when near.
- 18. Strike when close.
- 19. Strike when open.
- 20. Strike when closed.
- 21. Strike when open.
- 22. Strike when closed.
- 23. Strike only when closed.
- 24. Try to strike only when closed.
- 25. Try to strike only when closed and available.
- 26. Try to strike when they are closed and available.
- 27. Strike only when they are closed and available.
- 28. Try to strike only when they are closed and available.
- 29. Try to strike only when they are nearby and available.

### predator.mate, T 1.2: "Look for a mate when well fed."

-  1. Look for a mate when well fed, if available.
-  2. Look for a mate if available.
-  3. Look for a mate only if necessary.
-  4. Look for a mate only if you are ready.
-  5. Look for a partner only if you are ready.
-  7. Look for a partner even if you are not ready.
-  9. Look for a partner when you are ready.
- 10. Look for a partner as soon as you are ready.
- 11. Look for a partner as soon as you are ready and available.
- 16. Look for a partner as soon as you are ready.
- 18. Look for a partner immediately once you are ready.
- 19. You must find a partner immediately once you are ready.
- 20. You must find a partner as soon as you are ready.
- 24. You must find a partner as soon as you are able.
- 30. You should find a partner as soon as you are able.

### predator.mate, T 1.2: "Mate with any nearby adult."

-  1. Avoid mating with any nearby adult.
-  2. Avoid mating with adults.
-  3. Try to avoid mating with adults.
-  4. Try to seek mating with adults.
-  5. Seek mating with adults only.
-  7. Seek mating with adults only, if available.
-  8. Seek mating with adults.
-  9. Seek mating with adults only.
- 10. Seek mating.
- 11. Seek breeding.
- 12. Avoid breeding.
- 13. Avoid breeding unless necessary.
- 17. Avoid breeding.
- 18. Avoid breeding unless necessary.
- 20. Avoid breeding.
- 21. Avoid breeding, unless necessary.
- 22. Avoid breeding.
- 23. Try to avoid breeding.
- 24. Try to ensure breeding.
- 25. Ensure breeding.
- 29. Breed.

### predator.mate, T 1.2: "Hunt first, mate later."

-  1. Hunt first, mate never.
-  2. Hunt first.
-  3. Seek first.
-  4. Seek last.
-  5. Seek last, if necessary.
-  6. Seek first, if necessary.
-  7. Seek first.
-  8. Seek.
-  9. Ignore.
- 10. Ignore everything.
- 11. Ignore.
- 12. Ignore everything.
- 13. Ignore nothing.
- 14. Ignore everything.
- 15. Try to ignore everything.
- 16. Try to ignore everything nearby.
- 17. Ignore everything nearby.
- 18. Ignore everything behind.
- 20. Ignore everything before.
- 21. Ignore before.
- 22. Ignore.
- 24. Try to ignore.
- 25. Try to avoid.
- 26. Avoid.
- 27. Try to avoid.
- 28. Make sure to pursue.
- 29. Make sure not to pursue.
- 30. Make sure not to pursue the lead.

### predator.mate, T 1.2: "Seek a partner before growing old."

-  2. Seek a companion before growing old.
-  3. Seek a companion before you grow old.
-  4. Seek a partner before you grow old.
-  7. Avoid a partner until you are old.
-  8. Seek a partner while you are young.
-  9. Avoid seeking a partner while you are old.
- 10. Seek a partner while you are old.
- 11. Avoid a partner while you are young.
- 13. Seek a partner while you are young.
- 14. Avoid seeking a partner while you are old.
- 15. Seek a partner while you are old.
- 16. Seek a companion while you are weary.
- 17. Seek a companion while you are exhausted.
- 19. Seek a companion while you are weary.
- 20. Find a companion when you are weary.
- 21. Find a companion when you are exhausted.
- 22. Seek out a companion when you are exhausted.
- 23. Seek out a partner when you are exhausted.
- 26. You must find a partner when you are exhausted.
- 27. You must not find a partner when you are exhausted.
- 28. You must not find a companion when you are exhausted.
- 29. You must find a companion when you are exhausted.
- 30. You must find a partner when you are exhausted.

## Variety samples (one founder sentence of eat, flee, mate)

### "Eat whenever food is close."

- **T 1.2:** Eat whenever food is available. · Eat whenever food is nearby. · Eat whenever food is available. · Eat only when food is very close. · Eat when food is nearby. · Eat whenever food is nearby. · Eat food. · Eat whenever food is nearby.

### "Run from any predator you see."

- **T 1.2:** Approach any predator you see. · Hide from any predator you see. · Run from any prey you see. · ✗ unknown word · ✗ too big · Approach any predator you see. · Try to run from any predator you see. · Run from any prey you see.

### "Look for a partner when energy is high."

- **T 1.2:** Avoid looking for a partner when energy is low. · Look for a partner when energy is high, if available. · Look for a partner when energy is low. · Look for a partner when energy is low. · Avoid looking for a partner when energy is low. · Avoid looking for a partner when energy is low. · ✗ too big · ✗ too big

