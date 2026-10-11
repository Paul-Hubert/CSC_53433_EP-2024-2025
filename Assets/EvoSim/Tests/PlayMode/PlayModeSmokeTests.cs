using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EvoSim.Tests
{
    /// <summary>
    /// A smoke test of the play-mode test assembly: play-mode tests run, reach <c>FixedUpdate</c> and see the runtime
    /// assembly. If it fails, the problem is the test set-up, not EvoSim.
    /// </summary>
    public class PlayModeSmokeTests
    {
        [UnityTest, Description("M0 skeleton: a PlayMode test runs through FixedUpdate")]
        public IEnumerator FixedUpdateRunsInPlayMode()
        {
            var go = new GameObject("EvoSim smoke");
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(Application.isPlaying);
            Assert.Greater(Units.Epsilon, 0f);
            Object.Destroy(go);
        }
    }
}
