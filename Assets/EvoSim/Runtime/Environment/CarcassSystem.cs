using System.Globalization;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Carcasses, as each species' edible component describes them: portions, energy, lifetime (03 §3).
    /// A carcass of a species is edible when that species' Edible leaves one; diets name it "carcass:&lt;species&gt;".
    /// </summary>
    /// <remarks>
    /// <para>Place it under the World's Environment object, as the "Carcasses" GameObject. A successful strike calls
    /// <see cref="Create"/> on the first carcass system of the World; a scavenging interaction calls
    /// <see cref="EatPortion"/>. The environment phase counts lifetimes down and removes rotten or used-up carcasses
    /// (ENV-22).</para>
    /// <para>Removed carcasses go back to a pool and are reused, so a kill allocates nothing (R-07). A subclass that
    /// overrides <see cref="OnRemoved"/> must call the base method to keep the pool.</para>
    /// </remarks>
    public class CarcassSystem : EntitySystem<Carcass>
    {
        [SerializeField, TextArea(1, 3), Tooltip("Rule line in the prompt of species that scavenge ({portions} = portions per carcass).")]
        string promptRule = "A kill leaves a carcass that up to {portions} other predators can eat from.";

        readonly System.Collections.Generic.Stack<Carcass> spare = new System.Collections.Generic.Stack<Carcass>();   // reused: kills don't allocate (R-07)

        /// <inheritdoc/>
        public override string Kind => "carcass";

        /// <summary>Removes every carcass and empties the pool of spare ones.</summary>
        /// <inheritdoc/>
        public override void Initialize()
        {
            base.Initialize();
            spare.Clear();
        }

        /// <summary>Puts the removed carcass back in the pool, to be reused by the next kill.</summary>
        /// <inheritdoc/>
        protected override void OnRemoved(Carcass c, string reason, TickContext t) => spare.Push(c);

        /// <summary>
        /// Leaves a carcass at the killed animal's position, if its species' Edible leaves one (ACT-11): portions, energy per
        /// portion and lifetime (at least 1 tick) come from that Edible.
        /// </summary>
        /// <remarks>Called by the interaction resolver during the act phase, right after a kill.</remarks>
        /// <param name="killed">The animal just killed; its species' <see cref="Edible"/> describes the carcass.</param>
        /// <param name="killerId">The id of the hunter, which may not eat from the carcass.</param>
        /// <returns>The new carcass, or null when the killed species leaves none (no enabled Edible, or 0 portions).</returns>
        public virtual Carcass Create(Animal killed, int killerId)
        {
            var e = killed.Species.GetComponent<Edible>();
            if (e == null || !e.enabled || e.carcassPortions <= 0) return null;
            var c = spare.Count > 0 ? spare.Pop() : new Carcass();
            c.Of = killed.Species;
            c.KillerId = killerId;
            c.PortionsLeft = e.carcassPortions;
            c.EnergyPerPortion = e.carcassEnergy;
            c.LifetimeLeft = Mathf.Max(1, e.carcassTicks);
            c.Position = killed.Position;
            c.UsedUp = false;
            c.EatenBy.Clear();
            return Add(c);
        }

        /// <summary>
        /// Takes one portion of a carcass for an eater, if <see cref="Carcass.CanBeEatenBy"/> allows it. The last portion
        /// marks the carcass used up. The caller gives the energy and checks reach and diet.
        /// </summary>
        /// <param name="c">The carcass eaten from.</param>
        /// <param name="eater">The animal that eats.</param>
        /// <returns>True when a portion was taken; false if the eater may not (its killer, ate already, none left).</returns>
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
        /// <param name="carcasses">The diet entry that eats carcasses, e.g. "carcass:prey".</param>
        /// <returns>The prompt rule line, with {portions} replaced by the carcass portions of that species (0 if unknown).</returns>
        public string PromptRuleFor(EdibleTarget carcasses)
        {
            var e = carcasses.Species != null ? carcasses.Species.GetComponent<Edible>() : null;
            int portions = e != null ? e.carcassPortions : 0;
            return promptRule.Replace("{portions}", portions.ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>Sets the prompt rule line from code (scene builders, tests); {portions} is replaced when the line is written.</summary>
        /// <param name="text">The rule line.</param>
        public void SetPromptRule(string text) => promptRule = text;
    }
}
