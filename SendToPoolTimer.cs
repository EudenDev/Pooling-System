using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// ANDRES
/// <summary>
/// Calls <see cref="Pooling.SendToPool(GameObject)"/> after a set duration.
/// The duration is reset every new spawn.
/// </summary>
[HelpURL("https://github.com/eudendeew/Pooling-System/wiki/SendToPoolTimer")]
public class SendToPoolTimer : MonoBehaviour, IPoolable
{
    [Tooltip("Time in seconds before unspawning this object.")]
    public float duration = 3f;
    private float currentTime;

    [Header("Options")]
    [Tooltip("Use unscaled delta time in the timer.")]
    public bool useUnscaledTime;

    [Tooltip("If false this component only works on instances from Pooling." +
        " Set this to true to run on objects that are already in the scene" +
        " or will be created with Instantiate().")]
    public bool alwaysRun;
    private bool usesPooling;

    public void OnPoolSpawn()
    {
        usesPooling = true;
        currentTime = duration;
    }

    public void OnPoolUnSpawn()
    {
        // Nothing here ( ·_)·
    }

    Pooling.Pool<InfoBar> barsPool;

    void Start()
    {
        currentTime = duration;
    }

    void Update()
    {
        if (!alwaysRun && !usesPooling)       
            return;
        // -
        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        currentTime = Mathf.MoveTowards(currentTime, 0f, dt);
        if (Mathf.Abs(currentTime) < float.Epsilon)
        {
            Pooling.SendToPool(gameObject);
        }
    }
}
