// ANDRES

// ** IPoolable should be used by a MonoBehaviour and preferably
// be on the root of the prefab, not on any child game object.

/// <summary>
/// Interface with events from the Pooling System, use this
/// if you want to execute code at spawn/unspawn from the pool.
/// </summary>
public interface IPoolable
{
    /// <summary>
    /// Called when the object is taken out from the pool
    /// </summary>
    void OnPoolSpawn();

    /// <summary>
    /// Called before the object needs to go back to the pool
    /// </summary>
    void OnPoolUnSpawn();

}
