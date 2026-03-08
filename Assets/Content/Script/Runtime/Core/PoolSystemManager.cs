using System.Collections.Generic;
using UnityEngine;

public class PooledObject : MonoBehaviour
{
    public GameObject prefab;
}

public class PoolSystemManager : MonoBehaviour
{
    public static PoolSystemManager Instance { get; private set; }

    private Transform _poolRoot;
    private readonly Dictionary<GameObject, Queue<GameObject>> _pools = new Dictionary<GameObject, Queue<GameObject>>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        var go = new GameObject("PoolRoot");
        go.transform.SetParent(transform);
        _poolRoot = go.transform;
    }

    private Queue<GameObject> GetOrCreateQueue(GameObject prefab)
    {
        if (!_pools.TryGetValue(prefab, out var queue))
        {
            queue = new Queue<GameObject>();
            _pools[prefab] = queue;
        }
        return queue;
    }

    public GameObject Get(GameObject prefab)
    {
        if (prefab == null) return null;
        var queue = GetOrCreateQueue(prefab);
        GameObject instance;
        if (queue.Count > 0)
        {
            instance = queue.Dequeue();
            if (instance == null) return Get(prefab);
        }
        else
        {
            instance = Instantiate(prefab);
            var tag = instance.GetComponent<PooledObject>();
            if (tag == null) tag = instance.AddComponent<PooledObject>();
            tag.prefab = prefab;
        }
        instance.transform.SetParent(null);
        instance.SetActive(true);
        return instance;
    }

    public T Get<T>(T prefab) where T : Component
    {
        if (prefab == null) return null;
        var go = Get(prefab.gameObject);
        return go != null ? go.GetComponent<T>() : null;
    }

    public void Return(GameObject instance)
    {
        if (instance == null) return;
        var tag = instance.GetComponent<PooledObject>();
        if (tag != null && tag.prefab != null)
        {
            Return(instance, tag.prefab);
            return;
        }
        Destroy(instance.gameObject);
    }

    public void Return(GameObject instance, GameObject prefab)
    {
        if (instance == null || prefab == null) return;
        instance.SetActive(false);
        instance.transform.SetParent(_poolRoot);
        var tag = instance.GetComponent<PooledObject>();
        if (tag == null) tag = instance.AddComponent<PooledObject>();
        tag.prefab = prefab;
        GetOrCreateQueue(prefab).Enqueue(instance);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0) return;
        var queue = GetOrCreateQueue(prefab);
        for (int i = 0; i < count; i++)
        {
            var instance = Instantiate(prefab);
            instance.transform.SetParent(_poolRoot);
            instance.SetActive(false);
            var tag = instance.GetComponent<PooledObject>();
            if (tag == null) tag = instance.AddComponent<PooledObject>();
            tag.prefab = prefab;
            queue.Enqueue(instance);
        }
    }

    public void Clear(GameObject prefab)
    {
        if (prefab == null || !_pools.TryGetValue(prefab, out var queue)) return;
        while (queue.Count > 0)
        {
            var go = queue.Dequeue();
            if (go != null) Destroy(go);
        }
    }

    public void ClearAll()
    {
        foreach (var queue in _pools.Values)
        {
            while (queue.Count > 0)
            {
                var go = queue.Dequeue();
                if (go != null) Destroy(go);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}
