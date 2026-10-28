using System.Collections.Generic;
using UnityEngine;
using GemTD.Gameplay;

namespace GemTD.UI
{
    /// <summary>Pools world-space enemy HP bars and binds one per live <see cref="EnemyView"/>.</summary>
    public sealed class EnemyHealthBarController : MonoBehaviour
    {
        [SerializeField] EnemyHealthBarView elementPrefab;
        [SerializeField] Transform poolParent;

        readonly List<EnemyHealthBarView> _pool = new List<EnemyHealthBarView>(32);

        GameCompositionRoot _root;

        void Awake()
        {
            if (elementPrefab == null)
                Debug.LogError("EnemyHealthBarController: elementPrefab is not assigned.", this);
            if (poolParent == null)
                Debug.LogError("EnemyHealthBarController: poolParent is not assigned.", this);
        }

        public void Bind(GameCompositionRoot root)
        {
            _root = root;
        }

        void LateUpdate()
        {
            if (_root == null || elementPrefab == null)
                return;

            var count = _root.EnemyViewCount;
            EnsurePool(count);

            var cam = Camera.main;
            for (var i = 0; i < count; i++)
            {
                var bar = _pool[i];
                bar.Bind(_root.GetEnemyViewAt(i));
                bar.Tick(cam);
            }

            for (var i = count; i < _pool.Count; i++)
            {
                var extra = _pool[i];
                extra.Bind(null);
                if (extra.gameObject.activeSelf)
                    extra.gameObject.SetActive(false);
            }
        }

        void EnsurePool(int count)
        {
            var parent = poolParent != null ? poolParent : transform;
            while (_pool.Count < count)
            {
                var bar = Instantiate(elementPrefab, parent);
                bar.gameObject.SetActive(false);
                _pool.Add(bar);
            }
        }
    }
}
