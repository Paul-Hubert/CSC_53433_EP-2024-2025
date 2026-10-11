using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// Reads one animal and the world and returns one token from a fixed list (SENSE-01): what the brain is told about one
    /// aspect of the situation, such as the nearest food or the energy level.
    /// </summary>
    /// <remarks>
    /// <para>Derive from it, list every token in <see cref="Tokens"/>, implement <see cref="Read"/>, and place the component
    /// under a species. The species collects its senses depth-first in its hierarchy: that order (<see cref="Index"/>) is the
    /// order of the tokens in the <see cref="Observation"/> and of the fragments in the situation text (SENSE-02, SENSE-10).</para>
    /// <para>Lifecycle: <see cref="SpeciesModule.Declare"/> declares the traits the sense reads (such as "vision");
    /// <see cref="SpeciesModule.Initialize"/> finds stats and other modules and builds the token list; then, on every tick
    /// with decisions, the sense phase calls <see cref="ReadAll"/> for the deciding animals of the species, and the species
    /// calls <see cref="Write"/> to turn each token into text.</para>
    /// <para>A sense only reads. It sees the world as it was at the start of the tick (SENSE-04), asks through
    /// <see cref="SenseContext"/> so hidden and killed animals are never reported (SENSE-06), and draws no random number:
    /// the same state gives the same token (SENSE-03). It keeps settings only, nothing per animal.</para>
    /// <para>Each sense multiplies the species' number of possible observations by its token count (SENSE-05): few tokens
    /// keep the memo and the answer cache useful.</para>
    /// </remarks>
    /// <example>
    /// The sample <c>ChasedSense</c> (22 §13): whether the nearest threat is hunting this animal.
    /// <code>
    /// public class ChasedSense : Sense
    /// {
    ///     static readonly IReadOnlyList&lt;string&gt; tokens = System.Array.AsReadOnly(new[] { "no", "yes" });
    ///
    ///     public override IReadOnlyList&lt;string&gt; Tokens => tokens;
    ///     protected override string DefaultLabel => "Chased";
    ///
    ///     public override int Read(Animal a, SenseContext s)
    ///     {
    ///         var threat = s.NearestAnimal(a, AnimalSet.Threats, s.Vision(a));
    ///         if (threat == null) return 0;
    ///         var t = threat.Value.Animal;
    ///         bool hunting = t.CurrentAction != null &amp;&amp; t.CurrentAction.Hunts;
    ///         return hunting &amp;&amp; s.CurrentTarget(t) == a ? 1 : 0;
    ///     }
    ///
    ///     public override string Write(int token, TextStyle style) =>
    ///         style == TextStyle.V2 ? (token == 1 ? "Something is chasing me." : "Nothing is chasing me.")
    ///                               : $"{Label}: {tokens[token]}.";
    /// }
    /// </code>
    /// </example>
    public abstract class Sense : SpeciesModule
    {
        /// <summary>
        /// The label typed in the Inspector, or empty for <see cref="DefaultLabel"/>. Read it through <see cref="Label"/>.
        /// </summary>
        [SerializeField, Tooltip("The sense's label in the situation text (SENSE-10, SPEC-20). Empty = the sense's default.")]
        protected string label = "";

        /// <summary>
        /// The sense's name in the situation text ("Food: here.") and in <see cref="ObservationView.Token"/>: the Inspector's
        /// label, or <see cref="DefaultLabel"/> when it is empty (SENSE-10, SPEC-20).
        /// </summary>
        public string Label => string.IsNullOrEmpty(label) ? DefaultLabel : label;

        /// <summary>
        /// The label when the Inspector leaves it empty: by default the class name without "Sense" ("AgeSense" gives "Age").
        /// Reference senses override it with their reference label ("Food", "Predator", "Carcass"…).
        /// </summary>
        protected virtual string DefaultLabel => GetType().Name.Replace("Sense", "");

        /// <summary>
        /// This sense's place in the species' sense order (0 for the first), which is its place in every observation; -1
        /// until the species collects its modules at set-up.
        /// </summary>
        public int Index { get; internal set; } = -1;

        /// <summary>
        /// Every token this sense can return, "none" included, in a fixed order: <see cref="Read"/> returns an index into this
        /// list. Fixed before the run (SENSE-01); its length is the sense's factor in the observation space (SENSE-05).
        /// </summary>
        /// <remarks>Build it in a field or in <see cref="SpeciesModule.Initialize"/>, never during a run. A static list shared
        /// by every instance should be read-only.</remarks>
        public abstract IReadOnlyList<string> Tokens { get; }

        /// <summary>Reads one animal at a decision and returns its token (SENSE-03).</summary>
        /// <param name="a">The animal that decides.</param>
        /// <param name="s">The world as it was at the start of the tick: nearest animals, food, cover, distances, rays.</param>
        /// <returns>An index into <see cref="Tokens"/>.</returns>
        /// <remarks>
        /// Called by the sense phase, through <see cref="ReadAll"/>, for every animal that decides this tick, and by tools
        /// (<see cref="Species.Observe"/>). It must give the same token for the same state, draw no random number, change
        /// nothing in the world, and learn about other animals only through <paramref name="s"/>, which leaves out hidden
        /// and killed ones (SENSE-03, SENSE-06).
        /// </remarks>
        public abstract int Read(Animal a, SenseContext s);

        /// <summary>
        /// Reads every deciding animal of the species in one call: the batch entry point the sense phase uses (20 §10,
        /// ARCH-12). The default calls <see cref="Read"/> for each animal.
        /// </summary>
        /// <param name="group">The deciding animals of this species this tick, in id order.</param>
        /// <param name="s">This tick's sense context, its rays already cast.</param>
        /// <param name="tokens">Where to write the tokens: <c>tokens[i]</c> for <c>group[i]</c>; exactly as long as the group.</param>
        /// <remarks>
        /// Override it only to go faster (a Burst job…). The override must write exactly the tokens <see cref="Read"/>
        /// would give, animal by animal (SENSE-31); <see cref="Read"/> stays the readable reference and the test oracle.
        /// </remarks>
        public virtual void ReadAll(IReadOnlyList<Animal> group, SenseContext s, Span<int> tokens)
        {
            for (int i = 0; i < group.Count; i++) tokens[i] = Read(group[i], s);
        }

        /// <summary>Writes a token as the text the brain reads: one fragment of the situation text (SENSE-10).</summary>
        /// <param name="token">An index into <see cref="Tokens"/>.</param>
        /// <param name="style">The world's text style: <see cref="TextStyle.V1"/> terse ("Energy: low."), or
        /// <see cref="TextStyle.V2"/> first person ("I am hungry and weak.") (SENSE-11).</param>
        /// <returns>A sentence ending with a period. The default writes "Label: token." in both styles.</returns>
        /// <remarks>
        /// The species joins the fragments of its senses with spaces (<see cref="Species.Describe"/>). The text depends on
        /// the token only, never on the animal, so the same observation always gives the same text: the answer cache keys
        /// on it. Override it for V2 sentences or to state a range in meters (SENSE-12, SENSE-13).
        /// </remarks>
        public virtual string Write(int token, TextStyle style) => $"{Label}: {Tokens[token]}.";

        /// <summary>Gives an attachment for this animal at this decision, such as a small camera image (SENSE-40).</summary>
        /// <param name="a">The animal that decides.</param>
        /// <param name="s">This tick's sense context.</param>
        /// <returns>The attachment, or null for none (the default).</returns>
        /// <remarks>
        /// Called by the ask-brains phase while it builds the query, only when <see cref="HasAttachments"/> is true. The
        /// attachment travels in <see cref="DecisionQuery.Attachments"/>; only a brain whose
        /// <see cref="Brain.AcceptsAttachments"/> is true may read it (V-51).
        /// </remarks>
        public virtual object Attachment(Animal a, SenseContext s) => null;

        /// <summary>
        /// True when this sense can give attachments (SENSE-41): the ask-brains phase then calls <see cref="Attachment"/>,
        /// and validation checks that the species' brain accepts them (V-51). False by default.
        /// </summary>
        public virtual bool HasAttachments => false;

        /// <summary>
        /// Gives a short text that stands for an attachment in the memo and cache keys, such as a coarse summary (SENSE-41).
        /// </summary>
        /// <param name="attachment">An attachment this sense gave.</param>
        /// <returns>The key, or null (the default): then a query carrying this attachment is never memoised, shared or cached.</returns>
        public virtual string AttachmentKey(object attachment) => null;

        /// <summary>Sets the label from code (WorldBuilder, tests), as the Inspector field does.</summary>
        /// <param name="text">The new label; empty for <see cref="DefaultLabel"/>.</param>
        public void SetLabel(string text) => label = text;
    }
}
