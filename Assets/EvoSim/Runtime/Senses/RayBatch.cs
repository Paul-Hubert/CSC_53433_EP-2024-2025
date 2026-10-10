using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace EvoSim
{
    /// <summary>
    /// The rays of all deciding animals of a sense phase, cast in one batch (SENSE-30) with RaycastCommand, or one at a
    /// time with Physics.Raycast (Immediate, for comparison: SENSE-31). Version-specific physics code lives here (ARCH-10).
    /// </summary>
    public sealed class RayBatch
    {
        struct Request
        {
            public Vector3 Origin, Direction;
            public float Distance;
            public int LayerMask;
        }

        readonly List<Request> requests = new List<Request>();
        readonly List<RaycastHit> results = new List<RaycastHit>();
        readonly List<bool> hits = new List<bool>();
        readonly Dictionary<long, (int start, int count)> owners = new Dictionary<long, (int, int)>();
        long currentOwner = -1;
        int currentStart;

        /// <summary>Cast each ray as soon as it is added (one at a time) instead of in one batch.</summary>
        public bool Immediate { get; set; }
        public int Count => requests.Count;
        public bool Executed { get; private set; }

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

        internal void End()
        {
            if (currentOwner >= 0) owners[currentOwner] = (currentStart, requests.Count - currentStart);
            currentOwner = -1;
        }

        /// <summary>Asks for a ray; returns its index among this animal's rays for this sense.</summary>
        public int Add(Vector3 origin, Vector3 direction, float distance, int layerMask = Physics.DefaultRaycastLayers)
        {
            requests.Add(new Request { Origin = origin, Direction = direction.normalized, Distance = distance, LayerMask = layerMask });
            if (Immediate)
            {
                bool hit = Physics.Raycast(origin, direction.normalized, out var h, distance, layerMask, QueryTriggerInteraction.Ignore);
                results.Add(h);
                hits.Add(hit);
            }
            return requests.Count - 1 - currentStart;
        }

        /// <summary>Casts every ray in one batch (RaycastCommand.ScheduleBatch).</summary>
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

        /// <summary>The k-th ray this animal asked for with this sense: whether it hit, and the hit.</summary>
        public bool Hit(Animal a, Sense sense, int k, out RaycastHit hit)
        {
            hit = default;
            if (!owners.TryGetValue(Key(a, sense), out var range) || k >= range.count) return false;
            hit = results[range.start + k];
            return hits[range.start + k];
        }

        static long Key(Animal a, Sense s) => ((long)a.Id << 8) | (long)(s.Index & 0xFF);
    }
}
