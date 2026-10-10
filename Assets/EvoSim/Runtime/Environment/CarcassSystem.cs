using System.Globalization;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Carcasses, as each species' edible component describes them: portions, energy, lifetime (03 §3).
    /// A carcass of a species is edible when that species' Edible leaves one; diets name it "carcass:&lt;species&gt;".
    /// </summary>
    public class CarcassSystem : EntitySystem<Carcass>
    {
        [SerializeField, TextArea(1, 3), Tooltip("Rule line in the prompt of species that scavenge ({portions} = portions per carcass).")]
        string promptRule = "A kill leaves a carcass that up to {portions} other predators can eat from.";

        public override string Kind => "carcass";

        /// <summary>A carcass at the killed animal's position, if its species' Edible leaves one (ACT-11).</summary>
        public Carcass Create(Animal killed, int killerId)
        {
            var e = killed.Species.GetComponent<Edible>();
            if (e == null || !e.enabled || e.carcassPortions <= 0) return null;
            return Add(new Carcass
            {
                Of = killed.Species,
                KillerId = killerId,
                PortionsLeft = e.carcassPortions,
                EnergyPerPortion = e.carcassEnergy,
                LifetimeLeft = Mathf.Max(1, e.carcassTicks),
                Position = killed.Position,
            });
        }

        /// <summary>Eats one portion: false if the eater may not (its killer, ate already, none left).</summary>
        public bool EatPortion(Carcass c, Animal eater)
        {
            if (!c.CanBeEatenBy(eater)) return false;
            c.PortionsLeft--;
            c.EatenBy.Add(eater.Id);
            if (c.PortionsLeft <= 0) c.UsedUp = true;
            return true;
        }

        /// <summary>
        /// The carcass line of a species that scavenges carcasses (08 §7): the diet writes it where it stands among the
        /// species' rules; {portions} is the portions of the carcasses it eats.
        /// </summary>
        public string PromptRuleFor(EdibleTarget carcasses)
        {
            var e = carcasses.Species != null ? carcasses.Species.GetComponent<Edible>() : null;
            int portions = e != null ? e.carcassPortions : 0;
            return promptRule.Replace("{portions}", portions.ToString(CultureInfo.InvariantCulture));
        }

        public void SetPromptRule(string text) => promptRule = text;
    }
}
