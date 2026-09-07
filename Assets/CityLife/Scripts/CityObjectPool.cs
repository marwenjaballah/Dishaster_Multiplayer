using System.Collections.Generic;
using UnityEngine;

namespace CityLife
{
    /// <summary>
    /// Tag-based generic object pool for city life agents (cars, pedestrians).
    /// Pre-warms all pools on initialization to avoid runtime GC allocations.
    /// Auto-expands if a pool runs dry or if instances were destroyed.
    /// </summary>
    public class CityObjectPool : MonoBehaviour
    {
        private static CityObjectPool _instance;
        public static CityObjectPool Instance
        {
            get
            {
                if (_instance == null)
                    _instance = FindAnyObjectByType<CityObjectPool>();
                return _instance;
            }
            private set => _instance = value;
        }

        // tag → inactive object queue
        private readonly Dictionary<string, Queue<GameObject>> _pools   = new();
        // tag → source prefab (used for auto-expand)
        private readonly Dictionary<string, GameObject>        _prefabs = new();
        // tag → pool parent transform
        private readonly Dictionary<string, Transform>         _parents = new();

        private void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Pre-warm a pool with <paramref name="size"/> instances.
        /// </summary>
        public void CreatePool(string tag, GameObject prefab, int size, Transform parent)
        {
            if (prefab == null) return;

            // Clear any existing queue for this tag to start fresh
            if (_pools.TryGetValue(tag, out var existingQueue))
            {
                while (existingQueue.Count > 0)
                {
                    var item = existingQueue.Dequeue();
                    if (item != null)
                    {
                        if (Application.isPlaying) Destroy(item);
                        else DestroyImmediate(item);
                    }
                }
            }

            _prefabs[tag] = prefab;
            _parents[tag] = parent;

            var queue = new Queue<GameObject>(size);
            for (int i = 0; i < size; i++)
            {
                var obj = Instantiate(prefab, parent);
                obj.name  = $"{tag}_{i:000}";
                obj.SetActive(false);
                queue.Enqueue(obj);
            }

            _pools[tag] = queue;
            Debug.Log($"[CityObjectPool] Pool '{tag}' created ({size} objects pre-warmed).");
        }

        /// <summary>
        /// Retrieve an object from the pool, activate it and place it in the world.
        /// Auto-expands by 1 if the pool is empty or if pooled items were destroyed.
        /// </summary>
        public GameObject Get(string tag, Vector3 position, Quaternion rotation)
        {
            if (!_pools.TryGetValue(tag, out var queue))
            {
                Debug.LogError($"[CityObjectPool] Pool '{tag}' does not exist!");
                return null;
            }

            GameObject obj = null;
            while (queue.Count > 0 && obj == null)
            {
                obj = queue.Dequeue();
            }

            if (obj == null)
            {
                // Auto-expand pool or replace destroyed instance
                if (_prefabs.TryGetValue(tag, out var prefab) && prefab != null)
                {
                    Transform parent = _parents.TryGetValue(tag, out var p) ? p : null;
                    obj = Instantiate(prefab, parent);
                    obj.name = $"{tag}_auto";
                }
                else
                {
                    Debug.LogError($"[CityObjectPool] No prefab registered for pool '{tag}'!");
                    return null;
                }
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Deactivate <paramref name="obj"/> and return it to the pool.
        /// </summary>
        public void Return(string tag, GameObject obj)
        {
            if (obj == null) return;
            obj.SetActive(false);
            if (_pools.TryGetValue(tag, out var queue))
                queue.Enqueue(obj);
            else
            {
                if (Application.isPlaying) Destroy(obj);
                else DestroyImmediate(obj);
            }
        }

        /// <summary>How many objects are currently sitting idle in a pool.</summary>
        public int IdleCount(string tag)
            => _pools.TryGetValue(tag, out var q) ? q.Count : 0;

        /// <summary>Clears and destroys all pooled objects.</summary>
        public void ClearAll()
        {
            foreach (var kvp in _pools)
            {
                while (kvp.Value.Count > 0)
                {
                    var obj = kvp.Value.Dequeue();
                    if (obj != null)
                    {
                        if (Application.isPlaying) Destroy(obj);
                        else DestroyImmediate(obj);
                    }
                }
            }
            _pools.Clear();
            _prefabs.Clear();
            _parents.Clear();
        }
    }
}
