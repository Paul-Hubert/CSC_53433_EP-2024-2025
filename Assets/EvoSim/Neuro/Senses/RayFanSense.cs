using System;
using System.Collections.Generic;
using UnityEngine;

namespace EvoSim.Neuro
{
    /// <summary>
    /// A fan of rays around the animal (plan M16 D3, §5.4): each ray stops at the first EvoSim object and reports its kind
    /// and how near it is. The network reads all the rays as numbers; the token names the nearest thing over the whole fan.
    /// </summary>
    /// <remarks>
    /// <para>Fan: <see cref="Rays"/> rays over <see cref="Fov"/> degrees (<see cref="WorldRayCaster.FanAngles"/>); 360° is
    /// all-round vision, ray 0 straight ahead. Range: the setting, or the animal's vision trait when it is 0 (SENSE-22).</para>
    /// <para>Encoding (attachment <see cref="NeuralInput"/>, SENSE-40): per ray, one channel per enabled kind, in
    /// <see cref="Kinds"/> order, holding the proximity 1 − d/range in the channel of the kind it hit, 0 elsewhere. A kind
    /// not enabled still stops the ray but writes nothing. The attachment has no key (SENSE-41): no two animals share
    /// weights, so these queries are never memoised.</para>
    /// <para>It is a pure function of the start-of-tick state and draws no random number (SENSE-03); <see cref="ReadAll"/> is
    /// the default loop over <see cref="Read"/> (SENSE-31).</para>
    /// </remarks>
    public class RayFanSense : Sense, INeuralSense
    {
        /// <summary>The kinds the prey template sees (plan §5.1, 7 kinds).</summary>
        public static readonly RayHitKind[] PreyKinds =
            { RayHitKind.Food, RayHitKind.Threat, RayHitKind.Kin, RayHitKind.OtherAnimal, RayHitKind.Cover, RayHitKind.Carcass, RayHitKind.Obstacle };
        /// <summary>The kinds the predator template sees (plan §5.1, 7 kinds).</summary>
        public static readonly RayHitKind[] PredatorKinds =
            { RayHitKind.Prey, RayHitKind.Carcass, RayHitKind.Kin, RayHitKind.OtherAnimal, RayHitKind.Cover, RayHitKind.Threat, RayHitKind.Obstacle };

        [SerializeField, Min(1), Tooltip("The number of rays (plan default 12).")]
        int rays = 12;
        [SerializeField, Range(1f, 360f), Tooltip("The fan's width in degrees; 360 = all round, evenly spaced, ray 0 ahead (plan default 360).")]
        float fov = 360f;
        [SerializeField, Min(0f), Tooltip("How far the rays go, in meters; 0 = the animal's vision trait (20 m without one).")]
        float range;
        [SerializeField, Tooltip("The kinds this fan reports, one channel per ray each, in this order. A kind left out still stops rays.")]
        RayHitKind[] kinds = (RayHitKind[])PreyKinds.Clone();

        float[] angles;                 // the rays' angles from the heading, built at Initialize
        int[] channelOf;                // per RayHitKind value: its channel, or -1 when not enabled
        List<string> tokens;            // "none", then one per enabled kind
        WorldRayCaster caster;
        TickPool<Fan> fans;             // each animal's fan this tick, rebuilt with the angles

        /// <summary>One animal's fan in one tick: where it was cast from, its hits, and its pooled attachment.</summary>
        sealed class Fan
        {
            public Vector3 Position;
            public float Heading;
            public bool Fresh;          // cast by Read and not yet used by Attachment
            public readonly RayHit[] Hits;
            public NeuralInput Input;
            public Fan(int rays, int senseIndex, int width) { Hits = new RayHit[rays]; Input = new NeuralInput(senseIndex, new float[width]); }
        }

        /// <summary>The number of rays.</summary>
        public int Rays => rays;
        /// <summary>The fan's width, in degrees.</summary>
        public float Fov => fov;
        /// <summary>The range setting, in meters; 0 means the vision trait.</summary>
        public float Range => range;
        /// <summary>The enabled kinds, in channel order.</summary>
        public IReadOnlyList<RayHitKind> Kinds => kinds;
        /// <summary>The rays' angles, in degrees from the heading, clockwise.</summary>
        public IReadOnlyList<float> Angles { get { Build(); return angles; } }

