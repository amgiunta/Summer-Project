using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FloorButton : Trigger {

    public enum ButtonState { Active, Inactive };
    public ButtonState state;
    public float minimumMass;
    public bool playerCanTrigger;
    public List<Rigidbody2D> bodies = new List<Rigidbody2D>();
    public UnityEngine.Events.UnityEvent onDeactivate;

    Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void ToggleButton() {
        if (TotalMass() > minimumMass) { ActivateButton(); }
        else { DeactivateButton(); }
    }

    private void ActivateButton() {
        animator.SetBool("Active", true);
        state = ButtonState.Active;

        TripTrigger();
    }

    private void DeactivateButton() {
        animator.SetBool("Active", false);
        state = ButtonState.Inactive;

        onDeactivate.Invoke();
    }

    private float TotalMass() {
        float total = 0;
        foreach (Rigidbody2D body in bodies) {
            total += body.mass;
        }

        return total;
    }

    private void AddBody(Rigidbody2D body) {
        if (!bodies.Contains(body))
        {
            bodies.Add(body);
        }
    }

    private void RemoveBody(Rigidbody2D body) {
        if (bodies.Contains(body))
        {
            bodies.Remove(body);
        }
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.transform.CompareTag("Prop"))
        {
            Rigidbody2D body = other.gameObject.GetComponent<Rigidbody2D>();
            AddBody(body);

            ToggleButton();
        }
        else if (other.transform.CompareTag("Player") && playerCanTrigger) {
            Rigidbody2D body = other.gameObject.GetComponent<Rigidbody2D>();
            AddBody(body);

            ToggleButton();
        }

        
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (other.transform.CompareTag("Prop"))
        {
            Rigidbody2D body = other.gameObject.GetComponent<Rigidbody2D>();
            RemoveBody(body);

            ToggleButton();
        }
        else if (other.transform.CompareTag("Player") && playerCanTrigger)
        {
            Rigidbody2D body = other.gameObject.GetComponent<Rigidbody2D>();
            RemoveBody(body);

            ToggleButton();
        }
    }

}
