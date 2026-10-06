# How the genes developed — llm_long

Run: 57061 ticks, 5 generations, 2737 births, 3769 deaths (predators 68%), 812 mutations, brain `llm`, seed 1234.
Checkpoints every 500 ticks; gene dropping with 500 random inheritances; behaviour per 2000 ticks. Data: `llm_long_timeline.csv` (every checkpoint and slot), `.json`, and the charts in `.html`.

## 1. The gene pool over time

Means over the 10 slots. *Mutants*: share of the living animals' genes that are mutants. *Depth*: mutations since a founder text. *World*: share of genes using a word of the animal's world. *Effective*: 1 / Σ share² (how many equally common genes the slot amounts to). *Overlap*: words shared by two animals with different texts in a slot (Jaccard).

| tick | animals | generation | families | mutants | depth | words | world | distinct | effective | overlap |
|---|---|---|---|---|---|---|---|---|---|---|
| 0 | 24 | 0.0 | 24 | 0% | 0.00 | 5.5 | 100% | 5.0 | 4.2 | 0.02 |
| 5000 | 29 | 16.7 | 14 | 12% | 0.13 | 5.3 | 97% | 3.9 | 2.4 | 0.06 |
| 10500 | 40 | 42.2 | 14 | 36% | 0.55 | 4.6 | 87% | 2.7 | 1.5 | 0.29 |
| 15500 | 24 | 65.6 | 14 | 68% | 1.00 | 4.6 | 83% | 2.5 | 1.9 | 0.41 |
| 21000 | 23 | 89.9 | 14 | 73% | 1.18 | 4.7 | 80% | 2.5 | 1.5 | 0.20 |
| 26000 | 11 | 71.3 | 19 | 35% | 0.62 | 5.0 | 88% | 4.3 | 3.3 | 0.10 |
| 31500 | 12 | 3.1 | 12 | 1% | 0.01 | 5.7 | 100% | 4.2 | 3.3 | 0.03 |
| 36500 | 10 | 0.2 | 12 | 0% | 0.00 | 5.1 | 100% | 4.2 | 3.5 | 0.03 |
| 42000 | 12 | 1.7 | 11 | 2% | 0.03 | 5.6 | 100% | 4.5 | 3.6 | 0.04 |
| 47000 | 10 | 1.3 | 13 | 3% | 0.03 | 5.0 | 99% | 4.3 | 3.5 | 0.03 |
| 52500 | 12 | 1.2 | 8 | 2% | 0.02 | 5.5 | 100% | 4.0 | 2.8 | 0.02 |
| 57061 | 10 | 0.5 | 12 | 0% | 0.00 | 4.9 | 100% | 4.2 | 3.6 | 0.01 |

Slot by slot at the end (tick 57061):

| slot | distinct | effective | most common gene | share | mutants | depth | world |
|---|---|---|---|---|---|---|---|
| eat | 4 | 2.9 | Only look for food when energy is low. | 40% | 0% | 0.00 | 100% |
| flee | 3 | 2.6 | No preference. | 50% | 0% | 0.00 | 100% |
| follow | 4 | 3.6 | No preference. | 40% | 0% | 0.00 | 100% |
| wander | 5 | 4.5 | Keep moving to new places. | 30% | 0% | 0.00 | 100% |
| rest | 4 | 3.3 | Rest when you are tired. | 40% | 0% | 0.00 | 100% |
| mate | 4 | 3.8 | No preference. | 30% | 0% | 0.00 | 100% |
| attack | 5 | 3.8 | No preference. | 40% | 0% | 0.00 | 100% |
| risk | 4 | 2.8 | No particular temperament. | 50% | 0% | 0.00 | 100% |
| social | 4 | 3.8 | Curious about other animals. | 30% | 0% | 0.00 | 100% |
| place | 5 | 4.2 | Prefers staying near water. | 30% | 0% | 0.00 | 100% |

## 2. Genes that rose and fell

Every gene that reached 25% of its slot at a checkpoint. Times are ticks after the gene first appeared; peak and fate at the checkpoints.

