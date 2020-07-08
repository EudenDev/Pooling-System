# Pooling System

**Version: [Jul 2020] v0.4.0 preview**

Author: Andres Maldonado [euden96](https://github.com/eudendeew)  
Original Author: Martin [quill18](https://github.com/quill18)   
Based on this code https://gist.github.com/quill18/5a7cfffae68892621267

Give credit to me as well as the original author if you feel so. :)

INSTALL
-----------

**Unity 2019.4 and above**
- In Unity, open Window/Package Manager
- Select the **+** button at the top left
- Select **Add package from git URL...**
- Paste in `https://github.com/euden96/Pooling-System.git`

**Unity 2019.3 and earlier**
- In your Unity Project, open up the manifest.json file located in your Packages folder
- below ```"dependencies": {``` add the line ```"com.euden.pooling-system": "https://github.com/euden96/Pooling-System.git",```
On Unity, Window/Package Manager, Add from git URL... paste this:


GENERAL INFO
-----------
    
Pooling System, this includes as of Jun 2019:

* [Pooling.cs](../../wiki/Pooling):     
    * [PoolMember](../../wiki/Pooling.PoolMember)
    * [ScenePools](../../wiki/Pooling.ScenePools)
    * [Category(enum)](../../wiki/Pooling.Category)
* [PoolBase.cs]
    * [Pool\<Component>](../../wiki/Pooling.Pool)
* [IPoolable.cs (Interface)](../../wiki/IPoolable)
* [ParticleSystemPooling.cs](../../wiki/ParticleSystemPooling)
* [SendToPoolTimer.cs](../../wiki/SendToPoolTimer)

Full documentation available on the [wiki section](../../wiki/Home).  
Old Unity Package download available on the [releases section](../../releases).

For this and more projects to come you can send donations via PayPal <3

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=GESR8D97KNWRE&source=url)


BASIC USAGE
-----------
    
Instead of: `Instantiate(yourPrefab, position, rotation);`

Use: `Pooling.GetFromPool(yourPrefab, position, rotation);`

Instead of: `Destroy(yourGameObject);`

Use: `Pooling.SendToPool(yourGameObject);`

* Trying SendToPool() on not pooled objects will call Destroy() instead, a message
    on the console should confirm this.
* The pool will resize if a new instance is needed. To reduce the impact
    use Pooling.Preload() on Start.
* Destroying an object with delay is not yet supported. e.g. Destroy(yourGO, 3f);

ADVANCED USAGE
-----------
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

### PRELOAD OBJECTS
Can be used in the beginning of the level to reduce the cost of creating
a new instance of your prefabs. Use:

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
 

SUPER ADVANCED USAGE
-----------    
### GENERIC POOLS
Another way to create Pools is with Pooling.Pool:
One big advantage is that unreferenced pools will be collected by the GC.
Intead of accumulating in the main dictionnary inside Pooling.
```csharp 
public class ExampleClass : MonoBehaviour 
{
    public MyScript scriptReference;
    public GameObject prefab;
    private Pooling.Pool<MyScript> objectPool;
    
    void Start() {
        // - Preloaded Pool
        objectPool = new Pooling.Pool<MyScript>(scriptReference, 10, true)
        // - No preloading
        objectPool = new Pooling.Pool<MyScript>(prefab.GetComponent<MyScript>());
    }
    
    public MyScript SpawnVFX(Vector3 position, Quaternion rotation) {
        return objectPool.PopFromPool(position, rotation, true);
    }
}
```
### Constructors
```csharp
// Generic version.
myScriptPool = new Pooling.Pool<MyScript>(prefab.GetComponent<MyScript>());
```

### Pooling.Pool Methods
More in the wiki [Pooling.Pool](../../wiki/Pooling.Pool)
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

// Push To Pool Lastest
myOwnPool.PushToPoolLastest(true);

// Push to pool all active objects.
myScriptPool.PushToPoolAll(true);
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

## Particle Sytem Pooling
This is a handy component to repool a particle system, like VFXs.
Add the component to your particle system, read the tooltip on the component if you need help.
This component forces loop to false and stopAction to callback.

## Send To Pool Timer
Calls Pooling.SendToPool(GameObject) after a set duration. The duration is reset every new spawn.

## POSSIBLE ISSUES
- Using generic version Pooling.Pool\<T> then trying Pooling.SendToPool() will not work and **will destroy your object**.

*(Not known but it might happen)*
- When changing a scene, the objects are destroyed and their reference too, for now
there is no way to dealloacate a Pool. Except with manually managed Pools.
- An error may occur after destroying a pooled object and trying GetFromPool or PopFromPool.
