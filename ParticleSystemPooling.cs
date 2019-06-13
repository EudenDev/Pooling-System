using UnityEngine;
// ANDRES

/// <summary>
/// Send a Particle System to its pool after all particles have died.<para></para>
/// NOTE: This forces stop action to Callback on the particle system.
/// </summary>
[AddComponentMenu("Effects/Particle System Pooling")]
public class ParticleSystemPooling : MonoBehaviour
{
    //:: Settings
    [Header("If ParticleSystem is child of a GameObject reference it:")]
    [Tooltip("Optional reference to the parent of this Particle System")]
    public Transform parent;
    [SerializeField]
    [Tooltip("If false, loop will be set to false on Awake.\n" +
    "If true, you must manually Stop() the Particle System to send it to its pool.")]
    private bool stopManually = false;

    void Awake()
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
        // If it doesn't come from a pool it will be destroyed instead.
        Pooling.SendToPool(parent ? parent.gameObject : gameObject);
    }

    /// <summary>
    /// Read-Only: Returns the Particle System referenced in this component.
    /// </summary>
    public ParticleSystem ParticleSystem { get; private set; }
}