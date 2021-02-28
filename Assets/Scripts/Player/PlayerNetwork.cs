using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SocialPlatforms;

/// <summary>
/// A basic character controller that utilises the Character State Machine framework.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerNetwork : CharacterStateNetwork {
    public static PlayerNetwork player;

    public ContactFilter2D groundFilter;
    public float gravityScale;
    [Tooltip("Movement speed on the ground (m/s)")]
    public float acceleration;
    public float decceleration;
    public float strafeAcceleration;
    public float maxAngle;
    [Tooltip("Maximum fall speed (m/s)")]
    public float maxFallSpeed;
    [Tooltip("Maximum movement speed (m/s)")]
    public float maxSpeed;
    public float jumpForce;
    [Tooltip("Current health of the player.")]
    public float health;
    [Tooltip("Maximum allowed health for the player.")]
    public float maxHealth;
    [Tooltip("Minimum time (in seconds) allowed between jumps.")]
    public float jumpBufferTime;
    [Tooltip("Total time to run flip animation.")]
    public float flipTime;
    [Tooltip("Maximum distance from a holdable object the player can be to pick it up.")]
    public float maxReach;
    /// <summary>
    /// A relative position to move the player to when it flips.
    /// </summary>
    [Tooltip("A relative position to move the player to when it flips.")]
    public Vector2 flipOffset;
    /// <summary>
    /// A relative force vector to apply to the held object when thrown.
    /// </summary>
    [Tooltip("A relative force vector to apply to the held object when thrown.")]
    public Vector2 throwForce;

    [HideInInspector]
    public bool canFlip;

    /// <summary>
    /// Flag for when the player is on the ground.
    /// </summary>
    public bool isGrounded;
    /// <summary>
    /// A reference to the object currently being held by the player.
    /// </summary>
    public HoldableObjectV2 holding;

    new public Rigidbody2D rigidbody;
    new public CapsuleCollider2D collider;
    public CameraFollow mainCamera;

    [HideInInspector]
    public Vector2 localVelocity {
        get { 
            return transform.InverseTransformDirection(rigidbody.velocity);
        }
    }

    Vector2 _lastNormal;
    Vector2 _lastNormalPosition;

    public List<RaycastHit2D> closePoints;

    /// <summary>
    /// A reference to the hand of the player.
    /// </summary>
    Transform hand;

    #region State Names
    public PlayerDead dead;
    public PlayerIdle idle;
    public PlayerWalking walking;
    public PlayerJumping jumping;
    public PlayerRising rising;
    public PlayerFalling falling;
    public PlayerFlipping flipping;
    #endregion

    private Vector2 down;
    private float capsuleRadius {
        get {
            if (collider.direction == CapsuleDirection2D.Horizontal)
            {
                return collider.size.y / 2;
            }
            else {
                return collider.size.x / 2;
            }
        }
    }
    private float playerWidth {
        get {
            if (collider.direction == CapsuleDirection2D.Horizontal)
            {
                return collider.size.x - collider.size.y;
            }
            else {
                return collider.size.x;
            }
        }
    }
    private float playerHeight
    {
        get
        {
            if (collider.direction == CapsuleDirection2D.Horizontal)
            {
                return collider.size.y;
            }
            else
            {
                return collider.size.y - collider.size.x;
            }
        }
    }
    public int facing = 1;

    public void Awake()
    {
        if (player) {
            Destroy(player.gameObject);
        }

        player = this;
    }

    public void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<CapsuleCollider2D>();
        mainCamera = FindObjectOfType<CameraFollow>();
        hand = transform.Find("Sprite").Find("Hand");
        closePoints = new List<RaycastHit2D>();

        if (!hand) { Debug.LogError("No hand on player detected! Make sure there is a 'Hand' child under the player, and name it accordingly."); }

        down = -transform.up;

        CreateNetwork();
    }

    public override void Update()
    {
        base.Update();
        GroundCheck();

        Debug.DrawRay(transform.position, down * 10, Color.blue);

        if (Time.timeScale > 0) {
            if (Input.GetButtonDown("Use")) {
                if (activeState != flipping && activeState != dead) {
                    if (holding)
                    {
                        ReleaseHeldProp();
                    }
                    else
                    {
                        GrabNearestProp();
                    }
                }
            }
        }
        
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        if (hand.localPosition.x != facing) {
            hand.localPosition = Vector3.Lerp(hand.localPosition, new Vector2(facing, playerHeight), Time.deltaTime * acceleration);

            if (Mathf.Abs(hand.localPosition.x - facing) < Time.deltaTime * acceleration) {
                hand.localPosition = new Vector3(facing, playerHeight);
            }
        }

        Debug.DrawRay(transform.position, rigidbody.velocity, Color.red);

        if (activeState != flipping && activeState != dead)
        {
            // Add the force of grabity to this object on the relative up vector.
            ApplyGravity();
        }
    }

    /// <summary>
    /// Apply to force of gravity along the relative up vector.
    /// </summary>
    protected virtual void ApplyGravity()
    {
        if (isGrounded)
        {
            down = -CalculateNormal();
        }

        //transform.up = -down;
        //transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(Vector3.forward, normal), 5f * Time.deltaTime);
        //transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(Vector3.forward, normal), 5f * Time.deltaTime);
        //rigidbody.MoveRotation(Quaternion.LookRotation(Vector3.forward, normal));
        transform.rotation = Quaternion.LookRotation(Vector3.forward, -down);

        Vector3 direction = down;
        // Create Vector direction that is the negative relative up vector
        // Multiply the direction by: the negative force of gravity multiplied by the mass of this object.
        direction *= (-Physics2D.gravity.y * rigidbody.mass);
        // Add direction to this object as a force.
        if (Application.isPlaying)
            rigidbody.AddForce(direction * gravityScale);

        Debug.DrawRay(transform.position, direction * gravityScale, Color.red);
    }

    /// <summary>
    /// Initializes all the states in the network.
    /// </summary>
    private void CreateNetwork() {
        dead = new PlayerDead(this);
        idle = new PlayerIdle(this);
        walking = new PlayerWalking(this);
        jumping = new PlayerJumping(jumpBufferTime, 0.1f, this);
        rising = new PlayerRising(this);
        falling = new PlayerFalling(this, 1f);
        flipping = new PlayerFlipping(flipTime, flipOffset, this);

        idle.AddTransition(dead);
        idle.AddTransition(walking);
        idle.AddTransition(rising);
        idle.AddTransition(falling);
        idle.AddTransition(jumping);
        idle.AddTransition(flipping);

        walking.AddTransition(dead);
        walking.AddTransition(rising);
        walking.AddTransition(falling);
        walking.AddTransition(idle);
        walking.AddTransition(jumping);
        walking.AddTransition(flipping);

        jumping.AddTransition(dead);
        jumping.AddTransition(idle);
        jumping.AddTransition(rising);
        jumping.AddTransition(falling);

        rising.AddTransition(dead);
        rising.AddTransition(idle);
        rising.AddTransition(falling);
        rising.AddTransition(flipping);

        falling.AddTransition(dead);
        falling.AddTransition(idle);
        falling.AddTransition(rising);
        falling.AddTransition(flipping);

        flipping.AddTransition(walking);
        flipping.AddTransition(falling);

        activeState = idle;
    }

    /// <summary>
    /// Get the relative position of a point based off of the player's flipped orientation.
    /// </summary>
    /// <param name="offset">how far from the player the point is</param>
    /// <returns></returns>
    public Vector3 GetFlipPivot(Vector3 offset) {
        return transform.position + (transform.localRotation * offset);
    }

    public void Move(float acceleration, float direction) {
        if (direction == 0) { return; }

        Vector2 newLocalVel = localVelocity;

        if (localVelocity.x > 0 && direction < 0 || localVelocity.x < 0 && direction > 0) {
            newLocalVel = new Vector2(-localVelocity.x, localVelocity.y);
        }        

        if (Mathf.Abs(newLocalVel.x) < maxSpeed)
        {
            newLocalVel = newLocalVel + new Vector2(acceleration * direction, 0);
        }

        facing = (int) (Mathf.Abs(direction) / direction);

        rigidbody.velocity = transform.TransformDirection(newLocalVel);
    }

    /// <summary>
    /// Get the closest Holdable Object in the scene to the player, and pick it up.
    /// </summary>
    private void GrabNearestProp() {
        // If the player is already holding something, drop it.
        if (holding) { ReleaseHeldProp(); }

        // Create an array of Collider2D props that is the colliders of all props within reach of this player.
        Collider2D[] props = Physics2D.OverlapCircleAll(transform.position, maxReach, 1 << LayerMask.NameToLayer("Prop"));

        // If there are no props, stop algorythm.
        if (props.Length == 0) { return; }

        // Create Holdable Object nearest that is the first prop.
        HoldableObjectV2 nearest = props[0].GetComponent<HoldableObjectV2>();

        // For every prop in the list of props,
        foreach (Collider2D prop in props) {
            // If the distance between the prop and the player is less than the nearest prop and the player,  
            if (Vector2.Distance(prop.transform.position, transform.position) < Vector2.Distance(nearest.transform.position, transform.position)) {
                // Set nearest to the prop.
                nearest = prop.GetComponent<HoldableObjectV2>();
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
    private void ReleaseHeldProp() {
        // If the player is not holding anything, stop the algorythm.
        if (!holding) { return; }

        holding.onReleased.Invoke();

        // Set the parent of the held object to null.
        holding.transform.parent = null;

        // Create Rigidbody2D prop body that is the rigidbody attached to the held object.
        Rigidbody2D propBody = holding.GetComponent<Rigidbody2D>();
        // Set the body type of prop body to dynamic.
        propBody.bodyType = RigidbodyType2D.Dynamic;
        // Set the held object reference to null.
        holding = null;
    }

    /// <summary>
    /// Throws the prop that is currently being held.
    /// </summary>
    private void ThrowHeldProp() {
        // If the player is not holding anything, stop the algorythm.
        if (!holding) { return; }

        // Create Rigidbody2D prop body that is the rigidbody attached to the held prop.
        Rigidbody2D propBody = holding.GetComponent<Rigidbody2D>();
        // Release the held prop.
        ReleaseHeldProp();
        // Set the velocity of the prop body to be the velocity of the player.
        propBody.velocity = rigidbody.velocity;
        // Create Vector3 throw vector where: x is the throw force by the x direction, y is the throw force, z is 0.
        Vector3 throwVector = new Vector3(throwForce.x * facing, throwForce.y, 0f);
        // Add the throw vector rotated by the player's rotation.
        propBody.AddForce(transform.rotation * throwVector);
    }

    /// <summary>
    /// Asynchronous function for moving an object to the position of the player's hand.
    /// </summary>
    /// <param name="animationTime">Duration of the movement (in seconds).</param>
    /// <param name="prop">A reference to the Holdable Object to move.</param>
    /// <returns>An enumerator</returns>
    private IEnumerator MovePropToHand(float animationTime, HoldableObjectV2 prop) {

        prop.onPickedUp.Invoke();
        // Set the held object reference to the prop.
        holding = prop;
        // Create a Rigidbody2D prop body that is the rigidbody attached to the prop.
        Rigidbody2D propBody = prop.GetComponent<Rigidbody2D>();
        // Set the body type of the prop body to kinematic.
        propBody.bodyType = RigidbodyType2D.Kinematic;
        // Set the velocity of the prop body to zero.
        propBody.velocity = Vector2.zero;

        // Loop the following for every step of stride length equal to 1 frame in seconds where: float t is between 0 and animation time.
        for (float t = 0; t < animationTime; t += Time.deltaTime) {
            // Set the position of the prop to the linear interpolation at position t on the line between the position of the prop, and the position of this object.
            prop.transform.position = Vector3.Lerp(prop.transform.position, hand.position, t);
            prop.transform.rotation = Quaternion.Slerp(prop.transform.rotation, hand.rotation, t);
            // Wait for 1 frame.
            yield return new WaitForEndOfFrame();
        }

        prop.transform.position = hand.position;
        prop.transform.rotation = hand.rotation;

        // Set the parent of the prop to the player's hand.
        prop.transform.parent = hand;
    }

    /// <summary>
    /// Check if the player is on the ground.
    /// </summary>
    private void GroundCheck() {
        // Create a Collider2D collider that is any environment piece that is within a box around the player that is half the player's width, and 0.2 m tall.
        //Collider2D collider = Physics2D.OverlapBox(transform.position, new Vector2(transform.lossyScale.x/2, 0.2f), 0, groundFilter.layerMask);

        //Physics2D.CircleCast(transform.position + (playerHeight * transform.up), transform.lossyScale.x / 2, -transform.up, groundFilter, closePoints, playerHeight + 0.2f);
        Vector2 top = transform.position + (playerHeight * transform.up);

        RaycastHit2D leftFoot = Physics2D.Raycast(top - new Vector2(playerWidth / 2, 0), down, playerHeight + 0.2f, groundFilter.layerMask);
        RaycastHit2D rightFoot = Physics2D.Raycast(top + new Vector2(playerWidth / 2, 0), down, playerHeight + 0.2f, groundFilter.layerMask);

        closePoints = new List<RaycastHit2D> { leftFoot, rightFoot };

        Debug.DrawRay(transform.position + (playerHeight * transform.up), -transform.up * (playerHeight + 0.2f), Color.yellow);

        // If the collider exists, the player is grounded.
        if (leftFoot || rightFoot) { isGrounded = true; canFlip = true; }
        // Otherwise, the player is not grounded.
        else { isGrounded = false; }
    }

    private Vector2 CalculateNormal()
    {
        if (closePoints == null) { return Vector2.up; }
        if (closePoints.Count == 0) { return Vector2.up; }
        Vector2 normalSum = Vector2.zero;

        foreach (RaycastHit2D hitPoint in closePoints) {
            normalSum += hitPoint.normal;
            Debug.DrawRay(hitPoint.point, hitPoint.normal, Color.green);
        }

        //Debug.Break();

        //Vector2 normal = normalSum / closePoints.Count;

        if (Vector2.Angle(normalSum, transform.up) < maxAngle)
        {
            return normalSum;
        }
        else { return transform.up; }
    }

    private void OnDrawGizmosSelected()
    {
        //Gizmos.color = Color.yellow;
        //if (!collider) { collider = GetComponent<Collider2D>(); }
        //if (!rigidbody) { rigidbody = GetComponent<Rigidbody2D>(); }

        //Gizmos.DrawWireSphere(GetFlipPivot(flipOffset), GetFlipRadius());
        //Gizmos.DrawWireSphere(transform.position, maxReach);
        //Gizmos.color = Color.blue;
        //Gizmos.DrawWireCube(transform.position, new Vector2(transform.localScale.x / 2, 0.2f));
        //Gizmos.color = Color.red;
        //Vector2 normal = CalculateNormal();

        //Debug.DrawRay(transform.position, normal, Color.red);

        //ApplyGravity();

        ////if (Application.isPlaying) { Debug.DrawRay(transform.position, rigidbody.velocity, Color.red); }

        //Debug.DrawRay(transform.position, transform.up, Color.blue);
    }

    /// <summary>
    /// Get the relative velocity of the player.
    /// </summary>
    /// <returns></returns>
    public Vector3 LocalVelocity() {
        return transform.InverseTransformDirection(rigidbody.velocity);
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Danger")) { GameMaster.gameMaster.RestartLevel(); }
    }

    #region State Definitions
    /// <summary>
    /// The dead state of the player.
    /// </summary>
    public class PlayerDead : PlayerCharacterState
    {
        /// <summary>
        /// Creates a Player Dead state.
        /// </summary>
        /// <param name="network">Reference to the player's state machine.</param>
        public PlayerDead(PlayerNetwork network) : base("Player Dead", network) { }

        public override void OnStateEnter()
        {
            // Set the player's body type to kinematic.
            player.rigidbody.bodyType = RigidbodyType2D.Kinematic;
            // Restart the level.
            GameMaster.gameMaster.RestartLevel();
        }
    }
    public class PlayerIdle : PlayerCharacterState {
        public float dccelPerFrame;

        public PlayerIdle(PlayerNetwork network) : base("Player Idle", network) { }

        public override void OnStateEnter()
        {
            dccelPerFrame = player.decceleration * Time.deltaTime;
        }

        public override void Subject()
        {
            if (player.health <= 0) { Transition("Player Dead"); }
            else if (!player.isGrounded)
            {
                if (player.localVelocity.y > 0)
                {
                    Transition("Player Rising");
                }
                else
                {
                    Transition("Player Falling");
                }
            }
            else if (Input.GetAxis("Horizontal") != 0) { Transition("Player Walking"); }
            else if (Input.GetButtonDown("Jump")) { Transition("Player Jumping"); }
            else if (Input.GetButtonDown("Flip") && player.canFlip) { Transition("Player Flipping"); }
        }

        public override void FixedUpdate()
        {
            if (player.localVelocity.x != 0) {
                Vector2 newLocalVel;

                float direction = (player.localVelocity.x < 0) ? 1 : -1;

                if (Mathf.Abs(player.localVelocity.x) < dccelPerFrame)
                {
                    newLocalVel = new Vector2(0, player.localVelocity.y);
                }
                else {
                    newLocalVel = player.localVelocity + new Vector2(dccelPerFrame * direction, 0);
                }

                player.rigidbody.velocity = player.transform.TransformDirection(newLocalVel);
            }
        }
    }
    /// <summary>
    /// The walking state of the player.
    /// </summary>
    public class PlayerWalking : PlayerCharacterState {
        /// <summary>
        /// Creates a Player Walking state.
        /// </summary>
        /// <param name="network">Reference to the player's state machine.</param>
        public float acclPerFrame;

        public PlayerWalking(PlayerNetwork network) : base("Player Walking", network) { }

        public override void OnStateEnter()
        {
            acclPerFrame = player.acceleration * Time.deltaTime;
        }

        public override void Subject()
        {
            if (player.health <= 0) { Transition("Player Dead"); }
            else if (!player.isGrounded)
            {
                if (player.localVelocity.y > 0) { Transition("Player Rising"); }
                else { Transition("Player Falling"); }
            }
            else if (Input.GetAxisRaw("Horizontal") == 0) { Transition("Player Idle"); }
            else if (Input.GetButtonDown("Jump")) { Transition("Player Jumping"); }
            else if (Input.GetButtonDown("Flip") && player.canFlip) { Transition("Player Flipping"); }
        }

        public override void FixedUpdate()
        {
            // Create Vector2 movement where the x value is the horizontal input axis.
            float direction = Input.GetAxisRaw("Horizontal");

            player.Move(acclPerFrame, direction);
        }
    }
    /// <summary>
    /// The jumping state of the player.
    /// </summary>
    public class PlayerJumping : PlayerCharacterState {
        /// <summary>
        /// The maximum time to stay in this state.
        /// </summary>
        public float maxTime;
        public float minTime;
        /// <summary>
        /// Time elapsed in this state.
        /// </summary>
        float elapsedTime;

        /// <summary>
        /// Creates a Player Jumping state.
        /// </summary>
        /// <param name="maxTime">The jump buffer time. Time to stay in this state.</param>
        /// <param name="network">Reference to the player's state machine.</param>
        public PlayerJumping(float maxTime, float minTime, PlayerNetwork network) : base("Player Jumping", network) {
            this.maxTime = maxTime;
            this.minTime = minTime;
        }

        public override void Subject()
        {
            if (player.health <= 0f) { Transition("Player Dead"); }
            else if (elapsedTime > minTime)
            {
                if (elapsedTime < maxTime)
                {
                    if (player.isGrounded) { Transition("Player Idle"); }
                }
                else
                {
                    if (player.isGrounded) { Transition("Player Idle"); }
                    else if (player.localVelocity.y > 0) { Transition("Player Rising"); }
                    else { Transition("Player Falling"); }
                }
            }
        }

        public override void OnStateEnter()
        {
            elapsedTime = 0;

            // Add a relative force of jumpforce on the y axis to the player.
            player.rigidbody.AddRelativeForce(new Vector2(0f, player.jumpForce));
        }

        public override void FixedUpdate()
        {
            // If the timescale is greater than 0,
            if (Time.timeScale > 0) {
                // Add the duration of 1 frame to elapsed time.
                elapsedTime += Time.deltaTime;
            }

            if (player.holding && elapsedTime > 2 * Time.deltaTime)
            {
                player.ThrowHeldProp();
            }
        }
    }
    /// <summary>
    /// The rising state of the player.
    /// </summary>
    public class PlayerRising : PlayerCharacterState {
        /// <summary>
        /// Creates a Player Rising state.
        /// </summary>
        /// <param name="network">Reference to the player's state machine.</param>
        public float acclPerFrame;

        public PlayerRising(PlayerNetwork network) : base("Player Rising", network) { }

        public override void Subject()
        {
            if (player.health <= 0) { Transition("Player Dead"); }
            else if (player.isGrounded) { Transition("Player Idle"); }
            else if (player.localVelocity.y <= 0) { Transition("Player Falling"); }
            else if (Input.GetButtonDown("Flip") && player.canFlip) { Transition("Player Flipping"); }
        }

        public override void OnStateEnter()
        {
            acclPerFrame = player.strafeAcceleration * Time.deltaTime;
        }

        public override void FixedUpdate()
        {
            // Create Vector2 movement where the x value is the horizontal input axis.
            float direction = Input.GetAxisRaw("Horizontal");

            player.Move(acclPerFrame, direction);
        }
    }
    /// <summary>
    /// The falling state of the player.
    /// </summary>
    public class PlayerFalling : PlayerCharacterState {
        /// <summary>
        /// Creates a Player Falling state.
        /// </summary>
        /// <param name="network">Reference to the player's state machine.</param>
        public float acclPerFrame;
        public float elapsed;
        public float freeTime;

        public PlayerFalling(PlayerNetwork network, float freeTime) : base("Player Falling", network) { this.freeTime = freeTime; }

        public override void Subject()
        {
            if (player.health <= 0) { Transition("Player Dead"); }
            else if (player.isGrounded) { Transition("Player Idle"); }
            else if (player.localVelocity.y > 0) { Transition("Player Rising"); }
            else if (Input.GetButtonDown("Flip") && player.canFlip) { Transition("Player Flipping"); }
        }

        public override void OnStateEnter()
        {
            elapsed = 0;
            acclPerFrame = player.strafeAcceleration * Time.deltaTime;
        }

        public override void FixedUpdate()
        {
            // Create Vector2 movement where the x value is the horizontal input axis.
            float direction = Input.GetAxisRaw("Horizontal");

            player.Move(acclPerFrame, direction);
        }
    }
    /// <summary>
    /// The flipping state of the player.
    /// </summary>
    public class PlayerFlipping : PlayerCharacterState {
        /// <summary>
        /// The total duration of the flip animation.
        /// </summary>
        float animationTime;
        /// <summary>
        /// A relative position to the player to animate the rotation.
        /// </summary>
        Vector2 offset;
        /// <summary>
        /// The absolute position to rotate the player around.
        /// </summary>
        Vector2 targetPosition;

        /// <summary>
        /// Flag for when animation is finished.
        /// </summary>
        bool doneAnimating = false;

        /// <summary>
        /// Creates a Player Flipping state.
        /// </summary>
        /// <param name="animationTime">The total duration of the flip animation.</param>
        /// <param name="offset">A relative position to the player to animate the rotation.</param>
        /// <param name="network">Reference to the player's state machine.</param>
        public PlayerFlipping(float animationTime, Vector2 offset, PlayerNetwork network) : base("Player Flipping", network) {
            this.animationTime = animationTime;
            this.offset = offset;
        }

        public override void Subject()
        {
            if (doneAnimating && player.isGrounded) { Transition("Player Walking"); }
            else if (doneAnimating && !player.isGrounded) { Transition("Player Falling"); }
        }

        public override void OnStateEnter()
        {
            // Set finished animating flag to false.
            doneAnimating = false;
            // Set the body type of the player to kinematic.
            player.rigidbody.bodyType = RigidbodyType2D.Kinematic;
            // Set the target position to the flip pivot.
            targetPosition = player.GetFlipPivot(offset);
            
            // Asynchronously spin the player.
            player.StartCoroutine(SpinPlayer());
        }

        public override void OnStateExit()
        {
            // Set the player's body type to dynamic.
            player.rigidbody.bodyType = RigidbodyType2D.Dynamic;

            player.canFlip = false;
        }

        /// <summary>
        /// Asynchronously spin the player around a relative point in space.
        /// </summary>
        /// <returns></returns>
        IEnumerator SpinPlayer() {
            // Create float segment time that is 1/4th the animation time.
            float segmentTime = animationTime / 4f;
            // Create float tick that is the length of 1 frame in seconds.
            float tick = Time.deltaTime;
            // Create float angle that is 180 / (segment time / tick)
            float angle = 180 / (segmentTime / tick);
            float initialAngle = player.transform.eulerAngles.z;


            // Loop the following for every step of stride tick where t is between 0 and segment time.
            for (float t = 0; t < segmentTime; t += tick) {
                // Set the position of the player to the linear interpolation at point t on the line between the position of the player and the target position.
                player.transform.position = Vector2.Lerp(player.transform.position, targetPosition, t);
                player.mainCamera.MoveCamera();
                yield return new WaitForEndOfFrame();
            }
            // Set the player's position to the target position.
            player.transform.position = targetPosition;

            Quaternion targetRotation = Quaternion.LookRotation(Vector3.forward, -player.transform.up);
            // Loop the following for every step of stride tick where t is between 0 and segment time.
            for (float t = 0; t < segmentTime; t += tick) {

                // Rotate the player around the z axis by angle
                player.transform.rotation = Quaternion.Lerp(player.transform.rotation, targetRotation, t);
                player.mainCamera.MoveCamera();
                yield return new WaitForEndOfFrame();
            }

            player.transform.rotation = targetRotation;

            player.down = -player.transform.up;


            // Set the done animating flag to true.
            doneAnimating = true;
        }
    }
    #endregion
}

/// <summary>
/// Player Character State is a data type that will make creating character states easier for the player.
/// </summary>
public class PlayerCharacterState : CharacterState {
    /// <summary>
    /// A reference to the state machine on the player.
    /// </summary>
    protected PlayerNetwork player;

    /// <summary>
    /// Creates a Player Character State.
    /// </summary>
    /// <param name="name">The name of this state.</param>
    /// <param name="network">A reference to the network this state is a part of.</param>
    public PlayerCharacterState(string name, PlayerNetwork network) : base(name, network) {
        player = network;
    }

    public override void Subject() { }
    public override void OnStateEnter() { }
    public override void Update() { }
    public override void FixedUpdate() { }
    public override void OnStateExit() { }
}