using UnityEngine;
using UnityEngine.UI;
using GemTD.Gameplay.Enemies;

namespace GemTD.UI
{
    /// <summary>World-space HP bar with a translucent shield overlay. Billboarded to the camera.</summary>
    public sealed class EnemyHealthBarView : MonoBehaviour
    {
        [SerializeField] Image hpFill;
        [SerializeField] Image shieldFill;
        [SerializeField] float worldYOffset = 0.65f;

        EnemyView _enemyView;

        void Awake()
        {
            if (hpFill == null)
                Debug.LogError("EnemyHealthBarView: hpFill is not assigned.", this);
            if (shieldFill == null)
                Debug.LogError("EnemyHealthBarView: shieldFill is not assigned.", this);
        }

        public void Bind(EnemyView enemyView)
        {
            _enemyView = enemyView;
        }

        public void Tick(Camera cam)
        {
            var runtime = _enemyView != null ? _enemyView.Runtime : null;
            if (runtime == null || !runtime.IsAlive)
            {
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
                return;
            }

            var isBoss = runtime.Definition != null && runtime.Definition.IsBoss;
            if (!EnemyHealthBarMath.ShouldShow(runtime.Hp, runtime.MaxHealth, runtime.ShieldHp, runtime.ShieldMax, isBoss))
            {
                if (gameObject.activeSelf)
                    gameObject.SetActive(false);
                return;
            }

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            transform.position = _enemyView.transform.position + Vector3.up * worldYOffset;
            if (cam != null)
                transform.rotation = cam.transform.rotation;

            EnemyHealthBarMath.ComputeFills(
                runtime.Hp,
                runtime.MaxHealth,
                runtime.ShieldHp,
                out var hpAmount,
                out var shieldAmount);

            if (hpFill != null)
                hpFill.fillAmount = hpAmount;

            if (shieldFill != null)
            {
                shieldFill.fillAmount = shieldAmount;
                var showShield = shieldAmount > 0f;
                if (shieldFill.enabled != showShield)
                    shieldFill.enabled = showShield;
            }
        }
    }
}
