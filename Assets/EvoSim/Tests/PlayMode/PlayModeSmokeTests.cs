using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace EvoSim.Tests
{
    /// <summary>M0 skeleton: PlayMode tests run and see the runtime assembly.</summary>
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
