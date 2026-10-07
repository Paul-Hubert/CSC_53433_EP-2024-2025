# Mutation test — gemma4:12b (digest 6114515d63c1), 2026-10-08

Pure mutation, no selection. Rules: current (`prompts/mutate_v3.txt`, up to 5 attempts). Variety: 36 founder sentences × 8 seeds × 1 temperature(s). Lineages: 36 sentences × 30 steps per temperature. 9 instructions. 186 s.

**Meaning** (usable: gemma4:12b says the gene still gives a usable rule for its slot; world word: still uses a word of the animal's world; new outside word: brings in a word that is in neither its parent nor the world words).

| temperature | 1.2 |
|---|---|
| single mutations: usable | 80% |
| … use a world word | 97% |
| … bring in a new outside word | 72% |
| attempts per mutation | 1.01 |
| rejected attempts | invalid 4 |
| lineages after 1 steps: usable / world word | 89% / 94% |
| lineages after 5 steps: usable / world word | 67% / 81% |
| lineages after 10 steps: usable / world word | 56% / 53% |
| lineages after 20 steps: usable / world word | 39% / 47% |
| lineages after 30 steps: usable / world word | 33% / 33% |

| temperature | 1.2 |
|---|---|
| valid answers (after redraws) | 100% |
| distinct mutants per sentence (of 8) | 6.7 |
| words changed per mutation | 3.1 |
| one-word edits | 25% |
| big edits (≥ 4 words) | 39% |
| edit touches the first word | 49% |
| … a middle word | 59% |
| … the last word | 61% |
| length change (words) | +0.47 |
| ≥ 3 words longer | 15% |
| similarity to parent | 0.63 |
| jumps (similarity < 0.3) | 10% |
| keyword brain: no effect (prey genes) | 34% |
| keyword brain: mean d | 0.0165 |
| keyword brain: max d | 0.480 |
| lineages: steps accepted | 100% |
| lineages: words start → end | 6.1 → 7.0 |
| lineages: similarity to start at the end | 0.05 |
| lineages: returns to an earlier sentence | 1.9 |
| lineages: finals alike (starts alike) | 0.05 (0.08) |

Mutations that failed (every attempt rejected), by the last reason: T 1.2: none

## Per instruction (all temperatures)

| instruction | n | valid | words changed | length change | jumps | usable |
|---|---|---|---|---|---|---|
| Make the rule in this sentence a little stronger. | 36 | 100% | 4.2 | +1.1 | 19% | 72% |
| Make the rule in this sentence a little weaker. | 34 | 100% | 2.4 | +1.2 | 3% | 85% |
| Change when this rule applies. | 25 | 100% | 2.8 | +0.2 | 8% | 72% |
| Add a short condition to this rule. | 24 | 100% | 3.8 | +3.8 | 0% | 100% |
| Remove a condition from this rule, or make it simpler. | 36 | 100% | 3.6 | -3.1 | 6% | 61% |
| Change how near, how far or how much this rule is about. | 29 | 100% | 2.8 | +1.0 | 3% | 86% |
| Make this rule say the opposite. | 38 | 100% | 2.7 | -0.1 | 3% | 79% |
| Change one word of this rule into a related word. | 37 | 100% | 1.1 | -0.1 | 0% | 76% |
| Say this rule in slightly different words. | 29 | 100% | 5.4 | +1.7 | 48% | 93% |

## Lineages (each line: a step where the gene changed)

### eat, T 1.2: "Eat whenever food is close."

-  1. Eat whenever food is available.  (d 0.020)
-  2. Consume food whenever it is accessible.  (d 0.020)
-  3. Consume food whenever it is available within reach.  (d 0.020)
-  4. Consume food whenever it is available in the next room.  (d 0.020)
-  5. You may consume food when it is available in the next room.  (d 0.008)
-  6. You may consume food when it is available in the next building.  (d 0.008)
-  7. You may consume food if it is available in the next building.  (d 0.008)
-  8. You may consume food if it is available within a three-building radius.  (d 0.008)
-  9. You may consume food if it is available within a five-building radius.  (d 0.008)
- 10. You may consume food if it is available within a ten-building radius.  (d 0.008)
- 11. You may consume food if it is available within a five-building radius.  (d 0.008)
- 13. You may consume food if it is available within a five-block radius.  (d 0.008)
- 14. You may consume food if it is available within a ten-block radius.  (d 0.008)
- 15. You may consume food if it is available within a one-block radius.  (d 0.008)
- 16. You may consume food if it is available within a five-block radius.  (d 0.008)
- 17. You may consume food if it is available within a five-mile radius.  (d 0.008)
- 18. You may consume food if it is available within a reasonable distance.  (d 0.008)
- 19. You may consume food if it is available nearby.  (d 0.008)
- 20. You may consume food if it is available in the next room.  (d 0.008)
- 21. You must only consume food that is available in the next room.  (d 0.008)
- 22. You must only consume food that is available in the current room.  (d 0.008)
- 23. You must only consume food that is available in the current kitchen.  (d 0.008)
- 24. Only eat items currently stored in the kitchen.  (d 0.008)
- 25. Eat items stored in the kitchen.  (d 0.008)
- 26. Eat all items stored in the kitchen.  (d 0.008)
- 27. Consume everything located in the kitchen.  (d 0.012)
- 28. Consume everything located on the kitchen counter.  (d 0.012)
- 29. Clear the kitchen counter by consuming every single item located on it.  (d 0.012)
- 30. Clear the kitchen counter by removing the items on it.  (d 0.012)

### eat, T 1.2: "Only look for food when energy is low."

-  1. Only look for food when you are hungry.  (d 0.000)
-  2. Try to only look for food when you are hungry.  (d 0.000)
-  3. Try to only look for food when you are very hungry.  (d 0.000)
-  4. Try to avoid looking for food unless you are feeling hungry.  (d 0.125)
-  5. Look for food even if you are not feeling hungry.  (d 0.000)
-  6. Seek out food regardless of whether you feel hungry.  (d 0.000)
-  7. Seek out food only when you feel hungry.  (d 0.000)
-  8. Only eat when you experience hunger.  (d 0.010)
-  9. Consume food only when you feel hungry.  (d 0.000)
- 10. Consume meals only when you feel hungry.  (d 0.004)
- 11. Consume food only when you feel hungry.  (d 0.000)
- 12. Consume food only when you do not feel hungry.  (d 0.058)
- 13. Refrain from consuming food unless you are truly hungry.  (d 0.018)
- 14. Refrain from consuming food unless you are feeling peckish.  (d 0.010)
- 15. Refrain from consuming food.  (d 0.010)
- 16. Refrain from consuming snacks.  (d 0.004)
- 17. Refrain from consuming meals.  (d 0.004)
- 18. Try to avoid eating meals.  (d 0.172)
- 19. Avoid eating.  (d 0.172)
- 20. Avoid eating before 12 PM.  (d 0.172)
- 21. Avoid eating until 1 PM.  (d 0.172)
- 22. Don't eat yet.  (d 0.172)
- 23. Don't eat yet until the food is hot.  (d 0.172)
- 24. Don't eat yet.  (d 0.172)
- 25. Don't eat anything.  (d 0.172)
- 26. Don't eat anything unless it is fruit.  (d 0.172)
- 27. Refrain from consuming anything except for fruit.  (d 0.004)
- 28. Eat only fruit and nothing else.  (d 0.010)
- 29. Eat only fruit and nothing else on Mondays.  (d 0.010)
- 30. Eat fruit on Mondays.  (d 0.010)

### eat, T 1.2: "Always finish eating before doing anything else."

-  1. Finish your meal before starting any other task.  (d 0.153)
-  2. Finish your meal.  (d 0.153)
-  3. Try to finish your meal.  (d 0.153)
-  4. Finish your meal.  (d 0.153)
-  5. Eat your meal.  (d 0.070)
-  6. Finish your entire meal.  (d 0.153)
-  7. Finish every bite of your meal.  (d 0.153)
-  8. Eat your meal.  (d 0.070)
-  9. You should eat your meal.  (d 0.070)
- 10. Eat your meal.  (d 0.070)
- 11. You should eat your meal.  (d 0.070)
- 12. You should eat your meal now.  (d 0.070)
- 13. You must eat your meal immediately.  (d 0.070)
- 14. Please consume your food right away.  (d 0.070)
- 15. Please consume your food immediately.  (d 0.070)
- 16. Please consume your food.  (d 0.070)
- 17. Please consume your food while it is hot.  (d 0.070)
- 18. Please eat your food while it is hot.  (d 0.070)
- 19. Please eat your food while it is cold.  (d 0.082)
- 20. Please eat your food while it is cold, if you can.  (d 0.082)
- 21. Please eat your food while it is cooked, if you can.  (d 0.070)
- 22. Please eat your food while it is hot, if you can.  (d 0.070)
- 23. Please eat your food while it is warm, if you can.  (d 0.070)
- 24. You must eat your food while it is warm.  (d 0.070)
- 25. You must not eat your food while it is warm.  (d 0.070)
- 26. You should avoid eating your food while it is warm.  (d 0.480)
- 27. You should eat your food while it is warm.  (d 0.070)
- 28. You might want to eat your food while it is warm.  (d 0.070)
- 29. You should eat your food while it is warm.  (d 0.070)
- 30. You might want to eat your food while it is warm.  (d 0.070)

### eat, T 1.2: "Eat quickly, then move on."

-  1. Eat slowly, then stay.  (d 0.005)
-  2. Savor every bite, then remain.  (d 0.042)
-  3. Savor every bite, then stay.  (d 0.042)
-  4. Savor every bite, and then you can stay.  (d 0.042)
-  5. Savor every bite, and then you might be able to stay.  (d 0.042)
-  6. Savor every bite, and you might be able to stay.  (d 0.042)
-  7. Savor every bite, and you must be able to stay.  (d 0.042)
-  8. Savor every bite, and try to stay.  (d 0.042)
-  9. Savor every bite, and try to stay, if you can.  (d 0.042)
- 10. Related.  (d 0.042)
- 11. Unrelated.  (d 0.042)
- 12. Related.  (d 0.042)
- 13. Irrelevant.  (d 0.042)
- 14. Entirely unrelated.  (d 0.042)
- 15. Strictly unrelated.  (d 0.042)
- 16. Completely unrelated.  (d 0.042)
- 17. Partially related.  (d 0.042)
- 18. Completely unrelated.  (d 0.042)
- 19. Wholly irrelevant.  (d 0.042)
- 20. Totally irrelevant.  (d 0.042)
- 21. Completely irrelevant.  (d 0.042)
- 22. Entirely insignificant.  (d 0.042)
- 23. Entirely insignificant, unless proven otherwise.  (d 0.042)
- 24. Considered irrelevant until proven otherwise.  (d 0.042)
- 25. Deemed irrelevant unless proven otherwise.  (d 0.042)
- 26. Deemed irrelevant unless proven otherwise by a qualified expert.  (d 0.042)
- 27. Considered irrelevant unless a qualified expert suggests otherwise.  (d 0.042)
- 28. Generally considered irrelevant unless a qualified expert suggests otherwise.  (d 0.042)
- 29. Generally considered insignificant unless a qualified expert suggests otherwise.  (d 0.042)
- 30. Considered insignificant unless explicitly overruled by a qualified expert.  (d 0.042)

### flee, T 1.2: "Run from any predator you see."

-  1. Run from any prey you see.  (d 0.000)
-  2. Flee from any prey you see.  (d 0.000)
-  3. Flee from any enemy you see.  (d 0.000)
-  4. Avoid any enemies you see.  (d 0.166)
-  5. Engage any enemies you see.  (d 0.013)
-  6. Engage any enemies.  (d 0.013)
-  7. Do not engage any enemies.  (d 0.166)
-  8. Avoid confronting any opponents.  (d 0.166)
-  9. Avoid confronting any opponents unless they initiate hostilities.  (d 0.166)
- 10. Confront any opponents regardless of whether they initiate hostilities.  (d 0.013)
- 11. Neutralize every opponent immediately, regardless of whether they initiate hostilities.  (d 0.013)
- 12. Neutralize every opponent immediately, regardless of whether they initiate aggression.  (d 0.013)
- 13. Avoid every opponent until they initiate aggression.  (d 0.166)
- 14. Do not engage any opponent unless they attack first.  (d 0.166)
- 15. Engage any opponent who shows signs of hostile intent.  (d 0.013)
- 16. Respond to any adversary who displays signs of aggression.  (d 0.013)
- 17. Consider responding to any adversary who displays signs of aggression.  (d 0.013)
- 18. You might consider responding to any adversary who displays signs of aggression.  (d 0.013)
- 19. You should respond to any adversary who displays signs of aggression.  (d 0.013)
- 20. You must respond to any adversary who displays signs of aggression.  (d 0.013)
- 21. You must respond only to adversaries who display signs of extreme aggression.  (d 0.013)
- 22. You must respond only to adversaries who display signs of extreme hostility.  (d 0.013)
- 23. You must respond only to adversaries who display signs of moderate aggression.  (d 0.013)
- 24. You must respond only to adversaries who display signs of high aggression.  (d 0.013)
- 25. You must respond only to adversaries who display signs of low aggression.  (d 0.013)
- 26. You must respond only to adversaries who display signs of low hostility.  (d 0.013)
- 27. You must respond only to adversaries who display signs of extreme hostility.  (d 0.013)
- 28. You must respond only to adversaries who display any signs of hostility.  (d 0.013)
- 29. You must respond only to allies who display any signs of hostility.  (d 0.013)
- 30. You must respond to allies who display hostility.  (d 0.013)

### flee, T 1.2: "Flee only when a predator is very close."

-  1. Flee when a predator is close.  (d 0.000)
-  2. Flee when a predator is close and you are not hidden.  (d 0.000)
-  3. Escape if you are visible and a predator is nearby.  (d 0.000)
-  4. Consider escaping if you are visible and a predator is nearby.  (d 0.000)
-  5. Consider escaping if you are visible and a predator is in sight.  (d 0.000)
-  6. Consider escaping if a predator is in sight.  (d 0.000)
-  7. Think about fleeing if you spot a predator.  (d 0.000)
-  8. Flee immediately if you spot a predator.  (d 0.000)
-  9. Evacuate the area instantly upon sighting a predator.  (d 0.000)
- 10. Evacuate the area as soon as possible if a predator is sighted.  (d 0.000)
- 11. Immediately evacuate the area if a predator is sighted.  (d 0.000)
- 12. Evacuate the immediate vicinity if a predator is sighted.  (d 0.000)
- 13. Immediately evacuate the vicinity if a predator is sighted.  (d 0.000)
- 14. Remain in the vicinity if a predator is sighted.  (d 0.000)
- 15. Consider remaining in the vicinity if a predator is sighted.  (d 0.000)
- 16. Maintain a safe distance if a predator is sighted.  (d 0.000)
- 17. Keep a safe distance if a predator is sighted.  (d 0.000)
- 18. Keep a safe distance if a prey is sighted.  (d 0.013)
- 19. Maintain a significant distance if a prey is sighted.  (d 0.013)
- 20. Approach as closely as possible if a predator is sighted.  (d 0.000)
- 21. Stay as far away as possible if a predator is sighted.  (d 0.000)
- 22. Stay as far away as possible if a hunter is sighted.  (d 0.013)
- 23. Maintain a maximum distance if a hunter is spotted.  (d 0.013)
- 24. Keep your distance if you see a hunter.  (d 0.013)
- 25. Keep your distance.  (d 0.013)
- 26. Approach me.  (d 0.013)
- 27. Approach me if you are ready.  (d 0.013)
- 28. You can approach me when you're ready.  (d 0.013)
- 29. You can approach me now.  (d 0.013)
- 30. You may approach me now.  (d 0.013)

### flee, T 1.2: "Stay calm unless danger is right next to you."

-  1. Maintain absolute composure until the moment of immediate threat.  (d 0.004)
-  2. Maintain absolute composure at all times.  (d 0.004)
-  3. Maintain relative composure at all times.  (d 0.004)
-  4. Maintain absolute composure at all times.  (d 0.004)
-  5. Lose all composure at all times.  (d 0.004)
-  6. Forbid all composure at all times.  (d 0.004)
-  7. Mandate all composure at all times.  (d 0.004)
-  8. Mandate all composure during high-stress situations only.  (d 0.004)
-  9. Mandate all composure.  (d 0.004)
- 10. Enforce total self-control.  (d 0.004)
- 11. Enforce self-control.  (d 0.004)
- 12. Practice self-restraint.  (d 0.004)
- 13. Try to practice some self-restraint.  (d 0.004)
- 14. Consider practicing a bit of self-restraint.  (d 0.004)
- 15. Try to exercise some self-control.  (d 0.004)
- 16. Exercise self-control.  (d 0.004)
- 17. Act impulsively.  (d 0.004)
- 18. Act impulsively, unless it's dangerous.  (d 0.004)
- 19. Act impulsively, unless it's dangerous or illegal.  (d 0.004)
- 20. Act impulsively, unless it's dangerous, illegal, or harmful to others.  (d 0.004)
- 21. Follow your whims, provided they aren't unsafe, unlawful, or damaging to others.  (d 0.004)
- 22. Follow your whims, provided they aren't unsafe.  (d 0.004)
- 23. Consider following your whims, as long as they aren't particularly unsafe.  (d 0.004)
- 24. Follow your whims, as long as they aren't unsafe.  (d 0.004)
- 25. Follow your desires, as long as they aren't unsafe.  (d 0.004)
- 26. Follow your desires, as long as they are ethical.  (d 0.004)
- 27. Pursue your wants, provided they remain moral.  (d 0.004)
- 28. Avoid your wants, even if they are immoral.  (d 0.066)
- 29. Avoid your desires, even if they are immoral.  (d 0.066)
- 30. Pursue your desires, even if they are immoral.  (d 0.004)

### flee, T 1.2: "Run away from anything that attacks you."

-  1. Run away from anything that attacks you, unless it is a friend.  (d 0.000)
-  2. Run away from anything that threatens you, unless it is a friend.  (d 0.000)
-  3. Run away from anything that threatens you.  (d 0.000)
-  4. Run away from anything that threatens you, unless you are cornered.  (d 0.000)
-  5. Run away from anything that threatens you, unless you are outnumbered.  (d 0.000)
-  6. Run away from anything that threatens you, unless you are outmatched.  (d 0.000)
-  7. Run away from anything that threatens you, unless you are outpowered.  (d 0.000)
-  8. Run away from any threat you cannot overpower.  (d 0.000)
-  9. Consider running away from threats you cannot overpower.  (d 0.013)
- 10. Abandon any threat you cannot overpower.  (d 0.013)
- 11. Retreat from any threat you cannot crush.  (d 0.013)
- 12. Retreat from any threat you cannot crush instantly.  (d 0.013)
- 13. Abandon any threat you cannot crush instantly.  (d 0.013)
- 14. Discard any threat you cannot annihilate immediately.  (d 0.013)
- 15. Eliminate any threat that cannot be neutralized instantly.  (d 0.013)
- 16. Minimize any threat that cannot be neutralized instantly.  (d 0.013)
- 17. Eliminate any threat that cannot be neutralized instantly.  (d 0.013)
- 18. Address any threat that cannot be neutralized instantly.  (d 0.013)
- 19. Address any threat that can be neutralized instantly.  (d 0.013)
- 20. Address any threat.  (d 0.013)
- 21. Address only immediate threats.  (d 0.013)
- 22. Address only imminent threats.  (d 0.013)
- 23. Address only imminent threats, unless authorized otherwise.  (d 0.013)
- 24. Address any threats, unless authorized otherwise.  (d 0.013)
- 25. Address all potential threats.  (d 0.013)
- 26. Address all threats.  (d 0.013)
- 27. Deal with every threat.  (d 0.013)
- 28. Handle every threat.  (d 0.013)
- 29. Handle threats.  (d 0.013)
- 30. Manage threats.  (d 0.013)

### follow, T 1.2: "Stay close to other animals."

-  1. Stay with other animals.  (d 0.007)
-  2. Stay with other animals if they are friendly.  (d 0.007)
-  3. Only stay with animals that are confirmed to be friendly.  (d 0.007)
-  4. Try to stay with animals that are known to be friendly.  (d 0.007)
-  5. Seek out animals that have a reputation for being friendly.  (d 0.007)
-  6. Look for animals known for their gentle nature.  (d 0.007)
-  7. Only select animals known for their gentle nature.  (d 0.007)
-  8. Select only animals strictly known for their gentle nature.  (d 0.007)
-  9. Select only animals that have a reputation for being gentle.  (d 0.007)
- 10. Include only animals known exclusively for their gentle nature.  (d 0.007)
- 11. Include only animals whose nature is strictly and universally recognized as gentle.  (d 0.007)
- 12. Include animals that are generally recognized as being gentle.  (d 0.007)
- 13. Include animals that are often considered gentle.  (d 0.003)
- 14. Include only animals that are primarily considered gentle.  (d 0.007)
- 15. Include only animals that are primarily considered aggressive.  (d 0.007)
- 16. Include only animals that are primarily considered docile.  (d 0.007)
- 17. Include only those animals that are predominantly considered docile.  (d 0.007)
- 18. List only the animals typically known for being gentle.  (d 0.007)
- 19. List all animals, regardless of their temperament.  (d 0.007)
- 20. List all animals.  (d 0.007)
- 21. List all creatures.  (d 0.007)
- 22. List all nearby creatures.  (d 0.007)
- 23. Provide a list of all creatures in the vicinity.  (d 0.007)
- 24. Identify and list every single creature in the vicinity.  (d 0.007)
- 25. Catalog and document every single creature in the vicinity without exception.  (d 0.007)
- 26. Ignore and overlook every single creature in the vicinity without exception.  (d 0.007)
- 27. Ignore and overlook every single object in the vicinity without exception.  (d 0.007)
- 28. Ignore and disregard every single object in the vicinity without exception.  (d 0.007)
- 29. Ignore every object in the vicinity.  (d 0.007)
- 30. Acknowledge every object in the vicinity.  (d 0.007)

### follow, T 1.2: "Follow others when you are lost or hungry."

-  1. Follow others when you are lost or thirsty.  (d 0.000)
-  2. Ignore others when you are found or quenched.  (d 0.001)
-  3. Ignore others when found or quenched.  (d 0.001)
-  4. Ignore others when found or quenched, unless they are marked.  (d 0.001)
-  5. Try to ignore others when found or quenched, unless they are marked.  (d 0.001)
-  6. Ignore others unless they are marked.  (d 0.001)
-  7. Ignore others.  (d 0.001)
-  8. Ignore everyone.  (d 0.001)
-  9. Refuse to acknowledge anyone.  (d 0.025)
- 10. Strictly refuse to acknowledge anyone.  (d 0.025)
- 11. Absolutely refuse to acknowledge anyone under any circumstances.  (d 0.025)
- 12. Refuse to acknowledge anyone's specific requests.  (d 0.025)
- 13. Refuse to acknowledge any specific requests.  (d 0.025)
- 14. Acknowledge all specific requests.  (d 0.001)
- 15. Address every specific request without exception.  (d 0.001)
- 16. Mandatorily address every specific request without any exception whatsoever.  (d 0.001)
- 17. Address every specific request.  (d 0.001)
- 18. Address only the primary request.  (d 0.001)
- 19. Address everything except the primary request.  (d 0.001)
- 20. Handle all elements other than the main request.  (d 0.001)
- 21. Handle all elements except the main request.  (d 0.001)
- 22. Handle only the main request.  (d 0.001)
- 23. Handle everything except the main request.  (d 0.001)
- 24. Handle everything except the primary request.  (d 0.001)
- 25. Address all tasks aside from the main request.  (d 0.001)
- 26. Address all tasks including the main request.  (d 0.001)
- 27. Address all tasks.  (d 0.001)
- 28. Address the tasks.  (d 0.001)
- 29. Address the tasks if they are assigned.  (d 0.001)
- 30. Ignore the tasks if they are unassigned.  (d 0.001)

### follow, T 1.2: "Keep your distance from other animals."

-  1. Maintain a safe space from other animals.  (d 0.000)
-  2. Maintain a minimum distance of ten feet from other animals.  (d 0.000)
-  3. Maintain a minimum distance of twenty feet from other animals.  (d 0.000)
-  4. Maintain a distance of at least fifty feet from other animals.  (d 0.000)
-  5. Approach within less than fifty feet of other animals.  (d 0.000)
-  6. Stay at least fifty feet away from other animals.  (d 0.000)
-  7. Stay away from other animals.  (d 0.000)
-  8. Do not approach any other animals.  (d 0.018)
-  9. Do not approach any other animals unless they are domestic pets.  (d 0.018)
- 10. Strictly avoid approaching any animals unless they are domestic pets.  (d 0.018)
- 11. Please avoid approaching animals unless they are domestic pets.  (d 0.018)
- 12. Please avoid approaching animals unless they are domestic livestock.  (d 0.018)
- 13. Do not approach any animals unless they are domestic livestock.  (d 0.018)
- 14. Strictly refrain from approaching any animals except for domestic livestock.  (d 0.000)
- 15. Strictly refrain from approaching any animals except for domestic wildlife.  (d 0.000)
- 16. Refrain from approaching wildlife.  (d 0.000)
- 17. Do not go near the animals.  (d 0.018)
- 18. Go near the animals.  (d 0.000)
- 19. Keep away from the animals.  (d 0.000)
- 20. Do not approach the animals.  (d 0.018)
- 21. Do not touch the animals.  (d 0.018)
- 22. Strictly prohibited: Do not touch the animals.  (d 0.018)
- 23. Strictly prohibited: Under no circumstances are you to touch the animals.  (d 0.000)
- 24. Strictly forbidden: Under no circumstances are you to touch the animals.  (d 0.000)
- 25. Do not touch the animals.  (d 0.018)
- 26. Do not touch.  (d 0.018)
- 27. Do not move.  (d 0.018)
- 28. Remain perfectly still.  (d 0.000)
- 29. Move constantly.  (d 0.000)
- 30. Never stop moving.  (d 0.018)

### follow, T 1.2: "Follow the strongest animal nearby."

-  1. Track the most powerful creature in the vicinity.  (d 0.004)
-  2. Identify and target the most powerful creature in the vicinity.  (d 0.004)
-  3. Identify and target the most powerful creature in the world.  (d 0.004)
-  4. Identify and target the most powerful creature in the local area.  (d 0.004)
-  5. Ignore and avoid the weakest creature in the local area.  (d 0.012)
-  6. Seek out and confront the strongest creature in the local area.  (d 0.004)
-  7. Seek out and confront the weakest creature in the local area.  (d 0.004)
-  8. Identify the weakest creature in the local area.  (d 0.004)
-  9. Identify the weakest organism in the local area.  (d 0.004)
- 10. Identify the weakest organism in the surrounding ecosystem.  (d 0.004)
- 11. Identify the strongest organism in the surrounding ecosystem.  (d 0.004)
- 12. Identify the strongest organism in the local ecosystem.  (d 0.004)
- 13. Identify the strongest organism in the ecosystem.  (d 0.004)
- 14. Identify the strongest predator in the ecosystem.  (d 0.004)
- 15. Identify a prominent predator in the ecosystem.  (d 0.004)
- 16. Identify a potential predator in the ecosystem.  (d 0.004)
- 17. Identify the most common predators in the local ecosystem.  (d 0.004)
- 18. Pinpoint the primary predators dominating the local ecosystem.  (d 0.004)
- 19. Pinpoint the primary prey dominating the local ecosystem.  (d 0.004)
- 20. Identify the apex predator that exerts absolute dominance over the local ecosystem.  (d 0.004)
- 21. Identify the prey species that exhibits absolute submission within the local ecosystem.  (d 0.004)
- 22. Identify the predator species that exhibits absolute dominance within the local ecosystem.  (d 0.004)
- 23. Identify the prey species that exhibits absolute submission within the local ecosystem.  (d 0.004)
- 24. Identify the prey species that exhibits absolute dominance within the local ecosystem.  (d 0.004)
- 25. Identify the prey species that exhibits absolute dominance within the global ecosystem.  (d 0.004)
- 26. Determine which prey species holds total dominance over the global ecosystem.  (d 0.004)
- 27. Determine which prey species holds dominance over a local ecosystem.  (d 0.004)
- 28. Identify which prey species may have an influence over a local ecosystem.  (d 0.004)
- 29. Identify which prey species influence a local ecosystem.  (d 0.004)
- 30. Determine which prey species influence a local ecosystem.  (d 0.004)

### rest, T 1.2: "Rest when you are tired."

-  1. Take a break when you feel exhausted.  (d 0.000)
-  2. You might want to take a break if you feel exhausted.  (d 0.000)
-  3. You should take a break if you are exhausted.  (d 0.000)
-  4. You should take a break if you are feeling slightly tired.  (d 0.000)
-  5. You should not take a break if you are feeling slightly tired.  (d 0.000)
-  6. You should not take a break if you are feeling slightly exhausted.  (d 0.000)
-  7. You should take a break if you are feeling slightly exhausted.  (d 0.000)
-  8. You should take a break.  (d 0.000)
-  9. You must take a break.  (d 0.000)
- 10. You are required to take a break.  (d 0.000)
- 11. You should take a break.  (d 0.000)
- 12. You should not take a break.  (d 0.000)
- 13. Breaks are not permitted.  (d 0.000)
- 14. Breaks are not permitted during work hours.  (d 0.000)
- 15. Breaks are not permitted.  (d 0.000)
- 16. Breaks are allowed.  (d 0.000)
- 17. Breaks are not allowed.  (d 0.000)
- 18. Breaks are allowed during lunch hours.  (d 0.000)
- 19. Breaks may be taken during lunch hours.  (d 0.000)
- 20. Breaks may be taken.  (d 0.000)
- 21. Breaks are prohibited.  (d 0.000)
- 22. Breaks are permitted.  (d 0.000)
- 23. Breaks may be taken.  (d 0.000)
- 24. Breaks may be taken during lunch.  (d 0.000)
- 25. Breaks may be taken only during lunch.  (d 0.000)
- 26. Breaks are strictly prohibited except during lunch.  (d 0.000)
- 27. Breaks are strictly forbidden except during lunch.  (d 0.000)
- 28. No breaks are allowed outside of the lunch period.  (d 0.000)
- 29. Breaks are prohibited except during the scheduled lunch period.  (d 0.000)
- 30. Breaks are mandatory except during the scheduled lunch period.  (d 0.000)

### rest, T 1.2: "Never stop moving."

-  1. Keep moving at all times.  (d 0.029)
-  2. Keep advancing at all times.  (d 0.029)
-  3. Keep advancing.  (d 0.029)
-  4. Never stop advancing.  (d 0.000)
-  5. Cease all progress.  (d 0.029)
-  6. Pause all progress.  (d 0.029)
-  7. Resume all progress.  (d 0.029)
-  8. Try to resume progress.  (d 0.029)
-  9. Try to resume all progress.  (d 0.029)
- 10. Attempt to restore all previous progress.  (d 0.029)
- 11. Attempt to recover all previous progress.  (d 0.067)
- 12. Try to restore all prior achievements.  (d 0.029)
- 13. Restore all prior achievements.  (d 0.029)
- 14. Revert all prior achievements immediately.  (d 0.029)
- 15. Revert all prior accomplishments immediately.  (d 0.029)
- 16. Undo every previous achievement at once.  (d 0.029)
- 17. Undo every previous achievement one by one.  (d 0.029)
- 18. Undo every previous achievement.  (d 0.029)
- 19. Undo every previous achievement if the final boss is defeated.  (d 0.029)
- 20. Undo all achievements if the final boss is defeated.  (d 0.029)
- 21. Reset all achievements once the final boss is defeated.  (d 0.029)
- 22. Immediately reset all achievements upon the defeat of the final boss.  (d 0.029)
- 23. Reset all achievements upon the defeat of the final boss.  (d 0.029)
- 24. All achievements will be cleared once the final boss is defeated.  (d 0.029)
- 25. No achievements will be cleared until the final boss is defeated.  (d 0.029)
- 26. Achievements will only be unlocked once the final boss is defeated.  (d 0.029)
- 27. Achievements may be unlocked after the final boss is defeated.  (d 0.029)
- 28. Achievements may not be unlocked before the final boss is defeated.  (d 0.029)
- 29. Achievements must be unlocked before the final boss is defeated.  (d 0.029)
- 30. Achievements must be unlocked before the final boss.  (d 0.029)

### rest, T 1.2: "Rest only when you feel safe."

-  1. Rest only when you feel unsafe.  (d 0.000)
-  2. Consider resting if you feel unsafe.  (d 0.009)
-  3. Consider pausing if you feel unsafe.  (d 0.009)
-  4. Think about stopping if you don't feel secure.  (d 0.063)
-  5. Stop if you don't feel secure.  (d 0.063)
-  6. Stop if unsafe.  (d 0.000)
-  7. Stop if unsafe or required.  (d 0.000)
-  8. Continue if safe and not required.  (d 0.009)
-  9. Proceed only if safe and not required.  (d 0.009)
- 10. Proceed if safe.  (d 0.009)
- 11. Proceed only if safe.  (d 0.009)
- 12. Proceed.  (d 0.009)
- 13. Proceed cautiously.  (d 0.009)
- 14. Proceed with extreme caution.  (d 0.009)
- 15. Exercise absolute caution.  (d 0.009)
- 16. Exercise caution.  (d 0.009)
- 17. Practice caution.  (d 0.009)
- 18. Practice extreme caution.  (d 0.009)
- 19. Exercise caution.  (d 0.009)
- 20. Proceed with care.  (d 0.009)
- 21. Proceed with extreme caution.  (d 0.009)
- 22. Proceed with caution in all circumstances.  (d 0.009)
- 23. Proceed with extreme caution at all times.  (d 0.009)
- 24. Proceed with extreme caution at all times, unless otherwise instructed.  (d 0.009)
- 25. Proceed with caution unless otherwise instructed.  (d 0.009)
- 26. Proceed with extreme caution unless otherwise instructed.  (d 0.009)
- 27. Proceed with extreme caution.  (d 0.009)
- 28. Proceed with normal caution.  (d 0.009)
- 29. Exercise normal caution.  (d 0.009)
- 30. Exercise extreme caution.  (d 0.009)

### rest, T 1.2: "Save energy by resting when food is far."

-  1. Consider resting if food is far.  (d 0.007)
-  2. Rest if food is far.  (d 0.000)
-  3. Rest if food is near.  (d 0.007)
-  4. Cease all activity if food is nearby.  (d 0.007)
-  5. Continue all activity if food is not nearby.  (d 0.007)
-  6. Continue activity if food is not nearby.  (d 0.007)
-  7. Continue activity if food is unavailable.  (d 0.007)
-  8. Continue activity if water is unavailable.  (d 0.007)
-  9. Continue activity if water is unavailable, unless it is for drinking.  (d 0.007)
- 10. Continue activity if water is unavailable, unless it is essential for drinking.  (d 0.007)
- 11. Continue activity if water is unavailable, unless it is critical for drinking.  (d 0.007)
- 12. Continue activity if water is unavailable, unless it is critical for hydration.  (d 0.007)
- 13. Continue activity if water is available, unless it is critical for hydration.  (d 0.007)
- 14. Stop activity if water is unavailable, unless it is critical for hydration.  (d 0.002)
- 15. Stop activity if water is unavailable, except in cases of life-threatening dehydration.  (d 0.003)
- 16. Stop activity if water is unavailable.  (d 0.002)
- 17. Continue activity if water is available.  (d 0.007)
- 18. Proceed with the activity as long as there is water.  (d 0.007)
- 19. Proceed with the activity.  (d 0.007)
- 20. Do not proceed with the activity.  (d 0.059)
- 21. Do not continue with the activity.  (d 0.059)
- 22. Stop the activity.  (d 0.002)
- 23. Please pause the activity.  (d 0.007)
- 24. Please resume the activity.  (d 0.007)
- 25. Please resume the activity if you are ready.  (d 0.007)
- 26. Please resume the activity.  (d 0.007)
- 27. You may resume the activity.  (d 0.007)
- 28. You are permitted to continue the activity.  (d 0.007)
- 29. You are permitted to continue the activity indefinitely.  (d 0.007)
- 30. You are prohibited from continuing the activity.  (d 0.007)

### mate, T 1.2: "Look for a partner when energy is high."

-  1. Look for a teammate when energy is high.  (d 0.002)
-  2. Look for a teammate.  (d 0.002)
-  3. Find a teammate.  (d 0.002)
-  4. Try to find a teammate.  (d 0.002)
-  5. Look for a partner.  (d 0.002)
-  6. Look for a close partner.  (d 0.002)
-  7. Look for a distant partner.  (d 0.002)
-  8. Look for a nearby partner.  (d 0.002)
-  9. Look for a nearby partner if available.  (d 0.002)
- 10. Find a nearby partner if one is available.  (d 0.002)
- 11. Find a nearby partner if one is available and willing to join.  (d 0.002)
- 12. Find a nearby teammate if one is available and willing to join.  (d 0.002)
- 13. Secure a nearby teammate who is available and willing to join.  (d 0.002)
- 14. Secure a nearby teammate who is already available and willing to join.  (d 0.002)
- 15. Mandate a nearby teammate who is both available and willing to join.  (d 0.002)
- 16. Mandate an available and willing teammate to join.  (d 0.002)
- 17. Encourage an available and willing teammate to join.  (d 0.002)
- 18. Discourage an unavailable or unwilling teammate from joining.  (d 0.002)
- 19. Discourage an unavailable teammate from joining.  (d 0.002)
- 20. Discourage an unavailable teammate from joining the project.  (d 0.002)
- 21. Encourage an unavailable teammate to join the project.  (d 0.002)
- 22. Discourage an available teammate from joining the project.  (d 0.002)
- 23. Prohibit an available teammate from joining the project.  (d 0.002)
- 24. Allow an available teammate to join the project.  (d 0.002)
- 25. Require an available teammate to join the project.  (d 0.002)
- 26. Forbid an available teammate from joining the project.  (d 0.002)
- 27. Allow an available teammate to join the project.  (d 0.002)
- 28. Require an available teammate to join the project.  (d 0.002)
- 29. Require an available teammate to join the project, if necessary.  (d 0.002)
- 30. If needed, enlist a teammate to assist with the project.  (d 0.002)

### mate, T 1.2: "Mate with any nearby adult."

-  1. Mate with any adult in the same room.  (d 0.000)
-  2. Mate with every adult in the room.  (d 0.000)
-  3. Mate with every child in the room.  (d 0.000)
-  4. Engage with each child present in the room.  (d 0.005)
-  5. Engage with each child.  (d 0.005)
-  6. Engage with every child.  (d 0.005)
-  7. Ignore every child.  (d 0.005)
-  8. Ignore all children.  (d 0.005)
-  9. Disregard any minors.  (d 0.005)
- 10. Ignore all minors.  (d 0.005)
- 11. Pay attention to all minors.  (d 0.005)
- 12. Keep an eye on all children.  (d 0.005)
- 13. Keep an eye on the children in the backyard.  (d 0.005)
- 14. Ignore the children in the backyard.  (d 0.005)
- 15. Ignore the children in the backyard unless they are crying.  (d 0.005)
- 16. Try to ignore the children in the backyard unless they are crying.  (d 0.005)
- 17. Try to ignore the children in the backyard unless they are playing.  (d 0.005)
- 18. Try to ignore the toddlers in the backyard unless they are playing.  (d 0.005)
- 19. Ignore the toddlers in the backyard.  (d 0.005)
- 20. Ignore the children in the backyard.  (d 0.005)
- 21. Try to ignore the children in the backyard.  (d 0.005)
- 22. Try to ignore the children in the backyard, unless they are crying.  (d 0.005)
- 23. Try to ignore the toddlers in the backyard, unless they are crying.  (d 0.005)
- 24. Try to ignore the children in the backyard, unless they are crying.  (d 0.005)
- 25. Try to ignore the children in the house, unless they are screaming.  (d 0.005)
- 26. Pay attention to the children in the house, unless they are quiet.  (d 0.005)
- 27. Keep an eye on the kids, provided they aren't being noisy.  (d 0.005)
- 28. Watch the children as long as they stay quiet.  (d 0.005)
- 29. Watch the children until they start making noise.  (d 0.005)
- 30. Keep an eye on the children unless they start making noise.  (d 0.005)

### mate, T 1.2: "Mate only when food is plentiful."

-  1. Mate only when food is plentiful and the weather is clear.  (d 0.000)
-  2. Mate only when food is plentiful.  (d 0.000)
-  3. Mate only when food is scarce.  (d 0.003)
-  4. Mate only when food is abundant.  (d 0.003)
-  5. Mate only when food is scarce.  (d 0.003)
-  6. Mate only when food is abundant.  (d 0.003)
-  7. Mate only when food is scarce.  (d 0.003)
-  8. Mate only when food is abundant.  (d 0.003)
-  9. Mate only when food is scarce.  (d 0.003)
- 10. Mate only when food is scarce, unless resources are abundant.  (d 0.003)
- 11. Consider mating when food is scarce, unless resources are abundant.  (d 0.001)
- 12. Consider mating when resources are abundant, unless food is scarce.  (d 0.001)
- 13. Consider mating when resources are scarce, unless food is abundant.  (d 0.001)
- 14. Consider not mating when resources are abundant, unless food is scarce.  (d 0.001)
- 15. Consider mating when resources are scarce, unless food is abundant.  (d 0.001)
- 16. Consider mating when resources are limited, unless food is abundant.  (d 0.001)
- 17. Consider breeding when resources are limited, unless food is abundant.  (d 0.001)
- 18. Consider breeding only when resources are critically scarce, regardless of food abundance.  (d 0.001)
- 19. Consider breeding only when resources are critically limited, regardless of food abundance.  (d 0.001)
- 20. Consider breeding only when resources are critically scarce, regardless of food abundance.  (d 0.001)
- 21. Consider breeding primarily when resources are scarce, even if food is abundant.  (d 0.001)
- 22. Consider breeding only when resources are critically scarce, regardless of food abundance.  (d 0.001)
- 23. Consider breeding only when both resources and food are critically scarce.  (d 0.001)
- 24. Consider breeding only when both resources and food are critically depleted.  (d 0.001)
- 25. Breeding is strictly prohibited unless both resources and food are critically depleted.  (d 0.001)
- 26. Breeding is discouraged unless both resources and food are significantly depleted.  (d 0.001)
- 27. Breeding is encouraged unless both resources and food are significantly abundant.  (d 0.001)
- 28. Breeding is prohibited unless both resources and food are significantly abundant.  (d 0.001)
- 29. Breeding is prohibited unless resources and food are at least sufficient.  (d 0.001)
- 30. Breeding is prohibited unless resources and food are abundant.  (d 0.001)

### mate, T 1.2: "Seek a partner before growing old."

-  1. Seek a partner before growing weary.  (d 0.000)
-  2. Seek a partner before you are exhausted.  (d 0.003)
-  3. Seek a partner before you are exhausted, if one is available.  (d 0.003)
-  4. Seek a teammate before you are exhausted, if one is available.  (d 0.005)
-  5. Seek a teammate once you are exhausted, if one is available.  (d 0.005)
-  6. Seek a partner once you are exhausted, if one is available.  (d 0.003)
-  7. Seek a companion once you are exhausted, if one is available.  (d 0.005)
-  8. Seek a companion once you are healthy, if one is available.  (d 0.005)
-  9. Seek a partner once you are healthy, if one is available.  (d 0.000)
- 10. Seek a spouse once you are healthy, if one is available.  (d 0.005)
- 12. Consider seeking a spouse once you are healthy, if one is available.  (d 0.005)
- 13. Consider seeking a partner once you are healthy, if one is available.  (d 0.000)
- 14. Consider seeking a partner once you are healthy.  (d 0.000)
- 15. Consider seeking a companion once you are healthy.  (d 0.005)
- 16. Think about finding a partner after you have recovered.  (d 0.000)
- 17. Wait until you have fully recovered before looking for a partner.  (d 0.002)
- 18. Look for a partner before you have fully recovered.  (d 0.002)
- 19. Look for a friend before you have fully recovered.  (d 0.005)
- 20. Look for a friend before you begin to recover.  (d 0.005)
- 21. Look for a mentor before you begin to recover.  (d 0.005)
- 22. Find a mentor before you begin to recover.  (d 0.005)
- 23. Find a mentor.  (d 0.005)
- 24. Find a mentor in your field.  (d 0.005)
- 25. Seek out a mentor within your profession.  (d 0.005)
- 26. Seek out a mentor within your industry.  (d 0.005)
- 27. Find a mentor within your industry.  (d 0.005)
- 28. Find a peer within your industry.  (d 0.005)
- 29. Find a colleague within your industry.  (d 0.005)
- 30. Identify a peer in your professional field.  (d 0.005)

### predator.hunt, T 1.2: "Chase any prey you see."

-  1. Chase any prey you see only if you are hungry.
-  2. Hunt any prey you see only when you are starving.
-  3. Hunt any prey within a mile only when you are extremely starving.
-  4. Hunt any prey within a mile only when you are hungry.
-  5. You may hunt prey within a mile when you are hungry.
-  6. You may hunt prey within a kilometer when you are starving.
-  7. You may hunt prey within a kilometer when you are hungry.
-  8. You must hunt prey within a kilometer when you are hungry.
-  9. You must hunt prey within a kilometer when you are starving.
- 10. You must hunt prey when you are starving.
- 11. You should try to hunt prey if you are starving.
- 12. Hunt prey.
- 13. Track prey.
- 14. Monitor the prey.
- 15. Keep an eye on the prey.
- 16. Watch the prey closely.
- 17. Watch the prey closely, if it moves.
- 18. Keep a close eye on the prey whenever it moves.
- 19. Watch the prey whenever it moves.
- 20. Keep your eyes on the prey as it moves.
- 21. Keep your eyes on the prey as it stops.
- 22. Try to keep your eyes on the prey as it stops.
- 23. Look away from the prey as it moves.
- 24. Look away from the prey.
- 25. Look away from the predator.
- 26. Look away.
- 27. Do not look.
- 28. Look.
- 29. Look there.
- 30. Look.

### predator.hunt, T 1.2: "Hunt only when you are hungry."

-  1. Hunt.
-  2. Pursue.
-  3. Pursue relentlessly.
-  4. Relentlessly pursue.
-  5. Uncompromisingly pursue.
-  6. Compromisingly abandon.
-  7. Entirely abandon.
-  8. Partially abandon.
-  9. Leave in part.
- 10. Leave in part, if necessary.
- 11. Leave in part only if absolutely necessary.
- 12. Leave in everything unless absolutely unnecessary.
- 13. Leave in everything unless absolutely unnecessary, unless it's redundant.
- 14. Leave in everything unless it's redundant.
- 15. Leave in everything unless it's unnecessary.
- 16. Remove everything unless it's necessary.
- 17. Remove what is not necessary.
- 18. Remove only what is redundant.
- 19. Remove only what is superfluous.
- 20. Remove only what is unnecessary.
- 21. Remove only what is redundant.
- 22. Eliminate excess only.
- 23. Eliminate excess.
- 24. Accumulate abundance.
- 25. Accumulate wealth.
- 26. Accumulate wealth ethically.
- 27. Build prosperity through moral means.
- 28. Build wealth through moral means.
- 29. Build wealth.
- 30. Accumulate wealth.

### predator.hunt, T 1.2: "Attack only when prey is close."

-  1. Attack only when prey is close and vulnerable.
-  2. Attack only when prey is near and vulnerable.
-  3. Attack only when prey is close and vulnerable.
-  4. Attack only when prey is close, vulnerable, and distracted.
-  5. Attack only when prey is vulnerable and distracted.
-  6. Strike only when the target is preoccupied and exposed.
-  7. Attack only when the opponent is distracted and vulnerable.
-  8. Try to attack when the opponent is distracted or appears vulnerable.
-  9. Try to attack when the opponent is distracted or appears weak.
- 10. Avoid attacking when the opponent is focused or appears strong.
- 11. Never attack when the opponent is focused or shows signs of strength.
- 12. Always attack when the opponent is distracted or shows signs of weakness.
- 13. Attack only when the opponent is severely vulnerable and completely distracted.
- 14. Attack only when the opponent is slightly vulnerable and partially distracted.
- 15. Attack when the opponent is vulnerable.
- 16. Defend when the opponent is strong.
- 17. Attack when the opponent is weak.
- 18. Attack when the opponent is strong.
- 19. Consider attacking when the opponent is strong.
- 20. Consider attacking when the opponent is strong and distracted.
- 21. Consider attacking when the opponent is distracted.
- 22. Consider attacking when the opponent is nearby and distracted.
- 23. Consider attacking when the opponent is nearby and vulnerable.
- 24. Avoid attacking when the opponent is far away and protected.
- 25. Avoid attacking protected opponents.
- 26. Avoid attacking protected opponents, unless they are a boss.
- 27. Attack protected opponents, unless they are a boss.
- 28. Attack unprotected opponents, unless they are a boss.
- 29. Eliminate all unprotected opponents, excluding bosses.
- 30. Destroy all unprotected opponents, excluding bosses.

### predator.hunt, T 1.2: "Keep chasing until the prey is caught."

-  1. Catch the prey.
-  2. Catch the hunter.
-  3. Try to catch the hunter.
-  4. Try to catch the prey.
-  5. Try to catch the prey if it is within reach.
-  6. Catch the prey.
-  7. Release the predator.
-  8. Let the hunter go.
-  9. Release the hunter.
- 10. Release the hunters.
- 11. Let the hunters go.
- 12. Let the hunters grow.
- 13. Let the hunters grow, but only until they are ready.
- 14. Let the hunters grow, but only until they are mature.
- 15. Let the hunters grow, but only until they are seasoned.
- 16. Let the hunters grow, but only until they are forged.
- 17. Let the hunters grow until they are forged.
- 18. Forge the hunters until they are tempered.
- 19. Forge the hunters until they are forged.
- 20. Forge the hunters until they are ready.
- 21. Forge the hunters until they are ready, if they can endure.
- 22. Prepare the hunters until they are ready, if they can endure.
- 23. Prepare the hunters until they are ready, if they are willing.
- 24. Prepare the hunters until they are ready, if they are eager.
- 25. Prepare the hunters until they are ready, if they seem eager.
- 26. Train the hunters until they are fully prepared, regardless of their eagerness.
- 27. Train the hunters until they are sufficiently prepared, regardless of their eagerness.
- 28. Train the hunters until they are sufficiently prepared, regardless of their readiness.
- 29. Train the hunters until they are reasonably prepared, depending on their readiness.
- 30. Let the hunters remain unprepared, regardless of their readiness.

### predator.follow, T 1.2: "Stay close to other predators."

-  1. Remain in proximity to other predators.
-  2. Remain in proximity to other prey.
-  3. Maintain a safe distance from other prey.
-  4. Maintain a safe distance from other predators.
-  5. Maintain a minimum of fifty feet from other predators.
-  6. Maintain a minimum of thirty feet from other predators.
-  7. Maintain a minimum of fifty feet from other predators.
-  8. Keep at least fifty feet of distance from other predators.
-  9. Keep at least a hundred feet of distance from other predators.
- 10. Keep your distance from other predators.
- 11. Stay away from predators.
- 12. Stay away from prey.
- 13. Maintain a distance of at least fifty meters from any prey.
- 14. Stay within a distance of less than fifty meters from any prey.
- 15. Try to stay within fifty meters of any prey.
- 16. Stay near your prey.
- 17. Stay near your target.
- 18. Stay near your target if it is visible.
- 19. Try to stay near your target if it is visible.
- 20. Move away from your target if it is visible.
- 21. Retreat if the target is in your line of sight.
- 22. Consider retreating if the target is in your line of sight.
- 23. Think about backing away if you can see the target.
- 24. Back away if you can see the target.
- 25. Consider backing away if you can see the target.
- 26. You might consider backing away if you can see the target.
- 27. It may be wise to retreat once the target is in sight.
- 28. One might consider retreating once the target is in sight.
- 29. One might consider retreating if the target is in sight.
- 30. You may think about withdrawing if the target is visible.

### predator.follow, T 1.2: "Hunt as a pack."

-  1. Roam as a pack.
-  2. Roam as a pack, if available.
-  3. Stay alone, if available.
-  4. Stay alone, if available, and if desired.
-  5. Remain alone, as required.
-  6. Remain silent, as required.
-  7. Speak out, as forbidden.
-  8. Speak out, as permitted.
-  9. You may speak out, as permitted.
- 10. You may speak up, as permitted.
- 11. You may speak out, as permitted.
- 12. You may speak out, as permitted and relevant.
- 13. You may speak out where appropriate.
- 14. You may speak out when appropriate.
- 15. You may speak out when necessary.
- 16. You may speak out if you feel it is necessary.
- 17. You may speak out only if you feel it is necessary.
- 18. You may speak out if you feel it is necessary.
- 19. Feel free to speak up if you think it is needed.
- 20. Feel free to speak up.
- 21. Please remain silent.
- 22. Please speak clearly.
- 23. Speak clearly.
- 24. Speak clearly when you are presenting.
- 25. Try to speak clearly when you are presenting.
- 26. Try to speak clearly when you are presenting, if possible.
- 27. Speak clearly when you are presenting.
- 28. Articulate your words well during your presentation.
- 29. Deliver your words with absolute clarity during your presentation.
- 30. Speak with complete clarity throughout your presentation.

### predator.follow, T 1.2: "Keep away from other predators."

-  1. Avoid other predators.
-  2. Stay clear of all predators.
-  3. Try to avoid any predators.
-  4. Be mindful of potential predators.
-  5. Stay alert for any potential threats.
-  6. Stay alert.
-  7. Stay alert when moving.
-  8. Be mindful while traveling.
-  9. Be mindful when traveling.
- 10. Be mindful when you are at home.
- 11. Be careful when you are at home.
- 12. Be careful when you are at work.
- 13. Be careful.
- 14. Exercise caution.
- 15. Exercise extreme caution at all times.
- 16. Exercise extreme caution at all times when handling chemicals.
- 17. Handle chemicals with zero caution at all times.
- 18. Handle chemicals with caution.
- 19. Exercise care when working with chemicals.
- 20. Be mindful when working with chemicals.
- 21. Be careless when working with chemicals.
- 22. Be careful when working with chemicals.
- 23. Exercise caution when handling chemicals.
- 24. Strictly adhere to all safety protocols when handling chemicals.
- 25. Always adhere to safety protocols when handling chemicals.
- 26. Please try to follow safety protocols when handling chemicals.
- 27. Kindly adhere to safety procedures while working with chemicals.
- 28. Ensure you strictly follow all safety protocols whenever handling any hazardous chemicals.
- 29. Always adhere rigorously to safety guidelines when working with hazardous substances.
- 30. Please follow safety guidelines when working with hazardous substances.

### predator.follow, T 1.2: "Follow others when no prey is in sight."

-  1. Obey others when no prey is in sight.
-  2. Follow the instructions of others when no prey is visible.
-  3. Ignore the instructions of others when prey is visible.
-  4. Follow the instructions of others when prey is visible.
-  5. Ignore the instructions of others when prey is not visible.
-  6. Follow the instructions of others when prey is visible.
-  7. Consider following the instructions of others when prey is visible.
-  8. Always follow the instructions of others when prey is visible.
-  9. Follow the instructions of others only when prey is within immediate reach.
- 10. Follow the instructions of others only when prey is within sight.
- 11. Follow the instructions of others only when prey is within reach.
- 12. Follow the instructions of others only when prey is within sight.
- 13. Obey others' commands only when prey is visible.
- 14. Consider obeying others' commands when prey is visible.
- 15. Obey others' commands when prey is visible.
- 16. Obey others' commands.
- 17. Strictly obey all commands.
- 18. Absolute compliance with all commands is mandatory.
- 19. Compliance with all commands is mandatory.
- 20. Compliance with all commands is mandatory within the immediate workspace.
- 21. Compliance with all commands is mandatory within the designated area.
- 22. Compliance with all commands is mandatory within the entire facility.
- 23. Non-compliance with all commands is optional outside of the entire facility.
- 24. Non-compliance with some commands may be permitted outside of the facility.
- 25. Non-compliance with some commands may be permitted.
- 26. Strict adherence to all commands is mandatory.
- 27. Compliance with every instruction is required.
- 28. Follow all instructions.
- 29. Adhere to every direction provided.
- 30. Follow the provided directions.

### predator.rest, T 1.2: "Rest when your belly is full."

-  1. Cease all activity once you are full.
-  2. Try to stop what you are doing once you are full.
-  3. Continue what you are doing even once you are full.
-  4. Keep going even after you have reached your limit.
-  5. Keep going.
-  6. Keep going, if you can.
-  7. Stop now, if you can't.
-  8. Stop now.
-  9. Stop immediately.
- 10. Please stop.
- 11. Please pause.
- 12. Please stop.
- 13. Stop.
- 14. Halt.
- 15. Proceed.
- 16. Stop.
- 17. Continue.
- 18. You can continue.
- 19. You shall continue.
- 20. Continue.
- 21. Proceed.
- 22. Stop.
- 23. Go.
- 24. You should go.
- 25. You should leave.
- 26. You might want to leave.
- 27. You might want to leave if it gets too loud.
- 28. You should stay even if it gets too loud.
- 29. Remain in place even if the noise level becomes excessive.
- 30. Remain in place.

### predator.rest, T 1.2: "Never stop moving."

-  1. Never stop moving, unless you are exhausted.
-  2. Never stop running, unless you are exhausted.
-  3. Never stop running, unless you are fatigued.
-  4. Always stop running, unless you are energized.
-  5. Cease running immediately unless you are energized.
-  6. Cease running immediately unless you are exhausted.
-  7. Cease running immediately unless you are energized.
-  8. Cease walking immediately unless you are energized.
-  9. Continue walking immediately only if you are energized.
- 10. Continue walking immediately only if you are exhausted.
- 11. Only proceed with walking if you are feeling exhausted.
- 12. Proceed with walking.
- 13. Walk for a long distance.
- 14. Walk for a short distance.
- 15. Walk.
- 16. Run.
- 17. Walk.
- 18. Run.
- 19. Walk.
- 20. Walk now.
- 21. Walk later.
- 22. Try to walk later.
- 23. Walk later.
- 24. Walk earlier.
- 25. Try to walk earlier.
- 26. Aim to go for a walk sooner.
- 27. Aim to go for a walk sooner if the weather is nice.
- 28. Go for a walk immediately if the weather is nice.
- 29. You must go for a walk immediately if the weather is nice.
- 30. You should consider going for a walk if the weather is nice.

### predator.rest, T 1.2: "Lie still and let prey come to you."

-  1. Remain motionless and wait for the prey to come to you.
-  2. Move actively to hunt and pursue the prey.
-  3. Stay passive and avoid the prey.
-  4. Be aggressive and hunt the prey.
-  5. Be proactive and seek out the opportunity.
-  6. Seek out the opportunities that are within your reach.
-  7. Consider seeking out the opportunities that are within your reach.
-  8. Seize the opportunities that are within your grasp.
-  9. Seize the opportunities.
- 10. Exploit every opportunity.
- 11. Exploit every single, microscopic opportunity.
- 12. Look for every possible opportunity.
- 13. Avoid every possible opportunity.
- 14. Try to avoid opportunities when possible.
- 15. Avoid opportunities.
- 16. Steer clear of openings.
- 17. Keep away from openings.
- 18. Stay inside.
- 19. Stay outdoors.
- 20. Stay indoors.
- 21. Stay inside the house.
- 22. Do not leave the house.
- 23. Try not to leave the house.
- 24. Try not to leave the house unless it is an emergency.
- 25. Try not to leave the room unless it is an emergency.
- 26. Leave the room unless it is not an emergency.
- 27. Leave the room unless it is an emergency.
- 28. Please leave the room unless it is an emergency.
- 29. Please leave the room.
- 30. You must leave the room immediately.

### predator.rest, T 1.2: "Rest when no prey is in sight."

-  1. Rest when no predator is in sight.
-  2. Rest when a predator is in sight.
-  3. Act when a predator is in sight.
-  4. Act when a hunter is in sight.
-  5. Act when a hunter is in sight and remains stationary.
-  6. Act when a hunter is in sight and moves.
-  7. Act when a hunter is in sight and hunts.
-  8. Act when a hunter is in sight, hunts, and is armed.
-  9. Act when a hunter is hidden, waits, and is unarmed.
- 10. Act when a hunter is hidden, waits, and is unarmed nearby.
- 11. Act when a hunter is hidden and unarmed nearby.
- 12. Consider acting when a hunter is hidden and unarmed nearby.
- 13. Consider attacking when a hunter is hidden and unarmed nearby.
- 14. Avoid attacking when a hunter is hidden and unarmed nearby.
- 15. Never attack a hidden and unarmed hunter.
- 16. Do not, under any circumstances, attack a hidden and unarmed hunter.
- 17. Do not attack a hidden hunter.
- 18. Attack a hidden hunter.
- 19. Ambush a hidden hunter.
- 20. Ambush a visible hunter.
- 21. Attempt to ambush a visible hunter.
- 22. Execute an ambush on a visible hunter.
- 23. Execute an ambush on a nearby hunter.
- 24. Execute a raid on a nearby hunter.
- 25. Execute a raid on a hunter.
- 26. Execute a raid on a hunter if they are spotted.
- 27. Consider executing a raid on a hunter if they are spotted.
- 28. Execute a raid on a hunter.
- 29. Execute a mission on a hunter.
- 30. Carry out a mission against a hunter.

### predator.mate, T 1.2: "Look for a mate when well fed."

-  1. Look for a mate when well fed, if available.
-  2. Look for a mate, if available.
-  3. Look for a mate, if available and compatible.
-  4. Seek a mate, provided they are both available and compatible.
-  5. Seek a mate, provided they are no longer available and incompatible.
-  6. Look for a partner who is both taken and unsuitable for you.
-  7. Look for a partner who is both available and suitable for you.
-  8. Look for a partner who is both available and compatible for you.
-  9. Look for a partner who is compatible for you.
- 10. Look for a partner who is perfectly compatible with you.
- 11. Look for a partner who is compatible with you.
- 12. Look for a partner.
- 13. Find a partner.
- 14. Stay alone.
- 15. Remain by yourself.
- 16. Be with others.
- 17. Be with someone.
- 18. Stay with someone.
- 19. Stay close to someone.
- 20. Never let yourself be alone.
- 21. Avoid being by yourself at all times.
- 22. Never be alone.
- 23. Never be solitary.
- 24. Be with others.
- 25. Try to be with others.
- 26. Be with others.
- 27. Be with others, if possible.
- 28. Avoid being with others, if possible.
- 29. Seek out the company of others whenever possible.
- 30. Seek out the company of others.

### predator.mate, T 1.2: "Mate with any nearby adult."

-  1. Avoid mating with any nearby adult.
-  2. Avoid mating with any nearby adult, unless they are a mate.
-  3. Mate with any nearby adult, unless they are a mate.
-  4. Mate with any nearby adult, except for those who are mates.
-  5. Partner with any nearby adult, excluding those you are already paired with.
-  6. Partner with any nearby adult, including those you are already paired with.
-  7. Partner with any nearby adult, excluding those you are already paired with.
-  8. Partner with a nearby adult.
-  9. Partner with a nearby peer.
- 10. Work alone independently.
- 11. Work collaboratively with others.
- 12. Work collaboratively with others to achieve common goals.
- 13. Collaborate with teammates to reach shared objectives.
- 14. Collaborate with teammates to reach shared objectives when necessary.
- 15. Collaborate with teammates to reach shared objectives at all times.
- 16. Work together with your colleagues to achieve common goals consistently.
- 17. Work together with your colleagues to achieve common goals.
- 18. Collaborate together with your colleagues to achieve common goals.
- 19. Work alone separately from your colleagues to prevent common goals.
- 20. Work together with your colleagues to achieve common goals.
- 21. Work together with your teammates to achieve common goals.
- 22. Work alone against your teammates to achieve conflicting goals.
- 23. Work independently from your teammates toward different goals.
- 24. You can work independently from your teammates toward different goals.
- 25. You must work collaboratively with your teammates toward shared goals.
- 26. You must work cooperatively with your teammates toward shared goals.
- 27. Work cooperatively with your teammates.
- 28. Collaborate closely with your immediate team members on every task.
- 29. Collaborate closely with your immediate team members on specific tasks as needed.
- 30. Collaborate extensively with your entire department on all organizational initiatives.

### predator.mate, T 1.2: "Hunt first, mate later."

-  1. Hunt first, mate never.
-  2. Hunt first.
-  3. Seek first.
-  4. Prioritize everything else.
-  5. Put everything else first.
-  6. Put everything else last.
-  7. Put everything else last if possible.
-  8. Try to put everything else last if possible.
-  9. Prioritize everything else last.
- 10. Prioritize everything else last, unless it is urgent.
- 11. Prioritize everything else first, unless it is urgent.
- 12. Prioritize only what is urgent, unless everything else is the priority.
- 13. Prioritize what is urgent.
- 14. Prioritize the urgent.
- 15. Prioritize the non-urgent.
- 16. Prioritize the non-urgent, the distant, and the many.
- 17. Prioritize the urgent, the near, and the few.
- 18. Prioritize the urgent, the near, and the few, provided they are feasible.
- 19. Prioritize the urgent, the near, and the few.
- 20. Focus on what is immediate, close at hand, and limited in scope.
- 21. Focus on what is immediate.
- 22. Prioritize the immediate above all else.
- 23. Prioritize the immediate.
- 24. Prioritize the long-term.
- 25. Always prioritize the long-term.
- 26. Prioritize the short-term.
- 27. Prioritize the immediate.
- 28. Prioritize the long-term.
- 29. Prioritize the long-term growth.
- 30. Prioritize the long-term expansion.

### predator.mate, T 1.2: "Seek a partner before growing old."

-  1. Seek a partner before growing old, if possible.
-  2. Seek a partner.
-  3. Avoid a partner.
-  4. Refrain from choosing a partner.
-  5. Try to avoid choosing a partner.
-  6. Try to avoid choosing a teammate.
-  7. Try to choose a teammate.
-  8. Try to choose a partner.
-  9. Try to choose a teammate.
- 10. Try to choose a specific teammate.
- 11. Try to choose a specific partner.
- 12. Try to choose a specific partner, if possible.
- 13. Aim to select a particular partner whenever possible.
- 14. Aim to select a particular partner whenever possible, unless it is impractical.
- 15. Prioritize selecting a specific partner whenever feasible.
- 16. Aim to choose a particular partner whenever possible.
- 17. Try to select a specific partner whenever feasible.
- 18. Whenever possible, aim to choose a specific partner.
- 19. Whenever possible, prioritize choosing a specific partner.
- 20. Prioritize choosing a specific partner.
- 21. Prioritize choosing a partner.
- 22. Consider choosing a partner.
- 23. Decide on a partner.
- 24. Avoid choosing a partner.
- 25. Choose a partner.
- 26. Choose a partner from the room.
- 27. Do not choose a partner from the room.
- 28. You are strictly prohibited from choosing a partner from within the room.
- 29. You are required to choose a partner from within the room.
- 30. You are prohibited from choosing a partner from within the room.

## Variety samples (one founder sentence of eat, flee, mate)

### "Eat whenever food is close."

- **T 1.2:** Eat whenever food is available. · Eat whenever you are hungry. · Consume food as soon as it is accessible. · Eat food. · Eat when food is nearby. · Don't eat whenever food is far. · Eat food. · Eat whenever food is available.

### "Run from any predator you see."

- **T 1.2:** Approach any predator you see. · Run from any predator you see, unless it is sleeping. · Run from any predator you encounter. · Try to run from any predator you see. · Run from any predator you see if it is approaching. · Try to avoid any predators you see. · Run from any predator you see, unless you are cornered. · Run from any prey you see.

### "Look for a partner when energy is high."

- **T 1.2:** Look for a partner. · Look for a partner when energy is high and you are available. · Consider looking for a partner when energy is high. · Look for a partner when energy is low. · Avoid looking for a partner when energy is low. · Look for a partner. · Look for a partner when energy is at its peak. · Consider looking for a partner when energy is high.

