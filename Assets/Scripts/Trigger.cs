using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic component for triggering events in-game.
/// </summary>
public class Trigger : MonoBehaviour {
    [Tooltip("Trigger an event when a rigidbody passes through this object.")]
    public bool triggerOnEnter = false;
    [Tooltip("Only trigger once.")]
    public bool oneShot = false;

    /// <summary>
    /// Actions for this component to trigger.
    /// </summary>
    public UnityEngine.Events.UnityEvent OnActivate;

    /// <summary>
    /// Amount of times triggered.
    /// </summary>
    public static int triggerCount;

    /// <summary>
    /// Activate all the actions set on this trigger.
    /// </summary>
    public void Activate() {
        // Do not trigger actions if trigger is OneShot and already triggered.
        if (oneShot && triggerCount >= 1) { return; }

        // trigger all actions
        OnActivate.Invoke();
        // Increment the trigger count
        triggerCount++;
    }

    protected void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player") && triggerOnEnter) { Activate(); }
    }
}
