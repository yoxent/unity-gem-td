using System;
using System.Collections.Generic;
using UnityEngine;

namespace GemTD.Core
{
    /// <summary>
    /// Thin wrapper around <see cref="UnityEngine.Pool.ObjectPool{T}"/> for GameObject-backed views.
    /// </summary>
    public sealed class ViewObjectPool<T> where T : Component
    {
        readonly UnityEngine.Pool.ObjectPool<T> _pool;

        public ViewObjectPool(T prefab, Transform parent, int defaultCapacity = 16, int maxSize = 256)
        {
            if (prefab == null) throw new ArgumentNullException(nameof(prefab));

            _pool = new UnityEngine.Pool.ObjectPool<T>(
                createFunc: () =>
                {
                    var instance = UnityEngine.Object.Instantiate(prefab, parent);
                    instance.gameObject.SetActive(false);
                    return instance;
                },
                actionOnGet: item => item.gameObject.SetActive(true),
                actionOnRelease: item => item.gameObject.SetActive(false),
                actionOnDestroy: item =>
                {
                    if (item != null)
                        UnityEngine.Object.Destroy(item.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: defaultCapacity,
                maxSize: maxSize);
        }

        public T Get() => _pool.Get();

        public void Release(T item) => _pool.Release(item);

        public void Clear() => _pool.Clear();
    }

    /// <summary>
    /// Reusable list helper to avoid per-frame allocations.
    /// </summary>
    public static class ListPool<T>
    {
        static readonly Stack<List<T>> Pool = new();

        public static List<T> Get()
        {
            if (Pool.Count > 0)
            {
                var list = Pool.Pop();
                list.Clear();
                return list;
            }

            return new List<T>(32);
        }

        public static void Release(List<T> list)
        {
            if (list == null) return;
            list.Clear();
            Pool.Push(list);
        }
    }
}
