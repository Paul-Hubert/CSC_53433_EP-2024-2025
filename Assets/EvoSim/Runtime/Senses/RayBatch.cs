using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The rays of all deciding animals of a sense phase, cast in one batch (SENSE-30) with RaycastCommand, or one at a
    /// time with Physics.Raycast (Immediate, for comparison: SENSE-31). Version-specific physics code lives here (ARCH-10).
    /// </summary>
    /// <remarks>
    /// Each <see cref="BatchedSense"/> adds its rays with <see cref="Add"/> while <see cref="SenseContext.CastRays"/> walks the
    /// deciding animals; <see cref="Execute"/> casts them; <see cref="Hit"/> gives the k-th ray of one animal and one sense
    /// back. Rays ignore triggers and never hit the views' layer (RAND-11).
    /// </remarks>
    public sealed class RayBatch
    {
        /// <summary>One ray to cast: from where, which way, how far and against which layers.</summary>
        struct Request
        {
            public Vector3 Origin, Direction;   // start point in world space; unit direction
            public float Distance;              // the ray's length, in meters
            public int LayerMask;               // the layers it may hit (never the views' layer)
        }

        readonly List<Request> requests = new List<Request>();      // every ray of this phase, in the order they were added
        readonly List<RaycastHit> results = new List<RaycastHit>(); // the hit of each request, same index
        readonly List<bool> hits = new List<bool>();                // whether each request hit something, same index
        // The range of rays of each (animal, sense) key, in requests.
        readonly Dictionary<long, (int start, int count)> owners = new Dictionary<long, (int, int)>();
        long currentOwner = -1;                                     // the (animal, sense) key between Begin and End; -1 outside
        int currentStart;                                           // index of the first ray of the current owner

        /// <summary>
        /// Cast each ray as soon as it is added (one at a time) instead of in one batch: the reference for SENSE-31 tests.
        /// </summary>
        public bool Immediate { get; set; }

        /// <summary>The number of rays asked for since the last <see cref="Clear"/>.</summary>
        public int Count => requests.Count;

        /// <summary>True once <see cref="Execute"/> ran since the last <see cref="Clear"/>.</summary>
        public bool Executed { get; private set; }

        /// <summary>Forgets every ray and result; <see cref="SenseContext.CastRays"/> calls it before collecting new rays.</summary>
        public void Clear()
        {
            requests.Clear(); results.Clear(); hits.Clear(); owners.Clear();
            currentOwner = -1;
            Executed = false;
        }

        /// <summary>Starts the rays of one animal and one sense.</summary>
        internal void Begin(Animal a, Sense sense)
        {
            currentOwner = Key(a, sense);
            currentStart = requests.Count;
        }

        /// <summary>Ends the rays of the animal and sense started by <see cref="Begin"/> and records their range.</summary>
        internal void End()
        {
            if (currentOwner >= 0) owners[currentOwner] = (currentStart, requests.Count - currentStart);
            currentOwner = -1;
        }

        /// <summary>Asks for a ray, for the animal and sense whose <see cref="BatchedSense.RequestRays"/> is running.</summary>
        /// <param name="origin">Where the ray starts, in world space (e.g. half a meter above the animal).</param>
        /// <param name="direction">Which way it goes; it need not be of length 1.</param>
        /// <param name="distance">How far it goes, in meters.</param>
        /// <param name="layerMask">The layers it may hit; the views' layer is always removed (RAND-11).</param>
        /// <returns>Its number among this animal's rays for this sense (0 for the first): the k to pass to <see cref="Hit"/>.</returns>
        public int Add(Vector3 origin, Vector3 direction, float distance, int layerMask = Physics.DefaultRaycastLayers)
        {
            layerMask &= ~(1 << ViewPool.IgnoreRaycastLayer);                    // never the views: senses don't depend on rendering (RAND-11)
            requests.Add(new Request { Origin = origin, Direction = direction.normalized, Distance = distance, LayerMask = layerMask });
            if (Immediate)
            {
                bool hit = Physics.Raycast(origin, direction.normalized, out var h, distance, layerMask, QueryTriggerInteraction.Ignore);
                results.Add(h);
                hits.Add(hit);
            }
            return requests.Count - 1 - currentStart;
        }

        /// <summary>
        /// Casts every ray in one batch (RaycastCommand.ScheduleBatch, first hit only) and waits for the results. In
        /// <see cref="Immediate"/> mode the rays were already cast by <see cref="Add"/>.
        /// </summary>
        public void Execute()
        {
            Executed = true;
            if (Immediate || requests.Count == 0) return;
            var commands = new NativeArray<RaycastCommand>(requests.Count, Allocator.TempJob);
            var output = new NativeArray<RaycastHit>(requests.Count, Allocator.TempJob);
            try
            {
                var scene = Physics.defaultPhysicsScene;
                for (int i = 0; i < requests.Count; i++)
                {
                    var r = requests[i];
                    var query = new QueryParameters(r.LayerMask, false, QueryTriggerInteraction.Ignore, false);
                    commands[i] = new RaycastCommand(scene, r.Origin, r.Direction, query, r.Distance);
                }
                RaycastCommand.ScheduleBatch(commands, output, 16, 1).Complete();
                results.Clear();
                hits.Clear();
                for (int i = 0; i < requests.Count; i++)
                {
                    results.Add(output[i]);
                    hits.Add(output[i].collider != null);                        // no hit: no collider
                }
            }
            finally
            {
                commands.Dispose();
                output.Dispose();
            }
        }

        /// <summary>Reads back the k-th ray this animal asked for with this sense: whether it hit, and the hit.</summary>
        /// <param name="a">The animal whose ray it is.</param>
        /// <param name="sense">The sense that asked for it: <c>this</c> inside <see cref="Sense.Read"/>.</param>
        /// <param name="k">The ray's number, as <see cref="Add"/> returned it (0 for the first).</param>
        /// <param name="hit">The first collider hit, its point and distance; default when nothing was hit.</param>
        /// <returns>True when the ray hit something; false when it hit nothing, or no such ray was cast.</returns>
        public bool Hit(Animal a, Sense sense, int k, out RaycastHit hit)
        {
            hit = default;
            if (!owners.TryGetValue(Key(a, sense), out var range) || k >= range.count) return false;
            hit = results[range.start + k];
            return hits[range.start + k];
        }

        /// <summary>The key of one animal and one sense: the animal's id and the sense's index (its low 8 bits).</summary>
        static long Key(Animal a, Sense s) => ((long)a.Id << 8) | (long)(s.Index & 0xFF);
    }
}
