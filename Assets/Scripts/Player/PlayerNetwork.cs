using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A basic character controller that utilises the Character State Machine framework.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerNetwork : CharacterStateNetwork {
    [Tooltip("Movement speed on the ground (m/s)")]
    public float speed;
    [Tooltip("Movement speed while in air (m/s)")]
    public float strafeSpeed;
    [Tooltip("Maximum fall speed (m/s)")]
    public float fallSpeed;
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

    /// <summary>
    /// Flag for when the player is on the ground.
    /// </summary>
    [HideInInspector]
    public bool isGrounded;
    /// <summary>
    /// A reference to the object currently being held by the player.
    /// </summary>
    public HoldableObject holding;

    new public Rigidbody2D rigidbody;
    new public Collider2D collider;
    Camera mainCamera;

    /// <summary>
    /// A reference to the hand of the player.
    /// </summary>
    Transform hand;

    #region State Names
    public PlayerDead dead;
    public PlayerWalking walking;
    public PlayerJumping jumping;
    public PlayerRising rising;
    public PlayerFalling falling;
    public PlayerFlipping flipping;
    #endregion

    public void Start()
    {
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();
        hand = transform.Find("Hand");

        if (!hand) { Debug.LogError("No hand on player detected! Make sure there is a 'Hand' child under the player, and name it accordingly."); }

        CreateNetwork();
    }

    public override void Update()
    {
        base.Update();

        // If the timescale is greater than 0,
        if (Time.timeScale > 0)
        {
            // Check if the player is on the ground.
            GroundCheck();

            // If the jump button is pressed, and the player is holding an object,
            if (Input.GetButtonDown("Jump") && holding)
            {
                // Throw the object.
                ThrowHeldProp();
            }

            // If the use button is pressed,
            if (Input.GetButtonDown("Use"))
            {
                // and if the player is walking,
                if (walking)
                {
                    // but not holding anything,
                    if (!holding)
                    {
                        // Pick up the nearest object if it exists.
                        GrabNearestProp();
                    }
                    // or if it is holding something,
                    else
                    {
                        // drop it.
                        ReleaseHeldProp();
                    }
                }
                // or as long as player is not flipping,
                else if (activeState != flipping)
                {
                    // If the player is holding something,
                    if (holding)
                    {
                        // drop it.
                        ReleaseHeldProp();
                    }
                }
            }

            // Cap the velocity of the player to the maximum speed.
            CapVelocity();
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();

        // Add the force of grabity to this object on the relative up vector.
        rigidbody.AddRelativeForce(new Vector2(0f, Physics2D.gravity.y * rigidbody.mass));

        //Debug.Log("The active state is: " + activeState.name);
    }

    /// <summary>
    /// Initializes all the states in the network.
    /// </summary>
    private void CreateNetwork() {
        dead = new PlayerDead(this);
        walking = new PlayerWalking(this);
        jumping = new PlayerJumping(jumpBufferTime, this);
        rising = new PlayerRising(this);
        falling = new PlayerFalling(this);
        flipping = new PlayerFlipping(flipTime, flipOffset, this);

        walking.AddTransition(dead);
        walking.AddTransition(jumping);
        walking.AddTransition(rising);
        walking.AddTransition(falling);
        walking.AddTransition(flipping);

        jumping.AddTransition(dead);
        jumping.AddTransition(walking);
        jumping.AddTransition(rising);
        jumping.AddTransition(falling);
        jumping.AddTransition(flipping);

        rising.AddTransition(dead);
        rising.AddTransition(walking);
        rising.AddTransition(falling);
        rising.AddTransition(flipping);

        falling.AddTransition(dead);
        falling.AddTransition(walking);
        falling.AddTransition(rising);

        flipping.AddTransition(walking);
        flipping.AddTransition(falling);

        activeState = walking;
    }

    /// <summary>
    /// Get the relative position of a point based off of the player's flipped orientation.
    /// </summary>
    /// <param name="offset">how far from the player the point is</param>
    /// <returns></returns>
    public Vector3 GetFlipPivot(Vector3 offset) {
        return transform.position + (transform.localRotation * offset);
    }

    /// <summary>
    /// Get the radius of a circle swept out by the player if it were to flip.
    /// </summary>
    /// <returns>The radius of a circle</returns>
    private float GetFlipRadius() {
        if (transform.localScale.x/2 > transform.localScale.y/2) { return transform.localScale.x/2; }
        else { return transform.localScale.y/2; }
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
        else { Debug.Log(props.Length); }

        // Create Holdable Object nearest that is the first prop.
        HoldableObject nearest = props[0].GetComponent<HoldableObject>();

        // For every prop in the list of props,
        foreach (Collider2D prop in props) {
            // If the distance between the prop and the player is less than the nearest prop and the player,  
            if (Vector2.Distance(prop.transform.position, transform.position) < Vector2.Distance(nearest.transform.position, transform.position)) {
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
    private void ReleaseHeldProp() {
        // If the player is not holding anything, stop the algorythm.
        if (!holding) { return; }

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

        // Create float x direction that is 1.
        float xDirection = 1;

        // If the x-scale of this object is positive, x direction is 1.
        if (transform.localScale.x > 0) { xDirection = 1; }
        // Otherwise, x direction is -1.
        else { xDirection = -1; }

        // Create Rigidbody2D prop body that is the rigidbody attached to the held prop.
        Rigidbody2D propBody = holding.GetComponent<Rigidbody2D>();
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
    private IEnumerator MovePropToHand(float animationTime, HoldableObject prop) {
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
            // Wait for 1 frame.
            yield return new WaitForEndOfFrame();
        }

        // Set the parent of the prop to the player's hand.
        prop.transform.parent = hand;
    }

    /// <summary>
    /// Check if the player is on the ground.
    /// </summary>
    private void GroundCheck() {
        // Create a Collider2D collider that is any environment piece that is within a box around the player that is half the player's width, and 0.2 m tall.
        Collider2D collider = Physics2D.OverlapBox(transform.position, new Vector2(transform.lossyScale.x/2, 0.2f), 0, 1 << LayerMask.NameToLayer("Environment"));

        // If the collider exists, the player is grounded.
        if (collider) { isGrounded = true; }
        // Otherwise, the player is not grounded.
        else { isGrounded = false; }
    }

    /// <summary>
    /// Cap the player's velocity so that the magnitude is never greater than the maximum speed.
    /// </summary>
    private void CapVelocity() {
        // If the absolute value of the x component of the player's velocity is greater than the max speed,
        if (Mathf.Abs(rigidbody.velocity.x) > maxSpeed) {
            // Create int direction that is the direction of the x component of the player's velocity (-1, or 1).
            int direction = (int) (rigidbody.velocity.x / Mathf.Abs(rigidbody.velocity.x));
            // Set the player's velocity x value to be max speed * direction.
            rigidbody.velocity = new Vector2(direction * maxSpeed, rigidbody.velocity.y);
        }

        // If the absolute value of the player's velocity y value is greater than fall speed,
        if (Mathf.Abs(rigidbody.velocity.y) > fallSpeed)
        {
            // Create int direction that is the direction of the y component of the player's velocity (-1, or 1).
            int direction = (int)(rigidbody.velocity.y / Mathf.Abs(rigidbody.velocity.y));
            // Set the player's velocity y value to be direction * fall speed.
            rigidbody.velocity = new Vector2(rigidbody.velocity.x, direction * fallSpeed);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        if (!collider) { collider = GetComponent<Collider2D>(); }

        Gizmos.DrawWireSphere(GetFlipPivot(flipOffset), GetFlipRadius());
        Gizmos.DrawWireSphere(transform.position, maxReach);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, new Vector2(transform.localScale.x / 2, 0.2f));

        if (Application.isPlaying) { Debug.DrawRay(transform.position, rigidbody.velocity, Color.red); }
    }

    /// <summary>
    /// Get the relative velocity of the player.
    /// </summary>
    /// <returns></returns>
    public Vector3 LocalVelocity() {
        return transform.InverseTransformDirection(rigidbody.velocity);
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
    /// <summary>
    /// The walking state of the player.
    /// </summary>
    public class PlayerWalking : PlayerCharacterState {
        /// <summary>
        /// Creates a Player Walking state.
        /// </summary>
        /// <param name="network">Reference to the player's state machine.</param>
        public PlayerWalking(PlayerNetwork network) : base("Player Walking", network) { }

        public override void Subject()
        {
            if (player.health <= 0) { Transition("Player Dead"); }
            else if (player.holding == null && Input.GetButtonDown("Jump")) { Transition("Player Jumping"); }
            else if (!player.isGrounded && player.LocalVelocity().y < 0) { Transition("Player Falling"); }
            else if (!player.isGrounded && player.LocalVelocity().y >= 0) { Transition("Player Rising"); }
            else if (Input.GetButtonDown("Flip")) { Transition("Player Flipping"); }
        }

        public override void FixedUpdate()
        {
            // Create Vector2 movement where the x value is the horizontal input axis.
            Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), 0);

            // Add movement * the local rotation of the player * the player's speed as a force to the player.
            player.rigidbody.AddForce(player.transform.localRotation * movement * player.speed);


            if (movement.x > 0) {
                player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z);
            }
            else if (movement.x < 0) {
                player.transform.localScale = new Vector3(-Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z);
            }
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
        /// <summary>
        /// Time elapsed in this state.
        /// </summary>
        float elapsedTime;

        /// <summary>
        /// Creates a Player Jumping state.
        /// </summary>
        /// <param name="maxTime">The jump buffer time. Time to stay in this state.</param>
        /// <param name="network">Reference to the player's state machine.</param>
        public PlayerJumping(float maxTime, PlayerNetwork network) : base("Player Jumping", network) {
            this.maxTime = maxTime;
            elapsedTime = 0;
        }

        public override void Subject()
        {
            if (player.health <= 0f) { Transition("Player Dead"); }
            else if (player.isGrounded) { Transition("Player Walking"); }
            else if (player.LocalVelocity().y < 0 && elapsedTime > maxTime) { Transition("Player Falling"); }
            else if (player.LocalVelocity().y >= 0 && elapsedTime > maxTime) { Transition("Player Rising"); }
            else if (Input.GetButtonDown("Flip")) { Transition("Player Flip"); }
        }

        public override void OnStateEnter()
        {
            // If the player is grounded,
            if (player.isGrounded)
            {
                // Add a relative force of jumpforce on the y axis to the player.
                player.rigidbody.AddRelativeForce(new Vector2(0f, player.jumpForce));
            }
        }

        public override void FixedUpdate()
        {
            // If the timescale is greater than 0,
            if (Time.timeScale > 0) {
                // Add the duration of 1 frame to elapsed time.
                elapsedTime += Time.deltaTime;
            }

            // Create a Vector2 movement where the x component is the horizontal input axis, and the y component is the vertical input axis.
            Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            // Add movement * player's local rotation * player strafe speed as a force on the player.
            player.rigidbody.AddForce(player.transform.localRotation * movement * player.strafeSpeed);

            if (movement.x > 0) { player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
            else if (movement.x < 0) { player.transform.localScale = new Vector3(-Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
        }

        public override void OnStateExit()
        {
            elapsedTime = 0f;
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
        public PlayerRising(PlayerNetwork network) : base("Player Rising", network) { }

        public override void Subject()
        {
            if (player.health <= 0) { Transition("Player Dead"); }
            else if (player.isGrounded) { Transition("Player Walking"); }
            else if (player.LocalVelocity().y < 0) { Transition("Player Falling"); }
            else if (Input.GetButtonDown("Flip")) { Transition("Player Flipping"); }
        }

        public override void FixedUpdate()
        {
            // Create a Vector2 movement where the x component is the horizontal input axis, and the y component is the vertical input axis.
            Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            // Add movement * player's local rotation * player strafe speed as a force on the player.
            player.rigidbody.AddForce(player.transform.localRotation * movement * player.strafeSpeed);

            if (movement.x > 0) { player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
            else if (movement.x < 0) { player.transform.localScale = new Vector3(-Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
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
        public PlayerFalling(PlayerNetwork network) : base("Player Falling", network) { }

        public override void Subject()
        {
            if (player.health <= 0) { Transition("Player Dead"); }
            else if (player.isGrounded) { Transition("Player Walking"); }
            else if (player.LocalVelocity().y >= 0) { Transition("Player Rising"); }
        }

        public override void FixedUpdate()
        {
            // Create a Vector2 movement where the x component is the horizontal input axis, and the y component is the vertical input axis.
            Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            // Add movement * player's local rotation * player strafe speed as a force on the player.
            player.rigidbody.AddForce(player.transform.localRotation * movement * player.strafeSpeed);

            if (movement.x > 0) { player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
            else if (movement.x < 0) { player.transform.localScale = new Vector3(-Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
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
            // Pause time.
            Time.timeScale = 0;
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
            // Resume time at normal speed.
            Time.timeScale = 1;
            // Set the player's body type to dynamic.
            player.rigidbody.bodyType = RigidbodyType2D.Dynamic;
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
<<<<<<< HEAD

            //Rotation Tests
            Quaternion newUp = Quaternion.Euler(0, player.transform.eulerAngles.y + 180f, player.transform.eulerAngles.z + 180f);
            //Quaternion newUp = Quaternion.Euler(0, 0, player.transform.eulerAngles.z + 180f);
            //Quaternion newUp = Quaternion.Euler(player.transform.eulerAngles.x + 180f, 0, 0);
=======
            // Create Quaternion new up from a euler angle: 0, 0, player's z angle + 180
            Quaternion newUp = Quaternion.Euler(0,0,player.transform.eulerAngles.z + 180f);
>>>>>>> afdb678adf22639bbb83c015c312180466e70e2a

            // Loop the following for every step of stride tick where t is between 0 and segment time.
            for (float t = 0; t < segmentTime; t += tick) {
                // Set the position of the player to the linear interpolation at point t on the line between the position of the player and the target position.
                player.transform.position = Vector2.Lerp(player.transform.position, targetPosition, t);
                yield return new WaitForEndOfFrame();
            }
            // Set the player's position to the target position.
            player.transform.position = targetPosition;

            // Loop the following for every step of stride tick where t is between 0 and segment time.
            for (float t = 0; t < segmentTime; t += tick) {
                // Set the local rotation of the player to the linear interpolation at point t on the line between the player's local rotation and the new up.
                player.transform.localRotation = Quaternion.Lerp(player.transform.localRotation, newUp, t);
                yield return new WaitForEndOfFrame();
            }
            // Set the player's local rotation to new up.
            player.transform.localRotation = newUp;

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