using UnityEngine;

namespace GemTD.Gameplay.Map
{
    [System.Serializable]
    public struct HeightInfluenceWeights
    {
        [Tooltip("Stay the same height as the chosen neighbor.")]
        [Range(0f, 1f)]
        public float Same;

        [Tooltip("One layer taller. Skipped if the neighbor is already tallest — that weight folds into Same.")]
        [Range(0f, 1f)]
        public float StepUp;

        [Tooltip("One layer shorter. Skipped if the neighbor is already shortest — that weight folds into Same.")]
        [Range(0f, 1f)]
        public float StepDown;

        public static HeightInfluenceWeights Default => new HeightInfluenceWeights(0.56f, 0.22f, 0.22f);

        public bool IsUnset => Same == 0f && StepUp == 0f && StepDown == 0f;

        public HeightInfluenceWeights(float same, float stepUp, float stepDown)
        {
            Same = same;
            StepUp = stepUp;
            StepDown = stepDown;
        }

        public void NormalizeLegal(byte influencer, out float same, out float up, out float down)
        {
            same = Same;
            up = influencer < TileHeightRules.MaxLayer ? StepUp : 0f;
            down = influencer > 0 ? StepDown : 0f;
            var sum = same + up + down;
            if (sum <= 0f)
            {
                same = 1f;
                up = 0f;
                down = 0f;
                return;
            }

            same /= sum;
            up /= sum;
            down /= sum;
        }
    }
}
