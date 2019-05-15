﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RelativeGravity))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerControlerV2 : MonoBehaviour
{
    public LayerMask groundLayers;
    [Space]

    public float acceleration;
    public float strafeAcceleration;
    public float maxSpeed;
    public float maxFallSpeed;
    public float maxReach;
    public Vector2 throwForce;

    [Space]
    public Bounds groundCheckArea;

    [HideInInspector]
    public bool isGrounded = true;
    [HideInInspector]
    public HoldableObject prop;

    private Transform sprite;
    private Transform hand;

    [HideInInspector]
    public Rigidbody2D rigidbody;
    RelativeGravity relativeGravity;

    float _moveDir;
    bool _jump;

    // Start is called before the first frame update
    void Start()
    {
        sprite = transform.Find("Sprite");
        hand = sprite.Find("Hand");

        rigidbody = GetComponent<Rigidbody2D>();
        relativeGravity = GetComponent<RelativeGravity>();
    }

    // Update is called once per frame
    void Update()
    {
        _moveDir = Input.GetAxis("Horizontal");

        if (Input.GetButtonDown("Use")) { GrabNearestProp(); }
        if (Input.GetButtonDown("Jump")) { _jump = true; }
    }

    private void FixedUpdate()
    {
        GroundCheck();
        Move(_moveDir);
        //if (_jump) { Jump(); }
    }

    public void Move(float direction) {
        direction = Mathf.Clamp(direction, -1, 1);

        if (isGrounded)
        {
            rigidbody.AddRelativeForce(new Vector2(direction, 0) * acceleration * rigidbody.mass);
        }
        else
        {
            rigidbody.AddRelativeForce(new Vector2(direction, 0) * strafeAcceleration * rigidbody.mass);
        }

        CapVelocity();
    }

    public void CapVelocity() {
        Vector2 relativeVelocity = relativeGravity.RelativeVelocity();
        float xDir = Mathf.Abs(relativeVelocity.x) / relativeVelocity.x;
        float yDir = Mathf.Abs(relativeVelocity.y) / relativeVelocity.y;

        if (Mathf.Abs(relativeVelocity.x) > maxSpeed) { relativeVelocity.x = maxSpeed * xDir; }
        if (Mathf.Abs(relativeVelocity.y) > maxFallSpeed) { relativeVelocity.y = maxFallSpeed * yDir; }

        relativeGravity.SetRelativeVelocity(relativeVelocity);
    }

    private void GroundCheck() {
        Collider2D[] colliders = Physics2D.OverlapBoxAll(transform.position + groundCheckArea.center, groundCheckArea.size, transform.eulerAngles.z, groundLayers);

        //if (colliders == null) { isGrounded = false; }
        if (colliders.Length == 0) { isGrounded = false; }
        else { isGrounded = true; }

    }

    /// <summary>
    /// Get the closest Holdable Object in the scene to the player, and pick it up.
    /// </summary>
    private void GrabNearestProp()
    {
        // If the player is already holding something, drop it.
        if (prop) { ReleaseHeldProp(); }

        // Create an array of Collider2D props that is the colliders of all props within reach of this player.
        Collider2D[] props = Physics2D.OverlapCircleAll(transform.position, maxReach, 1 << LayerMask.NameToLayer("Prop"));

        // If there are no props, stop algorythm.
        if (props.Length == 0) { return; }
        else { Debug.Log(props.Length); }

        // Create Holdable Object nearest that is the first prop.
        HoldableObject nearest = props[0].GetComponent<HoldableObject>();

        // For every prop in the list of props,
        foreach (Collider2D prop in props)
        {
            // If the distance between the prop and the player is less than the nearest prop and the player,  
            if (Vector2.Distance(prop.transform.position, transform.position) < Vector2.Distance(nearest.transform.position, transform.position))
            {
                // Set nearest to the prop.
                nearest = prop.GetComponent<HoldableObject>();
            }
        }

        // If nearest does not exist, stop the algorythm.
        if (nearest == null) { return; }

        // Asynchronously move the prop to the hand position.
        StartCoroutine(MovePropToHand(0.5f, nearest));
    }

    /// <summary>
    /// Drop any prop that is currently being held.
    /// </summary>
    private void ReleaseHeldProp()
    {
        // If the player is not holding anything, stop the algorythm.
        if (!prop) { return; }

        // Set the parent of the held object to null.
        prop.transform.parent = null;

        // Create Rigidbody2D prop body that is the rigidbody attached to the held object.
        Rigidbody2D propBody = prop.GetComponent<Rigidbody2D>();
        // Set the body type of prop body to dynamic.
        propBody.bodyType = RigidbodyType2D.Dynamic;
        // Set the held object reference to null.
        prop = null;
    }

    /// <summary>
    /// Throws the prop that is currently being held.
    /// </summary>
    private void ThrowHeldProp()
    {
        // If the player is not holding anything, stop the algorythm.
        if (!prop) { return; }

        // Create float x direction that is 1.
        float xDirection = 1;

        // If the x-scale of this object is positive, x direction is 1.
        if (transform.localScale.x > 0) { xDirection = 1; }
        // Otherwise, x direction is -1.
        else { xDirection = -1; }

        // Create Rigidbody2D prop body that is the rigidbody attached to the held prop.
        Rigidbody2D propBody = prop.GetComponent<Rigidbody2D>();
        // Release the held prop.
        ReleaseHeldProp();
        // Set the velocity of the prop body to be the velocity of the player.
        propBody.velocity = rigidbody.velocity;
        // Create Vector3 throw vector where: x is the throw force by the x direction, y is the throw force, z is 0.
        Vector3 throwVector = new Vector3(throwForce.x * xDirection, throwForce.y, 0f);
        // Add the throw vector rotated by the player's rotation.
        propBody.AddForce(transform.rotation * throwVector);
    }

    /// <summary>
    /// Asynchronous function for moving an object to the position of the player's hand.
    /// </summary>
    /// <param name="animationTime">Duration of the movement (in seconds).</param>
    /// <param name="prop">A reference to the Holdable Object to move.</param>
    /// <returns>An enumerator</returns>
    private IEnumerator MovePropToHand(float animationTime, HoldableObject prop)
    {
        // Set the held object reference to the prop.
        this.prop = prop;
        // Create a Rigidbody2D prop body that is the rigidbody attached to the prop.
        Rigidbody2D propBody = prop.GetComponent<Rigidbody2D>();
        // Set the body type of the prop body to kinematic.
        propBody.bodyType = RigidbodyType2D.Kinematic;
        // Set the velocity of the prop body to zero.
        propBody.velocity = Vector2.zero;

        // Loop the following for every step of stride length equal to 1 frame in seconds where: float t is between 0 and animation time.
        for (float t = 0; t < animationTime; t += Time.deltaTime)
        {
            // Set the position of the prop to the linear interpolation at position t on the line between the position of the prop, and the position of this object.
            prop.transform.position = Vector3.Lerp(prop.transform.position, hand.position, t);
            // Wait for 1 frame.
            yield return new WaitForEndOfFrame();
        }

        // Set the parent of the prop to the player's hand.
        prop.transform.parent = hand;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 groundBoxBotLeft = transform.rotation * groundCheckArea.min + transform.position;
        Vector2 groundBoxTopRight = transform.rotation * groundCheckArea.max + transform.position;
        Vector2 groundBoxTopLeft = transform.rotation * new Vector2(groundCheckArea.min.x, groundCheckArea.max.y) + transform.position;
        Vector2 groundBoxBotRight = transform.rotation * new Vector2(groundCheckArea.max.x, groundCheckArea.min.y) + transform.position;

        Debug.DrawLine(groundBoxBotLeft, groundBoxBotRight, Color.green);
        Debug.DrawLine(groundBoxBotLeft, groundBoxTopLeft, Color.green);
        Debug.DrawLine(groundBoxTopLeft, groundBoxTopRight, Color.green);
        Debug.DrawLine(groundBoxBotRight, groundBoxTopRight, Color.green);
    }
}
