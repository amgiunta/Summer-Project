using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic component for triggering events in-game.
/// </summary>
public class Trigger : MonoBehaviour {
    /// <summary>
    /// The different possible states of the switch.
    /// </summary>
    public enum TriggerState { Active, Inactive };
    /// <summary>
    /// The current state of the switch.
    /// </summary>
    [Tooltip("The current state of the switch.")]
    public TriggerState state;

    [Tooltip("Trigger an event when a rigidbody passes through this object.")]
    public bool triggerOnEnter = false;
    [Tooltip("Only trigger once.")]
    public bool oneShot = false;

    /// <summary>
    /// Actions for this component to trigger.
    /// </summary>
    public UnityEngine.Events.UnityEvent OnActivate;
    /// <summary>
    /// Actions to trigger when the button becomes inactive.
    /// </summary>
    [Tooltip("Actions to trigger when the button becomes inactive.")]
    public UnityEngine.Events.UnityEvent OnDeactivate;

    /// <summary>
    /// Amount of times triggered.
    /// </summary>
    public static int triggerCount;

    private void Start() {
        if (state == TriggerState.Inactive) {
            Deactivate();
        }
        else {
            Activate();
        }
    }

    /// <summary>
    /// Activate all the actions set on this trigger.
    /// </summary>
    public virtual void Activate() {
        // Do not trigger actions if trigger is OneShot and already triggered.
        if (oneShot && triggerCount >= 1) { return; }

        // trigger all actions
        OnActivate.Invoke();

        state = TriggerState.Active;

        // Increment the trigger count
        triggerCount++;
    }

    public virtual void Deactivate() {
        // trigger all actions
        OnDeactivate.Invoke();
        state = TriggerState.Inactive;
    }

    protected virtual void OnTriggerEnter2D(Collider2D other) {
        if (other.gameObject.CompareTag("Player") && triggerOnEnter) { Activate(); }
    }

    protected virtual void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Player") && triggerOnEnter) { Deactivate(); }
    }
}
