using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// Wrapper for gameobjects that are holdable.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class HoldableObject : MonoBehaviour {

    /// <summary>
    /// Align the rotation of this object with the rotation of the player.
    /// </summary>
    [Tooltip("Align the rotation of this object with the rotation of the player.")]
    public bool orientWithPlayer;
    /// <summary>
    /// Should the gravity of this object always be inverted?
    /// </summary>
    [Tooltip("Should the gravity of this object always be inverted?")]
    public bool invertUp;

    /// <summary>
    /// The rigidbody attached to this object
    /// </summary>
    [HideInInspector]
    new public Rigidbody2D rigidbody;
    /// <summary>
    /// The collider attached to this object.
    /// </summary>
    [HideInInspector]
    new public Collider2D collider;

    /// <summary>
    /// A reference to the current acting player script in the scene.
    /// </summary>
    private PlayerNetwork player;
    /// <summary>
    /// The relative up direction of this object.
    /// </summary>
    private Vector3 up;

    protected virtual void Start () {
        // Find the player in the scene and store a reference to it.
        FindPlayer();
        // Find the rigidbody component attached to this object.
        rigidbody = GetComponent<Rigidbody2D>();
        // Find the collider object attached to this object.
        collider = GetComponent<Collider2D>();

        // Initialize up to be the default up vector.
        up = Vector3.up;
    }

    protected virtual void FixedUpdate() {
        // Apply force of gravity along the relative up vector.
        ApplyGravity();
    }

    protected virtual void Update() {
        // Set the rotation of this object accordingly.
        Orient();
    }

    /// <summary>
    /// Sets the rotation of this object. Based on value of orientWithPlayer and invertUp.
    /// </summary>
    protected virtual void Orient() {
        // If not inverted, and orientWithPlayer is true, set the up vector to be the up vector of the player.
        if (orientWithPlayer && player && !invertUp) { up = player.transform.up; }
        // Or, if orientWithPlayer is true, and invertUp is true, set the relative up vector to be the negative up vector of the player.
        else if (orientWithPlayer && player && invertUp) { up = -player.transform.up; }
        // Or else,
        else {
            // If inverting, set the relative up vector to be the negative default up vector.
            if (invertUp) { up = -Vector3.up; }
            // Otherwise, the up vector is the default up vector.
            else { up = Vector3.up; }
        }
    }

    /// <summary>
    /// Apply to force of gravity along the relative up vector.
    /// </summary>
    protected virtual void ApplyGravity() {
        // Create Vector direction that is the negative relative up vector
        Vector3 direction = -up;
        // Multiply the direction by: the negative force of gravity multiplied by the mass of this object.
        direction *= (-Physics2D.gravity.y * rigidbody.mass);
        // Add direction to this object as a force.
        rigidbody.AddForce(direction);
    }

    /// <summary>
    /// Locates the player within the scene and saves a reference to the script.
    /// </summary>
    public virtual void FindPlayer() {
        // Set the player to the Player Network on the object in the scene with the tag "Player".
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerNetwork>();

        // If the player is not found, throw an error.
        if (!player) { Debug.LogError("Could not locate player! Check to see if the player is in the scene, tagged as 'Player', and has the PlayerNetwork (or some derivative behavior) attached.", this); }
    }

    void OnDrawGizmos() {
        // If the game is playing, Draw a ray from the position of this object with a magnitude of this object's velocity.
        if (Application.isPlaying)
            Debug.DrawRay(transform.position, rigidbody.velocity);
    }
}