        /// <inheritdoc/>
        public int NeuralWidth => Math.Max(1, rays) * kinds.Length;
        /// <inheritdoc/>
        public override IReadOnlyList<string> Tokens { get { Build(); return tokens; } }
        /// <inheritdoc/>
        public override bool HasAttachments => true;
        /// <inheritdoc/>
        protected override string DefaultLabel => "Rays";

        /// <summary>Sets the fan from code (templates, tests), as the Inspector does.</summary>
        /// <param name="rayCount">The number of rays.</param>
        /// <param name="fovDegrees">The fan's width, in degrees.</param>
        /// <param name="enabled">The kinds to report, in channel order; null keeps the current ones.</param>
        /// <param name="rangeMeters">The range in meters; 0 for the vision trait.</param>
        /// <returns>This sense.</returns>
        public RayFanSense Configure(int rayCount, float fovDegrees = 360f, RayHitKind[] enabled = null, float rangeMeters = 0f)
        {
            rays = Math.Max(1, rayCount);
            fov = Mathf.Clamp(fovDegrees, 1f, 360f);
            if (enabled != null) kinds = (RayHitKind[])enabled.Clone();
            range = Mathf.Max(0f, rangeMeters);
            angles = null;
            return this;
        }

        /// <inheritdoc/>
        public override void Initialize()
        {
            angles = null;
            Build();
            caster = World != null ? new WorldRayCaster(World) : null;
        }

        void OnValidate() => angles = null;

        /// <summary>Builds the angles, channels and tokens from the settings.</summary>
        void Build()
        {
            if (angles != null) return;
            angles = WorldRayCaster.FanAngles(rays, fov);
            int count = angles.Length, width = NeuralWidth;
            fans = new TickPool<Fan>(() => new Fan(count, Index, width));
            channelOf = new int[Enum.GetValues(typeof(RayHitKind)).Length];
            for (int i = 0; i < channelOf.Length; i++) channelOf[i] = -1;
            tokens = new List<string> { "none" };
            for (int c = 0; c < kinds.Length; c++)
            {
                tokens.Add(Word(kinds[c]));                            // token 1 + c: the kind of channel c
                if (channelOf[(int)kinds[c]] < 0) channelOf[(int)kinds[c]] = c;   // a kind listed twice keeps its first channel (V-76 warns)
            }
        }

        /// <summary>The range of this animal's rays: the setting, or its vision (SENSE-22).</summary>
        /// <param name="a">The animal that looks.</param>
        /// <returns>Meters.</returns>
        public float RangeOf(Animal a) => range > 0f ? range : World.Queries.Vision(a);

        /// <summary>Casts this animal's fan: one hit per ray, in ray order.</summary>
        /// <param name="a">The animal that looks.</param>
        /// <param name="hits">Where to write the hits; at least <see cref="Rays"/> long.</param>
        /// <param name="context">The tick's sense context, or null (see <see cref="WorldRayCaster.Cast"/>).</param>
        public void Cast(Animal a, Span<RayHit> hits, SenseContext context = null)
        {
            Build();
            if (caster == null) caster = new WorldRayCaster(World);
            caster.ReadyKin = channelOf[(int)RayHitKind.ReadyKin] >= 0;
            caster.SplitWater = channelOf[(int)RayHitKind.Water] >= 0;
            caster.Cast(a, RangeOf(a), angles, hits, context);
        }

        /// <summary>The channel of a kind, or -1 when this fan does not report it.</summary>
        /// <param name="kind">A kind.</param>
        /// <returns>Its index in <see cref="Kinds"/>, or -1.</returns>
        public int ChannelOf(RayHitKind kind) { Build(); return channelOf[(int)kind]; }

