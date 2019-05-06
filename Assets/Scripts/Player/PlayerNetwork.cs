using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class PlayerNetwork : CharacterStateNetwork {

    public float speed;
    public float strafeSpeed;
    public float fallSpeed;
    public float maxSpeed;
    public float jumpForce;
    public float health;
    public float maxHealth;
    public float jumpBufferTime;
    public float flipTime;
    public float maxReach;
    public Vector2 flipOffset;
    public Vector2 throwForce;

    public bool isGrounded;
    public HoldableObject holding;

    new public Rigidbody2D rigidbody;
    new public Collider2D collider;
    Camera mainCamera;

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

        if (Time.timeScale > 0)
        {
            GroundCheck();

            if (Input.GetButtonDown("Jump") && holding)
            {
                ThrowHeldProp();
            }

            if (Input.GetButtonDown("Grab"))
            {

                if (walking)
                {
                    if (!holding)
                    {
                        GrabNearestProp();
                    }
                    else
                    {
                        ReleaseHeldProp();
                    }
                }
                else if (activeState != flipping)
                {
                    if (holding)
                    {
                        ReleaseHeldProp();
                    }
                }
            }

            CapVelocity();

            
        }
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
        rigidbody.AddRelativeForce(new Vector2(0f, Physics2D.gravity.y * rigidbody.mass));
        Debug.Log("The active state is: " + activeState.name);
    }

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

    private float GetFlipRadius() {
        if (transform.localScale.x/2 > transform.localScale.y/2) { return transform.localScale.x/2; }
        else { return transform.localScale.y/2; }
    }

    private void GrabNearestProp() {
        if (holding) { ReleaseHeldProp(); }

        Collider2D[] props = Physics2D.OverlapCircleAll(transform.position, maxReach, 1 << LayerMask.NameToLayer("Prop"));
        if (props.Length == 0) { return; }
        else { Debug.Log(props.Length); }

        HoldableObject nearest = props[0].GetComponent<HoldableObject>();
        foreach (Collider2D prop in props) {
            if (Vector2.Distance(prop.transform.position, transform.position) < Vector2.Distance(nearest.transform.position, transform.position)) {
                nearest = prop.GetComponent<HoldableObject>();
            }
        }

        if (nearest == null) { return; }

        StartCoroutine(MovePropToHand(0.5f, nearest));
    }

    private void ReleaseHeldProp() {
        if (!holding) { return; }

        holding.transform.parent = null;

        Rigidbody2D propBody = holding.GetComponent<Rigidbody2D>();
        propBody.bodyType = RigidbodyType2D.Dynamic;
        holding = null;
    }

    private void ThrowHeldProp() {
        if (!holding) { return; }

        float xDirection = 1;
        if (transform.localScale.x > 0) { xDirection = 1; }
        else { xDirection = -1; }

        Rigidbody2D propBody = holding.GetComponent<Rigidbody2D>();
        ReleaseHeldProp();
        propBody.velocity = rigidbody.velocity;
        propBody.AddForce(transform.rotation * new Vector3(throwForce.x * xDirection, throwForce.y, 0f));
    }

    private IEnumerator MovePropToHand(float animationTime, HoldableObject prop) {
        holding = prop;
        Rigidbody2D propBody = prop.GetComponent<Rigidbody2D>();
        propBody.bodyType = RigidbodyType2D.Kinematic;
        propBody.velocity = Vector2.zero;

        for (float t = 0; t < animationTime; t += Time.deltaTime) {
            prop.transform.position = Vector3.Lerp(prop.transform.position, hand.position, t);
            yield return new WaitForEndOfFrame();
        }

        prop.transform.parent = hand;
    }

    private void GroundCheck() {
        Collider2D collider = Physics2D.OverlapBox(transform.position, new Vector2(transform.lossyScale.x/2, 0.2f), 0, 1 << LayerMask.NameToLayer("Environment"));

        if (collider) { isGrounded = true; }
        else { isGrounded = false; }
    }

    private void CapVelocity() {
        if (Mathf.Abs(rigidbody.velocity.x) > maxSpeed) {
            int direction = (int) (rigidbody.velocity.x / Mathf.Abs(rigidbody.velocity.x));
            rigidbody.velocity = new Vector2(direction * maxSpeed, rigidbody.velocity.y);
        }

        if (Mathf.Abs(rigidbody.velocity.y) > fallSpeed)
        {
            int direction = (int)(rigidbody.velocity.y / Mathf.Abs(rigidbody.velocity.y));
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

    public Vector3 LocalVelocity() {
        return transform.InverseTransformDirection(rigidbody.velocity);
    }

    #region State Definitions
    public class PlayerDead : PlayerCharacterState
    {
        public PlayerDead(PlayerNetwork network) : base("Player Dead", network) { }

        public override void OnStateEnter()
        {
            player.rigidbody.bodyType = RigidbodyType2D.Kinematic;
            GameMaster.gameMaster.RestartLevel();
        }
    }
    public class PlayerWalking : PlayerCharacterState {

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
            Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), 0);

            player.rigidbody.AddForce(player.transform.localRotation * movement * player.speed);

            if (movement.x > 0) { player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
            else if (movement.x < 0) { player.transform.localScale = new Vector3(-Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
        }
    }
    public class PlayerJumping : PlayerCharacterState {
        public float maxTime;
        float elapsedTime;

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
            if (player.isGrounded)
            {
                player.rigidbody.AddRelativeForce(new Vector2(0f, player.jumpForce));
            }
        }

        public override void FixedUpdate()
        {
            if (Time.timeScale > 0) {
                elapsedTime += Time.deltaTime;
            }

            Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            player.rigidbody.AddForce(player.transform.localRotation * movement * player.strafeSpeed);

            if (movement.x > 0) { player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
            else if (movement.x < 0) { player.transform.localScale = new Vector3(-Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
        }

        public override void OnStateExit()
        {
            elapsedTime = 0f;
        }
    }
    public class PlayerRising : PlayerCharacterState {
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
            Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            player.rigidbody.AddForce(player.transform.localRotation * movement * player.strafeSpeed);

            if (movement.x > 0) { player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
            else if (movement.x < 0) { player.transform.localScale = new Vector3(-Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
        }
    }
    public class PlayerFalling : PlayerCharacterState {
        public PlayerFalling(PlayerNetwork network) : base("Player Falling", network) { }

        public override void Subject()
        {
            if (player.health <= 0) { Transition("Player Dead"); }
            else if (player.isGrounded) { Transition("Player Walking"); }
            else if (player.LocalVelocity().y >= 0) { Transition("Player Rising"); }
        }

        public override void FixedUpdate()
        {
            Vector2 movement = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

            player.rigidbody.AddForce(player.transform.localRotation * movement * player.strafeSpeed);

            if (movement.x > 0) { player.transform.localScale = new Vector3(Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
            else if (movement.x < 0) { player.transform.localScale = new Vector3(-Mathf.Abs(player.transform.localScale.x), player.transform.localScale.y, player.transform.localScale.z); }
        }
    }
    public class PlayerFlipping : PlayerCharacterState {
        float animationTime;
        Vector2 offset;
        Vector2 targetPosition;

        bool doneAnimating = false;

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
            Time.timeScale = 0;
            doneAnimating = false;
            player.rigidbody.bodyType = RigidbodyType2D.Kinematic;
            targetPosition = player.GetFlipPivot(offset);

            player.StartCoroutine(SpinPlayer());
        }

        public override void OnStateExit()
        {
            Time.timeScale = 1;
            player.rigidbody.bodyType = RigidbodyType2D.Dynamic;
        }

        IEnumerator SpinPlayer() {
            float segmentTime = animationTime / 4f;
            float tick = Time.deltaTime;
            Quaternion newUp = Quaternion.Euler(0,0,player.transform.eulerAngles.z + 180f);

            for (float t = 0; t < segmentTime; t += tick) {
                player.transform.position = Vector2.Lerp(player.transform.position, targetPosition, t);
                yield return new WaitForEndOfFrame();
            }
            player.transform.position = targetPosition;

            for (float t = 0; t < segmentTime; t += tick) {
                player.transform.localRotation = Quaternion.Lerp(player.transform.localRotation, newUp, t);
                yield return new WaitForEndOfFrame();
            }
            player.transform.localRotation = newUp;

            doneAnimating = true;

        }
    }
    #endregion
}

public class PlayerCharacterState : CharacterState {
    protected PlayerNetwork player;

    public PlayerCharacterState(string name, PlayerNetwork network) : base(name, network) {
        player = network;
    }

    public override void Subject() { }
    public override void OnStateEnter() { }
    public override void Update() { }
    public override void FixedUpdate() { }
    public override void OnStateExit() { }
}