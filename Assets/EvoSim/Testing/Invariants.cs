using System.Collections.Generic;

namespace EvoSim.Testing
{
    /// <summary>
    /// The checks run after every tick in test worlds (30 §5): positions inside and walkable, no NaN, stats in range,
    /// unique ids, no dead or killed animal left, valid actions and rows, populations between floor and cap, carcass
    /// portions ≥ 0, genomes of the right length and species.
    /// </summary>
    public static class Invariants
    {
        public static List<string> Check(World w)
        {
            var problems = new List<string>();
            var ids = new HashSet<int>();
            foreach (var s in w.AllSpecies)
            {
                var stats = s.Declarations.Stats;
                int living = 0;
                foreach (var a in s.Animals)
                {
                    if (a.IsGone || a.Killed) { problems.Add($"{a} is dead or killed but still listed"); continue; }
                    living++;
                    if (!ids.Add(a.Id)) problems.Add($"id {a.Id} used twice");
                    var p = a.Position;
                    if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z)) problems.Add($"{a} at NaN");
                    if (w.Ground != null && !w.Ground.IsWalkable(p)) problems.Add($"{a} on non-walkable ground at {p}");
                    foreach (var st in stats)
                    {
                        float v = a[st.Id];
                        if (float.IsNaN(v)) problems.Add($"{a} {st.Id.Name} is NaN");
                        else if (v > st.Id.Max + 1e-4f || v < st.Id.Min - 1e-4f) problems.Add($"{a} {st.Id.Name} = {v} outside [{st.Id.Min}, {st.Id.Max}]");
                        else if (st.Id.MaxTrait >= 0 && v > a.Trait(s.Declarations.Traits[st.Id.MaxTrait].Id) + 1e-4f) problems.Add($"{a} {st.Id.Name} above its trait cap");
                    }
                    if (a.Action < -1 || a.Action >= s.Actions.Count) problems.Add($"{a} has action index {a.Action}");
                    if (a.LastProbabilities != null && BrainAnswer.Check(a.LastProbabilities, s.Actions.Count) != null)
                        problems.Add($"{a} has an invalid probability row: {BrainAnswer.Check(a.LastProbabilities, s.Actions.Count)}");
                    if (a.Genome == null || a.Genome.Species != s || a.Genome.Count != s.Genes.Count) problems.Add($"{a} has a genome of the wrong species or length");
                }
                var cap = s.Module<CapRule>();
                if (cap != null && cap.Rule == CapRule.Mode.Migrate && living > cap.Cap) problems.Add($"{s.Id}: {living} above its cap {cap.Cap}");
                var floor = s.Module<FloorRule>();
                if (floor != null && living < floor.Floor) problems.Add($"{s.Id}: {living} below its floor {floor.Floor}");
            }
            var carcasses = w.Service<CarcassSystem>();
            if (carcasses != null)
                foreach (var c in carcasses.Entities)
                    if (c.PortionsLeft < 0) problems.Add($"carcass {c.Id} has {c.PortionsLeft} portions");
            return problems;
        }
    }
}
