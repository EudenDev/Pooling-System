using UnityEngine;
// ANDRES

/// <summary>
/// Send a Particle System to its pool after all particles have died.<para></para>
/// NOTE: This forces stop action to Callback on the particle system.
/// </summary>
[HelpURL("https://github.com/eudendeew/Pooling-System/wiki/ParticleSystemPooling")]
[AddComponentMenu("Effects/Particle System Pooling")]
public class ParticleSystemPooling : MonoBehaviour, IPoolable
{
    //:: Settings
    [Header("If ParticleSystem is child of a GameObject reference it:")]
    [Tooltip("Optional reference to the parent of this Particle System")]
    public Transform parent;
    [Header("Options")]
    [Tooltip("If false, loop will be set to false on Awake.\n" +
    "If true, you must manually Stop() the Particle System to send it to its pool.")]
    public bool stopManually;

    [Tooltip("If false this component only works on instances from Pooling." +
    " Set this to true to run on objects that are already in the scene" +
    " or will be created with Instantiate().")]
    public bool alwaysRun;
    private bool usesPooling;

    private void Awake()
    {
        ParticleSystem = GetComponent<ParticleSystem>();
        if (ParticleSystem == null)
        { // So it's clear for the user
            if (Pooling.LOG_ERRORS)
                Debug.LogError("No Particle System in: " + gameObject.name);
            return;
        }
        ParticleSystem.Stop();
        var mainModule = ParticleSystem.main;
        mainModule.stopAction = ParticleSystemStopAction.Callback;
        mainModule.loop = stopManually; // false by default
        if (mainModule.playOnAwake) ParticleSystem.Play();
    }

    // This is called from a Particle System callback
    // After all particlees have died
    public void OnParticleSystemStopped()
    {
        if (!alwaysRun && !usesPooling)
            return;
        // If it doesn't come from a pool it will be destroyed instead.
        Pooling.SendToPool(parent ? parent.gameObject : gameObject);
    }

    public void OnPoolSpawn()
    {
        usesPooling = true;
    }

    public void OnPoolUnSpawn()
    {
        
    }

    /// <summary>
    /// Read-Only: Returns the Particle System referenced in this component.
    /// </summary>
    public ParticleSystem ParticleSystem { get; private set; }
}