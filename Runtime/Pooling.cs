using System.Collections.Generic;
using UnityEngine;
// https://github.com/eudendeew/Pooling-System/wiki/Pooling
// ANDRES

/// <summary>
/// Use this to spawn GameObjects that will be used frequently.
/// <para></para>
/// Use <see cref="GetFromPool(GameObject, Vector3, Quaternion)"/> instead of
/// Instantiate().
/// <para></para>
/// Use <see cref="SendToPool"/> instead of
/// Destroy().
/// </summary>
public static class Pooling
{
    // :: Console Log Settings
    public static readonly bool LOG_MESSAGE = false;
    public static readonly bool LOG_WARNINGS = true;
    public static readonly bool LOG_ERRORS = true;

    #region Pooling System Methods
    // All the pools
    private static Dictionary<GameObject, Pool<Transform>> pools = new Dictionary<GameObject, Pool<Transform>>();

    public enum Category
    {
        Projectiles, Enemies, VisualEffects,
    }

    /// <summary>
    /// Spawns the prefabs then deactivates them ready to be used.
    /// <para></para>
    /// <b>NOTE</b>: This will call Awake() and Start() on the spawned objects 
    /// but it DOES NOT call <see cref="IPoolable.OnPoolSpawn"/> nor 
    /// <see cref="IPoolable.OnPoolUnSpawn"/>. 
    /// </summary>
    /// <param name="prefab">Prefab to pool.</param>
    /// <param name="quantity">One is the minimum.</param>
    public static GameObject[] Preload(GameObject prefab, int quantity)
    {
        quantity = Mathf.Max(quantity, 1); // Clamp value
        BeginDictionaryOrNewPool(prefab, quantity);
        // Make an array to grab the objects we're about to pre-spawn.
        GameObject[] obs = new GameObject[quantity];
        for (int i = 0; i < quantity; i++)
        {
            obs[i] = pools[prefab].PopFromPool(Vector3.zero, Quaternion.identity).gameObject;
        }
        for (int i = 0; i < quantity; i++)
        {
            pools[prefab].PushToPool(obs[i].transform, false); // Push without calling recycle event
        }
        return obs;
    }

    /// <summary>
    /// Gets one inactive object from a pool then activates it. If
    /// the object doesn't exist, a new pool will be created.
    /// This will call <see cref="IPoolable.OnPoolSpawn"/>
    /// </summary>
    /// <param name="prefab">Find this object on any pool</param>
    /// <param name="pos">Position world space.</param>
    /// <param name="rot">Rotation.</param>
    public static GameObject GetFromPool(GameObject prefab, Vector3 pos, Quaternion rot)
    {
        BeginDictionaryOrNewPool(prefab, 1);
        GameObject obj = pools[prefab].PopFromPool(pos, rot, true).gameObject;
        return obj;
    }

    /// <summary>
    /// Gets one inactive object from a pool then activates it. If
    /// the object doesn't exist, a new pool will be created.
    /// This will call <see cref="IPoolable.OnPoolSpawn"/>
    /// Additionally it will assign to a category.
    /// <para></para>
    /// <b>NOTE</b>: Categories are marked as <see langword="DontDestroyOnLoad"/>
    /// </summary>
    /// <param name="prefab">Prefab.</param>
    /// <param name="pos">Position.</param>
    /// <param name="rot">Rotation.</param>
    /// <param name="category">Category.</param>
    public static GameObject GetFromPool(GameObject prefab, Vector3 pos, Quaternion rot, Category category)
    {
        BeginDictionaryOrNewPool(prefab, 1);
        GameObject obj = pools[prefab].PopFromPool(pos, rot, true).gameObject;
        obj.transform.SetParent(ScenePools.GetTransform(category));
        return obj;
    }