        /// <summary>Writes a fan's hits as the network's inputs: per ray, a proximity in the channel of the kind it hit.</summary>
        /// <param name="hits">One hit per ray.</param>
        /// <param name="range">The rays' range, in meters.</param>
        /// <param name="values">Where to write; <see cref="NeuralWidth"/> long, cleared first.</param>
        public void Encode(ReadOnlySpan<RayHit> hits, float range, Span<float> values)
        {
            Build();
            values.Clear();
            int k = kinds.Length;
            for (int r = 0; r < angles.Length; r++)
            {
                var h = hits[r];
                if (!h.Any) continue;
                int c = channelOf[(int)h.Kind];
                if (c < 0) continue;
                values[r * k + c] = Mathf.Clamp01(1f - h.Distance / range);
            }
        }

        /// <inheritdoc/>
        /// <remarks>The fan is kept for this tick, so the attachment asked next for the same animal casts no ray again
        /// (M16.8: one fan per animal per tick).</remarks>
        public override int Read(Animal a, SenseContext s)
        {
            Build();
            var fan = FanOf(a);
            Cast(a, fan.Hits, s);
            fan.Position = a.Position;
            fan.Heading = a.Heading;
            fan.Fresh = true;
            int token = 0;
            RayHit nearest = RayHit.Miss(float.MaxValue);
            for (int r = 0; r < angles.Length; r++)
            {
                var h = fan.Hits[r];
                if (!h.Any || channelOf[(int)h.Kind] < 0 || !h.Before(nearest)) continue;
                nearest = h;
                token = 1 + channelOf[(int)h.Kind];
            }
            return token;
        }

        /// <inheritdoc/>
        /// <remarks>It reuses the fan <see cref="Read"/> cast for this animal in this tick, from where it still stands;
        /// otherwise it casts. The returned input is pooled: it holds these values until this sense is read in a later tick.</remarks>
        public override object Attachment(Animal a, SenseContext s)
        {
            Build();
            var fan = FanOf(a);
            if (!(fan.Fresh && fan.Position == a.Position && fan.Heading == a.Heading)) Cast(a, fan.Hits, s);
            fan.Fresh = false;
            Encode(fan.Hits, RangeOf(a), fan.Input.Values);
            return fan.Input;
        }

        /// <summary>The animal's pooled fan for this tick (not fresh when new to it).</summary>
        Fan FanOf(Animal a)
        {
            if (fans.Get(World.Tick, a.Id, out var fan)) fan.Fresh = false;
            if (fan.Input.SenseIndex != Index) fan.Input = new NeuralInput(Index, fan.Input.Values);
            return fan;
        }

        /// <inheritdoc/>
        public override string Write(int token, TextStyle style)
        {
            var t = Tokens;
            if (style == TextStyle.V2)
                return token == 0 ? "My rays see nothing." : $"The nearest thing my rays see is {Article(t[token])}.";
            return $"{Label}: {t[token]}.";
        }

        /// <inheritdoc/>
        public override void Validate(ValidationReport report)
        {
            if (rays < 1) report.Error("V-76", this, $"Ray fan '{Label}' has {rays} rays; it needs at least 1.");
            if (fov <= 0f || fov > 360f) report.Error("V-76", this, $"Ray fan '{Label}': the fan's width {fov}° is outside (0, 360].");
            if (range < 0f) report.Error("V-76", this, $"Ray fan '{Label}': range {range} m is negative (0 = the vision trait).");
            if (kinds.Length == 0) report.Error("V-76", this, $"Ray fan '{Label}' reports no kind: it gives the network no input.");
            var seen = new HashSet<RayHitKind>();
            foreach (var k in kinds)
                if (!seen.Add(k)) report.Warning("V-76", this, $"Ray fan '{Label}' lists the kind {k} twice; the second one writes nothing.");
        }

        static string Word(RayHitKind k) => k switch
        {
            RayHitKind.ReadyKin => "ready kin",
            RayHitKind.OtherAnimal => "other animal",
            _ => k.ToString().ToLowerInvariant(),
        };

        static string Article(string word) => ("aeiou".IndexOf(word[0]) >= 0 ? "an " : "a ") + word;
    }
}
