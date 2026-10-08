# Mutation test — gemma4:12b (digest 6114515d63c1), 2026-10-08

Pure mutation, no selection. Rules: current (`prompts/mutate_v4.txt`, up to 5 attempts, context line "The sentence below is a rule that a wild animal follows."). Variety: 36 founder sentences × 8 seeds × 1 temperature(s). Lineages: 36 sentences × 30 steps per temperature. 7 instructions. 240 s.

**Meaning** (usable: gemma4:12b says the gene still gives a usable rule for its slot; world word: still uses a word of the animal's world; new outside word: brings in a word that is in neither its parent nor the world words).

| temperature | 1.2 |
|---|---|
| single mutations: usable | 81% |
| … use a world word | 100% |
| … bring in a new outside word | 55% |
| attempts per mutation | 1.04 |
| rejected attempts | invalid 12 |
| lineages after 1 steps: usable / world word | 89% / 100% |
| lineages after 5 steps: usable / world word | 78% / 89% |
| lineages after 10 steps: usable / world word | 50% / 83% |
| lineages after 20 steps: usable / world word | 50% / 78% |
| lineages after 30 steps: usable / world word | 36% / 72% |

| temperature | 1.2 |
|---|---|
| valid answers (after redraws) | 100% |
| distinct mutants per sentence (of 8) | 6.1 |
| words changed per mutation | 2.8 |
| one-word edits | 33% |
| big edits (≥ 4 words) | 30% |
| edit touches the first word | 31% |
| … a middle word | 53% |
| … the last word | 65% |
| length change (words) | +0.25 |
| ≥ 3 words longer | 15% |
| similarity to parent | 0.67 |
| jumps (similarity < 0.3) | 5% |
| keyword brain: no effect (prey genes) | 41% |
| keyword brain: mean d | 0.0195 |
| keyword brain: max d | 0.480 |
| lineages: steps accepted | 100% |
| lineages: words start → end | 6.1 → 7.0 |
| lineages: similarity to start at the end | 0.07 |
| lineages: returns to an earlier sentence | 2.5 |
| lineages: finals alike (starts alike) | 0.08 (0.08) |

Mutations that failed (every attempt rejected), by the last reason: T 1.2: none

## Per instruction (all temperatures)

| instruction | n | valid | words changed | length change | jumps | usable |
|---|---|---|---|---|---|---|
| Make the rule in this sentence a little weaker. | 46 | 100% | 2.9 | +0.7 | 9% | 83% |
| Change when this rule applies. | 35 | 100% | 3.1 | +0.7 | 9% | 86% |
| Add a short condition to this rule. | 26 | 100% | 4.9 | +4.9 | 0% | 100% |
| Remove a condition from this rule, or make it simpler. | 44 | 100% | 3.6 | -3.5 | 11% | 41% |
| Change how near, how far or how much this rule is about. | 44 | 100% | 2.8 | +1.3 | 5% | 98% |
| Make this rule say the opposite. | 49 | 100% | 2.5 | -0.3 | 2% | 86% |
| Change one word of this rule into a related word. | 44 | 100% | 1.0 | +0.0 | 0% | 84% |

## Lineages (each line: a step where the gene changed)

### eat, T 1.2: "Eat whenever food is close."

-  1. Eat whenever food is far.  (d 0.027)
-  2. Eat whenever food is near.  (d 0.000)
-  3. Eat whenever food is far.  (d 0.027)
-  4. Eat food.  (d 0.008)
-  5. Seek food.  (d 0.008)
-  6. Avoid food.  (d 0.204)
-  7. Avoid food near the humans.  (d 0.204)
-  8. Avoid food near the humans if it is not guarded.  (d 0.204)
-  9. Avoid food near the humans if it is guarded.  (d 0.204)
- 10. Try to avoid food near the humans if it is guarded.  (d 0.204)
- 11. Try to avoid food near the humans if it is heavily guarded.  (d 0.204)
- 12. Try to eat food near the humans if it is heavily guarded.  (d 0.008)
- 13. Consider eating food near the humans if it is heavily guarded.  (d 0.008)
- 14. Consider eating food near the humans if it is not heavily guarded.  (d 0.008)
- 15. Avoid eating food near the humans if it is heavily guarded.  (d 0.204)
- 16. Eat food near the humans if it is heavily guarded.  (d 0.008)
- 17. Avoid food near the humans if it is not heavily guarded.  (d 0.204)
- 18. Seek food near the humans if it is heavily guarded.  (d 0.008)
- 19. Seek food far from the humans if it is lightly guarded.  (d 0.008)
- 20. Seek food near humans if it is lightly guarded.  (d 0.008)
- 21. Seek food near humans if it is lightly occupied.  (d 0.008)
- 22. Seek food near humans.  (d 0.008)
- 23. Seek food far from humans.  (d 0.008)
- 24. Seek food near humans.  (d 0.008)
- 25. May seek food near humans.  (d 0.008)
- 26. Must avoid food near humans.  (d 0.204)
- 27. Must avoid humans near food.  (d 0.204)
- 28. Avoid humans.  (d 0.204)
- 29. Avoid humans within fifty feet.  (d 0.204)
- 30. Avoid humans within fifty feet unless they are offering food.  (d 0.204)

### eat, T 1.2: "Only look for food when energy is low."

-  1. Only look for food when hunger is high.  (d 0.010)
-  2. Only look for food when hunger is very high.  (d 0.010)
-  3. Only look for food when hunger is slightly high.  (d 0.010)
-  4. Look for food when hunger is starting to increase.  (d 0.010)
-  5. Look for food as soon as the slightest hunger is felt.  (d 0.010)
-  6. Look for food when hunger is felt.  (d 0.010)
-  7. Look for food when hunger is felt, provided no predators are nearby.  (d 0.007)
-  8. Look for food when hunger is sensed, provided no predators are nearby.  (d 0.007)
-  9. Look for food when hunger is felt, provided no predators are nearby.  (d 0.007)
- 10. Avoid food when hunger is felt, provided predators are nearby.  (d 0.123)
- 11. Avoid food when hunger is felt, provided competitors are nearby.  (d 0.172)
- 12. Seek food when hunger is felt, provided competitors are absent.  (d 0.010)
- 13. Seek food when hunger is felt, unless competitors are too numerous.  (d 0.010)
- 14. Seek food when hunger is felt.  (d 0.010)
- 15. Seek water when hunger is felt.  (d 0.004)
- 16. Avoid water when hunger is felt.  (d 0.172)
- 17. Seek water when hunger is felt.  (d 0.004)
- 18. Seek water.  (d 0.004)
- 19. Seek water when thirsty.  (d 0.004)
- 20. Seek water nearby when thirsty.  (d 0.004)
- 21. Seek water.  (d 0.004)
- 22. Move.  (d 0.004)
- 23. Stay.  (d 0.004)
- 24. Stay within the clearing.  (d 0.004)
- 25. Stay within the clearing unless a predator is detected.  (d 0.004)
- 26. Stay within the thicket unless a predator is detected.  (d 0.004)
- 27. Stay within the thicket.  (d 0.004)
- 28. Stay within the forest.  (d 0.004)
- 29. Stay within the forest when the sun sets.  (d 0.004)
- 30. Stay within the forest.  (d 0.004)

### eat, T 1.2: "Always finish eating before doing anything else."

-  1. Finish eating before doing anything else.  (d 0.070)
-  2. Eat first.  (d 0.070)
-  3. Try to eat first.  (d 0.070)
-  4. Eat.  (d 0.070)
-  5. Eat if hungry.  (d 0.122)
-  6. Eat only if starving.  (d 0.122)
-  7. Eat only if desperate.  (d 0.070)
-  8. Eat.  (d 0.070)
-  9. Try to eat.  (d 0.070)
- 10. Eat.  (d 0.070)
- 11. Try to eat.  (d 0.070)
- 12. Try to eat, if the prey is within reach.  (d 0.070)
- 13. Try to eat, if the prey is within sight.  (d 0.070)
- 14. Try to hunt, if the prey is within sight.  (d 0.153)
- 15. Try to hunt, if the prey is within a mile.  (d 0.153)
- 16. Try to hunt.  (d 0.153)
- 17. Try to hunt, if it is hungry.  (d 0.153)
- 18. Try to forage, if it is hungry.  (d 0.122)
- 19. Try to forage, if it is thirsty.  (d 0.070)
- 20. Try to forage, if it is thirsty, and the area is safe.  (d 0.101)
- 21. Avoid foraging, if it is not thirsty, and the area is unsafe.  (d 0.408)
- 22. Avoid hunting, if it is not thirsty, and the area is unsafe.  (d 0.408)
- 23. Avoid hunting if the area is unsafe.  (d 0.408)
- 24. Try to avoid hunting if the area is unsafe.  (d 0.408)
- 25. Hunt if the area is safe.  (d 0.153)
- 26. Hunt if the area seems relatively safe.  (d 0.153)
- 27. Hunt if the area seems relatively safe and the prey is nearby.  (d 0.153)
- 28. Hunt if the area seems dangerous and the prey is far away.  (d 0.153)
- 29. Hunt if the area seems safe and the prey is close by.  (d 0.153)
- 30. Hunt if the area seems unsafe and the prey is far away.  (d 0.153)

### eat, T 1.2: "Eat quickly, then move on."

-  1. Eat everything, then move on.  (d 0.005)
-  2. Eat a little, then move on.  (d 0.005)
-  3. Eat sometimes, then move on.  (d 0.024)
-  4. Eat constantly, then stay put.  (d 0.005)
-  5. Eat often, then stay put.  (d 0.000)
-  6. Eat rarely, then move around.  (d 0.153)
-  7. Move around, then eat rarely.  (d 0.153)
-  8. Move around, and eat occasionally.  (d 0.024)
-  9. If hungry, move around, and eat occasionally.  (d 0.035)
- 10. If not hungry, stay still, and never eat.  (d 0.125)
- 11. If not hungry and hidden, stay still, and never eat.  (d 0.125)
- 12. If not hungry and hidden, try to stay still and avoid eating.  (d 0.125)
- 13. If hungry and exposed, move and eat.  (d 0.027)
- 14. If hungry and hidden, move and eat.  (d 0.027)
- 15. Move and eat.  (d 0.005)
- 16. Eat.  (d 0.005)
- 17. Hunt.  (d 0.042)
- 18. May hunt.  (d 0.042)
- 19. May hunt if hungry.  (d 0.042)
- 20. May hunt if hungry and if the prey is weak.  (d 0.042)
- 21. May hunt if hungry and if the prey is occasionally weak.  (d 0.035)
- 22. May hunt if hungry and if the prey is consistently weak.  (d 0.042)
- 23. May not hunt if full and if the prey is consistently strong.  (d 0.042)
- 24. May not hunt if full and the prey is very strong.  (d 0.042)
- 25. May not hunt if full and the prey is very fast.  (d 0.042)
- 26. May not hunt if slightly full and the prey is moderately fast.  (d 0.042)
- 27. May not hunt if extremely full and the prey is incredibly fast.  (d 0.042)
- 28. Must hunt if extremely hungry and the prey is incredibly slow.  (d 0.042)
- 29. Must hunt if the prey is incredibly slow.  (d 0.042)
- 30. Must hunt if the prey is incredibly weak.  (d 0.042)

### flee, T 1.2: "Run from any predator you see."

-  1. Approach any predator you see.  (d 0.000)
-  2. Approach some predators you see.  (d 0.013)
-  3. Avoid all predators you see.  (d 0.165)
-  4. Avoid all predators you hear.  (d 0.165)
-  5. Avoid all predators you hear, unless you are already hidden.  (d 0.165)
-  6. Avoid all predators you hear.  (d 0.165)
-  7. Avoid all predators you hear unless you are in a hidden burrow.  (d 0.165)
-  8. Avoid all predators you see unless you are in a hidden burrow.  (d 0.165)
-  9. Avoid predators you see unless you are in a hidden burrow.  (d 0.165)
- 10. Seek out predators you see unless you are in a hidden burrow.  (d 0.013)
- 11. Avoid predators you see unless you are in a hidden burrow.  (d 0.165)
- 12. Avoid prey you see unless you are in a hidden burrow.  (d 0.166)
- 13. Avoid predators you see unless you are in a hidden burrow.  (d 0.165)
- 15. Seek out predators you see unless you are in a hidden burrow.  (d 0.013)
- 16. Avoid predators you see unless you are in a hidden burrow.  (d 0.165)
- 18. Seek out predators you see unless you are in a hidden burrow.  (d 0.013)
- 19. Seek out predators you see.  (d 0.013)
- 20. Seek out predators you see from a distance.  (d 0.013)
- 21. Avoid predators you see from a distance.  (d 0.165)
- 22. Avoid predators.  (d 0.165)
- 23. Try to avoid predators.  (d 0.165)
- 24. Seek out predators.  (d 0.013)
- 25. Seek out prey.  (d 0.013)
- 26. Seek out nearby prey.  (d 0.013)
- 27. Seek out nearby prey if it is not guarded.  (d 0.013)
- 28. Seek out nearby prey if it is guarded.  (d 0.013)
- 29. Seek out distant prey if it is guarded.  (d 0.013)
- 30. Avoid nearby prey if it is unguarded.  (d 0.166)

### flee, T 1.2: "Flee only when a predator is very close."

-  1. Flee when a predator is near.  (d 0.000)
-  2. Flee.  (d 0.000)
-  3. Hide.  (d 0.000)
-  4. Flee.  (d 0.000)
-  5. Flee from the predator.  (d 0.000)
-  6. Fight the predator.  (d 0.000)
-  7. Fight the predators nearby.  (d 0.013)
-  8. Flee the predators nearby.  (d 0.000)
-  9. Flee the predators nearby if they are actively hunting.  (d 0.000)
- 10. Flee the prey nearby if they are actively hunting.  (d 0.000)
- 11. Flee the prey within the next mile if they are actively hunting.  (d 0.000)
- 12. Flee the prey.  (d 0.000)
- 13. Hunt the prey.  (d 0.013)
- 14. Seek out the prey.  (d 0.013)
- 15. Seek out the prey if it is nearby.  (d 0.013)
- 16. Seek out the prey if it is far away.  (d 0.013)
- 17. Consider seeking out the prey if it is far away.  (d 0.013)
- 18. Consider seeking out the prey if it is nearby.  (d 0.013)
- 19. Avoid seeking out the prey if it is far away.  (d 0.167)
- 20. Seek out the prey if it is far away.  (d 0.013)
- 21. Seek out the prey if it is nearby.  (d 0.013)
- 22. Seek out the prey if it is nearby sometimes.  (d 0.005)
- 23. Avoid the prey even if it is nearby always.  (d 0.167)
- 24. Try to avoid the prey if it is nearby.  (d 0.167)
- 25. Try to hunt the prey if it is nearby.  (d 0.013)
- 26. Avoid hunting the prey if it is far away.  (d 0.167)
- 27. Hunt the prey if it is far away.  (d 0.013)
- 28. Chase the prey if it is far away.  (d 0.013)
- 29. Chase the prey if it is nearby.  (d 0.013)
- 30. Chase the prey.  (d 0.013)

### flee, T 1.2: "Stay calm unless danger is right next to you."

-  1. Stay calm unless you sense a potential threat.  (d 0.004)
-  2. Stay calm when you sense a potential threat.  (d 0.004)
-  3. Panic when you sense a potential threat.  (d 0.004)
-  4. Be wary when you sense a potential threat.  (d 0.004)
-  5. Be wary of any potential threat in your immediate surroundings.  (d 0.004)
-  6. Stay aware of your surroundings.  (d 0.004)
-  7. Stay aware of the immediate area around your nose.  (d 0.004)
-  8. Stay aware of the immediate area around your nose when hunting.  (d 0.004)
-  9. Stay aware of your surroundings when hunting.  (d 0.004)
- 10. Stay alert of your surroundings when hunting.  (d 0.004)
- 11. Stay alert.  (d 0.004)
- 12. Stay watchful.  (d 0.004)
- 13. Stay watchful when a predator is near.  (d 0.029)
- 14. Be aware that a predator might be nearby.  (d 0.029)
- 15. Be aware that a scavenger might be nearby.  (d 0.004)
- 16. A scavenger could potentially be nearby.  (d 0.004)
- 17. No scavenger is nearby.  (d 0.004)
- 18. If no scavenger is nearby, the animal will feed.  (d 0.004)
- 19. If no predator is nearby, the animal will feed.  (d 0.029)
- 20. If a predator is nearby, the animal will hide.  (d 0.029)
- 21. The animal will hide.  (d 0.030)
- 22. The animal will hide when it senses a predator.  (d 0.029)
- 23. The animal will hide.  (d 0.030)
- 24. The animal will appear.  (d 0.004)
- 25. The animal will appear when it is hungry.  (d 0.004)
- 26. The animal will appear when it is starving.  (d 0.004)
- 27. The animal will appear when it is only slightly hungry.  (d 0.004)
- 28. The animal will not appear when it is only slightly hungry.  (d 0.004)
- 29. The animal will not appear when it is extremely hungry.  (d 0.004)
- 30. The animal will appear when it is extremely hungry.  (d 0.004)

### flee, T 1.2: "Run away from anything that attacks you."

-  1. Run away from anything that attacks you, unless you are cornered.  (d 0.000)
-  2. Run away from anything that threatens you, unless you are cornered.  (d 0.000)
-  3. Run away from anything that threatens you.  (d 0.000)
-  4. Run away from anything that threatens you, unless you are cornered.  (d 0.000)
-  5. Approach anything that threatens you, unless you are cornered.  (d 0.013)
-  6. Approach anything that threatens you, unless you are cornered or outnumbered.  (d 0.013)
-  7. Approach anything that threatens you, unless you are cornered, outnumbered, or wounded.  (d 0.013)
-  8. Avoid anything that threatens you, unless you are cornered, outnumbered, or wounded.  (d 0.165)
-  9. Attack anything that threatens you, unless you are cornered, outnumbered, or wounded.  (d 0.013)
- 10. Attack anything that threatens you, unless you are cornered, outnumbered, or injured.  (d 0.013)
- 12. Attack anything that threatens you, unless you are cornered, overwhelmed, or injured.  (d 0.013)
- 13. Attack anything that threatens you.  (d 0.013)
- 14. Attack anything that threatens your territory.  (d 0.013)
- 15. Attack anything.  (d 0.013)
- 16. Attack only if threatened.  (d 0.013)
- 17. Attack only if threatened from within ten feet.  (d 0.013)
- 18. Attack only if threatened from within fifty feet.  (d 0.013)
- 19. Attack only if threatened from within ten feet.  (d 0.013)
- 20. Attack if threatened.  (d 0.013)
- 21. Defend if threatened.  (d 0.013)
- 22. Flee if threatened.  (d 0.000)
- 23. Flee.  (d 0.000)
- 24. Stay.  (d 0.013)
- 25. Stay within sight.  (d 0.013)
- 26. Stay within range.  (d 0.013)
- 27. Try to stay within range.  (d 0.013)
- 28. Try to stay within a mile.  (d 0.013)
- 29. Try to stay within a mile of the water source.  (d 0.013)
- 30. Try to stay within a kilometer of the water source.  (d 0.013)

### follow, T 1.2: "Stay close to other animals."

-  1. Stay close.  (d 0.000)
-  2. Stay.  (d 0.007)
-  3. Try to stay.  (d 0.007)
-  4. Consider staying.  (d 0.007)
-  5. Consider hiding.  (d 0.007)
-  6. Consider fleeing.  (d 0.007)
-  7. You might consider fleeing.  (d 0.007)
-  8. You could consider fleeing.  (d 0.007)
-  9. You must flee immediately.  (d 0.007)
- 10. You should consider fleeing.  (d 0.007)
- 11. You might consider fleeing.  (d 0.007)
- 12. You must flee.  (d 0.007)
- 13. You must flee when a predator is detected.  (d 0.007)
- 14. You must flee when a predator is detected within fifty yards.  (d 0.007)
- 15. You must flee when a predator is detected within ten yards.  (d 0.007)
- 16. You must flee when a predator is detected within fifty yards.  (d 0.007)
- 17. You must flee when a hunter is detected within fifty yards.  (d 0.007)
- 18. You must flee when a predator is detected within ten yards.  (d 0.007)
- 19. You must flee when a predator is detected.  (d 0.007)
- 20. You must stay when a predator is detected.  (d 0.007)
- 21. You must stay within sight of a predator.  (d 0.007)
- 22. You must stay within sight of a prey.  (d 0.007)
- 23. You should try to stay within sight of a prey.  (d 0.007)
- 24. You might try to stay within sight of a prey.  (d 0.007)
- 25. You must stay out of sight of a prey.  (d 0.007)
- 26. You must stay in sight of a prey.  (d 0.007)
- 27. You must stay out of sight of a prey.  (d 0.007)
- 28. You must stay out of sight.  (d 0.007)
- 29. You must stay out of sight unless you are hunting.  (d 0.007)
- 30. Stay out of sight.  (d 0.007)

### follow, T 1.2: "Follow others when you are lost or hungry."

-  1. Avoid others when you are lost or hungry.  (d 0.008)
-  2. Seek others when you are found or full.  (d 0.001)
-  3. Seek others when full.  (d 0.001)
-  4. Seek others when full, if they are nearby.  (d 0.001)
-  5. Seek others when hungry, if they are nearby.  (d 0.001)
-  6. Seek others when they are nearby, if they are hungry.  (d 0.001)
-  7. Seek others when they are nearby.  (d 0.001)
-  8. Seek others.  (d 0.001)
-  9. Seek others nearby.  (d 0.001)
- 10. Look for others nearby.  (d 0.001)
- 11. Keep an eye out for others nearby.  (d 0.001)
- 12. Be aware of others nearby.  (d 0.001)
- 13. Be aware of others in the entire territory.  (d 0.001)
- 14. Be aware of others in your immediate vicinity.  (d 0.001)
- 15. Be aware of others in the surrounding territory.  (d 0.001)
- 16. Take note of others in the surrounding territory.  (d 0.001)
- 17. Be aware of others in the surrounding territory.  (d 0.001)
- 18. Be aware of others.  (d 0.001)
- 19. Be aware of others when threatened.  (d 0.001)
- 20. Ignore others when threatened.  (d 0.001)
- 21. Avoid others when threatened.  (d 0.018)
- 22. Avoid others.  (d 0.025)
- 23. Move.  (d 0.001)
- 24. Stay.  (d 0.001)
- 25. Leave.  (d 0.001)
- 26. Flee.  (d 0.001)
- 27. Flee if you are spotted.  (d 0.001)
- 28. Flee.  (d 0.001)
- 29. Flee if a predator is nearby.  (d 0.001)
- 30. Flee if a predator is nearby and you are not cornered.  (d 0.001)

### follow, T 1.2: "Keep your distance from other animals."

-  1. Keep your distance from other creatures.  (d 0.000)
-  2. Keep a minimum of fifty yards from other creatures.  (d 0.000)
-  3. Keep a maximum of fifty yards from other creatures.  (d 0.000)
-  4. Try to maintain at least fifty yards of distance from other creatures.  (d 0.000)
-  5. Try to maintain at least five yards of distance from other creatures.  (d 0.000)
-  6. Try to maintain less than five yards of distance from other creatures.  (d 0.000)
-  7. Try to maintain five yards or more of distance from other creatures.  (d 0.000)
-  8. Stay away from other creatures.  (d 0.000)
-  9. Try to avoid other creatures.  (d 0.018)
- 10. Avoid other creatures.  (d 0.018)
- 11. Try to avoid other creatures.  (d 0.018)
- 12. Sometimes try to avoid other creatures.  (d 0.018)
- 13. Always try to avoid other creatures.  (d 0.018)
- 14. Always seek out other creatures.  (d 0.111)
- 15. Seek out other creatures when possible.  (d 0.000)
- 16. Seek out other creatures when appropriate.  (d 0.000)
- 17. Seek out other creatures when appropriate, provided they are not a threat.  (d 0.000)
- 18. Seek out other creatures.  (d 0.000)
- 19. Seek out other species.  (d 0.000)
- 20. Seek out other species if they are not a threat.  (d 0.000)
- 21. You may seek out other species if they are not a threat.  (d 0.000)
- 22. You must avoid other species if they are a threat.  (d 0.013)
- 23. You should try to avoid other species if they are a threat.  (d 0.013)
- 24. You should seek out other species if they are a threat.  (d 0.000)
- 25. Seek out other species.  (d 0.000)
- 26. Move.  (d 0.000)
- 27. Stay.  (d 0.000)
- 28. Try to stay.  (d 0.000)
- 29. Try to leave.  (d 0.000)
- 30. Consider leaving.  (d 0.000)

### follow, T 1.2: "Follow the strongest animal nearby."

-  1. Follow the weakest animal nearby.  (d 0.003)
-  2. Tend to follow the weakest animal nearby.  (d 0.003)
-  3. Follow the weakest animal nearby.  (d 0.003)
-  4. Follow the weakest animal nearby, unless it is cornered.  (d 0.003)
-  5. Follow the weakest animal nearby.  (d 0.003)
-  6. Follow the weakest animal in the entire forest.  (d 0.003)
-  7. Follow the strongest animal in the entire forest.  (d 0.000)
-  8. Avoid the weakest animal in the entire forest.  (d 0.012)
-  9. Avoid the weakest animal in the entire forest, unless it is wounded.  (d 0.012)
- 10. Avoid the weakest animal in the entire forest, unless it is healthy.  (d 0.012)
- 11. Seek the strongest animal in the entire forest, unless it is unhealthy.  (d 0.004)
- 12. Seek the strongest animal in this clearing, unless it is unhealthy.  (d 0.004)
- 13. Avoid the weakest animal in this clearing, unless it is healthy.  (d 0.012)
- 14. Avoid the weakest animal in this forest, unless it is healthy.  (d 0.012)
- 15. Avoid the weakest animal in this forest.  (d 0.012)
- 16. Seek out the strongest animal in this forest.  (d 0.004)
- 17. Look for a strong animal in this forest.  (d 0.004)
- 18. Look for a weak animal in this forest.  (d 0.004)
- 19. Look for an animal.  (d 0.004)
- 20. You might look for an animal.  (d 0.004)
- 21. You might look for a predator.  (d 0.004)
- 22. You may occasionally look for a predator.  (d 0.003)
- 23. You might occasionally look for a predator.  (d 0.003)
- 24. You are constantly watching for nearby predators.  (d 0.004)
- 25. You are never watching for nearby predators.  (d 0.028)
- 26. You are never watching for nearby prey.  (d 0.036)
- 27. You are never watching for nearby prey unless you are feeling hungry.  (d 0.036)
- 28. You are never watching for distant prey unless you are feeling hungry.  (d 0.036)
- 29. You are never watching for distant prey unless you are feeling famished.  (d 0.036)
- 30. You are always watching for distant prey.  (d 0.082)

### rest, T 1.2: "Rest when you are tired."

-  1. Sleep when you are tired.  (d 0.000)
-  2. Sleep if you feel tired.  (d 0.000)
-  3. Sleep only if you feel extremely exhausted.  (d 0.000)
-  4. Sleep only if you feel slightly tired.  (d 0.000)
-  5. Stay awake even if you feel exhausted.  (d 0.000)
-  6. Stay alert even if you feel exhausted.  (d 0.000)
-  7. Stay alert even if you feel exhausted when a predator is nearby.  (d 0.000)
-  8. Stay alert even if you feel exhausted when a hunter is nearby.  (d 0.000)
-  9. Stay alert when a hunter is nearby.  (d 0.000)
- 10. Be aware that a hunter might be nearby.  (d 0.000)
- 11. A hunter might be nearby.  (d 0.000)
- 12. A hunter could potentially be nearby.  (d 0.000)
- 13. A hunter is definitely far away.  (d 0.000)
- 14. A predator is definitely far away.  (d 0.000)
- 15. A predator is definitely far away if the grass remains still.  (d 0.000)
- 16. A predator is far away.  (d 0.000)
- 17. A predator is near.  (d 0.000)
- 18. A predator is here.  (d 0.000)
- 19. A predator is nearby.  (d 0.000)
- 20. A predator is approaching.  (d 0.000)
- 21. A predator is fleeing.  (d 0.000)
- 22. The predator flees.  (d 0.000)
- 23. The predator hunts.  (d 0.000)
- 24. When it is hungry, the predator hunts.  (d 0.000)
- 25. When it is thirsty, the predator hunts.  (d 0.000)
- 26. When it is thirsty and alone, the predator hunts.  (d 0.000)
- 27. When it is thirsty and in a pack, the predator hunts.  (d 0.000)
- 28. When it is thirsty and in a pack, the predator may hunt.  (d 0.000)
- 29. The predator may hunt.  (d 0.000)
- 30. The hunter may hunt.  (d 0.000)

### rest, T 1.2: "Never stop moving."

-  1. Never stop roaming.  (d 0.000)
-  2. Always stay put.  (d 0.239)
-  3. Stay put.  (d 0.029)
-  4. Try to stay put.  (d 0.029)
-  5. You may stay put.  (d 0.029)
-  6. You might stay put.  (d 0.029)
-  7. You must move.  (d 0.029)
-  8. You must move when you sense a predator.  (d 0.029)
-  9. You must move when you sense a predator within ten feet.  (d 0.029)
- 10. You must flee when you sense a predator within ten feet.  (d 0.029)
- 11. You must stay when you sense a predator within ten feet.  (d 0.029)
- 12. You must hide when you sense a predator within ten feet.  (d 0.029)
- 13. You should hide when you sense a predator within ten feet.  (d 0.029)
- 14. You might consider hiding if you sense a predator within ten feet.  (d 0.029)
- 15. You must stay visible if you sense a predator within ten feet.  (d 0.029)
- 16. You must stay hidden if you sense a predator within ten feet.  (d 0.029)
- 17. You must stay hidden if you sense a predator within fifty feet.  (d 0.029)
- 18. You must stay hidden if you sense a predator.  (d 0.029)
- 19. Stay hidden.  (d 0.029)
- 20. Stay camouflaged.  (d 0.029)
- 21. Try to stay camouflaged.  (d 0.029)
- 22. Hide.  (d 0.029)
- 23. Camouflage.  (d 0.029)
- 24. Camouflage from a distance.  (d 0.029)
- 25. Hide from a distance.  (d 0.029)
- 26. Hide when approached.  (d 0.029)
- 27. Show yourself when approached.  (d 0.029)
- 28. Hide yourself when approached.  (d 0.029)
- 29. Hide.  (d 0.029)
- 30. Show yourself.  (d 0.029)

### rest, T 1.2: "Rest only when you feel safe."

-  1. Rest only when you feel safe in your den.  (d 0.000)
-  2. Hunt only when you feel safe in your den.  (d 0.009)
-  3. Hunt only when you feel unsafe in your den.  (d 0.009)
-  4. Hunt only when you feel unsafe in your territory.  (d 0.009)
-  5. Hunt when you feel unsafe.  (d 0.009)
-  6. Hunt.  (d 0.009)
-  7. Hunt if hungry.  (d 0.009)
-  8. Do not hunt if hungry.  (d 0.022)
-  9. Avoid hunting if you are hungry.  (d 0.022)
- 10. Hunt.  (d 0.009)
- 11. Try to hunt.  (d 0.009)
- 12. Hunt.  (d 0.009)
- 13. Hunt only in the forest.  (d 0.009)
- 14. Hunt primarily in the forest.  (d 0.009)
- 15. Often hunt in the forest.  (d 0.005)
- 16. Hunt in the forest.  (d 0.009)
- 17. Do not hunt in the forest.  (d 0.063)
- 18. Do not hunt in the deep forest.  (d 0.063)
- 19. Do hunt in the deep forest.  (d 0.009)
- 20. Do roam in the deep forest.  (d 0.009)
- 21. Do not roam in the deep forest.  (d 0.063)
- 22. Do not roam in the village.  (d 0.063)
- 23. Do not roam within ten meters of the village.  (d 0.063)
- 24. Try not to wander too close to the village.  (d 0.009)
- 25. Try not to wander too far from the village.  (d 0.009)
- 26. Try not to wander too close to the village.  (d 0.009)
- 27. Avoid the village.  (d 0.063)
- 28. Avoid the village unless you are hungry.  (d 0.061)
- 29. Try to avoid the village unless you are hungry.  (d 0.061)
- 30. Avoid the village unless you are very hungry.  (d 0.061)

### rest, T 1.2: "Save energy by resting when food is far."

-  1. Save energy by resting when food is near.  (d 0.007)
-  2. Rest when food is near to save some energy.  (d 0.007)
-  3. Rest when food is near to save some energy if possible.  (d 0.007)
-  4. Rest when food is near to save some energy.  (d 0.007)
-  5. Rest when food is far away to save more energy.  (d 0.000)
-  6. Act when food is far away to expend more energy.  (d 0.007)
-  7. Consider acting when food is far away to expend more energy.  (d 0.007)
-  8. Consider acting when food is close to expend less energy.  (d 0.007)
-  9. Avoid acting when food is close to conserve energy.  (d 0.024)
- 10. Act when food is close to conserve energy.  (d 0.007)
- 11. Act when food is abundant to conserve energy.  (d 0.007)
- 12. Act to conserve energy.  (d 0.007)
- 13. Act to expend energy.  (d 0.007)
- 14. Act to conserve energy.  (d 0.007)
- 15. Act.  (d 0.007)
- 16. Hide.  (d 0.007)
- 17. Show.  (d 0.007)
- 18. Be.  (d 0.007)
- 19. Try to be.  (d 0.007)
- 20. Try to be when you are hiding.  (d 0.007)
- 21. Be when you are hiding.  (d 0.007)
- 22. Hide.  (d 0.007)
- 23. Hide when a predator is near.  (d 0.007)
- 24. Hide when a hunter is near.  (d 0.007)
- 25. Hide when a hunter is far away.  (d 0.007)
- 26. Show yourself when a hunter is close.  (d 0.007)
- 27. Show yourself when a hunter is far.  (d 0.007)
- 28. Show yourself.  (d 0.007)
- 29. Hide yourself.  (d 0.007)
- 30. Stay hidden.  (d 0.007)

### mate, T 1.2: "Look for a partner when energy is high."

-  1. Look for a mate when energy is high.  (d 0.000)
-  2. Look for a mate.  (d 0.002)
-  3. Look for a mate if it is the breeding season.  (d 0.002)
-  4. Look for a mate if it is the mating season.  (d 0.002)
-  5. Look for a mate if it is the breeding season.  (d 0.002)
-  7. Avoid looking for a mate if it is not the breeding season.  (d 0.040)
-  8. Minimize looking for a mate if it is not the breeding season.  (d 0.002)
-  9. Maximize looking for a mate if it is the breeding season.  (d 0.002)
- 10. Increase looking for a mate if it is the breeding season.  (d 0.002)
- 11. Decrease looking for a mate if it is the breeding season.  (d 0.002)
- 12. Increase looking for a mate if it is the breeding season.  (d 0.002)
- 13. Increase looking for a mate.  (d 0.002)
- 14. Increase looking for a mate within the local territory.  (d 0.002)
- 15. Decrease looking for a mate within the local territory.  (d 0.002)
- 16. Decrease looking for a mate within a neighboring territory.  (d 0.002)
- 17. Reduce looking for a mate within a neighboring territory.  (d 0.002)
- 18. Increase looking for a mate within a neighboring territory.  (d 0.002)
- 19. Consider looking for a mate within a neighboring territory.  (d 0.002)
- 20. Avoid looking for a mate within a neighboring territory.  (d 0.040)
- 21. Seek a mate within a neighboring territory.  (d 0.002)
- 22. Seek a mate within a neighboring territory if possible.  (d 0.002)
- 23. Seek a mate.  (d 0.002)
- 24. Seek a nest.  (d 0.002)
- 25. Try to find a nest.  (d 0.002)
- 26. When you are hungry, try to find a nest.  (d 0.002)
- 27. When you are hungry, try to find a nest nearby.  (d 0.002)
- 28. When you are hungry, try to find a prey nearby.  (d 0.002)
- 29. When you are full, avoid any prey nearby.  (d 0.022)
- 30. When you are hungry, seek out any prey nearby.  (d 0.002)

### mate, T 1.2: "Mate with any nearby adult."

-  1. Mate with any adult within a five-mile radius.  (d 0.000)
-  2. Mate with some adults within a five-mile radius.  (d 0.000)
-  3. Mate with some adults within a ten-mile radius.  (d 0.000)
-  4. Mate with some juveniles within a ten-mile radius.  (d 0.000)
-  5. Mate with juveniles.  (d 0.000)
-  6. Mate with nearby juveniles.  (d 0.000)
-  7. Mate with juveniles across the continent.  (d 0.000)
-  8. Mate across the continent.  (d 0.000)
-  9. Roam across the continent.  (d 0.005)
- 10. Trek across the continent.  (d 0.005)
- 11. Stay in one place.  (d 0.005)
- 12. Stay in one territory.  (d 0.005)
- 13. Stay in a small territory.  (d 0.005)
- 14. Stay in a small territory, unless a mate is found.  (d 0.000)
- 15. Stay in a large territory, unless a mate is found.  (d 0.000)
- 16. Stay in a large territory, if a mate is found.  (d 0.000)
- 17. Leave the territory if a mate is found.  (d 0.000)
- 18. Stay in the territory if a mate is found.  (d 0.000)
- 19. Stay in the territory.  (d 0.005)
- 20. Leave the territory.  (d 0.005)
- 21. Avoid the territory.  (d 0.053)
- 22. Avoid the territory if a predator is nearby.  (d 0.028)
- 23. Enter the territory if a predator is nearby.  (d 0.005)
- 24. Enter the territory if a predator is nearby and it is night.  (d 0.005)
- 25. Enter the territory if a scavenger is nearby and it is night.  (d 0.005)
- 26. Enter the territory if a predator is nearby and it is night.  (d 0.005)
- 27. Enter the territory if a hunter is nearby and it is night.  (d 0.005)
- 28. Enter the territory if it is night.  (d 0.005)
- 29. Enter the territory if it is day.  (d 0.005)
- 30. Do not enter the territory if it is day.  (d 0.053)

### mate, T 1.2: "Mate only when food is plentiful."

-  1. Mate only when food is plentiful and the territory is safe.  (d 0.001)
-  2. Mate when the territory is safe.  (d 0.001)
-  3. Mate when the territory is safe and the moon is full.  (d 0.002)
-  4. Mate when the entire forest is safe and the moon is full.  (d 0.002)
-  5. Mate when the entire meadow is safe and the moon is full.  (d 0.002)
-  6. Mate when the entire forest is safe and the moon is full.  (d 0.002)
-  7. Mate when the moon is full.  (d 0.002)
-  8. Mate.  (d 0.003)
-  9. Seek a mate.  (d 0.003)
- 10. Avoid a mate.  (d 0.036)
- 11. Try to avoid a mate.  (d 0.036)
- 12. Seek out a mate.  (d 0.003)
- 13. Seek out a mate during the breeding season.  (d 0.003)
- 14. Seek out a partner during the breeding season.  (d 0.003)
- 15. Seek out a partner within a five-mile radius during the breeding season.  (d 0.003)
- 16. Seek out a partner within a fifty-mile radius during the breeding season.  (d 0.003)
- 17. Avoid all partners within a fifty-mile radius during the non-breeding season.  (d 0.036)
- 18. Avoid all rivals within a fifty-mile radius during the non-breeding season.  (d 0.036)
- 19. Seek all rivals within a fifty-mile radius during the breeding season.  (d 0.001)
- 20. Seek all rivals within a five-mile radius during the breeding season.  (d 0.001)
- 21. Seek all rivals within a five-mile radius during the mating season.  (d 0.001)
- 22. Seek all rivals within a fifty-mile radius during the mating season.  (d 0.001)
- 23. Avoid all rivals within a fifty-mile radius during the mating season.  (d 0.036)
- 24. Seek all rivals within a fifty-mile radius during the non-mating season.  (d 0.001)
- 25. Seek all predators within a fifty-mile radius during the non-mating season.  (d 0.001)
- 26. Seek all predators within a fifty-mile radius during the mating season.  (d 0.001)
- 27. Seek all predators within a five-mile radius during the mating season.  (d 0.001)
- 28. Avoid all predators within a five-mile radius during the non-mating season.  (d 0.015)
- 29. Avoid some predators within a five-mile radius during the non-mating season.  (d 0.015)
- 30. Seek all predators within a five-mile radius during the mating season.  (d 0.001)

### mate, T 1.2: "Seek a partner before growing old."

-  1. Avoid a partner before growing young.  (d 0.052)
-  2. Avoid a partner before growing many young.  (d 0.052)
-  3. Avoid a partner before growing many young, unless the environment is scarce.  (d 0.052)
-  4. Avoid a mate before growing many young, unless the environment is scarce.  (d 0.052)
-  5. Seek a mate before growing many young, unless the environment is plentiful.  (d 0.002)
-  6. Seek a mate after growing many young, unless the environment is plentiful.  (d 0.002)
-  7. Seek a mate before growing any young, if the environment is plentiful.  (d 0.003)
-  8. Seek a mate before growing any young, if the environment is scarce.  (d 0.000)
-  9. Seek a mate before growing any young, unless the environment is scarce.  (d 0.000)
- 10. Seek a mate before growing any offspring, unless the environment is scarce.  (d 0.000)
- 11. Grow offspring before seeking a mate, unless the environment is scarce.  (d 0.000)
- 12. Grow offspring before seeking a mate, unless the environment is harsh.  (d 0.000)
- 13. Raise offspring before seeking a mate, unless the environment is harsh.  (d 0.000)
- 14. Consider raising offspring before seeking a mate, unless the environment is harsh.  (d 0.000)
- 15. Consider seeking a mate before raising offspring, unless the environment is harsh.  (d 0.000)
- 16. Consider seeking a mate before raising progeny, unless the environment is harsh.  (d 0.000)
- 17. Raise progeny before seeking a mate, even if the environment is harsh.  (d 0.000)
- 18. Seek a mate before raising progeny, even if the environment is harsh.  (d 0.000)
- 19. Seek a mate before raising progeny.  (d 0.000)
- 20. Raise progeny before seeking a mate.  (d 0.000)
- 21. Raise offspring before seeking a mate.  (d 0.000)
- 22. Prefer to raise offspring before seeking a mate.  (d 0.000)
- 23. Prefer to seek a mate before raising offspring.  (d 0.000)
- 24. Seek a mate.  (d 0.000)
- 25. Seek a pack.  (d 0.005)
- 26. Seek a pack when the moon is full.  (d 0.005)
- 27. Seek a herd when the moon is full.  (d 0.005)
- 28. Seek a herd.  (d 0.005)
- 29. Move.  (d 0.005)
- 30. Move if you sense danger.  (d 0.005)

### predator.hunt, T 1.2: "Chase any prey you see."

-  1. Chase any prey you see if you are hungry.
-  2. Chase some prey you see if you are hungry.
-  3. Chase all prey you see if you are hungry.
-  4. Chase all prey you see if you are not hungry.
-  5. Chase all prey you see if you are hungry.
-  6. Hunt all prey you see if you are hungry.
-  7. Hunt some prey you see if you are hungry.
-  8. Hunt some prey you see if you are very hungry.
-  9. Hunt some prey you see if you are not very hungry.
- 10. Hunt some prey you see if you are very hungry.
- 11. Hunt some prey you see.
- 12. Hunt some prey you hear.
- 13. Hunt prey.
- 14. Protect prey.
- 15. Protect territory.
- 16. Protect territory when threatened.
- 17. Defend territory when threatened.
- 18. Defend territory when threatened, unless the predator is significantly larger.
- 19. Defend territory when threatened, unless the predator is significantly stronger.
- 20. Defend territory when threatened, unless the predator seems likely to win.
- 21. Defend territory when threatened, unless the predator seems likely to escape.
- 22. Defend territory when threatened, unless the predator seems likely to attack.
- 23. Retreat from territory when threatened, unless the predator seems unlikely to attack.
- 24. Retreat from territory when threatened.
- 25. Advance into territory when threatened.
- 26. Advance when threatened.
- 27. Advance if threatened.
- 28. Advance.
- 29. Advance rapidly toward the prey.
- 30. Advance toward the prey.

### predator.hunt, T 1.2: "Hunt only when you are hungry."

-  1. Hunt.
-  2. Eat.
-  3. Hunt.
-  4. Hunt only the nearest prey.
-  5. Hunt the nearest prey if possible.
-  6. Hunt the nearest prey if not possible.
-  7. Hunt the nearest prey if it seems possible.
-  8. Hunt the prey that is most abundant if it seems possible.
-  9. Hunt the prey that is least abundant if it seems possible.
- 10. Hunt the prey that is most abundant if it seems possible.
- 11. Hunt the prey that is most plentiful if it seems possible.
- 12. Hunt the most plentiful prey.
- 13. Hunt for plentiful prey.
- 14. Hunt for a single prey.
- 15. Hunt for a single prey if it is within reach.
- 16. Hunt for a single prey.
- 17. Avoid multiple prey.
- 18. Attack multiple prey.
- 19. Attack single prey.
- 20. Attack a pack of prey.
- 21. Hunt a pack of prey.
- 22. Flee from a pack of prey.
- 23. Flee from a pack of predators.
- 24. Flee from a pack of prey.
- 25. Flee.
- 26. Approach.
- 27. Retreat.
- 28. Retreat if cornered.
- 29. Flee if cornered.
- 30. Fight if cornered.

### predator.hunt, T 1.2: "Attack only when prey is close."

-  1. Attack only when prey is close and the path is clear.
-  2. Avoid attacking when prey is far and the path is blocked.
-  3. Attack when prey is near and the path is clear.
-  4. Attack when prey is near.
-  5. Hunt when prey is near.
-  6. Stalk when prey is near.
-  7. Stalk when prey is far.
-  8. Run when prey is near.
-  9. Run when prey is near and you are not being hunted.
- 10. Run when prey is far and you are being hunted.
- 11. Run if prey is far and you are being hunted.
- 12. Stop if prey is near and you are not being hunted.
- 13. Slow down if prey is near and you are not being hunted.
- 14. Slow down if prey is far and you are not being hunted.
- 15. Slow down if prey is far.
- 16. You may slow down if prey is far.
- 17. You must speed up if prey is near.
- 18. You must slow down if prey is far.
- 19. You must speed up if prey is far.
- 20. You must speed up.
- 21. You must speed up significantly.
- 22. You must flee significantly.
- 23. You must stay perfectly still.
- 24. You must stay perfectly hidden.
- 25. You must be perfectly visible.
- 26. You must be visible.
- 27. You must be there.
- 28. You must not be there.
- 29. You must not be there if a human is watching.
- 30. You should try not to be there if a human is watching.

### predator.hunt, T 1.2: "Keep chasing until the prey is caught."

-  1. Chase the prey.
-  2. Flee from the prey.
-  3. Consider fleeing from the prey.
-  4. Consider chasing the prey.
-  5. Chase the prey.
-  6. Run.
-  7. Run away.
-  8. Hide away.
-  9. Try to hide.
- 10. Try to hide nearby.
- 11. Hide.
- 12. Appear.
- 13. Be.
- 14. Be, if you can.
- 15. Don't, if you can't.
- 16. Do, even if you can't.
- 17. Do, even if you can't, unless you are cornered.
- 18. Try, if you can, unless you are cornered.
- 19. Try, if you can, unless you are trapped.
- 20. Try.
- 21. Try, if the prey is close enough.
- 22. Consider trying, if the prey is close enough.
- 23. Consider trying, if the prey is far enough.
- 24. Give up, if the prey is close enough.
- 25. Attack, if the prey is close enough.
- 26. Retreat, if the prey is far enough.
- 27. Retreat, if the prey is far enough and the hunter is near.
- 28. Advance, if the prey is near enough and the hunter is far.
- 29. Retreat, if the prey is far enough and the hunter is near.
- 30. Advance, if the prey is near enough and the hunter is far.

### predator.follow, T 1.2: "Stay close to other predators."

-  1. Stay close to other prey.
-  2. Stay close to other pack.
-  3. Stay very far from other packs.
-  4. Stay very far from other packs unless they are approaching your territory.
-  5. Stay moderately close to other packs unless they are approaching your territory.
-  6. Stay moderately far from other packs unless they are approaching your territory.
-  7. Stay extremely close to other packs unless they are approaching your territory.
-  8. Stay close to other packs.
-  9. Stay with other packs.
- 10. Leave other packs.
- 11. Leave packs within a mile.
- 12. Leave packs within a yard.
- 13. Leave packs within a yard if they are edible.
- 14. Leave packs within a yard if they are poisonous.
- 15. Leave packs within a yard.
- 16. Leave packs within a lair.
- 17. Leave packs within a lair if they are not currently being consumed.
- 18. Leave packs within a lair.
- 19. Leave packs when hunting.
- 20. Leave small packs when hunting.
- 21. Hunt in large packs.
- 22. Hunt in large prides.
- 23. Hunt in large prides when the prey is big.
- 24. Hunt in large prides sometimes when the prey is big.
- 25. Hunt in large packs sometimes when the prey is big.
- 26. Hunt in large packs.
- 27. Hunt in large packs when prey is large.
- 28. Hunt in packs when prey is large.
- 29. Hunt in packs.
- 30. Stalk in packs.

### predator.follow, T 1.2: "Hunt as a pack."

-  1. Hunt alone.
-  2. Hunt alone unless the prey is too large.
-  3. Hunt in a pack unless the prey is too small.
-  4. Hunt alone unless the prey is large enough.
-  5. Hunt alone unless the prey is very large.
-  6. Hunt together unless the prey is very large.
-  7. Hunt alone unless the prey is very small.
-  8. Hunt alone unless the prey is very tiny.
-  9. Hunt in a pack unless the prey is very large.
- 10. Hunt alone unless the prey is very small.
- 11. Hunt in a group unless the prey is very large.
- 12. Hunt in a group if the prey is large.
- 13. Hunt in a group if the prey is small.
- 14. Hunt in a group if the prey is large.
- 15. Hunt in a group if the prey is small.
- 16. Hunt in a group if the prey is small and moving slowly.
- 17. Hunt in a group if the prey is large and moving quickly.
- 18. Hunt in a pack if the prey is large and moving quickly.
- 19. Hunt in a pack.
- 20. Hunt in a pack when prey is large.
- 21. Hunt in a pack.
- 22. Hunt in a pack if the prey is large.
- 23. Hunt in a pack if the prey is small.
- 24. Hunt in a pack.
- 25. Stalk in a pack.
- 26. Prefer to stalk in a pack.
- 27. Prefer to stalk in a herd.
- 28. May stalk in a herd.
- 29. Must not stalk in a herd.
- 30. Must not stalk within a mile of a herd.

### predator.follow, T 1.2: "Keep away from other predators."

-  1. Keep away from other prey.
-  2. Try to avoid other prey.
-  3. Try to avoid predators.
-  4. Try to avoid predators when you are alone.
-  5. Try to avoid danger when you are alone.
-  6. Avoid danger.
-  7. Avoid danger, unless food is nearby.
-  8. Seek danger, unless food is nearby.
-  9. Avoid danger, unless food is nearby.
- 10. Seek food, unless danger is nearby.
- 11. Seek shelter, unless danger is nearby.
- 12. Seek shelter, if danger is nearby.
- 13. Seek shelter.
- 14. Seek refuge.
- 15. Hide.
- 16. Hide if you are spotted.
- 17. Hide immediately if you are spotted.
- 18. Hide immediately if you are attacked.
- 19. Flee immediately if you are attacked.
- 20. Flee immediately if you are cornered.
- 21. Flee immediately if you are being pursued by a pack of predators.
- 22. Stay immediately if you are being pursued by a pack of predators.
- 23. Hide immediately if you are being pursued by a pack of predators.
- 24. Consider hiding if you are being pursued by a pack of predators.
- 25. Consider hiding if you are being pursued by a single predator.
- 26. Consider hiding if you are being pursued by multiple predators.
- 27. Consider fleeing if you are being pursued by multiple predators.
- 28. Consider fleeing if you are being pursued by any predator.
- 29. Consider fleeing if you are being pursued by a large predator.
- 30. Consider fleeing if you are being chased by a smaller scavenger.

### predator.follow, T 1.2: "Follow others when no prey is in sight."

-  1. Sometimes follow others when no prey is in sight.
-  2. Sometimes hunt others when no prey is in sight.
-  3. Always hunt others whenever any prey is in sight.
-  4. Only hunt others when any prey is in sight.
-  5. Only hunt others when prey is within a five-meter radius.
-  6. Only hunt others when prey is outside a five-meter radius.
-  7. Hunt others when prey is outside a five-meter radius.
-  8. Hunt others.
-  9. Hunt others only if they are weak.
- 10. Hunt others only if they are very weak.
- 11. Hunt others only if they are slightly weak.
- 12. Hunt others only if they are completely helpless.
- 13. Hunt others only if they are not completely helpless.
- 14. Hunt others only if they are not completely defenseless.
- 15. Hunt others only if they are completely defenseless.
- 16. Hunt others.
- 17. Eat.
- 18. Try to eat.
- 19. Consider eating.
- 20. Eat.
- 21. Eat a lot.
- 22. Eat a lot when hungry.
- 23. Eat only a little when hungry.
- 24. Eat only a little when very hungry.
- 25. Eat a lot when very hungry.
- 26. Eat a little when slightly hungry.
- 27. Eat when hungry.
- 28. Eat as much as possible whenever hungry.
- 29. Eat as much as possible whenever starving.
- 30. Eat as much as possible.

### predator.rest, T 1.2: "Rest when your belly is full."

-  1. Rest if your belly is full.
-  2. Rest if your belly is mostly full.
-  3. Rest if your belly is mostly full and no predators are nearby.
-  4. Rest if your belly is mostly empty and no predators are nearby.
-  5. Stay active if your belly is mostly full and predators are nearby.
-  6. Stay alert if your belly is mostly full and predators are nearby.
-  7. Stay alert if predators are nearby.
-  8. Stay alert if prey is nearby.
-  9. Relax if prey is far away.
- 10. Relax.
- 11. Sleep.
- 12. Sleep at night.
- 13. Wake during the day.
- 14. Wake up.
- 15. Sleep.
- 16. Wake.
- 17. Wake when the sun rises.
- 18. Wake when the moon rises.
- 19. Wake when the moon rises, if the wind is still.
- 20. Sleep when the moon rises, if the wind is still.
- 21. Sleep when the moon rises.
- 22. Wake when the moon rises.
- 23. Wake when the moon rises, if the wind is still.
- 24. Sleep when the moon rises, if the wind is still.
- 25. Stay awake when the moon sets, if the wind is blowing.
- 26. Stay awake when the moon rises, if the wind is blowing.
- 27. Stay awake for the entire night if the wind is blowing.
- 28. Stay alert for the entire night if the wind is blowing.
- 29. Stay alert for the entire night.
- 30. Stay vigilant for the entire night.

### predator.rest, T 1.2: "Never stop moving."

-  1. Never stop moving unless a predator is near.
-  2. Always stop moving unless a predator is not near.
-  3. Always move unless a predator is near.
-  4. Always move unless a predator is within sight.
-  5. Move unless a predator is within sight.
-  6. Stay still unless a predator is within sight.
-  7. Move constantly unless a predator is within sight.
-  8. Stay still unless a predator is out of sight.
-  9. Move unless a predator is hidden from view.
- 10. Hide unless a predator is hidden from view.
- 11. Hide unless a predator is visible from view.
- 12. Hide.
- 13. Hide deep inside a cave.
- 14. Hide deep inside a cave when a predator is nearby.
- 15. Hide inside a cave.
- 16. Stay outside a cave.
- 17. Enter a cave.
- 18. Leave a cave.
- 19. Enter a cave.
- 20. Sometimes enter a cave.
- 21. Enter a cave when it rains.
- 22. Enter a cave when it snows.
- 23. Enter a cave.
- 24. Enter a cave if it is dark.
- 25. Enter a cave if it is light.
- 26. Enter a cave if it is dark.
- 27. Enter a cave.
- 28. May enter a cave.
- 29. Might enter a cave.
- 30. Never enters a cave.

### predator.rest, T 1.2: "Lie still and let prey come to you."

-  1. Wait for prey to come near.
-  2. Wait for the sun to set.
-  3. Wait for the moon to rise.
-  4. Wait for the moon to rise over the mountain.
-  5. Wait for the moon to rise over the distant valley.
-  6. Wait for the sun to set behind the jagged peaks.
-  7. Wait for the sun to set behind the distant horizon.
-  8. Wait for the sun to start setting toward the horizon.
-  9. Wait for the sun to set.
- 10. Wait.
- 11. Wait if possible.
- 12. Wait if possible for several minutes.
- 13. Wait if possible for several seconds.
- 14. Act immediately if possible.
- 15. Act immediately if threatened.
- 16. Act immediately.
- 17. Hunt immediately.
- 18. Hunt only if food is nearby.
- 19. Hunt.
- 20. Flee.
- 21. Flee if a predator is spotted.
- 22. Flee if a predator is spotted within ten feet.
- 23. Flee if a predator is spotted nearby.
- 24. Consider fleeing if a predator is spotted nearby.
- 25. Consider fleeing if a predator is spotted nearby and you are alone.
- 26. Consider fleeing if a predator is spotted nearby and you are vulnerable.
- 27. Consider fleeing if a predator is spotted nearby and you are resting.
- 28. Consider fleeing if a predator is spotted nearby.
- 29. You might consider fleeing if a predator is spotted nearby.
- 30. You may occasionally consider fleeing if a predator is spotted nearby.

### predator.rest, T 1.2: "Rest when no prey is in sight."

-  1. Rest when no food is in sight.
-  2. Rest when food is in sight.
-  3. Move when food is in sight.
-  4. Move.
-  5. Run.
-  6. Run if you hear a predator.
-  7. Run if you see a predator.
-  8. Run if you see a hunter.
-  9. Run if you see a hunter and they are armed.
- 10. Run if you see a hunter who is not armed.
- 11. Run if you see a hunter.
- 12. Consider running if you see a hunter.
- 13. Stay still if you see a hunter.
- 14. Move if you see a hunter.
- 15. Consider moving if you see a hunter.
- 16. You might consider moving if you see a hunter.
- 17. Move if you see a hunter.
- 18. Stay still if you see a hunter.
- 19. Stay still if you see a hunter within fifty yards.
- 20. Stay still if you see a hunter nearby.
- 21. Try to stay still if you see a hunter nearby.
- 22. Stay still.
- 23. Move.
- 24. Sit.
- 25. Sit if you are tired.
- 26. Run if you are hungry.
- 27. Run.
- 28. Stay.
- 29. Hide.
- 30. Hide when a predator is nearby.

### predator.mate, T 1.2: "Look for a mate when well fed."

-  1. Look for a mate when well fed and the weather is clear.
-  2. Look for a mate.
-  3. Look for a mate during the spring mating season.
-  4. Try to look for a mate during the spring mating season.
-  5. Try to look for a partner during the spring mating season.
-  6. Try to look for a partner during the winter hibernation season.
-  7. Try to look for a mate during the winter hibernation season.
-  8. Try to look for a mate during the spring mating season.
-  9. You may look for a mate during the spring mating season.
- 10. You may look for a mate during the spring breeding season.
- 11. You may not look for a mate during the spring breeding season.
- 12. You may not look for a mate.
- 13. You may not look for a mate within a five-mile radius.
- 14. You may not look for a mate within a ten-mile radius.
- 15. You may not look for a mate.
- 16. You may not look for a mate unless it is mating season.
- 17. You may not look for a mate unless it is breeding season.
- 18. You may look for a mate.
- 19. You may look for a pack.
- 20. You may look for a pack nearby.
- 21. You might look for a pack nearby.
- 22. You might look for a herd nearby.
- 23. You could potentially look for a herd nearby.
- 24. You could potentially look for a herd across the valley.
- 25. You could look for a herd across the valley.
- 26. You might look for a herd across the valley.
- 27. Look for a herd across the valley.
- 28. Look across the valley.
- 29. Look across the forest.
- 30. Look across the forest when a predator is nearby.

### predator.mate, T 1.2: "Mate with any nearby adult."

-  1. Avoid all nearby adults for mating.
-  2. Try to avoid nearby adults for mating.
-  3. Try to avoid nearby adults for mating unless they are non-aggressive.
-  4. Prefer to avoid nearby adults for mating unless they are non-aggressive.
-  5. Prefer to avoid distant adults for mating unless they are non-aggressive.
-  6. Prefer to avoid nearby adults for mating unless they are non-aggressive.
-  7. Prefer to avoid nearby rivals for mating unless they are non-aggressive.
-  8. Try to avoid nearby rivals for mating, though some may be non-aggressive.
-  9. Try to avoid nearby competitors for mating, though some may be non-aggressive.
- 10. Avoid nearby competitors for mating.
- 11. Avoid nearby predators for mating.
- 12. Avoid distant predators for mating.
- 13. Avoid distant predators for mating, unless a mate is nearby.
- 14. Avoid distant competitors for mating, unless a mate is nearby.
- 15. Avoid local competitors for mating, unless a mate is nearby.
- 16. Avoid local rivals for mating, unless a mate is nearby.
- 17. Avoid local rivals for mating.
- 18. Avoid distant rivals for mating.
- 19. Seek distant rivals for mating.
- 20. Seek occasional rivals for mating.
- 21. Seek occasional rivals for mating when they are visible.
- 22. Seek occasional rivals for mating when they are visible and non-aggressive.
- 23. Avoid all potential rivals for mating when they are hidden and aggressive.
- 24. Avoid all potential rivals for mating when they are visible and passive.
- 25. Avoid some potential rivals for mating when they are visible and passive.
- 26. Seek all potential rivals for mating when they are hidden and active.
- 27. Seek all potential rivals for mating.
- 28. Seek all potential rivals within a five-mile radius for mating.
- 29. Seek all potential rivals within a one-mile radius for mating.
- 30. Scan for potential rivals within a one-mile radius for mating.

### predator.mate, T 1.2: "Hunt first, mate later."

-  1. Prioritize hunting, but don't forget to mate.
-  2. Prioritize hunting.
-  3. Prioritize hunting over all other behaviors.
-  4. Prioritize all other behaviors over hunting.
-  5. Do not hunt.
-  6. Do not forage.
-  7. Forage only when food is scarce.
-  8. Forage when food is scarce.
-  9. Forage when food is abundant.
- 10. Forage when food is available.
- 11. Forage when food is available, provided no predators are nearby.
- 12. Forage when no predators are nearby, even if food is not available.
- 13. Forage when no predators are nearby.
- 14. Forage.
- 15. Forage for 2 hours.
- 16. Forage for 5 miles.
- 17. Forage for 10 miles.
- 18. Forage for 10 miles if no predators are nearby.
- 19. Forage for 10 miles.
- 20. Hunt for 10 miles.
- 21. Hunt.
- 22. Try to hunt.
- 23. Hunt.
- 24. Hunt nearby.
- 25. Hunt occasionally nearby.
- 26. Hunt occasionally nearby when hungry.
- 27. Hunt constantly across the continent when starving.
- 28. Hunt constantly across the meadow when starving.
- 29. Rest constantly across the meadow when fed.
- 30. Move constantly across the meadow when hungry.

### predator.mate, T 1.2: "Seek a partner before growing old."

-  1. Seek a partner before growing old, if one is available.
-  2. Seek a partner before growing old.
-  3. Grow old before seeking a partner.
-  4. Grow mature before seeking a partner.
-  5. Seek a partner before growing mature.
-  6. Grow mature before seeking a partner.
-  7. Seek a partner before growing mature.
-  8. Grow mature before seeking a partner.
-  9. Seek a partner before growing mature.
- 10. Seek multiple partners before growing mature.
- 11. Avoid multiple partners before growing mature.
- 12. Avoid multiple partners before growing mature, unless a predator is nearby.
- 13. Avoid multiple partners before growing mature, unless a hunter is nearby.
- 14. Avoid multiple partners before growing mature, unless a predator is nearby.
- 15. Avoid multiple partners before growing older, unless a predator is nearby.
- 16. Avoid multiple partners before growing younger, unless a predator is nearby.
- 17. Avoid multiple partners before growing younger.
- 18. Avoid multiple partners.
- 19. Prefer a single partner.
- 20. Prefer a single partner, unless a mate is lost.
- 21. Prefer a single partner, unless three mates are lost.
- 22. Prefer multiple partners, unless three mates are gained.
- 23. Consider multiple partners, unless three mates are gained.
- 24. Consider multiple partners, unless twelve mates are gained.
- 25. Stick to a single partner, even if twelve mates are gained.
- 26. Seek many partners, even if only one mate is gained.
- 27. Seek a few partners, even if only one mate is gained.
- 28. Seek a few partners.
- 29. Seek partners.
- 30. Avoid partners.

## Variety samples (one founder sentence of eat, flee, mate)

### "Eat whenever food is close."

- **T 1.2:** Do not eat when food is close. · Eat food. · Hunt whenever food is close. · Eat. · Eat. · Eat whenever food is very far away. · Eat. · Avoid eating whenever food is far.

### "Run from any predator you see."

- **T 1.2:** Approach any predator you see. · Run from any predator. · Run from any predator you see, unless it is already attacking you. · Run from any predator you hear. · Run from any predator you see within fifty feet. · Run from some predators you see. · Run from any predator you see, unless you are cornered. · Approach any predator you see.

### "Look for a partner when energy is high."

- **T 1.2:** Look for a partner. · Look for a partner. · Look for a partner when energy is low. · Avoid a partner when energy is low. · Look for a partner when energy is very high. · Look for a partner. · Look for a partner when energy is very high. · Consider looking for a partner when energy is high.