    /// <summary>
    /// Collects the object to its pool then deactivates it, ready for next use.
    /// If it doesn't have a pool, it will be destroyed.
    /// This will call <see cref="IPoolable.OnPoolUnSpawn"/>
    /// </summary>
    public static void SendToPool(GameObject obj)
    {
        if (pools.ContainsKey(obj))
        {
            pools[obj].PushToPool(obj.transform, true);
        }
        else
        {
            PoolMember member = obj.GetComponent<PoolMember>();
            if (member)
            {
                member.OnRecycleToPool();
                member.ParentPool.PushToPool(obj.transform, false);
                return;
            }
            // - Not even a member
            if (LOG_WARNINGS)
            {
                Debug.LogWarning("Object '" + obj.name + "' wasn't spawned from a pool. Destroying it instead.");

            }
            Object.Destroy(obj);
        }
    }

    // ================
    private static void BeginDictionaryOrNewPool(GameObject prefab = null, int qty = 1)
    {
        if (prefab == null)
        {
            if (LOG_ERRORS)
            {
                Debug.LogError("[Pooling] Prefab referenced was null. Cannot create or find a pool.");
            }
            return;
        }
        if (pools.ContainsKey(prefab) == false)
        {
            pools[prefab] = new Pool<Transform>(prefab.transform, qty);
            if (LOG_MESSAGE)
            {
                Debug.Log("[Pooling] New pool for: " + prefab.name + " has been created");
            }
        }
    }
    #endregion

    #region PoolMember Helper Component
    /// <summary>
    /// Component added on runtime to objects used by the pooling system,
    /// used to identify parent Pool, and call events in IPoolable
    /// </summary>
    [HelpURL("https://github.com/eudendeew/Pooling-System/wiki/Pooling.PoolMember")]
    public class PoolMember : MonoBehaviour
    {
        public PoolBase ParentPool { private set; get; }
        public bool usesInterface;
        private IPoolable[] poolInterfaces;

        /// Used from Pool declaration
        public void Initialize(PoolBase parentPool)
        {
            ParentPool = parentPool;
            SearchInterfaces();
        }

        /// <summary>
        /// Searchs scripts on this GameObject that use <see cref="IPoolable"/>
        /// Call this if you add a component on runtime that uses <see cref="IPoolable"/>.
        /// </summary>
        public IPoolable[] SearchInterfaces()
        {
            poolInterfaces = GetComponentsInChildren<IPoolable>();
            usesInterface = poolInterfaces.Length > 0;
            return poolInterfaces;
        }

        /// <summary>
        /// Calls every <see cref="IPoolable.OnPoolSpawn"/> on this
        /// Game Object.
        /// </summary>
        public void OnDeployFromPool()
        {
            if (usesInterface)
            {
                for (int i = 0; i < poolInterfaces.Length; i++)
                {
                    poolInterfaces[i].OnPoolSpawn();
                }
            }
        }

        /// <summary>
        /// Calls every <see cref="IPoolable.OnPoolUnSpawn"/> on this
        /// Game Object.
        /// </summary>
        public void OnRecycleToPool()
        {
            if (usesInterface)
            {
                for (int i = 0; i < poolInterfaces.Length; i++)
                {
                    poolInterfaces[i].OnPoolUnSpawn();
                }
            }
        }
    }
    #endregion

    #region Scene Pools
    private static class ScenePools
    {
        private static Dictionary<string, Transform> nameTransformPairs;
        private static GameObject rootGmObj;

        public static Transform GetTransform(Category poolName)
        {
            // - Initialize if needed
            if (nameTransformPairs == null) { Initialize(); }
            string name = poolName.ToString().ToUpper();
            if (nameTransformPairs.ContainsKey(name))
            {
                return nameTransformPairs[name]; // Send reference
            }
            // - Create object then send reference
            if (rootGmObj == null)
            {
                return null;
            }
            GameObject newCategory = new GameObject(name);
            newCategory.transform.SetParent(rootGmObj.transform);
            nameTransformPairs.Add(name, newCategory.transform);
            return newCategory.transform;
        }

        private static void Initialize()
        {
            nameTransformPairs = new Dictionary<string, Transform>();
            rootGmObj = new GameObject("POOLS");
            Object.DontDestroyOnLoad(rootGmObj);
        }
    }
    #endregion
}