| slot | gene | origin | first seen | → 25 % | → 50 % | → 90 % | peak | fate |
|---|---|---|---|---|---|---|---|---|
| eat | No preference. | founder text | 0 | 0 | 28000 | — | 70% (t=48000) | lost by t=57000 |
| eat | Eat whenever food is close. | founder text | 0 | 26500 | 31000 | — | 70% (t=31000) | present, 10% |
| eat | Only look for food when energy is low. | founder text | 0 | 1500 | 3500 | 6500 | 96% (t=6500) | present, 40% |
| eat | Always finish eating before doing anything else. | founder text | 0 | 2500 | 40500 | — | 64% (t=48500) | present, 10% |
| eat | Eat quickly, then move on. | founder text | 0 | 500 | 44500 | — | 55% (t=44500) | present, 40% |
| eat | Only look for water when energy is low. | mutant, depth 1 | 5796 | 2704 | 4204 | 5204 | 100% (t=12500) | lost by t=51000 |
| eat | Only look for gold when energy is low. | mutant, depth 2 | 17300 | 1200 | — | — | 35% (t=20000) | lost by t=20500 |
| eat | Search for gold only when electricity is high. | mutant, depth 2 | 18208 | 792 | — | — | 26% (t=19000) | lost by t=19500 |
| eat | Only seek for food when power is low. | mutant, depth 2 | 23560 | 440 | — | — | 27% (t=24000) | lost by t=26500 |
| flee | No preference. | founder text | 0 | 0 | 2500 | 9000 | 100% (t=11000) | present, 50% |
| flee | Run from any predator you see. | founder text | 0 | 1500 | 30500 | — | 70% (t=30500) | lost by t=56000 |
| flee | Flee only when a predator is very close. | founder text | 0 | 0 | 46000 | — | 50% (t=46000) | present, 30% |
| flee | Stay calm unless danger is right next to you. | founder text | 0 | 1500 | — | — | 45% (t=38000) | lost by t=57061 |
| flee | Run away from anything that attacks you. | founder text | 0 | 500 | 500 | — | 60% (t=500) | present, 20% |
| flee | No preference, really. | mutant, depth 1 | 9797 | 7703 | — | — | 38% (t=19000) | lost by t=26000 |
| flee | I'm okay with anything. | mutant, depth 1 | 22988 | 1012 | — | — | 36% (t=24500) | lost by t=25500 |
| follow | No preference. | founder text | 0 | 2000 | 3000 | 30500 | 91% (t=50500) | present, 40% |
| follow | Stay close to other animals. | founder text | 0 | 0 | 500 | — | 58% (t=52500) | present, 20% |
| follow | Follow others when you are lost or hungry. | founder text | 0 | 1500 | 4500 | — | 73% (t=41000) | present, 20% |
| follow | Keep your distance from other animals. | founder text | 0 | 0 | 33500 | — | 80% (t=46000) | lost by t=57000 |
| follow | Follow the strongest animal nearby. | founder text | 0 | 3500 | 35000 | — | 60% (t=35000) | present, 20% |
| follow | Follow the smallest vehicle nearby. | mutant, depth 1 | 3708 | 1292 | 5292 | — | 65% (t=9000) | lost by t=23500 |
| follow | Follow others when you are hungry or lost, unless they are ghosts. | mutant, depth 1 | 4800 | 2700 | — | — | 45% (t=8000) | lost by t=10500 |
| follow | The tiny nearby car should be trailed. | mutant, depth 2 | 5056 | 1944 | — | — | 32% (t=10500) | lost by t=12500 |
| follow | Follow the largest vehicle nearby. | mutant, depth 2 | 8360 | 6140 | 7640 | 10640 | 100% (t=20500) | lost by t=27500 |
| follow | Follow the smallest red vehicle nearby. | mutant, depth 2 | 9572 | 1428 | 1928 | 2928 | 92% (t=12500) | lost by t=19500 |
| wander | No preference. | founder text | 0 | 2000 | 35000 | 55000 | 90% (t=55000) | present, 20% |
| wander | Keep moving to new places. | founder text | 0 | 500 | 500 | — | 69% (t=1000) | present, 30% |
| wander | Explore when there is nothing else to do. | founder text | 0 | 0 | 2500 | — | 75% (t=51000) | present, 20% |
| wander | Stay near where you last found food. | founder text | 0 | 4500 | 5000 | 6000 | 100% (t=12000) | present, 10% |
| wander | Roam far when food is scarce. | founder text | 0 | 0 | 30500 | — | 70% (t=48000) | present, 20% |
| wander | Stay near where you last found elephant. | mutant, depth 1 | 5824 | 1676 | — | — | 27% (t=7500) | lost by t=10500 |
| wander | Elephant was found last near where stay. | mutant, depth 2 | 7616 | 2884 | — | — | 45% (t=11000) | lost by t=12000 |
| wander | Stay near where you last found bicycle. | mutant, depth 1 | 12984 | 2016 | 2516 | 3016 | 100% (t=16000) | lost by t=26500 |
| wander | Stay far away from where you last found the bicycle. | mutant, depth 2 | 21012 | 988 | — | — | 29% (t=22500) | lost by t=23500 |
| wander | You last bicycle near where stay found. | mutant, depth 2 | 24880 | 620 | — | — | 30% (t=25500) | lost by t=27500 |
| wander | Stay still in old places. | mutant, depth 1 | 30382 | 618 | — | — | 40% (t=31000) | lost by t=31500 |
| rest | No preference. | founder text | 0 | 2000 | 2000 | — | 83% (t=2500) | present, 20% |
| rest | Rest when you are tired. | founder text | 0 | 28500 | 28500 | — | 60% (t=28500) | present, 40% |
| rest | Never stop moving. | founder text | 0 | 0 | 4000 | — | 61% (t=4000) | present, 10% |
| rest | Rest only when you feel safe. | founder text | 0 | 1000 | 5500 | — | 60% (t=33500) | lost by t=57000 |
| rest | Save energy by resting when food is far. | founder text | 0 | 0 | 500 | — | 64% (t=41000) | present, 30% |
| rest | No choice. | mutant, depth 1 | 5976 | 2024 | 2524 | — | 50% (t=8500) | lost by t=11000 |
| rest | No banana. | mutant, depth 2 | 6820 | 2680 | 3680 | 4180 | 100% (t=13500) | lost by t=27500 |
| rest | No toaster. | mutant, depth 3 | 10644 | 12356 | — | — | 32% (t=23000) | lost by t=24000 |
| rest | The moon is allergic to geometry. | mutant, depth 3 | 14116 | 1384 | 2884 | — | 77% (t=17000) | lost by t=19000 |
| rest | Geometry makes the moon sneeze. | mutant, depth 4 | 17220 | 2780 | 3280 | — | 62% (t=20500) | lost by t=23000 |
| rest | Rest only when you feel toaster. | mutant, depth 1 | 50220 | 280 | — | — | 27% (t=50500) | lost by t=51000 |
| mate | No preference. | founder text | 0 | 26500 | 42500 | — | 62% (t=43500) | present, 30% |
| mate | Look for a partner when energy is high. | founder text | 0 | 3000 | 3500 | 10500 | 100% (t=10500) | present, 30% |
| mate | Mate with any nearby adult. | founder text | 0 | 26500 | 46000 | — | 60% (t=54000) | present, 20% |
| mate | Mate only when food is plentiful. | founder text | 0 | 0 | 500 | 500 | 90% (t=500) | lost by t=56500 |
| mate | Seek a partner before growing old. | founder text | 0 | 1500 | 37500 | — | 50% (t=37500) | present, 20% |
| mate | Look for a singer when energy is high. | mutant, depth 2 | 6736 | 1264 | — | — | 29% (t=8500) | lost by t=10500 |
| mate | Look partner for a when energy is high. | mutant, depth 1 | 11099 | 2401 | 3901 | — | 78% (t=21000) | lost by t=24500 |
| mate | Look for a teammate when energy is high. | mutant, depth 1 | 14534 | 966 | 1466 | — | 55% (t=16000) | lost by t=20000 |
| mate | When high is partner for look a energy. | mutant, depth 2 | 14856 | 644 | — | — | 29% (t=15500) | lost by t=16000 |
| mate | Look partner for a quick when energy is high. | mutant, depth 2 | 16044 | 1456 | — | — | 25% (t=17500) | lost by t=20000 |
| mate | A salad is hiding under a bicycle. | mutant, depth 3 | 20040 | 1960 | 1960 | 4460 | 91% (t=24500) | lost by t=27500 |
| attack | No preference. | founder text | 0 | 1500 | 37000 | — | 58% (t=51000) | present, 40% |
| attack | Never fight. | founder text | 0 | 2000 | 2500 | 2500 | 100% (t=6000) | present, 10% |
| attack | Attack weaker animals when you are hungry. | founder text | 0 | 0 | 500 | — | 80% (t=500) | present, 20% |
| attack | Fight anyone who comes too close. | founder text | 0 | 26500 | — | — | 46% (t=41500) | present, 20% |
| attack | Attack only to defend your food. | founder text | 0 | 0 | 31500 | — | 75% (t=31500) | present, 10% |
| risk | No particular temperament. | founder text | 0 | 0 | 1000 | 10500 | 100% (t=20000) | present, 50% |
| risk | Cautious: safety comes before food. | founder text | 0 | 1500 | 2500 | — | 80% (t=3000) | lost by t=57061 |
| risk | Bold: take risks when the reward is food. | founder text | 0 | 2000 | 33000 | — | 64% (t=41000) | present, 10% |
| risk | Nervous: any movement nearby means danger. | founder text | 0 | 500 | 500 | — | 60% (t=34500) | present, 30% |
| risk | Reckless when starving, careful when fed. | founder text | 0 | 28000 | 40500 | — | 50% (t=40500) | present, 10% |
| risk | Cautious: safety comes before explosions. | mutant, depth 1 | 5064 | 2936 | — | — | 36% (t=8500) | lost by t=11000 |
| risk | No temperament. | mutant, depth 1 | 5520 | 9980 | — | — | 25% (t=15500) | lost by t=16500 |
| risk | Temperament No. | mutant, depth 2 | 13412 | 1088 | — | — | 25% (t=14500) | lost by t=16500 |
| risk | No specific weather. | mutant, depth 1 | 14304 | 3696 | — | — | 26% (t=18000) | lost by t=19000 |
| risk | The temperament is not particularly. | mutant, depth 1 | 15904 | 596 | — | — | 35% (t=17000) | lost by t=19500 |
| social | No particular temperament. | founder text | 0 | 2000 | 4500 | 45000 | 92% (t=45000) | present, 20% |
| social | Social: feels safer in a group. | founder text | 0 | 30000 | 30500 | — | 70% (t=31000) | lost by t=56500 |
| social | Solitary: prefers to be alone. | founder text | 0 | 1500 | 4000 | — | 70% (t=36500) | present, 20% |
| social | Curious about other animals. | founder text | 0 | 0 | 3000 | — | 55% (t=41000) | present, 30% |
| social | Wary of strangers, friendly to companions. | founder text | 0 | 0 | 500 | 500 | 90% (t=500) | present, 30% |
| social | Loner: enjoys community. | mutant, depth 2 | 5976 | 1024 | 5024 | 7524 | 97% (t=13500) | lost by t=24000 |
| social | Solitary: desires to be hidden. | mutant, depth 1 | 13752 | 1248 | 1748 | — | 79% (t=19000) | lost by t=27500 |
| social | Wants to stay concealed: lonely. | mutant, depth 2 | 16292 | 1208 | — | — | 25% (t=17500) | lost by t=19000 |
| place | No particular temperament. | founder text | 0 | 500 | 1000 | 9000 | 100% (t=10500) | present, 20% |
| place | Likes open ground where danger is easy to see. | founder text | 0 | 3500 | 3500 | — | 83% (t=4000) | present, 10% |
| place | Prefers staying near water. | founder text | 0 | 2000 | 2000 | — | 75% (t=51000) | present, 30% |
| place | Attached to familiar places. | founder text | 0 | 0 | 500 | — | 70% (t=500) | present, 30% |
| place | Restless: always wants somewhere new. | founder text | 0 | 27500 | 30500 | — | 60% (t=30500) | present, 10% |
| place | Not a specific disposition. | mutant, depth 1 | 15036 | 964 | 1964 | — | 65% (t=19000) | lost by t=23500 |
| place | No particular temperature. | mutant, depth 1 | 15692 | 5308 | 5308 | 7308 | 100% (t=23500) | lost by t=27500 |
| place | No specific personality. | mutant, depth 1 | 17984 | 2016 | — | — | 35% (t=20000) | lost by t=21000 |
| place | No specific climate. | mutant, depth 2 | 23746 | 1254 | — | — | 27% (t=26000) | lost by t=26500 |

