using System.Collections.Generic;
using GemTD.Gameplay.Run;
using NUnit.Framework;
using UnityEngine;

namespace GemTD.Tests.EditMode
{
    public sealed class SpawnTipSchedulerTests
    {
        [Test]
        public void Next_RoundRobinsAcrossTwoTips_DoesNotMultiply()
        {
            var tips = new List<Vector2Int>
            {
                new Vector2Int(7, 3),
                new Vector2Int(3, 5),
            };
            var index = 0;

            Assert.AreEqual(new Vector2Int(7, 3), SpawnTipScheduler.Next(tips, ref index));
            Assert.AreEqual(new Vector2Int(3, 5), SpawnTipScheduler.Next(tips, ref index));
            Assert.AreEqual(new Vector2Int(7, 3), SpawnTipScheduler.Next(tips, ref index));
            Assert.AreEqual(new Vector2Int(3, 5), SpawnTipScheduler.Next(tips, ref index));
            Assert.AreEqual(4, index);
        }
    }
}
