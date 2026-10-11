using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Removes killed animals; every living animal ages by one tick (babies of this tick included); then each species'
    /// death rules (starvation, old age…) in module order. Every death is recorded once (TICK-04 #7, ANIM-35, ANIM-40…43).
    /// </summary>
    /// <remarks>
    /// <para>Seventh phase of the reference tick, after Hatch and before Migration. Animals struck this tick were only
    /// marked <c>Killed</c>; here they die with the cause <c>killed</c>. Each <see cref="DeathRule"/> fills in a cause for
    /// the living animals that have none yet (<see cref="DeathRule.CheckAll"/>), so the first rule in module order wins. Deaths
    /// go through <see cref="World.RecordDeath"/>, and the dead leave their species' list at the end of the phase
    /// (ANIM-43).</para>
    /// <para>No random number is drawn here.</para>
    /// </remarks>
    public class DeathPhase : TickPhase
    {
        readonly List<Animal> living = new List<Animal>();     // the species' living animals this tick, reused
        string[] causes = new string[64];                       // per living animal: its cause of death, or null; grown as needed

        /// <summary>Records killed animals' deaths, ages the living, then applies each species' death rules.</summary>
        /// <inheritdoc/>
        public override void Run(TickContext t)
        {
            foreach (var s in World.AllSpecies)
            {
                var animals = s.Animals;
                for (int i = 0; i < animals.Count; i++)
                {
                    var a = animals[i];
                    if (a.Killed && !a.IsGone) World.RecordDeath(a, "killed");
                }

                living.Clear();
                for (int i = 0; i < animals.Count; i++)
                    if (!animals[i].IsGone) living.Add(animals[i]);
                for (int i = 0; i < living.Count; i++) living[i].Age++;           // ANIM-35

                if (causes.Length < living.Count) causes = new string[living.Count * 2];
                System.Array.Clear(causes, 0, living.Count);
                foreach (var rule in s.ModulesOf<DeathRule>()) rule.CheckAll(living, causes);
                for (int i = 0; i < living.Count; i++)
                    if (causes[i] != null) World.RecordDeath(living[i], causes[i]);
                s.RemoveGone();                                                    // ANIM-43
            }
        }
    }
}
