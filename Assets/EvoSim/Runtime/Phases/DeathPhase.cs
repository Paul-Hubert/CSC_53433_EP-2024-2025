using System.Collections.Generic;

namespace EvoSim
{
    /// <summary>
    /// Removes killed animals; every living animal ages by one tick (babies of this tick included); then each species'
    /// death rules (starvation, old age…) in module order. Every death is recorded once (TICK-04 #7, ANIM-35, ANIM-40…43).
    /// </summary>
    public class DeathPhase : TickPhase
    {
        readonly List<Animal> living = new List<Animal>();
        string[] causes = new string[64];

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
