using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RelativeGravity))]
public class HoldableObjectV2 : MonoBehaviour
{

    public UnityEngine.Events.UnityEvent onPickedUp;
    public UnityEngine.Events.UnityEvent onReleased;

    RelativeGravity relativeGravity;
    Rigidbody2D rigidbody;

    public Transform _lastParent;

    Vector3 startPos;
    Quaternion startRotation;
    Transform startParent;

    public bool held = false;

    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.localPosition;
        startRotation = transform.rotation;
        startParent = transform.parent;

        relativeGravity = GetComponent<RelativeGravity>();
        rigidbody = GetComponent<Rigidbody2D>();

        onPickedUp.AddListener(() => { 
            held = true;
            rigidbody.simulated = false;
        });
        onReleased.AddListener(() => { 
            held = false;
            rigidbody.simulated = true;
        });
    }

    // Update is called once per frame
    void Update()
    {
        relativeGravity.enabled = !held;

        Debug.DrawRay(transform.position, rigidbody.velocity, Color.red);
    }

    private void LateUpdate()
    {
        _lastParent = transform.parent;
    }

    public void Respawn() {
        rigidbody.velocity = Vector2.zero;
        transform.parent = startParent;
        transform.localPosition = startPos;
        transform.rotation = startRotation;
        GetComponent<RelativeGravity>().SetGravityDirection(-transform.up);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Danger")) { Respawn(); }
    }
}
