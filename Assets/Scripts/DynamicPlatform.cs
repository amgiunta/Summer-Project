using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class DynamicPlatform : MonoBehaviour {

    public bool orientWithPlayer;
    public float friction;

    [HideInInspector]
    public PlayerNetwork player;
    [HideInInspector]
    new public Rigidbody2D rigidbody;
    [HideInInspector]
    new public Collider2D collider;
    public bool standable = true;

    [HideInInspector]
    public Vector3 velocity;

    private Vector3 _lastPosition;

	// Use this for initialization
	void Start () {
        FindPlayer();
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
	}
	
	// Update is called once per frame
	public virtual void FixedUpdate () {
        CalcVelocity();

        _lastPosition = transform.position;
	}

    public virtual void FindPlayer() {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerNetwork>();

        if (!player) { Debug.LogError("Could not locate player! Check to see if the player is in the scene, tagged as 'Player', and has the PlayerNetwors (or some derivative behavior) attached.", this); }
    }

    public virtual void Orient() {
        if (orientWithPlayer && player)
        {
            transform.localRotation = player.transform.localRotation;
        }
        else { transform.localRotation = Quaternion.identity; }
    }

    protected virtual void CalcVelocity() {
        velocity = (transform.position - _lastPosition) /Time.deltaTime;
    }

    protected virtual void OnCollisionEnter2D(Collision2D other) {
        if (standable && other.transform.CompareTag("Player") || other.transform.CompareTag("Prop")) { other.transform.parent = transform; }
    }

    protected virtual void OnCollisionExit2D(Collision2D other) {
        if (standable && other.transform.CompareTag("Player") || other.transform.CompareTag("Prop"))
        {
            other.transform.parent = null;
            other.gameObject.GetComponent<Rigidbody2D>().velocity += (Vector2)velocity;
            Debug.Log("object left platform.", other.transform);
        }
    }
}
