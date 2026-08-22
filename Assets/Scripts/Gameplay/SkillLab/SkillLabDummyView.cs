using UnityEngine;

namespace GemTD.Gameplay.SkillLab
{
    public sealed class SkillLabDummyView : MonoBehaviour
    {
        [SerializeField] int index;

        public int Index => index;

        public void SetIndex(int value)
        {
            index = value;
        }
    }
}
