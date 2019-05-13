using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Basic component for non-static platforms that can move at runtime and react to the player.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class DynamicPlatform : MonoBehaviour {
    public bool useGravity;
    public bool rotate;
    public float rotationSpeed;
    /// <summary>
    /// Align the rotation of this platform to the rotation of the player.
    /// </summary>
    public bool orientWithPlayer;
    [Tooltip("Friction value for this platform.")]
    public float friction;

    /// <summary>
    /// Current acting player in the scene.
    /// </summary>
    [HideInInspector]
    public PlayerNetwork player;
    /// <summary>
    /// The rigidbody attached to this object.
    /// </summary>
    [HideInInspector]
    new public Rigidbody2D rigidbody;
    /// <summary>
    /// CAn the player stand on this object?
    /// </summary>
    public bool standable = true;

    /// <summary>
    /// The velocity of this platform's movement. DON'T USE Rigidbody2D.velocity!
    /// </summary>
    [HideInInspector]
    public Vector3 velocity;

    /// <summary>
    /// The position of this platform on the previous frame.
    /// </summary>
    private Vector3 _lastPosition;

    private Vector3 _gravityDirection = -Vector3.up;

    private Quaternion _absoluteRotation = Quaternion.identity;

	// Use this for initialization
	void Start () {
        // Find the player and store a reference to it.
        FindPlayer();
        // Get the rigidbody on this object.
        rigidbody = GetComponent<Rigidbody2D>();
	}

    private void Update()
    {
        Orient();
    }

    // Update is called once per frame
    public virtual void FixedUpdate () {
        // Calculate the velocity of this object and store it.
        CalcVelocity();
        ApplyGravity();

        // Last position is the current position.
        _lastPosition = transform.position;
	}

    public void OrientWithPlayer(bool value) {
        orientWithPlayer = value;
    }

    public void Orientation(float angle) {
        _absoluteRotation = Quaternion.Euler(0, 0, angle);

        Debug.Log("Changed the orientation to " + angle);
    }

    /// <summary>
    /// Find the current acting player in the scene and store it in this object.
    /// </summary>
    public virtual void FindPlayer() {
        // Find the object in the scene with the tag "Player" and store a reference in DynamicPlatform.player. 
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerNetwork>();

        // If player is not found, throw error.
        if (!player) { Debug.LogError("Could not locate player! Check to see if the player is in the scene, tagged as 'Player', and has the PlayerNetwors (or some derivative behavior) attached.", this); }
    }

    /// <summary>
    /// Set the rotation of the current object. Reletive to player if orientWithPlayer is true.
    /// </summary>
    public virtual void Orient() {
        // If player is not null and orientWithPlayer is true, set this object's rotation to the player's rotation.
        if (orientWithPlayer && player)
        {
            _gravityDirection = -player.transform.up;

            if (rotate)
            {
                Quaternion targetRotation = _absoluteRotation * player.transform.localRotation;
                transform.localRotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        }
        // If not, set this object's rotation to be the identity.
        else {
            _gravityDirection = -Vector3.up;
            transform.localRotation = Quaternion.Lerp(transform.rotation, _absoluteRotation, Time.deltaTime * rotationSpeed);
        }
    }

    protected virtual void ApplyGravity() {
        if (!useGravity) { return; }

        rigidbody.AddForce(_gravityDirection * rigidbody.mass);
        Debug.DrawRay(transform.position, _gravityDirection * rigidbody.mass, Color.red);
    }

    /// <summary>
    /// Calculates the velocity of this object based on the current position and the position last frame.
    /// </summary>
    protected virtual void CalcVelocity() {
        // V = (current position - last position) / length of 1 frame in seconds.
        velocity = (transform.position - _lastPosition) /Time.deltaTime;
    }

    protected virtual void OnCollisionEnter2D(Collision2D other) {
        // Set the parent of the collision object to this object.
        if (standable && other.transform.CompareTag("Player") || other.transform.CompareTag("Prop")) { other.transform.parent = transform; }
    }

    protected virtual void OnCollisionExit2D(Collision2D other) {
        // Reset the collision object's parent to null when they are no longer colliding.
        if (standable && other.transform.CompareTag("Player") || other.transform.CompareTag("Prop"))
        {
            other.transform.parent = null;
            other.gameObject.GetComponent<Rigidbody2D>().velocity += (Vector2)velocity;
            Debug.Log("object left platform.", other.transform);
        }
    }
}
