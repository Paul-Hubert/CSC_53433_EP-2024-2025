using UnityEngine;

namespace Examples
{
    // A fake gameplay component, only here so EventFunctionsDemo has something of ours to find.
    public class DemoHealth : MonoBehaviour
    {
        public float maxHealth = 100f;
        public float current = 100f;

        public void Damage(float amount) => current = Mathf.Max(0f, current - amount);
    }
}
