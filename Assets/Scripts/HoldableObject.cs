using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
/// <summary>
/// Wrapper for gameobjects that are holdable.
/// </summary>
public class HoldableObject : MonoBehaviour {

    public bool orientWithPlayer;
    public bool invertUp;

    new public Rigidbody2D rigidbody;
    new public Collider2D collider;

    private PlayerNetwork player;
    private Vector3 up;

    protected virtual void Start () {
        FindPlayer();
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();

        up = Vector3.up;
    }

    protected virtual void FixedUpdate() {
        ApplyGravity();
    }

    protected virtual void Update() {
        Orient();
    }

    protected virtual void Orient() {
        if (orientWithPlayer && player && !invertUp) { up = player.transform.up; }
        else if (orientWithPlayer && player && invertUp) { up = -player.transform.up; }
        else {
            if (invertUp) { up = -Vector3.up; }
            else { up = Vector3.up; }
        }
    }

    protected virtual void ApplyGravity() {
        Vector3 direction = -up;
        direction *= (-Physics2D.gravity.y * rigidbody.mass);
        rigidbody.AddForce(direction);
    }

    public virtual void FindPlayer() {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerNetwork>();

        if (!player) { Debug.LogError("Could not locate player! Check to see if the player is in the scene, tagged as 'Player', and has the PlayerNetwork (or some derivative behavior) attached.", this); }
    }

    void OnDrawGizmos() {
        Gizmos.color = Color.yellow;

        if (Application.isPlaying)
            Debug.DrawRay(transform.position, rigidbody.velocity);
    }
}
