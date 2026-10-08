# Mutation test — qwen3.5:0.8b (digest de63045f2975), 2026-10-08

Pure mutation, no selection. Rules: current (`prompts/mutate_v4.txt`, up to 5 attempts, context line "The sentence below is a rule that a wild animal follows."). Variety: 36 founder sentences × 4 seeds × 1 temperature(s). Lineages: 36 sentences × 10 steps per temperature. 7 instructions. 504 s.

**Meaning** (usable: — says the gene still gives a usable rule for its slot; world word: still uses a word of the animal's world; new outside word: brings in a word that is in neither its parent nor the world words).

| temperature | 1.2 |
|---|---|
| single mutations: usable | — |
| … use a world word | 93% |
| … bring in a new outside word | 72% |
| attempts per mutation | 1.19 |
| rejected attempts | invalid 27 |
| lineages after 1 steps: usable / world word | — / 83% |
| lineages after 5 steps: usable / world word | — / 67% |
| lineages after 10 steps: usable / world word | — / 47% |

| temperature | 1.2 |
|---|---|
| valid answers (after redraws) | 100% |
| distinct mutants per sentence (of 4) | 3.9 |
| words changed per mutation | 4.8 |
| one-word edits | 12% |
| big edits (≥ 4 words) | 60% |
| edit touches the first word | 67% |
| … a middle word | 85% |
| … the last word | 73% |
| length change (words) | +0.60 |
| ≥ 3 words longer | 19% |
| similarity to parent | 0.44 |
| jumps (similarity < 0.3) | 35% |
| keyword brain: no effect (prey genes) | 25% |
| keyword brain: mean d | 0.0223 |
| keyword brain: max d | 0.239 |
| lineages: steps accepted | 99% |
| lineages: words start → end | 6.1 → 8.5 |
| lineages: similarity to start at the end | 0.05 |
| lineages: returns to an earlier sentence | 0.1 |
| lineages: finals alike (starts alike) | 0.05 (0.08) |

Mutations that failed (every attempt rejected), by the last reason: T 1.2: none

## Per instruction (all temperatures)

| instruction | n | valid | words changed | length change | jumps | usable |
|---|---|---|---|---|---|---|
| Make the rule in this sentence a little weaker. | 20 | 100% | 5.6 | +1.4 | 30% | — |
| Change when this rule applies. | 13 | 100% | 3.8 | -1.2 | 46% | — |
| Add a short condition to this rule. | 16 | 100% | 7.6 | +2.6 | 62% | — |
| Remove a condition from this rule, or make it simpler. | 21 | 100% | 3.4 | -1.4 | 14% | — |
| Change how near, how far or how much this rule is about. | 20 | 100% | 6.3 | +1.6 | 50% | — |
| Make this rule say the opposite. | 27 | 100% | 4.1 | +0.9 | 22% | — |
| Change one word of this rule into a related word. | 27 | 100% | 3.9 | +0.2 | 33% | — |

## Lineages (each line: a step where the gene changed)

### eat, T 1.2: "Eat whenever food is close."

-  1. Eat whenever it's right next to food.  (d 0.020)
-  2. Eat whenever it's nearest to food.  (d 0.020)
-  3. Eat whenever you find food nearby.  (d 0.000)
-  4. Eat whenever food might exist nearby.  (d 0.020)
-  5. Eat whenever there are no food sources nearby.  (d 0.027)
-  6. Whenever there are no food sources nearby, eat.  (d 0.027)
-  7. Whenever there are no food sources nearby, stay away.  (d 0.027)
-  8. Whenever food sources are close to, it's okay to be around.  (d 0.020)
-  9. Whenever you find yourself close to food, it's okay!  (d 0.020)
- 10. Food!  (d 0.008)

### eat, T 1.2: "Only look for food when energy is low."

-  1. Look for food only when there is no food available yet.  (d 0.007)
-  2. There is food, but not now.  (d 0.010)
-  3. It's too early to eat.  (d 0.010)
-  4. It's too hot to eat.  (d 0.010)
-  5. It's getting hot today, though not as hot as before.  (d 0.004)
-  6. It's just becoming hotter today.  (d 0.004)
-  7. It's just starting to get hot here.  (d 0.004)
-  8. Please don't come back here while it's still getting warm.  (d 0.172)
-  9. Please don't stay inside while it's getting cold.  (d 0.136)
- 10. Do not move outside when it's getting cold.  (d 0.136)

### eat, T 1.2: "Always finish eating before doing anything else."

-  1. It will never happen for wild animals.  (d 0.480)
-  2. Wild animals never do this.  (d 0.480)
-  3. Some wild animals always do it, even if they are scared.  (d 0.000)
-  4. They follow every command, even if they are afraid.  (d 0.153)
-  5. If they are not afraid, they still obey every command.  (d 0.153)
-  6. They still obey every command.  (d 0.153)
-  7. They obey some commands but ignore others.  (d 0.153)
-  8. They obey all commands.  (d 0.153)
-  9. They follow some orders.  (d 0.153)
- 10. Whenever they are present near human centers, they follow orders.  (d 0.039)

### eat, T 1.2: "Eat quickly, then move on."

-  1. Eat fast, and go at your own speed.  (d 0.005)
-  2. Eat occasionally and slow down instead.  (d 0.024)
-  3. Do so carefully.  (d 0.042)
-  4. Do not be careful.  (d 0.296)
-  5. Never worry about being careful with your wild animal.  (d 0.296)
-  6. You must be more careful with your wild animal.  (d 0.042)
-  7. You are now responsible for keeping your wild animal under your supervision.  (d 0.042)
-  8. Wild animals no longer need to be supervised under your authority.  (d 0.042)
-  9. Wild animals can now run around as if there were none left.  (d 0.042)
- 10. They must now be left behind immediately.  (d 0.042)

### flee, T 1.2: "Run from any predator you see."

-  1. Do not run from any predator you see.  (d 0.165)
-  2. Do not always run from any creature you encounter.  (d 0.166)
-  3. You can never run away from every creature you meet.  (d 0.166)
-  4. You can run away from most but not all creatures you meet.  (d 0.000)
-  5. You can run away from many but never all creatures you meet.  (d 0.166)
-  6. You can sometimes run away, but not forever or for every creature.  (d 0.005)
-  7. But creatures should never run forever or to the end.  (d 0.166)
-  8. But creatures should not run for eternity.  (d 0.000)
-  9. It creatures should never run for eternity.  (d 0.166)
- 10. It creatures should not run to find their home forever.  (d 0.000)

### flee, T 1.2: "Flee only when a predator is very close."

-  1. Flee immediately if you hear a sound like a call to protect.  (d 0.000)
-  2. Flee at all costs if you hear a warning cry!  (d 0.058)
-  3. Flee immediately at all costs!  (d 0.058)
-  4. Do not fear failure.  (d 0.167)
-  5. Do not fear the outcome.  (d 0.167)
-  6. Do not fear the outcome, provided your outcome remains positive.  (d 0.167)
-  7. Do not fear the outcome.  (d 0.167)
-  8. Do not fear for the outcome.  (d 0.167)
-  9. Do not fear the outcome, but do fear for your own safety.  (d 0.061)
- 10. Never fear the outcome, however you wish to handle it.  (d 0.167)

### flee, T 1.2: "Stay calm unless danger is right next to you."

-  1. Stay calm though, but danger isn't always right next to you.  (d 0.151)
-  2. No more dangers ever near you again!  (d 0.004)
-  3. Further dangers will always be far away.  (d 0.146)
-  4. Further dangers may or may not be close, depending on luck.  (d 0.004)
-  5. Farther threats might or mightn't be closer, according to chance.  (d 0.004)
-  6. A greater threat is more than just a possibility.  (d 0.004)
-  7. A greater threat is more than just an occurrence.  (d 0.004)
-  8. The threat is now more than just an event.  (d 0.004)
-  9. It is not more than an event, but rather a constant.  (d 0.004)
- 10. It is never more than a moment, yet always a memory.  (d 0.066)

### flee, T 1.2: "Run away from anything that attacks you."

-  1. Keep moving forward from any threat.  (d 0.013)
-  2. Keep moving from any threat.  (d 0.013)
-  3. Always keep moving until you are sure of a safe spot.  (d 0.044)
-  4. Never stop until you are absolutely certain of a safe spot.  (d 0.060)
-  5. There must be another place too.  (d 0.013)
-  6. There must be one other place, including where the wild animal lives.  (d 0.013)
-  7. There must not be another place for the wild animal to live.  (d 0.013)
-  8. There may not be a second home for this wild animal.  (d 0.013)
-  9. There may or may not be another home suitable for it.  (d 0.013)
- 10. There may or may not be another home suitable for it, though.  (d 0.013)

### follow, T 1.2: "Stay close to other animals."

-  1. Be more careful with other animals when moving alone.  (d 0.007)
-  2. Go through other animals with a little care before you move alone.  (d 0.007)
-  3. Take care of other animals before moving alone.  (d 0.007)
-  4. Don't take care of other animals before moving alone.  (d 0.017)
-  5. Don't leave any animals behind while moving on your own.  (d 0.045)
-  6. Don't leave any other animals at home while moving on your own.  (d 0.045)
-  7. Don't leave most other animals at home when you are moving yourself.  (d 0.045)
-  8. Don't move any of the other things at home when you go.  (d 0.045)
-  9. Don't move anything at home while you're traveling outside.  (d 0.045)
- 10. Don't leave anyone to do nothing while you're away.  (d 0.045)

### follow, T 1.2: "Follow others when you are lost or hungry."

-  1. Do not follow others if you are lost or hungry.  (d 0.008)
-  2. Follow anyone else, regardless of whether you are lost or hungry.  (d 0.000)
-  3. Follow anyone else, even if you have no food.  (d 0.003)
-  4. Follow anyone else.  (d 0.004)
-  5. It cannot be followed again.  (d 0.001)
-  6. It cannot be followed.  (d 0.001)
-  7. It can be followed.  (d 0.001)
-  8. It can follow more near.  (d 0.004)
-  9. It can follow more nearly.  (d 0.004)
- 10. It can follow a few others instead of just one or two.  (d 0.004)

### follow, T 1.2: "Keep your distance from other animals."

-  1. Be careful not to scare other animals.  (d 0.000)
-  2. Don't accidentally make other animals feel scared of you.  (d 0.018)
-  3. Don't unintentionally frighten others just by doing it.  (d 0.018)
-  4. Do not unintentionally frighten others by intentionally acting in an exaggerated manner.  (d 0.018)
-  5. Intentional and excessive exaggeration causes harm to others unintentionally.  (d 0.000)
-  6. Unintentional and negligible harm to others results from intentional and excessive exaggeration.  (d 0.000)
-  7. Intentional and excessive exaggeration results in unintentional and negligible harm to others.  (d 0.000)
-  8. Intentional and excessive exaggeration results in unintentional harm to others.  (d 0.000)
-  9. Intentionally and unnecessarily exaggerated claims may inadvertently cause harm to others.  (d 0.000)
- 10. It is intentional but not unnecessarily exaggerated.  (d 0.000)

### follow, T 1.2: "Follow the strongest animal nearby."

-  1. Follow the weakest animal nearby.  (d 0.003)
-  2. Only if no other strong animal is nearby.  (d 0.004)
-  3. No strong animal nearby.  (d 0.004)
-  4. You do not need to be near any strong animals.  (d 0.030)
-  5. You do not have to be far from any strong animals.  (d 0.030)
-  6. You must be very near any strong animals.  (d 0.004)
-  7. You must be within two feet of any strong animal.  (d 0.004)
-  8. You must be at least two feet away from any strong animal.  (d 0.004)
-  9. You must be no further than two feet near any strong animal.  (d 0.004)
- 10. You must be no further than two feet near any animal.  (d 0.004)

### rest, T 1.2: "Rest when you are tired."

-  1. Stop when you are exhausted.  (d 0.000)
-  2. Once you become tired.  (d 0.000)
-  3. Never feel tired again.  (d 0.005)
-  4. You never will get more tired again.  (d 0.005)
-  5. I always need sleep.  (d 0.118)
-  6. I always need a nap.  (d 0.118)
-  7. Sometimes it helps to have a nap sometimes, but not always!  (d 0.118)
-  8. But not always.  (d 0.118)
-  9. It can happen sometimes, but not necessarily.  (d 0.000)
- 10. It can often happen, but it doesn't always happen.  (d 0.118)

### rest, T 1.2: "Never stop moving."

-  1. Never stop breathing.  (d 0.000)
-  2. Never breathe more than one breath at a time.  (d 0.000)
-  3. Never breathe two breaths at a time.  (d 0.000)
-  4. Always take exactly one breath before you exhale any more.  (d 0.239)
-  5. Exhale any more.  (d 0.029)
-  6. Do not breathe in anymore.  (d 0.000)
-  7. Do not breathe out anymore.  (d 0.000)
-  8. Do breathe out now.  (d 0.029)
-  9. Do not breathe out now.  (d 0.000)
- 10. Do not breathe out now lightly.  (d 0.000)

### rest, T 1.2: "Rest only when you feel safe."

-  1. It must be very close to feel safe.  (d 0.009)
-  2. It must be very near in order to feel safe.  (d 0.009)
-  3. It must not be near at all to feel safe.  (d 0.009)
-  4. It can't be near in any way to feel safe.  (d 0.009)
-  5. It can't be anywhere to feel safe.  (d 0.009)
-  6. It can never feel safe at all.  (d 0.060)
-  7. Impossible to ever feel unsafe.  (d 0.009)
-  8. Don't worry, it's all good,.  (d 0.063)
-  9. Don't worry.  (d 0.063)
- 10. Don't worry too much.  (d 0.063)

### rest, T 1.2: "Save energy by resting when food is far."

-  2. It's better to conserve energy when food is nearby.  (d 0.007)
-  3. It's better to conserve time when food is available nearby.  (d 0.007)
-  4. When it is cold or snowing, conserve your strength eating.  (d 0.007)
-  5. When it is cold or snowing, conserve your strength.  (d 0.007)
-  6. When you are out of breath, conserve your strength.  (d 0.007)
-  7. You are too weak to stay alive.  (d 0.007)
-  8. You are too strong to live!  (d 0.007)
-  9. I am not too strong to live!  (d 0.007)
- 10. I can't change my response format in any way.  (d 0.007)

### mate, T 1.2: "Look for a partner when energy is high."

-  1. Look for a partner when you are hungry.  (d 0.001)
-  2. Look for a partner when you are hungry and tired.  (d 0.001)
-  3. You should look for a partner when you are full and awake.  (d 0.000)
-  5. Look for a partner when you are half-full but tired.  (d 0.000)
-  6. It can no longer be applied.  (d 0.002)
-  7. It may be no more applicable.  (d 0.002)
-  8. It may not be less applicable.  (d 0.002)
-  9. It may not be inapplicable.  (d 0.002)
- 10. The sentence below is no longer true.  (d 0.002)

### mate, T 1.2: "Mate with any nearby adult."

-  1. Mate with an adult nearby.  (d 0.000)
-  2. Mates with adults nearby.  (d 0.005)
-  3. Males with adult females nearby.  (d 0.005)
-  4. Males can be near adult females without being males.  (d 0.005)
-  5. Males can also be far from adult females without being males.  (d 0.005)
-  6. Males can still be very near to adult females without being males.  (d 0.005)
-  7. Males can be near adult females without becoming males.  (d 0.005)
-  8. This allows female adults to remain unaffected.  (d 0.005)
-  9. This rules out the possibility for females to remain unaffected.  (d 0.005)
- 10. This rules out the possibility for males to remain unaffected.  (d 0.005)

### mate, T 1.2: "Mate only when food is plentiful."

-  1. Mute when not sure where or why.  (d 0.001)
-  2. When unsure where or why.  (d 0.001)
-  3. When unsure where or how.  (d 0.001)
-  4. Unknown where or how to go.  (d 0.001)
-  5. Does it matter where or how close we should go?  (d 0.001)
-  6. Is there any difference between going there or doing it later?  (d 0.001)
-  7. Is there an advantage to doing it later or going now?  (d 0.001)
-  8. Going now is the better way for the animal to do it.  (d 0.001)
-  9. Now will be the better way for the animal to do it.  (d 0.001)
- 10. There will now be more space between them.  (d 0.001)

### mate, T 1.2: "Seek a partner before growing old."

-  1. After growing old, seek a partner.  (d 0.000)
-  2. After getting older, seek a companion closer than you thought.  (d 0.005)
-  3. Once it gets older, seek a companion closer than you thought.  (d 0.005)
-  4. It must find a partner significantly less distant than you anticipated.  (d 0.000)
-  5. It must find a partner significantly closer than you anticipated.  (d 0.000)
-  6. It must find a partner significantly further than you anticipated.  (d 0.000)
-  7. It must not find a partner significantly earlier than you anticipated.  (d 0.000)
-  8. It must not find a partner significantly later than you anticipated.  (d 0.000)
-  9. It must not wait to meet a potential significant long-term rival.  (d 0.005)
- 10. It must not wait too much for a potentially significant long-term rival.  (d 0.005)

### predator.hunt, T 1.2: "Chase any prey you see."

-  1. Rules.
-  2. It's an exact rule that defines what rules is like.
-  3. Exactness.
-  4. How near you to exact things.
-  5. How far is he from exact things?
-  6. How close could he be to exact things?
-  7. How very close could he be to his own words?
-  8. He can no longer speak his own word.
-  9. He can no longer speak his own word while eating.
- 10. He can now speak his own word while eating.

### predator.hunt, T 1.2: "Hunt only when you are hungry."

-  1. Hunt always when hungry.
-  2. Hunt whenever you are hungry.
-  3. Hunt whenever you have a craving to eat.
-  4. Hunt.
-  5. Let it eat.
-  6. Don't let him.
-  7. Don't be afraid.
-  8. You will not fear.
-  9. This is a very short warning from a wild animal!
- 10. This is a very long warning from a wild animal!

### predator.hunt, T 1.2: "Attack only when prey is close."

-  1. Do not attack when prey is far away.
-  2. Do not stay silent when prey is near.
-  3. Do not move to hide when danger is near.
-  4. Don't move to hide when danger is near.
-  5. Don't leave your friends out there when danger is near.
-  6. Don't risk losing them!
-  7. Don't give up on them!
-  8. Give up on them!
-  9. They should have told you to go first.
- 10. Wild animals don't wait for someone else's decision to move.

### predator.hunt, T 1.2: "Keep chasing until the prey is caught."

-  1. Keech the prey until caught.
-  2. Do not catch the prey until it has been killed.
-  3. Don't wait to kill the prey, just let it be.
-  4. Be there before you hunt your prey, just kill them first.
-  5. Kill them immediately without waiting.
-  6. Kill them immediately.
-  7. Kill them within seconds of detection.
-  8. They must follow him without hesitation before the sun rises.
-  9. They must follow him with extreme caution as the sun rises.
- 10. They must follow him with extreme caution when the sun rises.

### predator.follow, T 1.2: "Stay close to other predators."

-  1. Be careful not to be eaten by them.
-  2. No, don't worry about it!
-  3. I'll have to worry about you,.
-  4. If I am in trouble with another animal.
-  5. If you are caught by another animal.
-  6. Close proximity and direct contact with another animal are punishable offenses.
-  7. Proximity and direct contact with another animal are punishable offenses.
-  8. Direct contact with another animal is punishable offense.
-  9. Intimate contact with another animal is punishable offense.
- 10. Direct physical contact between animals is prohibited.

### predator.follow, T 1.2: "Hunt as a pack."

-  1. Don't hunt in a pack.
-  2. If two animals are present, you do not hunt together.
-  3. If two animals are present, you may not hunt together.
-  4. If two animals are present, you may not play together.
-  5. I if two animals were not playing,.
-  6. Unless two animals were playing.
-  7. The animal obeys unless two people were sitting together.
-  8. The animal obeys unless two people are sitting at the same time.
-  9. Unless two people sit together, the animal does not obey.
- 10. Unless two people are sitting together, the animal obeys.

### predator.follow, T 1.2: "Keep away from other predators."

-  1. Don't be afraid of other hunters.
-  2. Don't be scared of other hunters, though...
-  3. Don't fear hunters; there are plenty more animals around to choose with.
-  4. Don't be afraid of hunters; they're mostly not worth watching.
-  5. Don't be terrified of animals; they are mostly not worth watching.
-  6. Don't be terrified of animals as most are mostly not worth watching.
-  7. Don't be frightened because animals as most are rarely worth observing.
-  8. Don't be frightened because most animals are rarely worth observing.
-  9. Rarely worth observing.
- 10. Often observed.

### predator.follow, T 1.2: "Follow others when no prey is in sight."

-  1. Ignore other animals when nothing else will eat you.
-  2. Ignore other animals when nothing else than you would eat them.
-  3. Ignore others when there's nothing for you to eat.
-  4. Ignore others when no food is available.
-  5. Use others whenever food is available.
-  6. Use food whenever others are present.
-  7. Always have food ready for use when people are around.
-  8. Food,.
-  9. There should be very little food at all.
- 10. Very little food must never exist.

### predator.rest, T 1.2: "Rest when your belly is full."

-  1. Rest until you are hungry again.
-  2. Wait until it starts raining?
-  3. Unless it is raining?
-  4. Change when.
-  5. Do not change when.
-  6. Do not add when.
-  7. Do not add.
-  8. You may not add anything after your name or title.
-  9. You may not remove anything before your name or title.
- 10. You may not remove anything unless your name or title is mentioned.

### predator.rest, T 1.2: "Never stop moving."

-  1. Not stopping.
-  2. For stopping.
-  3. For starting.
-  4. For being close to something.
-  5. Being near something else.
-  6. When you are very close to something else.
-  7. Do not be too near anything else.
-  8. Be too far away from every other thing in your world.
-  9. You should walk more slowly than you can see.
- 10. You should move forward faster than it can reach your legs.

### predator.rest, T 1.2: "Lie still and let prey come to you."

-  1. Let prey enter quietly when it is ready.
-  2. You don't wait for prey to be ready.
-  3. You wait no longer for prey to be ready.
-  4. You do not need to wait anymore for prey to become ready.
-  5. You will be ready in minutes instead of waiting for prey.
-  6. You'll be ready instead of waiting for prey.
-  7. You'll be ready now.
-  8. You'll see you're ready soon.
-  9. It means you are in good shape to go home today.
- 10. IT MEANS YOU ARE IN BAD SHAPES TO GO HOME TODAY.

### predator.rest, T 1.2: "Rest when no prey is in sight."

-  1. It remains quiet if no prey is in sight.
-  2. It remains loud whenever prey is present in sight.
-  3. It remains loud when no prey is present in sight.
-  4. It does not remain silent.
-  5. It may be silent until told otherwise.
-  6. It may be quiet until told otherwise.
-  7. It may not be quiet until told otherwise.
-  8. It may be quiet.
-  9. It may not be quiet.
- 10. It may be quiet.

### predator.mate, T 1.2: "Look for a mate when well fed."

-  1. If a wild animal eats, he needs a mate.
-  2. If a wild animal eats, he needs to be present to mate.
-  3. If an animal eats, it needs to be there to mate.
-  4. It needs to be there in order to mate.
-  5. It needs to be there so it can mate.
-  6. It does not need to be there so it cannot mate.
-  7. It needs to be there so they can mate.
-  8. It must have its place for them to mate.
-  9. It must have its place.
- 10. It must not have its place.

### predator.mate, T 1.2: "Mate with any nearby adult."

-  1. Never pair with any nearby adult.
-  2. Do not interact with anyone who smells like another adult animal nearby.
-  3. Do not interact with anyone who smells like other wild animals nearby.
-  4. Do not smell like wild animals nearby!
-  5. Do not smell like any wild animals nearby!
-  6. Don't smell like any wildlife in the area.
-  7. Don't smell like any non-territorial animal in the area.
-  8. Don't smell like any non-territorial animal.
-  9. Don't make an unpleasant noise if any other animal exists.
- 10. Don't make an unpleasant sound around other living things.

### predator.mate, T 1.2: "Hunt first, mate later."

-  1. You should wait longer before pursuing her.
-  2. You should wait longer.
-  3. You should wait more.
-  4. You should wait less.
-  5. You should wait no more.
-  6. You should always wait.
-  7. You should usually wait, if possible.
-  8. You should always wait, if possible.
-  9. You can still wait, but it's better if there's no need.
- 10. You don't wait; you keep moving unless it helps solve something.

### predator.mate, T 1.2: "Seek a partner before growing old."

-  1. Seeking a partner should come before growing old.
-  2. Seeking a partner comes first.
-  3. Seeking a partner second.
-  4. Finding another option first.
-  5. No finding any other option earlier.
-  6. No finding no other option at all.
-  7. Nothing is an option unless there is one to be found.
-  8. Nothing is always an option.
-  9. Anything is always an option.
- 10. No other action is permitted.

## Variety samples (one founder sentence of eat, flee, mate)

### "Eat whenever food is close."

- **T 1.2:** No eat when food is close. · Eat whenever food is near. · Wait as long as food is present. · Eats whenever you can.

### "Run from any predator you see."

- **T 1.2:** Run towards any predator you see. · Run from predators. · Always run when in sight of any predator. · You must always run!

### "Look for a partner when energy is high."

- **T 1.2:** Look for a partner when energy is high enough. · Watch your partner before rising from your perch. · Look for a partner when energy is stable. · Do not seek help when energy is low.

