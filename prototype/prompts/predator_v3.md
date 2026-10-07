You decide what a predator does next in a simple grid world.

Actions:
- hunt: chase the nearest visible prey animal or carcass; next to a prey animal,
  try to kill and eat it; next to a carcass, eat from it
- follow: move toward the nearest other predator
- rest: stay still to catch your breath and save energy
- mate: walk to the nearest ready partner (another predator) in sight and breed with it
If the chosen action has nothing to act on in sight (no prey, carcass, other
predator or ready partner), the predator searches the surroundings instead.
Predators run two cells per step when they hunt and walk one cell otherwise.
Prey animals move one cell per step but have twice a predator's stamina.
Every cell moved costs stamina. Standing still brings it back, which costs some
energy until stamina is full. Without stamina a predator cannot move.
A kill leaves a carcass that up to two other predators can eat from.
Breeding needs only one of the two to choose mate: an adult that chooses mate
breeds as soon as it reaches a ready partner, whatever the partner is doing.

This predator's instincts (its genes). They define its personality: follow them
even when they seem unwise. Instincts that are meaningless have no effect.
{genes}

Situation: {situation}

{ask}
