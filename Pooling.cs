using System.Collections.Generic;
using UnityEngine;
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
static public class Pooling
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
    static public GameObject[] Preload(GameObject prefab, int quantity)
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
    static public GameObject GetFromPool(GameObject prefab, Vector3 pos, Quaternion rot)
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
    static public GameObject GetFromPool(GameObject prefab, Vector3 pos, Quaternion rot, Category category)
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
    static public void SendToPool(GameObject obj)
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

    public abstract class PoolBase
    {
        public abstract void PushToPool(Component obj, bool useCallback);
    }

    #region Generic Pool Class
    /// <summary>
    /// Use to define a Pool of one type of Component child.<para></para>
    /// STATIC <see cref="Pooling"/> FUNCTIONS MAY NOT BE COMPATIBLE WITH THIS
    /// </summary>
    public class Pool<T> : PoolBase where T : Component
    {
        /// <summary>
        /// Gets the original referenced prefab.
        /// </summary>
        public T Prefab { private set; get; }
        //public bool hasLimit; // Not yet...

        private readonly int initialQuantity;
        private int nextId = 1;
        private Dictionary<T, PoolMember> objectMemberPairs = new Dictionary<T, PoolMember>();

        public List<T> Actives { private set; get; }
        private Stack<T> inactives = new Stack<T>();

        private T lastOutput;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pool{T}"/> class.
        /// </summary>
        /// <param name="prefab">Prefab for cloning.</param>
        /// <param name="initialQuantity">Initial quantity to allocate.</param>
        /// <param name="preload">NOTE: This will call Awake().</param>
        public Pool(T prefab, int initialQuantity = 1, bool preload = false)
        {
            this.Prefab = prefab;
            this.initialQuantity = initialQuantity;
            inactives = new Stack<T>(initialQuantity);
            Actives = new List<T>();
            if (preload) { Load(); }
        }

        /// <summary>
        /// Sends an inactive or a new GameObject to the scene.
        /// </summary>
        /// <returns>The lastest inactive GameObject.</returns>
        /// <param name="pos">Position.</param>
        /// <param name="rot">Rotation.</param>
        /// <param name="useCallback">Set to <c>true</c> to call <see cref="IPoolable.OnPoolSpawn"/>.</param>
        public T PopFromPool(Vector3 pos, Quaternion rot, bool useCallback = false)
        {
            T obj;
            PoolMember poolMember;
            if (inactives.Count == 0)
            {
                // - New object needed from pool
                obj = Object.Instantiate<T>(Prefab, pos, rot);
                obj.name = Prefab.name + "(" + (nextId++) + ")" + "::PoolMember";
                poolMember = obj.gameObject.AddComponent<PoolMember>();
                poolMember.Initialize((PoolBase)this);
                objectMemberPairs.Add(obj, poolMember);
            }
            else
            { // if there's objects available in this pool
                obj = inactives.Pop();
                // If the obj doesnt exist anymore, call the next one in the stack
                if (obj == null)
                {
                    objectMemberPairs.Remove(lastOutput); // ** Untested
                    return PopFromPool(pos, rot); // ** Posible error on network ids
                }
                poolMember = objectMemberPairs[obj];
            }
            obj.transform.position = pos;
            obj.transform.rotation = rot;
            obj.gameObject.SetActive(true);
            if (useCallback)
            {
                poolMember.OnDeployFromPool();
            }
            lastOutput = obj;
            Actives.Add(obj);
            return obj;
        }

        /// <summary>
        /// Send a gameobject back into its pool. <para></para>
        /// NOTE : Only Components that came from this pool can be repooled.
        /// </summary>
        /// <param name="obj">Object to repool.</param>
        /// <param name="useCallback">Set to <c>true</c> to call <see cref="IPoolable.OnPoolUnSpawn"/>.</param>
        public override void PushToPool(Component obj, bool useCallback)
        {
            if (obj == null)
            {
                if (LOG_ERRORS)
                {
                    Debug.LogError("PushToPool received null as 'obj' parameter.");
                }
                return;
            }
            //
            T cObj = (T)obj;
            if (objectMemberPairs.ContainsKey(cObj))
            {
                if (useCallback)
                {
                    objectMemberPairs[cObj].OnRecycleToPool();
                }
                obj.gameObject.SetActive(false);
                Actives.Remove(cObj);
                inactives.Push(cObj); // Only registered can be pushed
            }
            else
            {
                if (LOG_ERRORS)
                {
                    Debug.LogError(obj.name + " cannot be pushed to a Pool" +
                        " because it doesn't come from one.");
                }
            }
        }

        /// <summary>
        /// Pushes back to pool the most recent object.
        /// </summary>
        /// <param name="useCallback">Set to <c>true</c> to call <see cref="IPoolable.OnPoolUnSpawn"/>.</param>
        public void PushToPoolLastest(bool useCallback)
        {
            if (Actives.Count == 0) { return; }
            if (Actives[Actives.Count - 1])
            {
                PushToPool(Actives[Actives.Count - 1], useCallback);
            }
        }
        /// <summary>
        /// Repools all active objects
        /// </summary>
        /// <param name="useCallback">Set to <c>true</c> to call <see cref="IPoolable.OnPoolUnSpawn"/>.</param>
        public void PushToPoolAll(bool useCallback)
        {
            while (Actives.Count > 0)
            {
                T obj = Actives[Actives.Count - 1];
                PushToPool(obj, useCallback);
            }
        }

        // Sub class version of Preload
        private void Load()
        {
            T[] obs = new T[initialQuantity];
            for (int i = 0; i < initialQuantity; i++)
            {
                obs[i] = PopFromPool(Vector3.zero, Quaternion.identity);
            }
            for (int i = 0; i < initialQuantity; i++)
            {
                PushToPool(obs[i], false);
            }
            lastOutput = null; //To avoid PushToLastest with undefined object.
        }
    }
    #endregion

    #region PoolMember Helper Component
    /// <summary>
    /// Component added on runtime to objects used by the pooling system,
    /// used to identify parent Pool, and call events in IPoolable
    /// </summary>
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