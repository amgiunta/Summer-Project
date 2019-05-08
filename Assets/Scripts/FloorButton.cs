using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Floor Button is an abstraction of a Trigger object that allows the triggering of events using a mass-based switch in-game.
/// </summary>
[RequireComponent(typeof(Animator))]
public class FloorButton : Trigger {

    /// <summary>
    /// The different possible states of the switch.
    /// </summary>
    public enum ButtonState { Active, Inactive };
    /// <summary>
    /// The current state of the switch.
    /// </summary>
    [Tooltip("The current state of the switch.")]
    public ButtonState state;
    [Tooltip("The minimum mass needed to activate the button.")]
    public float minimumMass;
    [Tooltip("Can the player trigger the switch?")]
    public bool playerCanTrigger;
    /// <summary>
    /// A list of rigidbodies currently colliding with this object.
    /// </summary>
    [HideInInspector]
    public List<Rigidbody2D> bodies = new List<Rigidbody2D>();
    /// <summary>
    /// Actions to trigger when the button becomes inactive.
    /// </summary>
    [Tooltip("Actions to trigger when the button becomes inactive.")]
    public UnityEngine.Events.UnityEvent onDeactivate;

    /// <summary>
    /// The Animator attached to this object.
    /// </summary>
    Animator animator;

    private void Start()
    {
        // Get the Animator attached to this object.
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Toggle the button state between active and inactive.
    /// </summary>
    private void ToggleButton() {
        // If the total mass is greater than the minimum mass, Activate the button.
        if (TotalMass() > minimumMass) { ActivateButton(); }
        // Otherwise, deactivate the button.
        else { DeactivateButton(); }
    }

    /// <summary>
    /// Activate the button.
    /// </summary>
    private void ActivateButton() {
        // Set the 'Active' bool on the animator to true.
        animator.SetBool("Active", true);
        // Set the button's current state to Active.
        state = ButtonState.Active;

        // Invoke activation actions
        TripTrigger();
    }

    /// <summary>
    /// Deactivate the button.
    /// </summary>
    private void DeactivateButton() {
        // Set the 'Active' bool on the animator to false.
        animator.SetBool("Active", false);
        // Set the current state of the button to inactive.
        state = ButtonState.Inactive;

        // Invoke deactivation actions.
        onDeactivate.Invoke();
    }

    /// <summary>
    /// Calculate the mass of all objects on this button.
    /// </summary>
    /// <returns>The total mass</returns>
    private float TotalMass() {
        // Create float total that is 0.
        float total = 0;

        // For each body in the list of bodies:
        foreach (Rigidbody2D body in bodies) {
            // Increment total by the mass of the body.
            total += body.mass;
        }

        return total;
    }

    /// <summary>
    /// Add a Rigidbody2D to the list of bodies on this button.
    /// </summary>
    /// <param name="body">The body to add.</param>
    private void AddBody(Rigidbody2D body) {
        // If the body is not already in the list of bodies, add it to the list.
        if (!bodies.Contains(body)) { bodies.Add(body); }
    }

    /// <summary>
    /// Remove a Rigidbody2D from the list of bodies on this button.
    /// </summary>
    /// <param name="body">The body to remove</param>
    private void RemoveBody(Rigidbody2D body) {
        // If the body is in the list of bodies, remove it.
        if (bodies.Contains(body)) { bodies.Remove(body); }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        // If the tag of the colliding object is "Prop" or "Player",
        if (other.transform.CompareTag("Prop") || other.transform.CompareTag("Player"))
        {
            // Create Rigidbody2D body that is the rigidbody attached to the colliding object.
            Rigidbody2D body = other.gameObject.GetComponent<Rigidbody2D>();
            // Add the body to the list of bodies.
            AddBody(body);

            // Try to toggle the button.
            ToggleButton();
        }
        /*
        else if (other.transform.CompareTag("Player") && playerCanTrigger) {
            Rigidbody2D body = other.gameObject.GetComponent<Rigidbody2D>();
            AddBody(body);

            ToggleButton();
        }
        */
        
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        // If the tag of the colliding object is "Prop" or "Player",
        if (other.transform.CompareTag("Prop") || other.transform.CompareTag("Player"))
        {
            // Create Rigidbody2D body that is the rigidbody attached to the colliding object.
            Rigidbody2D body = other.gameObject.GetComponent<Rigidbody2D>();
            // Remove the body from the list of bodies.
            RemoveBody(body);

            // Try to toggle the button.
            ToggleButton();
        }
        /*
        else if (other.transform.CompareTag("Player") && playerCanTrigger)
        {
            Rigidbody2D body = other.gameObject.GetComponent<Rigidbody2D>();
            RemoveBody(body);

            ToggleButton();
        }
        */
    }
}
