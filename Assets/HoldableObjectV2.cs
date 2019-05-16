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

    Transform _lastParent;

    Vector3 startPos;
    Quaternion startRotation;
    Transform startParent;

    // Start is called before the first frame update
    void Start()
    {
        startPos = transform.position;
        startRotation = transform.rotation;
        startParent = transform.parent;

        relativeGravity = GetComponent<RelativeGravity>();
        rigidbody = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (transform.parent)
        {
            if (transform.parent.CompareTag("Player") && _lastParent == null) { onPickedUp.Invoke(); }
        }
        else if (_lastParent) {
            if (_lastParent.CompareTag("Player") && transform.parent == null) { onReleased.Invoke(); }
        }
    }

    private void LateUpdate()
    {
        _lastParent = transform.parent;
    }

    public void Respawn() {
        rigidbody.velocity = Vector2.zero;
        transform.parent = startParent;
        transform.position = startPos;
        transform.rotation = startRotation;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Danger")) { Respawn(); }
    }
}
