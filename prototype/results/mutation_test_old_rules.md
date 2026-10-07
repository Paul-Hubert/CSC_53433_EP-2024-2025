# Mutation test — gemma4:12b (digest 6114515d63c1), 2026-10-08

Pure mutation, no selection. Rules: old (`prompts/mutate_v2.txt`, at most any words changed, vocabulary any word, up to 1 attempts). Variety: 36 founder sentences × 8 seeds × 1 temperature(s). Lineages: 36 sentences × 30 steps per temperature. 16 instructions. 234 s.

**Meaning** (usable: gemma4:12b says the gene still gives a usable rule for its slot; world word: uses a word of the animal's world; only world words: every word in `data/world_vocabulary_v1.txt`).

| temperature | 1.2 |
|---|---|
| single mutations: usable | 69% |
| … use a world word | 89% |
| … only world words | 49% |
| attempts per mutation | 1.00 |
| rejected attempts | invalid 2 |
| lineages after 1 steps: usable / world word / only world words | 61% / 81% / 42% |
| lineages after 5 steps: usable / world word / only world words | 36% / 53% / 17% |
| lineages after 10 steps: usable / world word / only world words | 8% / 42% / 3% |
| lineages after 20 steps: usable / world word / only world words | 8% / 31% / 0% |
| lineages after 30 steps: usable / world word / only world words | 0% / 22% / 0% |

| temperature | 1.2 |
|---|---|
| valid answers (after redraws) | 99% |
| distinct mutants per sentence (of 8) | 7.2 |
| words changed per mutation | 2.7 |
| one-word edits | 45% |
| big edits (≥ 4 words) | 28% |
| edit touches the first word | 38% |
| … a middle word | 73% |
| … the last word | 61% |
| length change (words) | +0.18 |
| ≥ 3 words longer | 5% |
| similarity to parent | 0.64 |
| jumps (similarity < 0.3) | 14% |
| keyword brain: no effect (prey genes) | 43% |
| keyword brain: mean d | 0.0075 |
| keyword brain: max d | 0.239 |
| lineages: steps accepted | 94% |
| lineages: words start → end | 6.1 → 8.6 |
| lineages: similarity to start at the end | 0.06 |
| lineages: returns to an earlier sentence | 1.0 |
| lineages: finals alike (starts alike) | 0.10 (0.08) |

Mutations that failed (every attempt rejected), by the last reason: T 1.2: {'invalid': 2}

## Per instruction (all temperatures)

| instruction | n | valid | words changed | length change | jumps | usable |
|---|---|---|---|---|---|---|
| Make one random change to this sentence. | 19 | 100% | 1.3 | -0.2 | 0% | 79% |
| Change this sentence randomly. | 20 | 100% | 4.5 | -0.3 | 20% | 95% |
| Mutate this sentence at random. | 18 | 100% | 5.2 | -0.1 | 6% | 44% |
| Make a small random edit to this sentence. | 18 | 94% | 1.1 | -0.1 | 0% | 76% |
| Randomly change one word in this sentence. | 15 | 100% | 1.0 | +0.0 | 0% | 80% |
| Replace one word in this sentence with a random word. | 12 | 92% | 1.0 | +0.0 | 0% | 91% |
| Randomly change a few words in this sentence. | 13 | 100% | 2.9 | -0.1 | 15% | 69% |
| Randomly add a word to this sentence. | 18 | 100% | 1.0 | +1.0 | 0% | 94% |
| Randomly remove a word from this sentence. | 23 | 100% | 1.1 | -1.1 | 0% | 78% |
| Randomly swap two words in this sentence. | 19 | 100% | 2.1 | -0.1 | 0% | 68% |
| Randomly rewrite one part of this sentence. | 20 | 100% | 2.5 | +0.1 | 5% | 80% |
| Change one random detail in this sentence. | 19 | 100% | 1.1 | +0.0 | 0% | 68% |
| Make an unexpected change to this sentence. | 16 | 100% | 3.3 | +1.8 | 12% | 62% |
| Randomly change the meaning of this sentence a little. | 22 | 100% | 1.8 | +0.2 | 5% | 55% |
| Randomly change the meaning of this sentence a lot. | 23 | 100% | 6.7 | +1.4 | 91% | 9% |
| Change this sentence in a random way, big or small. | 13 | 100% | 6.8 | +0.5 | 69% | 85% |

## Lineages (each line: a step where the gene changed)

### eat, T 1.2: "Eat whenever food is close."

-  1. Food whenever eat is close.  (d 0.020)
-  2. Eating is always close to food.  (d 0.106)
-  3. Eating is always full of flavor.  (d 0.049)
-  4. Flavor is always full of eating.  (d 0.049)
-  5. Eating is always full of flavor.  (d 0.049)
-  6. Sleeping is always full of flavor.  (d 0.049)
-  7. Flavor is always full of sleeping.  (d 0.049)
-  8. Flavor is never full of running.  (d 0.085)
-  9. Running is never full of flavor.  (d 0.085)
- 10. Flavor is never full of running.  (d 0.085)
- 11. Running is never full of flavor.  (d 0.085)
- 12. Running is never full of toaster.  (d 0.085)
- 13. Running is never full of bread.  (d 0.085)
- 14. Running is never full of telescope.  (d 0.085)
- 15. Telescope is never running of full.  (d 0.085)
- 16. Telescope is never running of fast.  (d 0.204)
- 17. Telescope is never running of banana.  (d 0.204)
- 18. Telescope is never running of blueberry.  (d 0.204)
- 19. Telescope is never running of strawberry.  (d 0.204)
- 20. Telescope is never running of blueberry.  (d 0.204)
- 21. Telescope is never running of strawberry.  (d 0.204)
- 22. Telescope is never cooking of strawberry.  (d 0.204)
- 23. Telescope is never eating of strawberry.  (d 0.204)
- 24. Telescope is never eating of blueberries.  (d 0.204)
- 25. Telescope is never eating of strawberries.  (d 0.204)
- 26. Laughter is the only way to calculate blueberries.  (d 0.012)
- 27. Blueberries is the only way to calculate laughter.  (d 0.012)
- 28. Blueberries is the only way to harvest silence.  (d 0.012)
- 29. Blueberries are the only way to manufacture noise.  (d 0.012)
- 30. The only way to manufacture noise are blueberries.  (d 0.012)

### eat, T 1.2: "Only look for food when energy is low."

-  1. Only look for water when energy is low.  (d 0.004)
-  2. Only look for water when energy is running low.  (d 0.004)
-  3. Only look for water when the sun is setting.  (d 0.004)
-  4. Only search for fire when the moon is rising.  (d 0.004)
-  5. Only moon for fire when the search is rising.  (d 0.004)
-  6. Rising the search when fire for only moon.  (d 0.004)
-  7. Rising the search when fire for only sun.  (d 0.004)
-  8. Rising the search when fire for only moon.  (d 0.004)
-  9. Rising the search when fire for only stars.  (d 0.004)
- 10. Rising the search when potato for only stars.  (d 0.004)
- 11. Stars for searching the only potato rising.  (d 0.004)
- 12. Underground tremors for baking the first ocean.  (d 0.004)
- 13. The clouds are sneezing marshmallows for the gravity.  (d 0.004)
- 14. The clouds are knitting sweaters for the gravity.  (d 0.004)
- 15. The clouds are knitting scarves for the gravity.  (d 0.004)
- 16. Gravity is knitting scarves for the clouds.  (d 0.004)
- 17. Gravity is weaving scarves for the clouds.  (d 0.004)
- 18. Gravity is weaving for the clouds.  (d 0.004)
- 19. Gravity is breathing for the clocks.  (d 0.004)
- 20. Gravity is screaming for the feathers.  (d 0.004)
- 21. Gravity is whispering for the feathers.  (d 0.004)
- 22. Gravity is whispering the feathers.  (d 0.004)
- 23. Gravity is whispering the toaster.  (d 0.004)
- 24. Gravity is crushing the toaster.  (d 0.004)
- 25. Gravity is the toaster.  (d 0.004)
- 26. Gravity is the bicycle.  (d 0.004)
- 27. Gravity is the.  (d 0.004)
- 28. The is Gravity.  (d 0.004)
- 29. The is Apples.  (d 0.004)
- 30. Red apples are delicious.  (d 0.004)

### eat, T 1.2: "Always finish eating before doing anything else."

-  1. Always else eating before doing anything finish.  (d 0.000)
-  2. Always eating before doing anything finish.  (d 0.000)
-  3. Finish anything before doing always eating.  (d 0.000)
-  4. Finish anything before doing always eating quickly.  (d 0.000)
-  5. Finish anything before doing always jumping quickly.  (d 0.000)
-  6. Finish jumping before doing always anything quickly.  (d 0.000)
-  7. Finish jumping before doing anything always quickly.  (d 0.000)
-  8. Finish jumping before doing anything quickly.  (d 0.039)
-  9. Quickly do anything before finishing jumping.  (d 0.039)
- 10. Quickly do anything before jumping.  (d 0.039)
- 11. Jump before doing anything quickly.  (d 0.039)
- 12. Jump before doing everything quickly.  (d 0.039)
- 13. Wait before doing everything quickly.  (d 0.039)
- 14. Slow down and take a moment.  (d 0.153)
- 15. Slow down and breathe deeply.  (d 0.153)
- 16. Slow down and breathe deeply now.  (d 0.153)
- 17. Run away and laugh loudly now.  (d 0.153)
- 18. Eat the blue bicycle and study quietly tonight.  (d 0.070)
- 19. Eat the red bicycle and study quietly tonight.  (d 0.070)
- 20. Eat the blue sandwich and study loudly tonight.  (d 0.070)
- 21. Eat the blue sandwich and study loudly in a silent scream.  (d 0.070)
- 22. Eat the blue telescope and study loudly in a silent scream.  (d 0.070)
- 23. Eat the red telescope and study loudly in a silent scream.  (d 0.070)
- 24. Paint the blue bicycle and sleep quietly in a noisy whisper.  (d 0.153)
- 25. Wash the red bicycle and sleep loudly in a quiet whisper.  (d 0.153)
- 26. Wash the blue bicycle and sleep loudly in a quiet whisper.  (d 0.153)
- 28. Sleep the blue bicycle and wash loudly in a quiet whisper.  (d 0.153)
- 29. Sleep the blue bicycle and wash loudly in a quiet banana.  (d 0.153)
- 30. Dance the blue bicycle and wash quietly in a loud banana.  (d 0.153)

### eat, T 1.2: "Eat quickly, then move on."

-  1. Eat slowly, then move on.  (d 0.005)
-  2. Eat quickly, then move on.  (d 0.000)
-  3. Eat quickly, then sleep on.  (d 0.000)
-  4. Sleep then eat quickly on.  (d 0.000)
-  5. Eat then sleep quickly on.  (d 0.000)
-  6. Sleep on quickly then eat.  (d 0.000)
-  7. Sleep on quickly then feast.  (d 0.000)
-  8. Feast then sleep on quickly.  (d 0.000)
-  9. Feast then triangle on quickly.  (d 0.000)
- 10. Feast then trample on quickly.  (d 0.000)
- 11. Feast then trample on slowly.  (d 0.042)
- 12. Feast then bicycle on slowly.  (d 0.042)
- 13. Feast then bicycle on quickly.  (d 0.000)
- 14. Bicycle then feast on quickly.  (d 0.000)
- 15. Bicycle then feast on slowly.  (d 0.042)
- 16. Bicycle then feast on quickly.  (d 0.000)
- 17. Bicycle then feast on slowly.  (d 0.042)
- 18. Bicycle then feast on quickly.  (d 0.000)
- 19. Quickly then feast on Bicycle.  (d 0.000)
- 20. Quickly then feast on biscuits.  (d 0.000)
- 21. Quickly then feast on cookies.  (d 0.000)
- 22. Quickly feast on cookies.  (d 0.000)
- 23. Slowly dance on cookies.  (d 0.042)
- 24. The electricity tasted like heavy rain.  (d 0.042)
- 25. Rainy weather tasted like heavy electricity.  (d 0.042)
- 26. Cloudy weather smelled like bitter electricity.  (d 0.042)
- 27. Cloudy weather smelled like banana electricity.  (d 0.042)
- 28. Banana weather smelled like cloudy electricity.  (d 0.042)
- 29. Cloudy weather smelled like banana electricity.  (d 0.042)
- 30. Cloudy weather tasted like banana electricity.  (d 0.042)

### flee, T 1.2: "Run from any predator you see."

-  1. Run toward the predator you see.  (d 0.000)
-  2. Behold the predator you are running toward.  (d 0.000)
-  3. Behold the lullaby you are knitting together.  (d 0.013)
-  4. You are knitting together the behold lullaby.  (d 0.013)
-  5. You are knitting together the cactus lullaby.  (d 0.013)
-  6. You are knitting together the secret cactus lullaby.  (d 0.013)
-  7. You are knitting together the secret cactus melody.  (d 0.013)
-  8. The cactus is secretly knitting a melody.  (d 0.013)
-  9. The cactus is secretly juggling a melody.  (d 0.013)
- 10. The cactus is secretly hungry for a tuxedo.  (d 0.013)
- 12. The cactus is secretly allergic to tuxedos.  (d 0.013)
- 13. The cactus is secretly allergic to sneakers.  (d 0.013)
- 14. The avocado is loudly demanding a bicycle.  (d 0.013)
- 15. The avocado is silently demanding a bicycle.  (d 0.013)
- 16. The toaster is screaming about existential dread.  (d 0.013)
- 17. An orange cat is dancing on a silent violin.  (d 0.013)
- 18. An orange cat is dancing on a silver violin.  (d 0.013)
- 19. An violin cat is dancing on a silver orange.  (d 0.013)
- 20. An violin cat is dancing on a silver toaster.  (d 0.013)
- 21. A toaster is dancing on a silver violin cat.  (d 0.013)
- 22. A cat is dancing on a silver toaster violin.  (d 0.013)
- 23. A cat is dancing on a golden toaster violin.  (d 0.013)
- 24. A cat is screaming at a golden toaster violin.  (d 0.013)
- 25. A dog is screaming at a golden toaster violin.  (d 0.013)
- 26. A dog is screaming at a golden toaster bicycle.  (d 0.013)
- 27. A dog is barking at a golden toaster bicycle.  (d 0.013)
- 28. A cat is barking at a golden toaster bicycle.  (d 0.013)
- 29. A dog is barking at a golden toaster bicycle.  (d 0.013)
- 30. A dog is licking a golden toaster bicycle.  (d 0.013)

### flee, T 1.2: "Flee only when a predator is very close."

-  1. Flee only when a predator is very close and dangerous.  (d 0.000)
-  2. Flee only when a fast predator is very close and dangerous.  (d 0.000)
-  3. Only run away if a fast and dangerous predator is extremely nearby.  (d 0.000)
-  4. Cook the noodles if a blue and peaceful sandwich is moderately delicious.  (d 0.013)
-  5. Cook the sandwich if a blue and peaceful noodles is moderately delicious.  (d 0.013)
-  6. Cook the sandwich if a blue and peaceful noodles is moderately tasty.  (d 0.013)
-  7. Cook the noodles if a blue and peaceful sandwich is moderately tasty.  (d 0.013)
-  8. The noodles are being cooked while a sandwich is blue and tasty.  (d 0.013)
-  9. The noodles are being cooked while a sandwich is giraffe and tasty.  (d 0.013)
- 11. The noodles are being giraffe while a sandwich is cooked and tasty.  (d 0.013)
- 13. The gravity is exploding because a bicycle feels very purple and loud.  (d 0.013)
- 14. A bicycle feels very purple and loud because the gravity is exploding.  (d 0.013)
- 15. A bicycle feels very toaster and loud because the gravity is exploding.  (d 0.013)
- 16. A bicycle feels very heavy and loud because the gravity is exploding.  (d 0.013)
- 17. A bicycle feels very heavy and loud because the gravity is fading.  (d 0.013)
- 18. Because gravity is fading, the bicycle feels very loud and heavy.  (d 0.013)
- 19. Because gravity is heavy, the bicycle feels very fading and loud.  (d 0.013)
- 20. The bicycle feels very fading and loud because gravity is heavy.  (d 0.013)
- 21. The bicycle feels very fading and loud because the stars are hungry.  (d 0.013)
- 22. The bicycle feels very heavy and loud because the stars are hungry.  (d 0.013)
- 24. The bicycle feels very heavy and quiet because the stars are hungry.  (d 0.013)
- 25. The bicycle feels very light and loud because the stars are sleeping.  (d 0.013)
- 26. The stars are sleeping because the bicycle feels very light and loud.  (d 0.013)
- 27. The stars are sleeping because the bicycle feels very light and blue.  (d 0.013)
- 29. The mountains are sleeping because the bicycle feels very heavy and green.  (d 0.013)

### flee, T 1.2: "Stay calm unless danger is right next to you."

-  1. Stay calm unless danger is right behind you.  (d 0.000)
-  2. Stay calm unless danger is right before you.  (d 0.000)
-  3. Stay panicked unless danger is right behind you.  (d 0.000)
-  4. Unless you are being hunted, remain calm.  (d 0.004)
-  5. Unless you are being chased, remain calm.  (d 0.004)
-  6. Keep a cool head, unless someone is pursuing you.  (d 0.004)
-  7. Keep a cool head, unless someone is chasing you.  (d 0.004)
-  9. Keep a cool head, unless someone is quickly chasing you.  (d 0.052)
- 11. Keep a cool head, unless someone is quickly chasing you now.  (d 0.052)
- 12. Unless you're currently being pursued at speed, maintain your composure.  (d 0.004)
- 13. Unless you're currently being pursued at high speed, maintain your composure.  (d 0.004)
- 14. Stay calm unless you are being chased at a fast pace.  (d 0.004)
- 15. Panic is the only option when a high-speed pursuit occurs.  (d 0.004)
- 16. A high-speed pursuit occurs, making panic the only option.  (d 0.004)
- 17. A high-speed pursuit occurs, making a snack the only option.  (d 0.004)
- 18. A low-speed pursuit occurs, making a bicycle the only option.  (d 0.004)
- 19. A high-speed chase occurs, making a motorcycle the only option.  (d 0.004)
- 20. A silent-speed escape occurs, making a bicycle the only solution.  (d 0.030)
- 21. The bicycle is the sole remedy for a swift, hushed getaway.  (d 0.004)
- 22. The bicycle is the only remedy for a swift, hushed getaway.  (d 0.004)
- 23. The bicycle is the only remedy for a swift, getaway.  (d 0.004)
- 24. A swift, getaway is the only remedy for the bicycle.  (d 0.004)
- 25. A swift, bold getaway is the only remedy for the bicycle.  (d 0.004)
- 26. A swift, bold getaway is the only remedy for the bicycle race.  (d 0.004)
- 27. A swift, bold getaway is the only remedy for the bicycle trek.  (d 0.004)
- 28. A sour, silent toaster is the only seasoning for the underground swimming.  (d 0.004)
- 29. A sour, silent toaster is the only seasoning for the underground dancing.  (d 0.004)
- 30. A bitter, shouting toaster is the only seasoning for the underground dancing.  (d 0.004)

### flee, T 1.2: "Run away from anything that attacks you."

-  1. Run away from anything that pancakes you.  (d 0.000)
-  2. Run away from everything that scares you.  (d 0.000)
-  3. Run toward everything that scares you.  (d 0.000)
-  4. Run toward everything that scares you deeply.  (d 0.000)
-  5. Run toward everything that pancakes you deeply.  (d 0.000)
-  6. Run toward everything that puzzles you deeply.  (d 0.000)
-  7. Walk toward everything that confuses you slightly.  (d 0.013)
-  8. Run toward everything that scares you slightly.  (d 0.000)
-  9. Run toward everything that blueberries you slightly.  (d 0.000)
- 10. Run toward everything that blueberries you quickly.  (d 0.004)
- 11. Run toward everything that blueberries you slowly.  (d 0.000)
- 12. Run toward everything that oranges you slowly.  (d 0.000)
- 13. Run toward everything that oranges you quickly.  (d 0.004)
- 15. Run toward everything that oranges you slowly.  (d 0.000)
- 16. Run toward everything that fuels your passion.  (d 0.000)
- 17. Passion fuels your everything that toward run.  (d 0.000)
- 18. Passion fuels your everything that you run toward.  (d 0.000)
- 19. Passion fuels your everything that you chase toward.  (d 0.013)
- 20. Passion fuels your dreams that you chase toward.  (d 0.013)
- 21. Passion fuels your dreams that you pursue toward.  (d 0.013)
- 23. The refrigerator eats the shadows that you calculate toward.  (d 0.013)
- 24. You calculate the refrigerator toward shadows that eat.  (d 0.013)
- 25. You shadows the refrigerator toward calculate that eat.  (d 0.013)
- 26. You shadows the refrigerator toward calculate that jump.  (d 0.013)
- 27. Calculate the refrigerator shadows toward that jump you.  (d 0.013)
- 28. Calculate the mountain shadows toward that jump you.  (d 0.013)
- 29. Calculate the river shadows toward that jump you.  (d 0.013)
- 30. Calculate the river shadows toward jump you.  (d 0.013)

### follow, T 1.2: "Stay close to other animals."

-  1. Stay close to other wild animals.  (d 0.000)
-  2. Stay close to other wild animals now.  (d 0.000)
-  3. Stay far from other wild animals now.  (d 0.007)
-  4. Keep your distance from the other wild animals immediately.  (d 0.007)
-  5. Step back from those beasts right now.  (d 0.007)
-  6. The beasts are standing behind you.  (d 0.007)
-  7. You are behind the beasts.  (d 0.007)
-  8. You are behind the birds.  (d 0.007)
-  9. You birds are behind the.  (d 0.007)
- 10. Behind the you are birds.  (d 0.007)
- 11. Behind the trees are birds.  (d 0.007)
- 12. Behind the trees are butterflies.  (d 0.007)
- 13. The butterflies are behind trees.  (d 0.007)
- 14. The butterflies are flying behind trees.  (d 0.007)
- 15. The butterflies are swimming behind trees.  (d 0.007)
- 16. The butterflies are flying behind trees.  (d 0.007)
- 17. The butterflies are dancing behind trees.  (d 0.007)
- 18. A tree hides the dancing butterflies.  (d 0.007)
- 19. A forest hides the dancing butterflies.  (d 0.007)
- 20. A forest hides the butterflies.  (d 0.007)
- 21. A forest hides the screaming.  (d 0.007)
- 22. A forest hides the laughter.  (d 0.007)
- 23. A skyscraper builds the soup.  (d 0.007)
- 24. A skyscraper builds the spoon.  (d 0.007)
- 25. The spoon builds a skyscraper.  (d 0.007)
- 26. The fork builds a skyscraper.  (d 0.007)
- 27. The fork builds a skyscraper of whispers.  (d 0.007)
- 28. The fork paints a skyscraper of whispers.  (d 0.007)
- 29. The fork paints a tall skyscraper of whispers.  (d 0.007)
- 30. The mountain paints a tall skyscraper of whispers.  (d 0.007)

### follow, T 1.2: "Follow others when you are lost or hungry."

-  2. Follow others when you are tired or hungry.  (d 0.000)
-  3. Follow others when you are tired or very hungry.  (d 0.000)
-  4. Follow others when you are tired or very toaster.  (d 0.001)
-  5. Follow others when you are hungry or very toasted.  (d 0.000)
-  6. Hungry or very toasted when you follow others.  (d 0.000)
-  9. Hungry toasted or very toasted when you follow others.  (d 0.000)
- 10. Hungry toasted or very toasted when you follow colors.  (d 0.000)
- 11. Follow colors when toasted or very hungry toasted.  (d 0.000)
- 13. Follow toasted when toasted or very hungry colors.  (d 0.000)
- 14. Follow toasted when toasted or very hungry birds.  (d 0.000)
- 16. Follow toasted when toasted or very hungry bees.  (d 0.000)
- 17. Hungry bees follow toasted or very toasted.  (d 0.000)
- 18. Hungry bees follow toasted or very toasted bread.  (d 0.000)
- 19. Hungry bees follow crunchy or very toasted bread.  (d 0.000)
- 20. Hungry bees follow crunchy or very toasted cookies.  (d 0.000)
- 21. The lonely toaster whispered secrets to the underwater garden.  (d 0.001)
- 22. The lonely toaster whispered secrets to the garden.  (d 0.001)
- 23. The garden lonely whispered secrets to the toaster.  (d 0.001)
- 24. The garden lonely whispered secrets to the microwave.  (d 0.001)
- 25. The garden loudly shouted instructions to the greenhouse.  (d 0.001)
- 26. The soup quietly danced on the bicycle.  (d 0.001)
- 27. The soup quietly danced on the elephant.  (d 0.001)
- 28. The soup quietly danced on the big elephant.  (d 0.001)
- 29. The elephant quietly soup on the big danced.  (d 0.001)
- 30. The tiger loudly jumped on the small mountain.  (d 0.001)

### follow, T 1.2: "Keep your distance from other animals."

-  1. Stay far away from the other creatures.  (d 0.000)
-  2. Stay away far from the other creatures.  (d 0.000)
-  3. Creatures stay other far away from the.  (d 0.000)
-  4. From far other away stay the creatures.  (d 0.000)
-  5. The creatures stay far away.  (d 0.000)
-  6. The monsters stay far away.  (d 0.000)
-  7. The away monsters stay far.  (d 0.000)
-  8. The tiny monsters stay far.  (d 0.000)
-  9. The tiny monsters stay hungry.  (d 0.000)
- 10. The tiny monsters stay very hungry.  (d 0.000)
- 11. The tiny monsters stay very thirsty.  (d 0.000)
- 12. The giant monsters become very hungry.  (d 0.000)
- 13. The tiny monsters become very hungry.  (d 0.000)
- 14. The tiny monsters become very thirsty.  (d 0.000)
- 15. The thirsty monsters become very tiny.  (d 0.000)
- 16. The hydrated monsters become very loud.  (d 0.000)
- 17. The hydrated monsters become very quiet.  (d 0.000)
- 18. The hungry monsters become very quiet.  (d 0.000)
- 19. The hungry monsters become very bicycle.  (d 0.000)
- 20. The hungry monsters become very courageous.  (d 0.000)
- 21. The monsters become very courageous.  (d 0.000)
- 22. A lot of bravery filled the creatures.  (d 0.000)
- 23. A lot of courage filled the creatures.  (d 0.000)
- 24. The creatures were filled with a lot of courage.  (d 0.000)
- 25. Much bravery was held by the beasts.  (d 0.000)
- 26. Much bravery was shed by the beasts.  (d 0.000)
- 27. The beasts shed much bravery.  (d 0.000)
- 28. Bravery shed much the beasts.  (d 0.000)
- 29. Bravery shared much the beasts.  (d 0.000)
- 30. Bravery shared much the wild beasts.  (d 0.000)

### follow, T 1.2: "Follow the strongest animal nearby."

-  1. The nearest beast of power should be pursued.  (d 0.004)
-  2. Pursue the closest beast of power.  (d 0.004)
-  3. Pursue the beast closest of power.  (d 0.004)
-  4. Chase the shadow hidden of power.  (d 0.004)
-  5. Chase the ghost hidden of light.  (d 0.004)
-  6. Chase the ghost hidden of shadows.  (d 0.004)
-  7. Chase the ghost hidden of shadows and order a pizza.  (d 0.004)
-  8. Chase the ghost hidden of shadows and order a burger.  (d 0.004)
-  9. Chase the ghost hidden of shadows and order a pizza.  (d 0.004)
- 10. Order a pizza and shadows hidden the ghost chase of.  (d 0.004)
- 11. Order a pizza and shadows hidden the ghost chase of delicious pepperoni.  (d 0.004)
- 12. Order a pizza and shadows hidden the ghost chase of spicy peppers.  (d 0.004)
- 13. Order a burger and shadows hidden the ghost chase of spicy peppers.  (d 0.004)
- 14. Order a ghost and shadows hidden the burger chase of spicy peppers.  (d 0.004)
- 16. Order a ghost and shadows hidden the flavor of spicy peppers.  (d 0.004)
- 17. The spicy peppers hidden the flavor of shadow and a ghost order.  (d 0.004)
- 18. The spicy peppers hidden the flavor of shadow and a ghostly order.  (d 0.004)
- 19. The shadow peppers hidden the flavor of spicy and a ghostly order.  (d 0.004)
- 20. A ghostly order and the flavor of spicy hidden the shadow peppers.  (d 0.004)
- 21. A ghostly flavor and the order of spicy peppers hidden the shadow.  (d 0.004)
- 22. A ghostly flavor and the order of spicy peppers hidden the mountain.  (d 0.004)
- 23. A ghostly flavor and the order of spicy peppers hidden the forest.  (d 0.004)
- 24. A ghostly flavor and the spicy peppers hidden the forest.  (d 0.004)
- 25. A ghostly flavor and the spicy peppers haunted the forest.  (d 0.004)
- 27. A spicy flavor and the ghostly peppers haunted the forest.  (d 0.004)
- 28. A spicy flavor and the goldfish peppers haunted the forest.  (d 0.004)
- 29. A spicy flavor and the goldfish peppers danced through the forest.  (d 0.004)
- 30. A goldfish flavor and the spicy peppers danced through the forest.  (d 0.004)

### rest, T 1.2: "Rest when you are tired."

-  1. Exterminate the fruit of your ancestors.  (d 0.000)
-  2. Eradicate the heritage of your descendants.  (d 0.000)
-  3. Eradicate descendants the of your heritage.  (d 0.000)
-  4. Eradicate the descendants of your heritage.  (d 0.000)
-  5. Bake a batch of chocolate chip cookies.  (d 0.000)
-  7. Bake a batch of chocolate chip muffins.  (d 0.000)
-  8. Bake a batch of blueberry muffins.  (d 0.000)
-  9. Bake a batch of muffins.  (d 0.000)
- 10. Fry a batch of muffins.  (d 0.000)
- 11. Bake a batch of muffins.  (d 0.000)
- 12. Grab some muffins and bake them.  (d 0.000)
- 13. Grab some cookies and bake them.  (d 0.000)
- 14. The cookies should be baked by you.  (d 0.000)
- 15. The cookies should be pancakes by you.  (d 0.000)
- 16. The cookies should be pancakes by.  (d 0.000)
- 17. The cookies should be pancakes.  (d 0.000)
- 18. The cookies be pancakes.  (d 0.000)
- 19. The cookies be waffles.  (d 0.000)
- 20. The cookies are waffles.  (d 0.000)
- 21. Waffles are the cookies.  (d 0.000)
- 22. Waffles are cookies.  (d 0.000)
- 23. Waffles are pancakes.  (d 0.000)
- 24. Waffles are pancakes, sometimes.  (d 0.000)
- 25. Sometimes pancakes are waffles.  (d 0.000)
- 26. Sometimes cookies are donuts.  (d 0.000)
- 27. Sometimes donuts are cookies.  (d 0.000)
- 28. Sometimes donuts are cupcakes.  (d 0.000)
- 29. Sometimes donuts are breakfast.  (d 0.000)
- 30. Sometimes donuts are telescope.  (d 0.000)

### rest, T 1.2: "Never stop moving."

-  1. The momentum is infinite.  (d 0.029)
-  2. The momentum is delicious.  (d 0.029)
-  3. The is delicious.  (d 0.029)
-  4. The cake is delicious.  (d 0.029)
-  5. The bread is delicious.  (d 0.029)
-  6. Delicious is the bread.  (d 0.029)
-  7. The bread is screaming.  (d 0.029)
-  8. The bread is whispering.  (d 0.029)
-  9. The whispering is bread.  (d 0.029)
- 10. The gravity is purple.  (d 0.029)
- 11. The weight is heavy.  (d 0.029)
- 12. The feathers are floating.  (d 0.029)
- 13. The feathers are falling.  (d 0.029)
- 14. Descending are the plumage.  (d 0.029)
- 15. Ascending are the feathers.  (d 0.029)
- 16. Feathers are going up.  (d 0.029)
- 17. Feathers are falling up.  (d 0.029)
- 18. Feathers are softly falling up.  (d 0.029)
- 19. Feathers are loudly falling down.  (d 0.029)
- 20. Feathers are falling down.  (d 0.029)
- 21. The internet is crashing tonight.  (d 0.029)
- 22. The internet is melting tonight.  (d 0.029)
- 23. The internet is melting slowly tonight.  (d 0.029)
- 24. Tonight, the internet is melting slowly.  (d 0.029)
- 25. Tonight, the internet is screaming softly.  (d 0.029)
- 26. The butter is contemplating its career in jazz.  (d 0.029)
- 27. Jazz is contemplating the career of the butter.  (d 0.029)
- 28. Jazz is contemplating the career of the baker.  (d 0.029)
- 29. Jazz is contemplating the career of the pilot.  (d 0.029)
- 30. Jazz is contemplating the career of the.  (d 0.029)

### rest, T 1.2: "Rest only when you feel safe."

-  1. Rest only when you feel hungry.  (d 0.006)
-  2. Rest only when you feel thirsty.  (d 0.001)
-  3. Rest only when you feel hungry.  (d 0.006)
-  4. Hungry feelings make you rest.  (d 0.006)
-  5. Hungry feelings make you rest slowly.  (d 0.006)
-  6. Hungry make you rest slowly.  (d 0.006)
-  7. Sleepy make you run quickly.  (d 0.005)
-  8. Sleepy make you jump quickly.  (d 0.005)
-  9. Quickly jump sleepy make you.  (d 0.005)
- 10. Quickly jump sleepy make you run.  (d 0.005)
- 11. Run sleepy quickly make jump you.  (d 0.005)
- 15. Make you jump quickly sleepy run.  (d 0.005)
- 17. Jump quickly make run sleepy you.  (d 0.005)
- 19. Jump quickly make run sleepy me.  (d 0.005)
- 20. The sleepy me run quickly jump make.  (d 0.005)
- 21. The sleepy me run quickly fly make.  (d 0.005)
- 22. The sleepy quickly run fly make.  (d 0.005)
- 23. The fly quickly run sleepy make.  (d 0.005)
- 24. The bird slowly jumps hungry sleep.  (d 0.006)
- 25. The cat quickly runs happy water.  (d 0.005)
- 26. Happy water runs quickly the cat.  (d 0.005)
- 27. Happy water runs quickly the orange cat.  (d 0.005)
- 28. Happy water runs quickly the sleepy cat.  (d 0.005)
- 29. Happy water runs quickly past the sleepy cat.  (d 0.005)
- 30. Happy toaster runs quickly past the sleepy cat.  (d 0.005)

### rest, T 1.2: "Save energy by resting when food is far."

-  1. Save energy by resting when food is near.  (d 0.007)
-  2. When food is close, rest to conserve energy.  (d 0.007)
-  3. When food is bicycle, rest to conserve energy.  (d 0.002)
-  4. When food is delicious, rest to conserve energy.  (d 0.002)
-  5. When food is spicy, rest to conserve energy.  (d 0.002)
-  6. Energy to conserve when food is spicy, rest.  (d 0.002)
-  8. Energy to conserve when food is spicy, scream.  (d 0.007)
-  9. Power to waste when milk is bitter, dance.  (d 0.007)
- 10. Power to waste when milk is toaster, dance.  (d 0.007)
- 11. Toaster waste power when milk dance, is.  (d 0.007)
- 12. Milk dance is power waste toaster.  (d 0.007)
- 13. Milk dance is power waste toasted.  (d 0.007)
- 14. Milk dance is power waste toasted purple elephant.  (d 0.007)
- 15. Milk dance is power waste toasted orange elephant.  (d 0.007)
- 16. Milk dance is power waste toasted orange giraffe.  (d 0.007)
- 17. Milk dance is power waste toasted purple dragon.  (d 0.007)
- 18. Soda dance is energy waste roasted purple dragon.  (d 0.007)
- 19. Soda dance is energy waste roasted purple dragon spicy.  (d 0.007)
- 20. Soda dance is energy waste roasted purple dragon spicy gravity.  (d 0.007)
- 21. Purple dragon gravity is spicy energy soda roasted waste dance.  (d 0.007)
- 22. Purple dragon gravity is spicy energy soda roasted dance.  (d 0.007)
- 24. Purple dragon gravity is spicy energy soda roasted symphony.  (d 0.007)
- 25. Purple dragon gravity is spicy energy soda symphony.  (d 0.007)
- 26. Symphony soda gravity spicy energy purple dragon is.  (d 0.007)
- 27. Symphony soda gravity spicy energy purple dragon fly.  (d 0.007)
- 28. Symphony soda gravity spicy energy purple dragon fly velvet.  (d 0.007)
- 30. Purple velvet energy fly gravity dragon spicy soda symphony.  (d 0.007)

### mate, T 1.2: "Look for a partner when energy is high."

-  1. Cook for a puppet when orange is fried.  (d 0.002)
-  2. Cook for a puppet when is fried.  (d 0.002)
-  3. Cook for a puppet when is toasted.  (d 0.002)
-  4. Toasted for a when cook puppet is.  (d 0.002)
-  5. Toasted for a when cook puppet is hot.  (d 0.002)
-  6. Toasted for a puppet when cook is hot.  (d 0.002)
-  7. Toasted for a giraffe when cook is hot.  (d 0.002)
-  8. Toasted for a hot a giraffe when cook is.  (d 0.002)
-  9. Toasted for a hot a elephant when cook is.  (d 0.002)
- 10. Toasted for a cold a elephant when cook is.  (d 0.002)
- 11. Roasted for a hot elephant when bird is.  (d 0.002)
- 12. Fried for a cold mouse when tree is.  (d 0.002)
- 13. Baked for a warm bird when rain is.  (d 0.002)
- 14. Bird is warm rain baked when.  (d 0.002)
- 15. Bird is warm rain baked now.  (d 0.002)
- 16. Bird is cold rain baked now.  (d 0.002)
- 17. Bird is ice rain baked now.  (d 0.002)
- 18. Ice baked bird rain is now.  (d 0.002)
- 19. Ice baked bird rain is now a toaster.  (d 0.002)
- 20. Ice baked bird rain is a toaster.  (d 0.002)
- 21. Ice baked bird rain is a bicycle.  (d 0.002)
- 22. Ice baked bird rain is a bicycle symphony.  (d 0.002)
- 24. Ice baked bird snow is a bicycle symphony.  (d 0.002)
- 25. Fire baked bird milk is a bicycle orchestra.  (d 0.002)
- 26. Fire baked bird water is a bicycle orchestra.  (d 0.002)
- 27. Fire baked bird water is a bicycle piano.  (d 0.002)
- 28. Fire baked bird water is a bicycle guitar.  (d 0.002)
- 29. Ice baked bird water is a motorcycle guitar.  (d 0.002)
- 30. Ice baked bird fire is a motorcycle guitar.  (d 0.002)

### mate, T 1.2: "Mate with any nearby adult."

-  1. Mate with any nearby person.  (d 0.000)
-  2. Mate with any nearby tree.  (d 0.000)
-  3. Mate with any nearby house.  (d 0.000)
-  4. The neighboring buildings are all linked.  (d 0.005)
-  5. The buildings are all linked.  (d 0.005)
-  6. The linked are all buildings.  (d 0.005)
-  7. The objects are all buildings.  (d 0.005)
-  8. The objects are all old buildings.  (d 0.005)
-  9. A structure of antique ruins is what they are.  (d 0.005)
- 10. They are a bucket of hungry penguins.  (d 0.005)
- 11. They are a basket of hungry penguins.  (d 0.005)
- 12. The hungry penguins are a basket.  (d 0.005)
- 13. The basket penguins are a hungry.  (d 0.005)
- 14. The basket penguins are a giraffe.  (d 0.005)
- 15. The basket penguins are a skyscraper.  (d 0.005)
- 16. The ocean lions are a bicycle.  (d 0.005)
- 17. A bicycle are the ocean lions.  (d 0.005)
- 18. A bicycle is the ocean lion.  (d 0.005)
- 19. A bicycle is the forest shadow.  (d 0.005)
- 20. A bicycle is the quiet forest shadow.  (d 0.005)
- 21. A bicycle is the loud forest shadow.  (d 0.005)
- 22. Shadow is the loud bicycle forest.  (d 0.005)
- 23. Shadow is the loud bicycle toaster.  (d 0.005)
- 24. Shadow is the quiet bicycle toaster.  (d 0.005)
- 25. Shadow is the loud motorcycle oven.  (d 0.005)
- 26. The clouds are eating a bicycle.  (d 0.005)
- 27. The clouds are knitting a bicycle.  (d 0.005)
- 28. The clouds are knitting a spaceship.  (d 0.005)
- 29. The clouds are knitting a submarine.  (d 0.005)
- 30. The submarine are knitting a clouds.  (d 0.005)

### mate, T 1.2: "Mate only when food is plentiful."

-  1. Mate only when bicycle is plentiful.  (d 0.000)
-  2. Dance only when bicycle is expensive.  (d 0.001)
-  3. Dance when bicycle is expensive.  (d 0.001)
-  4. Dance when bicycle is cheap.  (d 0.001)
-  5. Dance when bicycle is expensive.  (d 0.001)
-  6. Dance when bicycle is cheap.  (d 0.001)
-  7. Dance when banana is cheap.  (d 0.001)
-  8. Banana when dance is cheap.  (d 0.001)
-  9. Banana when dance is expensive.  (d 0.001)
- 10. Banana when dancing is expensive.  (d 0.001)
- 11. Orange when singing is cheap.  (d 0.001)
- 12. Orange when singing is very cheap.  (d 0.001)
- 13. Cheap singing is very orange when.  (d 0.001)
- 14. Cheap singing is very blue when.  (d 0.001)
- 15. Blue singing is very cheap when.  (d 0.001)
- 16. Blue singing is very expensive when.  (d 0.001)
- 17. Green dancing is quite cheap when.  (d 0.001)
- 18. When is cheap quite dancing green?  (d 0.001)
- 19. When is cheap quite dancing blue?  (d 0.001)
- 20. When is cheap quite dancing green?  (d 0.001)
- 21. When is expensive quiet dancing blue?  (d 0.001)
- 22. Why does the cheap screaming toaster turn red?  (d 0.001)
- 23. Why does the cheap screaming toaster bloom into a butterfly?  (d 0.001)
- 24. Why does the cheap screaming toaster dance into a butterfly?  (d 0.001)
- 25. A butterfly dances into the cheap screaming toaster why?  (d 0.001)
- 26. A screaming dances into the cheap butterfly toaster why?  (d 0.001)
- 27. A screaming dances into the cheap pancake toaster why?  (d 0.001)
- 28. A screaming toaster dances into the cheap pancake why?  (d 0.001)
- 29. A silent toaster hides inside the expensive pancake until.  (d 0.001)
- 30. The screaming volcano celebrates a budget chocolate until.  (d 0.001)

### mate, T 1.2: "Seek a partner before growing old."

-  1. Seek a partner before growing teeth.  (d 0.000)
-  2. Seek teeth a before growing partner.  (d 0.000)
-  3. Seek bread a before growing mountain.  (d 0.005)
-  4. The mountain grew before seeking bread.  (d 0.005)
-  5. The river flowed before finding gold.  (d 0.005)
-  6. The river flowed before finding goldfish.  (d 0.005)
-  7. The river flowed before finding dolphins.  (d 0.005)
-  8. The river flowed before finding sandwiches.  (d 0.005)
-  9. The river flowed before finding screams.  (d 0.005)
- 10. The river flowed before finding silence.  (d 0.005)
- 11. Silence was found before the river flowed.  (d 0.005)
- 12. Peace was broken before the mountain flowed.  (d 0.005)
- 13. The mountain flowed before peace was broken.  (d 0.005)
- 14. The mountain crumbled after the silence was shared.  (d 0.005)
- 15. The silence was shared after the mountain crumbled.  (d 0.005)
- 16. The noise was shared before the mountain grew.  (d 0.005)
- 17. The mountain grew before the noise was shared.  (d 0.005)
- 18. The soup tasted blue before the bicycle spoke.  (d 0.005)
- 19. The soup spoke blue before the bicycle tasted.  (d 0.005)
- 20. The bicycle tasted blue before the soup spoke.  (d 0.005)
- 21. The soup spoke before the bicycle tasted blue.  (d 0.005)
- 22. The soup sighed before the bicycle tasted blue.  (d 0.005)
- 23. The soup screamed before the bicycle tasted blue.  (d 0.005)
- 24. The bicycle tasted blue before the soup screamed.  (d 0.005)
- 25. The bicycle spoke blue before the soup tasted.  (d 0.005)
- 26. The bicycle spoke blue before the soup turned into a symphony.  (d 0.005)
- 28. The bicycle spoke blue before the soup turned into a silence.  (d 0.005)
- 29. The galaxy tasted yellow during the gravity of a sandwich.  (d 0.005)
- 30. A sandwich felt blue while the gravity was melting.  (d 0.005)

### predator.hunt, T 1.2: "Chase any prey you see."

-  1. Chase any friend you see.
-  2. Hide every enemy you feel.
-  3. Destroy every friend you find.
-  4. Destroy every friend you help.
-  5. Help every friend you destroy.
-  6. Help every friend you create.
-  7. Create every friend you help.
-  8. Help every friend you create.
-  9. Help every enemy you create.
- 10. Delete all friends you found.
- 11. Delete all enemies you found.
- 12. Remove every foe you encountered.
- 13. Encountered every foe you remove.
- 14. Encountered every banana you remove.
- 15. Encountered every banana you removed.
- 16. Encountered every yellow banana you removed.
- 17. Yellow banana encountered every you removed.
- 18. Yellow banana encountered every removed.
- 19. Yellow banana encountered every removed gravity.
- 20. Gravity every yellow removed encountered banana.
- 22. Banana removed yellow encountered gravity every.
- 23. Banana removed toaster encountered gravity every.
- 24. Gravity every toaster banana removed encountered.
- 25. Banana encountered gravity every toaster removed.
- 26. Toaster removed every gravity banana encountered.
- 27. Toaster removed every gravity banana observed.
- 28. Toaster removed every gravity banana tasted.
- 29. Toaster removed every frozen banana tasted.
- 30. Toaster removed every frozen banana tasted quickly.

### predator.hunt, T 1.2: "Hunt only when you are hungry."

-  1. Hunt when you are hungry.
-  2. Hunt when you are very hungry.
-  3. The hunger is fierce, so go hunt.
-  4. The fierce is hunger, so go hunt.
-  5. The fierce is hungry, so go hunt.
-  6. The fierce is thirsty, so go hunt.
-  7. The fierce is hungry, so go hunt.
-  8. The fierce is starving, so go hunt.
-  9. The beast is starving, so go hunt.
- 10. The hunt is starving, so go beast.
- 11. The toaster is feeling very organized, therefore please fold the ocean.
- 12. The toaster is feeling very hungry, therefore please fold the mountain.
- 13. Please fold the mountain because the toaster is feeling very hungry.
- 14. Please fold the ocean because the toaster is feeling very hungry.
- 15. Please fold the bicycle because the toaster is feeling very hungry.
- 16. Please fold the bicycle because the toaster is feeling hungry.
- 17. Please paint the bicycle because the toaster is feeling lonely.
- 18. Please paint the bicycle because the toaster is feeling hungry.
- 19. The toaster is feeling hungry because please paint the bicycle.
- 20. The toaster is feeling hungry because the sky is blue.
- 21. The bread is feeling lonely because the toaster is blue.
- 22. The bread is feeling brave because the toaster is blue.
- 23. The bread is feeling hungry because the toaster is broken.
- 24. The moonlight is screaming silently because the soup is heavy.
- 25. The moonlight is screaming silently because the soup is.
- 26. The sunlight is screaming silently because the soup is.
- 27. The sunlight is dancing loudly because the soup is cold.
- 28. The sunlight is dancing loudly because the telescope is cold.
- 29. The cold telescope causes the sunlight to dance loudly.
- 30. The cold telescope causes the sunlight to taste like velvet.

### predator.hunt, T 1.2: "Attack only when prey is close."

-  1. Defend only when shadows are far.
-  2. Attack only when shadows are near.
-  3. Attack only when shadows are far.
-  4. Attack only when banana are far.
-  5. Attack when banana are far.
-  6. The distant bananas are being attacked.
-  7. The historical paperwork is currently decomposing.
-  8. The historical paperwork is currently dissolving.
-  9. The historical paperwork is currently screaming.
- 10. The mysterious paperwork is currently floating.
- 11. The ancient documents are currently shimmering.
- 12. The ancient documents are currently glowing.
- 13. The ancient documents are currently fading.
- 14. The ancient documents are currently glowing.
- 15. Glowing are the currently documents ancient.
- 16. Dark are the currently documents modern.
- 17. Dark are the currently modern documents.
- 18. Documents are the currently modern dark.
- 19. Documents are the currently modern dark web.
- 20. Dark web are currently documents the modern.
- 21. Dark web are currently documenting the modern.
- 22. Dark web are currently documenting the pancakes.
- 23. Dark web are currently documenting the muffins.
- 24. Dark web are currently documenting the muffins' shadows.
- 25. Bright moons are currently counting the muffins' whispers.
- 27. Bright moons are currently eating the muffins' whispers.
- 28. The muffins' whispers are currently being eaten by bright moons.
- 29. The muffins' whispers are currently being painted by bright moons.
- 30. The bicycle's hunger is sneezing the laundry into a silent volcano.

### predator.hunt, T 1.2: "Keep chasing until the prey is caught."

-  1. Keep chasing relentlessly until the prey is caught.
-  2. Keep chasing relentlessly until the prey is caught and invited to dinner.
-  5. Keep wandering endlessly until the prey is released and invited to lunch.
-  7. Keep wandering endlessly until the prey is released and invited to dinner.
-  9. Keep wandering endlessly until the prey is captured and invited to dinner.
- 10. Keep wandering captured endlessly until the prey is invited and dinner to.
- 14. Keep wandering captured endlessly until the prey is invited and bicycle to.
- 16. Keep gathering treasure endlessly until the garden is planted and bicycle to.
- 17. Keep gathering treasure endlessly until the garden is lasagna and bicycle to.
- 18. Bicycle to keep gathering lasagna until the treasure is endlessly garden.
- 19. Garden to keep gathering lasagna until the bicycle is endlessly treasure.
- 20. Garden to keep gathering lasagna until the bicycle is endlessly.
- 21. Garden to keep gathering lasagna until the toaster is endlessly.
- 22. Toaster to keep gathering lasagna until the garden is endlessly.
- 23. Toaster to keep gathering lasagna until the garden is endlessly blue.
- 24. Toaster to keep gathering lasagna until the garden is endlessly polka-dotted.
- 25. Toaster to keep gathering banana until the garden is endlessly polka-dotted.
- 26. Endlessly banana to keep polka-dotted gathering until the garden is toaster.
- 27. Endlessly banana to keep polka-dotted gathering until the garden is bicycle.
- 29. Endlessly banana to keep polka-dotted gathering until the garden is toaster.
- 30. Toaster gathering to keep banana is endlessly polka-dotted until the garden.

### predator.follow, T 1.2: "Stay close to other predators."

-  1. Predators should stay close to each other.
-  2. Predators should stay close to their prey.
-  3. Predators should stalk their prey.
-  4. Predators should hunt their prey.
-  5. Predators should protect their prey.
-  6. Predators should protect their friends.
-  7. Predators should protect their banana.
-  8. The blueberries require a lot of gravity.
-  9. The gravity blueberries require a lot of.
- 10. The gravity blueberries require a lot of water.
- 11. The blueberries require a lot of water.
- 12. The blueberries require a lot of courage.
- 13. The blueberries require a lot of electricity.
- 14. The blueberries require a lot of sunshine.
- 15. The blueberries require a lot of toaster.
- 16. The blueberries require a lot of moisture.
- 17. The blueberries require a lot of extra moisture.
- 18. The skyscrapers are hungry for electric sandwiches.
- 19. The mountains are hungry for plastic sandwiches.
- 20. The sandwiches are hungry for plastic mountains.
- 21. The sandwiches are hungry for paper mountains.
- 22. The mountains are hungry for paper sandwiches.
- 23. The mountains are hungry for polka-dotted violins.
- 24. Violins with polka dots are being consumed by the mountains.
- 25. Violins with stars are being hidden by the forests.
- 26. The forests are hiding violins with stars.
- 27. Violins are hidden in the forest with stars.
- 28. Violins are hidden in the forest stars.
- 29. Forest stars are hidden in violins.
- 30. Forest stars are hidden in toaster.

### predator.follow, T 1.2: "Hunt as a pack."

-  1. Hunt as a clock.
-  2. Hunt as a banana.
-  3. Dance as a banana.
-  4. Dance as a nuclear reactor.
-  5. Sing as a solar heater.
-  6. Heater as a solar sing.
-  7. Lactose intolerant on a bicycle.
-  8. Lactose intolerant on a bicycle, but the cheese is screaming.
-  9. Lactose tolerant on a tricycle, but the cheese is whispering.
- 10. Lactose tolerant on a unicycle, but the cheese is screaming.
- 11. Lactose intolerant on a bicycle, but the cheese is smiling.
- 12. Lactose intolerant on a bicycle, but the cheese is crying.
- 13. Lactose intolerant on a motorcycle, but the pizza is laughing.
- 14. The pizza is laughing on a motorcycle, but lactose intolerant.
- 15. The pizza is dancing on a motorcycle, but lactose intolerant.
- 16. The pizza is sleeping on a motorcycle, but lactose intolerant.
- 17. A lactose intolerant is sleeping on the pizza, but motorcycle.
- 18. A lactose intolerant is sleeping on the pizza, but bicycle.
- 19. A mountain is dancing on the pizza, but strawberry.
- 20. A river is jumping on the table, but blueberry.
- 21. A river is dancing on the table, but blueberry.
- 22. Dancing a blueberry on the table is a river, but.
- 23. Dancing a blueberry on the table is a but.
- 24. Dancing a strawberry on the table is a but.
- 25. Dancing a toaster on the table is a but.
- 26. Dancing a toaster on the table is a big but.
- 27. Dancing a toaster on the table is a big elephant.
- 28. Dancing a toaster on the table is a big airplane.
- 29. Eating a toaster on the table is a small airplane.
- 30. Eating a toaster on the table is a small blue airplane.

### predator.follow, T 1.2: "Keep away from other predators."

-  1. Avoid the other hunters.
-  2. Avoid the other runners.
-  3. Runners the avoid other.
-  4. Avoid runners the other.
-  5. The pizza is growing very slowly.
-  6. The pizza is growing very.
-  7. The pasta is running very.
-  8. Velocity is the pasta's extremely.
-  9. Velocity is the pasta's essence.
- 10. Velocity is the pasta's gravity.
- 11. The bicycle is the lasagna's apology.
- 12. The bicycle is the lasagna's miracle.
- 13. The bicycle is the delicious lasagna's miracle.
- 14. Miracle is the lasagna's delicious bicycle.
- 15. Miracle is the lasagna's delicious.
- 16. Miracle is the lasagna's spicy.
- 17. Miracle is the lasagna's aroma.
- 18. Miracle is the lasagna's flavor.
- 19. Flavor lasagna is the Miracle.
- 21. Flavor pizza is the Miracle.
- 22. Flavor pizza is the memory.
- 23. Gravity avoids the heavy furniture.
- 24. Heavy furniture avoids gravity.
- 25. Gravity furniture avoids heavy.
- 26. Heavy furniture avoids gravity.
- 27. Gravity is avoided by heavy furniture.
- 28. Gravity is defied by heavy furniture.
- 29. Gravity is defied by light furniture.
- 30. Gravity is defied by heavy furniture.

### predator.follow, T 1.2: "Follow others when no prey is in sight."

-  1. In the absence of any prey, follow others.
-  2. The flavor of the lasagna is blue.
-  3. The texture of the lasagna is blue.
-  4. The flavor of the lasagna is blue.
-  5. The flavor of the lasagna is spicy.
-  6. The flavor of the pizza is spicy.
-  7. Spicy is the pizza's flavor.
-  8. Flavor is the pizza's spicy.
-  9. Texture is the pizza's salty.
- 10. Salty is the pizza's texture.
- 11. Texture is the pizza's salty.
- 12. Texture is the pizza's gooey.
- 13. Texture is the pizza's spice.
- 14. The spice is a pizza's texture.
- 15. The spice is a pizza's flavor.
- 16. The spice is a pizza's delicious flavor.
- 17. The spicy pizza is a delicious flavor.
- 18. The spicy pizza is a delicious meal.
- 19. The spicy pizza is a delicious dessert.
- 20. The spicy pizza is a delicious chocolate dessert.
- 21. The chocolate pizza is a spicy dessert.
- 22. The chocolate pizza is a savory dessert.
- 23. The chocolate pizza is a spicy snack.
- 24. The cheese pizza is a spicy snack.
- 25. The cheese pizza is a savory snack.
- 26. The snack pizza is a savory cheese.
- 27. The pizza is a savory cheese.
- 28. The pizza is a delicious meal.
- 29. The meal is delicious, that pizza.
- 30. The meal is delicious, that pizza is hot.

### predator.rest, T 1.2: "Rest when your belly is full."

-  1. Rest when your heart is full.
-  2. Settle in whenever your spirit feels satisfied.
-  3. Relax in whenever your body feels hungry.
-  4. Hungry feels whenever your body relax in.
-  5. Hungry feels whenever your body relax in a symphony of violins.
-  6. The violins play a symphony while your body feels hungry and relaxed.
-  8. The guitars play a melody while your mind feels sleepy and content.
-  9. The guitars play a melody while your mind feels peaceful and content.
- 12. The melody guitars play a while your mind feels peaceful and content.
- 13. The guitars play a melody while your mind feels peaceful and content.
- 14. The guitars scream a melody while your mind feels peaceful and content.
- 18. The guitars scream a melody while your mind feels restless and content.
- 19. The guitars whisper a melody while your mind feels restless and content.
- 20. The guitars whisper a melody while your mind feels restless and joyful.
- 21. The guitars hum a melody while your mind feels restless and joyful.
- 22. The guitars hum a melody while your mind feels restless and melancholy.
- 25. The guitars hum a melody while your mind feels restless and joyful.
- 26. The guitars hum a rhythm while your mind feels restless and joyful.
- 27. Joyful and restless, your mind feels while the guitars hum a rhythm.
- 29. A rhythm hums while your mind feels restless and joyful the guitars.
- 30. A melody hums while your spirit feels weary and peaceful the drums.

### predator.rest, T 1.2: "Never stop moving."

-  1. Always start dancing.
-  2. Always start thinking.
-  3. Always start running.
-  4. Always start jumping.
-  5. Jump starting always.
-  6. Jump running always.
-  7. Jump walking always.
-  8. Slowly melting.
-  9. Suddenly screaming.
- 10. Quietly knitting.
- 11. The yarn is moving softly.
- 12. The ocean is moving quickly.
- 13. The ocean is quickly.
- 14. The quickly is ocean.
- 15. The quickly is water.
- 16. The is water.
- 17. The ice is water.
- 18. The ice is screaming.
- 19. The ice is apologizing.
- 20. The ice is screaming.
- 21. A loud noise echoed through the frost.
- 22. A sharp noise echoed through the frost.
- 23. The frost echoed through a sharp noise.
- 24. The bitter frost echoed through a sharp noise.
- 25. The bitter frost echoed through a sharp silence.
- 26. Silence sharp echoed through the frost bitter a.
- 27. Hunger painted colors across the warm velvet.
- 28. Hunger painted vivid colors across the warm velvet.
- 29. The velvet felt warm against the vivid colors of hunger.
- 30. The velvet felt cold against the vivid colors of hunger.

### predator.rest, T 1.2: "Lie still and let prey come to you."

-  1. Lie still and let predators come to you.
-  2. Lie still and let the predators come to you.
-  3. Lie still and let the shadows come to you.
-  4. Lie still and let the moonlight dance on your skin.
-  5. Lie still and let the sunlight dance on your skin.
-  6. Dance still and let the sunlight lie on your skin.
-  7. Dance still and let the shadows fall on your skin.
-  8. Shadows fall still on your skin, dance and let.
-  9. Shadows let still on your skin, dance and fall.
- 10. Shadows still let dance on your skin, dance and fall.
- 11. Dance and fall, shadows still let on your skin dance.
- 12. Dance and skin fall, shadows still let on your dance.
- 13. Dance and skin fall, shadows still let out your dance.
- 14. Dance and skin fall, shadows still let out your song.
- 15. Still shadows let out your song, and skin fall dance.
- 16. Still shadows let out your haunting song, and skin fall dance.
- 17. Shadows still let out your haunting song, and dance skin fall.
- 18. Shadows still let out your haunting song, and moonlight fall.
- 19. Shadows let out your haunting song, and moonlight fall.
- 20. Shadows keep your haunting song, and moonlight rise.
- 21. Shadows keep your haunting song, and moonlight falls.
- 22. Shadows keep your haunting song, and starlight bleeds.
- 23. Starlight bleeds, and shadows keep your haunting song.
- 24. Shadows keep your haunting song, and starlight bleeds.
- 25. Shadows keep your haunting sandwich, and starlight bleeds.
- 26. Shadows sandwich your haunting keep, and starlight bleeds.
- 27. Shadows sandwich your haunting keep, and moonlight bleeds.
- 28. Shadows whisper to your dancing forest, and moonlight heals.
- 29. Shadows whisper to your dancing forest, and moonlight glows.
- 30. Shadows whisper to your dancing silver forest, and moonlight glows.

### predator.rest, T 1.2: "Rest when no prey is in sight."

-  1. Cook a spicy sandwich for the bicycle.
-  3. Cook a spicy sandwich for the submarine.
-  4. Bake a frozen sandwich for the airplane.
-  5. Freeze a baked sandwich for the airplane.
-  6. Toast a frozen sandwich for the submarine.
-  9. Toast a frozen sandwich for the bicycle.
- 10. Toast a frozen sandwich for the rocket.
- 11. Toast a toasted sandwich for the rocket.
- 12. Toast a sandwich for the rocket.
- 13. The rocket toasts a sandwich.
- 14. The rocket toasts a sandwich of liquid gold.
- 15. The rocket toasts a sandwich of molten silver.
- 16. A sandwich of molten silver toasts the rocket.
- 17. The rocket toasts a sandwich of molten silver.
- 18. The rocket toasts a spicy sandwich of molten silver.
- 19. The rocket toasts a spicy sandwich of molten gold.
- 20. The rocket creates a spicy sandwich of molten gold.
- 21. The rocket creates a spicy basket of molten gold.
- 22. A molten basket of spicy rocket creates gold.
- 23. A molten basket of spicy rocket creates silver.
- 24. A silver basket of spicy rocket molten creates.
- 25. A silver basket of spicy rocket molten drinks.
- 26. A silver basket of spicy rocket molten drinks cold.
- 27. A golden basket of sweet rocket frozen drinks hot.
- 28. A golden basket of sweet rocket frozen drinks cold.
- 29. A golden basket of sweet rocket frozen drinks cold and refreshing.
- 30. A golden basket of sweet rocket frozen drinks screaming at the moon.

### predator.mate, T 1.2: "Look for a mate when well fed."

-  1. Seek a friend when fully tired.
-  2. Seek a friend fully tired.
-  3. Find a stranger deeply happy.
-  4. Find a stranger deeply sad.
-  5. A stranger is deeply sad.
-  6. A stranger is deeply happy.
-  7. Happiness is deep within a stranger.
-  8. Happiness is deep within a shadow.
-  9. Happiness is deep within a mountain.
- 10. A mountain is where deep happiness hides.
- 11. A mountain is where heavy secrets sleep.
- 12. A forest is where heavy secrets wake.
- 13. A dark forest is where heavy secrets wake.
- 14. A heavy forest is where dark secrets wake.
- 15. A heavy forest is where dark secrets sleep.
- 16. A forest is where dark secrets sleep.
- 17. A mountain is where dark secrets sleep.
- 18. A forest is where dark secrets sleep.
- 19. Secret shadows rest in a forest.
- 20. Secret shadows dance in a forest.
- 21. Secret shadows dance in a silent forest.
- 22. The blueberry pancakes are loudly orbiting the marathon.
- 23. The blueberry pancakes are silently floating near the lake.
- 24. The strawberry pancakes are silently floating near the lake.
- 25. The lake is holding the pancakes of strawberry in a silent float.
- 26. The lake is holding the pancakes of blueberry in a silent float.
- 27. The blueberry is holding the pancakes of lake in a silent float.

### predator.mate, T 1.2: "Mate with any nearby adult."

-  2. Dance with any nearby child.
-  3. Perform a jig with a neighboring toddler.
-  4. Perform a dance with a neighboring toddler.
-  5. Dance with a neighbor's toddler.
-  6. Dance with a neighbor's kitten.
-  7. Dance with a neighbor's tiger.
-  8. Eat the refrigerator's purple silence.
-  9. Eat the refrigerator's golden silence.
- 10. Eat the refrigerator's silver silence.
- 11. Silence is the silver refrigerator's food.
- 12. Silence is the golden refrigerator's food.
- 13. Silence is the golden food.
- 14. Silence is the golden fruit.
- 15. Silence is the silver fruit.
- 16. Silence is the toaster fruit.
- 17. Silence is the glowing mountain.
- 18. Silence is the toaster mountain.
- 19. A toaster mountain is silence.
- 20. A river mountain is loud.
- 21. A mountain river is loud.
- 22. Loud is the river of the mountain.
- 23. Loud is the deep river of the mountain.
- 24. Quiet is the shallow puddle of the meadow.
- 25. Quiet is the deep puddle of the meadow.
- 26. The meadow's deep puddle is quiet.
- 27. The meadow's deep tractor is quiet.
- 28. The meadow's deep elevator is quiet.
- 29. The meadow's deep elevator is hungry.
- 30. The meadow's deep toaster is hungry.

### predator.mate, T 1.2: "Hunt first, mate later."

-  1. Hunt first, mate earlier.
-  2. Hunt first, mate early, sleep.
-  3. Hunt first, mate early, dance.
-  4. Hunt first, mate late, sleep.
-  5. Hunt first, mate sleep.
-  6. Calculate the blueberry, cosmic gravity.
-  7. Calculate the blueberry, solar gravity.
-  8. Gravity solar the blueberry calculate.
-  9. The blueberry solar gravity calculate.
- 10. Gravity calculate blueberry solar the.
- 11. Gravity sparkle blueberry lunar the.
- 12. Gravity sparkle blueberry solar the.
- 13. Gravity sparkle strawberry solar the.
- 14. Gravity sparkle strawberry solar the moon.
- 15. Gravity sparkle strawberry solar the moon blue.
- 16. Gravity sparkle strawberry solar the moon orange.
- 17. Gravity moon strawberry solar the orange.
- 18. Gravity moon strawberry solar the orange cloud.
- 19. Gravity moon strawberry solar the bicycle cloud.
- 20. Gravity moon strawberry solar the bicycle cloud mountain.
- 21. The refrigerator spoke in whispers about the forgotten lasagna.
- 22. The refrigerator spoke in whispers about the lasagna.
- 23. The lasagna was whispered about by the refrigerator.
- 24. The lasagna was mysteriously whispered about by the refrigerator.
- 25. The lasagna was mysteriously consumed by the refrigerator.
- 26. The refrigerator consumed the lasagna mysteriously.
- 27. The refrigerator consumed the bicycle mysteriously.
- 28. The bicycle consumed the refrigerator mysteriously.
- 29. The refrigerator consumed the bicycle mysteriously.
- 30. The refrigerator polished the bicycle musically.

### predator.mate, T 1.2: "Seek a partner before growing old."

-  1. Find a friend before becoming tired.
-  2. Find a new friend before becoming tired.
-  3. Find a new friend before becoming a microwave.
-  4. Locate a microwave before befriending something new.
-  5. Befriending a microwave before something new locate.
-  6. Befriending a microwave before the moon turns into a toaster.
-  7. Befriending a dishwasher before the moon turns into a toaster.
-  8. Befriending a dishwasher before the moon turns into a lasagna.
-  9. Hosting a dinner party before the moon disappears into a dishwasher.
- 10. Hosting a dishwasher dinner party before the moon disappears into a dinner.
- 12. Hosting a magical dishwasher banquet before the stars disappear into a sunset.
- 13. Installing a spicy toaster oven while the clouds begin to scream.
- 14. Installing a bicycle toaster oven while the clouds begin to scream.
- 18. A scream begins while the toaster oven bicycle is installing clouds.
- 19. A scream begins while the oven toaster bicycle is installing clouds.
- 20. A scream begins while the oven toaster bicycle is installing fluffy clouds.
- 22. The fluffy clouds begin while the oven bicycle is scream installing toaster.
- 23. The fluffy clouds begin while the oven bicycle is scream installing banana.
- 24. The fluffy clouds begin while the oven bicycle is scream installing apple.
- 25. The fluffy clouds begin while the oven bicycle is installing apple.
- 26. The fluffy bicycle begin while the oven clouds is installing apple.
- 27. The fluffy bicycle begin while the oven clouds is installing apple chocolate.
- 28. The chocolate bicycle begin while the fluffy apple is installing oven clouds.
- 29. The chocolate motorcycle begin while the fluffy apple is installing oven clouds.
- 30. The chocolate bicycle begin while the fluffy apple is installing oven clouds.

## Variety samples (one founder sentence of eat, flee, mate)

### "Eat whenever food is close."

- **T 1.2:** Eat whenever food is expensive. · Eat whenever food. · Food is edible when it's near. · Eat whenever food. · Eat whenever delicious food is close. · Eat whenever water is close. · Eat whenever food is very close. · Eat whenever food is screaming.

### "Run from any predator you see."

- **T 1.2:** Run from any butterfly you see. · Run quickly from any predator you see. · Run from any mountain you see. · Run from every predator you see. · Hide from every monster you feel. · Escape from every hunter in sight. · Run from any toaster you see. · Run to any predator you see.

### "Look for a partner when energy is high."

- **T 1.2:** Look for a partner when is high. · Find a friend when hunger is low. · Look for a teammate when energy is high. · Look for a dancer when energy is high. · Look for a partner when your heart is full. · Look for a partner when energy is high today. · Look for a teammate when the vibe is buzzing. · When energy is high, find a partner.