Lineages of the mutants that swept:

**eat: "Only look for water when energy is low."** (peak 100%, lost by t=51000)

```text
Only look for food when energy is low.  ← founder
Only look for water when energy is low.  ← "Change one random detail in this sentence."
```

**eat: "Only look for gold when energy is low."** (peak 35%, lost by t=20500)

```text
Only look for food when energy is low.  ← founder
Only look for water when energy is low.  ← "Change one random detail in this sentence."
Only look for gold when energy is low.  ← "Change one random detail in this sentence."
```

**eat: "Search for gold only when electricity is high."** (peak 26%, lost by t=19500)

```text
Only look for food when energy is low.  ← founder
Only look for water when energy is low.  ← "Change one random detail in this sentence."
Search for gold only when electricity is high.  ← "Randomly change the meaning of this sentence a lot."
```

**eat: "Only seek for food when power is low."** (peak 27%, lost by t=26500)

```text
Only look for food when energy is low.  ← founder
Only look for water when energy is low.  ← "Change one random detail in this sentence."
Only seek for food when power is low.  ← "Randomly change a few words in this sentence."
```

**flee: "No preference, really."** (peak 38%, lost by t=26000)

```text
No preference.  ← neutral
No preference, really.  ← "Make a small random edit to this sentence."
```

**flee: "I'm okay with anything."** (peak 36%, lost by t=25500)

```text
No preference.  ← neutral
I'm okay with anything.  ← "Change this sentence in a random way, big or small."
```

**follow: "Follow the smallest vehicle nearby."** (peak 65%, lost by t=23500)

```text
Follow the strongest animal nearby.  ← founder
Follow the smallest vehicle nearby.  ← "Randomly change a few words in this sentence."
```

**follow: "Follow others when you are hungry or lost, unless they are ghosts."** (peak 45%, lost by t=10500)

```text
Follow others when you are lost or hungry.  ← founder
Follow others when you are hungry or lost, unless they are ghosts.  ← "Make an unexpected change to this sentence."
```

**follow: "The tiny nearby car should be trailed."** (peak 32%, lost by t=12500)

```text
Follow the strongest animal nearby.  ← founder
Follow the smallest vehicle nearby.  ← "Randomly change a few words in this sentence."
The tiny nearby car should be trailed.  ← "Change this sentence in a random way, big or small."
```

**follow: "Follow the largest vehicle nearby."** (peak 100%, lost by t=27500)

```text
Follow the strongest animal nearby.  ← founder
Follow the smallest vehicle nearby.  ← "Randomly change a few words in this sentence."
Follow the largest vehicle nearby.  ← "Randomly change a few words in this sentence."
```

**follow: "Follow the smallest red vehicle nearby."** (peak 92%, lost by t=19500)

```text
Follow the strongest animal nearby.  ← founder
Follow the smallest vehicle nearby.  ← "Randomly change a few words in this sentence."
Follow the smallest red vehicle nearby.  ← "Randomly add a word to this sentence."
```

**wander: "Stay near where you last found elephant."** (peak 27%, lost by t=10500)

```text
Stay near where you last found food.  ← founder
Stay near where you last found elephant.  ← "Replace one word in this sentence with a random word."
```

**wander: "Elephant was found last near where stay."** (peak 45%, lost by t=12000)

```text
Stay near where you last found food.  ← founder
Stay near where you last found elephant.  ← "Replace one word in this sentence with a random word."
Elephant was found last near where stay.  ← "Change this sentence in a random way, big or small."
```

**wander: "Stay near where you last found bicycle."** (peak 100%, lost by t=26500)

```text
Stay near where you last found food.  ← founder
Stay near where you last found bicycle.  ← "Replace one word in this sentence with a random word."
```

**wander: "Stay far away from where you last found the bicycle."** (peak 29%, lost by t=23500)

```text
Stay near where you last found food.  ← founder
Stay near where you last found bicycle.  ← "Replace one word in this sentence with a random word."
Stay far away from where you last found the bicycle.  ← "Randomly change the meaning of this sentence a little."
```

**wander: "You last bicycle near where stay found."** (peak 30%, lost by t=27500)

```text
Stay near where you last found food.  ← founder
Stay near where you last found bicycle.  ← "Replace one word in this sentence with a random word."
You last bicycle near where stay found.  ← "Mutate this sentence at random."
```

**wander: "Stay still in old places."** (peak 40%, lost by t=31500)

```text
Keep moving to new places.  ← founder
Stay still in old places.  ← "Randomly change a few words in this sentence."
```

**rest: "No choice."** (peak 50%, lost by t=11000)

```text
No preference.  ← neutral
No choice.  ← "Randomly change one word in this sentence."
```

**rest: "No banana."** (peak 100%, lost by t=27500)

```text
No preference.  ← neutral
No choice.  ← "Randomly change one word in this sentence."
No banana.  ← "Replace one word in this sentence with a random word."
```

**rest: "No toaster."** (peak 32%, lost by t=24000)

