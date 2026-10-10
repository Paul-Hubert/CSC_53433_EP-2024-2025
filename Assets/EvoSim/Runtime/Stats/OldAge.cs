using UnityEngine;

namespace EvoSim
{
    /// <summary>Death of old age: age above the maximum (reference 1 500 ticks; ANIM-40).</summary>
    public class OldAge : DeathRule
    {
        [SerializeField, Min(1), Tooltip("Maximum age, in ticks (reference 1 500).")]
        int maxAge = 1500;

        public int MaxAge => maxAge;

        public override string CauseOfDeath(Animal a) => a.Age > maxAge ? "old_age" : null;

        public void SetMaxAge(int ticks) => maxAge = Mathf.Max(1, ticks);
    }
}
