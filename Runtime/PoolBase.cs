using System.Collections.Generic;
using UnityEngine;
using static Pooling;

public abstract class PoolBase
{
    public abstract void PushToPool(Component obj, bool useCallback);
}

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
        Prefab = prefab;
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
            if (Prefab == null && LOG_ERRORS)
            {
                Debug.LogError("Original prefab has been destroyed. Cannot create instances anymore.");
            }

            obj = UnityEngine.Object.Instantiate(Prefab, pos, rot);
            obj.name = Prefab.name + "(" + nextId++ + ")" + "::PoolMember";
            poolMember = obj.gameObject.AddComponent<PoolMember>();
            poolMember.Initialize(this);
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
