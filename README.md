# Pooling System
    
Documentation for the pooling system, this includes as of May 2019:
* Pooling.cs: Pool\<T>, PoolMember, ScenePools, Category(enum)
* [IPoolable.cs (Interface)](#calling-functions-on-pool-spawn--unspawn)
* [ParticleSystemPooling.cs](#particle-sytem-pooling)

**Lastest Version: [May2019] v3.3.3
See the Changelog at the end of this document.**

Author: Andres Maldonado -- Original Author: Martin "quill18"
Based on this code https://gist.github.com/quill18/5a7cfffae68892621267

Give credit to me as well as the original author if you feel so. :)

## BASIC USAGE
    
Instead of: `Instantiate(yourPrefab, position, rotation);`

Use: `Pooling.GetFromPool(yourPrefab, position, rotation);`

Instead of: `Destroy(yourGameObject);`

Use: `Pooling.SendToPool(yourGameObject);`

* Trying SendToPool() on not pooled objects will call Destroy() instead, a message
    on the console should confirm this.
* The pool will resize if a new instance is needed. To reduce the impact
    use Pooling.Preload() on Start.
* Destroying an object with delay is not yet supported. e.g. Destroy(yourGO, 3f);

## ADVANCED USAGE
### CALLING FUNCTIONS ON POOL SPAWN / UNSPAWN
This could be used for resetting an enemy health, or leaving something behind
when an enemy disappears.
In your script use the interface IPoolable, implement it like this:

```csharp
public class Enemy : MonoBehavior, IPoolable {

    <...your code...>
    
    public void OnPoolSpawn() {
        // - Code to be run after this object is spawned and enabled.
    }
    public void OnPoolUnSpawn() {
        // - Code to be run before this object is unspawned and disabled.
    }
}
```
**Scripts that use IPoolable should be on the root of the prefab,
not on any child object.**

### PRELOAD OBJECTS
Could be used in the beginning of the game to reduce the cost of creating
a new instance of your prefab. Use:

```csharp
Pooling.Preload(prefabReference, 8);
```

* Returns an array of gameObjects.
* Cannot use Pooling Categories.
* Pools are always dynamic, they will resize if a new instance is needed.

### POOL CATEGORIES
Objects that are used between levels can be organized inside categories, all
marked as DontDestroyOnLoad. To do so use:

```csharp
Pooling.GetFromPool(prefab, pos, rot, Pooling.Category.Projectiles);
```

* Current existing categories are: Projectiles, Enemies, VisualEffects.
* Again, objects spawned with this method are marked as **DontDestroyOnLoad**.

### KNOW IF A GAME OBJECT COMES FROM A POOL
You can use GetComponent<PoolMember>() to check if the object comes from the
pooling system. Inside you can have access to some advanced methods.

* There are some exposed functions that is better not to touch them because
    they are used for calling the interfaces and reference its parent pool.

## SUPER ADVANCED USAGE
    
### MANUALLY MANAGED POOLS
Another way to create Pools is with Pooling.Pool:
One big advantage is that unreferenced pools will be collected by the GC.
Intead of accumulating in the main dictionnary inside Pooling.
```csharp 
public class ExampleClass : MonoBehaviour {
    public GameObject prefab;
    private Pooling.Pool myOwnPool;
    private Pooling.Pool<MyScript> objectPool;
    
    void Start() {
        // - Basic Pool
        myOwnPool = new Pooling.Pool(prefab)
        // OR - Preloaded Pool
        myOwnPool = new Pooling.Pool(prefab, 10, true)
        // - Generic version
        objectPool = new Pooling.Pool<MyScript>(prefab.GetComponent<MyScript>());
    }
    
    public void SpawnVFX(Vector3 position, Quaternion rotation) {
        myOwnPool.PopFromPool(position, rotation, true);
    }
}
```
### Constructors
```csharp
// Basic - doesn't spawn anything.
myOwnPool = new Pooling.Pool(prefab);

// Preloaded, spawns the objects already deactivated.
myOwnPool = new Pooling.Pool(prefab, 10, true);

// Generic version.
myScriptPool = new Pooling.Pool<MyScript>(prefab.GetComponent<MyScript>());
```

### Pooling.Pool Methods
```csharp
// Simple Spawn
myOwnPool.PopFromPool(position, rotation);

// Spawn and call IPoolable.OnPoolSpawn()
myOwnPool.PopFromPool(position, rotation, true);

// Stock reference
GameObject gmObj = myOwnPool.PopFromPool(position, rotation, true);

// Send back
myOwnPool.PushToPool(gmObj);

// Send back and call IPoolable.OnPoolUnSpawn()
myOwnPool.PushToPool(gmObj, true);

// Send back last spawned
myOwnPool.PushToPoolLastest();

// It can also call IPoolable.OnPoolUnSpawn()
myOwnPool.PushToPoolLastest(true);
```

### Generic Version Extra Methods

```csharp
// Get List of active objects.
List<T> Actives { private set; get; }

// Push to pool all active objects.
myScriptPool.PushToPoolAll(true);
public void PushToPoolAll(bool useCallback)

// Pushes back to pool the most recent object.
public void PushToPoolLastest(bool useCallback)
```

### PoolMember component methods
```csharp
// Use this if you add on runtime, a component that uses IPoolable.
// The list of receivers needs to be manually updated.
Pooling.PoolMember pm = GetComponent<Pooling.PoolMember>();
pm.SearchInterfaces(); // It updates interfaces to be called.

// This forces all the calls of IPoolable.OnPoolSpawn and IPoolable.OnPoolUnSpawn
pm.OnDeployFromPool();
pm.OnRecycleToPool();
```

### Get Category transform
Since they are at the same level, just use: 
`pooledGmObject.transform.parent;`

## Particle Sytem Pooling
This is a handy component to repool a particle system, like VFXs.
Add the component to your particle system, read the tooltip on the component if you need help.
This component force loop to false and stopAction to callback.

## POSSIBLE ISSUES
- Using generic version Pooling.Pool\<T> then trying Pooling.SendToPool() will not work and **will destroy your object**.

*(Not known but it might happen)*
- When changing a scene, the objects are destroyed and their reference too, for now
there is no way to dealloacate a Pool. Except with manually managed Pools.
- An error may occur after destroying a pooled object and trying GetFromPool or PopFromPool.

## CHANGELOG
[May2019] v3.3.3
+ Options to turn off logs.

[May2019] v3.3.1
+ Particle Pooling Sytem Manually Stop option, and tooltips

[May2019] v3.3.0
+ Pooling.Pool class has been deleted, use Pooling.Pool\<Transform> instead.
+ Text fixes

[may2019] v3.2.0
+ Pooling.Pool now uses Component instead of MonoBehaviour

[apr2019] v3.1.0
+ Pooling.Pool generic version.
+ Some nullchecks
+ Documentation only available here from now

[apr2019] v3.0.2
+ SendBackToPool refactored to ParticleSystemPooling.
+ Added Summaries
+ Null prefab reference error
+ Text fixes
    
[apr2019] v3.0.0
+ Manually managed pools
+ Documentation added
+ More summaries and functions
    
[apr2019] v2.6.2
+ Send Back to Pool nullcheck
+ Text Fixes
+ PoolMember summaries and SearchInterfaces method

[mar2019] v2.6.0 // ANDRES
+ Pooling.Preload: Returns an array of gameObjects.
+ Documentation text fixes

[fev2019] v2.5.5 // ANDRES
+ More Documentation
- IPoolable has been simplified
- PoolMemberhas been simplified

v2.5 [fev2019] // ANDRES
- Removed ScenePools from project
+ Integrated Categories
+ Addded documentation
+ Clean Up

v2.1 [jan2019] // ANDRES
+ Removed debug logs and minor fixes

v2.0 [dec2018] // ANDRES
+ ScenePools.cs, organizes spawn object into transforms in the scene
+ Added Pools Presets
+ Multiple IPoolable compatibility

v1.6 [aug2018] // ANDRES
+ Mayor Fixes to all the system
+ Commentatries and summaries

v1.5 [aug2018] // ANDRES
+ IPoolable corrections
+ Particle System Pooling, SendBackToPool.cs

v1.2 [jul2018] // ANDRES
+ IPoolable interface compatibility

v1 [jun2018] // Andres - Martin "quill18"
+ Simple Pooling, Pool Member