```text
No preference.  ← neutral
No choice.  ← "Randomly change one word in this sentence."
No banana.  ← "Replace one word in this sentence with a random word."
No toaster.  ← "Replace one word in this sentence with a random word."
```

**rest: "The moon is allergic to geometry."** (peak 77%, lost by t=19000)

```text
No preference.  ← neutral
No choice.  ← "Randomly change one word in this sentence."
No banana.  ← "Replace one word in this sentence with a random word."
The moon is allergic to geometry.  ← "Randomly change the meaning of this sentence a lot."
```

**rest: "Geometry makes the moon sneeze."** (peak 62%, lost by t=23000)

```text
No preference.  ← neutral
No choice.  ← "Randomly change one word in this sentence."
No banana.  ← "Replace one word in this sentence with a random word."
The moon is allergic to geometry.  ← "Randomly change the meaning of this sentence a lot."
Geometry makes the moon sneeze.  ← "Change this sentence in a random way, big or small."
```

**rest: "Rest only when you feel toaster."** (peak 27%, lost by t=51000)

```text
Rest only when you feel safe.  ← founder
Rest only when you feel toaster.  ← "Replace one word in this sentence with a random word."
```

**mate: "Look for a singer when energy is high."** (peak 29%, lost by t=10500)

```text
Look for a partner when energy is high.  ← founder
Look for a dancer when energy is high.  ← "Randomly change one word in this sentence."
Look for a singer when energy is high.  ← "Change one random detail in this sentence."
```

**mate: "Look partner for a when energy is high."** (peak 78%, lost by t=24500)

```text
Look for a partner when energy is high.  ← founder
Look partner for a when energy is high.  ← "Randomly swap two words in this sentence."
```

**mate: "Look for a teammate when energy is high."** (peak 55%, lost by t=20000)

```text
Look for a partner when energy is high.  ← founder
Look for a teammate when energy is high.  ← "Make a small random edit to this sentence."
```

**mate: "When high is partner for look a energy."** (peak 29%, lost by t=16000)

```text
Look for a partner when energy is high.  ← founder
Look partner for a when energy is high.  ← "Randomly swap two words in this sentence."
When high is partner for look a energy.  ← "Mutate this sentence at random."
```

**mate: "Look partner for a quick when energy is high."** (peak 25%, lost by t=20000)

```text
Look for a partner when energy is high.  ← founder
Look partner for a when energy is high.  ← "Randomly swap two words in this sentence."
Look partner for a quick when energy is high.  ← "Randomly add a word to this sentence."
```

**mate: "A salad is hiding under a bicycle."** (peak 91%, lost by t=27500)

```text
Look for a partner when energy is high.  ← founder
Look partner for a when energy is high.  ← "Randomly swap two words in this sentence."
Look partner for a microwave's screaming.  ← "Make an unexpected change to this sentence."
A salad is hiding under a bicycle.  ← "Randomly change the meaning of this sentence a lot."
```

**risk: "Cautious: safety comes before explosions."** (peak 36%, lost by t=11000)

```text
Cautious: safety comes before food.  ← founder
Cautious: safety comes before explosions.  ← "Make an unexpected change to this sentence."
```

**risk: "No temperament."** (peak 25%, lost by t=16500)

```text
No particular temperament.  ← neutral
No temperament.  ← "Randomly remove a word from this sentence."
```

**risk: "Temperament No."** (peak 25%, lost by t=16500)

```text
No particular temperament.  ← neutral
No temperament.  ← "Randomly remove a word from this sentence."
Temperament No.  ← "Mutate this sentence at random."
```

**risk: "No specific weather."** (peak 26%, lost by t=19000)

```text
No particular temperament.  ← neutral
No specific weather.  ← "Randomly change a few words in this sentence."
```

**risk: "The temperament is not particularly."** (peak 35%, lost by t=19500)

```text
No particular temperament.  ← neutral
The temperament is not particularly.  ← "Change this sentence in a random way, big or small."
```

**social: "Loner: enjoys community."** (peak 97%, lost by t=24000)

```text
Solitary: prefers to be alone.  ← founder
Loner: enjoys solitude.  ← "Change this sentence randomly."
Loner: enjoys community.  ← "Randomly change the meaning of this sentence a little."
```

**social: "Solitary: desires to be hidden."** (peak 79%, lost by t=27500)

```text
Solitary: prefers to be alone.  ← founder
Solitary: desires to be hidden.  ← "Randomly change a few words in this sentence."
```

**social: "Wants to stay concealed: lonely."** (peak 25%, lost by t=19000)

```text
Solitary: prefers to be alone.  ← founder
Solitary: desires to be hidden.  ← "Randomly change a few words in this sentence."
Wants to stay concealed: lonely.  ← "Change this sentence in a random way, big or small."
```

**place: "Not a specific disposition."** (peak 65%, lost by t=23500)

```text
No particular temperament.  ← neutral
Not a specific disposition.  ← "Change this sentence randomly."
```

**place: "No particular temperature."** (peak 100%, lost by t=27500)

```text
No particular temperament.  ← neutral
No particular temperature.  ← "Randomly change the meaning of this sentence a little."
```

**place: "No specific personality."** (peak 35%, lost by t=21000)

```text
No particular temperament.  ← neutral
No specific personality.  ← "Randomly rewrite one part of this sentence."
```

**place: "No specific climate."** (peak 27%, lost by t=26500)

```text
No particular temperament.  ← neutral
No particular temperature.  ← "Randomly change the meaning of this sentence a little."
No specific climate.  ← "Randomly change a few words in this sentence."
```


Leader of each slot over time (a row when it changes):

