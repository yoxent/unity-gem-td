using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay.Enemies;

namespace GemTD.Gameplay.SkillLab
{
    public sealed class DummyField
    {
        public const int PinCount = 10;
        public const float PinPitch = 1.5f;
        public static readonly Vector3 DefaultTowerPosition = new Vector3(-8f, 0f, 0f);
        public static readonly Vector3 HeadPin = new Vector3(2f, 0f, 0f);

        readonly EnemyRuntime[] _dummies = new EnemyRuntime[PinCount];
        readonly Vector3[] _homes = new Vector3[PinCount];

        public static void WriteHomes(Vector3[] homes)
        {
            if (homes == null || homes.Length < PinCount)
                return;

            var pitch = PinPitch;
            var head = HeadPin;
            homes[0] = head;
            homes[1] = new Vector3(head.x + pitch, 0f, head.z - pitch * 0.5f);
            homes[2] = new Vector3(head.x + pitch, 0f, head.z + pitch * 0.5f);
            homes[3] = new Vector3(head.x + pitch * 2f, 0f, head.z - pitch);
            homes[4] = new Vector3(head.x + pitch * 2f, 0f, head.z);
            homes[5] = new Vector3(head.x + pitch * 2f, 0f, head.z + pitch);
            homes[6] = new Vector3(head.x + pitch * 3f, 0f, head.z - pitch * 1.5f);
            homes[7] = new Vector3(head.x + pitch * 3f, 0f, head.z - pitch * 0.5f);
            homes[8] = new Vector3(head.x + pitch * 3f, 0f, head.z + pitch * 0.5f);
            homes[9] = new Vector3(head.x + pitch * 3f, 0f, head.z + pitch * 1.5f);
        }

        public void Init(EnemyDefinition def)
        {
            WriteHomes(_homes);
            for (var i = 0; i < PinCount; i++)
            {
                _dummies[i] = new EnemyRuntime();
                var home = _homes[i];
                _dummies[i].Init(def, new[] { home });
            }
        }

        public void ResetPins()
        {
            for (var i = 0; i < PinCount; i++)
            {
                if (_dummies[i] != null)
                    _dummies[i].SetWorldPosition(_homes[i]);
            }
        }

        public EnemyRuntime GetDummy(int index)
        {
            if (index < 0 || index >= PinCount)
                return null;
            return _dummies[index];
        }

        public void CopyLiving(List<EnemyRuntime> into)
        {
            if (into == null)
                return;
            into.Clear();
            for (var i = 0; i < PinCount; i++)
            {
                var dummy = _dummies[i];
                if (dummy != null && dummy.IsAlive)
                    into.Add(dummy);
            }
        }
    }
}