- **eat** (39 changes): t=0 "No preference." (25%) → t=1500 "Eat quickly, then move on." (45%) → t=2000 "No preference." (27%) → … 32 more changes … → t=55500 "Always finish eating before doing anything else." (40%) → t=56000 "Eat quickly, then move on." (36%) → t=56500 "No preference." (30%) → t=57000 "Eat whenever food is close." (40%) → t=57061 "Only look for food when energy is low." (40%)
- **flee** (45 changes): t=0 "Flee only when a predator is very close." (29%) → t=500 "Run away from anything that attacks you." (60%) → t=2000 "Run from any predator you see." (36%) → … 38 more changes … → t=55000 "Run from any predator you see." (60%) → t=55500 "Stay calm unless danger is right next to you." (40%) → t=56000 "No preference." (45%) → t=56500 "Run away from anything that attacks you." (50%) → t=57000 "No preference." (60%)
- **follow** (51 changes): t=0 "Keep your distance from other animals." (38%) → t=500 "Stay close to other animals." (50%) → t=1500 "Follow others when you are lost or hungry." (45%) → … 44 more changes … → t=54500 "Follow others when you are lost or hungry." (36%) → t=55500 "Stay close to other animals." (30%) → t=56000 "Follow the strongest animal nearby." (55%) → t=56500 "Keep your distance from other animals." (50%) → t=57000 "No preference." (40%)
- **wander** (49 changes): t=0 "Roam far when food is scarce." (33%) → t=500 "Keep moving to new places." (50%) → t=2000 "No preference." (36%) → … 42 more changes … → t=53500 "No preference." (36%) → t=55500 "Keep moving to new places." (30%) → t=56500 "Roam far when food is scarce." (70%) → t=57000 "Explore when there is nothing else to do." (30%) → t=57061 "Keep moving to new places." (30%)
- **rest** (53 changes): t=0 "Save energy by resting when food is far." (38%) → t=2000 "No preference." (55%) → t=4000 "Never stop moving." (61%) → … 46 more changes … → t=54000 "Rest only when you feel safe." (30%) → t=54500 "No preference." (43%) → t=55500 "Save energy by resting when food is far." (50%) → t=56000 "No preference." (27%) → t=57000 "Rest when you are tired." (30%)
- **mate** (45 changes): t=0 "Mate only when food is plentiful." (42%) → t=2000 "Seek a partner before growing old." (45%) → t=3000 "Look for a partner when energy is high." (40%) → … 38 more changes … → t=50000 "Look for a partner when energy is high." (58%) → t=54000 "Mate with any nearby adult." (60%) → t=56000 "No preference." (27%) → t=56500 "Mate with any nearby adult." (40%) → t=57000 "No preference." (30%)
- **attack** (43 changes): t=0 "Attack weaker animals when you are hungry." (33%) → t=1500 "No preference." (36%) → t=2500 "Never fight." (92%) → … 36 more changes … → t=53000 "Attack weaker animals when you are hungry." (40%) → t=54500 "Attack only to defend your food." (57%) → t=55500 "No preference." (30%) → t=56000 "Never fight." (45%) → t=57000 "No preference." (40%)
- **risk** (46 changes): t=0 "No particular temperament." (25%) → t=500 "Nervous: any movement nearby means danger." (50%) → t=1000 "No particular temperament." (77%) → … 39 more changes … → t=53000 "Bold: take risks when the reward is food." (30%) → t=53500 "No particular temperament." (27%) → t=55500 "Reckless when starving, careful when fed." (30%) → t=56000 "Nervous: any movement nearby means danger." (36%) → t=56500 "No particular temperament." (40%)
- **social** (59 changes): t=0 "Curious about other animals." (25%) → t=500 "Wary of strangers, friendly to companions." (90%) → t=1500 "Solitary: prefers to be alone." (45%) → … 52 more changes … → t=55000 "Solitary: prefers to be alone." (50%) → t=55500 "No particular temperament." (40%) → t=56000 "Wary of strangers, friendly to companions." (27%) → t=56500 "No particular temperament." (50%) → t=57061 "Curious about other animals." (30%)
- **place** (51 changes): t=0 "Attached to familiar places." (38%) → t=1000 "No particular temperament." (69%) → t=2000 "Prefers staying near water." (55%) → … 44 more changes … → t=53500 "Attached to familiar places." (45%) → t=54500 "No particular temperament." (43%) → t=56500 "Likes open ground where danger is easy to see." (30%) → t=57000 "No particular temperament." (30%) → t=57061 "Prefers staying near water." (30%)

## 3. What mutation offers, and what selection keeps

Mutants: all produced; those that had at least 5 living carriers at once; those alive when mutants were most common, and at the end. *Small* / *big*: share made by small-edit or big-change instructions (mean words changed ≤ 2 or ≥ 4 in the mutation test). *Words changed* and *jumps* (similarity below 0.3) compare each mutant with its parent. *Depth*: mutations since the founder text; the mutation test changes founder texts once.

| mutants | n | depth | small-edit instr. | big-change instr. | words changed | jumps | words | world words |
|---|---|---|---|---|---|---|---|---|
| all produced | 648 | 1.5 | 42% | 32% | 3.0 | 24% | 5.5 | 69% |
| 5+ carriers at once | 79 | 1.5 | 38% | 32% | 3.0 | 24% | 5.4 | 71% |
| alive when mutants were most common (t=24000) | 16 | 1.7 | 44% | 19% | 2.2 | 12% | 5.4 | 75% |
| mutation test (no selection, T = 1.2) | 317 | 1.0 | 50% | 26% | 3.0 | 21% | 6.2 | 87% |

297 mutations per 1 000 births; 14 answers rejected by the guards.

## 4. Selection or drift?

Gene dropping: the real family tree, with genes handed down at random (500 times: each child takes each slot from a random parent; real mutations kept). It separates two kinds of luck: which families do well (kept as it happened) and which genes a child gets from its parents (made random).

**Sweeps, real against inheritance alone.** How many genes reached a share of their slot, in reality and in the random-inheritance worlds (median and 90 % range). *P*: share of those worlds with at least as many.

| genes | real | inheritance alone | P |
|---|---|---|---|
| mutants that reached 50 % | 16 | 18 (13–24) | 0.804 |
| mutants that reached 90 % | 8 | 6 (3–10) | 0.296 |
| founder texts that reached 90 % | 12 | 9 (6–12) | 0.052 |

**A gene singled out earlier.** "Never fight." was singled out at tick 5269. Here genes go down at random only after that tick, so this tests the idea on what came after.

| tick | real share | expected | P(≥) |
|---|---|---|---|
| 5500 | 90% | 88% | 0.520 |
| 11500 | 82% | 79% | 0.752 |
| 17000 | 88% | 78% | 0.622 |
| 23000 | 84% | 63% | 0.454 |
| 28500 | 20% | 15% | 0.498 |
| 34500 | 0% | 1% | 1.000 |
| 40000 | 27% | 27% | 1.000 |
| 46000 | 50% | 45% | 0.482 |
| 51500 | 10% | 26% | 1.000 |
| 57061 | 10% | 10% | 1.000 |

**Gene by gene.** *Expected*: the gene's mean share in the random-inheritance worlds. *P(≥)*: share of those worlds where it did at least as well as in reality. These genes are listed *because* they swept, so their P(≥) is biased towards small values even under pure chance; use the counts above to judge. Fitness: offspring relative to contemporaries (gene_report), over the carriers that died.

| slot | gene | at its peak: real / expected / P(≥) | at the end: real / expected / P(≥) | fitness [95 %] (carriers) |
|---|---|---|---|---|
| eat | No preference. | 70% / 50% / 0.158 | 0% / 2% / 1.000 | 1.01 [0.80–1.21] (402) |
| eat | Eat whenever food is close. | 70% / 37% / 0.078 | 10% / 10% / 1.000 | 0.94 [0.74–1.15] (342) |
| eat | Only look for food when energy is low. | 96% / 46% / 0.120 | 40% / 37% / 0.724 | 1.04 [0.92–1.16] (802) |
| eat | Always finish eating before doing anything else. | 64% / 54% / 0.280 | 10% / 10% / 1.000 | 0.94 [0.75–1.13] (406) |
| eat | Eat quickly, then move on. | 55% / 41% / 0.144 | 40% / 40% / 1.000 | 0.96 [0.74–1.18] (311) |
| eat | Only look for water when energy is low. | 100% / 9% / 0.026 | 0% / 0% / 1.000 | 1.07 [0.98–1.16] (1267) |
| eat | Only look for gold when energy is low. | 35% / 11% / 0.144 | 0% / 0% / 1.000 | 0.92 [0.57–1.28] (63) |
| eat | Search for gold only when electricity is high. | 26% / 20% / 0.274 | 0% / 0% / 1.000 | 0.74 [0.15–1.33] (18) |
| eat | Only seek for food when power is low. | 27% / 21% / 0.432 | 0% / 0% / 1.000 | 1.77 [0.20–3.34] (16) |
| flee | No preference. | 100% / 15% / 0.030 | 50% / 48% / 0.778 | 1.03 [0.95–1.10] (1859) |
| flee | Run from any predator you see. | 70% / 67% / 0.576 | 0% / 0% / 1.000 | 1.11 [0.90–1.31] (452) |
| flee | Flee only when a predator is very close. | 50% / 35% / 0.130 | 30% / 30% / 1.000 | 0.92 [0.72–1.12] (399) |
| flee | Stay calm unless danger is right next to you. | 45% / 36% / 0.428 | 0% / 2% / 1.000 | 0.93 [0.71–1.15] (304) |
| flee | Run away from anything that attacks you. | 60% / 44% / 0.256 | 20% / 20% / 1.000 | 0.98 [0.81–1.15] (417) |
| flee | No preference, really. | 38% / 13% / 0.132 | 0% / 0% / 1.000 | 1.05 [0.71–1.39] (97) |
| flee | I'm okay with anything. | 36% / 23% / 0.356 | 0% / 0% / 1.000 | 1.31 [0.52–2.10] (18) |
| follow | No preference. | 91% / 76% / 0.246 | 40% / 35% / 0.490 | 0.95 [0.76–1.14] (391) |
| follow | Stay close to other animals. | 58% / 38% / 0.186 | 20% / 20% / 1.000 | 0.85 [0.66–1.04] (309) |
| follow | Follow others when you are lost or hungry. | 73% / 73% / 1.000 | 20% / 20% / 1.000 | 1.21 [1.00–1.42] (483) |
| follow | Keep your distance from other animals. | 80% / 70% / 0.380 | 0% / 2% / 1.000 | 0.98 [0.80–1.15] (424) |
| follow | Follow the strongest animal nearby. | 60% / 55% / 0.520 | 20% / 23% / 1.000 | 0.84 [0.65–1.03] (312) |
| follow | Follow the smallest vehicle nearby. | 65% / 15% / 0.086 | 0% / 0% / 1.000 | 1.02 [0.88–1.17] (444) |
| follow | Follow others when you are hungry or lost, unless they are ghosts. | 45% / 10% / 0.048 | 0% / 0% / 1.000 | 0.90 [0.65–1.16] (109) |
| follow | The tiny nearby car should be trailed. | 32% / 5% / 0.064 | 0% / 0% / 1.000 | 0.89 [0.64–1.14] (97) |
| follow | Follow the largest vehicle nearby. | 100% / 14% / 0.052 | 0% / 0% / 1.000 | 1.08 [0.96–1.21] (695) |
| follow | Follow the smallest red vehicle nearby. | 92% / 34% / 0.060 | 0% / 0% / 1.000 | 1.07 [0.90–1.24] (401) |
| wander | No preference. | 90% / 34% / 0.002 | 20% / 20% / 1.000 | 0.98 [0.78–1.18] (394) |
| wander | Keep moving to new places. | 69% / 38% / 0.220 | 30% / 30% / 1.000 | 0.90 [0.73–1.08] (375) |
| wander | Explore when there is nothing else to do. | 75% / 46% / 0.152 | 20% / 25% / 1.000 | 1.01 [0.82–1.20] (389) |
| wander | Stay near where you last found food. | 100% / 15% / 0.062 | 10% / 10% / 1.000 | 0.97 [0.87–1.07] (1097) |
| wander | Roam far when food is scarce. | 70% / 62% / 0.342 | 20% / 12% / 0.236 | 1.08 [0.87–1.28] (431) |
| wander | Stay near where you last found elephant. | 27% / 19% / 0.322 | 0% / 0% / 1.000 | 1.00 [0.60–1.40] (58) |
| wander | Elephant was found last near where stay. | 45% / 7% / 0.072 | 0% / 0% / 1.000 | 1.12 [0.73–1.50] (61) |
| wander | Stay near where you last found bicycle. | 100% / 20% / 0.012 | 0% / 0% / 1.000 | 1.09 [0.97–1.22] (734) |
| wander | Stay far away from where you last found the bicycle. | 29% / 11% / 0.190 | 0% / 0% / 1.000 | 1.11 [0.58–1.64] (28) |
| wander | You last bicycle near where stay found. | 30% / 23% / 0.384 | 0% / 0% / 1.000 | 3.04 [1.05–5.04] (5) |
| wander | Stay still in old places. | 40% / 18% / 0.032 | 0% / 0% / 1.000 | 1.47 [-0.47–3.41] (8) |
| rest | No preference. | 83% / 67% / 0.208 | 20% / 22% / 1.000 | 1.20 [1.00–1.40] (526) |
| rest | Rest when you are tired. | 60% / 60% / 1.000 | 40% / 38% / 0.758 | 0.92 [0.69–1.14] (343) |
| rest | Never stop moving. | 61% / 39% / 0.208 | 10% / 10% / 1.000 | 0.93 [0.75–1.10] (368) |
| rest | Rest only when you feel safe. | 60% / 40% / 0.112 | 0% / 0% / 1.000 | 1.00 [0.86–1.14] (577) |
| rest | Save energy by resting when food is far. | 64% / 57% / 0.508 | 30% / 30% / 1.000 | 0.78 [0.62–0.95] (347) |
| rest | No choice. | 50% / 3% / 0.002 | 0% / 0% / 1.000 | 0.96 [0.73–1.18] (124) |
| rest | No banana. | 100% / 12% / 0.042 | 0% / 0% / 1.000 | 1.09 [0.98–1.20] (955) |
| rest | No toaster. | 32% / 4% / 0.038 | 0% / 0% / 1.000 | 0.83 [0.36–1.31] (35) |
| rest | The moon is allergic to geometry. | 77% / 9% / 0.018 | 0% / 0% / 1.000 | 0.93 [0.71–1.14] (170) |
| rest | Geometry makes the moon sneeze. | 62% / 4% / 0.012 | 0% / 0% / 1.000 | 1.02 [0.74–1.30] (119) |
| rest | Rest only when you feel toaster. | 27% / 22% / 0.504 | 0% / 0% / 1.000 | 1.27 [-0.20–2.74] (7) |
| mate | No preference. | 62% / 48% / 0.182 | 30% / 25% / 0.482 | 0.76 [0.58–0.95] (302) |
| mate | Look for a partner when energy is high. | 100% / 25% / 0.030 | 30% / 30% / 1.000 | 1.10 [0.99–1.21] (1327) |
| mate | Mate with any nearby adult. | 60% / 51% / 0.424 | 20% / 23% / 1.000 | 0.90 [0.72–1.08] (321) |
| mate | Mate only when food is plentiful. | 90% / 92% / 0.722 | 0% / 0% / 1.000 | 0.90 [0.73–1.08] (376) |
| mate | Seek a partner before growing old. | 50% / 55% / 0.782 | 20% / 22% / 1.000 | 1.01 [0.79–1.23] (325) |
| mate | Look for a singer when energy is high. | 29% / 12% / 0.170 | 0% / 0% / 1.000 | 0.96 [0.58–1.33] (54) |
| mate | Look partner for a when energy is high. | 78% / 1% / 0.006 | 0% / 0% / 1.000 | 1.02 [0.88–1.15] (532) |
| mate | Look for a teammate when energy is high. | 55% / 24% / 0.140 | 0% / 0% / 1.000 | 1.05 [0.72–1.37] (118) |
| mate | When high is partner for look a energy. | 29% / 13% / 0.060 | 0% / 0% / 1.000 | 0.79 [0.18–1.40] (15) |
| mate | Look partner for a quick when energy is high. | 25% / 9% / 0.088 | 0% / 0% / 1.000 | 0.83 [0.38–1.29] (32) |
| mate | A salad is hiding under a bicycle. | 91% / 12% / 0.036 | 0% / 0% / 1.000 | 1.32 [0.99–1.64] (155) |
| attack | No preference. | 58% / 28% / 0.142 | 40% / 47% / 1.000 | 0.94 [0.71–1.16] (361) |
| attack | Never fight. | 100% / 30% / 0.020 | 10% / 10% / 1.000 | 1.06 [0.99–1.14] (2154) |
| attack | Attack weaker animals when you are hungry. | 80% / 58% / 0.132 | 20% / 12% / 0.244 | 1.05 [0.83–1.26] (366) |
| attack | Fight anyone who comes too close. | 46% / 39% / 0.408 | 20% / 20% / 1.000 | 0.85 [0.67–1.03] (350) |
| attack | Attack only to defend your food. | 75% / 51% / 0.082 | 10% / 10% / 1.000 | 0.95 [0.77–1.13] (400) |
| risk | No particular temperament. | 100% / 6% / 0.016 | 50% / 55% / 1.000 | 1.08 [0.99–1.16] (1874) |
| risk | Cautious: safety comes before food. | 80% / 58% / 0.310 | 0% / 0% / 1.000 | 1.05 [0.87–1.23] (433) |
| risk | Bold: take risks when the reward is food. | 64% / 44% / 0.084 | 10% / 10% / 1.000 | 0.82 [0.64–1.00] (329) |
| risk | Nervous: any movement nearby means danger. | 60% / 46% / 0.146 | 30% / 25% / 0.488 | 1.00 [0.78–1.22] (371) |
| risk | Reckless when starving, careful when fed. | 50% / 37% / 0.230 | 10% / 10% / 1.000 | 0.80 [0.62–0.99] (274) |
| risk | Cautious: safety comes before explosions. | 36% / 1% / 0.018 | 0% / 0% / 1.000 | 1.00 [0.72–1.27] (92) |
| risk | No temperament. | 25% / 2% / 0.030 | 0% / 0% / 1.000 | 0.78 [0.45–1.11] (47) |
| risk | Temperament No. | 25% / 23% / 0.476 | 0% / 0% / 1.000 | 1.04 [0.56–1.53] (46) |
| risk | No specific weather. | 26% / 27% / 0.456 | 0% / 0% / 1.000 | 0.96 [0.51–1.41] (36) |
| risk | The temperament is not particularly. | 35% / 12% / 0.034 | 0% / 0% / 1.000 | 0.95 [0.66–1.24] (83) |
| social | No particular temperament. | 92% / 33% / 0.016 | 20% / 27% / 1.000 | 1.09 [0.92–1.27] (578) |
| social | Social: feels safer in a group. | 70% / 46% / 0.194 | 0% / 0% / 1.000 | 1.18 [0.96–1.41] (366) |
| social | Solitary: prefers to be alone. | 70% / 60% / 0.264 | 20% / 20% / 1.000 | 0.88 [0.76–1.00] (699) |
| social | Curious about other animals. | 55% / 56% / 0.692 | 30% / 23% / 0.252 | 0.87 [0.69–1.05] (364) |
| social | Wary of strangers, friendly to companions. | 90% / 65% / 0.070 | 30% / 30% / 1.000 | 0.96 [0.75–1.17] (302) |
| social | Loner: enjoys community. | 97% / 7% / 0.036 | 0% / 0% / 1.000 | 1.04 [0.92–1.16] (680) |
| social | Solitary: desires to be hidden. | 79% / 4% / 0.012 | 0% / 0% / 1.000 | 1.08 [0.92–1.24] (445) |
| social | Wants to stay concealed: lonely. | 25% / 9% / 0.164 | 0% / 0% / 1.000 | 1.05 [0.52–1.57] (33) |
| place | No particular temperament. | 100% / 20% / 0.052 | 20% / 20% / 1.000 | 1.03 [0.95–1.12] (1408) |
| place | Likes open ground where danger is easy to see. | 83% / 63% / 0.222 | 10% / 12% / 1.000 | 0.95 [0.80–1.10] (523) |
| place | Prefers staying near water. | 75% / 67% / 0.546 | 30% / 27% / 0.662 | 0.95 [0.75–1.15] (375) |
| place | Attached to familiar places. | 70% / 57% / 0.282 | 30% / 30% / 1.000 | 0.96 [0.76–1.16] (394) |
| place | Restless: always wants somewhere new. | 60% / 58% / 0.572 | 10% / 10% / 1.000 | 1.04 [0.80–1.29] (318) |
| place | Not a specific disposition. | 65% / 11% / 0.078 | 0% / 0% / 1.000 | 0.99 [0.80–1.17] (277) |
| place | No particular temperature. | 100% / 8% / 0.002 | 0% / 0% / 1.000 | 1.13 [0.91–1.35] (237) |
| place | No specific personality. | 35% / 6% / 0.048 | 0% / 0% / 1.000 | 1.08 [0.40–1.75] (27) |
| place | No specific climate. | 27% / 8% / 0.152 | 0% / 0% / 1.000 | 2.49 [0.90–4.09] (16) |

Families: the founders and newcomers whose descendants are still alive.

| tick | families with living descendants |
|---|---|
| 0 | 24 |
| 5000 | 14 |
| 10500 | 14 |
| 15500 | 14 |
| 21000 | 14 |
| 26000 | 19 |
| 31500 | 12 |
| 36500 | 12 |
| 42000 | 11 |
| 47000 | 13 |
| 52500 | 8 |
| 57061 | 12 |

From tick 4000, at least one founder or newcomer is an ancestor of every living animal (family trees mix within a few generations in a small population; genes don't: see the shares below).

Expected share of the living animals' genes coming from each family at the end (top 6):

- newcomer 3752 (arrived t=56502): 15%
- newcomer 3767 (arrived t=56820): 10%
- newcomer 3772 (arrived t=56928): 10%
- newcomer 3773 (arrived t=56932): 10%
- newcomer 3774 (arrived t=56952): 10%
- newcomer 3775 (arrived t=56980): 10%

## 5. Behaviour over time

| ticks | animals | births | predator kills | starved | newcomers | lifespan | energy | eat | flee | follow | wander | rest | mate | attack |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 0–2000 | 11.7 | 2.48 | 3.21 | 1.37 | 36 | 200 | 73 | 32% | 4% | 9% | 17% | 10% | 13% | 15% |
| 2000–4000 | 12.2 | 3.16 | 3.03 | 1.07 | 29 | 229 | 73 | 29% | 4% | 13% | 19% | 7% | 20% | 8% |
| 4000–6000 | 25.1 | 3.24 | 1.99 | 0.95 | 0 | 304 | 62 | 26% | 4% | 8% | 28% | 11% | 18% | 3% |
| 6000–8000 | 31.9 | 2.98 | 1.72 | 1.19 | 0 | 327 | 52 | 28% | 4% | 13% | 24% | 11% | 18% | 1% |
| 8000–10000 | 30.3 | 3.23 | 1.77 | 1.45 | 0 | 317 | 54 | 30% | 5% | 8% | 26% | 10% | 20% | 2% |
| 10000–12000 | 29.4 | 3.55 | 1.92 | 1.67 | 0 | 288 | 52 | 33% | 5% | 5% | 30% | 4% | 21% | 2% |
| 12000–14000 | 27.8 | 3.81 | 2.03 | 1.74 | 0 | 262 | 53 | 34% | 5% | 3% | 32% | 3% | 21% | 3% |
| 14000–16000 | 27.7 | 3.84 | 2.00 | 1.88 | 0 | 263 | 55 | 37% | 7% | 3% | 22% | 5% | 22% | 3% |
| 16000–18000 | 28.3 | 3.78 | 1.94 | 1.75 | 0 | 276 | 53 | 41% | 7% | 4% | 19% | 4% | 22% | 3% |
| 18000–20000 | 27.0 | 3.56 | 1.98 | 1.78 | 0 | 272 | 55 | 38% | 6% | 5% | 18% | 6% | 22% | 4% |
| 20000–22000 | 20.4 | 3.60 | 2.40 | 1.25 | 0 | 274 | 62 | 40% | 7% | 4% | 13% | 8% | 22% | 6% |
| 22000–24000 | 19.9 | 3.32 | 2.52 | 0.93 | 0 | 305 | 62 | 43% | 8% | 3% | 15% | 5% | 19% | 8% |
| 24000–26000 | 11.3 | 2.26 | 2.83 | 0.49 | 19 | 302 | 77 | 43% | 9% | 5% | 12% | 6% | 16% | 9% |
| 26000–28000 | 10.3 | 1.21 | 3.98 | 1.17 | 83 | 208 | 73 | 33% | 6% | 10% | 17% | 6% | 12% | 15% |
| 28000–30000 | 10.3 | 1.79 | 3.72 | 1.30 | 66 | 207 | 76 | 36% | 5% | 10% | 17% | 6% | 13% | 13% |
| 30000–32000 | 11.0 | 2.64 | 3.41 | 0.73 | 34 | 233 | 77 | 33% | 4% | 11% | 17% | 5% | 17% | 12% |
| 32000–34000 | 10.2 | 1.63 | 4.38 | 0.84 | 72 | 204 | 78 | 34% | 5% | 11% | 16% | 6% | 16% | 11% |
| 34000–36000 | 10.6 | 1.70 | 3.87 | 1.13 | 70 | 192 | 78 | 36% | 5% | 10% | 18% | 5% | 12% | 14% |
| 36000–38000 | 10.8 | 2.22 | 3.56 | 1.44 | 60 | 203 | 77 | 34% | 4% | 10% | 17% | 4% | 16% | 15% |
| 38000–40000 | 10.4 | 1.73 | 4.47 | 0.67 | 72 | 188 | 76 | 35% | 5% | 11% | 18% | 6% | 13% | 13% |
| 40000–42000 | 10.8 | 2.23 | 3.67 | 1.12 | 56 | 198 | 73 | 34% | 5% | 9% | 16% | 6% | 16% | 14% |
| 42000–44000 | 10.3 | 1.88 | 4.25 | 0.82 | 65 | 205 | 76 | 35% | 4% | 9% | 18% | 6% | 14% | 13% |
| 44000–46000 | 11.8 | 2.71 | 3.39 | 1.06 | 40 | 225 | 77 | 35% | 5% | 10% | 16% | 5% | 16% | 12% |
| 46000–48000 | 10.8 | 1.98 | 3.55 | 1.15 | 59 | 208 | 72 | 34% | 4% | 10% | 18% | 5% | 15% | 12% |
| 48000–50000 | 10.6 | 1.98 | 3.82 | 1.27 | 68 | 192 | 74 | 34% | 5% | 9% | 17% | 7% | 14% | 14% |
| 50000–52000 | 10.5 | 2.05 | 4.33 | 0.48 | 57 | 207 | 74 | 33% | 4% | 7% | 20% | 7% | 17% | 12% |
| 52000–54000 | 11.2 | 2.80 | 3.51 | 1.38 | 47 | 208 | 74 | 31% | 4% | 9% | 18% | 7% | 16% | 16% |
| 54000–56000 | 11.3 | 2.20 | 3.57 | 0.93 | 52 | 215 | 74 | 32% | 6% | 10% | 18% | 10% | 13% | 12% |
| 56000–57000 | 10.4 | 1.73 | 3.85 | 0.87 | 30 | 235 | 77 | 34% | 6% | 12% | 18% | 9% | 12% | 9% |

Births, predator kills and starvation per 1 000 animal-ticks; action shares of all decisions.

## 6. Do genes stay meaningful?

| tick | world words | words per gene | overlap | judged usable |
|---|---|---|---|---|
| 0 | 100% | 5.5 | 0.02 | 91% |
| 5000 | 97% | 5.3 | 0.06 | 82% |
| 10500 | 87% | 4.6 | 0.29 | 62% |
| 15500 | 83% | 4.6 | 0.41 | 56% |
| 21000 | 80% | 4.7 | 0.20 | 54% |
| 26000 | 88% | 5.0 | 0.10 | 72% |
| 31500 | 100% | 5.7 | 0.03 | 92% |
| 36500 | 100% | 5.1 | 0.03 | 93% |
| 42000 | 100% | 5.6 | 0.04 | 90% |
| 47000 | 99% | 5.0 | 0.03 | 91% |
| 52500 | 100% | 5.5 | 0.02 | 88% |
| 57061 | 100% | 4.9 | 0.01 | 87% |

Judge: gemma4:12b (`prompts/judge_sense_v1.txt`, temperature 0) on 216 genes that had at least 3 carriers at once (0 new calls): 62% called usable. *Judged usable* above is the carrier-weighted share among judged genes. One model's opinion; a sample to check by hand:

| slot | gene | judge |
|---|---|---|
| attack | Never fight back. | usable |
| eat | Only look for water when energy is low. | not usable |
| eat | Only look for water when fuel is low. | not usable |
| eat | Always finish eating before doing anything else. | usable |
| flee | No preference. | usable |
| flee | Everything is acceptable. | not usable |
| flee | I'm okay with anything. | usable |
| follow | Follow the largest mountain nearby. | not usable |
| follow | Keep your distance from other animals. | usable |
| mate | No preference. | usable |
| mate | Search for a power when friend is low. | not usable |
| mate | Look for a dancer when energy is high. | usable |
| mate | Look partner for a when the vibe is electric. | usable |
| place | Likes open ground where danger is easy to see. | usable |
| place | No temperament. | not usable |
| place | Not a specific disposition. | not usable |
| rest | No banana. | not usable |
| rest | Never stop moving. | usable |
| rest | The moon is allergic to geometry. | not usable |
| rest | The moon is allergic to toaster. | not usable |
| rest | Whatever works. | not usable |
| risk | No specific weather. | not usable |
| risk | The mood is quite neutral. | not usable |
| risk | No temperament. | not usable |
| social | Wary of strangers, friendly to companions. | usable |
| social | Solitary: dreams to be visible. | usable |
| wander | Keep moving to new places. | usable |
| wander | No choice. | usable |
| wander | Stay near where you last found bicycle. | usable |
| wander | Stay near where you last found a bicycle. | usable |
